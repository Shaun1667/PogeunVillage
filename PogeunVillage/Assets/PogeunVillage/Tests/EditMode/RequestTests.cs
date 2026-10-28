using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // 3-3 주민 부탁 (BALANCE 14번 검산 14-1 ~ 14-5)
    public class RequestTests
    {
        static readonly string[] Residents = { "V01", "V02", "V03", "V04", "V05", "V06" };
        static readonly string[] Items = { "Apple", "Peach", "Flower", "Wood" };

        [Test]
        public void Pick_SameSeedSameRequest_FromCandidatesAndItems()
        {
            var a = VillagerRequest.Pick(Residents, Items, 1, 3, new SystemRandomSource(31));
            var b = VillagerRequest.Pick(Residents, Items, 1, 3, new SystemRandomSource(31));
            Assert.AreEqual((a.VillagerId, a.ItemId, a.Count), (b.VillagerId, b.ItemId, b.Count));
            CollectionAssert.Contains(Residents, a.VillagerId);
            CollectionAssert.Contains(Items, a.ItemId);
            Assert.IsFalse(a.Done);
        }

        [Test]
        public void Case14_1_CountBetweenOneAndThree_AllAppear()
        {
            var counts = new HashSet<int>();
            for (int seed = 0; seed < 100; seed++)
                counts.Add(VillagerRequest.Pick(Residents, Items, 1, 3, new SystemRandomSource(seed)).Count);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, counts);
        }

        [Test]
        public void Pick_NoCandidatesOrItems_Null()
        {
            Assert.IsNull(VillagerRequest.Pick(new string[0], Items, 1, 3, new SystemRandomSource(1)));
            Assert.IsNull(VillagerRequest.Pick(Residents, new string[0], 1, 3, new SystemRandomSource(1)));
        }

        [TestCase(100, 2, 2, 400)]   // 14-2 사과 2개
        [TestCase(40, 3, 2, 240)]    // 14-3 꽃 3개
        [TestCase(20, 1, 2, 40)]     // 14-4 목재 1개
        public void MoaReward_SellPriceTimesCountTimesTwo(int sellPrice, int count, int multiplier, int expected)
        {
            Assert.AreEqual(expected, VillagerRequest.MoaReward(sellPrice, count, multiplier));
        }

        [Test]
        public void CanFulfill_NeedsEnough_NotTwice()
        {
            var r = new VillagerRequest("V01", "Apple", 2);
            Assert.IsFalse(r.CanFulfill(1));
            Assert.IsTrue(r.CanFulfill(2));
            r.Complete();
            Assert.IsFalse(r.CanFulfill(5), "한 번만");
        }

        [Test]
        public void TryRemove_TakesFromFrontSlots_OrNothing()
        {
            var inv = new Inventory(4);
            inv.TryAdd("Apple", 2, 2);
            inv.TryAdd("Wood", 10, 1);
            inv.TryAdd("Apple", 2, 1);   // 0번 칸 꽉 참 → 2번 칸
            var changed = new List<int>();
            inv.SlotChanged += changed.Add;

            Assert.IsFalse(inv.TryRemove("Apple", 4), "모자라면");
            Assert.AreEqual(3, inv.Count("Apple"), "아무것도 안 뺌");
            Assert.IsEmpty(changed);

            Assert.IsTrue(inv.TryRemove("Apple", 3));
            Assert.AreEqual(0, inv.Count("Apple"));
            Assert.AreEqual("Wood", inv.Slots[1].itemId, "다른 칸은 그대로");
            CollectionAssert.AreEqual(new[] { 0, 2 }, changed);
        }

        [Test]
        public void Case14_5_FriendshipReward_SeparateFromDailyCap_StopsAtMax()
        {
            var f = new Friendship(1, 2, 5, -20, 100);
            f.Gift("A"); f.Gift("A"); f.Gift("A");   // 오늘 상한 5 다 씀
            Assert.AreEqual(3, f.Reward("A", 3), "상한과 별개");
            Assert.AreEqual(8, f.Get("A"));
            var g = new Friendship(1, 2, 5, -20, 100);
            g.Begin("B", 99);
            Assert.AreEqual(1, g.Reward("B", 3));
            Assert.AreEqual(100, g.Get("B"));
        }

        [Test]
        public void SpeakerAndRequestLine()
        {
            Assert.AreEqual("주민 01  친밀도 3 · 부탁 있음", HudText.Speaker("주민 01", 3, false, true));
            Assert.AreEqual("주민 01  친밀도 3", HudText.Speaker("주민 01", 3, false, false));
            Assert.AreEqual("사과 2개만 가져다줄 수 있어?", HudText.RequestLine("{0} {1}개만 가져다줄 수 있어?", "사과", 2));
            Assert.AreEqual("사과 2개만 가져다줄 수 있어?", HudText.RequestLine("{0} {9}", "사과", 2), "틀이 잘못돼도 기본 문장");
        }
    }
}
