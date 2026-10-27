using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // BALANCE.md 5번 검산 케이스(내구도 50) + 도구 고르기 규칙
    public class ToolTests
    {
        const int Durability = 50;
        static readonly string[] KeyOrder = { "Net", "FishingRod", "Axe", "Shovel", "WateringCan" };

        [Test]
        public void Tool_TakesOwnSlot_WithFullDurability()
        {
            var inv = new Inventory(20);
            inv.TryAdd("Apple", 10);
            Assert.AreEqual(1, inv.TryAddWithDurability("Axe", Durability));
            Assert.AreEqual(2, inv.TryAddWithDurability("Axe", Durability), "도구는 겹치지 않음");
            Assert.AreEqual(Durability, inv.Slots[1].durability);
            Assert.AreEqual(2, inv.Count("Axe"));
        }

        [Test]
        public void Tool_NoEmptySlot_ReturnsMinusOne()
        {
            var inv = new Inventory(2);
            inv.TryAdd("Apple", 10);
            inv.TryAdd("Peach", 10);
            Assert.AreEqual(-1, inv.TryAddWithDurability("Axe", Durability));
        }

        [Test]
        public void UseDurability_49Times_NotBroken_50th_Breaks()  // 5-6
        {
            var inv = new Inventory(20);
            int slot = inv.TryAddWithDurability("Axe", Durability);
            for (int i = 0; i < Durability - 1; i++)
                Assert.IsFalse(inv.UseDurability(slot));
            Assert.AreEqual(1, inv.Slots[slot].durability);
            Assert.IsTrue(inv.UseDurability(slot), "50번째에 부서짐");
            Assert.IsTrue(inv.Slots[slot].IsEmpty, "부서진 도구는 인벤토리에서 사라짐");
        }

        [Test]
        public void UseDurability_OnStackable_Throws()
        {
            var inv = new Inventory(20);
            inv.TryAdd("Apple", 10);
            Assert.Throws<InvalidOperationException>(() => inv.UseDurability(0));
            Assert.Throws<InvalidOperationException>(() => inv.UseDurability(1));
        }

        static Inventory WithTools(params string[] ids)
        {
            var inv = new Inventory(20);
            inv.TryAdd("Apple", 10);  // 0번 칸은 사과
            foreach (string id in ids)
                inv.TryAddWithDurability(id, Durability);
            return inv;
        }

        [Test]
        public void Select_KeyEquips_SameKeyUnequips()
        {
            var inv = WithTools("Shovel", "Axe");  // 1: 삽, 2: 도끼
            int equipped = ToolBelt.Select(inv.Slots, "Axe", -1);
            Assert.AreEqual(2, equipped);
            Assert.AreEqual(-1, ToolBelt.Select(inv.Slots, "Axe", equipped), "같은 키 다시 = 내려놓기");
            Assert.AreEqual(1, ToolBelt.Select(inv.Slots, "Shovel", equipped), "다른 키 = 바꾸기");
        }

        [Test]
        public void Select_MissingTool_KeepsCurrent()
        {
            var inv = WithTools("Axe");
            Assert.AreEqual(1, ToolBelt.Select(inv.Slots, "Net", 1));
            Assert.AreEqual(-1, ToolBelt.Select(inv.Slots, "Net", -1));
        }

        [Test]
        public void Cycle_GoesThroughEmptyHandAndToolsInKeyOrder()
        {
            var inv = WithTools("WateringCan", "Axe");  // 1: 물뿌리개, 2: 도끼 → 키 순서는 도끼(3) 다음 물뿌리개(5)
            var visited = new List<int>();
            int equipped = -1;
            for (int i = 0; i < 4; i++)
            {
                equipped = ToolBelt.Cycle(inv.Slots, KeyOrder, equipped, +1);
                visited.Add(equipped);
            }
            CollectionAssert.AreEqual(new[] { 2, 1, -1, 2 }, visited);
            Assert.AreEqual(1, ToolBelt.Cycle(inv.Slots, KeyOrder, -1, -1), "왼쪽 = 거꾸로");
        }

        [Test]
        public void Cycle_NoTools_StaysEmptyHanded()
        {
            var inv = WithTools();
            Assert.AreEqual(-1, ToolBelt.Cycle(inv.Slots, KeyOrder, -1, +1));
        }
    }
}
