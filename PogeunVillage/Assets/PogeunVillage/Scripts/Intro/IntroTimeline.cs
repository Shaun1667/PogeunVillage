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
        Click,  // P-5b: 화면 속 커서가 '게임 시작'으로 가서 누름
        Dive,   // P-5b: 모니터로 돌진 · 섬광
        Fall,   // P-5b: 마을로 컷 · 섬광이 걷힘 / P-5c: 플레이어가 하늘에서 떨어짐
        Land,   // P-5c: 착지(찌그러졌다 펴짐 · 먼지)
        Settle, // P-5c: 카메라가 평소 따라가는 자리로
    }

    /// <summary>
    /// 인트로 시간표(엔진 상태 없음, P-5): (장면, 길이) 차례대로 받아 흐른 시간 → 지금 장면 · 그 장면 안 진행(0~1).
    /// 길이 0 이하인 장면은 건너뜀, 다 지나면 Done.
    /// </summary>
    public class IntroTimeline
    {
        readonly (IntroPhase phase, float duration)[] steps;

        public IntroTimeline(IReadOnlyList<(IntroPhase phase, float duration)> steps)
        {
            this.steps = new (IntroPhase, float)[steps.Count];
            for (int i = 0; i < steps.Count; i++)
                this.steps[i] = (steps[i].phase, Math.Max(0f, steps[i].duration));
        }

        public float Total
        {
            get
            {
                float sum = 0f;
                foreach (var step in steps) sum += step.duration;
                return sum;
            }
        }

        /// <summary>그 장면이 시작하는 시각(없으면 Total)</summary>
        public float StartOf(IntroPhase phase)
        {
            float start = 0f;
            foreach (var step in steps)
            {
                if (step.phase == phase && step.duration > 0f)
                    return start;
                start += step.duration;
            }
            return start;
        }

        public IntroPhase PhaseAt(float time) => At(time).phase;
        public float ProgressAt(float time) => At(time).progress;

        (IntroPhase phase, float progress) At(float time)
        {
            float start = 0f;
            foreach (var step in steps)
            {
                float end = start + step.duration;
                if (step.duration > 0f && time < end)
                    return (step.phase, Math.Clamp((time - start) / step.duration, 0f, 1f));
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

        /// <summary>떨어지기(중력처럼 점점 빠름)</summary>
        public static float EaseInQuad(float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return t * t;
        }

        /// <summary>착지 찌그러짐(키 배율): 처음 squash → 살짝 늘었다가 → 1</summary>
        public static float LandSquash(float t, float squash)
        {
            t = Math.Clamp(t, 0f, 1f);
            return 1f - (1f - squash) * (float)Math.Cos(t * Math.PI * 1.5) * (1f - t);
        }

        /// <summary>처음에 느리고 끝에서 빠름(돌진)</summary>
        public static float EaseInCubic(float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return t * t * t;
        }

        /// <summary>양 끝 감속(커서 이동)</summary>
        public static float EaseInOutSine(float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return 0.5f - 0.5f * (float)Math.Cos(t * Math.PI);
        }
    }
}
