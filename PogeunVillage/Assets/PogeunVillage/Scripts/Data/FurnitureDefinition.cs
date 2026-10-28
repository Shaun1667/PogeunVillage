using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 가구 한 종류(물건이기도 함 — 인벤토리 · 판매함). 에셋: Assets/PogeunVillage/Data/Items/Furniture/
    /// 결정 (2026-10-28, GDD 4-10): 풀밭 땅 칸 1칸, 네 방향. 수치는 BALANCE.md 16번.
    /// </summary>
    [CreateAssetMenu(fileName = "Furniture", menuName = "Pogeun Village/Furniture")]
    public class FurnitureDefinition : ItemDefinition
    {
        [Header("가구 (BALANCE.md 16번)")]
        [Tooltip("놓았을 때 모습(맵 생성 메뉴가 그레이박스로 만듦)")]
        public GameObject prefab;
        [Tooltip("작업대에서 만들 때 필요한 목재(0이면 만들 수 없음 — 구매만, 3-4b)")]
        [Min(0)] public int woodCost;
        [Tooltip("놓였을 때 마을 평가 점수(3-4e)")]
        [Min(0)] public int ratingPoint;
        [Tooltip("상점 판매 · 제작법이 열리는 마을 평가 별(1 = 처음부터, 3-4b · c)")]
        [Range(1, 5)] public int unlockStars = 1;
        [Tooltip("테마 가구면 그 주민(색 · 세트, 3-4d). 기본 가구는 비움")]
        public VillagerDefinition themeOwner;
    }
}
