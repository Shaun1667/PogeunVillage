using System;
using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>인트로 장면(P-5). 끝에만 추가.</summary>
    public enum IntroPhase
    {
        Village,
        PullOut,
        Room,
        Done,
    }

    /// <summary>
    /// 인트로 시간표(엔진 상태 없음, P-5): 장면마다 길이를 받아 흐른 시간 → 지금 장면 · 그 장면 안 진행(0~1).
    /// 길이 0인 장면은 건너뜀.
    /// </summary>
    public class IntroTimeline
    {
        readonly float[] durations;

        public IntroTimeline(IReadOnlyList<float> durations)
        {
            this.durations = new float[durations.Count];
            for (int i = 0; i < durations.Count; i++)
                this.durations[i] = Math.Max(0f, durations[i]);
        }

        public float Total
        {
            get
            {
                float sum = 0f;
                foreach (float d in durations) sum += d;
                return sum;
            }
        }

        public IntroPhase PhaseAt(float time) => At(time).phase;
        public float ProgressAt(float time) => At(time).progress;

        (IntroPhase phase, float progress) At(float time)
        {
            float start = 0f;
            for (int i = 0; i < durations.Length; i++)
            {
                float end = start + durations[i];
                if (durations[i] > 0f && time < end)
                    return ((IntroPhase)i, Math.Clamp((time - start) / durations[i], 0f, 1f));
                start = end;
            }
            return (IntroPhase.Done, 1f);
        }

        /// <summary>끝에서 감속(뒤로 빠지기)</summary>
        public static float EaseOutCubic(float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            float u = 1f - t;
            return 1f - u * u * u;
        }
    }
}
