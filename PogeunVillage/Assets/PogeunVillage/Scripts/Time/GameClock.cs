using System;

namespace PogeunVillage
{
    /// <summary>시간대. enum은 끝에만 추가.</summary>
    public enum DayPeriod
    {
        Morning,
        Daytime,
        Evening,
        Night,
    }

    /// <summary>
    /// 게임 시계(엔진 상태 없음, 실제 시계와 무관 — CLAUDE.md 핵심 설계 규칙 3). 수치는 BALANCE.md 12번 · DayRules.
    /// 결정 (2026-10-27): 아침 dayStartHour시에 시작, 실제 realSecondsPerGameMinute초 = 게임 1분(기본 1초 → 하루 6시~2시 20시간 = 실제 20분).
    /// 자지 않으면 시계는 계속 감(새벽 2시 뒤에도). 다음 날은 Sleep으로만 옴 → 날짜 +1, 다시 아침 시작 시각.
    /// 시간대 경계(아침 · 낮 · 저녁 · 밤 시작 시각)는 데이터. 바뀐 것만 알림(MinuteChanged: 게임 1분마다, PeriodChanged: 시간대가 바뀔 때).
    /// </summary>
    public class GameClock
    {
        readonly int dayStartHour;
        readonly double realSecondsPerGameMinute;
        readonly int[] periodStartHours;  // Morning, Daytime, Evening, Night 시작 시각

        double minutesSinceDayStart;

        public int Day { get; private set; } = 1;
        public DayPeriod Period { get; private set; }

        public event Action<int> MinuteChanged;       // 오늘 시작부터 지난 게임 분
        public event Action<DayPeriod> PeriodChanged;
        public event Action<int> DayStarted;          // 새 날짜(잠에서 깸)

        public GameClock(int dayStartHour, double realSecondsPerGameMinute, int[] periodStartHours)
        {
            if (dayStartHour < 0 || dayStartHour > 23) throw new ArgumentOutOfRangeException(nameof(dayStartHour));
            if (realSecondsPerGameMinute <= 0) throw new ArgumentOutOfRangeException(nameof(realSecondsPerGameMinute));
            if (periodStartHours == null || periodStartHours.Length != 4) throw new ArgumentException("시간대 시작 시각은 4개(아침 · 낮 · 저녁 · 밤)", nameof(periodStartHours));
            this.dayStartHour = dayStartHour;
            this.realSecondsPerGameMinute = realSecondsPerGameMinute;
            this.periodStartHours = periodStartHours;
            Period = PeriodAt(HourOfDay);
        }

        /// <summary>오늘 시작부터 지난 게임 분(정수)</summary>
        public int MinutesSinceDayStart => (int)minutesSinceDayStart;

        /// <summary>0 ~ 23</summary>
        public int HourOfDay => (dayStartHour + MinutesSinceDayStart / 60) % 24;
        public int Minute => MinutesSinceDayStart % 60;

        public string TimeText => $"{HourOfDay:00}:{Minute:00}";

        public void Advance(double realSeconds)
        {
            if (realSeconds <= 0)
                return;
            int before = MinutesSinceDayStart;
            minutesSinceDayStart += realSeconds / realSecondsPerGameMinute;
            if (MinutesSinceDayStart != before)
                OnMinuteChanged();
        }

        /// <summary>잠자기: 날짜 +1, 아침 시작 시각으로</summary>
        public void Sleep()
        {
            Day++;
            minutesSinceDayStart = 0;
            OnMinuteChanged();
            DayStarted?.Invoke(Day);
        }

        void OnMinuteChanged()
        {
            MinuteChanged?.Invoke(MinutesSinceDayStart);
            var period = PeriodAt(HourOfDay);
            if (period != Period)
            {
                Period = period;
                PeriodChanged?.Invoke(period);
            }
        }

        /// <summary>그 시각(0~23)의 시간대. 밤은 자정을 넘어 아침 시작 전까지</summary>
        public DayPeriod PeriodAt(int hour)
        {
            for (int i = periodStartHours.Length - 1; i >= 0; i--)
            {
                int start = periodStartHours[i];
                int next = periodStartHours[(i + 1) % periodStartHours.Length];
                bool inRange = start < next ? hour >= start && hour < next : hour >= start || hour < next;
                if (inRange)
                    return (DayPeriod)i;
            }
            return DayPeriod.Night;
        }
    }
}
