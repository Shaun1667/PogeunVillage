using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 땅 칸 하나를 차지하는 것(꽃 · 묘목 · 구멍 · 밑동). 시작할 때 자기 위치의 칸에 등록하고, 사라지면 칸을 비웁니다.
    /// </summary>
    public class GridObject : MonoBehaviour
    {
        public VillageGround ground;
        public GroundObjectKind kind = GroundObjectKind.Flower;

        public Vector2Int Cell { get; private set; }
        public bool Registered { get; private set; }

        void Start()
        {
            if (!ground || ground.Grid == null)
            {
                Debug.LogError($"{name}: VillageGround가 연결되지 않았습니다.", this);
                return;
            }
            Cell = ground.CellAt(transform.position);
            Registered = ground.Grid.TryPlace(Cell, kind);
            if (!Registered)
                Debug.LogError($"{name}: 칸 {Cell}에 놓을 수 없습니다(막힘 또는 이미 있음).", this);
        }

        void OnDestroy()
        {
            if (Registered && ground && ground.Grid != null)
                ground.Grid.Remove(Cell);
        }
    }
}
