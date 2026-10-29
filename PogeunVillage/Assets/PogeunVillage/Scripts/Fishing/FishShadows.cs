using System;
using System.Collections.Generic;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 강의 물고기 그림자 관리(결정 2026-10-29, 3-6a): 게임을 시작할 때와 매일 아침 shadowCount개를 채우고,
    /// 낚거나 놓쳐 사라진 그림자는 게임 respawnGameMinutes분 뒤 강의 다른 자리에 새로. 물고기는 생길 때의 시간대에 나오는 것 중
    /// 희귀도 가중치로(FishPicker). 그림자 크기 = 물고기 크기. 수치는 FishingRules(BALANCE.md 17번).
    /// </summary>
    public class FishShadows : MonoBehaviour
    {
        public FishingRules rules;
        public WorldSettings settings;
        public VillageGround ground;
        public DayCycle dayCycle;
        public FishShadow shadowPrefab;
        [Tooltip("그림자 지름(m): 소 · 중 · 대")]
        public float[] sizeScales = { 0.35f, 0.55f, 0.8f };
        [Tooltip("물 높이 위로 띄우는 높이(물결에 묻히지 않게)")]
        public float surfaceOffset = 0.07f;

        readonly List<FishShadow> shadows = new();
        readonly List<int> respawns = new();  // 다시 생기는 게임 분(오늘 시작부터) — 시계는 여러 분을 한 번에 건너뛸 수 있음
        List<Vector2Int> water;
        IRandomSource random;
        IRandomSource wanderRandom;

        public IReadOnlyList<FishShadow> Shadows => shadows;
        public int WaterCellCount => water?.Count ?? 0;

        void Start()
        {
            if (!rules || !settings || !ground || ground.data == null || !dayCycle || dayCycle.Clock == null || !shadowPrefab || rules.fish.Length == 0)
            {
                Debug.LogError($"{name}: 물고기 그림자 연결이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            water = ground.data.WaterCells();
            random = new SystemRandomSource(rules.seed);
            wanderRandom = new SystemRandomSource(rules.seed + 1);
            dayCycle.Clock.MinuteChanged += OnMinute;
            dayCycle.Clock.DayStarted += OnDay;
            Fill();
        }

        void OnDestroy()
        {
            if (dayCycle && dayCycle.Clock != null)
            {
                dayCycle.Clock.MinuteChanged -= OnMinute;
                dayCycle.Clock.DayStarted -= OnDay;
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
            for (int i = 0; i < due && shadows.Count < rules.shadowCount; i++)
                Spawn();
        }

        // 새 날: 모두 새로(그날 아침 시간대 물고기로)
        void OnDay(int _)
        {
            foreach (var s in shadows)
                if (s) Destroy(s.gameObject);
            shadows.Clear();
            respawns.Clear();
            Fill();
        }

        void Fill()
        {
            for (int i = shadows.Count + respawns.Count; i < rules.shadowCount; i++)
                if (!Spawn()) break;
        }

        /// <summary>지금 시간대 물고기 하나를 강의 빈자리에. 자리나 물고기가 없으면 null</summary>
        public FishShadow Spawn()
        {
            var period = dayCycle.Clock.Period;
            var rarities = Array.ConvertAll(rules.fish, f => f.rarity);
            var available = Array.ConvertAll(rules.fish, f => f.AppearsIn(period));
            int index = FishPicker.PickFish(rarities, available, rules.rarityWeights, random);
            var occupied = new List<Vector2Int>();
            foreach (var s in shadows) occupied.Add(s.Cell);
            var cell = FishPicker.PickWaterCell(water, occupied, rules.shadowSpacingCells, random);
            return index < 0 || cell == null ? null : SpawnAt(cell.Value, rules.fish[index]);
        }

        /// <summary>그 물 칸에 그 물고기 그림자(테스트도 씀)</summary>
        public FishShadow SpawnAt(Vector2Int cell, FishDefinition fish)
        {
            Vector2 c = ground.Grid.CellCenter(cell);
            var shadow = Instantiate(shadowPrefab, new Vector3(c.x, settings.waterLevel + surfaceOffset, c.y), Quaternion.identity, transform);
            float d = sizeScales[Mathf.Clamp((int)fish.size, 0, sizeScales.Length - 1)];
            shadow.transform.localScale = new Vector3(d, shadow.transform.localScale.y, d * 1.6f);  // 물고기 모양처럼 길쭉하게
            shadow.name = "FishShadow_" + fish.size;
            shadow.Setup(fish, cell, rules.shadowWanderRadius, rules.shadowWanderSpeed, wanderRandom);
            shadows.Add(shadow);
            return shadow;
        }

        /// <summary>찌에서 distance 안의 가장 가까운 그림자(다가오는 중인 것 제외)</summary>
        public FishShadow NearestWithin(Vector3 point, float distance)
        {
            FishShadow best = null;
            float bestSq = distance * distance;
            foreach (var s in shadows)
            {
                if (!s || s.IsAttracted) continue;
                Vector3 d = s.transform.position - point;
                d.y = 0f;
                if (d.sqrMagnitude <= bestSq)
                {
                    bestSq = d.sqrMagnitude;
                    best = s;
                }
            }
            return best;
        }

        /// <summary>낚거나 놓쳐 사라짐 → 게임 respawnGameMinutes분 뒤 새로</summary>
        public void Remove(FishShadow shadow)
        {
            if (!shadow || !shadows.Remove(shadow))
                return;
            Destroy(shadow.gameObject);
            respawns.Add(dayCycle.Clock.MinutesSinceDayStart + rules.respawnGameMinutes);
        }
    }
}
