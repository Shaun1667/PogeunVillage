using System.Collections.Generic;
using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // 3-4d 테마 가구: 친밀도 보상 · 오늘의 테마 가구 (BALANCE 16번)
    public class ThemeFurnitureTests
    {
        static readonly int[] Thresholds = { 10, 25, 45, 70 };

        class ScriptedRandom : IRandomSource
        {
            readonly Queue<int> ints;
            public ScriptedRandom(params int[] ints) => this.ints = new Queue<int>(ints);
            public double NextDouble() => 0;
            public int Next(int maxExclusive) => ints.Dequeue() % maxExclusive;
        }

        [Test]
        public void Friendship9To10_FirstReward_NotAgain()  // 16-6
        {
            var rewards = new ThemeRewards(Thresholds);
            Assert.AreEqual(-1, rewards.Due("A", 9));
            Assert.AreEqual(0, rewards.Due("A", 10), "화분");
            rewards.MarkGiven("A");
            Assert.AreEqual(-1, rewards.Due("A", 10), "다시 10이 돼도 또 받지 않음");
            Assert.AreEqual(-1, rewards.Due("A", 24));
            Assert.AreEqual(1, rewards.Due("A", 25), "벤치");
        }

        [Test]
        public void SeveralReached_OnePerTalk_InOrder()
        {
            var rewards = new ThemeRewards(Thresholds);
            var got = new List<int>();
            for (int talk = 0; talk < 6; talk++)
            {
                int due = rewards.Due("A", 100);
                if (due < 0) break;
                got.Add(due);
                rewards.MarkGiven("A");
            }
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, got, "화분 → 벤치 → 탁자 → 가로등, 네 개로 끝");
            Assert.AreEqual(4, rewards.Given("A"));
        }

        [Test]
        public void EachVillagerCountsSeparately_DropDoesNotTakeBack()
        {
            var rewards = new ThemeRewards(Thresholds);
            rewards.MarkGiven("A");
            Assert.AreEqual(0, rewards.Due("B", 10));
            Assert.AreEqual(-1, rewards.Due("A", -20), "내려가도 받은 건 그대로");
            Assert.AreEqual(1, rewards.Given("A"));
        }

        [Test]
        public void TodayTheme_RandomResidentAndPiece()
        {
            Assert.AreEqual((2, 3), ShopRules.PickTodayTheme(6, 4, new ScriptedRandom(2, 3)));
            Assert.AreEqual((5, 0), ShopRules.PickTodayTheme(6, 4, new ScriptedRandom(5, 0)));
            Assert.AreEqual((-1, -1), ShopRules.PickTodayTheme(0, 4, new ScriptedRandom()), "주민이 없으면 없음");
        }
    }
}
