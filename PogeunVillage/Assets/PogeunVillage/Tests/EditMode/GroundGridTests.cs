using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PogeunVillage.Tests
{
    // BALANCE.md 11번 검산 케이스 (칸 1m)
    public class GroundGridTests
    {
        // 칸 (-2,-2) ~ (2,2) 5×5, (0,1)만 막힘
        static GroundGrid Small()
        {
            var blocked = new bool[25];
            blocked[(1 + 2) * 5 + (0 + 2)] = true;
            return new GroundGrid(1f, new Vector2Int(-2, -2), 5, 5, blocked);
        }

        [Test]
        public void ToCell_FloorsIncludingNegative()  // 11-1
        {
            var g = Small();
            Assert.AreEqual(new Vector2Int(0, 0), g.ToCell(new Vector2(0.2f, 0.99f)));
            Assert.AreEqual(new Vector2Int(-1, -1), g.ToCell(new Vector2(-0.01f, -0.5f)));
            Assert.AreEqual(new Vector2Int(1, 0), g.ToCell(new Vector2(1f, 0f)), "경계는 위쪽 칸");
        }

        [Test]
        public void CellCenter_IsMiddleOfCell()
        {
            Assert.AreEqual(new Vector2(-0.5f, 1.5f), Small().CellCenter(new Vector2Int(-1, 1)));
        }

        [Test]
        public void FrontCell_IsCellAheadByReach()  // 11-2
        {
            var g = Small();
            Assert.AreEqual(new Vector2Int(0, 1), g.FrontCell(new Vector2(0.5f, 0.5f), Vector2.up, 0.9f));
            Assert.AreEqual(new Vector2Int(1, 0), g.FrontCell(new Vector2(0.5f, 0.5f), new Vector2(3f, 0f), 0.9f));
            Assert.AreEqual(new Vector2Int(0, 0), g.FrontCell(new Vector2(0.5f, 0.5f), Vector2.up, 0.3f), "가까우면 같은 칸");
        }

        [Test]
        public void Blocked_AndOutOfBounds_CannotPlace()  // 11-3
        {
            var g = Small();
            Assert.IsTrue(g.IsBlocked(new Vector2Int(0, 1)));
            Assert.IsTrue(g.IsBlocked(new Vector2Int(3, 0)), "범위 밖은 막힘");
            Assert.IsFalse(g.TryPlace(new Vector2Int(0, 1), GroundObjectKind.Flower));
            Assert.IsFalse(g.TryPlace(new Vector2Int(-3, 0), GroundObjectKind.Flower));
        }

        [Test]
        public void OneObjectPerCell()  // 11-4
        {
            var g = Small();
            var cell = new Vector2Int(1, 1);
            Assert.IsTrue(g.TryPlace(cell, GroundObjectKind.Flower));
            Assert.IsFalse(g.TryPlace(cell, GroundObjectKind.Hole), "이미 무언가 있음");
            Assert.AreEqual(GroundObjectKind.Flower, g.Get(cell));
        }

        [Test]
        public void Remove_FreesCell()  // 11-5
        {
            var g = Small();
            var cell = new Vector2Int(-1, 0);
            g.TryPlace(cell, GroundObjectKind.Flower);
            Assert.IsTrue(g.Remove(cell));
            Assert.AreEqual(GroundObjectKind.None, g.Get(cell));
            Assert.IsTrue(g.CanPlace(cell));
            Assert.IsFalse(g.Remove(cell), "빈 칸 치우기 = false");
        }

        [Test]
        public void CellChanged_OnlyOnRealChanges()
        {
            var g = Small();
            var changed = new List<Vector2Int>();
            g.CellChanged += changed.Add;
            g.TryPlace(new Vector2Int(1, 1), GroundObjectKind.Flower);
            g.TryPlace(new Vector2Int(1, 1), GroundObjectKind.Flower);  // 실패
            g.TryPlace(new Vector2Int(0, 1), GroundObjectKind.Flower);  // 막힘
            g.Remove(new Vector2Int(2, 2));                               // 빈 칸
            g.Remove(new Vector2Int(1, 1));
            CollectionAssert.AreEqual(new[] { new Vector2Int(1, 1), new Vector2Int(1, 1) }, changed);
        }

        [Test]
        public void BadInput_Throws()
        {
            Assert.Throws<ArgumentException>(() => new GroundGrid(1f, Vector2Int.zero, 2, 2, new bool[3]));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GroundGrid(0f, Vector2Int.zero, 2, 2, new bool[4]));
            Assert.Throws<ArgumentException>(() => Small().TryPlace(Vector2Int.zero, GroundObjectKind.None));
        }
    }
}
