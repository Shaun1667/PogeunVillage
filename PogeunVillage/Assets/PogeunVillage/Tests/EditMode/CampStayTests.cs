using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // 3-1 캠핑 주민 이주 확정 (BALANCE 1번 검산 1-1 ~ 1-6, 1-12) · 이사 온 주민 친밀도 이어받기
    public class CampStayTests
    {
        static CampStay Stay() => new("V07", talkGain: 1, giftGain: 2, dailyCap: 5, moveInThreshold: 5);

        [Test]
        public void Case1_1_OneTalk_NotConfirmed()
        {
            var s = Stay();
            s.Talk();
            Assert.AreEqual(1, s.Value);
            Assert.IsFalse(s.Confirmed);
        }

        [Test]
        public void Case1_2_ThreeTalks_OnlyFirstCounts()
        {
            var s = Stay();
            s.Talk(); s.Talk(); s.Talk();
            Assert.AreEqual(1, s.Value);
            Assert.IsFalse(s.Confirmed);
        }

        [Test]
        public void Case1_3_TwoGifts_NotConfirmed()
        {
            var s = Stay();
            s.Gift(); s.Gift();
            Assert.AreEqual(4, s.Value);
            Assert.IsFalse(s.Confirmed);
        }

        [Test]
        public void Case1_4_TalkAndTwoGifts_Confirmed()
        {
            var s = Stay();
            s.Talk();
            s.Gift();
            Assert.IsFalse(s.Confirmed);
            s.Gift();
            Assert.AreEqual(5, s.Value);
            Assert.IsTrue(s.Confirmed, "최소 경로");
        }

        [Test]
        public void Case1_5_ThreeGifts_CappedAtFive_Confirmed()
        {
            var s = Stay();
            s.Gift(); s.Gift(); s.Gift();
            Assert.AreEqual(5, s.Value);
            Assert.IsTrue(s.Confirmed);
        }

        [Test]
        public void Case1_6_TalkAndFiveGifts_ExtraDropped()
        {
            var s = Stay();
            s.Talk();
            for (int i = 0; i < 5; i++) s.Gift();
            Assert.AreEqual(5, s.Value);
            Assert.IsTrue(s.Confirmed);
        }

        [Test]
        public void Case1_12_AfterConfirmed_NothingChanges()
        {
            var s = Stay();
            s.Gift(); s.Gift(); s.Talk();
            Assert.IsTrue(s.Confirmed);
            Assert.AreEqual(0, s.Gift());
            Assert.AreEqual(0, s.Talk());
            Assert.AreEqual(5, s.Value);
        }

        [Test]
        public void NewVisit_StartsFromZero()
        {
            var yesterday = Stay();
            yesterday.Talk();
            Assert.AreEqual(1, yesterday.Value);
            Assert.AreEqual(0, Stay().Value, "미달 → 다음 방문 때 0부터");
        }

        [Test]
        public void MovedInVillager_KeepsCampValue_ClampedToRange()
        {
            var f = new Friendship(1, 2, 5, -20, 100);
            var seen = new System.Collections.Generic.List<int>();
            f.Changed += (_, v) => seen.Add(v);
            f.Begin("V07", 5);
            Assert.AreEqual(5, f.Get("V07"));
            Assert.AreEqual(1, f.Talk("V07"), "이어받은 값은 하루 상한과 별개");
            f.Begin("V08", 500);
            Assert.AreEqual(100, f.Get("V08"));
            CollectionAssert.AreEqual(new[] { 5, 6, 100 }, seen);
        }
    }
}
