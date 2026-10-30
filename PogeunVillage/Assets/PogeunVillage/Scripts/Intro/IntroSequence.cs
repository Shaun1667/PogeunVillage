using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PogeunVillage
{
    /// <summary>
    /// 인트로 연출(P-5, GDD 결정 2026-10-30): 마을 화면 가득 → 뒤로 빠지며 아이 방 모니터 화면이었음이 드러남 → 방
    /// → 화면 속 커서가 '게임 시작'을 누름 → 모니터로 돌진 · 섬광 → 마을로 컷(→ P-5c 낙하).
    /// 마을 카메라를 텍스처로 받아 모니터 화면에 띄우고(화면 속 버튼 · 커서도 마을 카메라의 화면 UI), 방 카메라는 화면이 꽉 찬 자리에서 출발 · 돌아옴(매치 컷).
    /// 새로 시작할 때만 시작 화면(StartScreen)이 부름. 그동안 시계 · 플레이어 · HUD를 멈추고 숨김, 아무 키 = 건너뛰기. 시간표는 IntroTimeline, 수치는 IntroSettings.
    /// </summary>
    public class IntroSequence : MonoBehaviour
    {
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        /// <summary>자동 테스트는 끔(VillageSceneTest가 씬을 열기 전에 정함)</summary>
        public static bool AutoPlay = true;

        public IntroSettings settings;
        public Camera mainCamera;
        public CameraFollow follow;
        public Camera roomCamera;
        [Tooltip("모니터 화면(쿼드, 앞 = +Z 쪽에서 봄)")]
        public Renderer screen;
        public Transform player;
        [Tooltip("플레이어 모습(인트로 동안 숨김)")]
        public GameObject playerVisual;
        public PlayerLock playerLock;
        public DayCycle dayCycle;
        public Canvas hud;

        [Header("화면 속 UI · 섬광 (P-5b)")]
        [Tooltip("마을 카메라에 그려지는 화면 UI(모니터에 보임)")]
        public Canvas screenUi;
        public RectTransform startButton;
        public Image startButtonImage;
        public RectTransform cursor;
        [Tooltip("화면 전체 흰 섬광(맨 위 캔버스)")]
        public Image flash;

        public bool IsPlaying { get; private set; }
        public IntroPhase Phase => timeline == null ? IntroPhase.Done : timeline.PhaseAt(time);
        public float Total => timeline?.Total ?? 0f;
        public float StartOf(IntroPhase phase) => timeline?.StartOf(phase) ?? 0f;
        /// <summary>화면 속 버튼이 눌렸는지(클릭 순간 이후)</summary>
        public bool ButtonPressed { get; private set; }

        /// <summary>인트로가 끝남(다 보거나 건너뜀)</summary>
        public event Action Finished;

        IntroTimeline timeline;
        float time;
        int startFrame;
        RenderTexture screenTexture;
        MaterialPropertyBlock block;
        Pose roomStart, roomEnd;
        Color buttonColor;

        public void Play()
        {
            if (!AutoPlay || IsPlaying)
                return;
            if (!settings || !mainCamera || !follow || !roomCamera || !screen || !player || !playerLock || !dayCycle
                || !screenUi || !startButton || !startButtonImage || !cursor || !flash)
            {
                Debug.LogError($"{name}: 인트로 연결이 비어 있습니다.", this);
                return;
            }
            timeline = new IntroTimeline(new[]
            {
                (IntroPhase.Village, settings.villageTime),
                (IntroPhase.PullOut, settings.pullOutTime),
                (IntroPhase.Room, settings.roomTime),
                (IntroPhase.Click, settings.clickTime),
                (IntroPhase.Dive, settings.diveTime),
                (IntroPhase.Fall, settings.fallTime),
            });
            time = 0f;
            startFrame = UnityEngine.Time.frameCount;
            IsPlaying = true;
            buttonColor = startButtonImage.color;

            dayCycle.enabled = false;
            playerLock.Acquire(this);
            if (playerVisual) playerVisual.SetActive(false);
            if (hud) hud.enabled = false;
            follow.enabled = false;
            Vector3 focus = player.position + Vector3.up * 0.8f;
            mainCamera.transform.SetPositionAndRotation(
                CameraOrbit.Position(focus, settings.villagePitch, 0f, settings.villageDistance),
                CameraOrbit.Rotation(settings.villagePitch, 0f));

            // 모니터 화면 = 마을 카메라 그림, 화면 비율은 게임 창과 같게
            screenTexture = new RenderTexture(Mathf.Max(16, Screen.width), Mathf.Max(16, Screen.height), 24) { name = "IntroScreen" };
            block ??= new MaterialPropertyBlock();
            block.SetTexture(BaseMapId, screenTexture);
            screen.SetPropertyBlock(block);
            float aspect = (float)screenTexture.width / screenTexture.height;
            var screenTransform = screen.transform;
            screenTransform.localScale = new Vector3(settings.screenHeight * aspect, settings.screenHeight, 1f);
            roomCamera.aspect = aspect;
            roomCamera.fieldOfView = mainCamera.fieldOfView;
            roomStart = FillPose(screenTransform, settings.screenHeight, roomCamera.fieldOfView);
            Vector3 endPosition = screenTransform.position - screenTransform.forward * settings.roomCameraBack + Vector3.up * settings.roomCameraUp;
            roomEnd = new Pose(endPosition, Quaternion.LookRotation(screenTransform.position + Vector3.down * settings.roomLookDown - endPosition));
            Apply();
        }

        /// <summary>화면(높이 height)이 시야(세로 fov)를 꽉 채우는 카메라 자리(화면 정면)</summary>
        public static Pose FillPose(Transform screen, float height, float fov)
        {
            float distance = height * 0.5f / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            return new Pose(screen.position - screen.forward * distance, Quaternion.LookRotation(screen.forward, screen.up));
        }

        /// <summary>건너뛰기(아무 키) — 바로 끝 상태</summary>
        public void Skip()
        {
            if (IsPlaying)
                Finish();
        }

        /// <summary>시간 맞추기(테스트 · 확인용)</summary>
        public void JumpTo(float seconds)
        {
            if (!IsPlaying)
                return;
            time = Mathf.Max(0f, seconds);
            Apply();
        }

        void Update()
        {
            if (!IsPlaying)
                return;
            if (UnityEngine.Time.frameCount != startFrame && SkipPressed())
            {
                Finish();
                return;
            }
            time += UnityEngine.Time.deltaTime;
            Apply();
        }

        static bool SkipPressed()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var gamepad = Gamepad.current;
            return (keyboard?.anyKey.wasPressedThisFrame ?? false)
                   || (mouse?.leftButton.wasPressedThisFrame ?? false)
                   || (gamepad?.buttonSouth.wasPressedThisFrame ?? false)
                   || (gamepad?.startButton.wasPressedThisFrame ?? false);
        }

        void Apply()
        {
            var phase = timeline.PhaseAt(time);
            if (phase == IntroPhase.Done)
            {
                Finish();
                return;
            }
            float progress = timeline.ProgressAt(time);
            bool inRoom = phase is IntroPhase.PullOut or IntroPhase.Room or IntroPhase.Click or IntroPhase.Dive;
            mainCamera.targetTexture = inRoom ? screenTexture : null;
            roomCamera.enabled = inRoom;
            screenUi.enabled = phase is IntroPhase.Room or IntroPhase.Click or IntroPhase.Dive;
            cursor.gameObject.SetActive(phase is IntroPhase.Click or IntroPhase.Dive);
            SetFlash(phase switch
            {
                IntroPhase.Dive => Mathf.InverseLerp(settings.flashStart, 1f, progress),
                IntroPhase.Fall => 1f - progress,
                _ => 0f,
            });

            switch (phase)
            {
                case IntroPhase.PullOut:
                    PlaceRoomCamera(roomStart, roomEnd, IntroTimeline.EaseOutCubic(progress));
                    break;
                case IntroPhase.Room:
                    float drift = Mathf.Sin(progress * Mathf.PI) * settings.roomDrift;
                    roomCamera.transform.SetPositionAndRotation(roomEnd.position + roomEnd.rotation * new Vector3(drift, drift * 0.5f, 0f), roomEnd.rotation);
                    break;
                case IntroPhase.Click:
                    roomCamera.transform.SetPositionAndRotation(roomEnd.position, roomEnd.rotation);
                    float move = IntroTimeline.EaseInOutSine(progress / Mathf.Max(0.01f, settings.clickPressAt));
                    cursor.anchoredPosition = Vector2.Lerp(startButton.anchoredPosition + settings.cursorStartOffset, startButton.anchoredPosition + settings.cursorEndOffset, move);
                    SetPressed(progress >= settings.clickPressAt);
                    break;
                case IntroPhase.Dive:
                    PlaceRoomCamera(roomEnd, roomStart, IntroTimeline.EaseInCubic(progress) * settings.diveOvershoot);
                    break;
            }
        }

        void PlaceRoomCamera(Pose from, Pose to, float t) =>
            roomCamera.transform.SetPositionAndRotation(Vector3.LerpUnclamped(from.position, to.position, t), Quaternion.SlerpUnclamped(from.rotation, to.rotation, Mathf.Min(t, 1f)));

        void SetPressed(bool pressed)
        {
            ButtonPressed = pressed;
            startButton.localScale = Vector3.one * (pressed ? settings.buttonPressScale : 1f);
            float tint = pressed ? settings.buttonPressTint : 1f;
            startButtonImage.color = new Color(buttonColor.r * tint, buttonColor.g * tint, buttonColor.b * tint, buttonColor.a);
        }

        void SetFlash(float alpha)
        {
            var c = flash.color;
            c.a = Mathf.Clamp01(alpha);
            flash.color = c;
            flash.enabled = c.a > 0f;
        }

        void Finish()
        {
            IsPlaying = false;
            mainCamera.targetTexture = null;
            roomCamera.enabled = false;
            screenUi.enabled = false;
            cursor.gameObject.SetActive(false);
            SetPressed(false);
            SetFlash(0f);
            if (screenTexture)
            {
                screenTexture.Release();
                Destroy(screenTexture);
                screenTexture = null;
            }
            follow.enabled = true;
            if (playerVisual) playerVisual.SetActive(true);
            if (hud) hud.enabled = true;
            playerLock.Release(this);
            dayCycle.enabled = true;
            Finished?.Invoke();
        }

        void OnDestroy()
        {
            if (screenTexture)
            {
                screenTexture.Release();
                Destroy(screenTexture);
            }
        }
    }
}
