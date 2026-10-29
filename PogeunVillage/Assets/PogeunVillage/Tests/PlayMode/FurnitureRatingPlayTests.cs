using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// 3-4e 확인 (놓은 가구는 다음 날 아침 평가에 들어감 · 울타리 6개 중 5개만 · 한 주민 테마 4가지 = +40 · 게시판 '가구' 점수 ·
    /// 들면 다음 아침에 빠짐).
    /// </summary>
    public class FurnitureRatingPlayTests : ToolSceneTest
    {
        VillageRater rater;
        FurnitureKeeper keeper;

        [UnitySetUp]
        public IEnumerator FindRater()
        {
            rater = Object.FindAnyObjectByType<VillageRater>();
            keeper = Object.FindAnyObjectByType<FurnitureKeeper>();
            Assert.IsNotNull(rater.furniture, "평가가 놓인 가구를 봄");
            yield break;
        }

        [UnityTest]
        public IEnumerator PlacedFurniture_CountsNextMorning_CapAndSetBonus()
        {
            Assert.AreEqual(0, rater.Current.furniturePoints);
            var spot = FindOpenSpot();
            var cells = Enumerable.Range(-1, 3).SelectMany(dx => Enumerable.Range(0, 4).Select(dy => spot + new Vector2Int(dx, dy))).ToList();
            var fence = (FurnitureDefinition)Item("Fence");
            var owner = Object.FindAnyObjectByType<Village>(FindObjectsInactive.Include).rules.villagers[0];
            for (int i = 0; i < 6; i++)
                Assert.IsNotNull(keeper.Place(fence, cells[i], 0));
            for (int k = 0; k < 4; k++)
                Assert.IsNotNull(keeper.Place(owner.themeFurniture[k], cells[6 + k], 0));
            Assert.AreEqual(0, rater.Current.furniturePoints, "놓자마자는 그대로(아침 기준)");

            yield return Tap(keyboard.f8Key);                   // 다음 날 아침
            yield return null;
            Assert.AreEqual(5 * 5 + 4 * 20 + 40, rater.Current.furniturePoints, "울타리 5개까지 + 테마 4가지 + 세트");
            StringAssert.Contains("가구 145", string.Join("|", VillageRating.BoardLines(rater.Current, rater.rules.firstGoalStars)));

            Assert.IsTrue(keeper.PickUp(keeper.Placed.First(p => p.Definition == owner.themeFurniture[3])));
            yield return Tap(keyboard.f8Key);
            yield return null;
            Assert.AreEqual(25 + 3 * 20, rater.Current.furniturePoints, "하나를 들면 세트 보너스도 빠짐");
        }
    }
}
