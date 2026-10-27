using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // BALANCE.md 7번 검산 케이스 (시작 3000 모아, 도구 500, 영업 8 ~ 22시)
    public class ShopTests
    {
        [Test]
        public void StartingMoa_BuysAtMostSixTools()  // 7-1
        {
            var wallet = new Wallet(3000);
            int bought = 0;
            while (wallet.TrySpend(500)) bought++;
            Assert.AreEqual(6, bought);
            Assert.AreEqual(0, wallet.Moa);

            var wallet2 = new Wallet(3000);
            for (int i = 0; i < 5; i++) wallet2.TrySpend(500);
            Assert.AreEqual(500, wallet2.Moa, "5종 다 사면 500 남음");
        }

        [Test]
        public void NotEnough_SpendsNothing()
        {
            var wallet = new Wallet(200);
            int changes = 0;
            wallet.Changed += _ => changes++;
            Assert.IsFalse(wallet.TrySpend(300));
            Assert.AreEqual(200, wallet.Moa);
            Assert.AreEqual(0, changes);
        }

        [Test]
        public void Earn_AddsAndNotifies()
        {
            var wallet = new Wallet(0);
            var seen = new List<int>();
            wallet.Changed += seen.Add;
            wallet.Earn(300);
            wallet.Earn(0);
            CollectionAssert.AreEqual(new[] { 300 }, seen, "0은 알리지 않음");
        }

        [Test]
        public void BadInput_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Wallet(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Wallet(10).TrySpend(-5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Wallet(10).Earn(-5));
        }

        [TestCase(7, false)]
        [TestCase(8, true)]
        [TestCase(15, true)]
        [TestCase(21, true)]
        [TestCase(22, false)]
        [TestCase(0, false)]
        public void OpenFromEightToTwentyTwo(int hour, bool open)  // 7-2
        {
            Assert.AreEqual(open, ShopRules.IsOpen(hour, 8, 22));
        }

        [Test]
        public void OpenPastMidnight_Wraps()
        {
            Assert.IsTrue(ShopRules.IsOpen(1, 20, 2));
            Assert.IsFalse(ShopRules.IsOpen(3, 20, 2));
        }

        static readonly Dictionary<string, int> SellPrices = new() { ["Apple"] = 100, ["Flower"] = 40, ["Axe"] = 0 };
        static int PriceOf(string id) => SellPrices.TryGetValue(id, out int p) ? p : 0;

        [Test]
        public void SellSlot_FirstSellable_SkipsTools()  // 7-3
        {
            var inv = new Inventory(20);
            inv.TryAddWithDurability("Axe", 50);
            inv.TryAdd("Flower", 10, 3);
            inv.TryAdd("Apple", 10, 2);
            Assert.AreEqual(1, ShopRules.ChooseSellSlot(inv.Slots, PriceOf), "도구 건너뛰고 꽃 칸");
        }

        [Test]
        public void SellSlot_NothingSellable_MinusOne()
        {
            var inv = new Inventory(20);
            inv.TryAddWithDurability("Axe", 50);
            Assert.AreEqual(-1, ShopRules.ChooseSellSlot(inv.Slots, PriceOf));
        }

        [Test]
        public void TakeAll_EmptiesSlot_ReturnsWhatWasThere()
        {
            var inv = new Inventory(20);
            inv.TryAdd("Apple", 10, 3);
            var taken = inv.TakeAll(0);
            Assert.AreEqual("Apple", taken.itemId);
            Assert.AreEqual(3, taken.count);
            Assert.IsTrue(inv.Slots[0].IsEmpty);
            Assert.Throws<InvalidOperationException>(() => inv.TakeAll(0));
        }
    }
}
