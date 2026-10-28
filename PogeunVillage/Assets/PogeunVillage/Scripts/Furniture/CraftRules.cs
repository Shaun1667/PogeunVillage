using System;

namespace PogeunVillage
{
    public enum CraftResult
    {
        Made,
        Locked,         // 마을 평가 별이 모자람
        NotEnoughWood,
        Full,           // 인벤토리에 자리가 없음(목재는 그대로)
    }

    /// <summary>
    /// 작업대 제작 규칙(엔진 상태 없음, 결정 2026-10-28 · 3-4b): 목재가 드는 가구만 제작법, 마을 평가 별이 닿으면 열림.
    /// 만들면 목재를 빼고 가구 1개를 인벤토리에. 못 만들면 인벤토리는 그대로. 수치는 BALANCE.md 16번.
    /// </summary>
    public static class CraftRules
    {
        /// <summary>목재가 드는 가구만 작업대에서 만듦(0이면 구매만)</summary>
        public static bool IsRecipe(int woodCost) => woodCost > 0;

        public static bool IsUnlocked(int unlockStars, int stars) => stars >= unlockStars;

        public static CraftResult Craft(Inventory inventory, string woodId, int woodMaxStack, int woodCost,
            string furnitureId, int furnitureMaxStack, int unlockStars, int stars)
        {
            if (!IsRecipe(woodCost)) throw new ArgumentOutOfRangeException(nameof(woodCost), "목재가 들지 않는 가구는 만들 수 없습니다.");
            if (!IsUnlocked(unlockStars, stars))
                return CraftResult.Locked;
            if (!inventory.TryRemove(woodId, woodCost))
                return CraftResult.NotEnoughWood;
            if (inventory.TryAdd(furnitureId, furnitureMaxStack))
                return CraftResult.Made;
            inventory.TryAdd(woodId, woodMaxStack, woodCost);  // 자리가 없으면 뺀 목재를 그대로 돌려놓음(방금 뺀 칸이라 들어감)
            return CraftResult.Full;
        }
    }
}
