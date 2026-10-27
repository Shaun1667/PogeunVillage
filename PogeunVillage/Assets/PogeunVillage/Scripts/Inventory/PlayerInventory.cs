using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 플레이어 인벤토리(Inventory) 어댑터. 지금은 넣기 결과를 콘솔에 알리고, 화면 표시는 2-3 HUD에서 SlotChanged를 구독합니다.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        public InventoryRules rules;

        public Inventory Inventory { get; private set; }

        void Awake()
        {
            if (!rules)
            {
                Debug.LogError($"{name}: InventoryRules가 연결되지 않았습니다.", this);
                enabled = false;
                return;
            }
            Inventory = new Inventory(rules.slotCount);
        }

        /// <returns>넣었으면 true, 자리가 없으면 false(물건은 그대로 둠)</returns>
        public bool TryAdd(ItemDefinition item, int amount = 1)
        {
            if (Inventory == null)
                return false;
            if (!Inventory.TryAdd(item.id, item.maxStack, amount))
            {
                Debug.Log($"[인벤토리] 가득 찼어요: {item.displayName}", this);
                return false;
            }
            Debug.Log($"[인벤토리] {item.displayName} +{amount} → {Inventory.Count(item.id)}개 (칸 {Inventory.UsedSlots}/{Inventory.Slots.Count})", this);
            return true;
        }
    }
}
