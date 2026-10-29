using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 마을 곤충 관리(결정 2026-10-29, 3-7a): 게임을 시작할 때와 매일 아침 insectCount마리를 채우고, 잡거나 도망가 사라진 곤충은
    /// 게임 respawnGameMinutes분 뒤 다른 자리에 새로. 곤충은 생길 때의 시간대에 나오는 것 중 희귀도 가중치로, 자리는 그 곤충이 사는 곳
    /// (꽃 위 · 마을 안쪽 나무 줄기 · 빈 풀밭 · 강가 물 위). 달려서 다가오거나(예민하면 더 멀리서) 예민한 곤충에게 너무 붙으면 도망,
    /// 잠자리채를 헛휘두르면 가까운 곤충이 도망, 맞으면 잡음(인벤토리, 가득 차면 발밑 · 내구도는 PlayerTools가 -1).
    /// 규칙은 InsectLogic, 수치는 InsectRules(BALANCE.md 18번).
    /// </summary>
    public class Insects : MonoBehaviour
    {
        const int IgnoreRaycastLayer = 2;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public InsectRules rules;
        public WorldSettings settings;
        public VillageGround ground;
        public DayCycle dayCycle;
        public Transform player;
        public PlayerController controller;
        public PlayerTools tools;
        public PlayerInventory inventory;
        public Pickup pickupPrefab;
        public InsectBehaviour insectPrefab;
        [Tooltip("잡으면 기록(3-7b)")]
        public InsectEncyclopedia encyclopedia;
        [Header("자리 높이(m, 그레이박스 모습)")]
        public float flowerHover = 0.6f;
        public float waterHover = 0.8f;
        public float trunkHeight = 1.1f;
        public float trunkOffset = 0.55f;
        [Tooltip("풀밭 자리를 찾는 범위(맵 가운데에서 ±m)")]
        public float grassSearchHalfSize = 40f;

        readonly List<InsectBehaviour> insects = new();
        readonly List<int> respawns = new();  // 다시 생기는 게임 분(오늘 시작부터)
        SeededRandom random;
        List<Vector3> waterSites;

        public IReadOnlyList<InsectBehaviour> All => insects;

        void Start()
        {
            if (!rules || !settings || !ground || !dayCycle || dayCycle.Clock == null || !player || !controller || !tools || !inventory
                || !pickupPrefab || !insectPrefab || rules.insects.Length == 0)
            {
                Debug.LogError($"{name}: 곤충 연결이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            random = new SeededRandom(rules.seed);
            waterSites = WaterSites();
            dayCycle.Clock.MinuteChanged += OnMinute;
            dayCycle.Clock.DayStarted += OnDay;
            tools.ToolUsed += OnToolUsed;
            Fill();
        }

        void OnDestroy()
        {
            if (dayCycle && dayCycle.Clock != null)
            {
                dayCycle.Clock.MinuteChanged -= OnMinute;
                dayCycle.Clock.DayStarted -= OnDay;
            }
            if (tools) tools.ToolUsed -= OnToolUsed;
        }

        void Update()
        {
            Vector2 p = new(player.position.x, player.position.z);
            for (int i = insects.Count - 1; i >= 0; i--)
            {
                var insect = insects[i];
                if (!insect) continue;
                float d = Vector2.Distance(p, new Vector2(insect.transform.position.x, insect.transform.position.z));
                if (InsectLogic.ShouldFlee(d, controller.IsRunning, insect.Definition.sensitive,
                        rules.startleRunDistance, rules.startleRunDistanceSensitive, rules.tooCloseSensitive))
                    Flee(insect);
            }
        }

        void OnMinute(int minute)
        {
            int due = 0;
            for (int i = respawns.Count - 1; i >= 0; i--)
                if (respawns[i] <= minute)
                {
                    respawns.RemoveAt(i);
                    due++;
                }
            for (int i = 0; i < due && insects.Count < rules.insectCount; i++)
                Spawn();
        }

        void OnDay(int _)
        {
            foreach (var insect in insects)
                if (insect) Destroy(insect.gameObject);
            insects.Clear();
            respawns.Clear();
            Fill();
        }

        void Fill()
        {
            for (int i = insects.Count + respawns.Count; i < rules.insectCount; i++)
                Spawn();
        }

        /// <summary>지금 시간대 곤충 하나를 그 곤충이 사는 빈자리에. 없으면 null</summary>
        public InsectBehaviour Spawn()
        {
            var period = dayCycle.Clock.Period;
            var rarities = Array.ConvertAll(rules.insects, d => d.rarity);
            var available = Array.ConvertAll(rules.insects, d => d.AppearsIn(period));
            for (int tries = 0; tries < 4; tries++)
            {
                int index = InsectLogic.PickInsect(rarities, available, rules.rarityWeights, random);
                if (index < 0)
                    return null;
                var spot = PickSite(rules.insects[index].site);
                if (spot.HasValue)
                    return SpawnAt(rules.insects[index], spot.Value);
            }
            return null;
        }

        /// <summary>그 자리에 그 곤충(테스트도 씀)</summary>
        public InsectBehaviour SpawnAt(InsectDefinition definition, Vector3 position)
        {
            var insect = Instantiate(insectPrefab, position, Quaternion.identity, transform);
            insect.Setup(definition, position, (float)random.NextDouble() * 10f);
            insect.GetComponent<Interactable>().HitByTool += OnHit;
            insects.Add(insect);
            return insect;
        }

        // 잠자리채에 맞음 = 잡음
        void OnHit(Interactable target, PlayerTools by, ToolDefinition tool)
        {
            if (tool.kind != ToolKind.Net)
                return;
            var insect = target.GetComponent<InsectBehaviour>();
            if (!insect || !insects.Contains(insect))
                return;
            var definition = insect.Definition;
            Remove(insect);
            if (inventory.Inventory.SpaceFor(definition.id, definition.maxStack) >= 1)
                inventory.TryAdd(definition);
            else
                DropAtFeet(definition);
            GameNotice.Post($"[곤충] {definition.displayName}을(를) 잡았어요! ({HudText.Moa(definition.sellPrice)})", this);
            if (encyclopedia) encyclopedia.Record(definition);
        }

        // 헛스윙: 가까운 곤충이 놀라 도망
        void OnToolUsed(ToolDefinition tool, Interactable target)
        {
            if (tool.kind != ToolKind.Net || target)
                return;
            for (int i = insects.Count - 1; i >= 0; i--)
                if (insects[i] && FlatDistance(insects[i].transform.position, player.position) < rules.missStartleDistance)
                    Flee(insects[i]);
        }

        /// <summary>저장(6-1c): 오늘 곤충(종류 · 자리)</summary>
        public List<DropData> CaptureToday()
        {
            var list = new List<DropData>();
            foreach (var i in insects)
                if (i) list.Add(new DropData { id = i.Definition.id, x = i.Home.x, y = i.Home.y, z = i.Home.z });
            return list;
        }

        public string RandomState { get => random.State; set => random.State = value; }

        /// <summary>불러오기(6-1c): 그날 곤충 그대로(데이터에 없는 곤충은 건너뜀). 아침 기준이라 다시 생길 것은 없음</summary>
        public void RestoreToday(IReadOnlyList<DropData> saved)
        {
            foreach (var i in insects)
                if (i) Destroy(i.gameObject);
            insects.Clear();
            respawns.Clear();
            foreach (var d in saved)
            {
                var definition = Array.Find(rules.insects, x => x && x.id == d.id);
                if (definition) SpawnAt(definition, new Vector3(d.x, d.y, d.z));
            }
        }

        public void Flee(InsectBehaviour insect)
        {
            GameNotice.Post($"[곤충] {insect.Definition.displayName}이(가) 도망갔어요", this);
            Remove(insect);
        }

        /// <summary>알림 없이 없앰(잡음 · 도망 · 테스트) → 게임 respawnGameMinutes분 뒤 새로</summary>
        public void Remove(InsectBehaviour insect)
        {
            if (!insects.Remove(insect))
                return;
            Destroy(insect.gameObject);
            respawns.Add(dayCycle.Clock.MinutesSinceDayStart + rules.respawnGameMinutes);
        }

        static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        bool FarFromOthers(Vector3 p)
        {
            foreach (var other in insects)
                if (other && FlatDistance(other.Home, p) < rules.spacing)
                    return false;
            return true;
        }

        Vector3? PickSite(InsectSite site)
        {
            var candidates = new List<Vector3>();
            switch (site)
            {
                case InsectSite.Flower:
                    foreach (var obj in ground.Objects())
                        if (obj && obj.kind == GroundObjectKind.Flower)
                            candidates.Add(GroundPoint(obj.Cell) + Vector3.up * flowerHover);  // 칸 땅 높이 기준 — 다시 심은 꽃과 처음 꽃의 높이 차이와 상관없이(6-1c)
                    break;
                case InsectSite.Tree:
                    var trees = FindObjectsByType<ChoppableTreeBehaviour>().OrderBy(t => t.transform.position.x).ThenBy(t => t.transform.position.z);  // 찾은 차례와 상관없이(6-1c)
                    foreach (var tree in trees)
                    {
                        float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                        candidates.Add(tree.transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * trunkOffset + Vector3.up * trunkHeight);
                    }
                    break;
                case InsectSite.Grass:
                    for (int i = 0; i < 40; i++)
                    {
                        var cell = ground.CellAt(new Vector3(((float)random.NextDouble() * 2f - 1f) * grassSearchHalfSize, 0f,
                            ((float)random.NextDouble() * 2f - 1f) * grassSearchHalfSize));
                        if (ground.Grid.InBounds(cell) && ground.Grid.CanPlace(cell))
                            candidates.Add(GroundPoint(cell));
                    }
                    break;
                case InsectSite.Water:
                    candidates.AddRange(waterSites);
                    break;
            }
            candidates.RemoveAll(c => !FarFromOthers(c));
            candidates.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.z.CompareTo(b.z));  // 찾은 차례와 상관없이 같은 결과(세이브 6-1c)
            return candidates.Count == 0 ? null : candidates[random.Next(candidates.Count)];
        }

        // 강가: 물 칸 중 옆이 땅인 칸 위 공중(땅에서 잠자리채가 닿게)
        List<Vector3> WaterSites()
        {
            var list = new List<Vector3>();
            var data = ground.data;
            foreach (var w in data.WaterCells())
            {
                if (data.IsWater(w + Vector2Int.up) && data.IsWater(w + Vector2Int.down) && data.IsWater(w + Vector2Int.left) && data.IsWater(w + Vector2Int.right))
                    continue;
                Vector2 c = ground.Grid.CellCenter(w);
                list.Add(new Vector3(c.x, settings.waterLevel + waterHover, c.y));
            }
            return list;
        }

        Vector3 GroundPoint(Vector2Int cell)
        {
            Vector2 c = ground.Grid.CellCenter(cell);
            Vector3 p = new(c.x, 0f, c.y);
            if (Physics.Raycast(p + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f, ~(1 << IgnoreRaycastLayer), QueryTriggerInteraction.Ignore))
                p.y = hit.point.y + 0.08f;
            return p;
        }

        // 인벤토리가 가득 찼을 때: 발밑 땅에 떨어진 곤충(F로 줍기)
        void DropAtFeet(InsectDefinition definition)
        {
            Vector3 spot = player.position - player.forward * 0.6f;
            if (Physics.Raycast(spot + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f, ~(1 << IgnoreRaycastLayer), QueryTriggerInteraction.Ignore))
                spot = hit.point;
            var pickup = Instantiate(pickupPrefab, spot, Quaternion.identity);
            pickup.name = definition.id;
            pickup.item = definition;
            pickup.GetComponent<Interactable>().displayName = definition.displayName;
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, definition.color);
            pickup.GetComponentInChildren<Renderer>().SetPropertyBlock(block);
            GameNotice.Post($"[인벤토리] 가득 차서 {definition.displayName}을(를) 발밑에 내려놨어요", this);
        }
    }
}
