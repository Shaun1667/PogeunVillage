using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 곤충 수치. 값의 기준은 BALANCE.md 18번. 에셋: Assets/PogeunVillage/Data/InsectRules.asset
    /// </summary>
    [CreateAssetMenu(fileName = "InsectRules", menuName = "Pogeun Village/Insect Rules")]
    public class InsectRules : ScriptableObject
    {
        [Tooltip("마을에 나오는 곤충(생길 때 지금 시간대 중에서 고름)")]
        public InsectDefinition[] insects = { };

        [Header("나오기")]
        [Tooltip("동시에 있는 곤충 수")]
        [Min(0)] public int insectCount = 6;
        [Tooltip("잡거나 도망가 사라진 곤충이 다시 생기는 때(게임 분)")]
        [Min(1)] public int respawnGameMinutes = 30;
        [Tooltip("희귀도 가중치: 흔함 · 보통 · 드문")]
        public int[] rarityWeights = { 60, 30, 10 };
        [Tooltip("곤충끼리 최소 거리(m)")]
        public float spacing = 3f;
        public int seed = 1818;

        [Header("놀라기")]
        [Tooltip("달려서 이 거리(m) 안에 들어오면 도망")]
        public float startleRunDistance = 3f;
        [Tooltip("예민한 곤충: 달려서 이 거리(m) 안")]
        public float startleRunDistanceSensitive = 5f;
        [Tooltip("예민한 곤충: 걸어도 이 거리(m) 안이면 도망(결정 2026-10-29: 잠자리채가 닿는 약 1.3m보다 짧게)")]
        public float tooCloseSensitive = 0.8f;
        [Tooltip("헛스윙하면 이 거리(m) 안의 곤충이 도망")]
        public float missStartleDistance = 2f;
    }
}
