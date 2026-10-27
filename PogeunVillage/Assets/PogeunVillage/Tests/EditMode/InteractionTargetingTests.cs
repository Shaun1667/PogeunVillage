using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PogeunVillage.Tests
{
    public class InteractionTargetingTests
    {
        const float Range = 1f;
        const float HalfAngle = 60f;
        static readonly Vector2 Origin = Vector2.zero;
        static readonly Vector2 Forward = Vector2.up;

        static int Select(params InteractionCandidate[] candidates) =>
            InteractionTargeting.Select(candidates, Origin, Forward, Range, HalfAngle);

        static InteractionCandidate At(float x, float y, float radius = 0f) => new(new Vector2(x, y), radius);

        [Test]
        public void NoCandidates_ReturnsNone()
        {
            Assert.AreEqual(-1, Select());
        }

        [Test]
        public void InFrontWithinRange_IsSelected()
        {
            Assert.AreEqual(0, Select(At(0f, 0.8f)));
        }

        [Test]
        public void BeyondRange_IsIgnored()
        {
            Assert.AreEqual(-1, Select(At(0f, 1.5f)));
        }

        [Test]
        public void Radius_ExtendsReach()
        {
            // 중심은 1.5m지만 가장자리는 0.9m
            Assert.AreEqual(0, Select(At(0f, 1.5f, 0.6f)));
        }

        [Test]
        public void Behind_IsIgnored()
        {
            Assert.AreEqual(-1, Select(At(0f, -0.5f)));
        }

        [Test]
        public void OutsideCone_IsIgnored()
        {
            // 정면에서 70도
            float a = 70f * Mathf.Deg2Rad;
            Assert.AreEqual(-1, Select(At(Mathf.Sin(a) * 0.8f, Mathf.Cos(a) * 0.8f)));
        }

        [Test]
        public void Overlapping_IsSelectedEvenBehind()
        {
            Assert.AreEqual(0, Select(At(0f, -0.3f, 0.5f)));
        }

        [Test]
        public void Nearest_WinsAmongCandidates()
        {
            Assert.AreEqual(1, Select(At(0.3f, 0.9f), At(-0.2f, 0.5f), At(0f, 1.6f)));
        }

        [Test]
        public void NearerButBehind_LosesToFartherInFront()
        {
            Assert.AreEqual(1, Select(At(0f, -0.4f), At(0f, 0.9f)));
        }

        [Test]
        public void Tie_FirstInListWins()
        {
            Assert.AreEqual(0, Select(At(0.3f, 0.6f), At(-0.3f, 0.6f)));
        }

        [Test]
        public void ForwardNeedNotBeNormalized()
        {
            var list = new List<InteractionCandidate> { At(0.8f, 0f) };
            Assert.AreEqual(0, InteractionTargeting.Select(list, Origin, new Vector2(5f, 0f), Range, HalfAngle));
        }
    }
}
