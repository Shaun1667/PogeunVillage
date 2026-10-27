using System;
using NUnit.Framework;

namespace PogeunVillage.Tests
{
    public class PouchTests
    {
        [Test]
        public void Empty_ReturnsZero()
        {
            Assert.AreEqual(0, new Pouch().Get("Apple"));
        }

        [Test]
        public void Add_CountsPerItem()
        {
            var pouch = new Pouch();
            pouch.Add("Apple");
            pouch.Add("Apple");
            pouch.Add("Flower", 3);
            Assert.AreEqual(2, pouch.Get("Apple"));
            Assert.AreEqual(3, pouch.Get("Flower"));
            Assert.AreEqual(0, pouch.Get("Peach"));
        }

        [Test]
        public void Add_NotifiesOnlyChangedItem()
        {
            var pouch = new Pouch();
            pouch.Add("Apple");
            string changedId = null;
            int changedCount = -1;
            int calls = 0;
            pouch.Changed += (id, count) => { changedId = id; changedCount = count; calls++; };

            pouch.Add("Apple");

            Assert.AreEqual(1, calls);
            Assert.AreEqual("Apple", changedId);
            Assert.AreEqual(2, changedCount);
        }

        [Test]
        public void Add_RejectsBadInput()
        {
            var pouch = new Pouch();
            Assert.Throws<ArgumentOutOfRangeException>(() => pouch.Add("Apple", 0));
            Assert.Throws<ArgumentException>(() => pouch.Add(""));
        }
    }
}
