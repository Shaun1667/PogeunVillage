using System;
using System.Collections.Generic;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 물고기 그림자 고르기(엔진 상태 없음, 결정 2026-10-29 · 3-6a): 지금 시간대에 나오는 물고기 중 희귀도 가중치로 한 종류,
    /// 강 칸 중 다른 그림자와 떨어진 칸 하나. 수치는 BALANCE.md 17번.
    /// </summary>
    public static class FishPicker
    {
        /// <param name="available">지금 시간대에 나오는지(물고기마다)</param>
        /// <param name="weights">희귀도별 가중치(Rarity 순서)</param>
        /// <returns>물고기 번호, 나올 물고기가 없으면 -1</returns>
        public static int PickFish(IReadOnlyList<Rarity> rarities, IReadOnlyList<bool> available, IReadOnlyList<int> weights, IRandomSource random)
        {
            int total = 0;
            for (int i = 0; i < rarities.Count; i++)
                if (available[i]) total += Weight(rarities[i], weights);
            if (total <= 0)
                return -1;
            int roll = random.Next(total);
            for (int i = 0; i < rarities.Count; i++)
            {
                if (!available[i]) continue;
                roll -= Weight(rarities[i], weights);
                if (roll < 0) return i;
            }
            return -1;
        }

        static int Weight(Rarity rarity, IReadOnlyList<int> weights)
        {
            int r = (int)rarity;
            return r < weights.Count ? Math.Max(0, weights[r]) : 0;
        }

        /// <summary>강 칸 중 무작위 하나. 이미 있는 그림자와 spacing칸 이상 떨어진 칸만(없으면 null)</summary>
        public static Vector2Int? PickWaterCell(IReadOnlyList<Vector2Int> waterCells, IReadOnlyCollection<Vector2Int> occupied, int spacing, IRandomSource random)
        {
            if (waterCells.Count == 0)
                return null;
            var free = new List<Vector2Int>();
            foreach (var c in waterCells)
            {
                bool near = false;
                foreach (var o in occupied)
                    if (Mathf.Max(Mathf.Abs(c.x - o.x), Mathf.Abs(c.y - o.y)) < spacing) { near = true; break; }
                if (!near) free.Add(c);
            }
            return free.Count == 0 ? null : free[random.Next(free.Count)];
        }
    }
}
