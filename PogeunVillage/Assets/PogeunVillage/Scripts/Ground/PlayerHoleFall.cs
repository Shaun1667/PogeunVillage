using System.Collections;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 플레이어가 구멍 칸 가운데로 들어가면 빠져서 잠깐 못 움직임(결정 2026-10-27: 1.5초, 구멍은 그대로).
    /// 나온 뒤 그 칸을 벗어날 때까지는 다시 빠지지 않음. 수치는 WorldSettings.
    /// </summary>
    public class PlayerHoleFall : MonoBehaviour
    {
        public WorldSettings settings;
        public VillageGround ground;
        public PlayerController controller;

        public bool IsStuck { get; private set; }

        Vector2Int? climbedOutOf;

        void Update()
        {
            if (IsStuck || !ground || ground.Grid == null)
                return;

            Vector2Int cell = ground.CellAt(transform.position);
            if (climbedOutOf.HasValue && climbedOutOf.Value != cell)
                climbedOutOf = null;
            if (climbedOutOf == cell || ground.Grid.Get(cell) != GroundObjectKind.Hole)
                return;

            Vector2 center = ground.Grid.CellCenter(cell);
            if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), center) <= settings.holeFallRadius)
                StartCoroutine(Fall(cell));
        }

        IEnumerator Fall(Vector2Int cell)
        {
            IsStuck = true;
            controller.enabled = false;
            Transform visual = controller.visual;
            Vector3 rest = visual ? visual.localPosition : Vector3.zero;
            if (visual)
                visual.localPosition = rest + Vector3.down * settings.holeSinkDepth;
            Debug.Log("[구멍] 구멍에 빠졌어요", this);

            float end = Time.time + settings.holeStuckTime;
            while (Time.time < end)
                yield return null;

            if (visual)
                visual.localPosition = rest;
            controller.enabled = true;
            climbedOutOf = cell;
            IsStuck = false;
        }
    }
}
