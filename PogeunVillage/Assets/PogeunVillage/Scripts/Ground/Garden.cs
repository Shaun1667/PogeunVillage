using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>아침 처리 결과. 칸은 모두 정렬된 순서(결정론).</summary>
    public class GardenMorning
    {
        /// <summary>다 자라 나무가 될 묘목 칸</summary>
        public readonly List<Vector2Int> grownSaplings = new();
        /// <summary>시든 채 물을 못 받아 사라질 꽃 칸</summary>
        public readonly List<Vector2Int> removedFlowers = new();
        /// <summary>물을 받아 살아난 꽃 칸</summary>
        public readonly List<Vector2Int> revivedFlowers = new();
        /// <summary>번식: (원래 꽃 칸, 새 꽃 칸)</summary>
        public readonly List<(Vector2Int from, Vector2Int to)> spreadFlowers = new();
        /// <summary>새로 시든 꽃 칸</summary>
        public readonly List<Vector2Int> wiltedFlowers = new();
    }

    /// <summary>
    /// 꽃 · 묘목의 하루 규칙(엔진 상태 없음). 수치는 BALANCE.md 8번 · GatheringRules. 결정 (2026-10-19, 2026-10-27):
    /// - 물 주기: 그날 꽃 · 묘목에 물을 줌(하루 1번만 인정, 여러 번 줘도 같음)
    /// - 아침 순서: 묘목 성장(+1, 물 준 날 +1 더, growDays 이상이면 다 자람) → 시든 채 물 못 받은 꽃 사라짐 →
    ///   물 받은 시든 꽃은 살아나기만 → 물 받은 성한 꽃은 상하좌우 빈 칸 중 무작위 1칸에 번식 →
    ///   물 못 받은 꽃(이번 아침에 새로 난 꽃 제외)은 wiltChance로 시듦 → 물 준 기록 초기화
    /// 칸에 무엇이 있는지는 GroundGrid가 기준이고, 여기서는 꽃 · 묘목의 상태만 기억합니다(그리드는 바꾸지 않음 — 결과를 어댑터가 적용).
    /// </summary>
    public class Garden
    {
        static readonly Vector2Int[] Cross = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        readonly GroundGrid grid;
        readonly IRandomSource random;
        readonly int growDays;
        readonly int waterBonus;
        readonly double wiltChance;

        readonly HashSet<Vector2Int> watered = new();
        readonly HashSet<Vector2Int> wilted = new();
        readonly Dictionary<Vector2Int, int> growth = new();

        public Garden(GroundGrid grid, IRandomSource random, int growDays, int waterBonus, double wiltChance)
        {
            this.grid = grid;
            this.random = random;
            this.growDays = growDays;
            this.waterBonus = waterBonus;
            this.wiltChance = wiltChance;
        }

        bool IsPlant(Vector2Int cell) => grid.Get(cell) is GroundObjectKind.Flower or GroundObjectKind.Sapling;

        /// <summary>불러오기(6-1b): 아침 기준이라 물 준 칸은 없음 — 시든 꽃 · 묘목 자란 날만</summary>
        public void Restore(IEnumerable<Vector2Int> wiltedCells, IEnumerable<KeyValuePair<Vector2Int, int>> saplingGrowth)
        {
            watered.Clear();
            wilted.Clear();
            growth.Clear();
            foreach (var c in wiltedCells) wilted.Add(c);
            foreach (var g in saplingGrowth) growth[g.Key] = g.Value;
        }

        /// <returns>꽃 · 묘목이 있어 물을 줬으면 true</returns>
        public bool Water(Vector2Int cell)
        {
            if (!IsPlant(cell))
                return false;
            watered.Add(cell);
            return true;
        }

        public bool IsWatered(Vector2Int cell) => watered.Contains(cell) && IsPlant(cell);
        public bool IsWilted(Vector2Int cell) => wilted.Contains(cell) && grid.Get(cell) == GroundObjectKind.Flower;
        public int Growth(Vector2Int cell) => grid.Get(cell) == GroundObjectKind.Sapling && growth.TryGetValue(cell, out int g) ? g : 0;

        /// <summary>묘목 모습 단계 0 ~ stages-1 (결정 2026-10-27: 3단계)</summary>
        public int SaplingStage(Vector2Int cell, int stages = 3) =>
            Mathf.Clamp(Growth(cell) * stages / growDays, 0, stages - 1);

        public GardenMorning Morning(IEnumerable<Vector2Int> flowerCells, IEnumerable<Vector2Int> saplingCells)
        {
            var result = new GardenMorning();
            var flowers = flowerCells.OrderBy(c => c.y).ThenBy(c => c.x).ToList();
            var saplings = saplingCells.OrderBy(c => c.y).ThenBy(c => c.x).ToList();

            // 지난 상태 중 이제 없는 칸은 버림(꺾임 · 회수 · 파냄)
            wilted.RemoveWhere(c => grid.Get(c) != GroundObjectKind.Flower);
            foreach (var stale in growth.Keys.Where(c => grid.Get(c) != GroundObjectKind.Sapling).ToList())
                growth.Remove(stale);

            // 1. 묘목 성장
            foreach (var cell in saplings)
            {
                int g = (growth.TryGetValue(cell, out int before) ? before : 0) + 1 + (watered.Contains(cell) ? waterBonus : 0);
                if (g >= growDays)
                {
                    growth.Remove(cell);
                    result.grownSaplings.Add(cell);
                }
                else
                    growth[cell] = g;
            }

            // 2. 시든 채 물 못 받은 꽃 사라짐 / 물 받은 시든 꽃은 살아나기만
            var freed = new HashSet<Vector2Int>();
            var healthyWatered = new List<Vector2Int>();
            foreach (var cell in flowers)
            {
                bool isWatered = watered.Contains(cell);
                if (wilted.Contains(cell))
                {
                    wilted.Remove(cell);
                    if (isWatered) result.revivedFlowers.Add(cell);
                    else { result.removedFlowers.Add(cell); freed.Add(cell); }
                }
                else if (isWatered)
                    healthyWatered.Add(cell);
            }

            // 3. 번식: 물 받은 성한 꽃 → 상하좌우 빈 칸 중 무작위 1칸
            var taken = new HashSet<Vector2Int>();
            foreach (var cell in healthyWatered)
            {
                var free = new List<Vector2Int>();
                foreach (var d in Cross)
                {
                    var n = cell + d;
                    if (taken.Contains(n) || grid.IsBlocked(n)) continue;
                    var content = grid.Get(n);
                    if (content == GroundObjectKind.None || (freed.Contains(n) && content == GroundObjectKind.Flower))
                        free.Add(n);
                }
                if (free.Count == 0) continue;
                var target = free[random.Next(free.Count)];
                taken.Add(target);
                result.spreadFlowers.Add((cell, target));
            }

            // 4. 시듦: 물 못 받은 성한 꽃(이번 아침에 사라지는 꽃 · 새로 난 꽃 제외)
            foreach (var cell in flowers)
            {
                if (watered.Contains(cell) || freed.Contains(cell) || result.revivedFlowers.Contains(cell))
                    continue;
                if (random.NextDouble() < wiltChance)
                {
                    wilted.Add(cell);
                    result.wiltedFlowers.Add(cell);
                }
            }

            watered.Clear();
            return result;
        }
    }
}
