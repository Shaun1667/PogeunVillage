using System;
using System.Collections.Generic;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>칸에 놓이는 것. enum은 끝에만 추가.</summary>
    public enum GroundObjectKind
    {
        None,
        Flower,
        Sapling,
        Hole,
        Stump,
        Furniture,
    }

    /// <summary>
    /// 땅 칸(격자) 규칙(엔진 상태 없음). 결정 (2026-10-27): 칸 1m, 한 칸에 하나, 물 · 집 · 나무 · 바위 · 다리 · 길 · 광장 칸은 막힘.
    /// 칸 (i, j)는 월드 xz가 [i·size, (i+1)·size) × [j·size, (j+1)·size) 인 정사각형. 범위 밖은 막힌 칸.
    /// 바뀐 칸만 CellChanged로 알림.
    /// </summary>
    public class GroundGrid
    {
        readonly Vector2Int origin;
        readonly int width, height;
        readonly bool[] blocked;
        readonly Dictionary<Vector2Int, GroundObjectKind> objects = new();

        public float CellSize { get; }
        public event Action<Vector2Int> CellChanged;

        /// <param name="blocked">width × height, 인덱스 = (cell.y - origin.y) * width + (cell.x - origin.x)</param>
        public GroundGrid(float cellSize, Vector2Int origin, int width, int height, bool[] blocked)
        {
            if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize));
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (blocked == null || blocked.Length != width * height)
                throw new ArgumentException("막힌 칸 배열 크기가 width × height와 다릅니다.", nameof(blocked));
            CellSize = cellSize;
            this.origin = origin;
            this.width = width;
            this.height = height;
            this.blocked = blocked;
        }

        public Vector2Int ToCell(Vector2 position) =>
            new(Mathf.FloorToInt(position.x / CellSize), Mathf.FloorToInt(position.y / CellSize));

        public Vector2 CellCenter(Vector2Int cell) => new((cell.x + 0.5f) * CellSize, (cell.y + 0.5f) * CellSize);

        /// <summary>서 있는 곳에서 바라보는 방향으로 reach만큼 앞의 점이 든 칸</summary>
        public Vector2Int FrontCell(Vector2 position, Vector2 forward, float reach)
        {
            Vector2 facing = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector2.up;
            return ToCell(position + facing * reach);
        }

        public bool InBounds(Vector2Int cell) =>
            cell.x >= origin.x && cell.y >= origin.y && cell.x < origin.x + width && cell.y < origin.y + height;

        public bool IsBlocked(Vector2Int cell) =>
            !InBounds(cell) || blocked[(cell.y - origin.y) * width + (cell.x - origin.x)];

        public GroundObjectKind Get(Vector2Int cell) => objects.TryGetValue(cell, out var kind) ? kind : GroundObjectKind.None;

        public bool CanPlace(Vector2Int cell) => !IsBlocked(cell) && Get(cell) == GroundObjectKind.None;

        public bool TryPlace(Vector2Int cell, GroundObjectKind kind)
        {
            if (kind == GroundObjectKind.None)
                throw new ArgumentException("None은 놓을 수 없습니다.", nameof(kind));
            if (!CanPlace(cell))
                return false;
            objects[cell] = kind;
            CellChanged?.Invoke(cell);
            return true;
        }

        /// <summary>칸을 막음(묘목이 다 자라 나무가 된 자리). 그 칸에 있던 것은 먼저 치워야 함. 바뀌었으면 true</summary>
        public bool Block(Vector2Int cell)
        {
            if (!InBounds(cell) || IsBlocked(cell) || Get(cell) != GroundObjectKind.None)
                return false;
            blocked[(cell.y - origin.y) * width + (cell.x - origin.x)] = true;
            CellChanged?.Invoke(cell);
            return true;
        }

        /// <summary>막힌 칸을 빈 땅으로(밑동을 파낸 자리). 바뀌었으면 true</summary>
        public bool Unblock(Vector2Int cell)
        {
            if (!InBounds(cell))
                return false;
            int index = (cell.y - origin.y) * width + (cell.x - origin.x);
            if (!blocked[index])
                return false;
            blocked[index] = false;
            CellChanged?.Invoke(cell);
            return true;
        }

        /// <returns>무언가 있어서 치웠으면 true</returns>
        public bool Remove(Vector2Int cell)
        {
            if (!objects.Remove(cell))
                return false;
            CellChanged?.Invoke(cell);
            return true;
        }
    }
}
