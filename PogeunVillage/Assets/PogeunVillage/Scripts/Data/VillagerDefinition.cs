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
        [Tooltip("캠핑 왔을 때 잡담 대신 하는 말(무작위 1줄, 2-6)")]
        public string[] campLines = { "이 마을 참 포근하다… 며칠 머물러도 될까?", "캠핑하기 딱 좋은 곳이네! 너는 여기 살아?" };
        [Tooltip("캠핑 중 이주가 확정된 순간(3-1)")]
        public string[] moveInLines = { "결정했어! 나 이 마을에 이사 올게!" };
        [Tooltip("이주 확정 뒤 그날 다시 말 걸었을 때(3-1)")]
        public string[] movingLines = { "내일 이사 갈게! 잘 부탁해.", "어떤 집을 지을지 벌써 설레~" };
        [Tooltip("이사 준비 중일 때 잡담 대신(3-2)")]
        public string[] leavingLines = { "…요즘 이 마을을 떠날까 생각 중이야.", "짐을 싸고 있었어. 별일 없으면 내일 떠날 거야." };
        [Tooltip("선물을 받았을 때(무작위 1줄, 2-5)")]
        public string[] thanksLines = { "와, 고마워! 소중히 간직할게.", "나한테 주는 거야? 정말 기뻐!" };
    }
}
