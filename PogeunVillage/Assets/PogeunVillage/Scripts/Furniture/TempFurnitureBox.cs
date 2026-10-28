using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 임시 가구 상자(결정 2026-10-28, 3-4a): F마다 기본 가구를 차례로 1개씩 인벤토리로. 3-4b 작업대 · 3-4c 상점이 생기면 없앰
    /// (1-5 임시 도구 상자와 같은 방식).
    /// </summary>
    public class TempFurnitureBox : MonoBehaviour
    {
        public FurnitureDefinition[] items = { };
        public PlayerInventory inventory;

        public int NextIndex { get; private set; }

        Interactable interactable;

        void OnEnable()
        {
            interactable = GetComponent<Interactable>();
            if (interactable) interactable.Interacted += Give;
        }

        void OnDisable()
        {
            if (interactable) interactable.Interacted -= Give;
        }

        void Give(Interactable _, PlayerInteractor by)
        {
            if (items.Length == 0 || !inventory)
                return;
            var item = items[NextIndex];
            if (!inventory.TryAdd(item))
                return;  // '가득 찼어요' 알림은 PlayerInventory가 띄움
            GameNotice.Post($"[가구] 임시 가구 상자에서 {item.displayName}을(를) 꺼냈어요", this);
            NextIndex = FurnitureRules.NextInBox(NextIndex, items.Length);
        }
    }
}
