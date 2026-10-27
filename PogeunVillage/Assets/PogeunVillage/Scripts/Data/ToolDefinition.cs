using UnityEngine;

namespace PogeunVillage
{
    /// <summary>도구 종류. 순서 = 숫자키 1~5 순서. enum은 끝에만 추가.</summary>
    public enum ToolKind
    {
        Net,
        FishingRod,
        Axe,
        Shovel,
        WateringCan,
    }

    /// <summary>
    /// 도구 한 종류. 인벤토리에서 한 칸을 차지하고(겹치지 않음) 칸마다 내구도를 가집니다.
    /// 에셋: Assets/PogeunVillage/Data/Items/Tools/. 수치는 BALANCE.md 5번.
    /// </summary>
    [CreateAssetMenu(fileName = "Tool", menuName = "Pogeun Village/Tool")]
    public class ToolDefinition : ItemDefinition
    {
        public ToolKind kind;
        [Tooltip("새 도구의 내구도. 무언가에 맞을 때마다 1씩 줄고 0이면 부서짐")]
        [Min(1)] public int durability = 50;
        [Tooltip("휘둘렀을 때 '맞음'으로 치는 대상 종류. 실제 효과는 각 기능(1-7 도끼 · 1-8 삽 · 1-9 물뿌리개 …)이 붙임")]
        public InteractableKind[] hitKinds = new InteractableKind[0];
        [Tooltip("땅 칸을 다루는 도구(삽 · 물뿌리개). 들고 있으면 앞 칸 표시가 뜸")]
        public bool usesGroundCell;

        void Reset() => maxStack = 1;
    }
}
