using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // BALANCE.md 10번 검산 케이스 (20칸, 겹침 10)
    public class InventoryTests
    {
        const int Slots = 20;
        const int Stack = 10;

        static Inventory Fresh() => new(Slots);

        [Test]
        public void Empty_HasNoItems()  // 10-1
        {
            var inv = Fresh();
            Assert.AreEqual(Slots, inv.Slots.Count);
            Assert.IsTrue(inv.Slots.All(s => s.IsEmpty));
            Assert.AreEqual(0, inv.Count("Apple"));
        }

        [Test]
        public void SameItem_StacksInOneSlot_UpToMax()  // 10-2
        {
            var inv = Fresh();
            for (int i = 0; i < Stack; i++)
                Assert.IsTrue(inv.TryAdd("Apple", Stack));
            Assert.AreEqual(1, inv.UsedSlots);
            Assert.AreEqual(Stack, inv.Slots[0].count);

            Assert.IsTrue(inv.TryAdd("Apple", Stack));  // 11번째
            Assert.AreEqual(2, inv.UsedSlots);
            Assert.AreEqual(1, inv.Slots[1].count);
            Assert.AreEqual(Stack + 1, inv.Count("Apple"));
        }

        [Test]
        public void DifferentItems_UseSeparateSlots_InOrder()  // 10-3
        {
            var inv = Fresh();
            inv.TryAdd("Apple", Stack);
            inv.TryAdd("Flower", Stack);
            inv.TryAdd("Apple", Stack);
            Assert.AreEqual("Apple", inv.Slots[0].itemId);
            Assert.AreEqual(2, inv.Slots[0].count);
            Assert.AreEqual("Flower", inv.Slots[1].itemId);
        }

        static Inventory FilledWithSingles()
        {
            var inv = Fresh();
            for (int i = 0; i < Slots; i++)
                inv.TryAdd("Filler" + i, 1);
            return inv;
        }

        [Test]
        public void Full_RejectsNewItem_WithoutChange()  // 10-4
        {
            var inv = FilledWithSingles();
            int changes = 0;
            inv.SlotChanged += _ => changes++;
            Assert.IsFalse(inv.TryAdd("Apple", Stack));
            Assert.AreEqual(0, inv.Count("Apple"));
            Assert.AreEqual(0, changes);
        }

        [Test]
        public void Full_StillAcceptsIntoUnfilledStack()  // 10-5
        {
            var inv = Fresh();
            inv.TryAdd("Apple", Stack);
            for (int i = 1; i < Slots; i++)
                inv.TryAdd("Filler" + i, 1);
            Assert.IsTrue(inv.TryAdd("Apple", Stack));
            Assert.AreEqual(2, inv.Count("Apple"));
        }

        [Test]
        public void NotEnoughRoom_AddsNothing()  // 10-6
        {
            var inv = Fresh();
            for (int i = 0; i < Slots - 1; i++)
                inv.TryAdd("Filler" + i, 1);
            // 빈 칸 1개 = 10개까지
            Assert.IsFalse(inv.TryAdd("Apple", Stack, 11));
            Assert.AreEqual(0, inv.Count("Apple"));
            Assert.IsTrue(inv.TryAdd("Apple", Stack, 10));
        }

        [Test]
        public void SlotChanged_ReportsOnlyChangedSlots()
        {
            var inv = Fresh();
            inv.TryAdd("Apple", Stack, 9);
            var changed = new List<int>();
            inv.SlotChanged += changed.Add;
            inv.TryAdd("Apple", Stack, 3);  // 0번 칸 +1, 1번 칸 +2
            CollectionAssert.AreEqual(new[] { 0, 1 }, changed);
        }

        [Test]
        public void NewItem_GoesToFirstEmptySlot()
        {
            var inv = Fresh();
            inv.TryAdd("Apple", 1);
            inv.TryAdd("Peach", 1);
            Assert.AreEqual(1, Array.IndexOf(inv.Slots.Select(s => s.itemId).ToArray(), "Peach"));
        }

        [Test]
        public void BadInput_Throws()
        {
            var inv = Fresh();
            Assert.Throws<ArgumentOutOfRangeException>(() => new Inventory(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => inv.TryAdd("Apple", Stack, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => inv.TryAdd("Apple", 0));
            Assert.Throws<ArgumentException>(() => inv.TryAdd("", Stack));
        }
    }
}
