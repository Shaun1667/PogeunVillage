using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // 3-4c 상점 가구 사기 · 별 해금 (BALANCE 16번)
    public class FurnitureShopTests
    {
        // 기본 8가지의 열리는 별(데이터 차례: 울타리 · 상자 · 화분 · 의자 · 벤치 · 탁자 · 가로등 · 새집)
        static readonly int[] UnlockStars = { 1, 1, 1, 2, 2, 3, 3, 4 };

        [Test]
        public void Buy_SpendsMoa_AddsOne()
        {
            var inv = new Inventory(3);
            var wallet = new Wallet(1000);
            Assert.AreEqual(BuyResult.Bought, ShopRules.BuyFurniture(inv, wallet, "Fence", 5, 200, 1, 1));
            Assert.AreEqual(800, wallet.Moa);
            Assert.AreEqual(1, inv.Count("Fence"));
        }

        [Test]
        public void Locked_Full_NotEnoughMoa_NothingChanges()
        {
            var inv = new Inventory(1);
            var wallet = new Wallet(1000);
            Assert.AreEqual(BuyResult.Locked, ShopRules.BuyFurniture(inv, wallet, "LogStool", 5, 400, 2, 1));
            Assert.AreEqual(BuyResult.NotEnoughMoa, ShopRules.BuyFurniture(inv, wallet, "Birdhouse", 5, 1500, 4, 4));
            inv.TryAdd("Apple", 10, 1);
            Assert.AreEqual(BuyResult.Full, ShopRules.BuyFurniture(inv, wallet, "Fence", 5, 200, 1, 1));
            Assert.AreEqual(1000, wallet.Moa);
            Assert.AreEqual(0, inv.Count("Fence"));
        }

        [Test]
        public void StackOfFive_SixthNeedsNewSlot()
        {
            var inv = new Inventory(1);
            var wallet = new Wallet(5000);
            for (int i = 0; i < 5; i++)
                Assert.AreEqual(BuyResult.Bought, ShopRules.BuyFurniture(inv, wallet, "Fence", 5, 200, 1, 1));
            Assert.AreEqual(BuyResult.Full, ShopRules.BuyFurniture(inv, wallet, "Fence", 5, 200, 1, 1));
            Assert.AreEqual(4000, wallet.Moa);
        }

        [Test]
        public void Star1_FenceCratePlanterOnly()  // 16-5 (상점)
        {
            int open = 0;
            foreach (int u in UnlockStars)
                if (FurnitureRules.IsUnlocked(u, 1)) open++;
            Assert.AreEqual(3, open);
        }

        [Test]
        public void NewlyUnlocked_OnlyFirstTime()
        {
            CollectionAssert.AreEqual(new[] { 3, 4 }, ShopRules.NewlyUnlocked(UnlockStars, 1, 2));
            CollectionAssert.AreEqual(new[] { 3, 4, 5, 6 }, ShopRules.NewlyUnlocked(UnlockStars, 1, 3), "두 별을 한 번에");
            CollectionAssert.IsEmpty(ShopRules.NewlyUnlocked(UnlockStars, 3, 2), "내려가면 없음");
            CollectionAssert.IsEmpty(ShopRules.NewlyUnlocked(UnlockStars, 3, 3), "다시 닿아도 없음");
            CollectionAssert.AreEqual(new[] { 7 }, ShopRules.NewlyUnlocked(UnlockStars, 3, 5));
        }
    }
}
