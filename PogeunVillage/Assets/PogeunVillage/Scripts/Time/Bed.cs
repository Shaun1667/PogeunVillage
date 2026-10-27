using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 임시 침대(광장): 상호작용(F)하면 자고 다음 날 아침이 됨. 결정 (2026-10-27): 아무 때나 잘 수 있음,
    /// 플레이어 집이 생기면 침대를 집으로 옮김.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class Bed : MonoBehaviour
    {
        public DayCycle dayCycle;

        Interactable interactable;

        void Awake() => interactable = GetComponent<Interactable>();
        void OnEnable() => interactable.Interacted += OnInteracted;
        void OnDisable() => interactable.Interacted -= OnInteracted;

        void OnInteracted(Interactable _, PlayerInteractor by)
        {
            if (dayCycle)
                dayCycle.Sleep();
        }
    }
}
