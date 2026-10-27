using System;
using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // BALANCE.md 5번 검산 케이스 (목재 2, 5번이면 벌목)
    public class ChoppableTreeTests
    {
        static ChoppableTree Fresh() => new(5, 2);

        static int ChopTimes(ChoppableTree tree, int times)
        {
            int wood = 0;
            for (int i = 0; i < times; i++)
                wood += tree.Chop().wood;
            return wood;
        }

        [Test]
        public void OneHit_GivesTwoWood_TreeStands()  // 5-1
        {
            var tree = Fresh();
            var result = tree.Chop();
            Assert.AreEqual(2, result.wood);
            Assert.IsFalse(result.felled);
            Assert.IsFalse(tree.Felled);
        }

        [Test]
        public void FifthHit_Fells_TenWoodTotal()  // 5-2
        {
            var tree = Fresh();
            Assert.AreEqual(8, ChopTimes(tree, 4));
            Assert.IsFalse(tree.Felled);
            var fifth = tree.Chop();
            Assert.IsTrue(fifth.felled, "5번째에 베어짐");
            Assert.AreEqual(2, fifth.wood);
            Assert.IsTrue(tree.Felled);
        }

        [Test]
        public void AfterFelled_ChopGivesNothing()
        {
            var tree = Fresh();
            ChopTimes(tree, 5);
            var again = tree.Chop();
            Assert.AreEqual(0, again.wood);
            Assert.IsFalse(again.felled, "이미 베어진 나무는 다시 '베어짐'이 아님");
        }

        [Test]
        public void HalfChopped_ResetsNextMorning()  // 5-4
        {
            var tree = Fresh();
            int day1 = ChopTimes(tree, 3);
            tree.ResetHits();
            Assert.AreEqual(0, tree.Hits);
            int day2 = ChopTimes(tree, 4);
            Assert.IsFalse(tree.Felled, "아침에 초기화돼서 4번으로는 안 베어짐");
            day2 += ChopTimes(tree, 1);
            Assert.IsTrue(tree.Felled);
            Assert.AreEqual(16, day1 + day2, "1일차 6 + 2일차 10");
        }

        [Test]
        public void ResetAfterFelled_StaysFelled()
        {
            var tree = Fresh();
            ChopTimes(tree, 5);
            tree.ResetHits();
            Assert.IsTrue(tree.Felled, "벤 나무는 다시 자라지 않음");
        }

        [Test]
        public void BadInput_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChoppableTree(0, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChoppableTree(5, -1));
        }
    }
}
