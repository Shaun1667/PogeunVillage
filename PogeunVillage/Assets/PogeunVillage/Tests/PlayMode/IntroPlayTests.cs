using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// P-5a · b 확인(BALANCE 20번): 저장 없이 시작하면 인트로 — 시계 · 플레이어 · HUD 멈춤 · 숨김, 마을 → 뒤로 빠지기(시작 순간 모니터 화면이 방 카메라를 꽉 채움) → 방 → 끝나면 조작.
    /// ESC = 건너뛰기(클릭 · 다른 키는 무시). 이어하기에는 인트로 없음(다른 저장 테스트는 AutoPlay가 꺼져 있음).
    /// </summary>
    public class IntroPlayTests : ToolSceneTest
    {
        protected override bool WithIntro => true;

        IntroSequence intro;

        [UnitySetUp]
        public IEnumerator FindIntro()
        {
            intro = Object.FindAnyObjectByType<IntroSequence>();
            Assert.IsNotNull(intro);
            yield break;
        }

        [UnityTest]
        public IEnumerator NewGame_IntroPlays_ThenControlReturns()
        {
            Assert.IsTrue(intro.IsPlaying, "저장 없음 = 새로 시작 = 인트로");
            Assert.IsFalse(intro.dayCycle.enabled, "시계 멈춤");
            Assert.IsFalse(intro.playerLock.IsFree, "플레이어 멈춤");
            Assert.IsFalse(intro.playerVisual.activeSelf, "플레이어 숨김");
            Assert.IsFalse(intro.hud.enabled, "HUD 숨김");

            intro.JumpTo(intro.settings.villageTime + 0.01f);
            Assert.AreEqual(IntroPhase.PullOut, intro.Phase);
            Assert.IsTrue(intro.roomCamera.enabled);
            Assert.IsNotNull(intro.mainCamera.targetTexture, "마을 카메라 → 모니터 화면");

            intro.JumpTo(intro.Total + 0.01f);
            yield return null;
            Assert.IsFalse(intro.IsPlaying);
            Assert.IsNull(intro.mainCamera.targetTexture);
            Assert.IsFalse(intro.roomCamera.enabled);
            Assert.IsTrue(intro.follow.enabled, "카메라가 플레이어를 따라감");
            Assert.IsTrue(intro.playerVisual.activeSelf);
            Assert.IsTrue(intro.hud.enabled);
            Assert.IsTrue(intro.dayCycle.enabled);
            yield return null;
            Assert.IsTrue(intro.playerLock.IsFree, "조작 가능");
        }

        // P-5d: 방 소품은 Varco3D 모델(그릇 Model_Room…), 모니터 화면 쿼드는 모니터 화면 칸 안(테두리 앞으로 튀어나오지 않음)
        [UnityTest]
        public IEnumerator Room_UsesModels_ScreenOnMonitorFront()
        {
            var room = intro.transform;
            foreach (string id in new[] { "RoomDesk", "RoomMonitor", "RoomKeyboard", "RoomMouse", "RoomLamp", "RoomMug", "RoomWindow" })
            {
                var holder = room.Find("Model_" + id);
                Assert.IsNotNull(holder, id);
                Assert.IsNotNull(holder.GetComponentInChildren<MeshRenderer>(), id + " 모델");
            }
            Assert.IsNull(room.Find("MonitorBezel"), "그레이박스 모니터 없음");

            var monitor = room.Find("Model_RoomMonitor").GetComponentInChildren<MeshRenderer>().bounds;
            var screen = intro.screen.transform.position;
            Assert.GreaterOrEqual(screen.z, monitor.min.z, "테두리 앞으로 튀어나오지 않음(화면 칸 안으로 들어감)");
            Assert.Less(screen.z - monitor.min.z, monitor.size.z * 0.5f, "모니터 앞쪽(뒷면이 아님)");
            Assert.Greater(screen.y, monitor.min.y + monitor.size.y * 0.3f, "받침대가 아닌 화면 높이");
            Assert.Less(screen.y, monitor.max.y);

            // 게임 화면이 모니터 화면 칸(모델 폭 0.10 ~ 0.91 · 높이 0.42 ~ 0.88) 안 — 테두리 · 모서리로 튀어나오지 않음
            var quad = intro.screen.bounds;
            float Fx(float x) => (x - monitor.min.x) / monitor.size.x;
            float Fy(float y) => (y - monitor.min.y) / monitor.size.y;
            Assert.Greater(Fx(quad.min.x), 0.10f, "왼쪽");
            Assert.Less(Fx(quad.max.x), 0.91f, "오른쪽");
            Assert.Greater(Fy(quad.min.y), 0.42f, "아래");
            Assert.Less(Fy(quad.max.y), 0.88f, "위");
            yield break;
        }

        // P-5d 저녁 조명: 방 물건은 해를 조금만 받고(마을은 그대로), 스탠드 · 모니터 · 창 점 조명이 방 안에
        [UnityTest]
        public IEnumerator Room_EveningLights()
        {
            var room = intro.transform;
            foreach (string name in new[] { "LampLight", "MonitorLight", "WindowLight" })
            {
                var light = room.Find(name).GetComponent<Light>();
                Assert.AreEqual(LightType.Point, light.type, name);
                Assert.Greater(light.intensity, 0f, name);
                Assert.Less(Vector3.Distance(light.transform.position, room.position), 4f, name + "는 방 안");
            }
            var desk = room.Find("Model_RoomDesk").GetComponentInChildren<MeshRenderer>().sharedMaterial;
            Assert.AreEqual(intro.settings.roomSunlight, desk.GetFloat("_MainLightScale"), 1e-4f, "방은 해를 조금만");
            var fountain = GameObject.Find("Model_Fountain").GetComponentInChildren<MeshRenderer>().sharedMaterial;
            Assert.AreEqual(1f, fountain.GetFloat("_MainLightScale"), 1e-4f, "마을은 그대로");
            yield break;
        }

        [UnityTest]
        public IEnumerator PullOutStart_ScreenFillsRoomCamera()
        {
            intro.JumpTo(intro.settings.villageTime);
            var cam = intro.roomCamera;
            var screen = intro.screen.transform;
            Vector3 bottomLeft = cam.WorldToViewportPoint(screen.TransformPoint(new Vector3(-0.5f, -0.5f, 0f)));
            Vector3 topRight = cam.WorldToViewportPoint(screen.TransformPoint(new Vector3(0.5f, 0.5f, 0f)));
            Assert.AreEqual(0f, bottomLeft.x, 0.01f);
            Assert.AreEqual(0f, bottomLeft.y, 0.01f);
            Assert.AreEqual(1f, topRight.x, 0.01f);
            Assert.AreEqual(1f, topRight.y, 0.01f);
            Assert.Greater(bottomLeft.z, 0f, "화면 앞에서 봄");

            intro.JumpTo(intro.settings.villageTime + intro.settings.pullOutTime);
            Vector3 end = cam.WorldToViewportPoint(screen.TransformPoint(new Vector3(0.5f, 0.5f, 0f)));
            Assert.Less(end.x, 1f, "뒤로 빠지면 화면이 작아짐");
            yield break;
        }

        // P-5b: 방 → 화면 속 '게임 시작'과 커서 → 누름 → 모니터로 돌진(화면을 뚫고 들어감) · 섬광 → 마을로 컷 · 섬광이 걷힘
        [UnityTest]
        public IEnumerator Click_Dive_CutsToVillage()
        {
            intro.JumpTo(intro.StartOf(IntroPhase.Room) + 0.01f);
            Assert.IsTrue(intro.screenUi.enabled, "모니터에 제목 · 버튼");
            Assert.IsFalse(intro.cursor.gameObject.activeSelf);

            float click = intro.StartOf(IntroPhase.Click);
            intro.JumpTo(click + 0.01f);
            Assert.IsTrue(intro.cursor.gameObject.activeSelf, "커서 나타남");
            Assert.IsTrue(intro.cursor.GetComponent<CursorArrow>(), "화살표 모양(P-4l)");
            Assert.IsFalse(intro.ButtonPressed);
            float before = Vector2.Distance(intro.cursor.anchoredPosition, intro.startButton.anchoredPosition);
            intro.JumpTo(click + intro.settings.clickTime * intro.settings.clickPressAt + 0.01f);
            Assert.IsTrue(intro.ButtonPressed, "누름");
            Assert.Less(Vector2.Distance(intro.cursor.anchoredPosition, intro.startButton.anchoredPosition), before, "커서가 버튼으로");
            Assert.Less(intro.startButton.localScale.x, 1f, "눌린 버튼");

            float dive = intro.StartOf(IntroPhase.Dive);
            intro.JumpTo(dive + intro.settings.diveTime - 0.01f);
            Assert.AreEqual(IntroPhase.Dive, intro.Phase);
            var cam = intro.roomCamera;
            var screen = intro.screen.transform;
            Vector3 topRight = cam.WorldToViewportPoint(screen.TransformPoint(new Vector3(0.5f, 0.5f, 0f)));
            Assert.Greater(topRight.x, 1f, "화면을 뚫고 들어감(화면이 시야보다 큼)");
            Assert.Greater(intro.flash.color.a, 0.9f, "섬광");

            intro.JumpTo(intro.StartOf(IntroPhase.Fall) + 0.01f);
            Assert.AreEqual(IntroPhase.Fall, intro.Phase);
            Assert.IsNull(intro.mainCamera.targetTexture, "마을로 컷");
            Assert.IsFalse(intro.roomCamera.enabled);
            Assert.IsFalse(intro.screenUi.enabled);
            float early = intro.flash.color.a;
            intro.JumpTo(intro.StartOf(IntroPhase.Fall) + intro.settings.fallTime * 0.8f);
            Assert.Less(intro.flash.color.a, early, "섬광이 걷힘");
            yield break;
        }

        // P-5c: 하늘에서 떨어짐(높이 → 0, 점점 빠름) → 착지(찌그러짐 · 먼지) → 카메라가 평소 따라가는 자리로 → 조작
        [UnityTest]
        public IEnumerator Fall_Land_Settle()
        {
            var visual = intro.playerVisual.transform;
            float fall = intro.StartOf(IntroPhase.Fall);
            intro.JumpTo(fall + 0.01f);
            Assert.IsTrue(intro.playerVisual.activeSelf, "떨어지는 플레이어가 보임");
            float high = visual.localPosition.y;
            Assert.Greater(high, intro.settings.fallHeight * 0.9f, "하늘 높이에서");
            intro.JumpTo(fall + intro.settings.fallTime * 0.5f);
            float mid = visual.localPosition.y;
            intro.JumpTo(fall + intro.settings.fallTime * 0.9f);
            Assert.Greater(high - mid, 0f, "아래로");
            Assert.Greater(mid - visual.localPosition.y, (high - mid) * 0.5f, "점점 빨라짐");

            float land = intro.StartOf(IntroPhase.Land);
            intro.JumpTo(land + 0.01f);
            Assert.Less(visual.localScale.y, 0.8f, "착지 찌그러짐");
            Assert.IsTrue(intro.landingDust.gameObject.activeSelf, "먼지");
            Assert.AreEqual(intro.player.position.x, intro.landingDust.position.x, 0.01f, "먼지는 착지 자리");
            Assert.AreEqual(3, intro.landingDust.GetChild(0).childCount, "먼지는 구름 덩어리(P-4l)");
            float dustLow = intro.landingDust.GetChild(0).localPosition.y;
            intro.JumpTo(land + 0.1f);
            Assert.Greater(intro.landingDust.GetChild(0).localPosition.y, dustLow, "먼지가 떠오름");

            intro.JumpTo(intro.StartOf(IntroPhase.Settle) + intro.settings.settleTime * 0.99f);
            Assert.IsFalse(intro.landingDust.gameObject.activeSelf);
            Assert.AreEqual(1f, visual.localScale.y, 0.01f);
            var world = intro.follow.settings;
            Vector3 followPosition = CameraOrbit.Position(intro.player.position + Vector3.up * world.cameraHeightOffset, world.cameraPitch, 0f, world.cameraDistance);
            Assert.Less(Vector3.Distance(intro.mainCamera.transform.position, followPosition), 0.3f, "카메라가 평소 자리 가까이");

            intro.JumpTo(intro.Total + 0.01f);
            yield return null;
            yield return null;
            Assert.IsFalse(intro.IsPlaying);
            Assert.Less(Vector3.Distance(intro.mainCamera.transform.position, followPosition), 0.3f, "따라가기로 넘어갈 때 튀지 않음");
            Assert.IsTrue(intro.playerLock.IsFree);
        }

        [UnityTest]
        public IEnumerator ClickOrOtherKey_DoesNotSkip()
        {
            yield return null;
            yield return Tap(mouse.leftButton);
            Assert.IsTrue(intro.IsPlaying, "클릭으로는 안 넘어감");
            yield return Tap(keyboard.spaceKey);
            Assert.IsTrue(intro.IsPlaying, "다른 키로도 안 넘어감");
            Assert.IsFalse(intro.playerLock.IsFree);
        }

        [UnityTest]
        public IEnumerator Escape_Skips()
        {
            yield return null;
            yield return Tap(keyboard.escapeKey);
            Assert.IsFalse(intro.IsPlaying, "건너뜀");
            Assert.IsNull(intro.mainCamera.targetTexture);
            Assert.IsFalse(intro.flash.enabled, "섬광 없음");
            Assert.IsFalse(intro.screenUi.enabled);
            yield return null;
            Assert.IsTrue(intro.playerLock.IsFree);
        }
    }
}
