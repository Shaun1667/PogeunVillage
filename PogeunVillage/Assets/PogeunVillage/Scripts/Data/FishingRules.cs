using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 낚시 수치. 값의 기준은 BALANCE.md 17번. 에셋: Assets/PogeunVillage/Data/FishingRules.asset
    /// </summary>
    [CreateAssetMenu(fileName = "FishingRules", menuName = "Pogeun Village/Fishing Rules")]
    public class FishingRules : ScriptableObject
    {
        [Tooltip("강에 나오는 물고기(그림자가 생길 때 지금 시간대 중에서 고름)")]
        public FishDefinition[] fish = { };

        [Header("그림자")]
        [Tooltip("동시에 있는 그림자 수")]
        [Min(0)] public int shadowCount = 4;
        [Tooltip("낚거나 놓쳐 사라진 그림자가 다시 생기는 때(게임 분)")]
        [Min(1)] public int respawnGameMinutes = 30;
        [Tooltip("희귀도 가중치: 흔함 · 보통 · 드문")]
        public int[] rarityWeights = { 60, 30, 10 };
        [Tooltip("그림자끼리 최소 거리(칸)")]
        [Min(0)] public int shadowSpacingCells = 4;
        [Tooltip("그림자가 떠다니는 반경(m, 생긴 자리 둘레)")]
        public float shadowWanderRadius = 0.8f;
        [Tooltip("떠다니는 속도(m/s)")]
        public float shadowWanderSpeed = 0.3f;
        public int seed = 1717;

        [Header("던지기 · 입질")]
        [Tooltip("바라보는 앞쪽 이 거리(m)에 찌가 떨어짐 — 물이 아니면 못 던짐")]
        public float castDistance = 2.5f;
        [Tooltip("찌에서 이 거리(m) 안의 그림자(가장 가까운 하나)가 다가옴")]
        public float approachDistance = 3f;
        [Tooltip("그림자가 찌까지 다가오는 시간(실제 초)")]
        public float approachSeconds = 1.5f;
        [Tooltip("속임수 횟수(최소 · 최대, 포함)")]
        public Vector2Int nibbleCount = new(0, 2);
        [Tooltip("속임수 · 입질 사이 간격(실제 초, 최소 · 최대)")]
        public Vector2 nibbleInterval = new(1f, 2f);
        [Tooltip("입질 뒤 Space를 누를 수 있는 시간(실제 초)")]
        public float biteWindow = 0.8f;
    }
}
