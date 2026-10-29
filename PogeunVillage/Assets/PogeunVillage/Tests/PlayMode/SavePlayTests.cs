using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// 6-1a 확인 (테스트는 임시 폴더 · 처음엔 저장 없음 / 자면 자동 저장 + 알림 / 다시 켜면 그 날짜 아침 · 모아 · 인벤토리 · 도감 · 평가 기록 /
    /// 없어진 물건은 건너뛰고 알림).
    /// </summary>
    public class SavePlayTests : ToolSceneTest
    {
        SaveManager save;
        HudView hud;

        [UnitySetUp]
        public IEnumerator FindSave()
        {
            save = Object.FindAnyObjectByType<SaveManager>();
            hud = Object.FindAnyObjectByType<HudView>();
            Assert.IsNotNull(save);
            yield break;
        }

        string Shown => hud.noticeText.text + "|" + string.Join("|", hud.PendingNotices);

        /// <summary>같은 저장 폴더로 씬을 다시 엶(= 게임을 끄고 다시 켬)</summary>
        IEnumerator Reopen()
        {
            SceneManager.LoadScene("VillagePrototype");
            yield return null;
            yield return null;
            save = Object.FindAnyObjectByType<SaveManager>();
            hud = Object.FindAnyObjectByType<HudView>();
            player = Object.FindAnyObjectByType<PlayerController>().transform;
            playerInventory = player.GetComponent<PlayerInventory>();
            yield return null;
        }

        [Test]
        public void Tests_UseTempFolder_NoSaveAtStart()
        {
            Assert.AreEqual(TestSaveFolder, save.Folder, "직접 플레이한 저장을 건드리지 않음");
            Assert.IsFalse(File.Exists(save.FilePath));
            Assert.IsFalse(save.Loaded);
        }

        [UnityTest]
        public IEnumerator Sleep_AutoSaves_ReopenRestoresMorning()
        {
            var wallet = player.GetComponent<PlayerWallet>().Wallet;
            wallet.Earn(450);
            Assert.IsTrue(playerInventory.TryAdd(Item("Apple"), 3));
            Assert.GreaterOrEqual(Inv.TryAddWithDurability("Axe", 50), 0);
            Assert.IsTrue(Inv.UseDurability(ToolBelt.FindSlot(Inv.Slots, "Axe")) == false);
            var fishDoc = Object.FindAnyObjectByType<FishEncyclopedia>();
            fishDoc.Record(fishDoc.rules.fish[0]);

            yield return Tap(keyboard.f8Key);                    // 자고 일어남 → 2일차 아침 저장
            yield return null;
            Assert.IsTrue(File.Exists(save.FilePath), "자면 저장");
            StringAssert.Contains("2일차 아침을 저장했어요", Shown);

            wallet.Earn(9999);                                     // 저장 뒤 한 일은 남지 않음
            yield return Reopen();
            Assert.IsTrue(save.Loaded);
            var clock = Object.FindAnyObjectByType<DayCycle>().Clock;
            Assert.AreEqual(2, clock.Day);
            Assert.AreEqual(0, clock.MinutesSinceDayStart, "아침 6시");
            Assert.AreEqual(3000 + 450, player.GetComponent<PlayerWallet>().Wallet.Moa);
            Assert.AreEqual(3, playerInventory.Inventory.Count("Apple"));
            int axe = ToolBelt.FindSlot(playerInventory.Inventory.Slots, "Axe");
            Assert.AreEqual(49, playerInventory.Inventory.Slots[axe].durability, "내구도도");
            var doc = Object.FindAnyObjectByType<FishEncyclopedia>();
            Assert.AreEqual(1, doc.Log.Species, "도감");
            StringAssert.Contains("2일차 아침부터 이어서 해요", Shown);
        }

        [UnityTest]
        public IEnumerator MissingItem_Skipped_Notice()
        {
            var data = save.Capture();
            data.day = 4;
            data.slots[0] = new SlotData { id = "Apple", count = 2 };
            data.slots[1] = new SlotData { id = "RemovedThing", count = 1 };
            SaveCodec.Write(save.FilePath, save.BackupPath, SaveCodec.ToJson(data));
            yield return Reopen();
            Assert.AreEqual(4, Object.FindAnyObjectByType<DayCycle>().Clock.Day);
            Assert.AreEqual(2, playerInventory.Inventory.Count("Apple"));
            Assert.IsTrue(playerInventory.Inventory.Slots[1].IsEmpty, "없어진 물건 칸은 비움");
            StringAssert.Contains("없어진 물건 1가지는 건너뛰었어요", Shown);
        }

        [Test]
        public void RatingRecord_Restored_NoStarNotice()
        {
            var rater = Object.FindAnyObjectByType<VillageRater>();
            var data = save.Capture();
            data.firstGoalReached = true;
            data.bestStars = 4;
            save.Load(data);
            Assert.IsTrue(rater.GoalReached);
            Assert.AreEqual(4, Object.FindAnyObjectByType<FurnitureCounter>().BestStars);
            StringAssert.DoesNotContain("마을 평가가", Shown, "불러올 때 별 바뀜 알림 없음");
        }
    }
}
