namespace PogeunVillage
{
    /// <summary>주입하는 난수(용도별로 따로 만든다). 테스트에서는 정해진 값을 돌려주는 가짜로 바꿔 끼운다.</summary>
    public interface IRandomSource
    {
        /// <summary>[0, 1)</summary>
        double NextDouble();
        /// <summary>[0, maxExclusive)</summary>
        int Next(int maxExclusive);
    }

    public class SystemRandomSource : IRandomSource
    {
        readonly System.Random random;

        public SystemRandomSource(int seed) => random = new System.Random(seed);

        public double NextDouble() => random.NextDouble();
        public int Next(int maxExclusive) => random.Next(maxExclusive);
    }
}
