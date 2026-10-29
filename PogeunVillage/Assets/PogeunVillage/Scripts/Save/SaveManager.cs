using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 세이브 어댑터(결정 2026-10-29, GDD 4-13 · 6-1a): 자고 일어나 아침 처리가 모두 끝나면 자동 저장("저장했어요").
    /// 게임을 시작할 때 시작 화면(StartScreen, 6-1d)에서 '이어하기'를 고르면 그 날짜 아침으로 불러옴.
    /// 저장하는 것: 날짜 · 모아 · 인벤토리 · 도감 2가지 · 평가 기록(6-1a), 마을 주민 · 캠핑 · 부탁 · 테마 보상 · 땅 · 가구(6-1b, Village · GroundSaver),
    /// 그날 정해진 것(오늘의 테마 · 물고기 그림자 · 곤충)과 난수 상태(6-1c) — 불러와 진행해도 저장 없이 진행한 것과 같은 결과.
    /// 자동 테스트는 PathOverride(임시 폴더)로 저장 위치를 바꿔 직접 플레이한 저장을 건드리지 않음.
    /// 쓰기 · 읽기 규칙은 SaveCodec, 수치는 SaveRules(BALANCE.md 19번).
    /// </summary>
    [DefaultExecutionOrder(200)]  // 다른 것들이 모두 시작한 뒤 불러오고, 아침 처리 알림도 마지막에 받음
    public class SaveManager : MonoBehaviour
    {
        /// <summary>테스트가 저장 폴더를 바꿀 때(씬을 열기 전에). null이면 persistentDataPath</summary>
        public static string PathOverride;

        public SaveRules rules;
        public DayCycle dayCycle;
        public MorningRunner morning;
        public PlayerWallet wallet;
        public PlayerInventory inventory;
        [Tooltip("물건 id → 데이터(인벤토리에 들어가는 모든 물건이 있는 목록)")]
        public ShopData catalogSource;
        public FishEncyclopedia fishEncyclopedia;
        public InsectEncyclopedia insectEncyclopedia;
        public VillageRater rater;
        public FurnitureCounter furnitureCounter;
        [Header("마을 상태 (6-1b)")]
        public Village village;
        public GroundSaver groundSaver;
        [Tooltip("수조 · 채집통 가구를 찾을 때(상점 목록에는 없음)")]
        public FishingRules fishingRules;
        public InsectRules insectRules;
        [Header("그날 정해진 것 · 난수 (6-1c)")]
        public FishShadows fishShadows;
        public Insects insects;
        public FishingRodUser fishingRod;

        public string Folder => string.IsNullOrEmpty(PathOverride) ? Application.persistentDataPath : PathOverride;
        public string FilePath => Path.Combine(Folder, rules.fileName);
        public string BackupPath => Path.Combine(Folder, rules.backupFileName);
        public bool Loaded { get; private set; }

        void Start()
        {
            if (!rules || !dayCycle || dayCycle.Clock == null || !morning || !wallet || !inventory || inventory.Inventory == null
                || !catalogSource || !fishEncyclopedia || !insectEncyclopedia || !rater || !furnitureCounter
                || !village || !groundSaver || !fishingRules || !insectRules || !fishShadows || !insects || !fishingRod)
            {
                Debug.LogError($"{name}: 세이브 연결이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            morning.MorningPassed += SaveNow;
        }

        /// <summary>저장이 있으면 그 내용(시작 화면이 요약을 보임, 6-1d), 없으면 null</summary>
        public SaveData Peek() => enabled ? SaveCodec.Read(FilePath, BackupPath) : null;

        /// <summary>이어하기(시작 화면 6-1d)</summary>
        public bool Continue()
        {
            var data = Peek();
            if (data == null)
                return false;
            Load(data);
            return true;
        }

        /// <summary>새로 시작(시작 화면에서 확인한 뒤, 6-1d): 저장 · 백업을 지움. 지금 마을이 처음 마을</summary>
        public void DeleteSave()
        {
            foreach (string path in new[] { FilePath, BackupPath, FilePath + ".tmp" })
                if (File.Exists(path)) File.Delete(path);
            GameNotice.Post("[세이브] 새 마을에서 시작해요", this);
        }

        void OnDestroy()
        {
            if (morning) morning.MorningPassed -= SaveNow;
        }

        public SaveData Capture()
        {
            var data = new SaveData
            {
                day = dayCycle.Clock.Day,
                moa = wallet.Wallet.Moa,
                slots = SaveCodec.SaveSlots(inventory.Inventory.Slots),
                fishCaught = Counts(fishEncyclopedia.Log),
                insectCaught = Counts(insectEncyclopedia.Log),
                firstGoalReached = rater.GoalReached,
                bestStars = furnitureCounter.BestStars,
                stars = rater.Current.stars,
                village = village.Capture(),
                ground = groundSaver.Capture(),
                furniture = groundSaver.CaptureFurniture(),
                today = new TodaySave
                {
                    theme = furnitureCounter.TodayTheme ? furnitureCounter.TodayTheme.id : "",
                    shadows = fishShadows.CaptureToday(),
                    insects = insects.CaptureToday(),
                },
            };
            village.CaptureRandoms(data.randoms);
            RandomData.Add(data.randoms, "nature", groundSaver.garden.NatureRandom);
            data.randoms.Add(new RandomData { key = "fish", state = fishShadows.RandomState });
            data.randoms.Add(new RandomData { key = "insects", state = insects.RandomState });
            data.randoms.Add(new RandomData { key = "theme", state = furnitureCounter.ThemeRandomState });
            data.randoms.Add(new RandomData { key = "rod", state = fishingRod.RandomState });
            return SaveCodec.Normalize(data);
        }

        /// <summary>아침 처리가 끝난 뒤(자고 일어남) 자동 저장</summary>
        public void SaveNow()
        {
            if (!enabled)
                return;
            try
            {
                SaveCodec.Write(FilePath, BackupPath, SaveCodec.ToJson(Capture()));
                GameNotice.Post($"[세이브] {dayCycle.Clock.Day}일차 아침을 저장했어요", this);
            }
            catch (IOException e)
            {
                Debug.LogError($"[세이브] 저장하지 못했어요: {e.Message}", this);
                GameNotice.Post("[세이브] 저장하지 못했어요", this);
            }
        }

        /// <summary>불러오기: 그 날짜 아침 상태로(없어진 물건은 건너뛰고 알림)</summary>
        public void Load(SaveData data)
        {
            var skipped = new List<string>();
            dayCycle.Clock.Restore(data.day);
            wallet.Wallet.Restore(data.moa);
            inventory.Inventory.Restore(SaveCodec.RestoreSlots(data.slots, inventory.Inventory.Slots.Count, Known, skipped));
            Restore(fishEncyclopedia.Log, data.fishCaught);
            Restore(insectEncyclopedia.Log, data.insectCaught);
            village.Restore(data.village ?? new VillageSave(), skipped);
            groundSaver.Restore(data.ground ?? new GroundSave(), data.furniture ?? new List<PlacedData>(), FindItem, FindFurniture, skipped);
            var today = data.today ?? new TodaySave();
            furnitureCounter.RestoreTodayTheme(today.theme);
            fishShadows.RestoreToday(today.shadows);
            insects.RestoreToday(today.insects);
            RestoreRandoms(data.randoms ?? new List<RandomData>());  // 그날 것을 다시 놓은 뒤에(놓으며 쓴 난수를 덮어씀)
            rater.Restore(data.firstGoalReached);
            furnitureCounter.RestoreBestStars(data.bestStars);
            Loaded = true;
            GameNotice.Post($"[세이브] {data.day}일차 아침부터 이어서 해요", this);
            if (skipped.Count > 0)
                GameNotice.Post($"[세이브] 없어진 물건 {skipped.Count}가지는 건너뛰었어요", this);  // 물건 · 주민 · 가구
            Debug.Log(skipped.Count > 0 ? $"[세이브] 건너뜀: {string.Join(", ", skipped)}" : "[세이브] 모두 불러옴", this);
        }

        void RestoreRandoms(List<RandomData> list)
        {
            village.RestoreRandoms(list);
            RandomData.Restore(list, "nature", groundSaver.garden.NatureRandom);
            foreach (var r in list)
            {
                if (string.IsNullOrEmpty(r.state)) continue;
                switch (r.key)
                {
                    case "fish": fishShadows.RandomState = r.state; break;
                    case "insects": insects.RandomState = r.state; break;
                    case "theme": furnitureCounter.ThemeRandomState = r.state; break;
                    case "rod": fishingRod.RandomState = r.state; break;
                }
            }
        }

        bool Known(string id) => FindItem(id);

        ItemDefinition FindItem(string id) => Array.Find(catalogSource.catalog, i => i && i.id == id);

        FurnitureDefinition FindFurniture(string id)
        {
            if (FindItem(id) is FurnitureDefinition f) return f;
            foreach (var fish in fishingRules.fish)
                if (fish && fish.aquarium && fish.aquarium.id == id) return fish.aquarium;
            foreach (var insect in insectRules.insects)
                if (insect && insect.cage && insect.cage.id == id) return insect.cage;
            return null;
        }

        static List<CountData> Counts(CatchLog log)
        {
            var list = new List<CountData>();
            foreach (var e in log.Entries)
                list.Add(new CountData { id = e.Key, count = e.Value });
            return list;
        }

        static void Restore(CatchLog log, List<CountData> saved)
        {
            foreach (var c in saved)
                log.Restore(c.id, c.count);
        }
    }
}
