using UnityEngine;
using UnityEngine.InputSystem;

namespace PogeunVillage
{
    /// <summary>
    /// 가구 놓기 모드(결정 2026-10-28, 3-4a): 인벤토리 창에서 가구 칸을 F로 고르면 시작.
    /// 걸어 다닐 수 있고(상호작용 · 도구 · 인벤토리 창은 멈춤), 바라보는 앞 칸에 미리보기 + 테두리(못 놓는 칸이면 빨강).
    /// R(패드 RB) = 90도 돌리기, F(패드 A) = 놓기(그 칸에서 1개 빠짐, 모드 끝), Tab(패드 Y) = 그만두기.
    /// 물고기 칸이면 그 물고기의 수조를 놓음(3-6c).
    /// </summary>
    public class FurniturePlacer : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        const int IgnoreRaycastLayer = 2;

        public WorldSettings settings;
        public FurnitureKeeper keeper;
        public PlayerInventory inventory;
        public PlayerLock playerLock;
        [Tooltip("앞 칸 테두리(켜고 끄는 대상)")]
        public GameObject outline;

        public bool IsPlacing => Definition;
        public FurnitureDefinition Definition { get; private set; }
        public int Slot { get; private set; } = -1;
        public int Rotation { get; private set; }
        public Vector2Int Cell { get; private set; }
        public bool CanPlaceHere { get; private set; }

        GameObject ghost;
        Renderer[] outlineRenderers;
        MaterialPropertyBlock block;
        int startedFrame = -1;

        void Awake()
        {
            if (!settings || !keeper || !inventory || !playerLock || !outline)
            {
                Debug.LogError($"{name}: 가구 놓기 연결이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            outlineRenderers = outline.GetComponentsInChildren<Renderer>(true);
            block = new MaterialPropertyBlock();
            outline.SetActive(false);
        }

        /// <summary>인벤토리 그 칸의 가구로 놓기 모드 시작</summary>
        public bool Begin(int slot, FurnitureDefinition definition)
        {
            if (!enabled || IsPlacing || !definition || !definition.prefab)
                return false;
            Definition = definition;
            Slot = slot;
            Rotation = 0;
            startedFrame = Time.frameCount;
            ghost = MakeGhost(definition);
            outline.SetActive(true);
            playerLock.Acquire(this, allowMove: true);
            UpdatePreview();
            return true;
        }

        /// <summary>그만두기(가구는 인벤토리에 그대로)</summary>
        public void Cancel() => End();

        void Update()
        {
            if (!IsPlacing)
                return;
            UpdatePreview();
            if (Time.frameCount == startedFrame)
                return;

            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            if ((keyboard?.rKey.wasPressedThisFrame ?? false) || (gamepad?.rightShoulder.wasPressedThisFrame ?? false))
            {
                Rotation = FurnitureRules.NextRotation(Rotation);
                UpdatePreview();
            }
            if ((keyboard?.tabKey.wasPressedThisFrame ?? false) || (gamepad?.buttonNorth.wasPressedThisFrame ?? false))
            {
                End();
                return;
            }
            if ((keyboard?.fKey.wasPressedThisFrame ?? false) || (gamepad?.buttonSouth.wasPressedThisFrame ?? false))
                TryPlace();
        }

        void TryPlace()
        {
            var slot = inventory.Inventory.Slots[Slot];
            if (slot.IsEmpty || slot.itemId != Definition.InventoryItem.id)
            {
                End();  // 그 칸의 가구가 없어졌으면 그만둠
                return;
            }
            if (!CanPlaceHere || !keeper.Place(Definition, Cell, Rotation))
            {
                GameNotice.Post("[가구] 여기에는 놓을 수 없어요", this);
                return;
            }
            inventory.Inventory.RemoveOne(Slot);
            End();
        }

        void UpdatePreview()
        {
            Vector3 p = transform.position, f = transform.forward;
            Cell = keeper.ground.Grid.FrontCell(new Vector2(p.x, p.z), new Vector2(f.x, f.z), settings.groundCursorReach);
            CanPlaceHere = FurnitureRules.CanPlace(keeper.ground.Grid, Cell);
            Vector3 at = keeper.GroundPoint(Cell);
            outline.transform.position = at;
            ghost.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, FurnitureRules.Yaw(Rotation), 0f));
            block.SetColor(BaseColorId, CanPlaceHere ? settings.groundCursorColor : settings.groundCursorBlockedColor);
            foreach (var r in outlineRenderers)
                r.SetPropertyBlock(block);
        }

        void End()
        {
            if (ghost) Destroy(ghost);
            ghost = null;
            Definition = null;
            Slot = -1;
            outline.SetActive(false);
            playerLock.Release(this);
        }

        // 미리보기: 같은 모습만(충돌 · 상호작용 · 칸 등록 없음), 레이캐스트에 맞지 않게
        static GameObject MakeGhost(FurnitureDefinition definition)
        {
            var go = Instantiate(definition.prefab);
            go.name = definition.prefab.name + " (미리보기)";
            foreach (var p in go.GetComponentsInChildren<PlacedFurniture>(true)) p.Tint(definition);  // 테마 가구 색(3-4d)
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
            foreach (var p in go.GetComponentsInChildren<PlacedFurniture>(true)) DestroyImmediate(p);
            foreach (var g in go.GetComponentsInChildren<GridObject>(true)) DestroyImmediate(g);
            foreach (var i in go.GetComponentsInChildren<Interactable>(true)) DestroyImmediate(i);
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = IgnoreRaycastLayer;
            return go;
        }
    }
}
