using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// P-5a · b 확인(BALANCE 20번): 저장 없이 시작하면 인트로 — 시계 · 플레이어 · HUD 멈춤 · 숨김, 마을 → 뒤로 빠지기(시작 순간 모니터 화면이 방 카메라를 꽉 채움) → 방 → 끝나면 조작.
    /// 아무 키 = 건너뛰기. 이어하기에는 인트로 없음(다른 저장 테스트는 AutoPlay가 꺼져 있음).
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

        [UnityTest]
        public IEnumerator AnyKey_Skips()
        {
            yield return null;
            yield return Tap(keyboard.spaceKey);
            Assert.IsFalse(intro.IsPlaying, "건너뜀");
            Assert.IsNull(intro.mainCamera.targetTexture);
            Assert.IsFalse(intro.flash.enabled, "섬광 없음");
            Assert.IsFalse(intro.screenUi.enabled);
            yield return null;
            Assert.IsTrue(intro.playerLock.IsFree);
        }
    }
}
