using System.Collections.Generic;
using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // 2-3 HUD 글자 · 인벤토리 창 칸 이동 · 맞바꾸기
    public class HudTests
    {
        [Test]
        public void DayAndTime_Format()
        {
            Assert.AreEqual("1일차  06:00", HudText.DayAndTime(1, 6, 0));
            Assert.AreEqual("12일차  23:05", HudText.DayAndTime(12, 23, 5));
        }

        [Test]
        public void Moa_HasThousandsSeparator()
        {
            Assert.AreEqual("3,000 모아", HudText.Moa(3000));
            Assert.AreEqual("0 모아", HudText.Moa(0));
        }

        [Test]
        public void HeldTool_EmptyHandIsBlank()
        {
            Assert.AreEqual("도끼  49/50", HudText.HeldTool("도끼", 49, 50));
            Assert.AreEqual("", HudText.HeldTool(null, 0, 0));
        }

        [TestCase("[상점] 모아가 모자라요", "모아가 모자라요")]
        [TestCase("머리말 없음", "머리말 없음")]
        [TestCase("[깨짐", "[깨짐")]
        [TestCase("", "")]
        public void Notice_DropsConsoleTag(string message, string expected)
        {
            Assert.AreEqual(expected, HudText.Notice(message));
        }

        [TestCase("사과", 3, 0, "사과 ×3")]
        [TestCase("사과", 1, 0, "사과")]
        [TestCase("도끼", 1, 49, "도끼 (49)")]
        [TestCase("", 0, 0, "")]
        public void Slot_Text(string name, int count, int durability, string expected)
        {
            Assert.AreEqual(expected, HudText.Slot(name, count, durability));
        }

        // 20칸 = 5열 × 4줄
        [TestCase(0, 1, 0, 1)]
        [TestCase(4, 1, 0, 4)]      // 오른쪽 끝에서 멈춤
        [TestCase(0, -1, 0, 0)]
        [TestCase(0, 0, 1, 5)]
        [TestCase(17, 0, 1, 17)]    // 아래 끝에서 멈춤
        [TestCase(7, 0, -1, 2)]
        public void Cursor_MovesWithinGrid(int from, int dx, int dy, int expected)
        {
            Assert.AreEqual(expected, InventoryCursor.Move(from, dx, dy, 5, 20));
        }

        [Test]
        public void Swap_ExchangesSlots_AndNotifiesBoth()
        {
            var inv = new Inventory(20);
            inv.TryAddWithDurability("Axe", 50);
            inv.TryAdd("Apple", 10, 3);
            var changed = new List<int>();
            inv.SlotChanged += changed.Add;

            inv.Swap(0, 1);
            Assert.AreEqual("Apple", inv.Slots[0].itemId);
            Assert.AreEqual(3, inv.Slots[0].count);
            Assert.AreEqual("Axe", inv.Slots[1].itemId);
            Assert.AreEqual(50, inv.Slots[1].durability, "내구도도 함께 옮김");
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, changed);

            changed.Clear();
            inv.Swap(5, 5);
            Assert.IsEmpty(changed, "같은 칸은 아무 일 없음");
        }

        [Test]
        public void Swap_WithEmptySlot_MovesItem()
        {
            var inv = new Inventory(20);
            inv.TryAdd("Flower", 10, 2);
            inv.Swap(0, 7);
            Assert.IsTrue(inv.Slots[0].IsEmpty);
            Assert.AreEqual("Flower", inv.Slots[7].itemId);
        }
    }
}
