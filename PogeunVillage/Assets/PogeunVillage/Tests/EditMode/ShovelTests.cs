using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PogeunVillage.Tests
{
    // 1-8 삽 · 심기 규칙 (BALANCE.md 5번 · 11번)
    public class ShovelTests
    {
        [TestCase(false, false, GroundObjectKind.None, ShovelAction.DigHole)]
        [TestCase(false, false, GroundObjectKind.Hole, ShovelAction.FillHole)]
        [TestCase(false, false, GroundObjectKind.Flower, ShovelAction.LiftFlower)]
        [TestCase(false, false, GroundObjectKind.Sapling, ShovelAction.None)]
        [TestCase(false, true, GroundObjectKind.None, ShovelAction.None)]
        [TestCase(true, true, GroundObjectKind.None, ShovelAction.RemoveStump)]
        [TestCase(true, false, GroundObjectKind.Flower, ShovelAction.RemoveStump)]
        public void Decide(bool stump, bool blocked, GroundObjectKind content, ShovelAction expected)
        {
            Assert.AreEqual(expected, ShovelRules.Decide(stump, blocked, content));
        }

        static readonly HashSet<string> Plantables = new() { "Sapling", "FlowerClump" };

        [Test]
        public void ChoosePlantSlot_FirstPlantableInSlotOrder()
        {
            var inv = new Inventory(20);
            inv.TryAddWithDurability("Shovel", 50);
            inv.TryAdd("Apple", 10);
            inv.TryAdd("FlowerClump", 10);
            inv.TryAdd("Sapling", 10);
            Assert.AreEqual(2, ShovelRules.ChoosePlantSlot(inv.Slots, Plantables), "앞쪽 칸의 꽃 포기");
        }

        [Test]
        public void ChoosePlantSlot_NothingPlantable_MinusOne()
        {
            var inv = new Inventory(20);
            inv.TryAdd("Flower", 10);  // 꺾은 꽃은 못 심음
            Assert.AreEqual(-1, ShovelRules.ChoosePlantSlot(inv.Slots, Plantables));
        }

        [Test]
        public void RemoveOne_DecrementsThenEmpties()
        {
            var inv = new Inventory(20);
            inv.TryAdd("Sapling", 10, 2);
            inv.RemoveOne(0);
            Assert.AreEqual(1, inv.Count("Sapling"));
            inv.RemoveOne(0);
            Assert.IsTrue(inv.Slots[0].IsEmpty);
            Assert.Throws<InvalidOperationException>(() => inv.RemoveOne(0));
        }

        [Test]
        public void Unblock_MakesStumpCellPlantable()
        {
            var blocked = new bool[9];
            blocked[4] = true;  // 가운데 (0,0)
            var grid = new GroundGrid(1f, new Vector2Int(-1, -1), 3, 3, blocked);
            var changed = new List<Vector2Int>();
            grid.CellChanged += changed.Add;

            Assert.IsTrue(grid.Unblock(Vector2Int.zero));
            Assert.IsTrue(grid.CanPlace(Vector2Int.zero));
            Assert.IsFalse(grid.Unblock(Vector2Int.zero), "이미 빈 땅");
            Assert.IsFalse(grid.Unblock(new Vector2Int(5, 5)), "맵 밖은 못 풂");
            CollectionAssert.AreEqual(new[] { Vector2Int.zero }, changed);
        }
    }
}
