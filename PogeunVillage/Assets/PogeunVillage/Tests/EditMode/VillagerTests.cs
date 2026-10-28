using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace PogeunVillage.Tests
{
    // 2-4 주민 · 집터 · 배회 목표 · 대사
    public class VillagerTests
    {
        class ScriptedRandom : IRandomSource
        {
            readonly Queue<double> doubles;
            readonly Queue<int> ints;
            public ScriptedRandom(IEnumerable<double> doubles = null, IEnumerable<int> ints = null)
            {
                this.doubles = new Queue<double>(doubles ?? Enumerable.Empty<double>());
                this.ints = new Queue<int>(ints ?? Enumerable.Empty<int>());
            }
            public double NextDouble() => doubles.Count > 0 ? doubles.Dequeue() : 0.5;
            public int Next(int maxExclusive) => ints.Count > 0 ? ints.Dequeue() % maxExclusive : 0;
        }

        static readonly List<string> Twenty = Enumerable.Range(1, 20).Select(i => $"V{i:00}").ToList();

        // ---------------------------------------------------------------- 시작 주민 · 집터

        [Test]
        public void PickStarting_SixDistinct_SameSeedSameResult()
        {
            var a = VillageResidents.PickStarting(Twenty, 6, new SystemRandomSource(2026));
            var b = VillageResidents.PickStarting(Twenty, 6, new SystemRandomSource(2026));
            Assert.AreEqual(6, a.Count);
            Assert.AreEqual(6, a.Distinct().Count(), "겹치지 않음");
            CollectionAssert.IsSubsetOf(a, Twenty);
            CollectionAssert.AreEqual(a, b, "같은 시드 = 같은 6명");
        }

        [Test]
        public void PickStarting_DifferentSeeds_GiveDifferentVillages()
        {
            var seen = new HashSet<string>();
            for (int seed = 0; seed < 10; seed++)
                seen.Add(string.Join(",", VillageResidents.PickStarting(Twenty, 6, new SystemRandomSource(seed)).OrderBy(x => x)));
            Assert.Greater(seen.Count, 1);
        }

        [Test]
        public void PickStarting_MoreThanAll_ReturnsAll()
        {
            Assert.AreEqual(3, VillageResidents.PickStarting(new[] { "A", "B", "C" }, 6, new ScriptedRandom()).Count);
            Assert.AreEqual(0, VillageResidents.PickStarting(Twenty, 0, new ScriptedRandom()).Count);
        }

        [Test]
        public void CreateStarting_SixInDistinctPlots_Deterministic()
        {
            var a = VillageResidents.CreateStarting(12, Twenty, 6, new SystemRandomSource(7));
            var b = VillageResidents.CreateStarting(12, Twenty, 6, new SystemRandomSource(7));
            Assert.AreEqual(6, a.Count);
            Assert.AreEqual(6, a.Plots.Where(p => p != null).Distinct().Count());
            CollectionAssert.AreEqual(a.Plots, b.Plots, "같은 시드 = 같은 집터");
        }

        [Test]
        public void MoveIn_PicksAmongEmptyPlotsOnly()
        {
            var r = new VillageResidents(3);
            Assert.AreEqual(1, r.MoveIn("A", new ScriptedRandom(ints: new[] { 1 })));  // 빈 곳 0,1,2 중 두 번째
            Assert.AreEqual(2, r.MoveIn("B", new ScriptedRandom(ints: new[] { 1 })));  // 빈 곳 0,2 중 두 번째
            Assert.AreEqual(0, r.MoveIn("C", new ScriptedRandom()));
            Assert.AreEqual(-1, r.MoveIn("D", new ScriptedRandom()), "가득 참");
            Assert.AreEqual(3, r.Count);
        }

        [Test]
        public void MoveIn_AlreadyResident_Refused()
        {
            var r = new VillageResidents(3);
            r.MoveIn("A", new ScriptedRandom());
            Assert.AreEqual(-1, r.MoveIn("A", new ScriptedRandom()));
            Assert.AreEqual(1, r.Count);
        }

        [Test]
        public void MoveOut_FreesPlot_NotifiesOnlyThatPlot()
        {
            var r = new VillageResidents(4);
            r.MoveIn("A", new ScriptedRandom(ints: new[] { 2 }));
            var changed = new List<int>();
            r.PlotChanged += changed.Add;
            Assert.IsTrue(r.MoveOut("A"));
            Assert.IsFalse(r.MoveOut("A"), "이미 떠남");
            Assert.IsNull(r.Plots[2]);
            CollectionAssert.AreEqual(new[] { 2 }, changed);
            Assert.IsFalse(r.IsResident("A"));
        }

        // ---------------------------------------------------------------- 걸을 수 있는 칸 · 배회 목표

        // 10 × 10 칸(0 ~ 9). x = 5 줄은 벽(걸을 수 없음), (2,2)는 길(놓기 막힘이지만 걸을 수 있음)
        static (WalkMap walk, GroundGrid grid) Field()
        {
            const int n = 10;
            var walkable = new bool[n * n];
            var blocked = new bool[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                walkable[y * n + x] = x != 5;
                blocked[y * n + x] = x == 5 || (x == 2 && y == 2);
            }
            var grid = new GroundGrid(1f, Vector2Int.zero, n, n, (bool[])blocked.Clone());
            return (new WalkMap(Vector2Int.zero, n, n, walkable, blocked), grid);
        }

        [Test]
        public void WalkMap_PathIsWalkable_WallAndOutsideAreNot()
        {
            var (walk, grid) = Field();
            Assert.IsTrue(walk.IsWalkable(new Vector2Int(2, 2), grid), "길 칸");
            Assert.IsTrue(walk.IsWalkable(new Vector2Int(0, 0), grid));
            Assert.IsFalse(walk.IsWalkable(new Vector2Int(5, 3), grid), "벽");
            Assert.IsFalse(walk.IsWalkable(new Vector2Int(-1, 0), grid), "범위 밖");
            Assert.IsFalse(walk.IsWalkable(new Vector2Int(10, 0), grid));
        }

        [Test]
        public void WalkMap_PlacedThingsAndLaterBlockedCellsAreAvoided()
        {
            var (walk, grid) = Field();
            grid.TryPlace(new Vector2Int(1, 1), GroundObjectKind.Flower);
            grid.Block(new Vector2Int(3, 3));  // 묘목이 다 자란 나무
            Assert.IsFalse(walk.IsWalkable(new Vector2Int(1, 1), grid), "꽃");
            Assert.IsFalse(walk.IsWalkable(new Vector2Int(3, 3), grid), "새로 자란 나무");
            grid.Remove(new Vector2Int(1, 1));
            Assert.IsTrue(walk.IsWalkable(new Vector2Int(1, 1), grid), "치우면 다시 걸을 수 있음");
        }

        [Test]
        public void WalkMap_LineAcrossWall_NotWalkable()
        {
            var (walk, grid) = Field();
            Assert.IsTrue(walk.IsLineWalkable(new Vector2(0.5f, 0.5f), new Vector2(4.5f, 9.5f), grid, 0.5f));
            Assert.IsFalse(walk.IsLineWalkable(new Vector2(0.5f, 0.5f), new Vector2(8.5f, 0.5f), grid, 0.5f));
        }

        [Test]
        public void PickWanderTarget_ReturnsReachablePointInsideRadius()
        {
            var (walk, grid) = Field();
            var from = new Vector2(2.5f, 5.5f);
            var random = new SystemRandomSource(3);
            for (int i = 0; i < 30; i++)
            {
                var target = walk.PickWanderTarget(from, from, 4f, random, grid, 12, 0.25f);
                Assert.IsTrue(target.HasValue);
                Assert.LessOrEqual(Vector2.Distance(from, target.Value), 4f + 1e-4f);
                Assert.IsTrue(walk.IsLineWalkable(from, target.Value, grid, 0.25f));
                Assert.Less(target.Value.x, 5f, "벽 너머로 가지 않음");
            }
        }

        [Test]
        public void PickWanderTarget_NowhereToGo_ReturnsNull()
        {
            var (walk, grid) = Field();
            // 벽 칸 한가운데에 갇혀 있으면(출발점부터 못 걸음) 목표 없음
            Assert.IsNull(walk.PickWanderTarget(new Vector2(5.5f, 5.5f), new Vector2(5.5f, 5.5f), 3f, new SystemRandomSource(1), grid, 12, 0.25f));
        }

        // ---------------------------------------------------------------- 일과 · 대사

        [TestCase(DayPeriod.Morning, true)]
        [TestCase(DayPeriod.Daytime, true)]
        [TestCase(DayPeriod.Evening, true)]
        [TestCase(DayPeriod.Night, false)]
        public void IsOutside_UntilNight(DayPeriod period, bool expected)
        {
            Assert.AreEqual(expected, VillagerTalk.IsOutside(period));
        }

        static readonly string[] M = { "아침1", "아침2" }, D = { "낮1" }, E = { "저녁1" }, S = { "잡담1", "잡담2" };

        [Test]
        public void Compose_GreetingByPeriod_ThenSmallTalk()
        {
            CollectionAssert.AreEqual(new[] { "아침2", "잡담1" }, VillagerTalk.Compose(DayPeriod.Morning, M, D, E, S, new ScriptedRandom(ints: new[] { 1, 0 })));
            CollectionAssert.AreEqual(new[] { "낮1", "잡담2" }, VillagerTalk.Compose(DayPeriod.Daytime, M, D, E, S, new ScriptedRandom(ints: new[] { 0, 1 })));
            CollectionAssert.AreEqual(new[] { "저녁1", "잡담1" }, VillagerTalk.Compose(DayPeriod.Evening, M, D, E, S, new ScriptedRandom()));
            CollectionAssert.AreEqual(new[] { "저녁1", "잡담1" }, VillagerTalk.Compose(DayPeriod.Night, M, D, E, S, new ScriptedRandom()), "밤은 저녁 인사");
        }

        [Test]
        public void Compose_NoLines_GivesEllipsis()
        {
            CollectionAssert.AreEqual(new[] { "…" }, VillagerTalk.Compose(DayPeriod.Morning, null, D, E, new string[0], new ScriptedRandom()));
            CollectionAssert.AreEqual(new[] { "잡담1" }, VillagerTalk.Compose(DayPeriod.Morning, new string[0], D, E, S, new ScriptedRandom()), "인사가 없으면 잡담만");
        }

        [Test]
        public void Dialogue_AdvancesThenFinishes()
        {
            var d = new Dialogue("주민 01", new[] { "안녕", "잘 가" });
            Assert.AreEqual("안녕", d.Current);
            Assert.IsTrue(d.Advance());
            Assert.AreEqual("잘 가", d.Current);
            Assert.IsFalse(d.Advance());
            Assert.IsTrue(d.IsFinished);
            Assert.IsNull(d.Current);
            Assert.IsFalse(d.Advance(), "끝난 뒤에도 그대로");
            Assert.Throws<System.ArgumentException>(() => new Dialogue("x", new string[0]));
        }
    }
}
