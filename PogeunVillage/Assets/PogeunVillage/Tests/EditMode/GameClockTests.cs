using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // BALANCE.md 12번 검산 케이스 (6시 시작, 실제 1초 = 게임 1분, 시간대 6 · 10 · 17 · 20시)
    public class GameClockTests
    {
        static readonly int[] Periods = { 6, 10, 17, 20 };
        static GameClock Fresh() => new(6, 1.0, Periods);

        [Test]
        public void StartsAtSixOnDayOne()  // 12-1
        {
            var clock = Fresh();
            Assert.AreEqual(1, clock.Day);
            Assert.AreEqual("06:00", clock.TimeText);
            Assert.AreEqual(DayPeriod.Morning, clock.Period);
        }

        [Test]
        public void OneRealMinute_IsOneGameHour()  // 12-2
        {
            var clock = Fresh();
            clock.Advance(60);
            Assert.AreEqual("07:00", clock.TimeText);
        }

        [Test]
        public void TwentyRealMinutes_ReachTwoAm_SameDay()  // 12-3
        {
            var clock = Fresh();
            clock.Advance(20 * 60);
            Assert.AreEqual("02:00", clock.TimeText);
            Assert.AreEqual(1, clock.Day, "자기 전엔 날짜가 안 바뀜");
            Assert.AreEqual(DayPeriod.Night, clock.Period);
        }

        [Test]
        public void NotSleeping_ClockKeepsGoing()  // 12-4 (결정 2026-10-27: 밤이 계속됨)
        {
            var clock = Fresh();
            clock.Advance(23 * 60);
            Assert.AreEqual("05:00", clock.TimeText);
            clock.Advance(2 * 60);
            Assert.AreEqual("07:00", clock.TimeText);
            Assert.AreEqual(1, clock.Day);
        }

        [Test]
        public void Sleep_NextDayAtSix()  // 12-5
        {
            var clock = Fresh();
            clock.Advance(15 * 60);
            int startedDay = 0;
            clock.DayStarted += d => startedDay = d;
            clock.Sleep();
            Assert.AreEqual(2, clock.Day);
            Assert.AreEqual(2, startedDay);
            Assert.AreEqual("06:00", clock.TimeText);
            Assert.AreEqual(DayPeriod.Morning, clock.Period);
        }

        [TestCase(6, DayPeriod.Morning)]
        [TestCase(9, DayPeriod.Morning)]
        [TestCase(10, DayPeriod.Daytime)]
        [TestCase(16, DayPeriod.Daytime)]
        [TestCase(17, DayPeriod.Evening)]
        [TestCase(20, DayPeriod.Night)]
        [TestCase(0, DayPeriod.Night)]
        [TestCase(5, DayPeriod.Night)]
        public void PeriodBoundaries(int hour, DayPeriod expected)  // 12-6
        {
            Assert.AreEqual(expected, Fresh().PeriodAt(hour));
        }

        [Test]
        public void Events_OnlyOnChanges()
        {
            var clock = Fresh();
            var minutes = new List<int>();
            var periods = new List<DayPeriod>();
            clock.MinuteChanged += minutes.Add;
            clock.PeriodChanged += periods.Add;

            clock.Advance(0.4);
            clock.Advance(0.4);   // 0.8분 → 아직 0분
            Assert.IsEmpty(minutes);
            clock.Advance(0.4);   // 1.2분
            CollectionAssert.AreEqual(new[] { 1 }, minutes);

            clock.Advance(4 * 60);  // 10:01 → 낮
            CollectionAssert.AreEqual(new[] { DayPeriod.Daytime }, periods);
        }

        [Test]
        public void BadInput_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameClock(24, 1, Periods));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameClock(6, 0, Periods));
            Assert.Throws<ArgumentException>(() => new GameClock(6, 1, new[] { 6, 10 }));
        }
    }
}
