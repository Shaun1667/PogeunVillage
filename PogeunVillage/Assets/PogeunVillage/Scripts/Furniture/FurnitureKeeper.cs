using System.Collections.Generic;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 마을에 놓인 가구 어댑터(3-4a): 가구를 칸에 놓고(GridObject 등록 · 방향), 놓인 가구 F → '들기 / 그대로' → 들면 인벤토리로.
    /// 인벤토리가 가득 차면 못 듦(알림). 놓인 가구는 땅 칸을 차지해 꽃 · 묘목 · 주민 걸음이 피함.
    /// </summary>
    public class FurnitureKeeper : MonoBehaviour
    {
        const int IgnoreRaycastLayer = 2;
        static readonly string[] PickUpChoices = { "들기", "그대로" };

        public VillageGround ground;
        public PlayerInventory inventory;
        public DialogueWindow dialogue;

        readonly List<PlacedFurniture> placed = new();
        public IReadOnlyList<PlacedFurniture> Placed => placed;

        /// <summary>그 칸에 놓음. 놓을 수 없으면 null</summary>
        public PlacedFurniture Place(FurnitureDefinition definition, Vector2Int cell, int rotation)
        {
            if (!definition || !definition.prefab || !FurnitureRules.CanPlace(ground.Grid, cell))
                return null;
            var go = Instantiate(definition.prefab, GroundPoint(cell), Quaternion.Euler(0f, FurnitureRules.Yaw(rotation), 0f), transform);
            var gridObject = go.GetComponent<GridObject>();
            gridObject.ground = ground;
            gridObject.kind = GroundObjectKind.Furniture;
            if (!gridObject.Register())
            {
                Destroy(go);
                return null;
            }
            var furniture = go.GetComponent<PlacedFurniture>();
            furniture.Setup(this, definition, rotation);
            placed.Add(furniture);
            return furniture;
        }

        /// <summary>칸 가운데 땅 높이(가구 · 미리보기가 씀)</summary>
        public Vector3 GroundPoint(Vector2Int cell)
        {
            Vector2 c = ground.Grid.CellCenter(cell);
            Vector3 p = new(c.x, 0f, c.y);
            if (Physics.Raycast(p + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f, ~(1 << IgnoreRaycastLayer), QueryTriggerInteraction.Ignore))
                p.y = hit.point.y;
            return p;
        }

        /// <summary>놓인 가구 F: '들기 / 그대로'</summary>
        public void AskPickUp(PlacedFurniture furniture)
        {
            if (!dialogue || dialogue.IsOpen)
                return;
            var ask = new Dialogue(furniture.Definition.displayName, new[] { "어떻게 할까요?" }, PickUpChoices);
            dialogue.Open(ask, d =>
            {
                if (d.Chosen == 0)
                    PickUp(furniture);
            });
        }

        /// <summary>들기: 인벤토리에 넣고 칸을 비움. 가득 차면 그대로(알림)</summary>
        public bool PickUp(PlacedFurniture furniture)
        {
            if (!furniture)
                return false;
            if (!inventory.TryAdd(furniture.Definition.InventoryItem))  // 수조는 물고기로(3-6c)
                return false;  // '가득 찼어요' 알림은 PlayerInventory가 띄움
            placed.Remove(furniture);
            furniture.GetComponent<GridObject>().Unregister();
            furniture.gameObject.SetActive(false);
            Destroy(furniture.gameObject);
            return true;
        }
    }
}
