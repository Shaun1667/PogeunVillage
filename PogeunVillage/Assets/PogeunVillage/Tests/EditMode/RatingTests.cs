using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // 3-5 마을 평가 (BALANCE 15번 검산 15-1 ~ 15-6)
    public class RatingTests
    {
        static readonly int[] Stars = { 300, 400, 550, 750 };

        static RatingResult Rate(int flowers, int residents, int friendshipSum, int trees) =>
            VillageRating.Evaluate(flowers, residents, friendshipSum, trees, 1, 10, 1, 10, Stars);

        [Test]
        public void Case15_1_Start_OneStar_22ToNext()
        {
            var r = Rate(218, 6, 0, 0);
            Assert.AreEqual(278, r.total);
            Assert.AreEqual(1, r.stars);
            Assert.AreEqual(300, r.nextThreshold);
            Assert.AreEqual(22, r.PointsToNext);
            Assert.AreEqual((218, 60, 0, 0), (r.flowerPoints, r.residentPoints, r.friendshipPoints, r.treePoints));
        }

        [Test]
        public void Case15_2_ExactlyThreshold_IsNextStar()
        {
            Assert.AreEqual(2, Rate(240, 6, 0, 0).stars);  // 300
            Assert.AreEqual(1, Rate(239, 6, 0, 0).stars);  // 299
        }

        [Test]
        public void Case15_3_Mixed_TwoStars_10ToNext()
        {
            var r = Rate(250, 8, 40, 2);
            Assert.AreEqual(390, r.total);
            Assert.AreEqual(2, r.stars);
            Assert.AreEqual(10, r.PointsToNext);
        }

        [Test]
        public void Case15_4_FirstGoal_ThreeStars()
        {
            Assert.AreEqual(3, Rate(250, 10, 50, 0).stars);  // 400
        }

        [Test]
        public void Case15_5_FiveStars_NoNext()
        {
            var r = Rate(300, 12, 300, 3);
            Assert.AreEqual(5, r.stars);
            Assert.AreEqual(-1, r.nextThreshold);
            Assert.AreEqual(0, r.PointsToNext);
        }

        [Test]
        public void Case15_6_NegativeFriendship_Lowers_ButAtLeastOneStar()
        {
            var r = Rate(218, 6, -100, 0);
            Assert.AreEqual(178, r.total);
            Assert.AreEqual(-100, r.friendshipPoints);
            Assert.AreEqual(1, r.stars);
            Assert.AreEqual(1, Rate(0, 0, -500, 0).stars);
        }

        [Test]
        public void StarTextAndBoardLines()
        {
            Assert.AreEqual("★★☆☆☆", VillageRating.StarText(2));
            Assert.AreEqual("★★★★★", VillageRating.StarText(9));
            var lines = VillageRating.BoardLines(Rate(218, 6, 0, 0), 3);
            Assert.AreEqual(3, lines.Count);
            StringAssert.Contains("★☆☆☆☆", lines[0]);
            StringAssert.Contains("278점", lines[0]);
            Assert.AreEqual("꽃 218 · 주민 60 · 친밀도 0 · 키운 나무 0 · 가구 0 · 도감 0", lines[1]);
            Assert.AreEqual("다음 별까지 22점 · 첫 목표 ★3", lines[2]);
            Assert.AreEqual("최고 평가예요! · 첫 목표 ★3 달성!", VillageRating.BoardLines(Rate(300, 12, 300, 3), 3)[2]);
        }
    }
}
