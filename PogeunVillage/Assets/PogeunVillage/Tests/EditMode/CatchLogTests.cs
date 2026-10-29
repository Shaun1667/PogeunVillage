using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // 3-6b 물고기 도감 · 평가 (BALANCE 17번)
    public class CatchLogTests
    {
        [Test]
        public void FirstCatch_IsNew_ThenCounts()
        {
            var log = new CatchLog();
            Assert.IsFalse(log.Has("CrucianCarp"));
            Assert.IsTrue(log.Record("CrucianCarp"), "처음 낚은 붕어 = 새 종류");
            Assert.IsFalse(log.Record("CrucianCarp"), "두 번째는 새 종류 아님");
            Assert.IsTrue(log.Record("Carp"));
            Assert.AreEqual(2, log.Species);
            Assert.AreEqual(2, log.CaughtCount("CrucianCarp"));
            Assert.AreEqual(0, log.CaughtCount("Catfish"));
        }

        [Test]
        public void EntryText()
        {
            Assert.AreEqual("붕어 · 중 · 언제나 · 3마리", HudText.FishEntry("붕어", FishSize.Medium, new DayPeriod[0], 3));
            Assert.AreEqual("메기 · 대 · 저녁/밤 · 1마리", HudText.FishEntry("메기", FishSize.Large, new[] { DayPeriod.Evening, DayPeriod.Night }, 1));
            Assert.AreEqual("아침/낮", HudText.PeriodNames(new[] { DayPeriod.Morning, DayPeriod.Daytime }));
            Assert.AreEqual("소", HudText.FishSizeName(FishSize.Small));
        }

        [Test]
        public void FishSpecies_AddToRating()  // 17-4: 처음 보는 붕어 → +10
        {
            var thresholds = new[] { 300, 400, 550, 750 };
            var before = VillageRating.Evaluate(218, 6, 0, 0, 1, 10, 1, 10, thresholds);
            var after = VillageRating.Evaluate(218, 6, 0, 0, 1, 10, 1, 10, thresholds, 0, 1 * 10);
            Assert.AreEqual(10, after.encyclopediaPoints);
            Assert.AreEqual(before.total + 10, after.total);
            Assert.AreEqual(2, VillageRating.Evaluate(218, 6, 0, 0, 1, 10, 1, 10, thresholds, 0, 3 * 10).stars, "278 + 30 = 308 → ★2");
            StringAssert.EndsWith("· 도감 10", VillageRating.BoardLines(after, 3)[1]);
        }
    }
}
