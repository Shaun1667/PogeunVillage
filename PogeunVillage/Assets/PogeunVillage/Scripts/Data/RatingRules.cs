using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 마을 평가 수치. 값의 기준은 BALANCE.md 15번. 에셋: Assets/PogeunVillage/Data/RatingRules.asset
    /// </summary>
    [CreateAssetMenu(fileName = "RatingRules", menuName = "Pogeun Village/Rating Rules")]
    public class RatingRules : ScriptableObject
    {
        [Tooltip("시들지 않은 꽃 한 송이")]
        public int flowerPoint = 1;
        [Tooltip("마을 주민 한 명")]
        public int residentPoint = 10;
        [Tooltip("마을 주민 친밀도 합 1당(음수면 깎임)")]
        public int friendshipPoint = 1;
        [Tooltip("묘목에서 자란 나무 한 그루")]
        public int grownTreePoint = 10;
        [Tooltip("★2 · ★3 · ★4 · ★5가 되는 합계(오름차순)")]
        public int[] starThresholds = { 300, 400, 550, 750 };
        [Tooltip("첫 목표 별 개수")]
        [Range(1, 5)] public int firstGoalStars = 3;
    }
}
