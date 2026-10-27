using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 현재 상호작용 대상 위에 떠서 통통 튀는 마커(그레이박스). PlayerInteractor.TargetChanged를 구독합니다.
    /// </summary>
    public class InteractionMarker : MonoBehaviour
    {
        public WorldSettings settings;
        public PlayerInteractor interactor;

        Interactable target;
        float bobTime;

        void Awake()
        {
            if (!settings || !interactor)
            {
                Debug.LogError($"{name}: WorldSettings 또는 PlayerInteractor가 연결되지 않았습니다.", this);
                enabled = false;
                return;
            }
            transform.localScale = Vector3.one * settings.markerSize;
            Show(null);
        }

        void OnEnable()
        {
            if (interactor)
                interactor.TargetChanged += Show;
        }

        void OnDisable()
        {
            if (interactor)
                interactor.TargetChanged -= Show;
        }

        void Show(Interactable newTarget)
        {
            target = newTarget;
            bobTime = 0f;
            foreach (Transform child in transform)
                child.gameObject.SetActive(target);
            if (target)
                transform.position = target.MarkerPosition;
        }

        void LateUpdate()
        {
            if (!target)
                return;
            bobTime += Time.deltaTime * settings.markerBobSpeed;
            float bob = Mathf.Abs(Mathf.Sin(bobTime * Mathf.PI)) * settings.markerBobHeight;
            transform.position = target.MarkerPosition + Vector3.up * bob;
        }
    }
}
