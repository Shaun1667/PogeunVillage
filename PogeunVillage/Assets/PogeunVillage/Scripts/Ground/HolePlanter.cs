using System;
using System.Linq;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 구멍 앞에서 F → 인벤토리 앞쪽 칸부터 심을 수 있는 물건(ItemDefinition.plantsAs) 1개를 심음(결정 2026-10-27).
    /// 구멍은 사라지고 그 칸에 꽃 · 묘목이 생깁니다. 심을 것이 없으면 아무 일 없음.
    /// </summary>
    [RequireComponent(typeof(Interactable), typeof(GridObject))]
    public class HolePlanter : MonoBehaviour
    {
        [Tooltip("심을 수 있는 물건들")]
        public ItemDefinition[] plantables = Array.Empty<ItemDefinition>();

        Interactable interactable;
        GridObject gridObject;

        void Awake()
        {
            interactable = GetComponent<Interactable>();
            gridObject = GetComponent<GridObject>();
        }

        void OnEnable() => interactable.Interacted += OnInteracted;
        void OnDisable() => interactable.Interacted -= OnInteracted;

        void OnInteracted(Interactable _, PlayerInteractor by)
        {
            var inventory = by.GetComponent<PlayerInventory>();
            if (!inventory || inventory.Inventory == null)
                return;

            var ids = plantables.Select(p => p.id).ToArray();
            int slot = ShovelRules.ChoosePlantSlot(inventory.Inventory.Slots, ids);
            if (slot < 0)
            {
                Debug.Log("[심기] 심을 것이 없어요(묘목 · 꽃 포기)", this);
                return;
            }

            string id = inventory.Inventory.Slots[slot].itemId;
            var item = Array.Find(plantables, p => p.id == id);
            Vector2Int cell = gridObject.Cell;
            var ground = gridObject.ground;

            inventory.Inventory.RemoveOne(slot);
            gridObject.Unregister();
            Destroy(gameObject);
            ground.Placer.Spawn(item.plantsAs, cell);
            Debug.Log($"[심기] {item.displayName}을(를) 심었어요 {cell}", this);
        }
    }
}
