using System;

namespace PogeunVillage
{
    /// <summary>
    /// 열매 나무 한 그루의 열매 개수 규칙(엔진 상태 없음). 수치는 BALANCE.md 6번 · GatheringRules.
    /// - 흔들면 달린 열매가 전부 떨어진다
    /// - 아침마다 perDay개씩 열리되 max개를 넘지 않는다 (2-1 하루 루프에서 Refill 호출)
    /// </summary>
    public class FruitTree
    {
        public int Count { get; private set; }

        public FruitTree(int initialCount)
        {
            if (initialCount < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCount));
            Count = initialCount;
        }

        /// <returns>떨어진 열매 수</returns>
        public int Shake()
        {
            int dropped = Count;
            Count = 0;
            return dropped;
        }

        public void Refill(int perDay, int max)
        {
            Count = Math.Min(max, Count + perDay);
        }
    }
}
