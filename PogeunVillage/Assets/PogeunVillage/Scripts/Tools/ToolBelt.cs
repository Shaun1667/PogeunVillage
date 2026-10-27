using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>
    /// 들 도구 고르기 규칙(엔진 상태 없음). 결정 (2026-10-27): 숫자 1~5 = 잠자리채 · 낚싯대 · 도끼 · 삽 · 물뿌리개,
    /// 패드 십자키 좌우 = [빈손, 가진 도구(키 순서)]를 돌아가며.
    /// 같은 도구가 여러 개면 인벤토리 앞쪽 칸의 것을 듦.
    /// </summary>
    public static class ToolBelt
    {
        /// <returns>그 도구가 든 첫 칸, 없으면 -1</returns>
        public static int FindSlot(IReadOnlyList<InventorySlot> slots, string toolId)
        {
            for (int i = 0; i < slots.Count; i++)
                if (slots[i].itemId == toolId)
                    return i;
            return -1;
        }

        /// <summary>숫자키를 눌렀을 때 들 칸. 이미 그 도구를 들고 있으면 내려놓음(-1). 없는 도구면 지금 그대로.</summary>
        public static int Select(IReadOnlyList<InventorySlot> slots, string toolId, int equippedSlot)
        {
            int slot = FindSlot(slots, toolId);
            if (slot < 0)
                return equippedSlot;
            return slot == equippedSlot ? -1 : slot;
        }

        /// <summary>direction(+1/-1)으로 [빈손, 가진 도구들] 중 다음 것.</summary>
        public static int Cycle(IReadOnlyList<InventorySlot> slots, IReadOnlyList<string> toolIdsInKeyOrder, int equippedSlot, int direction)
        {
            var order = new List<int> { -1 };
            foreach (string id in toolIdsInKeyOrder)
            {
                int slot = FindSlot(slots, id);
                if (slot >= 0)
                    order.Add(slot);
            }

            int current = order.IndexOf(equippedSlot);
            if (current < 0)
                current = 0;
            int next = ((current + direction) % order.Count + order.Count) % order.Count;
            return order[next];
        }
    }
}
