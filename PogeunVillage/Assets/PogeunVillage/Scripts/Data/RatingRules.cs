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
        [Header("가구 (BALANCE.md 16번, 3-4e) — 가구마다 점수는 가구 데이터 ratingPoint")]
        [Tooltip("같은 가구는 몇 개까지 점수")]
        [Min(0)] public int furnitureKindCap = 5;
        [Tooltip("한 주민의 테마 가구를 모두 놓으면(주민마다 한 번)")]
        public int themeSetBonus = 40;

        [Header("도감 (BALANCE.md 17 · 18번, 3-6b · 3-7b)")]
        [Tooltip("도감에 잡은 종류 하나(물고기 · 곤충)")]
        [UnityEngine.Serialization.FormerlySerializedAs("fishSpeciesPoint")]
        public int speciesPoint = 10;

        [Tooltip("★2 · ★3 · ★4 · ★5가 되는 합계(오름차순)")]
        public int[] starThresholds = { 300, 400, 550, 750 };
        [Tooltip("첫 목표 별 개수")]
        [Range(1, 5)] public int firstGoalStars = 3;
    }
}
