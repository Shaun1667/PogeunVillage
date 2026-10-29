using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace PogeunVillage.Tests
{
    // BALANCE.md 8번 검산 케이스 (묘목 6일 · 물 준 날 +1 · 꽃 번식 1칸 · 물 안 준 꽃 10% 시듦)
    public class GardenTests
    {
        /// <summary>정해 둔 값을 차례로 돌려주는 난수. 다 쓰면 NextDouble = 0.99(안 시듦), Next = 0</summary>
        class ScriptedRandom : IRandomSource
        {
            readonly Queue<double> doubles;
            readonly Queue<int> ints;
            public ScriptedRandom(IEnumerable<double> doubles = null, IEnumerable<int> ints = null)
            {
                this.doubles = new Queue<double>(doubles ?? Enumerable.Empty<double>());
                this.ints = new Queue<int>(ints ?? Enumerable.Empty<int>());
            }
            public double NextDouble() => doubles.Count > 0 ? doubles.Dequeue() : 0.99;
            public int Next(int maxExclusive) => ints.Count > 0 ? ints.Dequeue() % maxExclusive : 0;
        }

        const int GrowDays = 6, WaterBonus = 1;
        const double Wilt = 0.1;

        GroundGrid grid;

        [SetUp]
        public void EmptyField()
        {
            // 칸 (-5,-5) ~ (4,4) 10×10 빈 땅
            grid = new GroundGrid(1f, new Vector2Int(-5, -5), 10, 10, new bool[100]);
        }

        Garden NewGarden(IRandomSource random = null) => new(grid, random ?? new ScriptedRandom(), GrowDays, WaterBonus, Wilt);

        IEnumerable<Vector2Int> Cells(GroundObjectKind kind)
        {
            for (int y = -5; y < 5; y++)
            for (int x = -5; x < 5; x++)
                if (grid.Get(new Vector2Int(x, y)) == kind)
                    yield return new Vector2Int(x, y);
        }

        // 결과를 그리드에 적용(어댑터가 하는 일)
        GardenMorning Morning(Garden garden)
        {
            var m = garden.Morning(Cells(GroundObjectKind.Flower).ToList(), Cells(GroundObjectKind.Sapling).ToList());
            foreach (var c in m.grownSaplings) grid.Remove(c);
            foreach (var c in m.removedFlowers) grid.Remove(c);
            foreach (var (_, to) in m.spreadFlowers)
            {
                grid.Remove(to);
                grid.TryPlace(to, GroundObjectKind.Flower);
            }
            return m;
        }

        int MorningsToGrow(Garden garden, Vector2Int cell, System.Func<int, bool> waterOnDay)
        {
            for (int day = 1; day <= 10; day++)
            {
                if (waterOnDay(day)) garden.Water(cell);
                if (Morning(garden).grownSaplings.Contains(cell)) return day;
            }
            return -1;
        }

        [Test]
        public void Sapling_NoWater_GrowsOnSixthMorning()  // 8-1
        {
            var cell = Vector2Int.zero;
            grid.TryPlace(cell, GroundObjectKind.Sapling);
            Assert.AreEqual(6, MorningsToGrow(NewGarden(), cell, _ => false));
        }

        [Test]
        public void Sapling_WaterDaily_GrowsInThree()  // 8-2
        {
            var cell = Vector2Int.zero;
            grid.TryPlace(cell, GroundObjectKind.Sapling);
            Assert.AreEqual(3, MorningsToGrow(NewGarden(), cell, _ => true));
        }

        [Test]
        public void Sapling_WaterFirstTwoDays_GrowsInFour()  // 8-3: 2 + 2 + 1 + 1
        {
            var cell = Vector2Int.zero;
            grid.TryPlace(cell, GroundObjectKind.Sapling);
            Assert.AreEqual(4, MorningsToGrow(NewGarden(), cell, day => day <= 2));
        }

        [Test]
        public void Sapling_WaterThreeTimesADay_CountsOnce()  // 8-4
        {
            var cell = Vector2Int.zero;
            grid.TryPlace(cell, GroundObjectKind.Sapling);
            var garden = NewGarden();
            garden.Water(cell); garden.Water(cell); garden.Water(cell);
            Morning(garden);
            Assert.AreEqual(2, garden.Growth(cell));
        }

        [Test]
        public void Sapling_StageFollowsGrowth()
        {
            var cell = Vector2Int.zero;
            grid.TryPlace(cell, GroundObjectKind.Sapling);
            var garden = NewGarden();
            var stages = new List<int> { garden.SaplingStage(cell) };
            for (int i = 0; i < 5; i++) { Morning(garden); stages.Add(garden.SaplingStage(cell)); }
            CollectionAssert.AreEqual(new[] { 0, 0, 1, 1, 2, 2 }, stages);
        }

        [Test]
        public void WateredFlower_SpreadsToOneFreeCrossCell()
        {
            var cell = Vector2Int.zero;
            grid.TryPlace(cell, GroundObjectKind.Flower);
            var garden = NewGarden(new ScriptedRandom(ints: new[] { 1 }));  // 빈 칸 [위, 오른쪽, 아래, 왼쪽] 중 1번
            garden.Water(cell);
            var m = Morning(garden);
            Assert.AreEqual(1, m.spreadFlowers.Count);
            Assert.AreEqual(new Vector2Int(1, 0), m.spreadFlowers[0].to);
            Assert.AreEqual(GroundObjectKind.Flower, grid.Get(new Vector2Int(1, 0)));
        }

        [Test]
        public void SurroundedFlower_DoesNotSpread()  // 8-5
        {
            var cell = Vector2Int.zero;
            grid.TryPlace(cell, GroundObjectKind.Flower);
            foreach (var n in new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left })
                grid.TryPlace(n, GroundObjectKind.Hole);
            var garden = NewGarden();
            garden.Water(cell);
            Assert.AreEqual(0, Morning(garden).spreadFlowers.Count);
        }

        [Test]
        public void UnwateredFlower_WiltsWithChance()  // 8-6
        {
            var a = new Vector2Int(0, 0);
            var b = new Vector2Int(2, 0);
            grid.TryPlace(a, GroundObjectKind.Flower);
            grid.TryPlace(b, GroundObjectKind.Flower);
            // 정렬 순서 a, b — a는 0.05(< 10% → 시듦), b는 0.5(그대로)
            var garden = NewGarden(new ScriptedRandom(doubles: new[] { 0.05, 0.5 }));
            var m = Morning(garden);
            CollectionAssert.AreEqual(new[] { a }, m.wiltedFlowers);
            Assert.IsTrue(garden.IsWilted(a));
            Assert.IsFalse(garden.IsWilted(b));
        }

        [Test]
        public void WiltedFlower_NotWatered_Disappears()  // 8-7
        {
            var cell = Vector2Int.zero;
            grid.TryPlace(cell, GroundObjectKind.Flower);
            var garden = NewGarden(new ScriptedRandom(doubles: new[] { 0.0 }));
            Morning(garden);  // 시듦
            var m = Morning(garden);
            CollectionAssert.AreEqual(new[] { cell }, m.removedFlowers);
            Assert.AreEqual(GroundObjectKind.None, grid.Get(cell));
        }

        [Test]
        public void WiltedFlower_Watered_RevivesWithoutSpreading()  // 8-8 (결정 2026-10-27: 살아나기만)
        {
            var cell = Vector2Int.zero;
            grid.TryPlace(cell, GroundObjectKind.Flower);
            var garden = NewGarden(new ScriptedRandom(doubles: new[] { 0.0 }));
            Morning(garden);  // 시듦
            garden.Water(cell);
            var m = Morning(garden);
            CollectionAssert.AreEqual(new[] { cell }, m.revivedFlowers);
            Assert.IsEmpty(m.spreadFlowers, "살아난 날은 번식 안 함");
            Assert.IsFalse(garden.IsWilted(cell));
            Assert.AreEqual(GroundObjectKind.Flower, grid.Get(cell));
        }

        [Test]
        public void NewlySpreadFlower_DoesNotWiltSameMorning()
        {
            var cell = Vector2Int.zero;
            grid.TryPlace(cell, GroundObjectKind.Flower);
            var garden = NewGarden(new ScriptedRandom(doubles: new[] { 0.0, 0.0, 0.0 }));
            garden.Water(cell);
            var m = Morning(garden);
            Assert.AreEqual(1, m.spreadFlowers.Count);
            Assert.IsEmpty(m.wiltedFlowers, "물 받은 꽃도, 새로 난 꽃도 이번 아침엔 안 시듦");
        }

        [Test]
        public void SameSeed_SameResults()  // 8-9
        {
            List<Vector2Int> Run()
            {
                EmptyField();
                for (int x = -4; x <= 4; x += 2)
                    grid.TryPlace(new Vector2Int(x, 0), GroundObjectKind.Flower);
                var garden = NewGarden(new SeededRandom(1234));
                var log = new List<Vector2Int>();
                for (int day = 0; day < 5; day++)
                {
                    garden.Water(new Vector2Int(-4, 0));
                    var m = Morning(garden);
                    log.AddRange(m.wiltedFlowers);
                    log.AddRange(m.spreadFlowers.Select(s => s.to));
                }
                return log;
            }
            CollectionAssert.AreEqual(Run(), Run());
        }

        [Test]
        public void Water_OnlyOnPlants()
        {
            var garden = NewGarden();
            Assert.IsFalse(garden.Water(Vector2Int.zero), "빈 땅");
            grid.TryPlace(Vector2Int.zero, GroundObjectKind.Hole);
            Assert.IsFalse(garden.Water(Vector2Int.zero), "구멍");
            grid.Remove(Vector2Int.zero);
            grid.TryPlace(Vector2Int.zero, GroundObjectKind.Flower);
            Assert.IsTrue(garden.Water(Vector2Int.zero));
            Assert.IsTrue(garden.IsWatered(Vector2Int.zero));
        }
    }
}
