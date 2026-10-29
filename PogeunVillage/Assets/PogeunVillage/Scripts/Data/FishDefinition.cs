using System;
using UnityEngine;

namespace PogeunVillage
{
    public enum FishSize
    {
        Small,
        Medium,
        Large,
    }

    /// <summary>희귀도(물고기 · 곤충 함께 씀, 3-6a · 3-7a). 가중치는 각 규칙 데이터</summary>
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
    }

    /// <summary>
    /// 물고기 한 종류(물건이기도 함 — 인벤토리 · 판매함 · 선물). 에셋: Assets/PogeunVillage/Data/Items/Fish/
    /// 결정 (2026-10-29, GDD 4-11): 그림자 크기 · 희귀도 · 나오는 시간대. 수치는 BALANCE.md 17번.
    /// </summary>
    [CreateAssetMenu(fileName = "Fish", menuName = "Pogeun Village/Fish")]
    public class FishDefinition : ItemDefinition
    {
        [Header("물고기 (BALANCE.md 17번)")]
        [Tooltip("강에 보이는 그림자 크기(어떤 물고기일지 힌트)")]
        public FishSize size;
        public Rarity rarity;
        [Tooltip("나오는 시간대. 비우면 언제나")]
        public DayPeriod[] periods = { };
        [Tooltip("그레이박스 색(떨어진 물고기 · 수조)")]
        public Color color = Color.gray;
        [Tooltip("수조로 놓았을 때 마을 평가 점수(3-6c)")]
        [Min(0)] public int aquariumPoint;
        [Tooltip("인벤토리에서 고르면 놓는 수조(3-6c, 맵 생성 메뉴가 채움)")]
        public FurnitureDefinition aquarium;

        public bool AppearsIn(DayPeriod period) => periods.Length == 0 || Array.IndexOf(periods, period) >= 0;
    }
}
