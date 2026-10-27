using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // BALANCE.md 6번 검산 케이스 (열매 3개, 최대 3개)
    public class FruitTreeTests
    {
        const int PerDay = 3;
        const int Max = 3;

        [Test]
        public void ShakeInMorning_DropsAll()  // 6-1
        {
            var tree = new FruitTree(Max);
            Assert.AreEqual(3, tree.Shake());
            Assert.AreEqual(0, tree.Count);
        }

        [Test]
        public void ShakeTwiceSameDay_SecondDropsNothing()  // 6-2
        {
            var tree = new FruitTree(Max);
            tree.Shake();
            Assert.AreEqual(0, tree.Shake());
        }

        [Test]
        public void NotShaken_NextMorningStaysAtMax()  // 6-3
        {
            var tree = new FruitTree(Max);
            tree.Refill(PerDay, Max);
            Assert.AreEqual(3, tree.Count);
        }

        [Test]
        public void ShakenThenNextMorning_RefillsToMax()  // 6-4
        {
            var tree = new FruitTree(Max);
            tree.Shake();
            tree.Refill(PerDay, Max);
            Assert.AreEqual(3, tree.Count);
        }
    }
}
