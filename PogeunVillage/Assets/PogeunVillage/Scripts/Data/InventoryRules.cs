using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 인벤토리 수치. 값의 기준은 BALANCE.md 10번. 에셋: Assets/PogeunVillage/Data/InventoryRules.asset
    /// 물건별 겹침 수는 ItemDefinition.maxStack.
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryRules", menuName = "Pogeun Village/Inventory Rules")]
    public class InventoryRules : ScriptableObject
    {
        [Tooltip("인벤토리 칸 수")]
        [Min(1)] public int slotCount = 20;
    }
}
