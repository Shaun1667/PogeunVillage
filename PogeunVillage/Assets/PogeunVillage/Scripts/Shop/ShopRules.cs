using System;
using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>
    /// 상점 규칙(엔진 상태 없음). 수치는 BALANCE.md 7번 · ShopData. 결정 (2026-10-27):
    /// - 영업 시간 openHour ≤ 시각 &lt; closeHour (8시 ~ 22시)
    /// - 판매함 = 인벤토리 앞쪽 칸부터 판매가가 있는 물건 칸 하나를 통째로(도구는 판매가 0 → 못 팖)
    /// </summary>
    public static class ShopRules
    {
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
