using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 열매 나무 어댑터: 상호작용(흔들기)하면 FruitTree 규칙대로 열매를 떨어뜨려 주울 수 있는 Pickup으로 만듭니다.
    /// 달린 열매 모습(fruitVisuals)은 남은 개수만큼 보입니다.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class FruitTreeBehaviour : MonoBehaviour
    {
        const int IgnoreRaycastLayer = 2;

        public GatheringRules rules;
        public WorldSettings settings;
        public ItemDefinition fruitItem;
        public Pickup pickupPrefab;
        [Tooltip("나무에 달린 열매 모습. 앞에서부터 남은 개수만큼 보입니다.")]
        public Transform[] fruitVisuals;

        FruitTree tree;
        Interactable interactable;
        Material fruitMaterial;

        public int FruitCount => tree?.Count ?? 0;

        void Awake()
        {
            if (!rules || !settings || !fruitItem || !pickupPrefab || fruitVisuals == null || fruitVisuals.Length == 0)
            {
                Debug.LogError($"{name}: 열매 나무 설정(rules · settings · fruitItem · pickupPrefab · fruitVisuals)이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            interactable = GetComponent<Interactable>();
            fruitMaterial = fruitVisuals[0].GetComponent<Renderer>().sharedMaterial;
            tree = new FruitTree(rules.fruitMaxOnTree);
            RefreshVisuals();
        }

        void OnEnable()
        {
            if (interactable)
                interactable.Interacted += OnShaken;
        }

        void OnDisable()
        {
            if (interactable)
                interactable.Interacted -= OnShaken;
        }

        void OnShaken(Interactable _, PlayerInteractor by) => DropAll();

        /// <summary>달린 열매를 전부 떨어뜨림(흔들기 · 베어질 때)</summary>
        public void DropAll()
        {
            if (tree == null)
                return;
            int dropped = tree.Shake();
            for (int i = 0; i < dropped; i++)
                Drop(i, dropped);
            RefreshVisuals();
        }

        void Drop(int index, int total)
        {
            Transform visual = fruitVisuals[Mathf.Min(index, fruitVisuals.Length - 1)];
            float angle = (transform.eulerAngles.y + 360f * index / total) * Mathf.Deg2Rad;
            Vector3 ring = transform.position + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * settings.fruitDropRadius;

            Vector3 ground = ring;
            if (Physics.Raycast(ring + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f,
                    ~(1 << IgnoreRaycastLayer), QueryTriggerInteraction.Ignore))
                ground = hit.point;

            Pickup pickup = Instantiate(pickupPrefab, visual.position, Quaternion.identity);
            pickup.name = fruitItem.id;
            pickup.item = fruitItem;
            pickup.GetComponent<Interactable>().displayName = fruitItem.displayName;
            pickup.GetComponentInChildren<Renderer>().sharedMaterial = fruitMaterial;
            pickup.Fall(visual.position, ground, settings.fruitFallTime);
        }

        void RefreshVisuals()
        {
            for (int i = 0; i < fruitVisuals.Length; i++)
                fruitVisuals[i].gameObject.SetActive(i < tree.Count);
        }
    }
}
