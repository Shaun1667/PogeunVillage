using System;
using System.Collections.Generic;

namespace PogeunVillage
{
    public enum BitePhase
    {
        Approaching,   // 그림자가 찌로 다가옴
        Waiting,       // 다가와서 기다림(속임수 사이)
        Nibble,        // 톡 — 이때 누르면 놓침
        Bite,          // 쑥 — 이때 누르면 낚음
        Gone,          // 늦음 — 도망
    }

    public enum CatchResult
    {
        Caught,
        Spooked,   // 입질 전에 누름
        TooLate,   // 입질 뒤 시간을 넘김
    }

    /// <summary>
    /// 한 번 던졌을 때의 입질 시간표(엔진 상태 없음, 결정 2026-10-29 · 3-6a): 다가옴 → 속임수 0 ~ N번(간격 무작위) → 입질 → 짧은 시간 안에 누르면 낚음.
    /// 시각은 실제 초. 수치는 FishingRules(BALANCE.md 17번).
    /// </summary>
    public class BiteTimeline
    {
        /// <summary>속임수가 찌를 건드리는 모습이 보이는 시간(초)</summary>
        public const float NibbleShowSeconds = 0.25f;

        readonly List<float> nibbles;

        public float ApproachEnd { get; }
        public IReadOnlyList<float> NibbleTimes => nibbles;
        public float BiteTime { get; }
        public float BiteEnd { get; }

        public BiteTimeline(float approachEnd, IEnumerable<float> nibbleTimes, float biteTime, float biteWindow)
        {
            ApproachEnd = approachEnd;
            nibbles = new List<float>(nibbleTimes);
            BiteTime = biteTime;
            BiteEnd = biteTime + biteWindow;
        }

        public static BiteTimeline Create(float start, float approachSeconds, int nibbleMin, int nibbleMax,
            float intervalMin, float intervalMax, float biteWindow, IRandomSource random)
        {
            float t = start + approachSeconds;
            float approachEnd = t;
            int count = nibbleMin + random.Next(Math.Max(0, nibbleMax - nibbleMin) + 1);
            var times = new List<float>();
            for (int i = 0; i < count; i++)
            {
                t += Interval(intervalMin, intervalMax, random);
                times.Add(t);
            }
            t += Interval(intervalMin, intervalMax, random);
            return new BiteTimeline(approachEnd, times, t, biteWindow);
        }

        static float Interval(float min, float max, IRandomSource random) => min + (float)random.NextDouble() * Math.Max(0f, max - min);

        public BitePhase PhaseAt(float time)
        {
            if (time < ApproachEnd) return BitePhase.Approaching;
            if (time >= BiteEnd) return BitePhase.Gone;
            if (time >= BiteTime) return BitePhase.Bite;
            foreach (float n in nibbles)
                if (time >= n && time < n + NibbleShowSeconds)
                    return BitePhase.Nibble;
            return BitePhase.Waiting;
        }

        /// <summary>그 시각에 Space를 누르면</summary>
        public CatchResult Press(float time) =>
            time < BiteTime ? CatchResult.Spooked : time < BiteEnd ? CatchResult.Caught : CatchResult.TooLate;
    }
}
