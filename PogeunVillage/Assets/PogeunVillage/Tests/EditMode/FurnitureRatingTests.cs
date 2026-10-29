using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // 3-4e 가구 평가 점수 (BALANCE 15 · 16번)
    public class FurnitureRatingTests
    {
        const int Cap = 5, Bonus = 40, SetSize = 4;

        static int Points(IEnumerable<(string, int, string)> placed) => VillageRating.FurniturePoints(placed, Cap, Bonus, SetSize);

        static IEnumerable<(string, int, string)> Many(string id, int point, int count, string owner = null) =>
            Enumerable.Repeat((id, point, owner), count);

        static IEnumerable<(string, int, string)> ThemeSet(string owner) => new[]
        {
            ($"{owner}_Planter", 20, owner), ($"{owner}_Bench", 20, owner), ($"{owner}_Table", 20, owner), ($"{owner}_Lamp", 20, owner),
        };

        [Test]
        public void SevenFences_OnlyFiveCount()  // 16-3
        {
            Assert.AreEqual(25, Points(Many("Fence", 5, 7)));
            Assert.AreEqual(25 + 15, Points(Many("Fence", 5, 7).Concat(Many("Bench", 15, 1))), "종류마다 따로 셈");
        }

        [Test]
        public void FullThemeSet_PlusBonus()  // 16-4
        {
            Assert.AreEqual(120, Points(ThemeSet("A")));
            Assert.AreEqual(60, Points(ThemeSet("A").Take(3)), "3가지면 보너스 없음");
            Assert.AreEqual(60 + 20, Points(ThemeSet("A").Take(3).Concat(Many("A_Planter", 20, 1, "A"))), "같은 것 두 개로는 세트가 안 됨");
            Assert.AreEqual(120 + 20, Points(ThemeSet("A").Concat(Many("A_Bench", 20, 1, "A"))), "세트 보너스는 주민마다 한 번");
            Assert.AreEqual(240, Points(ThemeSet("A").Concat(ThemeSet("B"))), "두 주민이면 두 번");
        }

        [Test]
        public void Evaluate_AddsFurnitureToTotal_AndStars()
        {
            // 15-1(278점 ★1) + 울타리 5개(25) = 303점 → ★2
            var r = VillageRating.Evaluate(218, 6, 0, 0, 1, 10, 1, 10, new[] { 300, 400, 550, 750 }, Points(Many("Fence", 5, 5)));
            Assert.AreEqual(25, r.furniturePoints);
            Assert.AreEqual(303, r.total);
            Assert.AreEqual(2, r.stars);
            StringAssert.Contains("· 가구 25", VillageRating.BoardLines(r, 3)[1]);
            Assert.AreEqual(0, Points(new (string, int, string)[0]));
        }
    }
}
