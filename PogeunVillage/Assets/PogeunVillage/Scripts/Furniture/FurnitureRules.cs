namespace PogeunVillage
{
    /// <summary>가구 놓기 규칙(엔진 상태 없음). 결정 (2026-10-28): 1칸 · 네 방향(0 ~ 3, 90도씩), 놓을 수 있는 칸은 꽃 · 묘목과 같음.</summary>
    public static class FurnitureRules
    {
        public const int Directions = 4;

        /// <summary>R: 다음 방향(3 다음은 0)</summary>
        public static int NextRotation(int rotation) => ((rotation % Directions) + Directions + 1) % Directions;

        /// <summary>방향 → 돌림 각(도)</summary>
        public static float Yaw(int rotation) => ((rotation % Directions) + Directions) % Directions * 90f;

        /// <summary>풀밭 빈 칸이면 놓을 수 있음(길 · 광장 · 물 · 건물 · 나무 · 바위 · 다른 것이 있는 칸은 안 됨)</summary>
        public static bool CanPlace(GroundGrid grid, UnityEngine.Vector2Int cell) => grid.CanPlace(cell);

        /// <summary>임시 가구 상자(3-4a): 차례로 하나씩, 끝나면 처음부터</summary>
        public static int NextInBox(int index, int count) => count <= 0 ? 0 : (index + 1) % count;
    }
}
