using System;
using System.Linq;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 상점 가구 판매(결정 2026-10-28, 3-4c): 영업 중 상점 주인 F → 가구 목록 창(이름 · 값, 안 열린 건 "★2에 열려요"), F = 1개 사기.
    /// 마을 평가 별(VillageRater, 아침 기준)이 처음 오른 아침에 새로 열린 가구 · 제작법을 알림. 되팔기는 판매함(절반).
    /// 오늘의 테마 가구(3-4d): 게임 시작과 매일 아침 마을 주민 1명의 테마 가구 1가지를 목록 맨 위에(별 제한 없음).
    /// 규칙은 ShopRules, 값은 가구 데이터(BALANCE.md 16번).
    /// </summary>
    [DefaultExecutionOrder(110)]  // Village.Start(주민) · VillageRater.Start(첫 평가) 뒤
    public class FurnitureCounter : MonoBehaviour, IListSource
    {
        public Shop shop;
        [Tooltip("파는 가구(창에 이 차례로)")]
        public FurnitureDefinition[] furniture = { };
        public PlayerInventory inventory;
        public PlayerWallet wallet;
        public VillageRater rater;
        public ListWindow window;
        [Tooltip("오늘의 테마 가구를 고를 마을 주민(3-4d)")]
        public Village village;
        public MorningRunner morning;

        public int Stars => rater ? Math.Max(1, rater.Current.stars) : 1;
        /// <summary>지금까지 닿은 가장 높은 별(새로 열림 알림용)</summary>
        public int BestStars { get; private set; }
        /// <summary>오늘의 테마 가구(마을 주민이 없으면 null)</summary>
        public FurnitureDefinition TodayTheme { get; private set; }

        public string Title => "가구 사기";
        public string Hint => "F 사기 · Tab 닫기";
        public int Count => furniture.Length + (TodayTheme ? 1 : 0);
        public string Footer => $"가진 돈 {HudText.Moa(wallet.Wallet.Moa)}";

        /// <summary>목록 번호 → 가구(오늘의 테마가 있으면 맨 위)</summary>
        public FurnitureDefinition ItemAt(int index) => TodayTheme ? (index == 0 ? TodayTheme : furniture[index - 1]) : furniture[index];

        public string Line(int index)
        {
            var f = ItemAt(index);
            string line = HudText.Unlockable(f.displayName, HudText.Moa(f.buyPrice), f.unlockStars, Stars);
            return f == TodayTheme ? $"오늘의 테마 · {line}" : line;
        }

        public bool Usable(int index) => FurnitureRules.IsUnlocked(ItemAt(index).unlockStars, Stars) && wallet.Wallet.Moa >= ItemAt(index).buyPrice;

        public void Choose(int index) => Buy(ItemAt(index));

        IRandomSource themeRandom;

        void Start()
        {
            if (!shop || !inventory || !wallet || !rater || !window || !village || !morning || furniture.Length == 0)
            {
                Debug.LogError($"{name}: 상점 가구 연결이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            BestStars = rater.Current.stars;  // 평가 전이면 0 — 첫 평가는 알리지 않음
            rater.Changed += OnRated;
            themeRandom = new SystemRandomSource(shop.data.themeSeed);
            morning.MorningPassed += PickTodayTheme;
            PickTodayTheme();
        }

        void OnDestroy()
        {
            if (rater) rater.Changed -= OnRated;
            if (morning) morning.MorningPassed -= PickTodayTheme;
        }

        /// <summary>오늘의 테마 가구: 마을 주민 중 무작위 1명 · 그 주민 테마 가구 중 무작위 1가지(3-4d)</summary>
        public void PickTodayTheme()
        {
            var residents = new System.Collections.Generic.List<VillagerDefinition>();
            if (village.Residents != null)
                foreach (string id in village.Residents.Plots)
                {
                    var v = id == null ? null : Array.Find(village.rules.villagers, d => d && d.id == id);
                    if (v && v.themeFurniture.Length > 0) residents.Add(v);
                }
            var (resident, piece) = ShopRules.PickTodayTheme(residents.Count, FurnitureRules.ThemePieces, themeRandom);
            TodayTheme = resident < 0 ? null : residents[resident].themeFurniture[Math.Min(piece, residents[resident].themeFurniture.Length - 1)];
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
