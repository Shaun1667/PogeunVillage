using System;
using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>
    /// 상점 규칙(엔진 상태 없음). 수치는 BALANCE.md 7번 · ShopData. 결정 (2026-10-27):
    /// - 영업 시간 openHour ≤ 시각 &lt; closeHour (8시 ~ 22시)
    /// - 판매함 = 인벤토리 앞쪽 칸부터 판매가가 있는 물건 칸 하나를 통째로(도구는 판매가 0 → 못 팖)
    /// </summary>
    public enum BuyResult
    {
        Bought,
        Locked,         // 마을 평가 별이 모자람
        Full,
        NotEnoughMoa,
    }

    public static class ShopRules
    {
        /// <summary>
        /// 상점 가구 1개 사기(3-4c): 별이 닿았고 · 자리가 있고 · 모아가 있으면 모아를 내고 인벤토리에. 못 사면 아무것도 안 바뀜.
        /// </summary>
        public static BuyResult BuyFurniture(Inventory inventory, Wallet wallet, string itemId, int maxStack, int price, int unlockStars, int stars)
        {
            if (!FurnitureRules.IsUnlocked(unlockStars, stars))
                return BuyResult.Locked;
            if (inventory.SpaceFor(itemId, maxStack) < 1)
                return BuyResult.Full;
            if (!wallet.TrySpend(price))
                return BuyResult.NotEnoughMoa;
            inventory.TryAdd(itemId, maxStack);
            return BuyResult.Bought;
        }

        /// <summary>
        /// 오늘의 테마 가구(3-4d): 마을 주민 중 무작위 1명 · 그 주민 테마 가구 중 무작위 1가지. 주민이 없으면 (-1, -1)
        /// </summary>
        public static (int resident, int piece) PickTodayTheme(int residentCount, int piecesPerResident, IRandomSource random)
        {
            if (residentCount <= 0 || piecesPerResident <= 0)
                return (-1, -1);
            int resident = random.Next(residentCount);
            return (resident, random.Next(piecesPerResident));
        }

        /// <summary>최고 별이 before → after로 오를 때 새로 열리는 것(번호). 처음 닿을 때만(after ≤ before면 없음)</summary>
        public static List<int> NewlyUnlocked(IReadOnlyList<int> unlockStars, int before, int after)
        {
            var result = new List<int>();
            for (int i = 0; i < unlockStars.Count; i++)
                if (unlockStars[i] > before && unlockStars[i] <= after)
                    result.Add(i);
            return result;
        }

        public static bool IsOpen(int hourOfDay, int openHour, int closeHour)
        {
            return openHour <= closeHour
                ? hourOfDay >= openHour && hourOfDay < closeHour
                : hourOfDay >= openHour || hourOfDay < closeHour;  // 자정을 넘기는 영업
        }

        /// <returns>팔 칸, 없으면 -1</returns>
        public static int ChooseSellSlot(IReadOnlyList<InventorySlot> slots, Func<string, int> sellPriceOf)
        {
            for (int i = 0; i < slots.Count; i++)
                if (!slots[i].IsEmpty && sellPriceOf(slots[i].itemId) > 0)
                    return i;
            return -1;
        }
    }
}
