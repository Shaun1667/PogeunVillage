using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PogeunVillage
{
    /// <summary>
    /// 인벤토리 창(결정 2026-10-27, 2-3, 가구 · 물고기 칸은 처음 F로 놓기 모드 3-4a · 3-6c): Tab(패드 Y)으로 열고 닫음. 방향키(패드 십자키)로 칸을 고르고,
    /// F(패드 A)로 집은 뒤 다른 칸에서 F를 누르면 두 칸을 맞바꿈(같은 칸에서 다시 F = 내려놓기).
    /// 고르기 모드(2-5 선물): OpenForPick으로 열면 F = 그 칸을 고름(고를 수 없으면 알림), Tab = 취소.
    /// 창이 열린 동안 플레이어 조작을 멈춤(PlayerLock). 다른 창이 열려 있으면 Tab으로 열리지 않음. 칸 글자는 바뀐 칸만 고침.
    /// </summary>
    public class InventoryWindow : MonoBehaviour
    {
        public const int Columns = 5;
        const string DefaultTitle = "인벤토리";

        public WorldSettings settings;
        public ShopData catalogSource;
        public PlayerInventory inventory;
        public PlayerLock playerLock;
        [Tooltip("가구 칸을 F로 고르면 놓기 모드(3-4a)")]
        public FurniturePlacer furniturePlacer;
        public GameObject panel;
        public TMP_Text titleText;
        public Image[] slotBackgrounds = Array.Empty<Image>();
        public TMP_Text[] slotTexts = Array.Empty<TMP_Text>();

        public bool IsOpen => panel && panel.activeSelf;
        public bool IsPicking => pickFilter != null;
        public int Cursor { get; private set; }
        public int Picked { get; private set; } = -1;

        Func<int, bool> pickFilter;
        Action<int> onPicked;
        Action onCancelled;
        string pickRefusal;
        int openedFrame = -1;

        void Start()
        {
            if (!settings || !catalogSource || !inventory || !playerLock || !panel || !titleText || inventory.Inventory == null
                || slotTexts.Length != inventory.Inventory.Slots.Count || slotBackgrounds.Length != slotTexts.Length)
            {
                Debug.LogError($"{name}: 인벤토리 창 연결이 비어 있거나 칸 수가 다릅니다.", this);
                enabled = false;
                return;
            }
            inventory.Inventory.SlotChanged += ShowSlot;
            for (int i = 0; i < slotTexts.Length; i++)
                ShowSlot(i);
            if (!IsPicking)
                panel.SetActive(false);
        }

        void OnDestroy()
        {
            if (inventory && inventory.Inventory != null)
                inventory.Inventory.SlotChanged -= ShowSlot;
        }

        /// <summary>그 칸의 물건 데이터(빈 칸 · 모르는 물건이면 null)</summary>
        public ItemDefinition ItemAt(int index)
        {
            var slot = inventory.Inventory.Slots[index];
            return slot.IsEmpty ? null : Array.Find(catalogSource.catalog, i => i && i.id == slot.itemId);
        }

        /// <summary>
        /// 칸 하나를 고르는 창으로 엶(선물 등). F = canPick이 참인 칸이면 닫고 picked, 아니면 refusal 알림. Tab = 닫고 cancelled.
        /// </summary>
        public void OpenForPick(string title, Func<int, bool> canPick, string refusal, Action<int> picked, Action cancelled)
        {
            pickFilter = canPick;
            onPicked = picked;
            onCancelled = cancelled;
            pickRefusal = refusal;
            titleText.text = title;
            openedFrame = Time.frameCount;
            SetOpen(true);
        }

        /// <summary>닫음(잠에서 깸 등). 고르기 중이었으면 알림 없이 그만둠</summary>
        public void Close()
        {
            pickFilter = null;
            onPicked = null;
            onCancelled = null;
            if (IsOpen)
                SetOpen(false);
        }

        void Update()
        {
            if (Time.frameCount == openedFrame)
                return;
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            if ((keyboard?.tabKey.wasPressedThisFrame ?? false) || (gamepad?.buttonNorth.wasPressedThisFrame ?? false))
            {
                if (IsPicking)
                    EndPick(-1);
                else if (IsOpen)
                    SetOpen(false);
                else if (playerLock.IsFree)
                    SetOpen(true);
                return;
            }
            if (!IsOpen)
                return;

            int dx = 0, dy = 0;
            if ((keyboard?.rightArrowKey.wasPressedThisFrame ?? false) || (gamepad?.dpad.right.wasPressedThisFrame ?? false)) dx++;
            if ((keyboard?.leftArrowKey.wasPressedThisFrame ?? false) || (gamepad?.dpad.left.wasPressedThisFrame ?? false)) dx--;
            if ((keyboard?.downArrowKey.wasPressedThisFrame ?? false) || (gamepad?.dpad.down.wasPressedThisFrame ?? false)) dy++;
            if ((keyboard?.upArrowKey.wasPressedThisFrame ?? false) || (gamepad?.dpad.up.wasPressedThisFrame ?? false)) dy--;
            if (dx != 0 || dy != 0)
                MoveCursor(InventoryCursor.Move(Cursor, dx, dy, Columns, slotTexts.Length));

            if ((keyboard?.fKey.wasPressedThisFrame ?? false) || (gamepad?.buttonSouth.wasPressedThisFrame ?? false))
            {
                if (!IsPicking)
                    PickOrSwap();
                else if (pickFilter(Cursor))
                    EndPick(Cursor);
                else
                    GameNotice.Post(pickRefusal, this);
            }
        }

        void EndPick(int picked)
        {
            var pickedAction = onPicked;
            var cancelled = onCancelled;
            pickFilter = null;
            onPicked = null;
            onCancelled = null;
            SetOpen(false);
            if (picked >= 0) pickedAction?.Invoke(picked);
            else cancelled?.Invoke();
        }

        void SetOpen(bool open)
        {
            panel.SetActive(open);
            if (open)
                playerLock.Acquire(this);
            else
            {
                playerLock.Release(this);
                titleText.text = DefaultTitle;
            }
            int before = Picked;
            Picked = -1;
            if (before >= 0) Tint(before);
            Tint(Cursor);
        }

        void MoveCursor(int to)
        {
            int from = Cursor;
            Cursor = to;
            Tint(from);
            Tint(to);
        }

        void PickOrSwap()
        {
            if (Picked < 0)
            {
                if (inventory.Inventory.Slots[Cursor].IsEmpty)
                    return;
                // 가구 칸을 처음 고르면 창을 닫고 놓기 모드(3-4a). 가구 칸으로 맞바꾸기는 다른 칸을 먼저 집으면 됨
                var placeable = ItemAt(Cursor) switch
                {
                    FurnitureDefinition f => f,
                    FishDefinition fish => fish.aquarium,  // 물고기는 수조로 놓음(3-6c)
                    _ => null,
                };
                if (furniturePlacer && placeable)
                {
                    int slot = Cursor;
                    SetOpen(false);
                    furniturePlacer.Begin(slot, placeable);
                    return;
                }
                Picked = Cursor;
                Tint(Picked);
                return;
            }
            int a = Picked;
            Picked = -1;
            inventory.Inventory.Swap(a, Cursor);  // 같은 칸이면 아무 일 없음(내려놓기)
            Tint(a);
            Tint(Cursor);
        }

        void ShowSlot(int index)
        {
            var slot = inventory.Inventory.Slots[index];
            var item = ItemAt(index);
            string name = item ? item.displayName : slot.itemId;
            slotTexts[index].text = HudText.Slot(name, slot.count, slot.durability);
            Tint(index);
        }

        void Tint(int index)
        {
            slotBackgrounds[index].color = index == Picked ? settings.inventoryPickedColor
                : index == Cursor && IsOpen ? settings.inventoryCursorColor
                : settings.inventorySlotColor;
        }
    }
}
