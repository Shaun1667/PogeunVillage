using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// 6-1d 확인: 저장이 없으면 시작 화면 없음 / 있으면 '이어하기(n일차 · 모아 · ★) / 새로 시작' — 그동안 시계 · 플레이어 멈춤,
    /// 이어하기 = 불러옴, 새로 시작 = 한 번 더 묻고(아니요 · Esc는 돌아감) 저장을 지우고 지금 마을 그대로.
    /// </summary>
    public class StartScreenPlayTests : ToolSceneTest
    {
        SaveManager save;
        StartScreen screen;

        [UnitySetUp]
        public IEnumerator FindScreen()
        {
            save = Object.FindAnyObjectByType<SaveManager>();
            screen = Object.FindAnyObjectByType<StartScreen>();
            Assert.IsNotNull(screen);
            yield break;
        }

        /// <summary>4일차 저장을 만들고 씬을 다시 엶(시작 화면이 뜬 채로)</summary>
        IEnumerator ReopenWithSave()
        {
            var data = save.Capture();
            data.day = 4;
            data.moa = 3450;
            SaveCodec.Write(save.FilePath, save.BackupPath, SaveCodec.ToJson(data));
            SceneManager.LoadScene("VillagePrototype");
            yield return null;
            yield return null;
            save = Object.FindAnyObjectByType<SaveManager>();
            screen = Object.FindAnyObjectByType<StartScreen>();
            player = Object.FindAnyObjectByType<PlayerController>().transform;
            yield return null;
        }

        int Day => Object.FindAnyObjectByType<DayCycle>().Clock.Day;

        [Test]
        public void NoSave_NoStartScreen()
        {
            Assert.IsFalse(screen.IsOpen);
            Assert.IsTrue(Object.FindAnyObjectByType<DayCycle>().enabled);
            Assert.IsTrue(player.GetComponent<PlayerLock>().IsFree);
        }

        [UnityTest]
        public IEnumerator WithSave_Shown_ClockAndPlayerStopped_ContinueLoads()
        {
            yield return ReopenWithSave();
            Assert.IsTrue(screen.IsOpen);
            StringAssert.Contains("4일차 · 3,450 모아", screen.listText.text);
            Assert.IsFalse(Object.FindAnyObjectByType<DayCycle>().enabled, "시계 멈춤");
            Assert.IsFalse(player.GetComponent<PlayerLock>().IsFree, "플레이어 멈춤");
            Assert.IsFalse(save.Loaded);

            yield return Tap(keyboard.fKey);
            Assert.IsFalse(screen.IsOpen);
            Assert.IsTrue(save.Loaded);
            Assert.AreEqual(4, Day);
            Assert.IsTrue(Object.FindAnyObjectByType<DayCycle>().enabled);
            yield return null;
            Assert.IsTrue(player.GetComponent<PlayerLock>().IsFree);
        }

        [UnityTest]
        public IEnumerator NewGame_AsksAgain_NoAndEscGoBack()
        {
            yield return ReopenWithSave();
            yield return Tap(keyboard.sKey);
            yield return Tap(keyboard.fKey);
            Assert.IsTrue(screen.Menu.Confirming);
            StringAssert.Contains("정말 새로 시작", screen.promptText.text);

            yield return Tap(keyboard.fKey);                       // 아니요
            Assert.IsFalse(screen.Menu.Confirming);
            yield return Tap(keyboard.fKey);
            yield return Tap(keyboard.escapeKey);                  // Esc
            Assert.IsFalse(screen.Menu.Confirming);
            Assert.IsTrue(screen.IsOpen);
            Assert.IsTrue(File.Exists(save.FilePath), "아직 지우지 않음");
        }

        [UnityTest]
        public IEnumerator NewGame_Confirmed_DeletesSave_FreshVillage()
        {
            yield return ReopenWithSave();
            yield return Tap(keyboard.sKey);
            yield return Tap(keyboard.fKey);
            yield return Tap(keyboard.sKey);
            yield return Tap(keyboard.fKey);                       // 예, 새로 시작
            Assert.IsFalse(screen.IsOpen);
            Assert.IsFalse(save.Loaded);
            Assert.IsFalse(File.Exists(save.FilePath), "저장 지움");
            Assert.IsFalse(File.Exists(save.BackupPath), "백업도");
            Assert.AreEqual(1, Day, "처음 마을");
            Assert.AreEqual(3000, player.GetComponent<PlayerWallet>().Wallet.Moa);
        }
    }
}
