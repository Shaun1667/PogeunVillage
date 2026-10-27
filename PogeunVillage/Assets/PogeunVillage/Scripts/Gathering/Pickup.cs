using System.Collections;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 상호작용하면 인벤토리에 들어가고 사라지는 물건(떨어진 열매, 꽃). 인벤토리가 가득 차면 그대로 남습니다.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class Pickup : MonoBehaviour
    {
        public ItemDefinition item;

        Interactable interactable;

        void Awake() => interactable = GetComponent<Interactable>();
        void OnEnable() => interactable.Interacted += OnPicked;
        void OnDisable() => interactable.Interacted -= OnPicked;

        void OnPicked(Interactable _, PlayerInteractor by)
        {
            var inventory = by.GetComponent<PlayerInventory>();
            if (!inventory || !item)
            {
                Debug.LogError($"{name}: 인벤토리(PlayerInventory) 또는 물건(item)이 없습니다.", this);
                return;
            }
            if (inventory.TryAdd(item))
                Destroy(gameObject);
        }

        /// <summary>from에서 to까지 떨어뜨림(점점 빨라짐).</summary>
        public void Fall(Vector3 from, Vector3 to, float duration)
        {
            StartCoroutine(FallRoutine(from, to, duration));
        }

        IEnumerator FallRoutine(Vector3 from, Vector3 to, float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                transform.position = Vector3.LerpUnclamped(from, to, k * k);
                yield return null;
            }
            transform.position = to;
        }
    }
}
