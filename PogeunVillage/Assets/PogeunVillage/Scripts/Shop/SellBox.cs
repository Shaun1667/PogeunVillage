using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 판매함: 앞에서 F → 인벤토리 앞쪽 칸부터 팔 수 있는 물건 칸 하나를 통째로 팔고 모아를 받음(결정 2026-10-27).
    /// 도구는 못 팖. 문을 닫았으면 아무 일 없음.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class SellBox : MonoBehaviour
    {
        public Shop shop;

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

            int slot = ShopRules.ChooseSellSlot(inventory.Inventory.Slots, shop.SellPriceOf);
            if (slot < 0)
            {
                Debug.Log("[상점] 팔 수 있는 물건이 없어요", this);
                return;
            }

            var taken = inventory.Inventory.TakeAll(slot);
            int earned = shop.SellPriceOf(taken.itemId) * taken.count;
            wallet.Wallet.Earn(earned);
            Debug.Log($"[상점] {shop.NameOf(taken.itemId)} {taken.count}개를 {earned} 모아에 팔았어요", this);
        }
    }
}
