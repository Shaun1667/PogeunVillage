using System;
using System.Collections.Generic;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 마을 주민 어댑터(2-4): 시작할 때 전체 주민 중 무작위 N명을 빈 집터 중 무작위에 입주시키고(시드 고정) 주민을 만듦.
    /// 시계를 구독해 밤이 되면 집으로, 잠에서 깨면(새 날) 모두 현관에서 다시 나오게 함. 대사는 대사용 난수로 고름.
    /// </summary>
    public class Village : MonoBehaviour
    {
        public VillageRules rules;
        public WorldSettings settings;
        public DayCycle dayCycle;
        public VillageGround ground;
        public DialogueWindow dialogue;
        public HousePlot[] plots = { };
        public VillagerBehaviour villagerPrefab;

        public VillageResidents Residents { get; private set; }
        public WalkMap WalkMap { get; private set; }
        public IReadOnlyList<VillagerBehaviour> Villagers => villagers;

        readonly List<VillagerBehaviour> villagers = new();
        IRandomSource dialogueRandom;

        void Start()
        {
            if (!rules || !settings || !dayCycle || dayCycle.Clock == null || !ground || ground.Grid == null || !villagerPrefab || plots.Length == 0)
            {
                Debug.LogError($"{name}: 마을 연결이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            WalkMap = ground.data.CreateWalkMap();
            dialogueRandom = new SystemRandomSource(rules.dialogueSeed);

            var ids = new List<string>();
            foreach (var v in rules.villagers)
                if (v) ids.Add(v.id);
            Residents = VillageResidents.CreateStarting(plots.Length, ids, rules.startingResidents, new SystemRandomSource(rules.residentSeed));

            for (int i = 0; i < plots.Length; i++)
            {
                var resident = Find(Residents.Plots[i]);
                plots[i].Show(resident);
                if (!resident)
                    continue;
                var villager = Instantiate(villagerPrefab, plots[i].DoorPosition, Quaternion.identity, transform);
                villager.Setup(this, resident, plots[i], new SystemRandomSource(rules.wanderSeed + i));
                villagers.Add(villager);
            }

            dayCycle.Clock.PeriodChanged += OnPeriod;
            dayCycle.Clock.DayStarted += OnDay;
            StartRoutine();
            Debug.Log($"[주민] {Residents.Count}명: {string.Join(", ", villagers.ConvertAll(v => v.Definition.displayName))}", this);
        }

        void OnDestroy()
        {
            if (dayCycle && dayCycle.Clock != null)
            {
                dayCycle.Clock.PeriodChanged -= OnPeriod;
                dayCycle.Clock.DayStarted -= OnDay;
            }
        }

        VillagerDefinition Find(string id) => id == null ? null : Array.Find(rules.villagers, v => v && v.id == id);

        void StartRoutine()
        {
            bool outside = VillagerTalk.IsOutside(dayCycle.Clock.Period);
            foreach (var v in villagers)
            {
                if (outside) v.ComeOut();
                else v.StayHome();
            }
        }

        void OnPeriod(DayPeriod period)
        {
            if (VillagerTalk.IsOutside(period))
                return;
            foreach (var v in villagers)
                v.GoHome();
        }

        // 잠에서 깨면 모두 현관에서 새로 시작(대화 중이던 창은 닫음)
        void OnDay(int _)
        {
            if (dialogue) dialogue.Close();
            StartRoutine();
        }

        public Vector2? PickWanderTarget(Vector2 from, Vector2 home, IRandomSource random) =>
            WalkMap.PickWanderTarget(from, home, rules.wanderRadius, random, ground.Grid, rules.wanderTries, rules.walkCheckStep);

        public Dialogue ComposeTalk(VillagerDefinition who) =>
            new(who.displayName, VillagerTalk.Compose(dayCycle.Clock.Period, who.morningLines, who.daytimeLines, who.eveningLines, who.smallTalk, dialogueRandom));
    }
}
