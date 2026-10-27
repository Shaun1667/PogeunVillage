using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 물건 한 종류(사과 · 복숭아 · 꽃 …). 에셋: Assets/PogeunVillage/Data/Items/
    /// id는 저장 · 주머니에서 쓰는 키이므로 만든 뒤 바꾸지 않습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Item", menuName = "Pogeun Village/Item")]
    public class ItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [Tooltip("인벤토리 한 칸에 겹칠 수 있는 최대 개수 (BALANCE.md 10번)")]
        [Min(1)] public int maxStack = 10;
        [Tooltip("구멍에 심으면 무엇이 되는지(꽃 포기 → 꽃, 묘목 → 묘목). None이면 심을 수 없음")]
        public GroundObjectKind plantsAs;

        [Header("상점 (BALANCE.md 7번)")]
        [Tooltip("상점에서 살 때 가격(모아). 0이면 상점에서 안 팖")]
        [Min(0)] public int buyPrice;
        [Tooltip("판매함에 팔 때 1개 가격(모아). 0이면 팔 수 없음")]
        [Min(0)] public int sellPrice;
    }
}
