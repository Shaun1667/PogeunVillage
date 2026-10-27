using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 매대: 앞에서 F → 물건 1개를 삼(buyPrice). 문을 닫았거나 · 자리가 없거나 · 모아가 모자라면 아무것도 안 바뀜.
    /// 도구는 칸 하나에 새 내구도로, 그 밖의 물건은 겹쳐서 들어감.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class ShopStand : MonoBehaviour
    {
        public Shop shop;
        public ItemDefinition item;

        Interactable interactable;

        void Awake() => interactable = GetComponent<Interactable>();
        void OnEnable() => interactable.Interacted += OnInteracted;
        void OnDisable() => interactable.Interacted -= OnInteracted;

        void OnInteracted(Interactable _, PlayerInteractor by)
        {
            var inventory = by.GetComponent<PlayerInventory>();
            var wallet = by.GetComponent<PlayerWallet>();
            if (!inventory || !wallet || inventory.Inventory == null || wallet.Wallet == null)
                return;

            if (!shop.IsOpen)
            {
                Debug.Log($"[상점] 지금은 문을 닫았어요 ({shop.data.openHour}시 ~ {shop.data.closeHour}시)", this);
                return;
            }

            bool isTool = item is ToolDefinition;
            bool hasRoom = isTool
                ? inventory.Inventory.UsedSlots < inventory.Inventory.Slots.Count
                : inventory.Inventory.SpaceFor(item.id, item.maxStack) >= 1;
            if (!hasRoom)
            {
                Debug.Log($"[상점] 인벤토리가 가득 찼어요: {item.displayName}", this);
                return;
            }
            if (!wallet.Wallet.TrySpend(item.buyPrice))
            {
                Debug.Log($"[상점] 모아가 모자라요: {item.displayName} {item.buyPrice} 모아 (가진 모아 {wallet.Wallet.Moa})", this);
                return;
            }

            if (item is ToolDefinition tool) inventory.TryAddTool(tool);
            else inventory.TryAdd(item);
            Debug.Log($"[상점] {item.displayName}을(를) {item.buyPrice} 모아에 샀어요", this);
        }
    }
}
