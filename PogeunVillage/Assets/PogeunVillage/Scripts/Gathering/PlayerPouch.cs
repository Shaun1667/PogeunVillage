using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 플레이어의 임시 주머니(Pouch) 어댑터. 바뀐 물건만 콘솔에 알립니다. 1-4 인벤토리에서 교체.
    /// </summary>
    public class PlayerPouch : MonoBehaviour
    {
        public Pouch Pouch { get; } = new();

        public void Add(ItemDefinition item)
        {
            Pouch.Add(item.id);
            Debug.Log($"[주머니] {item.displayName} +1 → {Pouch.Get(item.id)}개", this);
        }
    }
}
