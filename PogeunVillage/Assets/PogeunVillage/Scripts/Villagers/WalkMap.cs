using System;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 주민이 걸을 수 있는 칸(엔진 상태 없음). 맵 생성 메뉴가 구운 '걸을 수 있는 칸'(물 · 절벽 · 충돌체 제외, 길 · 광장 · 다리는 걸을 수 있음)에
    /// 지금 땅 칸 상태를 더함: 무언가 놓인 칸(꽃 · 묘목 · 구멍 · 밑동)과 나중에 막힌 칸(다 자란 나무)은 피함.
    /// 칸 번호는 GroundGrid와 같음.
    /// </summary>
    public class WalkMap
    {
        readonly Vector2Int origin;
        readonly int width, height;
        readonly bool[] walkable;
        readonly bool[] bakedBlocked;

        /// <param name="walkable">width × height, 구운 '걸을 수 있음'</param>
        /// <param name="bakedBlocked">width × height, 구운 '놓을 수 없음'(길 · 광장 포함). 이것과 지금 막힘이 다르면 나중에 막힌 칸</param>
        public WalkMap(Vector2Int origin, int width, int height, bool[] walkable, bool[] bakedBlocked)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (walkable == null || walkable.Length != width * height)
                throw new ArgumentException("걸을 수 있는 칸 배열 크기가 width × height와 다릅니다.", nameof(walkable));
            if (bakedBlocked == null || bakedBlocked.Length != width * height)
                throw new ArgumentException("막힌 칸 배열 크기가 width × height와 다릅니다.", nameof(bakedBlocked));
            this.origin = origin;
            this.width = width;
            this.height = height;
            this.walkable = walkable;
            this.bakedBlocked = bakedBlocked;
        }

        public bool IsWalkable(Vector2Int cell, GroundGrid live)
        {
            if (cell.x < origin.x || cell.y < origin.y || cell.x >= origin.x + width || cell.y >= origin.y + height)
                return false;
            int i = (cell.y - origin.y) * width + (cell.x - origin.x);
            if (!walkable[i] || live.Get(cell) != GroundObjectKind.None)
                return false;
            return !live.IsBlocked(cell) || bakedBlocked[i];
        }

        public bool IsWalkable(Vector2 position, GroundGrid live) => IsWalkable(live.ToCell(position), live);

        /// <summary>a에서 b까지 곧게 걸을 때 지나는 칸이 모두 걸을 수 있는지(step 간격으로 확인)</summary>
        public bool IsLineWalkable(Vector2 a, Vector2 b, GroundGrid live, float step)
        {
            float length = Vector2.Distance(a, b);
            int samples = Mathf.Max(1, Mathf.CeilToInt(length / Mathf.Max(step, 0.05f)));
            for (int s = 0; s <= samples; s++)
                if (!IsWalkable(Vector2.Lerp(a, b, s / (float)samples), live))
                    return false;
            return true;
        }

        /// <summary>
        /// 배회 목표: center 둘레 radius 안의 무작위 점 중, from에서 곧게 걸어갈 수 있는 첫 점. tries번 안에 못 찾으면 null.
        /// </summary>
        public Vector2? PickWanderTarget(Vector2 from, Vector2 center, float radius, IRandomSource random, GroundGrid live, int tries, float step)
        {
            for (int t = 0; t < tries; t++)
            {
                double angle = random.NextDouble() * Math.PI * 2.0;
                float distance = radius * (float)Math.Sqrt(random.NextDouble());
                var target = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * distance;
                if (IsLineWalkable(from, target, live, step))
                    return target;
            }
            return null;
        }
    }
}
