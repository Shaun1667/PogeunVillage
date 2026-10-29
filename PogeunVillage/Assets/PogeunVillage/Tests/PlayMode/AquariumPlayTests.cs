using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// 3-6c 확인 (Tab 창에서 물고기 칸 F → 수조 놓기 모드 · F 놓기 → 물고기 하나 빠짐 · 물고기 색 / 수조 F → 들기 → 다시 물고기 /
    /// 다음 아침 평가에 수조 점수 / 부탁 목록에 송사리 · 붕어).
    /// </summary>
    public class AquariumPlayTests : ToolSceneTest
    {
        FurniturePlacer placer;
        FurnitureKeeper keeper;
        InventoryWindow window;
        VillageRater rater;

        [UnitySetUp]
        public IEnumerator FindParts()
        {
            placer = player.GetComponent<FurniturePlacer>();
            keeper = Object.FindAnyObjectByType<FurnitureKeeper>();
            window = Object.FindAnyObjectByType<InventoryWindow>();
            rater = Object.FindAnyObjectByType<VillageRater>();
            yield break;
        }

        FishDefinition Fish(string id) => (FishDefinition)Item(id);

        [UnityTest]
        public IEnumerator FishSlot_PlacesAquarium_PickUpReturnsFish()
        {
            var catfish = Fish("Catfish");
            Assert.IsNotNull(catfish.aquarium, "물고기마다 수조");
            Assert.IsTrue(playerInventory.TryAdd(catfish, 2));
            var spot = FindOpenSpot();
            var cell = spot + Vector2Int.up;
            yield return StandFacingNorth(spot);
            yield return Tap(keyboard.tabKey);
            yield return Tap(keyboard.fKey);
            Assert.IsTrue(placer.IsPlacing, "물고기 칸 F → 놓기 모드");
            Assert.AreSame(catfish.aquarium, placer.Definition);
            yield return Tap(keyboard.fKey);
            Assert.IsFalse(placer.IsPlacing);
            Assert.AreEqual(1, Inv.Count("Catfish"), "물고기 하나 빠짐");
            var tank = keeper.Placed.Single();
            Assert.AreSame(catfish.aquarium, tank.Definition);
            Assert.AreEqual(GroundObjectKind.Furniture, Grid.Get(cell));
            Assert.AreEqual("메기 수조", tank.GetComponent<Interactable>().displayName);
            var block = new MaterialPropertyBlock();
            tank.bodyRenderers[0].GetPropertyBlock(block);
            Assert.AreEqual(catfish.color.r, block.GetColor("_BaseColor").r, 0.002f, "물고기 색");

            yield return Frames(2);
            yield return Tap(keyboard.fKey);
            yield return Tap(keyboard.fKey);                     // 들기
            Assert.IsEmpty(keeper.Placed);
            Assert.AreEqual(2, Inv.Count("Catfish"), "들면 다시 물고기");
            Assert.AreEqual(0, Inv.Count(catfish.aquarium.id), "수조 물건은 인벤토리에 없음");
        }

        [UnityTest]
        public IEnumerator Tanks_CountInRating_NextMorning()
        {
            var catfish = Fish("Catfish");
            var spot = FindOpenSpot();
            for (int i = 0; i < 2; i++)
                Assert.IsNotNull(keeper.Place(catfish.aquarium, spot + new Vector2Int(i - 1, 1), 0));
            int before = rater.Current.furniturePoints;
            yield return Tap(keyboard.f8Key);
            yield return null;
            Assert.AreEqual(before + 2 * catfish.aquariumPoint, rater.Current.furniturePoints, "수조 점수(메기 10 × 2)");
        }

        [Test]
        public void Requests_IncludeCommonFish()
        {
            var village = Object.FindAnyObjectByType<Village>(FindObjectsInactive.Include);
            var ids = village.rules.requestItems.Select(i => i.id).ToArray();
            CollectionAssert.IsSupersetOf(ids, new[] { "Medaka", "CrucianCarp" });
            CollectionAssert.IsSupersetOf(ids, new[] { "Apple", "Peach", "Flower", "Wood" }, "원래 부탁 물건도 그대로");
            Assert.IsTrue(Object.FindAnyObjectByType<Shop>().data.catalog.OfType<FurnitureDefinition>().All(f => !f.aquariumFish), "수조는 상점에 없음");
        }
    }
}
