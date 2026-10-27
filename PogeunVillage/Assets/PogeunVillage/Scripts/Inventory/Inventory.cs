using System;
using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>인벤토리 한 칸. 비어 있으면 itemId가 null, count 0.</summary>
    public readonly struct InventorySlot
    {
        public readonly string itemId;
        public readonly int count;

        public InventorySlot(string itemId, int count)
        {
            this.itemId = itemId;
            this.count = count;
        }

        public bool IsEmpty => count == 0;
    }

    /// <summary>
    /// 칸 인벤토리 규칙(엔진 상태 없음). 수치는 BALANCE.md 10번 · InventoryRules · ItemDefinition.maxStack.
    /// - 넣기: 같은 물건이 있는 칸(앞쪽부터)을 겹침 최대까지 채우고, 남으면 앞쪽 빈 칸부터
    /// - 다 들어가지 않으면 아무것도 넣지 않음(가득 참 → 물건은 땅에 그대로)
    /// - 바뀐 칸만 SlotChanged로 알림
    /// </summary>
    public class Inventory
    {
        readonly InventorySlot[] slots;

        /// <summary>바뀐 칸의 번호</summary>
        public event Action<int> SlotChanged;

        public Inventory(int slotCount)
        {
            if (slotCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(slotCount));
            slots = new InventorySlot[slotCount];
        }

        public IReadOnlyList<InventorySlot> Slots => slots;

        public int UsedSlots
        {
            get
            {
                int used = 0;
                foreach (var slot in slots)
                    if (!slot.IsEmpty) used++;
                return used;
            }
        }

        public int Count(string itemId)
        {
            int total = 0;
            foreach (var slot in slots)
                if (slot.itemId == itemId) total += slot.count;
            return total;
        }

        /// <summary>지금 더 넣을 수 있는 개수</summary>
        public int SpaceFor(string itemId, int maxStack)
        {
            Validate(itemId, maxStack);
            int space = 0;
            foreach (var slot in slots)
            {
                if (slot.IsEmpty) space += maxStack;
                else if (slot.itemId == itemId) space += Math.Max(0, maxStack - slot.count);
            }
            return space;
        }

        /// <returns>다 넣었으면 true, 자리가 모자라면 아무것도 넣지 않고 false</returns>
        public bool TryAdd(string itemId, int maxStack, int amount = 1)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (SpaceFor(itemId, maxStack) < amount)
                return false;

            for (int i = 0; i < slots.Length && amount > 0; i++)
                if (slots[i].itemId == itemId && slots[i].count < maxStack)
                    amount = Put(i, itemId, maxStack, amount);
            for (int i = 0; i < slots.Length && amount > 0; i++)
                if (slots[i].IsEmpty)
                    amount = Put(i, itemId, maxStack, amount);
            return true;
        }

        int Put(int index, string itemId, int maxStack, int amount)
        {
            int added = Math.Min(maxStack - slots[index].count, amount);
            slots[index] = new InventorySlot(itemId, slots[index].count + added);
            SlotChanged?.Invoke(index);
            return amount - added;
        }

        static void Validate(string itemId, int maxStack)
        {
            if (string.IsNullOrEmpty(itemId))
                throw new ArgumentException("물건 id가 비어 있습니다.", nameof(itemId));
            if (maxStack <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxStack));
        }
    }
}
