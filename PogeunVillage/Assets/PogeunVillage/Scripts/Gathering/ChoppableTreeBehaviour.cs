using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 도끼로 베는 나무 어댑터(마을 안쪽 나무만, 결정 2026-10-27). 도끼에 맞을 때마다 ChoppableTree 규칙대로
    /// 목재를 인벤토리에 넣고(자리가 없으면 나무 앞 땅에 떨어뜨림) 나무가 흔들립니다.
    /// 베어지면 달린 열매를 전부 떨어뜨리고, 나무는 사라지고 밑동만 남습니다.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class ChoppableTreeBehaviour : MonoBehaviour
    {
        const int IgnoreRaycastLayer = 2;

        public GatheringRules rules;
        public WorldSettings settings;
        public ItemDefinition woodItem;
        public Material woodMaterial;
        public Pickup pickupPrefab;
        public GameObject stumpPrefab;

        ChoppableTree tree;
        Interactable interactable;

        public int Hits => tree?.Hits ?? 0;

        void Awake()
        {
            if (!rules || !settings || !woodItem || !woodMaterial || !pickupPrefab || !stumpPrefab)
            {
                Debug.LogError($"{name}: 도끼 나무 설정(rules · settings · woodItem · woodMaterial · pickupPrefab · stumpPrefab)이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            interactable = GetComponent<Interactable>();
            tree = new ChoppableTree(rules.axeHitsToFell, rules.axeWoodPerHit);
        }

        void OnEnable()
        {
            if (interactable)
                interactable.HitByTool += OnHit;
        }

        void OnDisable()
        {
            if (interactable)
                interactable.HitByTool -= OnHit;
        }

        void OnHit(Interactable _, PlayerTools by, ToolDefinition tool)
        {
            if (tool.kind != ToolKind.Axe)
                return;

            ChopResult result = tree.Chop();
            for (int i = 0; i < result.wood; i++)
                if (!by.inventory.TryAdd(woodItem))
                    DropWood(by.transform.position, i);

            if (!result.felled)
            {
                interactable.Wiggle(settings);
                return;
            }

            Debug.Log($"[도끼] {interactable.displayName}가 베어졌어요", this);
            var fruit = GetComponent<FruitTreeBehaviour>();
            if (fruit)
                fruit.DropAll();
            var stump = Instantiate(stumpPrefab, transform.position, transform.rotation, transform.parent);
            stump.name = "Stump";
            stump.transform.localScale = transform.localScale;
            Destroy(gameObject);
        }

        // 인벤토리에 못 넣은 목재: 나무에서 플레이어 쪽으로 조금 나온 땅에, 여러 개면 옆으로 벌려서
        void DropWood(Vector3 playerPosition, int index)
        {
            Vector3 toPlayer = Vector3.ProjectOnPlane(playerPosition - transform.position, Vector3.up).normalized;
            if (toPlayer == Vector3.zero) toPlayer = Vector3.back;
            Vector3 side = Vector3.Cross(Vector3.up, toPlayer);
            Vector3 spot = transform.position + toPlayer * settings.fruitDropRadius + side * ((index - 0.5f) * 0.5f);
            if (Physics.Raycast(spot + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f, ~(1 << IgnoreRaycastLayer), QueryTriggerInteraction.Ignore))
                spot = hit.point;

            Pickup pickup = Instantiate(pickupPrefab, spot, Quaternion.identity);
            pickup.name = woodItem.id;
            pickup.item = woodItem;
            pickup.GetComponent<Interactable>().displayName = woodItem.displayName;
            pickup.GetComponentInChildren<Renderer>().sharedMaterial = woodMaterial;
            Debug.Log($"[도끼] 인벤토리가 가득 차서 {woodItem.displayName}이(가) 땅에 떨어졌어요", this);
        }
    }
}
