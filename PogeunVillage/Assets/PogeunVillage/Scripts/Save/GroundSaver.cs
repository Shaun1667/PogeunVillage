using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 땅 · 가구 저장(결정 2026-10-29, 6-1b): 꽃(시듦 · 꽃잎 재질) · 묘목(자란 날) · 구멍 · 처음 맵 나무 중 베어진 것 · 밑동 · 키운 나무 ·
    /// 떨어진 물건(색) · 놓인 가구(수조 · 채집통 포함). 처음 맵의 나무는 자리로 알아봄(시작할 때 기억).
    /// 불러오기는 지금 땅의 꽃 · 묘목 · 구멍 · 가구 · 떨어진 물건을 치우고 저장대로 다시 놓음. 데이터에 없는 물건 · 가구는 건너뜀.
    /// </summary>
    public class GroundSaver : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public VillageGround ground;
        public GardenKeeper garden;
        public FurnitureKeeper furniture;
        public GameObject stumpPrefab;
        public Pickup pickupPrefab;

        readonly Dictionary<Vector2Int, ChoppableTreeBehaviour> originalTrees = new();  // 자리(0.1m) → 처음 맵 나무
        readonly Dictionary<Vector2Int, Vector3> originalTreePositions = new();
        readonly Dictionary<string, Material> petalLooks = new();
        MaterialPropertyBlock block;

        void Start()
        {
            if (!ground || !garden || !furniture || !stumpPrefab || !pickupPrefab)
            {
                Debug.LogError($"{name}: 땅 저장 연결이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            block = new MaterialPropertyBlock();
            foreach (var tree in FindObjectsByType<ChoppableTreeBehaviour>())
            {
                if (tree.transform.IsChildOf(garden.transform))
                    continue;  // 키운 나무는 따로
                var key = Key(tree.transform.position);
                originalTrees[key] = tree;
                originalTreePositions[key] = tree.transform.position;
            }
            foreach (var obj in ground.Objects())
            {
                var petals = Petals(obj);
                if (obj.kind == GroundObjectKind.Flower && petals && petals.sharedMaterial)
                    petalLooks[petals.sharedMaterial.name] = petals.sharedMaterial;
            }
        }

        static Vector2Int Key(Vector3 p) => new(Mathf.RoundToInt(p.x * 10f), Mathf.RoundToInt(p.z * 10f));

        static Renderer Petals(GridObject obj)
        {
            var t = obj ? obj.transform.Find("Petals") : null;
            return t ? t.GetComponent<Renderer>() : null;
        }

        public GroundSave Capture()
        {
            var save = new GroundSave();
            foreach (var obj in ground.Objects())
            {
                if (!obj) continue;
                var c = obj.Cell;
                switch (obj.kind)
                {
                    case GroundObjectKind.Flower:
                        var petals = Petals(obj);
                        save.flowers.Add(new CellData { x = c.x, y = c.y, value = garden.Garden.IsWilted(c) ? 1 : 0, look = petals && petals.sharedMaterial ? petals.sharedMaterial.name : "" });
                        break;
                    case GroundObjectKind.Sapling:
                        save.saplings.Add(new CellData { x = c.x, y = c.y, value = garden.Garden.Growth(c) });
                        break;
                    case GroundObjectKind.Hole:
                        save.holes.Add(new CellData { x = c.x, y = c.y });
                        break;
                }
            }
            foreach (var pair in originalTrees)
                if (!pair.Value)
                    save.felledTrees.Add(Point(originalTreePositions[pair.Key], 1f, 0f));
            foreach (var stump in Stumps())
                save.stumps.Add(Point(stump.transform.position, stump.transform.localScale.x, stump.transform.eulerAngles.y));
            foreach (var tree in garden.GetComponentsInChildren<ChoppableTreeBehaviour>())
            {
                var c = ground.CellAt(tree.transform.position);
                save.grownTrees.Add(new CellData { x = c.x, y = c.y });
            }
            foreach (var pickup in Drops())
            {
                var p = pickup.transform.position;
                var color = PickupColor(pickup);
                save.drops.Add(new DropData { id = pickup.item.id, x = p.x, y = p.y, z = p.z, r = color.r, g = color.g, b = color.b });
            }
            return save;
        }

        public List<PlacedData> CaptureFurniture()
        {
            var list = new List<PlacedData>();
            foreach (var f in furniture.Placed)
            {
                if (!f || !f.Definition) continue;
                var c = ground.CellAt(f.transform.position);
                list.Add(new PlacedData { id = f.Definition.id, x = c.x, y = c.y, rotation = f.Rotation });
            }
            return list;
        }

        /// <param name="findItem">물건 id → 데이터(없으면 null)</param>
        /// <param name="findFurniture">가구 id → 데이터(수조 · 채집통 포함, 없으면 null)</param>
        public void Restore(GroundSave save, List<PlacedData> placed, Func<string, ItemDefinition> findItem, Func<string, FurnitureDefinition> findFurniture, List<string> skipped)
        {
            // 치우기
            furniture.RemoveAll();
            foreach (var obj in ground.Objects())
            {
                if (!obj || obj.kind is not (GroundObjectKind.Flower or GroundObjectKind.Sapling or GroundObjectKind.Hole)) continue;
                obj.Unregister();
                obj.gameObject.SetActive(false);
                Destroy(obj.gameObject);
            }
            foreach (var pickup in Drops())
            {
                pickup.gameObject.SetActive(false);
                Destroy(pickup.gameObject);
            }
            foreach (var stump in Stumps())
            {
                stump.SetActive(false);
                Destroy(stump);
            }
            foreach (var tree in garden.GetComponentsInChildren<ChoppableTreeBehaviour>())
            {
                ground.Grid.Unblock(ground.CellAt(tree.transform.position));
                tree.gameObject.SetActive(false);
                Destroy(tree.gameObject);
            }

            // 나무 · 밑동
            var stumpKeys = new HashSet<Vector2Int>(save.stumps.Select(s => Key(new Vector3(s.x, s.y, s.z))));
            foreach (var f in save.felledTrees)
            {
                var key = Key(new Vector3(f.x, f.y, f.z));
                if (!originalTrees.TryGetValue(key, out var tree) || !tree) continue;
                tree.gameObject.SetActive(false);
                Destroy(tree.gameObject);
                if (!stumpKeys.Contains(key))
                    ground.Grid.Unblock(ground.CellAt(tree.transform.position));  // 삽으로 밑동까지 치운 자리
            }
            foreach (var s in save.stumps)
            {
                var position = new Vector3(s.x, s.y, s.z);
                var stump = Instantiate(stumpPrefab, position, Quaternion.Euler(0f, s.yaw, 0f), garden.transform.parent);
                stump.name = "Stump";
                stump.transform.localScale = Vector3.one * s.scale;
                if (!originalTrees.ContainsKey(Key(position)))
                    ground.Grid.Block(ground.CellAt(position));  // 키운 나무를 벤 밑동
            }
            foreach (var t in save.grownTrees)
                garden.PlantGrownTree(new Vector2Int(t.x, t.y));

            // 꽃 · 묘목 · 구멍
            foreach (var f in save.flowers)
            {
                var spawned = ground.Placer.Spawn(GroundObjectKind.Flower, new Vector2Int(f.x, f.y));
                var petals = spawned ? Petals(spawned.GetComponent<GridObject>()) : null;
                if (petals && petalLooks.TryGetValue(f.look, out var look))
                    petals.sharedMaterial = look;
            }
            foreach (var s in save.saplings)
                ground.Placer.Spawn(GroundObjectKind.Sapling, new Vector2Int(s.x, s.y));
            foreach (var h in save.holes)
                ground.Placer.Spawn(GroundObjectKind.Hole, new Vector2Int(h.x, h.y));
            garden.Garden.Restore(
                save.flowers.Where(f => f.value != 0).Select(f => new Vector2Int(f.x, f.y)),
                save.saplings.Select(s => new KeyValuePair<Vector2Int, int>(new Vector2Int(s.x, s.y), s.value)));
            garden.RefreshLooks();

            // 가구
            foreach (var p in placed)
            {
                var definition = findFurniture(p.id);
                if (!definition) { skipped.Add(p.id); continue; }
                furniture.Place(definition, new Vector2Int(p.x, p.y), p.rotation);
            }

            // 떨어진 물건
            foreach (var d in save.drops)
            {
                var item = findItem(d.id);
                if (!item) { skipped.Add(d.id); continue; }
                var pickup = Instantiate(pickupPrefab, new Vector3(d.x, d.y, d.z), Quaternion.identity);
                pickup.name = item.id;
                pickup.item = item;
                pickup.GetComponent<Interactable>().displayName = item.displayName;
                block.Clear();
                block.SetColor(BaseColorId, new Color(d.r, d.g, d.b));
                pickup.GetComponentInChildren<Renderer>().SetPropertyBlock(block);
            }
        }

        static PointData Point(Vector3 p, float scale, float yaw) => new() { x = p.x, y = p.y, z = p.z, scale = scale, yaw = yaw };

        static IEnumerable<GameObject> Stumps() =>
            FindObjectsByType<Interactable>(FindObjectsInactive.Exclude).Where(i => i.kind == InteractableKind.Stump).Select(i => i.gameObject).ToList();

        // 떨어진 물건만(땅 칸의 꽃은 Pickup이어도 칸 물건이라 따로)
        static IEnumerable<Pickup> Drops() =>
            FindObjectsByType<Pickup>(FindObjectsInactive.Exclude).Where(p => p.item && !p.GetComponent<GridObject>()).ToList();

        Color PickupColor(Pickup pickup)
        {
            var r = pickup.GetComponentInChildren<Renderer>();
            if (!r) return Color.white;
            if (r.HasPropertyBlock())
            {
                r.GetPropertyBlock(block);
                if (block.HasColor(BaseColorId)) return block.GetColor(BaseColorId);
            }
            return r.sharedMaterial && r.sharedMaterial.HasProperty(BaseColorId) ? r.sharedMaterial.GetColor(BaseColorId) : Color.white;
        }
    }
}
