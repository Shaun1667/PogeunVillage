using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// 3-7c 확인 (Tab 창에서 곤충 칸 F → 채집통 놓기 · 하나 빠짐 · 곤충 색 / 채집통 F → 들기 → 다시 곤충 /
    /// 다음 아침 채집통 점수 / 부탁 목록에 배추흰나비 · 메뚜기).
    /// </summary>
    public class CagePlayTests : ToolSceneTest
    {
        FurniturePlacer placer;
        FurnitureKeeper keeper;
        VillageRater rater;

        [UnitySetUp]
        public IEnumerator FindParts()
        {
            placer = player.GetComponent<FurniturePlacer>();
            keeper = Object.FindAnyObjectByType<FurnitureKeeper>();
            rater = Object.FindAnyObjectByType<VillageRater>();
            yield break;
        }

        InsectDefinition Insect(string id) => (InsectDefinition)Item(id);

        [UnityTest]
        public IEnumerator InsectSlot_PlacesCage_PickUpReturnsInsect()
        {
            var hopper = Insect("Grasshopper");
            Assert.IsNotNull(hopper.cage, "곤충마다 채집통");
            Assert.IsTrue(playerInventory.TryAdd(hopper, 2));
            var spot = FindOpenSpot();
            yield return StandFacingNorth(spot);
            yield return Tap(keyboard.tabKey);
            yield return Tap(keyboard.fKey);
            Assert.IsTrue(placer.IsPlacing, "곤충 칸 F → 놓기 모드");
            Assert.AreSame(hopper.cage, placer.Definition);
            yield return Tap(keyboard.fKey);
            Assert.AreEqual(1, Inv.Count("Grasshopper"), "곤충 하나 빠짐");
            var cage = keeper.Placed.Single();
            Assert.AreEqual("메뚜기 채집통", cage.GetComponent<Interactable>().displayName);
            var block = new MaterialPropertyBlock();
            cage.bodyRenderers[0].GetPropertyBlock(block);
            Assert.AreEqual(hopper.color.g, block.GetColor("_BaseColor").g, 0.002f, "곤충 색");

            yield return Frames(2);
            yield return Tap(keyboard.fKey);
            yield return Tap(keyboard.fKey);                     // 들기
            Assert.IsEmpty(keeper.Placed);
            Assert.AreEqual(2, Inv.Count("Grasshopper"), "들면 다시 곤충");
        }

        [UnityTest]
        public IEnumerator Cages_CountInRating_NextMorning()
        {
            var beetle = Insect("StagBeetle");
            var spot = FindOpenSpot();
            Assert.IsNotNull(keeper.Place(beetle.cage, spot + Vector2Int.up, 0));
            int before = rater.Current.furniturePoints;
            yield return Tap(keyboard.f8Key);
            yield return null;
            Assert.AreEqual(before + beetle.cagePoint, rater.Current.furniturePoints, "사슴벌레 채집통 20");
        }

        [Test]
        public void Requests_IncludeCommonInsects()
        {
            var village = Object.FindAnyObjectByType<Village>(FindObjectsInactive.Include);
            var ids = village.rules.requestItems.Select(i => i.id).ToArray();
            CollectionAssert.IsSupersetOf(ids, new[] { "CabbageWhite", "Grasshopper", "Medaka", "CrucianCarp", "Apple" });
            Assert.IsTrue(Object.FindAnyObjectByType<Shop>().data.catalog.OfType<FurnitureDefinition>().All(f => !f.cageInsect), "채집통은 상점에 없음");
        }
    }
}
