using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 삽 동작(ShovelRules). 앞의 밑동 파내기, 앞 칸에 구멍 파기 · 메우기 · 꽃 통째 회수('꽃 포기').
    /// 무언가를 했을 때만 true(내구도 -1). 꽃 포기가 인벤토리에 안 들어가면 꽃을 그대로 둠.
    /// </summary>
    public class ShovelUser : MonoBehaviour, IToolAction
    {
        public WorldSettings settings;
        public VillageGround ground;
        public PlayerInteractor interactor;
        public PlayerInventory inventory;
        [Tooltip("꽃을 삽으로 회수하면 받는 물건(다시 심을 수 있음)")]
        public ItemDefinition flowerClumpItem;

        public bool TryUse(PlayerTools by, ToolDefinition tool)
        {
            if (tool.kind != ToolKind.Shovel || !ground || ground.Grid == null)
                return false;

            Interactable front = interactor.Target;
            bool stumpInFront = front && front.kind == InteractableKind.Stump;
            Vector3 p = transform.position, f = transform.forward;
            Vector2Int cell = ground.Grid.FrontCell(new Vector2(p.x, p.z), new Vector2(f.x, f.z), settings.groundCursorReach);

            switch (ShovelRules.Decide(stumpInFront, ground.Grid.IsBlocked(cell), ground.Grid.Get(cell)))
            {
                case ShovelAction.RemoveStump:
                    ground.Grid.Unblock(ground.CellAt(front.transform.position));
                    Destroy(front.gameObject);
                    Debug.Log("[삽] 밑동을 파냈어요", this);
                    return true;

                case ShovelAction.DigHole:
                    if (!ground.Placer.Spawn(GroundObjectKind.Hole, cell))
                        return false;
                    Debug.Log($"[삽] 구멍을 팠어요 {cell}", this);
                    return true;

                case ShovelAction.FillHole:
                    RemoveObjectAt(cell);
                    Debug.Log($"[삽] 구멍을 메웠어요 {cell}", this);
                    return true;

                case ShovelAction.LiftFlower:
                    if (!inventory.TryAdd(flowerClumpItem))
                        return false;
                    RemoveObjectAt(cell);
                    return true;

                default:
                    return false;
            }
        }

        void RemoveObjectAt(Vector2Int cell)
        {
            var obj = ground.ObjectAt(cell);
            if (!obj)
                return;
            obj.Unregister();
            Destroy(obj.gameObject);
        }
    }
}
