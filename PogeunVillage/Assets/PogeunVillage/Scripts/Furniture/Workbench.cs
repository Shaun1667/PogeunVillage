using System;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 광장 작업대 어댑터(결정 2026-10-28, 3-4b): F = 제작 창. 제작법은 목재 가구, 열림은 마을 평가 별(VillageRater, 아침 기준).
    /// 규칙은 CraftRules, 결과는 알림으로.
    /// </summary>
    public class Workbench : MonoBehaviour
    {
        [Tooltip("제작법(목재가 드는 가구, 창에 이 차례로)")]
        public FurnitureDefinition[] recipes = { };
        public ItemDefinition wood;
        public PlayerInventory inventory;
        public VillageRater rater;
        public CraftWindow window;

        /// <summary>지금 마을 평가 별(평가 전이면 1)</summary>
        public int Stars => rater ? Math.Max(1, rater.Current.stars) : 1;
        public int Wood => inventory.Inventory.Count(wood.id);

        Interactable interactable;

        void Start()
        {
            if (!wood || !inventory || !rater || !window || recipes.Length == 0)
            {
                Debug.LogError($"{name}: 작업대 연결이 비어 있습니다.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            interactable = GetComponent<Interactable>();
            if (interactable) interactable.Interacted += Open;
        }

        void OnDisable()
        {
            if (interactable) interactable.Interacted -= Open;
        }

        void Open(Interactable _, PlayerInteractor by)
        {
            if (enabled && !window.IsOpen)
                window.Open(this);
        }

        public CraftResult Craft(FurnitureDefinition furniture)
        {
            int have = Wood;
            var result = CraftRules.Craft(inventory.Inventory, wood.id, wood.maxStack, furniture.woodCost,
                furniture.id, furniture.maxStack, furniture.unlockStars, Stars);
            switch (result)
            {
                case CraftResult.Made:
                    GameNotice.Post($"[제작] {furniture.displayName}을(를) 만들었어요 (목재 {Wood}개 남음)", this);
                    break;
                case CraftResult.Locked:
                    GameNotice.Post($"[제작] {furniture.displayName}은(는) 마을 평가 ★{furniture.unlockStars}에 열려요", this);
                    break;
                case CraftResult.NotEnoughWood:
                    GameNotice.Post($"[제작] 목재가 모자라요 ({furniture.woodCost}개 필요, {have}개 있음)", this);
                    break;
                case CraftResult.Full:
                    GameNotice.Post($"[인벤토리] 가득 찼어요: {furniture.displayName}", this);
                    break;
            }
            return result;
        }
    }
}
