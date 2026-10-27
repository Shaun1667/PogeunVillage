using UnityEngine;

namespace PogeunVillage
{
    /// <summary>물뿌리개 동작: 앞 칸의 꽃 · 묘목에 물을 줌(그날 1번만 인정). 꽃 · 묘목이 아니면 허공.</summary>
    public class WateringCanUser : MonoBehaviour, IToolAction
    {
        public WorldSettings settings;
        public VillageGround ground;
        public GardenKeeper garden;

        public bool TryUse(PlayerTools by, ToolDefinition tool)
        {
            if (tool.kind != ToolKind.WateringCan || !garden || garden.Garden == null)
                return false;
            Vector3 p = transform.position, f = transform.forward;
            Vector2Int cell = ground.Grid.FrontCell(new Vector2(p.x, p.z), new Vector2(f.x, f.z), settings.groundCursorReach);
            if (!garden.Garden.Water(cell))
                return false;
            Debug.Log($"[물뿌리개] {ground.Grid.Get(cell)}에 물을 줬어요 {cell}", this);
            return true;
        }
    }
}
