using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PogeunVillage.Tests
{
    // 3-6a 낚시: 입질 시간표 · 물고기 고르기 · 그림자 자리 (BALANCE 17번)
    public class FishingTests
    {
        class ScriptedRandom : IRandomSource
        {
            readonly Queue<double> doubles;
            readonly Queue<int> ints;
            public ScriptedRandom(IEnumerable<int> ints = null, IEnumerable<double> doubles = null)
            {
                this.ints = new Queue<int>(ints ?? new int[0]);
                this.doubles = new Queue<double>(doubles ?? new double[0]);
            }
            public double NextDouble() => doubles.Count > 0 ? doubles.Dequeue() : 0;
            public int Next(int maxExclusive) => (ints.Count > 0 ? ints.Dequeue() : 0) % maxExclusive;
        }

        static readonly Rarity[] Rarities =
        {
            Rarity.Common, Rarity.Common, Rarity.Common, Rarity.Common,
            Rarity.Uncommon, Rarity.Uncommon, Rarity.Rare, Rarity.Rare,
        };
        static readonly int[] Weights = { 60, 30, 10 };

        [Test]
        public void Timeline_ApproachNibblesBite()
        {
            // 다가옴 1.5초 → 속임수 2번(간격 1 · 2초) → 입질(간격 1.5초), 누를 시간 0.8초
            var t = BiteTimeline.Create(10f, 1.5f, 0, 2, 1f, 2f, 0.8f, new ScriptedRandom(new[] { 2 }, new[] { 0.0, 1.0, 0.5 }));
            Assert.AreEqual(11.5f, t.ApproachEnd, 1e-4f);
            CollectionAssert.AreEqual(new[] { 12.5f, 14.5f }, t.NibbleTimes);
            Assert.AreEqual(16f, t.BiteTime, 1e-4f);
            Assert.AreEqual(BitePhase.Approaching, t.PhaseAt(11f));
            Assert.AreEqual(BitePhase.Waiting, t.PhaseAt(12f));
            Assert.AreEqual(BitePhase.Nibble, t.PhaseAt(12.6f));
            Assert.AreEqual(BitePhase.Bite, t.PhaseAt(16.5f));
            Assert.AreEqual(BitePhase.Gone, t.PhaseAt(16.8f));
        }

        [Test]
        public void Press_OnBite_Caught_EarlyOrLate_Missed()  // 17-1 · 17-2
        {
            var t = new BiteTimeline(1f, new[] { 2f }, 4f, 0.8f);
            Assert.AreEqual(CatchResult.Caught, t.Press(4.5f), "입질 0.5초 뒤");
            Assert.AreEqual(CatchResult.Spooked, t.Press(2.1f), "속임수 때");
            Assert.AreEqual(CatchResult.Spooked, t.Press(0.5f), "다가오는 중");
            Assert.AreEqual(CatchResult.TooLate, t.Press(5f), "입질 1초 뒤");
        }

        [Test]
        public void NibbleCount_FromZeroToMax()
        {
            var counts = new HashSet<int>();
            var random = new SystemRandomSource(7);
            for (int i = 0; i < 50; i++)
                counts.Add(BiteTimeline.Create(0f, 1.5f, 0, 2, 1f, 2f, 0.8f, random).NibbleTimes.Count);
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, counts);
        }

        [Test]
        public void PickFish_OnlyThisPeriod()  // 17-3 낮 11시: 미꾸라지 · 메기 · 황금잉어 없음
        {
            var fish = new List<FishDefinition>();
            DayPeriod[][] periods =
            {
                new DayPeriod[0], new[] { DayPeriod.Morning, DayPeriod.Daytime }, new DayPeriod[0], new[] { DayPeriod.Evening, DayPeriod.Night },
                new DayPeriod[0], new[] { DayPeriod.Evening, DayPeriod.Night }, new[] { DayPeriod.Daytime }, new[] { DayPeriod.Evening },
            };
            foreach (var p in periods)
            {
                var f = ScriptableObject.CreateInstance<FishDefinition>();
                f.periods = p;
                fish.Add(f);
            }
            var available = fish.ConvertAll(f => f.AppearsIn(DayPeriod.Daytime));
            CollectionAssert.AreEqual(new[] { true, true, true, false, true, false, true, false }, available);

            var random = new SystemRandomSource(3);
            var seen = new HashSet<int>();
            for (int i = 0; i < 500; i++)
                seen.Add(FishPicker.PickFish(Rarities, available, Weights, random));
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 4, 6 }, seen);
            foreach (var f in fish) Object.DestroyImmediate(f);
        }

        [Test]
        public void PickFish_WeightsByRarity()
        {
            var all = new bool[8];
            for (int i = 0; i < 8; i++) all[i] = true;
            // 합계 = 흔함 60×4 + 보통 30×2 + 드문 10×2 = 320
            Assert.AreEqual(0, FishPicker.PickFish(Rarities, all, Weights, new ScriptedRandom(new[] { 0 })));
            Assert.AreEqual(3, FishPicker.PickFish(Rarities, all, Weights, new ScriptedRandom(new[] { 239 })));
            Assert.AreEqual(4, FishPicker.PickFish(Rarities, all, Weights, new ScriptedRandom(new[] { 240 })));
            Assert.AreEqual(7, FishPicker.PickFish(Rarities, all, Weights, new ScriptedRandom(new[] { 319 })));
            Assert.AreEqual(-1, FishPicker.PickFish(Rarities, new bool[8], Weights, new ScriptedRandom()), "나올 물고기 없음");
        }

        [Test]
        public void PickWaterCell_KeepsSpacing()
        {
            var water = new List<Vector2Int>();
            for (int x = 0; x < 10; x++) water.Add(new Vector2Int(x, 0));
            var occupied = new List<Vector2Int> { new(0, 0) };
            for (int i = 0; i < 20; i++)
            {
                var c = FishPicker.PickWaterCell(water, occupied, 4, new SystemRandomSource(i));
                Assert.IsTrue(c.HasValue);
                Assert.GreaterOrEqual(c.Value.x, 4);
            }
            occupied.Add(new Vector2Int(5, 0));
            occupied.Add(new Vector2Int(9, 0));
            Assert.IsNull(FishPicker.PickWaterCell(water, occupied, 4, new SystemRandomSource(1)), "빈자리 없음");
            Assert.IsNull(FishPicker.PickWaterCell(new List<Vector2Int>(), occupied, 4, new SystemRandomSource(1)));
        }
    }
}
