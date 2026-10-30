using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // P-5 인트로 시간표: 장면 경계 · 진행 · 길이 0 장면 건너뛰기 · 장면 시작 시각 · 감속
    public class IntroTimelineTests
    {
        static IntroTimeline Make(params (IntroPhase, float)[] steps) => new(steps);

        [Test]
        public void Phases_FollowDurations()
        {
            var timeline = Make((IntroPhase.Village, 1.5f), (IntroPhase.PullOut, 1f), (IntroPhase.Room, 1.5f));
            Assert.AreEqual(4f, timeline.Total, 1e-5f);
            Assert.AreEqual(IntroPhase.Village, timeline.PhaseAt(0f));
            Assert.AreEqual(IntroPhase.Village, timeline.PhaseAt(1.49f));
            Assert.AreEqual(IntroPhase.PullOut, timeline.PhaseAt(1.5f));
            Assert.AreEqual(0.5f, timeline.ProgressAt(2f), 1e-5f);
            Assert.AreEqual(IntroPhase.Room, timeline.PhaseAt(2.5f));
            Assert.AreEqual(IntroPhase.Done, timeline.PhaseAt(4f));
            Assert.AreEqual(1f, timeline.ProgressAt(10f));
        }

        [Test]
        public void ZeroDuration_Skipped_StartOf()
        {
            var timeline = Make((IntroPhase.Village, 0f), (IntroPhase.PullOut, 1f), (IntroPhase.Click, -2f), (IntroPhase.Dive, 0.5f));
            Assert.AreEqual(IntroPhase.PullOut, timeline.PhaseAt(0f));
            Assert.AreEqual(IntroPhase.Dive, timeline.PhaseAt(1f), "음수 길이는 0 — 건너뜀");
            Assert.AreEqual(1f, timeline.StartOf(IntroPhase.Dive), 1e-5f);
            Assert.AreEqual(1.5f, timeline.StartOf(IntroPhase.Fall), 1e-5f, "없는 장면 = 끝");
        }

        [Test]
        public void Easing_Shapes()
        {
            Assert.AreEqual(0f, IntroTimeline.EaseOutCubic(0f));
            Assert.AreEqual(1f, IntroTimeline.EaseOutCubic(1f));
            Assert.Greater(IntroTimeline.EaseOutCubic(0.5f), 0.8f, "뒤로 빠지기: 앞쪽에서 대부분 움직임");
            Assert.Less(IntroTimeline.EaseInCubic(0.5f), 0.2f, "돌진: 끝에서 빠름");
            Assert.AreEqual(0.5f, IntroTimeline.EaseInOutSine(0.5f), 1e-5f);
        }
    }
}
