using System;
using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>
    /// 임시 주머니: 물건 id별 개수만 센다(엔진 상태 없음).
    /// 결정 (2026-10-27): 1-4에서 슬롯 · 겹치기 수량이 있는 인벤토리로 바꾼다.
    /// </summary>
    public class Pouch
    {
        readonly Dictionary<string, int> counts = new();

        /// <summary>바뀐 물건 id와 바뀐 뒤 개수</summary>
        public event Action<string, int> Changed;

        public int Get(string itemId) => counts.TryGetValue(itemId, out int count) ? count : 0;

        public void Add(string itemId, int amount = 1)
        {
            if (string.IsNullOrEmpty(itemId))
                throw new ArgumentException("물건 id가 비어 있습니다.", nameof(itemId));
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            int count = Get(itemId) + amount;
            counts[itemId] = count;
            Changed?.Invoke(itemId, count);
        }
    }
}
