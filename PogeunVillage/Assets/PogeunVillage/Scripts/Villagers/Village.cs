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
    /// 캠핑 주민 이주(3-1): 캠핑 주민과도 같은 대화 · 선물로 친밀도를 올리고(CampStay, 이름 옆 '친밀도 3/5'), 기준에 닿으면 이주 확정 →
    /// 다음 날 아침 빈 집터 중 무작위에 입주(친밀도는 캠핑 때 값을 이어받음).
    /// 괴롭히기 · 떠남(3-2): 잠자리채로 마을 주민을 연속으로 때리면 N번마다 친밀도가 깎이고(대화 · 선물 · 3분이면 연속 끊김),
    /// 아침에 -20이면 그날 이사 준비 → 회복하지 않으면 다음 날 아침 떠나 집터가 빔. 캠핑 주민은 맞아도 그대로.
    /// 아침 순서(GDD 4-5): ① 떠남 → ② 입주 → ③ 이사 준비 → ④ 캠핑 교체 → 오늘의 부탁.
    /// 부탁(3-3): 매일 아침 마을 주민 1명이 '물건 N개 가져다줘'(그날만). 다 가지고 말 걸면 '부탁한 것 주기' → 친밀도 +3(상한과 별개) + 모아.
    /// </summary>
    public class Village : MonoBehaviour
    {
        const string GiveRequestChoice = "부탁한 것 주기", GiftChoice = "선물하기", ByeChoice = "안녕";
        static readonly string[] TalkChoices = { GiftChoice, ByeChoice };
        static readonly string[] RequestChoices = { GiveRequestChoice, GiftChoice, ByeChoice };

        public VillageRules rules;
        public WorldSettings settings;
        public DayCycle dayCycle;
        public VillageGround ground;
        public DialogueWindow dialogue;
        public InventoryWindow inventoryWindow;
        public PlayerInventory playerInventory;
        public PlayerWallet wallet;
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
        /// <summary>오늘 캠핑 주민의 친밀도 · 이주 확정(없으면 null)</summary>
        public CampStay CampStay { get; private set; }
        public BullyStreak Bully { get; private set; }
        public Departures Departures { get; private set; }
        /// <summary>오늘의 부탁(없으면 null)</summary>
        public VillagerRequest Request { get; private set; }

        readonly List<VillagerBehaviour> villagers = new();
        IRandomSource dialogueRandom;
        IRandomSource campRandom;
        IRandomSource residentRandom;
        IRandomSource requestRandom;

        void Start()
        {
            if (!rules || !settings || !dayCycle || dayCycle.Clock == null || !ground || ground.Grid == null || !villagerPrefab || plots.Length == 0
                || !dialogue || !inventoryWindow || !playerInventory || !wallet || !campSite)
            {
                Debug.LogError($"{name}: 마을 연결이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            WalkMap = ground.data.CreateWalkMap();
            dialogueRandom = new SystemRandomSource(rules.dialogueSeed);
            campRandom = new SystemRandomSource(rules.campSeed);
            requestRandom = new SystemRandomSource(rules.requestSeed);
            Friendship = new Friendship(rules.talkGain, rules.giftGain, rules.dailyGainCap, rules.friendshipMin, rules.friendshipMax);
            Bully = new BullyStreak(rules.bullyHitsPerPenalty, rules.bullyStreakTimeoutSeconds);
            Departures = new Departures(rules.leaveThreshold);
            Friendship.Changed += OnFriendshipChanged;

            var ids = new List<string>();
            foreach (var v in rules.villagers)
                if (v) ids.Add(v.id);
            residentRandom = new SystemRandomSource(rules.residentSeed);  // 시작 주민 · 입주 집터를 이어서 고름
            Residents = VillageResidents.CreateStarting(plots.Length, ids, rules.startingResidents, residentRandom);

            for (int i = 0; i < plots.Length; i++)
            {
                var resident = Find(Residents.Plots[i]);
                plots[i].Show(resident);
                if (resident)
                    SpawnResident(resident, i);
            }
            ChangeCamper(announce: false);
            PickRequest();

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

        VillagerBehaviour SpawnResident(VillagerDefinition resident, int plot)
        {
            var villager = Instantiate(villagerPrefab, plots[plot].DoorPosition, Quaternion.identity, transform);
            villager.Setup(this, resident, plots[plot], new SystemRandomSource(rules.wanderSeed + plot), rules.wanderRadius);
            villagers.Add(villager);
            return villager;
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
            CampStay = null;
            if (!visitor)
                return;
            CampStay = new CampStay(visitor.id, rules.talkGain, rules.giftGain, rules.dailyGainCap, rules.moveInThreshold);
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
            LeaveVillagers();
            MoveInConfirmedCamper();
            StartPreparingToLeave();
            ChangeCamper(announce: true);
            PickRequest();
            StartRoutine();
        }

        // ① 이사 준비 중이던 주민이 떠남: 집터가 비고(표지판), 미거주 주민으로 돌아가 다시 캠핑 후보가 됨
        void LeaveVillagers()
        {
            foreach (string id in Departures.TakeLeaving())
            {
                int plot = Residents.PlotOf(id);
                if (plot < 0 || !Residents.MoveOut(id))
                    continue;
                plots[plot].Show(null);
                var villager = villagers.Find(v => v.Definition.id == id);
                if (villager)
                {
                    villagers.Remove(villager);
                    villager.gameObject.SetActive(false);
                    Destroy(villager.gameObject);
                }
                GameNotice.Post($"[주민] {Find(id).displayName}이(가) 마을을 떠났어요", this);
            }
        }

        // ③ 친밀도가 기준(-20) 이하인 주민이 그날 이사 준비
        void StartPreparingToLeave()
        {
            foreach (string id in Departures.StartPreparing(Residents.Plots, Friendship.Get))
                GameNotice.Post($"[주민] {Find(id).displayName}이(가) 이사를 준비하고 있어요", this);
        }

        // 오늘의 부탁: 마을 주민(이사 준비 중 제외) 중 1명, 그날만(어제 부탁은 사라짐)
        void PickRequest()
        {
            var candidates = new List<string>();
            foreach (string id in Residents.Plots)
                if (id != null && !Departures.IsPreparing(id))
                    candidates.Add(id);
            var items = new List<string>();
            foreach (var item in rules.requestItems)
                if (item) items.Add(item.id);
            Request = VillagerRequest.Pick(candidates, items, rules.requestCount.x, rules.requestCount.y, requestRandom);
        }

        bool HasOpenRequest(string id) => Request != null && !Request.Done && Request.VillagerId == id;

        ItemDefinition RequestItem => Request == null ? null : Array.Find(rules.requestItems, i => i && i.id == Request.ItemId);

        void OnFriendshipChanged(string id, int value)
        {
            if (Departures.OnFriendshipChanged(id, value))
                GameNotice.Post($"[주민] {Find(id).displayName}이(가) 이사를 그만두기로 했어요", this);
        }

        /// <summary>도구에 맞음: 잠자리채면 흔들리고, 마을 주민이면 연속 횟수를 셈(N번째마다 친밀도 감소 + 알림). 캠핑 주민은 그대로(2-13)</summary>
        public void HitWithTool(VillagerBehaviour villager, ToolDefinition tool)
        {
            if (tool.kind != ToolKind.Net)
                return;
            var interactable = villager.GetComponent<Interactable>();
            if (interactable)
                interactable.Wiggle(settings);
            if (villager.IsCamper)
                return;
            var who = villager.Definition;
            if (Bully.Hit(who.id, Time.time))
            {
                Friendship.Lose(who.id, rules.bullyPenalty);
                GameNotice.Post($"[주민] {who.displayName}이(가) 화가 났어요", this);
            }
        }

        // 어제 이주가 확정된 캠핑 주민이 빈 집터 중 무작위에 입주(GDD 4-5 ②), 친밀도는 캠핑 때 값
        void MoveInConfirmedCamper()
        {
            if (CampStay == null || !CampStay.Confirmed || !Camper)
                return;
            var who = Camper.Definition;
            int plot = Residents.MoveIn(who.id, residentRandom);
            if (plot < 0)
                return;
            plots[plot].Show(who);
            SpawnResident(who, plot);
            Friendship.Begin(who.id, CampStay.Value);
            GameNotice.Post($"[이주] {who.displayName}이(가) 이사 왔어요!", this);
        }

        public Vector2? PickWanderTarget(Vector2 from, Vector2 home, float radius, IRandomSource random) =>
            WalkMap.PickWanderTarget(from, home, radius, random, ground.Grid, rules.wanderTries, rules.walkCheckStep);

        public string SpeakerOf(VillagerDefinition who) =>
            HudText.Speaker(who.displayName, Friendship.Get(who.id), Departures.IsPreparing(who.id), HasOpenRequest(who.id));

        /// <summary>
        /// 시간대 인사 + 잡담(이사 준비 중이면 떠날까 하는 말, 부탁이 있으면 부탁 대사) + 고르기.
        /// 부탁한 물건을 다 가지고 있으면 고르기 맨 앞에 '부탁한 것 주기'
        /// </summary>
        public Dialogue ComposeTalk(VillagerDefinition who)
        {
            IReadOnlyList<string> second = Departures.IsPreparing(who.id) ? who.leavingLines : who.smallTalk;
            var choices = TalkChoices;
            if (HasOpenRequest(who.id) && RequestItem)
            {
                string template = VillagerTalk.PickLine(who.requestLines, dialogueRandom, "{0} {1}개만 가져다줄 수 있어?");
                second = new[] { HudText.RequestLine(template, RequestItem.displayName, Request.Count) };
                if (Request.CanFulfill(playerInventory.Inventory.Count(Request.ItemId)))
                    choices = RequestChoices;
            }
            return new(SpeakerOf(who), VillagerTalk.Compose(dayCycle.Clock.Period, who.morningLines, who.daytimeLines, who.eveningLines, second, dialogueRandom), choices);
        }

        string CampSpeakerOf(VillagerDefinition who) =>
            HudText.CampSpeaker(who.displayName, CampStay.Value, CampStay.Threshold, CampStay.Confirmed);

        /// <summary>캠핑 주민: 시간대 인사 + 캠핑 대사 + 고르기. 이주가 확정됐으면 '내일 이사 갈게' 대사만(고르기 없음)</summary>
        public Dialogue ComposeCampTalk(VillagerDefinition who) => CampStay.Confirmed
            ? new(CampSpeakerOf(who), new[] { VillagerTalk.PickLine(who.movingLines, dialogueRandom, "내일 이사 갈게!") })
            : new(CampSpeakerOf(who), VillagerTalk.Compose(dayCycle.Clock.Period, who.morningLines, who.daytimeLines, who.eveningLines, who.campLines, dialogueRandom), TalkChoices);

        /// <summary>말 걸기: 첫 대화 친밀도 → 대사 → 선물하기면 선물 고르기 → 고맙다는 말. 모두 끝나면 done</summary>
        public void Talk(VillagerBehaviour villager, Action done)
        {
            var who = villager.Definition;
            if (villager.IsCamper)
                CampStay.Talk();
            else
            {
                Bully.Reset(who.id);  // 대화하면 괴롭히기 연속이 끊김
                Friendship.Talk(who.id);
            }
            dialogue.Open(villager.IsCamper ? ComposeCampTalk(who) : ComposeTalk(who), talk =>
            {
                string chosen = talk.Chosen >= 0 ? talk.Choices[talk.Chosen] : null;
                if (chosen == GiftChoice)
                    PickGift(villager, done);
                else if (chosen == GiveRequestChoice)
                    FulfillRequest(villager, done);
                else
                    done();
            });
        }

        // 부탁한 물건을 앞쪽 칸부터 빼고, 친밀도 +3(하루 상한과 별개) · 모아 보상 · 답례
        void FulfillRequest(VillagerBehaviour villager, Action done)
        {
            var who = villager.Definition;
            var item = RequestItem;
            if (!HasOpenRequest(who.id) || !item || !playerInventory.Inventory.TryRemove(Request.ItemId, Request.Count))
            {
                done();
                return;
            }
            int moa = VillagerRequest.MoaReward(item.sellPrice, Request.Count, rules.requestMoaMultiplier);
            wallet.Wallet.Earn(moa);
            Bully.Reset(who.id);
            Friendship.Reward(who.id, rules.requestFriendshipGain);
            Request.Complete();
            GameNotice.Post($"[부탁] {who.displayName}의 부탁을 들어줬어요 (+{HudText.Moa(moa)})", this);
            string thanks = VillagerTalk.PickLine(who.requestThanksLines, dialogueRandom, "고마워! 이건 답례야.");
            dialogue.Open(new Dialogue(SpeakerOf(who), new[] { thanks }), _ => done());
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
            string given = $"[선물] {who.displayName}에게 {item.displayName}을(를) 줬어요";
            if (!villager.IsCamper)
            {
                GameNotice.Post(given, this);
                Bully.Reset(who.id);
                Friendship.Gift(who.id);
                string thanks = VillagerTalk.PickLine(who.thanksLines, dialogueRandom, "고마워!");
                dialogue.Open(new Dialogue(SpeakerOf(who), new[] { thanks }), _ => done());
                return;
            }

            bool wasConfirmed = CampStay.Confirmed;
            CampStay.Gift();
            string line = VillagerTalk.PickLine(who.thanksLines, dialogueRandom, "고마워!");
            if (CampStay.Confirmed && !wasConfirmed)
            {
                // 이주 확정(결정 2026-10-28): 답례 대신 확정 대사, 선물 알림 대신 확정 알림
                line = VillagerTalk.PickLine(who.moveInLines, dialogueRandom, "나 이 마을에 이사 올게!");
                GameNotice.Post($"[이주] {who.displayName}이(가) 내일 이사 와요!", this);
            }
            else
                GameNotice.Post(given, this);
            dialogue.Open(new Dialogue(CampSpeakerOf(who), new[] { line }), _ => done());
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
            if (!villager.IsCamper)  // 캠핑 주민은 괴롭힐 수 없음(GDD 4-2) — 캠핑 친밀도는 그대로
                Friendship.Lose(villager.Definition.id, rules.holePenalty);
        }
    }
}
