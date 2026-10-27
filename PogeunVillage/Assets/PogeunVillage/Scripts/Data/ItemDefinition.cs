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
    }
}
