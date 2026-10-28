using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // 3-4b 작업대 제작 규칙 (BALANCE 16번)
    public class CraftTests
    {
        static CraftResult Craft(Inventory inv, int woodCost, string id = "Fence", int unlockStars = 1, int stars = 1) =>
            CraftRules.Craft(inv, "Wood", 30, woodCost, id, 5, unlockStars, stars);

        [Test]
        public void Wood10_MakesFiveFences_WoodZero()  // 16-1
        {
            var inv = new Inventory(5);
            inv.TryAdd("Wood", 30, 10);
            for (int i = 0; i < 5; i++)
                Assert.AreEqual(CraftResult.Made, Craft(inv, 2));
            Assert.AreEqual(5, inv.Count("Fence"));
            Assert.AreEqual(0, inv.Count("Wood"));
            Assert.AreEqual(CraftResult.NotEnoughWood, Craft(inv, 2));
        }

        [Test]
        public void Wood7_CannotMakeBench_NothingChanges()  // 16-2
        {
            var inv = new Inventory(5);
            inv.TryAdd("Wood", 30, 7);
            Assert.AreEqual(CraftResult.NotEnoughWood, Craft(inv, 8, "Bench"));
            Assert.AreEqual(7, inv.Count("Wood"));
            Assert.AreEqual(0, inv.Count("Bench"));
        }

        [Test]
        public void Locked_UntilStars()  // 16-5 (제작: ★1이면 울타리 · 상자만)
        {
            Assert.IsTrue(CraftRules.IsUnlocked(1, 1));
            Assert.IsFalse(CraftRules.IsUnlocked(2, 1));
            Assert.IsTrue(CraftRules.IsUnlocked(2, 3));
            var inv = new Inventory(5);
            inv.TryAdd("Wood", 30, 20);
            Assert.AreEqual(CraftResult.Locked, Craft(inv, 4, "LogStool", unlockStars: 2, stars: 1));
            Assert.AreEqual(20, inv.Count("Wood"), "안 열렸으면 목재 그대로");
            Assert.AreEqual(CraftResult.Made, Craft(inv, 4, "LogStool", unlockStars: 2, stars: 2));
        }

        [Test]
        public void FullInventory_WoodStays()
        {
            var inv = new Inventory(2);
            inv.TryAdd("Wood", 30, 10);
            inv.TryAdd("Apple", 10, 1);
            Assert.AreEqual(CraftResult.Full, Craft(inv, 2));
            Assert.AreEqual(10, inv.Count("Wood"), "뺀 목재를 돌려놓음");
            Assert.AreEqual("Wood", inv.Slots[0].itemId, "같은 칸");

            // 목재가 딱 맞으면 그 칸이 비어 가구가 들어감
            var exact = new Inventory(2);
            exact.TryAdd("Wood", 30, 2);
            exact.TryAdd("Apple", 10, 1);
            Assert.AreEqual(CraftResult.Made, Craft(exact, 2));
            Assert.AreEqual("Fence", exact.Slots[0].itemId);
        }

        [Test]
        public void OnlyWoodFurniture_IsRecipe()
        {
            Assert.IsTrue(CraftRules.IsRecipe(2));
            Assert.IsFalse(CraftRules.IsRecipe(0), "화분 · 가로등 · 새집은 구매만");
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Craft(new Inventory(1), 0));
        }

        [Test]
        public void RecipeText_LockedShowsStars_MutedWhenUnusable()
        {
            Assert.AreEqual("나무 울타리 · 목재 2", HudText.Recipe("나무 울타리", 2, 1, 1));
            Assert.AreEqual("통나무 의자 · ★2에 열려요", HudText.Recipe("통나무 의자", 4, 2, 1));
            string list = HudText.RecipeList(new[] { "A", "B", "C" }, new[] { true, false, true }, 0, "#111111", "#999999");
            Assert.AreEqual("<color=#111111><b>A</b></color>\n<color=#999999>B</color>\nC", list);
        }
    }
}
