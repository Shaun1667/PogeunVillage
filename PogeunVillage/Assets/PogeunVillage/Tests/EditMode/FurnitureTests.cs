using NUnit.Framework;
using UnityEngine;

namespace PogeunVillage.Tests
{
    // 3-4a 가구 놓기 규칙 (BALANCE 16번)
    public class FurnitureTests
    {
        [Test]
        public void Rotation_CyclesFourDirections()
        {
            Assert.AreEqual(1, FurnitureRules.NextRotation(0));
            Assert.AreEqual(3, FurnitureRules.NextRotation(2));
            Assert.AreEqual(0, FurnitureRules.NextRotation(3), "네 번 돌리면 처음으로");
            Assert.AreEqual(0f, FurnitureRules.Yaw(0));
            Assert.AreEqual(270f, FurnitureRules.Yaw(3));
            Assert.AreEqual(90f, FurnitureRules.Yaw(5));
            Assert.AreEqual(270f, FurnitureRules.Yaw(-1));
        }

        [Test]
        public void CanPlace_GrassOnly_OnePerCell()
        {
            var blocked = new bool[9];
            blocked[4] = true;  // 가운데(1,1) = 길
            var grid = new GroundGrid(1f, Vector2Int.zero, 3, 3, blocked);
            Assert.IsTrue(FurnitureRules.CanPlace(grid, new Vector2Int(0, 0)));
            Assert.IsFalse(FurnitureRules.CanPlace(grid, new Vector2Int(1, 1)), "길 · 광장 · 물 · 건물");
            Assert.IsFalse(FurnitureRules.CanPlace(grid, new Vector2Int(5, 5)), "맵 밖");
            Assert.IsTrue(grid.TryPlace(new Vector2Int(0, 0), GroundObjectKind.Furniture));
            Assert.IsFalse(FurnitureRules.CanPlace(grid, new Vector2Int(0, 0)), "한 칸에 하나");
            grid.Remove(new Vector2Int(0, 0));
            Assert.IsTrue(FurnitureRules.CanPlace(grid, new Vector2Int(0, 0)), "들면 다시 빈 칸");
        }

        [Test]
        public void FurnitureCell_VillagersWalkAround()
        {
            var walkable = new bool[9];
            for (int i = 0; i < 9; i++) walkable[i] = true;
            var grid = new GroundGrid(1f, Vector2Int.zero, 3, 3, new bool[9]);
            var walk = new WalkMap(Vector2Int.zero, 3, 3, walkable, new bool[9]);
            grid.TryPlace(new Vector2Int(2, 2), GroundObjectKind.Furniture);
            Assert.IsFalse(walk.IsWalkable(new Vector2Int(2, 2), grid));
        }

        [Test]
        public void StackOfFive_SixthGoesToNextSlot()
        {
            var inv = new Inventory(3);
            Assert.IsTrue(inv.TryAdd("Fence", 5, 6));
            Assert.AreEqual(5, inv.Slots[0].count);
            Assert.AreEqual(1, inv.Slots[1].count);
        }
    }
}
