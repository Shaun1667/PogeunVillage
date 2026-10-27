using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>삽을 휘둘렀을 때 할 일</summary>
    public enum ShovelAction
    {
        None,
        DigHole,
        FillHole,
        LiftFlower,
        RemoveStump,
    }

    /// <summary>
    /// 삽 · 심기 규칙(엔진 상태 없음). 결정 (2026-10-27):
    /// - 앞에 밑동이 있으면 파냄(그 칸은 빈 땅이 됨)
    /// - 아니면 앞 칸이: 빈 땅 → 구멍 파기 / 구멍 → 메우기 / 꽃 → 통째 회수('꽃 포기') / 막힌 칸 · 묘목 → 아무것도
    /// - 구멍 앞 F = 인벤토리 앞쪽 칸부터 심을 수 있는 물건 1개를 심음
    /// </summary>
    public static class ShovelRules
    {
        public static ShovelAction Decide(bool stumpInFront, bool cellBlocked, GroundObjectKind cellContent)
        {
            if (stumpInFront)
                return ShovelAction.RemoveStump;
            if (cellBlocked)
                return ShovelAction.None;
            return cellContent switch
            {
                GroundObjectKind.None => ShovelAction.DigHole,
                GroundObjectKind.Hole => ShovelAction.FillHole,
                GroundObjectKind.Flower => ShovelAction.LiftFlower,
                _ => ShovelAction.None,
            };
        }

        /// <returns>심을 물건이 든 첫 칸, 없으면 -1</returns>
        public static int ChoosePlantSlot(IReadOnlyList<InventorySlot> slots, ICollection<string> plantableIds)
        {
            for (int i = 0; i < slots.Count; i++)
                if (!slots[i].IsEmpty && plantableIds.Contains(slots[i].itemId))
                    return i;
            return -1;
        }
    }
}
