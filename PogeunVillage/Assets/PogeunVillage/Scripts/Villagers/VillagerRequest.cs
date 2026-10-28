using System;
using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>
    /// 주민 부탁 하나(엔진 상태 없음). 결정 (2026-10-28, 3-3): 매일 아침 마을 주민 1명이 '물건 N개 가져다줘' 부탁, 그날만 유효.
    /// 들어주면 친밀도 +3(하루 상한과 별개) + 모아(팔 때 값 × 개수 × 2).
    /// </summary>
    public sealed class VillagerRequest
    {
        public VillagerRequest(string villagerId, string itemId, int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            VillagerId = villagerId;
            ItemId = itemId;
            Count = count;
        }

        public string VillagerId { get; }
        public string ItemId { get; }
        public int Count { get; }
        public bool Done { get; private set; }

        /// <summary>가진 개수로 들어줄 수 있는지</summary>
        public bool CanFulfill(int have) => !Done && have >= Count;

        public void Complete() => Done = true;

        /// <summary>
        /// 오늘의 부탁: 후보 주민(이사 준비 중 제외는 부르는 쪽) 중 1명, 물건 목록 중 1가지, 개수 min ~ max. 후보나 물건이 없으면 null.
        /// 같은 난수 = 같은 부탁.
        /// </summary>
        public static VillagerRequest Pick(IReadOnlyList<string> candidates, IReadOnlyList<string> itemIds, int minCount, int maxCount, IRandomSource random)
        {
            if (candidates.Count == 0 || itemIds.Count == 0)
                return null;
            string villager = candidates[random.Next(candidates.Count)];
            string item = itemIds[random.Next(itemIds.Count)];
            int lo = Math.Max(1, minCount), hi = Math.Max(lo, maxCount);
            int count = lo + random.Next(hi - lo + 1);
            return new VillagerRequest(villager, item, count);
        }

        /// <summary>보상 모아: 팔 때 값 × 개수 × 배수</summary>
        public static int MoaReward(int sellPrice, int count, int multiplier) => Math.Max(0, sellPrice) * count * multiplier;
    }
}
