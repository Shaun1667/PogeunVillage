using System.Linq;
using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // 6-1c 저장할 수 있는 난수
    public class SeededRandomTests
    {
        [Test]
        public void SameSeed_SameSequence()
        {
            var a = new SeededRandom(42);
            var b = new SeededRandom(42);
            for (int i = 0; i < 20; i++)
                Assert.AreEqual(a.Next(1000), b.Next(1000));
        }

        [Test]
        public void SavedState_ContinuesSameSequence()
        {
            var original = new SeededRandom(7);
            for (int i = 0; i < 5; i++) original.NextDouble();
            string state = original.State;
            var expected = Enumerable.Range(0, 10).Select(_ => original.Next(100)).ToArray();

            var restored = new SeededRandom(999);
            restored.State = state;
            CollectionAssert.AreEqual(expected, Enumerable.Range(0, 10).Select(_ => restored.Next(100)).ToArray(), "저장한 상태부터 같은 수");
        }

        [Test]
        public void Ranges()
        {
            var r = new SeededRandom(1);
            var seen = new bool[3];
            for (int i = 0; i < 300; i++)
            {
                double d = r.NextDouble();
                Assert.IsTrue(d >= 0 && d < 1);
                int n = r.Next(3);
                Assert.IsTrue(n >= 0 && n < 3);
                seen[n] = true;
            }
            Assert.IsTrue(seen.All(s => s), "0 · 1 · 2 모두 나옴");
            Assert.AreEqual(0, r.Next(0));
        }
    }
}
