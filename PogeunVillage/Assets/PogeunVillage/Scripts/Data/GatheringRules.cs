using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 채집 규칙 수치. 값의 기준은 BALANCE.md 6번. 에셋: Assets/PogeunVillage/Data/GatheringRules.asset
    /// </summary>
    [CreateAssetMenu(fileName = "GatheringRules", menuName = "Pogeun Village/Gathering Rules")]
    public class GatheringRules : ScriptableObject
    {
        [Header("열매 나무")]
        [Tooltip("아침마다 열리는 열매 수")]
        public int fruitPerDay = 3;
        [Tooltip("나무에 달릴 수 있는 최대 열매 수. 게임 시작 때도 이만큼 달려 있습니다.")]
        public int fruitMaxOnTree = 3;

        [Header("도끼 (BALANCE.md 5번)")]
        [Tooltip("도끼로 나무를 칠 때마다 받는 목재 수")]
        public int axeWoodPerHit = 2;
        [Tooltip("이 횟수만큼 치면 나무가 베어져 밑동만 남음")]
        [Min(1)] public int axeHitsToFell = 5;

        [Header("묘목 · 꽃 (BALANCE.md 8번)")]
        [Tooltip("묘목이 다 자라는 데 필요한 성장일(아침마다 +1)")]
        [Min(1)] public int saplingGrowDays = 6;
        [Tooltip("그날 물을 준 묘목은 아침에 이만큼 더 자람(하루 1번만 인정)")]
        public int saplingWaterBonus = 1;
        [Tooltip("그날 물을 안 준 꽃이 다음 날 아침 시들 확률")]
        [Range(0f, 1f)] public float flowerWiltChance = 0.1f;
        [Tooltip("꽃 번식 · 시듦 난수 시드(같은 시드 + 같은 행동 = 같은 결과)")]
        public int natureSeed = 20261027;
    }
}
