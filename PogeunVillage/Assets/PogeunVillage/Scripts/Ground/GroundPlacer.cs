using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 칸에 새로 놓이는 것(구멍 · 묘목 · 심은 꽃)을 만들어 그 칸 가운데 땅 위에 두고, 바로 칸에 등록합니다.
    /// </summary>
    [RequireComponent(typeof(VillageGround))]
    public class GroundPlacer : MonoBehaviour
    {
        const int IgnoreRaycastLayer = 2;

        public GameObject holePrefab;
        public GameObject saplingPrefab;
        public GameObject flowerPrefab;

        VillageGround ground;

        void Awake() => ground = GetComponent<VillageGround>();

        /// <returns>놓은 것, 놓을 수 없으면 null</returns>
        public GameObject Spawn(GroundObjectKind kind, Vector2Int cell)
        {
            GameObject prefab = kind switch
            {
                GroundObjectKind.Hole => holePrefab,
                GroundObjectKind.Sapling => saplingPrefab,
                GroundObjectKind.Flower => flowerPrefab,
                _ => null,
            };
            if (!prefab || !ground.Grid.CanPlace(cell))
                return null;

            Vector2 c = ground.Grid.CellCenter(cell);
            Vector3 position = new(c.x, 0f, c.y);
            if (Physics.Raycast(position + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f, ~(1 << IgnoreRaycastLayer), QueryTriggerInteraction.Ignore))
                position.y = hit.point.y;

            var go = Instantiate(prefab, position, Quaternion.identity, transform);
            var gridObject = go.GetComponent<GridObject>();
            gridObject.ground = ground;
            gridObject.kind = kind;
            if (!gridObject.Register())
            {
                Destroy(go);
                return null;
            }
            return go;
        }
    }
}
