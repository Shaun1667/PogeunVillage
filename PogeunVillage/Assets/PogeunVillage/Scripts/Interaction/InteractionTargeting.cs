using System.Collections.Generic;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>상호작용 후보 하나(평평한 월드의 xz 위치와 크기).</summary>
    public readonly struct InteractionCandidate
    {
        public readonly Vector2 position;
        public readonly float radius;

        public InteractionCandidate(Vector2 position, float radius)
        {
            this.position = position;
            this.radius = radius;
        }
    }

    /// <summary>
    /// 상호작용 대상 고르기 규칙(엔진 상태 없음). 결정 (2026-10-27): 앞쪽 부채꼴 안에서 가장 가까운 것.
    /// - 거리: 플레이어에서 대상 가장자리까지(중심 거리 - radius)가 range 이하
    /// - 방향: 바라보는 방향과 대상 중심 방향의 각도가 halfAngle 이하 (대상과 겹쳐 있으면 방향 무시)
    /// - 같은 거리면 목록에서 앞에 있는 것
    /// </summary>
    public static class InteractionTargeting
    {
        /// <returns>고른 후보의 인덱스, 없으면 -1</returns>
        public static int Select(IReadOnlyList<InteractionCandidate> candidates, Vector2 origin, Vector2 forward,
            float range, float halfAngle)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            float minCos = Mathf.Cos(halfAngle * Mathf.Deg2Rad);
            Vector2 facing = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector2.up;

            for (int i = 0; i < candidates.Count; i++)
            {
                Vector2 offset = candidates[i].position - origin;
                float centerDistance = offset.magnitude;
                float edgeDistance = centerDistance - candidates[i].radius;
                if (edgeDistance > range)
                    continue;

                bool overlapping = edgeDistance <= 0f;
                if (!overlapping && Vector2.Dot(offset / centerDistance, facing) < minCos)
                    continue;

                if (edgeDistance < bestDistance)
                {
                    bestDistance = edgeDistance;
                    best = i;
                }
            }
            return best;
        }
    }
}
