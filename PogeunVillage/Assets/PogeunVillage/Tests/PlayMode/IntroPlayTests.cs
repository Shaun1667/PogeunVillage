using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// P-5a 확인(BALANCE 20번): 저장 없이 시작하면 인트로 — 시계 · 플레이어 · HUD 멈춤 · 숨김, 마을 → 뒤로 빠지기(시작 순간 모니터 화면이 방 카메라를 꽉 채움) → 방 → 끝나면 조작.
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

            intro.JumpTo(intro.settings.villageTime + intro.settings.pullOutTime + intro.settings.roomTime + 0.01f);
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

        [UnityTest]
        public IEnumerator AnyKey_Skips()
        {
            yield return null;
            yield return Tap(keyboard.spaceKey);
            Assert.IsFalse(intro.IsPlaying, "건너뜀");
            Assert.IsNull(intro.mainCamera.targetTexture);
            yield return null;
            Assert.IsTrue(intro.playerLock.IsFree);
        }
    }
}
