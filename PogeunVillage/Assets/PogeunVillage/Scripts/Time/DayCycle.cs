using UnityEngine;
using UnityEngine.InputSystem;

namespace PogeunVillage
{
    /// <summary>
    /// 게임 시계(GameClock) 어댑터: 프레임 시간(일시정지 · 시간 배율 반영)만큼 시계를 돌리고,
    /// 잠자기(침대 · 개발용 F8)면 다음 날 아침 시작 시각으로 넘기고 아침 처리(MorningRunner)를 부릅니다.
    /// 시간대가 바뀔 때만 콘솔에 알림. 시계 표시는 2-3 HUD에서 Clock 이벤트를 구독.
    /// </summary>
    public class DayCycle : MonoBehaviour
    {
        public DayRules rules;
        public MorningRunner morning;

        public GameClock Clock { get; private set; }
        public int Day => Clock?.Day ?? 1;

        void Awake()
        {
            if (!rules || !morning)
            {
                Debug.LogError($"{name}: DayRules 또는 MorningRunner가 연결되지 않았습니다.", this);
                enabled = false;
                return;
            }
            Clock = new GameClock(rules.dayStartHour, rules.realSecondsPerGameMinute, rules.periodStartHours);
            Clock.PeriodChanged += p => Debug.Log($"[시계] {Clock.Day}일차 {Clock.TimeText} — {p}", this);
        }

        void Update()
        {
            Clock.Advance(Time.deltaTime);
            // 개발용 F8(에디터 · 개발 빌드에서만)
            if (Debug.isDebugBuild && Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
                Sleep();
        }

        /// <summary>자고 일어남: 날짜 +1, 아침 시작 시각, 아침 처리</summary>
        public void Sleep()
        {
            Clock.Sleep();
            morning.RunMorning();
            Debug.Log($"[잠] {Clock.Day}일차 아침 {Clock.TimeText}", this);
        }
    }
}
