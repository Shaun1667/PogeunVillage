using System;
using System.Linq;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 상점 가구 판매(결정 2026-10-28, 3-4c): 영업 중 상점 주인 F → 가구 목록 창(이름 · 값, 안 열린 건 "★2에 열려요"), F = 1개 사기.
    /// 마을 평가 별(VillageRater, 아침 기준)이 처음 오른 아침에 새로 열린 가구 · 제작법을 알림. 되팔기는 판매함(절반).
    /// 규칙은 ShopRules, 값은 가구 데이터(BALANCE.md 16번).
    /// </summary>
    public class FurnitureCounter : MonoBehaviour, IListSource
    {
        public Shop shop;
        [Tooltip("파는 가구(창에 이 차례로)")]
        public FurnitureDefinition[] furniture = { };
        public PlayerInventory inventory;
        public PlayerWallet wallet;
        public VillageRater rater;
        public ListWindow window;

        public int Stars => rater ? Math.Max(1, rater.Current.stars) : 1;
        /// <summary>지금까지 닿은 가장 높은 별(새로 열림 알림용)</summary>
        public int BestStars { get; private set; }

        public string Title => "가구 사기";
        public string Hint => "F 사기 · Tab 닫기";
        public int Count => furniture.Length;
        public string Footer => $"가진 돈 {HudText.Moa(wallet.Wallet.Moa)}";

        public string Line(int index)
        {
            var f = furniture[index];
            return HudText.Unlockable(f.displayName, HudText.Moa(f.buyPrice), f.unlockStars, Stars);
        }

        public bool Usable(int index) => FurnitureRules.IsUnlocked(furniture[index].unlockStars, Stars) && wallet.Wallet.Moa >= furniture[index].buyPrice;

        public void Choose(int index) => Buy(furniture[index]);

        void Start()
        {
            if (!shop || !inventory || !wallet || !rater || !window || furniture.Length == 0)
            {
                Debug.LogError($"{name}: 상점 가구 연결이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            BestStars = rater.Current.stars;  // 평가 전이면 0 — 첫 평가는 알리지 않음
            rater.Changed += OnRated;
        }

        void OnDestroy()
        {
            if (rater) rater.Changed -= OnRated;
        }

        /// <summary>상점 주인 F(영업 중)</summary>
        public void Open()
        {
            if (enabled && !window.IsOpen)
                window.Open(this);
        }

        public BuyResult? Buy(FurnitureDefinition item)
        {
            if (!shop.IsOpen)
            {
                GameNotice.Post($"[상점] 지금은 문을 닫았어요 ({shop.data.openHour}시 ~ {shop.data.closeHour}시)", this);
                return null;
            }
            var result = ShopRules.BuyFurniture(inventory.Inventory, wallet.Wallet, item.id, item.maxStack, item.buyPrice, item.unlockStars, Stars);
            switch (result)
            {
                case BuyResult.Bought:
                    GameNotice.Post($"[상점] {item.displayName}을(를) {item.buyPrice} 모아에 샀어요", this);
                    break;
                case BuyResult.Locked:
                    GameNotice.Post($"[상점] {item.displayName}은(는) 마을 평가 ★{item.unlockStars}에 들어와요", this);
                    break;
                case BuyResult.Full:
                    GameNotice.Post($"[상점] 인벤토리가 가득 찼어요: {item.displayName}", this);
                    break;
                case BuyResult.NotEnoughMoa:
                    GameNotice.Post($"[상점] 모아가 모자라요: {item.displayName} {item.buyPrice} 모아 (가진 모아 {wallet.Wallet.Moa})", this);
                    break;
            }
            return result;
        }

        void OnRated(RatingResult rating)
        {
            if (BestStars == 0)
            {
                BestStars = rating.stars;
                return;
            }
            var opened = ShopRules.NewlyUnlocked(furniture.Select(f => f.unlockStars).ToArray(), BestStars, rating.stars);
            BestStars = Math.Max(BestStars, rating.stars);
            if (opened.Count == 0)
                return;
            string names = string.Join(" · ", opened.Select(i => furniture[i].displayName));
            bool recipes = opened.Any(i => CraftRules.IsRecipe(furniture[i].woodCost));
            GameNotice.Post($"[상점] 새 가구가 들어왔어요: {names}" + (recipes ? " (작업대 제작법도 열렸어요)" : ""), this);
        }
    }
}
