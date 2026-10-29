using System.IO;
using System.Text;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 플레이 기록(결정 2026-10-29, 4-1 재미 검증 측정 도구): 에디터 · 개발 빌드에서만. 게임 1시간마다 상태(모아 · 별 · 점수 · 주민 ·
    /// 친밀도 합 · 인벤토리 칸 · 목재 · 놓인 가구)와, 알림(판매 · 제작 · 선물 · 부탁 · 이주 …)이 뜰 때마다 한 줄씩
    /// Temp/PlayLogs/PlayLog_날짜_시각.csv에 씀. 플레이마다 새 파일. 짧은 실행(테스트 등)이 파일을 남기지 않도록
    /// 처음 minSessionSeconds 동안은 메모리에 모았다가 그 뒤에 파일을 만듦(PlayMode 테스트는 이 컴포넌트를 끔).
    /// </summary>
    public class PlayLogger : MonoBehaviour
    {
        public DayCycle dayCycle;
        public PlayerWallet wallet;
        public PlayerInventory inventory;
        public Village village;
        public VillageRater rater;
        public FurnitureKeeper furniture;
        [Tooltip("목재 수를 셀 물건")]
        public ItemDefinition wood;
        [Tooltip("이보다 짧은 실행은 파일을 남기지 않음(실제 초)")]
        public float minSessionSeconds = 30f;

        public string FilePath { get; private set; }
        public int Rows { get; private set; }

        StreamWriter writer;
        readonly System.Collections.Generic.List<string> pending = new();
        float startTime;
        int lastHour = -1;

        void Start()
        {
            if (!Application.isEditor && !Debug.isDebugBuild || !dayCycle || dayCycle.Clock == null || !wallet || !inventory || !village || !rater || !furniture || !wood)
            {
                enabled = false;
                return;
            }
            startTime = Time.unscaledTime;
            GameNotice.Posted += OnNotice;
            dayCycle.Clock.MinuteChanged += OnMinute;
            dayCycle.Clock.DayStarted += OnDay;
        }

        void OnDisable() => Stop();
        void OnDestroy() => Stop();

        void Stop()
        {
            GameNotice.Posted -= OnNotice;
            if (dayCycle && dayCycle.Clock != null)
            {
                dayCycle.Clock.MinuteChanged -= OnMinute;
                dayCycle.Clock.DayStarted -= OnDay;
            }
            pending.Clear();  // 짧은 실행은 버림
            if (writer == null)
                return;
            writer.Flush();
            writer.Dispose();
            writer = null;
            Debug.Log($"[기록] 플레이 기록 {Rows}줄: {FilePath}");
        }

        void OnMinute(int _)
        {
            int hour = dayCycle.Clock.HourOfDay;
            if (hour == lastHour)
                return;
            lastHour = hour;
            Write("hour", "");
        }

        void OnDay(int day) => Write("day", $"{day}일차 아침");

        void OnNotice(string message) => Write("notice", message);

        void Write(string kind, string text)
        {
            if (!enabled)
                return;
            int friendshipSum = 0;
            if (village.Residents != null && village.Friendship != null)
                foreach (string id in village.Residents.Plots)
                    if (id != null) friendshipSum += village.Friendship.Get(id);
            var clock = dayCycle.Clock;
            var row = new PlayLogRow(Time.unscaledTime, clock.Day, clock.TimeText, kind,
                wallet.Wallet.Moa, rater.Current.stars, rater.Current.total, village.Residents?.Count ?? 0, friendshipSum,
                inventory.Inventory.UsedSlots, inventory.Inventory.Count(wood.id), furniture.Placed.Count, text);
            pending.Add(PlayLog.Row(row));
            Rows++;
            if (writer == null && Time.unscaledTime - startTime < minSessionSeconds)
                return;
            if (writer == null)
            {
                string dir = Path.Combine(Application.dataPath, "..", "Temp", "PlayLogs");
                Directory.CreateDirectory(dir);
                FilePath = Path.GetFullPath(Path.Combine(dir, $"PlayLog_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv"));
                writer = new StreamWriter(FilePath, false, new UTF8Encoding(true)) { AutoFlush = true };
                writer.WriteLine(PlayLog.Header);
            }
            foreach (string line in pending)
                writer.WriteLine(line);
            pending.Clear();
        }
    }
}
