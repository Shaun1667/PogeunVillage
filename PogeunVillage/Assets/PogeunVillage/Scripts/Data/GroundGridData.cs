using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 땅 칸 데이터: 칸 크기와 맵 생성 메뉴가 구운 막힌 칸(물 · 길 · 광장 · 집 · 나무 · 바위 · 다리 …).
    /// 에셋: Assets/PogeunVillage/Data/GroundGrid.asset. 칸 크기는 BALANCE.md 11번.
    /// 막힌 칸은 Build Map Prototype이 매번 다시 구움(손으로 고치지 않음).
    /// </summary>
    [CreateAssetMenu(fileName = "GroundGrid", menuName = "Pogeun Village/Ground Grid")]
    public class GroundGridData : ScriptableObject
    {
        [Tooltip("칸 한 변의 길이(m)")]
        [Min(0.1f)] public float cellSize = 1f;

        [SerializeField, HideInInspector] Vector2Int origin;
        [SerializeField, HideInInspector] int width;
        [SerializeField, HideInInspector] int height;
        [SerializeField, HideInInspector] byte[] blocked;  // 1 = 막힘
        [SerializeField, HideInInspector] byte[] walkable; // 1 = 주민이 걸을 수 있음(2-4)

        public int BlockedCount
        {
            get
            {
                int count = 0;
                if (blocked != null)
                    foreach (byte b in blocked) count += b;
                return count;
            }
        }

        public void Bake(Vector2Int origin, int width, int height, bool[] blockedCells)
        {
            this.origin = origin;
            this.width = width;
            this.height = height;
            blocked = new byte[blockedCells.Length];
            for (int i = 0; i < blockedCells.Length; i++)
                blocked[i] = blockedCells[i] ? (byte)1 : (byte)0;
        }

        /// <summary>주민이 걸을 수 있는 칸(2-4). 막힌 칸과 같은 크기. 매번 다시 구움</summary>
        public void BakeWalkable(bool[] walkableCells)
        {
            walkable = new byte[walkableCells.Length];
            for (int i = 0; i < walkableCells.Length; i++)
                walkable[i] = walkableCells[i] ? (byte)1 : (byte)0;
        }

        public int WalkableCount
        {
            get
            {
                int count = 0;
                if (walkable != null)
                    foreach (byte b in walkable) count += b;
                return count;
            }
        }

        public WalkMap CreateWalkMap()
        {
            var cells = new bool[width * height];
            var baked = new bool[width * height];
            bool hasWalkable = walkable != null && walkable.Length == cells.Length;
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = hasWalkable && walkable[i] != 0;
                baked[i] = blocked[i] != 0;
            }
            return new WalkMap(origin, width, height, cells, baked);
        }

        public GroundGrid CreateGrid()
        {
            var cells = new bool[width * height];
            for (int i = 0; i < cells.Length; i++)
                cells[i] = blocked[i] != 0;
            return new GroundGrid(cellSize, origin, width, height, cells);
        }
    }
}
