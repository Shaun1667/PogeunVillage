using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // P-5 인트로 시간표: 장면 경계 · 진행 · 길이 0 장면 건너뛰기 · 감속
    public class IntroTimelineTests
    {
        [Test]
        public void Phases_FollowDurations()
        {
            var timeline = new IntroTimeline(new[] { 1.5f, 1f, 1.5f });
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
        public void ZeroDuration_Skipped()
        {
            var timeline = new IntroTimeline(new[] { 0f, 1f, -2f });
            Assert.AreEqual(IntroPhase.PullOut, timeline.PhaseAt(0f));
            Assert.AreEqual(IntroPhase.Done, timeline.PhaseAt(1f), "음수 길이는 0");
        }

        [Test]
        public void EaseOutCubic_StartsFastEndsSlow()
        {
            Assert.AreEqual(0f, IntroTimeline.EaseOutCubic(0f));
            Assert.AreEqual(1f, IntroTimeline.EaseOutCubic(1f));
            Assert.Greater(IntroTimeline.EaseOutCubic(0.5f), 0.8f, "앞쪽에서 대부분 움직임");
        }
    }
}
