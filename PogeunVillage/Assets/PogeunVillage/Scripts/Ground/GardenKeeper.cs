using System.Linq;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 꽃 · 묘목 어댑터: Garden 규칙을 들고, 물 주기를 받고, 아침 결과를 씬에 적용합니다.
    /// (사라진 꽃 치우기 · 번진 꽃 놓기(원래 꽃 색) · 시든 꽃 모습 · 묘목 단계 모습 · 다 자란 묘목 → 나무 + 칸 막기)
    /// </summary>
    [RequireComponent(typeof(VillageGround))]
    public class GardenKeeper : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        const int IgnoreRaycastLayer = 2;

        public GatheringRules rules;
        public WorldSettings settings;
        [Tooltip("묘목이 다 자라면 생기는 나무(열매 없는 활엽수, 벨 수 있음)")]
        public GameObject grownTreePrefab;

        public Garden Garden { get; private set; }

        VillageGround ground;
        MaterialPropertyBlock block;

        void Start()
        {
            ground = GetComponent<VillageGround>();
            if (!rules || !settings || !grownTreePrefab || ground.Grid == null)
            {
                Debug.LogError($"{name}: 꽃 · 묘목 설정(rules · settings · grownTreePrefab)이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            block = new MaterialPropertyBlock();
            Garden = new Garden(ground.Grid, new SystemRandomSource(rules.natureSeed),
                rules.saplingGrowDays, rules.saplingWaterBonus, rules.flowerWiltChance);
        }

        public GardenMorning RunMorning()
        {
            if (Garden == null)
                return new GardenMorning();

            var objects = ground.Objects();
            var morning = Garden.Morning(
                objects.Where(o => o.kind == GroundObjectKind.Flower).Select(o => o.Cell),
                objects.Where(o => o.kind == GroundObjectKind.Sapling).Select(o => o.Cell));

            foreach (var cell in morning.removedFlowers)
                Remove(cell);

            foreach (var (from, to) in morning.spreadFlowers)
            {
                var sourcePetals = Petals(ground.ObjectAt(from));
                var spawned = ground.Placer.Spawn(GroundObjectKind.Flower, to);
                var petals = spawned ? Petals(spawned.GetComponent<GridObject>()) : null;
                if (petals && sourcePetals)
                    petals.sharedMaterial = sourcePetals.sharedMaterial;
            }

            foreach (var cell in morning.grownSaplings)
            {
                Remove(cell);
                ground.Grid.Block(cell);
                Vector2 c = ground.Grid.CellCenter(cell);
                Instantiate(grownTreePrefab, GroundPoint(c), Quaternion.identity, transform).name = "Tree";
            }

            foreach (var obj in ground.Objects())
            {
                if (obj.kind == GroundObjectKind.Flower)
                    ShowWilted(obj, Garden.IsWilted(obj.Cell));
                else if (obj.kind == GroundObjectKind.Sapling)
                    ShowStage(obj, Garden.SaplingStage(obj.Cell));
            }
            return morning;
        }

        void Remove(Vector2Int cell)
        {
            var obj = ground.ObjectAt(cell);
            if (!obj)
                return;
            obj.Unregister();
            Destroy(obj.gameObject);
        }

        static Renderer Petals(GridObject flower)
        {
            if (!flower) return null;
            var t = flower.transform.Find("Petals");
            return t ? t.GetComponent<Renderer>() : null;
        }

        void ShowWilted(GridObject flower, bool wilted)
        {
            var petals = Petals(flower);
            if (petals)
            {
                block.Clear();
                if (wilted)
                    block.SetColor(BaseColorId, settings.wiltedPetalColor);
                petals.SetPropertyBlock(wilted ? block : null);
            }
            flower.transform.localScale = new Vector3(1f, wilted ? settings.wiltedFlowerHeight : 1f, 1f);
        }

        static void ShowStage(GridObject sapling, int stage)
        {
            for (int i = 0; i < 3; i++)
            {
                var child = sapling.transform.Find("Stage" + i);
                if (child) child.gameObject.SetActive(i == stage);
            }
        }

        static Vector3 GroundPoint(Vector2 c)
        {
            Vector3 p = new(c.x, 0f, c.y);
            if (Physics.Raycast(p + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f, ~(1 << IgnoreRaycastLayer), QueryTriggerInteraction.Ignore))
                p.y = hit.point.y;
            return p;
        }
    }
}
