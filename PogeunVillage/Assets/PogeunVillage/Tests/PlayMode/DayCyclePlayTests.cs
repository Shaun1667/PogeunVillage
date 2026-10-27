using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>2-1 게임 시간 · 하루 루프 확인 (실제 1초 = 게임 1분 · 침대 F로 자면 다음 날 6시 + 아침 처리 · F8 · 밤이 계속됨 · 시간대 알림 · 일시정지).</summary>
    public class DayCyclePlayTests : VillageSceneTest
    {
        DayCycle dayCycle;
        MorningRunner morning;
        PlayerInteractor interactor;
        GameClock Clock => dayCycle.Clock;

        [UnitySetUp]
        public IEnumerator FindParts()
        {
            dayCycle = Object.FindAnyObjectByType<DayCycle>();
            morning = Object.FindAnyObjectByType<MorningRunner>();
            interactor = player.GetComponent<PlayerInteractor>();
            Assert.IsNotNull(dayCycle);
            yield break;
        }

        [TearDown]
        public void RestoreTimeScale() => Time.timeScale = 1f;

        IEnumerator Tap(ButtonControl button)
        {
            Press(button);
            yield return null;
            Release(button);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Clock_StartsAtSix_AndRunsWithRealTime()
        {
            Assert.AreEqual(1, Clock.Day);
            Assert.AreEqual(6, Clock.HourOfDay);
            int before = Clock.MinutesSinceDayStart;
            float t0 = Time.time;
            yield return WaitSeconds(2f);
            float expected = (Time.time - t0) / dayCycle.rules.realSecondsPerGameMinute;
            Assert.AreEqual(expected, Clock.MinutesSinceDayStart - before, 1.5f, "실제 1초 = 게임 1분");
        }

        [UnityTest]
        public IEnumerator Bed_F_SleepsToNextMorning_AndRunsMorning()
        {
            Clock.Advance(5 * 60);  // 11시
            int mornings = 0;
            morning.MorningPassed += () => mornings++;

            var bed = Object.FindAnyObjectByType<Bed>().transform;
            Vector3 spot = bed.position + bed.right * 1.3f;
            Teleport(spot, Vector3.ProjectOnPlane(bed.position - spot, Vector3.up));
            yield return Frames(3);
            Assert.AreEqual(InteractableKind.Bed, interactor.Target ? interactor.Target.kind : InteractableKind.None, "침대가 대상");

            yield return Tap(keyboard.fKey);
            Assert.AreEqual(2, Clock.Day, "다음 날");
            Assert.AreEqual(6, Clock.HourOfDay, "아침 6시");
            Assert.Less(Clock.MinutesSinceDayStart, 2);
            Assert.AreEqual(1, mornings, "아침 처리 한 번");
        }

        [UnityTest]
        public IEnumerator F8_AlsoSleeps()
        {
            int mornings = 0;
            morning.MorningPassed += () => mornings++;
            yield return Tap(keyboard.f8Key);
            Assert.AreEqual(2, Clock.Day);
            Assert.AreEqual(1, mornings);
        }

        [UnityTest]
        public IEnumerator NotSleeping_PastTwoAm_SameDayClockKeepsGoing()
        {
            Clock.Advance(21 * 60);  // 새벽 3시
            Assert.AreEqual(3, Clock.HourOfDay);
            int before = Clock.MinutesSinceDayStart;
            yield return WaitSeconds(1.5f);
            Assert.Greater(Clock.MinutesSinceDayStart, before, "시계가 계속 감");
            Assert.AreEqual(1, Clock.Day, "자야만 다음 날");
        }

        [UnityTest]
        public IEnumerator PeriodChanged_FiresOnceAtTen()
        {
            Clock.Advance(4 * 60 - 1);  // 9:59
            Assert.AreEqual(DayPeriod.Morning, Clock.Period);
            int changes = 0;
            DayPeriod last = Clock.Period;
            Clock.PeriodChanged += p => { changes++; last = p; };
            yield return WaitSeconds(2.5f * dayCycle.rules.realSecondsPerGameMinute);
            Assert.AreEqual(1, changes);
            Assert.AreEqual(DayPeriod.Daytime, last);
        }

        [UnityTest]
        public IEnumerator Paused_ClockStops()
        {
            Time.timeScale = 0f;
            int before = Clock.MinutesSinceDayStart;
            float end = Time.realtimeSinceStartup + 1.5f;
            while (Time.realtimeSinceStartup < end)
                yield return null;
            Assert.AreEqual(before, Clock.MinutesSinceDayStart, "일시정지 중엔 안 감");
        }
    }
}
