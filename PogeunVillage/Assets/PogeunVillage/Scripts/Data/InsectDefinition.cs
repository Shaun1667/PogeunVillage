using System;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>곤충이 사는 자리(3-7a). enum은 끝에만 추가.</summary>
    public enum InsectSite
    {
        Flower,   // 꽃 위
        Tree,     // 마을 안쪽 나무 줄기
        Grass,    // 빈 풀밭 땅
        Water,    // 강가 물 위 공중
    }

    /// <summary>
    /// 곤충 한 종류(물건이기도 함 — 인벤토리 · 판매함 · 선물). 에셋: Assets/PogeunVillage/Data/Items/Insects/
    /// 결정 (2026-10-29, GDD 4-12): 자리 · 희귀도 · 나오는 시간대 · 예민함. 수치는 BALANCE.md 18번.
    /// </summary>
    [CreateAssetMenu(fileName = "Insect", menuName = "Pogeun Village/Insect")]
    public class InsectDefinition : ItemDefinition
    {
        [Header("곤충 (BALANCE.md 18번)")]
        public InsectSite site;
        public Rarity rarity;
        [Tooltip("나오는 시간대. 비우면 언제나")]
        public DayPeriod[] periods = { };
        [Tooltip("예민함: 달릴 때 더 멀리서 놀라고, 걸어도 너무 가까이 붙으면 도망")]
        public bool sensitive;
        [Tooltip("그레이박스 색(곤충 · 떨어진 곤충 · 채집통)")]
        public Color color = Color.gray;
        [Tooltip("채집통으로 놓았을 때 마을 평가 점수(3-7c)")]
        [Min(0)] public int cagePoint;
        [Tooltip("인벤토리에서 고르면 놓는 채집통(3-7c, 맵 생성 메뉴가 채움)")]
        public FurnitureDefinition cage;

        public bool AppearsIn(DayPeriod period) => periods.Length == 0 || Array.IndexOf(periods, period) >= 0;
    }
}
