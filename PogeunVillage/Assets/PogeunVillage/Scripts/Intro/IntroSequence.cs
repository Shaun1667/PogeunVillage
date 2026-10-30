using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PogeunVillage
{
    /// <summary>
    /// 인트로 연출(P-5, GDD 결정 2026-10-30): 마을 화면 가득 → 뒤로 빠지며 아이 방 모니터 화면이었음이 드러남 → 방 → (P-5b 클릭 · 빨려 들기, P-5c 낙하).
    /// 마을 카메라를 텍스처로 받아 모니터 화면에 띄우고, 방 카메라는 화면이 꽉 찬 자리에서 출발해 끊김 없이 이어짐(매치 컷).
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

        public bool IsPlaying { get; private set; }
        public float Time01 => timeline == null || timeline.Total <= 0f ? 1f : Mathf.Clamp01(time / timeline.Total);
        public IntroPhase Phase => timeline == null ? IntroPhase.Done : timeline.PhaseAt(time);

        /// <summary>인트로가 끝남(다 보거나 건너뜀)</summary>
        public event Action Finished;

        IntroTimeline timeline;
        float time;
        int startFrame;
        RenderTexture screenTexture;
        MaterialPropertyBlock block;
        Pose roomStart, roomEnd;

        public void Play()
        {
            if (!AutoPlay || IsPlaying)
                return;
            if (!settings || !mainCamera || !follow || !roomCamera || !screen || !player || !playerLock || !dayCycle)
            {
                Debug.LogError($"{name}: 인트로 연결이 비어 있습니다.", this);
                return;
            }
            timeline = new IntroTimeline(new[] { settings.villageTime, settings.pullOutTime, settings.roomTime });
            time = 0f;
            startFrame = UnityEngine.Time.frameCount;
            IsPlaying = true;

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
            bool inRoom = phase != IntroPhase.Village;
            mainCamera.targetTexture = inRoom ? screenTexture : null;
            roomCamera.enabled = inRoom;
            if (!inRoom)
                return;
            float progress = timeline.ProgressAt(time);
            if (phase == IntroPhase.PullOut)
            {
                float t = IntroTimeline.EaseOutCubic(progress);
                roomCamera.transform.SetPositionAndRotation(
                    Vector3.LerpUnclamped(roomStart.position, roomEnd.position, t),
                    Quaternion.SlerpUnclamped(roomStart.rotation, roomEnd.rotation, t));
            }
            else
            {
                float drift = Mathf.Sin(progress * Mathf.PI) * settings.roomDrift;
                roomCamera.transform.SetPositionAndRotation(roomEnd.position + roomEnd.rotation * new Vector3(drift, drift * 0.5f, 0f), roomEnd.rotation);
            }
        }

        void Finish()
        {
            IsPlaying = false;
            mainCamera.targetTexture = null;
            roomCamera.enabled = false;
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
