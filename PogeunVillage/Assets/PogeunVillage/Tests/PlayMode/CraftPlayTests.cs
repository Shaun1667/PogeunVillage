using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// 3-4b 확인 (광장 작업대 F → 제작 창 · 조작 멈춤 · 위아래 고르기 · F 만들기(창은 열린 채) · 목재 모자람 · 안 열린 제작법 ·
    /// 가득 참 · Tab / Esc 닫기 · 제작법 = 목재 가구 5가지).
    /// </summary>
    public class CraftPlayTests : ToolSceneTest
    {
        Workbench bench;
        CraftWindow window;
        InventoryWindow inventoryWindow;
        HudView hud;

        [UnitySetUp]
        public IEnumerator FindWorkbench()
        {
            bench = Object.FindAnyObjectByType<Workbench>();
            window = Object.FindAnyObjectByType<CraftWindow>();
            inventoryWindow = Object.FindAnyObjectByType<InventoryWindow>();
            hud = Object.FindAnyObjectByType<HudView>();
            Assert.IsNotNull(bench);
            Assert.IsNotNull(window);
            yield break;
        }

        /// <summary>작업대 앞(서쪽)에서 작업대를 보고 F</summary>
        IEnumerator OpenAtBench()
        {
            Teleport(bench.transform.position + bench.transform.forward * 1.2f, -bench.transform.forward);
            yield return Frames(3);
            Assert.AreSame(bench.GetComponent<Interactable>(), interactor.Target);
            yield return Tap(keyboard.fKey);
            Assert.IsTrue(window.IsOpen, "제작 창");
        }

        [UnityTest]
        public IEnumerator Bench_F_OpensWindow_CraftsFence_StaysOpen()
        {
            Assert.IsTrue(playerInventory.TryAdd(Item("Wood"), 5));
            yield return OpenAtBench();
            Assert.IsFalse(interactor.enabled, "창이 열린 동안 조작 멈춤");
            Assert.IsFalse(player.GetComponent<PlayerController>().enabled);
            Assert.AreEqual(0, Inv.Count("Fence"), "여는 F로 만들지 않음");
            StringAssert.Contains("나무 울타리 · 목재 2", window.listText.text);
            StringAssert.Contains("가진 목재 5개", window.woodText.text);

            yield return Tap(keyboard.fKey);
            Assert.AreEqual(1, Inv.Count("Fence"));
            Assert.AreEqual(3, Inv.Count("Wood"));
            Assert.IsTrue(window.IsOpen, "만들어도 창은 열린 채");
            StringAssert.Contains("가진 목재 3개", window.woodText.text);
            StringAssert.Contains("만들었어요", hud.noticeText.text);

            yield return Tap(keyboard.fKey);
            yield return Tap(keyboard.fKey);                       // 목재 1개 — 모자람
            Assert.AreEqual(2, Inv.Count("Fence"));
            Assert.AreEqual(1, Inv.Count("Wood"));
            StringAssert.Contains("목재가 모자라요", hud.noticeText.text);

            yield return Tap(keyboard.tabKey);
            Assert.IsFalse(window.IsOpen);
            yield return null;
            Assert.IsFalse(inventoryWindow.IsOpen, "닫은 Tab으로 인벤토리가 열리지 않음");
            Assert.IsTrue(interactor.enabled, "다시 조작");
        }

        [UnityTest]
        public IEnumerator Down_PicksCrate_LockedRecipe_Notice()
        {
            var rater = Object.FindAnyObjectByType<VillageRater>();
            Assert.AreEqual(1, rater.Current.stars, "시작 마을은 ★1");
            Assert.IsTrue(playerInventory.TryAdd(Item("Wood"), 20));
            yield return OpenAtBench();
            StringAssert.Contains("통나무 의자 · ★2에 열려요", window.listText.text);

            yield return Tap(keyboard.downArrowKey);
            Assert.AreEqual(1, window.Cursor);
            yield return Tap(keyboard.fKey);
            Assert.AreEqual(1, Inv.Count("Crate"));
            Assert.AreEqual(17, Inv.Count("Wood"));

            yield return Tap(keyboard.sKey);                        // 통나무 의자(★2)
            Assert.AreEqual(2, window.Cursor);
            yield return Tap(keyboard.fKey);
            Assert.AreEqual(0, Inv.Count("LogStool"));
            Assert.AreEqual(17, Inv.Count("Wood"), "안 열렸으면 목재 그대로");
            StringAssert.Contains("★2에 열려요", hud.noticeText.text);

            yield return Tap(keyboard.escapeKey);
            Assert.IsFalse(window.IsOpen, "Esc로도 닫힘");
        }

        [UnityTest]
        public IEnumerator FullInventory_CannotCraft_WoodStays()
        {
            Assert.IsTrue(playerInventory.TryAdd(Item("Wood"), 10));
            while (Inv.UsedSlots < Inv.Slots.Count)
                Assert.IsTrue(playerInventory.TryAdd(Item("Apple"), 10));
            yield return OpenAtBench();
            yield return Tap(keyboard.fKey);
            Assert.AreEqual(0, Inv.Count("Fence"));
            Assert.AreEqual(10, Inv.Count("Wood"));
            StringAssert.Contains("가득 찼어요", hud.noticeText.text);
            yield return Tap(keyboard.tabKey);
        }

        [Test]
        public void Recipes_AreFiveWoodFurniture_FromBalance()
        {
            CollectionAssert.AreEqual(new[] { "Fence", "Crate", "LogStool", "Bench", "RoundTable" }, bench.recipes.Select(r => r.id).ToArray());
            CollectionAssert.AreEqual(new[] { 2, 3, 4, 8, 6 }, bench.recipes.Select(r => r.woodCost).ToArray());
            CollectionAssert.AreEqual(new[] { 1, 1, 2, 2, 3 }, bench.recipes.Select(r => r.unlockStars).ToArray());
        }
    }
}
