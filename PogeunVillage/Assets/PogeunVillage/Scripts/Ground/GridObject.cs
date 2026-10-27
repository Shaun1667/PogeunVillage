using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 땅 칸 하나를 차지하는 것(꽃 · 묘목 · 구멍). 시작할 때(또는 GroundPlacer가 놓는 즉시) 자기 위치의 칸에 등록하고,
    /// 사라지면 칸을 비웁니다. 같은 프레임에 치우고 다시 놓을 때는 Unregister를 먼저 부릅니다.
    /// </summary>
    public class GridObject : MonoBehaviour
    {
        public VillageGround ground;
        public GroundObjectKind kind = GroundObjectKind.Flower;

        public Vector2Int Cell { get; private set; }
        public bool Registered { get; private set; }

        void Start()
        {
            if (!Registered && !Register())
                Debug.LogError($"{name}: 칸 {Cell}에 놓을 수 없습니다(막힘 · 이미 있음 · 땅 없음).", this);
        }

        /// <returns>칸에 놓였으면 true</returns>
        public bool Register()
        {
            if (Registered)
                return true;
            if (!ground || ground.Grid == null)
                return false;
            Cell = ground.CellAt(transform.position);
            Registered = ground.Grid.TryPlace(Cell, kind);
            if (Registered)
                ground.Track(this);
            return Registered;
        }

        public void Unregister()
        {
            if (!Registered)
                return;
            Registered = false;
            if (ground && ground.Grid != null)
            {
                ground.Untrack(this);
                ground.Grid.Remove(Cell);
            }
        }

        void OnDestroy() => Unregister();
    }
}
