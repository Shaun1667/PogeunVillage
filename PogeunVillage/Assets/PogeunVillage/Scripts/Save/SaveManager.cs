using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 세이브 어댑터(결정 2026-10-29, GDD 4-13 · 6-1a): 자고 일어나 아침 처리가 모두 끝나면 자동 저장("저장했어요").
    /// 게임을 시작할 때 저장이 있으면 그 날짜 아침으로 불러옴(시작 화면은 6-1d — 그때까지는 자동 이어하기).
    /// 지금 저장하는 것: 날짜 · 모아 · 인벤토리 · 도감 2가지 · 평가 기록(6-1b · 6-1c에서 더함).
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

        public string Folder => string.IsNullOrEmpty(PathOverride) ? Application.persistentDataPath : PathOverride;
        public string FilePath => Path.Combine(Folder, rules.fileName);
        public string BackupPath => Path.Combine(Folder, rules.backupFileName);
        public bool Loaded { get; private set; }

        void Start()
        {
            if (!rules || !dayCycle || dayCycle.Clock == null || !morning || !wallet || !inventory || inventory.Inventory == null
                || !catalogSource || !fishEncyclopedia || !insectEncyclopedia || !rater || !furnitureCounter)
            {
                Debug.LogError($"{name}: 세이브 연결이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            morning.MorningPassed += SaveNow;
            var data = SaveCodec.Read(FilePath, BackupPath);
            if (data != null)
                Load(data);
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
            };
            return data;
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
            rater.Restore(data.firstGoalReached);
            furnitureCounter.RestoreBestStars(data.bestStars);
            Loaded = true;
            GameNotice.Post($"[세이브] {data.day}일차 아침부터 이어서 해요", this);
            if (skipped.Count > 0)
                GameNotice.Post($"[세이브] 없어진 물건 {skipped.Count}가지는 건너뛰었어요", this);
        }

        bool Known(string id) => Array.Exists(catalogSource.catalog, i => i && i.id == id);

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
