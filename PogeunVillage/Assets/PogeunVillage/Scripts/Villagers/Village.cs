using System;
using System.Collections.Generic;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 마을 주민 어댑터(2-4): 시작할 때 전체 주민 중 무작위 N명을 빈 집터 중 무작위에 입주시키고(시드 고정) 주민을 만듦.
    /// 시계를 구독해 밤이 되면 집으로, 잠에서 깨면(새 날) 모두 현관에서 다시 나오게 함. 대사는 대사용 난수로 고름.
    /// 친밀도(2-5): 말 걸기 = 그날 첫 대화 +1 → 대사 끝에 '선물하기 / 안녕' → 선물하기면 인벤토리에서 1개 골라 줌(+2, 도구 제외).
    /// 구멍에 빠졌다 나온 주민은 구멍이 없어지고 친밀도가 깎임. 수치는 VillageRules.
    /// 캠핑(2-6): 시작할 때와 매일 아침 마을에 살지 않는 주민 중 1명(어제 온 주민 제외, 꽉 차면 없음)이 텐트에 옴.
    /// 캠핑 주민과는 대사만(이름 옆 '캠핑 중') — 친밀도 · 선물 · 이주는 3-1.
    /// </summary>
    public class Village : MonoBehaviour
    {
        static readonly string[] TalkChoices = { "선물하기", "안녕" };
        const int GiftChoice = 0;

        public VillageRules rules;
        public WorldSettings settings;
        public DayCycle dayCycle;
        public VillageGround ground;
        public DialogueWindow dialogue;
        public InventoryWindow inventoryWindow;
        public PlayerInventory playerInventory;
        public HousePlot[] plots = { };
        [Tooltip("캠핑 구역 텐트(집 대신). 입구가 door")]
        public HousePlot campSite;
        public VillagerBehaviour villagerPrefab;

        public VillageResidents Residents { get; private set; }
        public Friendship Friendship { get; private set; }
        public WalkMap WalkMap { get; private set; }
        public IReadOnlyList<VillagerBehaviour> Villagers => villagers;
        /// <summary>오늘 캠핑 온 주민(없으면 null)</summary>
        public VillagerBehaviour Camper { get; private set; }

        readonly List<VillagerBehaviour> villagers = new();
        IRandomSource dialogueRandom;
        IRandomSource campRandom;

        void Start()
        {
            if (!rules || !settings || !dayCycle || dayCycle.Clock == null || !ground || ground.Grid == null || !villagerPrefab || plots.Length == 0
                || !dialogue || !inventoryWindow || !playerInventory || !campSite)
            {
                Debug.LogError($"{name}: 마을 연결이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            WalkMap = ground.data.CreateWalkMap();
            dialogueRandom = new SystemRandomSource(rules.dialogueSeed);
            campRandom = new SystemRandomSource(rules.campSeed);
            Friendship = new Friendship(rules.talkGain, rules.giftGain, rules.dailyGainCap, rules.friendshipMin, rules.friendshipMax);

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
                villager.Setup(this, resident, plots[i], new SystemRandomSource(rules.wanderSeed + i), rules.wanderRadius);
                villagers.Add(villager);
            }
            ChangeCamper(announce: false);

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
            foreach (var v in AllVillagers())
            {
                if (outside) v.ComeOut();
                else v.StayHome();
            }
        }

        IEnumerable<VillagerBehaviour> AllVillagers()
        {
            foreach (var v in villagers)
                yield return v;
            if (Camper)
                yield return Camper;
        }

        // 캠핑 주민 교체(GDD 4-5 ④): 어제 온 주민 제외, 꽉 차면 없음. 아침에 바뀔 때만 화면 알림
        void ChangeCamper(bool announce)
        {
            string yesterday = Camper ? Camper.Definition.id : null;
            if (Camper)
            {
                Camper.gameObject.SetActive(false);
                Destroy(Camper.gameObject);
                Camper = null;
            }
            var ids = new List<string>();
            foreach (var v in rules.villagers)
                if (v) ids.Add(v.id);
            var visitor = Find(CampVisits.PickVisitor(ids, Residents.IsResident, yesterday, Residents.Count, rules.maxResidents, campRandom));
            campSite.Show(visitor);
            if (!visitor)
                return;
            Camper = Instantiate(villagerPrefab, campSite.DoorPosition, Quaternion.identity, transform);
            Camper.Setup(this, visitor, campSite, new SystemRandomSource(rules.wanderSeed + plots.Length), rules.campWanderRadius, isCamper: true);
            if (announce)
                GameNotice.Post($"[캠핑] 오늘은 {visitor.displayName}이(가) 캠핑 왔어요", this);
        }

        void OnPeriod(DayPeriod period)
        {
            if (VillagerTalk.IsOutside(period))
                return;
            foreach (var v in AllVillagers())
                v.GoHome();
        }

        // 잠에서 깨면 첫 대화 · 하루 상한을 다시 세고, 모두 현관에서 새로 시작(열려 있던 대화 · 선물 고르기 창은 닫음)
        void OnDay(int _)
        {
            dialogue.Close();
            inventoryWindow.Close();
            Friendship.NewDay();
            ChangeCamper(announce: true);
            StartRoutine();
        }

        public Vector2? PickWanderTarget(Vector2 from, Vector2 home, float radius, IRandomSource random) =>
            WalkMap.PickWanderTarget(from, home, radius, random, ground.Grid, rules.wanderTries, rules.walkCheckStep);

        public string SpeakerOf(VillagerDefinition who) => HudText.Speaker(who.displayName, Friendship.Get(who.id));

        public Dialogue ComposeTalk(VillagerDefinition who) =>
            new(SpeakerOf(who), VillagerTalk.Compose(dayCycle.Clock.Period, who.morningLines, who.daytimeLines, who.eveningLines, who.smallTalk, dialogueRandom), TalkChoices);

        /// <summary>캠핑 주민: 시간대 인사 + 캠핑 대사, 고르기 · 친밀도 없음(3-1)</summary>
        public Dialogue ComposeCampTalk(VillagerDefinition who) =>
            new(HudText.CampSpeaker(who.displayName), VillagerTalk.Compose(dayCycle.Clock.Period, who.morningLines, who.daytimeLines, who.eveningLines, who.campLines, dialogueRandom));

        /// <summary>말 걸기: 첫 대화 친밀도 → 대사 → 선물하기면 선물 고르기 → 고맙다는 말. 모두 끝나면 done</summary>
        public void Talk(VillagerBehaviour villager, Action done)
        {
            var who = villager.Definition;
            if (villager.IsCamper)
            {
                dialogue.Open(ComposeCampTalk(who), _ => done());
                return;
            }
            Friendship.Talk(who.id);
            dialogue.Open(ComposeTalk(who), talk =>
            {
                if (talk.Chosen == GiftChoice)
                    PickGift(villager, done);
                else
                    done();
            });
        }

        void PickGift(VillagerBehaviour villager, Action done)
        {
            inventoryWindow.OpenForPick("선물할 물건 고르기", CanGift, "[선물] 그건 줄 수 없어요",
                slot => Give(villager, slot, done), done);
        }

        /// <summary>줄 수 있는 칸: 물건이 있고 도구가 아님(결정 2026-10-28)</summary>
        public bool CanGift(int slot)
        {
            var item = inventoryWindow.ItemAt(slot);
            return item && item is not ToolDefinition;
        }

        void Give(VillagerBehaviour villager, int slot, Action done)
        {
            var who = villager.Definition;
            var item = inventoryWindow.ItemAt(slot);
            playerInventory.Inventory.RemoveOne(slot);
            Friendship.Gift(who.id);
            GameNotice.Post($"[선물] {who.displayName}에게 {item.displayName}을(를) 줬어요", this);
            string thanks = VillagerTalk.PickLine(who.thanksLines, dialogueRandom, "고마워!");
            dialogue.Open(new Dialogue(SpeakerOf(who), new[] { thanks }), _ => done());
        }

        /// <summary>그 위치의 칸이 구멍이면 true(주민이 빠짐)</summary>
        public bool HoleAt(Vector3 position, out Vector2Int cell)
        {
            cell = ground.CellAt(position);
            return ground.Grid.Get(cell) == GroundObjectKind.Hole;
        }

        /// <summary>구멍에서 나옴: 구멍이 없어지고 친밀도 감소(하루 상한과 별개, 하한에서 멈춤)</summary>
        public void ClimbedOutOfHole(VillagerBehaviour villager, Vector2Int cell)
        {
            var hole = ground.ObjectAt(cell);
            if (hole && hole.kind == GroundObjectKind.Hole)
            {
                hole.Unregister();
                Destroy(hole.gameObject);
            }
            if (!villager.IsCamper)  // 캠핑 주민 친밀도는 3-1에서(캠핑 주민은 괴롭힐 수 없음 — GDD 4-2)
                Friendship.Lose(villager.Definition.id, rules.holePenalty);
        }
    }
}
