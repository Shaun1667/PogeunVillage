using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 주민 한 명(20명 중 하나). 에셋: Assets/PogeunVillage/Data/Villagers/Villager01 ~ 20.asset
    /// 결정 (2026-10-28): 이름 · 종족은 임시('주민 01' …), 나중에 에셋에서 바꿈. 색은 그레이박스 몸 · 집 색.
    /// </summary>
    [CreateAssetMenu(fileName = "Villager", menuName = "Pogeun Village/Villager")]
    public class VillagerDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [Tooltip("종족(임시). 모델을 가져오면 모델과 맞춤")]
        public string species;
        [Tooltip("그레이박스 몸 색 · 집 벽 색")]
        public Color bodyColor = Color.white;
        [Tooltip("그레이박스 귀 · 집 지붕 색")]
        public Color accentColor = Color.gray;

        [Header("대사 (시간대 인사 1줄 + 잡담 1줄)")]
        public string[] morningLines = { };
        public string[] daytimeLines = { };
        public string[] eveningLines = { };
        public string[] smallTalk = { };
        [Tooltip("선물을 받았을 때(무작위 1줄, 2-5)")]
        public string[] thanksLines = { "와, 고마워! 소중히 간직할게.", "나한테 주는 거야? 정말 기뻐!" };
    }
}
