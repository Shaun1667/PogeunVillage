using UnityEngine;

namespace PogeunVillage
{
    /// <summary>씬의 땅 칸(GroundGrid)을 들고 있는 어댑터. 칸에 놓이는 것(GridObject)과 앞 칸 표시(GroundCursor)가 씀.</summary>
    public class VillageGround : MonoBehaviour
    {
        public GroundGridData data;

        public GroundGrid Grid { get; private set; }

        void Awake()
        {
            if (!data)
            {
                Debug.LogError($"{name}: GroundGridData가 연결되지 않았습니다.", this);
                enabled = false;
                return;
            }
            Grid = data.CreateGrid();
        }

        public Vector2Int CellAt(Vector3 position) => Grid.ToCell(new Vector2(position.x, position.z));
    }
}
