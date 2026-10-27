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
    }
}
