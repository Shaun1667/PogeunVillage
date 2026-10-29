using NUnit.Framework;
using UnityEngine;

namespace PogeunVillage.Tests
{
    // 3-7c 채집통: 인벤토리에서는 곤충 · 점수 · 색은 곤충 것 (BALANCE 16 · 18번)
    public class CageTests
    {
        [Test]
        public void Cage_IsInsectInInventory_InsectPointsAndColor()
        {
            var beetle = ScriptableObject.CreateInstance<InsectDefinition>();
            beetle.id = "StagBeetle";
            beetle.cagePoint = 20;
            beetle.color = Color.red;
            var cage = ScriptableObject.CreateInstance<FurnitureDefinition>();
            cage.cageInsect = beetle;
            var bench = ScriptableObject.CreateInstance<FurnitureDefinition>();

            Assert.AreSame(beetle, cage.InventoryItem, "들면 곤충");
            Assert.AreEqual(20, cage.RatingPoint);
            Assert.AreEqual(Color.red, cage.ContentColor);
            Assert.IsNull(bench.ContentColor, "보통 가구는 든 것 없음");
            Object.DestroyImmediate(beetle);
            Object.DestroyImmediate(cage);
            Object.DestroyImmediate(bench);
        }
    }
}
