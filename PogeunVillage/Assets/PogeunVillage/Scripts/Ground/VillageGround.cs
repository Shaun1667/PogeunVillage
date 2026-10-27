using System.Collections.Generic;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 씬의 땅 칸(GroundGrid)을 들고 있는 어댑터. 칸에 놓인 것(GridObject)을 칸별로 기억하고,
    /// 새로 놓을 것(구멍 · 묘목 · 심은 꽃)은 GroundPlacer로 만듭니다.
    /// </summary>
    public class VillageGround : MonoBehaviour
    {
        public GroundGridData data;

        public GroundGrid Grid { get; private set; }
        public GroundPlacer Placer { get; private set; }

        readonly Dictionary<Vector2Int, GridObject> objects = new();

        void Awake()
        {
            if (!data)
            {
                Debug.LogError($"{name}: GroundGridData가 연결되지 않았습니다.", this);
                enabled = false;
                return;
            }
            Grid = data.CreateGrid();
            Placer = GetComponent<GroundPlacer>();
        }

        public Vector2Int CellAt(Vector3 position) => Grid.ToCell(new Vector2(position.x, position.z));

        public GridObject ObjectAt(Vector2Int cell) => objects.TryGetValue(cell, out var obj) ? obj : null;

        /// <summary>칸에 놓인 것 모두(복사본)</summary>
        public List<GridObject> Objects() => new(objects.Values);

        internal void Track(GridObject obj) => objects[obj.Cell] = obj;

        internal void Untrack(GridObject obj)
        {
            if (objects.TryGetValue(obj.Cell, out var current) && current == obj)
                objects.Remove(obj.Cell);
        }
    }
}
