using System.Linq;
using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // 3-2 괴롭히기 · 이사 준비 · 떠남 (BALANCE 2번 검산 2-1 ~ 2-11)
    public class BullyTests
    {
        const double Timeout = 180;

        static Friendship Friends() => new(1, 2, 5, -20, 100);
        static BullyStreak Streak() => new(5, Timeout);

        /// <summary>n번 연속 때림(1초 간격). 깎인 횟수만큼 -2</summary>
        static void HitTimes(BullyStreak s, Friendship f, string id, int n, ref double now)
        {
            for (int i = 0; i < n; i++)
            {
                if (s.Hit(id, now)) f.Lose(id, 2);
                now += 1;
            }
        }

        [Test]
        public void Case2_1_FourHits_NoChange()
        {
            var (s, f) = (Streak(), Friends());
            double t = 0;
            HitTimes(s, f, "A", 4, ref t);
            Assert.AreEqual(0, f.Get("A"));
            Assert.AreEqual(4, s.Count("A", t));
        }

        [Test]
        public void Case2_2_FiveHits_Minus2()
        {
            var (s, f) = (Streak(), Friends());
            double t = 0;
            HitTimes(s, f, "A", 5, ref t);
            Assert.AreEqual(-2, f.Get("A"));
        }

        [Test]
        public void Case2_3_TenHits_Minus4()
        {
            var (s, f) = (Streak(), Friends());
            double t = 0;
            HitTimes(s, f, "A", 10, ref t);
            Assert.AreEqual(-4, f.Get("A"));
        }

        [Test]
        public void Case2_4_ToMinus20_Takes50Hits()
        {
            var (s, f) = (Streak(), Friends());
            double t = 0;
            int hits = 0;
            while (f.Get("A") > -20)
            {
                HitTimes(s, f, "A", 1, ref t);
                hits++;
            }
            Assert.AreEqual(50, hits);
        }

        [Test]
        public void Case2_5_FourHits_Talk_OneHit_StreakBroken()
        {
            var (s, f) = (Streak(), Friends());
            double t = 0;
            HitTimes(s, f, "A", 4, ref t);
            s.Reset("A");
            f.Talk("A");
            HitTimes(s, f, "A", 1, ref t);
            Assert.AreEqual(1, f.Get("A"));
            Assert.AreEqual(1, s.Count("A", t));
        }

        [Test]
        public void Case2_6_FourHits_ThreeMinutes_OneHit_StreakBroken()
        {
            var (s, f) = (Streak(), Friends());
            double t = 0;
            HitTimes(s, f, "A", 4, ref t);
            t += Timeout;
            HitTimes(s, f, "A", 1, ref t);
            Assert.AreEqual(0, f.Get("A"));
            Assert.AreEqual(1, s.Count("A", t - 1));
        }

        [Test]
        public void JustUnderThreeMinutes_StreakContinues()
        {
            var (s, f) = (Streak(), Friends());
            double t = 0;
            HitTimes(s, f, "A", 4, ref t);
            t += Timeout - 2;  // 마지막 타(t-1)에서 179초 뒤
            HitTimes(s, f, "A", 1, ref t);
            Assert.AreEqual(-2, f.Get("A"));
        }

        [Test]
        public void Case2_7_OtherVillagerDoesNotBreakStreak()
        {
            var (s, f) = (Streak(), Friends());
            double t = 0;
            HitTimes(s, f, "A", 4, ref t);
            HitTimes(s, f, "B", 1, ref t);
            HitTimes(s, f, "A", 1, ref t);
            Assert.AreEqual(-2, f.Get("A"));
            Assert.AreEqual(0, f.Get("B"));
        }

        [Test]
        public void Case2_8_AtMinus20_StaysMinus20()
        {
            var (s, f) = (Streak(), Friends());
            f.Lose("A", 20);
            double t = 0;
            HitTimes(s, f, "A", 5, ref t);
            Assert.AreEqual(-20, f.Get("A"));
        }

        // ---------------------------------------------------------------- 이사 준비 · 떠남

        [Test]
        public void Case2_9_Minus19_NothingHappens()
        {
            var f = Friends();
            f.Lose("A", 19);
            var d = new Departures(-20);
            Assert.IsEmpty(d.StartPreparing(new[] { "A" }, f.Get));
            Assert.IsEmpty(d.TakeLeaving());
        }

        [Test]
        public void Case2_10_Minus20_PrepareNextMorning_LeaveTheMorningAfter()
        {
            var f = Friends();
            f.Lose("A", 20);                                          // 1일차
            var d = new Departures(-20);
            Assert.IsEmpty(d.TakeLeaving(), "2일차 아침 ①: 아직 떠나지 않음");
            CollectionAssert.AreEqual(new[] { "A" }, d.StartPreparing(new[] { "A", "B" }, f.Get), "2일차 아침 ③: 이사 준비");
            Assert.IsTrue(d.IsPreparing("A"));
            CollectionAssert.AreEqual(new[] { "A" }, d.TakeLeaving(), "3일차 아침 ①: 떠남");
            Assert.IsFalse(d.IsPreparing("A"));
        }

        [Test]
        public void Case2_11_RecoveredWhilePreparing_Cancelled()
        {
            var f = Friends();
            var d = new Departures(-20);
            f.Changed += (id, v) => d.OnFriendshipChanged(id, v);
            f.Lose("A", 20);
            d.StartPreparing(new[] { "A" }, f.Get);
            f.Talk("A");                                              // -19
            Assert.IsFalse(d.IsPreparing("A"), "취소");
            Assert.IsEmpty(d.TakeLeaving(), "3일차에도 마을에 삶");
        }

        [Test]
        public void StartPreparing_NotTwice_SortedById()
        {
            var f = Friends();
            f.Lose("B", 20);
            f.Lose("A", 20);
            var d = new Departures(-20);
            CollectionAssert.AreEqual(new[] { "A", "B" }, d.StartPreparing(new[] { "B", null, "A" }, f.Get));
            Assert.IsEmpty(d.StartPreparing(new[] { "A", "B" }, f.Get), "이미 준비 중");
            Assert.AreEqual(2, d.PreparingCount);
        }

        [Test]
        public void SpeakerShowsPreparing()
        {
            Assert.AreEqual("주민 01  친밀도 -20 · 이사 준비 중", HudText.Speaker("주민 01", -20, true));
            Assert.AreEqual("주민 01  친밀도 -19", HudText.Speaker("주민 01", -19, false));
        }

        [Test]
        public void Case2_12_LeftVillagerFreesRoomForCamper()
        {
            var ids = Enumerable.Range(1, 20).Select(i => $"V{i:00}").ToList();
            var r = new VillageResidents(12);
            for (int i = 0; i < 12; i++) r.MoveIn(ids[i], new SeededRandom(i));
            Assert.IsNull(CampVisits.PickVisitor(ids, r.IsResident, null, r.Count, 12, new SeededRandom(1)), "12명이면 없음");
            r.MoveOut("V03");                                         // ① 떠남 뒤 ④ 캠핑 교체
            Assert.IsNotNull(CampVisits.PickVisitor(ids, r.IsResident, null, r.Count, 12, new SeededRandom(1)));
        }
    }
}
