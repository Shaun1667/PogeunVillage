using System.Globalization;

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

    /// <summary>
    /// 시드로 시작하는 난수(SplitMix64). 상태를 꺼내고 되돌릴 수 있어 세이브가 "저장 없이 진행한 것과 같은 결과"를 낼 수 있음(6-1c).
    /// 같은 시드 = 같은 수열.
    /// </summary>
    public class SeededRandom : IRandomSource
    {
        ulong state;

        public SeededRandom(int seed) => state = unchecked((ulong)seed);

        /// <summary>지금 상태(저장용 16진수 글자)</summary>
        public string State
        {
            get => state.ToString("x16", CultureInfo.InvariantCulture);
            set => state = ulong.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        ulong NextULong()
        {
            unchecked
            {
                state += 0x9E3779B97F4A7C15UL;
                ulong z = state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        public double NextDouble() => (NextULong() >> 11) * (1.0 / (1UL << 53));

        public int Next(int maxExclusive) => maxExclusive <= 0 ? 0 : (int)(NextDouble() * maxExclusive);
    }
}
