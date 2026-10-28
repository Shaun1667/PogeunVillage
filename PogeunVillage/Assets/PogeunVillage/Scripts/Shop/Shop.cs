using System;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 상점(광장 서쪽 노점). 매대(ShopStand) · 판매함(SellBox)이 영업 시간과 물건 값을 여기서 물어봅니다.
    /// 상점 주인 NPC(그레이박스)에게 F: 영업 중이면 가구 목록 창(3-4c), 닫았으면 영업 시간 알림. 이름 · 종족 · 모델은 나중에.
    /// </summary>
    public class Shop : MonoBehaviour
    {
        public ShopData data;
        public DayCycle dayCycle;
        [Tooltip("상점 주인(그레이박스). F → 가구 목록 창(영업 중) · 영업 시간 알림")]
        public Interactable keeper;
        [Tooltip("가구 판매(3-4c)")]
        public FurnitureCounter furnitureCounter;

        public bool IsOpen => dayCycle && dayCycle.Clock != null &&
                              ShopRules.IsOpen(dayCycle.Clock.HourOfDay, data.openHour, data.closeHour);

        void OnEnable()
        {
            if (keeper) keeper.Interacted += Greet;
        }

        void OnDisable()
        {
            if (keeper) keeper.Interacted -= Greet;
        }

        void Greet(Interactable _, PlayerInteractor by)
        {
            if (IsOpen && furnitureCounter)
                furnitureCounter.Open();
            else
                GameNotice.Post(IsOpen
                    ? $"[상점] 어서 오세요! 매대 앞에서 F로 사고, 판매함에 F로 팔 수 있어요 ({data.openHour}시 ~ {data.closeHour}시)"
                    : $"[상점] 지금은 문을 닫았어요 ({data.openHour}시 ~ {data.closeHour}시)", this);
        }

        public int SellPriceOf(string itemId)
        {
            var item = Array.Find(data.catalog, i => i && i.id == itemId);
            return item ? item.sellPrice : 0;
        }

        public string NameOf(string itemId)
        {
            var item = Array.Find(data.catalog, i => i && i.id == itemId);
            return item ? item.displayName : itemId;
        }
    }
}
