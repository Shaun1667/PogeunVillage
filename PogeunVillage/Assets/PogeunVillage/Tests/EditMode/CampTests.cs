using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // 2-6 캠핑 방문 주민 고르기 (BALANCE 1번 검산 1-9 ~ 1-11)
    public class CampTests
    {
        static readonly List<string> Twenty = Enumerable.Range(1, 20).Select(i => $"V{i:00}").ToList();

        static string Pick(VillageResidents residents, string yesterday, int seed = 1, int max = 12) =>
            CampVisits.PickVisitor(Twenty, residents.IsResident, yesterday, residents.Count, max, new SystemRandomSource(seed));

        static VillageResidents Village(int count)
        {
            var r = new VillageResidents(12);
            for (int i = 0; i < count; i++)
                r.MoveIn(Twenty[i], new SystemRandomSource(i));
            return r;
        }

        [Test]
        public void Visitor_IsNeverAResident()
        {
            var r = Village(6);
            for (int seed = 0; seed < 50; seed++)
            {
                string v = Pick(r, null, seed);
                Assert.IsNotNull(v);
                Assert.IsFalse(r.IsResident(v), v);
            }
        }

        [Test]
        public void SameSeed_SameVisitor()
        {
            var r = Village(6);
            Assert.AreEqual(Pick(r, "V10", 7), Pick(r, "V10", 7));
        }

        [Test]
        public void Case1_9_FullVillage_NoVisitor()
        {
            Assert.IsNull(Pick(Village(12), null));
            Assert.IsNotNull(Pick(Village(11), null), "11명이면 옴");
        }

        [Test]
        public void Case1_10_LeftVillager_IsCandidateAgain()
        {
            var r = Village(6);
            r.MoveOut("V01");
            bool seen = Enumerable.Range(0, 200).Any(seed => Pick(r, null, seed) == "V01");
            Assert.IsTrue(seen, "떠난 주민도 다시 후보");
        }

        [Test]
        public void Case1_11_YesterdaysVisitor_Excluded_ThenEligibleNextDay()
        {
            var r = Village(6);
            for (int seed = 0; seed < 100; seed++)
                Assert.AreNotEqual("V07", Pick(r, "V07", seed), "어제 온 주민은 오늘 안 옴");
            bool back = Enumerable.Range(0, 200).Any(seed => Pick(r, "V08", seed) == "V07");
            Assert.IsTrue(back, "하루 걸러서는 다시 올 수 있음");
        }

        [Test]
        public void OnlyYesterdayLeft_NoVisitor()
        {
            var r = new VillageResidents(20);
            for (int i = 1; i < 20; i++)
                r.MoveIn(Twenty[i], new SystemRandomSource(i));
            Assert.IsNull(CampVisits.PickVisitor(Twenty, r.IsResident, "V01", r.Count, 20, new SystemRandomSource(1)));
        }

        [Test]
        public void CampSpeaker_Text()
        {
            Assert.AreEqual("주민 07  캠핑 중", HudText.CampSpeaker("주민 07"));
        }
    }
}
