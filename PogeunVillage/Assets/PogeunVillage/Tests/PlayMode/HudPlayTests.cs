using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>2-3 HUD 확인 (시계 · 모아 · 든 도구 · 화면 알림 · Tab 인벤토리 창 · 맞바꾸기 · 창이 열리면 멈춤 · 한글 글꼴).</summary>
    public class HudPlayTests : ToolSceneTest
    {
        HudView hud;
        InventoryWindow window;
        DayCycle dayCycle;
        Wallet Wallet => player.GetComponent<PlayerWallet>().Wallet;

        [UnitySetUp]
        public IEnumerator FindHud()
        {
            hud = Object.FindAnyObjectByType<HudView>();
            window = Object.FindAnyObjectByType<InventoryWindow>();
            dayCycle = Object.FindAnyObjectByType<DayCycle>();
            Assert.IsNotNull(hud);
            Assert.IsNotNull(window);
            yield break;
        }

        [UnityTest]
        public IEnumerator Clock_ShowsDayAndTime_AndFollowsSleep()
        {
            Assert.AreEqual(HudText.DayAndTime(1, dayCycle.Clock.HourOfDay, dayCycle.Clock.Minute), hud.clockText.text);
            yield return Tap(keyboard.f8Key);
            Assert.AreEqual("2일차  06:00", hud.clockText.text);
        }

        [UnityTest]
        public IEnumerator Moa_ShowsAndUpdates()
        {
            Assert.AreEqual("3,000 모아", hud.moaText.text);
            Wallet.TrySpend(500);
            yield return null;
            Assert.AreEqual("2,500 모아", hud.moaText.text);
        }

        [UnityTest]
        public IEnumerator HeldTool_ShowsNameAndDurability()
        {
            Assert.AreEqual("", hud.toolText.text, "빈손");
            yield return Hold(Axe);
            Assert.AreEqual("도끼  50/50", hud.toolText.text);

            var tree = Object.FindObjectsByType<ChoppableTreeBehaviour>().Where(t => !t.GetComponent<FruitTreeBehaviour>())
                .OrderBy(t => Vector3.Distance(t.transform.position, Plaza)).First();
            Teleport(tree.transform.position + Vector3.back * 0.9f, Vector3.forward);
            yield return Frames(3);
            yield return Swing();
            Assert.AreEqual("도끼  49/50", hud.toolText.text, "맞으면 내구도가 바로 바뀜");
        }

        [UnityTest]
        public IEnumerator Notice_ShowsOnScreen_ThenHides()
        {
            Assert.IsFalse(hud.NoticeVisible);
            GameNotice.Post("[상점] 지금은 문을 닫았어요");
            yield return null;
            Assert.IsTrue(hud.NoticeVisible);
            Assert.AreEqual("지금은 문을 닫았어요", hud.noticeText.text, "머리말은 뗌");
            yield return WaitSeconds(settings.hudNoticeSeconds + 0.3f);
            Assert.IsFalse(hud.NoticeVisible, "잠시 뒤 사라짐");
        }

        [UnityTest]
        public IEnumerator ClosedShop_ShowsNotice()
        {
            var stand = Object.FindObjectsByType<ShopStand>().First(s => s.item.id == "Axe");
            Vector3 front = Object.FindAnyObjectByType<Shop>().transform.forward;
            Teleport(stand.transform.position + front * 0.95f, -front);
            yield return Frames(3);
            yield return Tap(keyboard.fKey);  // 6시 = 닫힘
            Assert.IsTrue(hud.NoticeVisible);
            StringAssert.Contains("닫았어요", hud.noticeText.text);
        }

        [UnityTest]
        public IEnumerator Tab_OpensWindow_ShowsSlots_PausesPlayer()
        {
            Assert.IsTrue(playerInventory.TryAddTool(tools.tools[Axe]));
            Assert.IsTrue(playerInventory.TryAdd(Item("Apple"), 3));
            yield return Tap(keyboard.tabKey);
            Assert.IsTrue(window.IsOpen);
            Assert.AreEqual("도끼 (50)", window.slotTexts[0].text);
            Assert.AreEqual("사과 ×3", window.slotTexts[1].text);
            Assert.AreEqual("", window.slotTexts[2].text);

            Vector2 before = Flat(player.position);
            Press(keyboard.wKey);
            yield return WaitSeconds(0.4f);
            Release(keyboard.wKey);
            Assert.Less(Vector2.Distance(before, Flat(player.position)), 0.01f, "창이 열린 동안 안 움직임");

            yield return Tap(keyboard.tabKey);
            Assert.IsFalse(window.IsOpen);
            Press(keyboard.wKey);
            yield return WaitSeconds(0.3f);
            Release(keyboard.wKey);
            Assert.Greater(Vector2.Distance(before, Flat(player.position)), 0.3f, "닫으면 다시 움직임");
        }

        [UnityTest]
        public IEnumerator Window_PickAndSwapSlots()
        {
            Assert.IsTrue(playerInventory.TryAddTool(tools.tools[Axe]));
            Assert.IsTrue(playerInventory.TryAdd(Item("Apple"), 3));
            yield return Tap(keyboard.tabKey);

            yield return Tap(keyboard.fKey);          // 0번(도끼) 집기
            Assert.AreEqual(0, window.Picked);
            yield return Tap(keyboard.rightArrowKey);
            yield return Tap(keyboard.downArrowKey);  // 6번
            Assert.AreEqual(6, window.Cursor);
            yield return Tap(keyboard.fKey);          // 맞바꾸기
            Assert.AreEqual(-1, window.Picked);
            Assert.IsTrue(Inv.Slots[0].IsEmpty);
            Assert.AreEqual("Axe", Inv.Slots[6].itemId);
            Assert.AreEqual("도끼 (50)", window.slotTexts[6].text);
            Assert.AreEqual("", window.slotTexts[0].text);

            // 사과를 맨 앞으로 → 판매함이 사과부터 팖
            yield return Tap(keyboard.upArrowKey);    // 1번(사과)
            yield return Tap(keyboard.fKey);
            yield return Tap(keyboard.leftArrowKey);  // 0번
            yield return Tap(keyboard.fKey);
            Assert.AreEqual("Apple", Inv.Slots[0].itemId);
        }

        [UnityTest]
        public IEnumerator GamepadY_TogglesWindow()
        {
            yield return Tap(gamepad.buttonNorth);
            Assert.IsTrue(window.IsOpen);
            yield return Tap(gamepad.buttonNorth);
            Assert.IsFalse(window.IsOpen);
        }

        [UnityTest]
        public IEnumerator KoreanFont_HasGlyphs()
        {
            var font = hud.clockText.font;
            Assert.IsNotNull(font, "글꼴 연결");
            Assert.IsTrue(font.HasCharacters("일차모아인벤토리도끼사과", out uint[] missing, false, true),
                $"한글 글자 없음: {(missing == null ? "" : string.Join(",", missing.Select(u => (char)u)))}");
            yield break;
        }
    }
}
