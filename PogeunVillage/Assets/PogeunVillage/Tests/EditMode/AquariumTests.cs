using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace PogeunVillage.Tests
{
    // 3-6c 수조: 인벤토리에서는 물고기 · 점수는 물고기의 수조 점수 · 같은 수조는 5개까지 (BALANCE 16 · 17번)
    public class AquariumTests
    {
        [Test]
        public void Aquarium_IsFishInInventory_FishPoints()
        {
            var fish = ScriptableObject.CreateInstance<FishDefinition>();
            fish.id = "Catfish";
            fish.aquariumPoint = 10;
            var tank = ScriptableObject.CreateInstance<FurnitureDefinition>();
            tank.id = "Aquarium_Catfish";
            tank.aquariumFish = fish;
            var bench = ScriptableObject.CreateInstance<FurnitureDefinition>();
            bench.ratingPoint = 15;

            Assert.AreSame(fish, tank.InventoryItem, "들면 물고기");
            Assert.AreEqual(10, tank.RatingPoint);
            Assert.AreSame(bench, bench.InventoryItem, "보통 가구는 자신");
            Assert.AreEqual(15, bench.RatingPoint);
            Object.DestroyImmediate(fish);
            Object.DestroyImmediate(tank);
            Object.DestroyImmediate(bench);
        }

        [Test]
        public void SixCatfishTanks_FiveCount()  // 17-5
        {
            var placed = Enumerable.Repeat(("Aquarium_Catfish", 10, (string)null), 6);
            Assert.AreEqual(50, VillageRating.FurniturePoints(placed, 5, 40, 4));
        }
    }
}
