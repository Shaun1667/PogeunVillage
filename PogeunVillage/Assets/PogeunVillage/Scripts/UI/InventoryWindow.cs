using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PogeunVillage
{
    /// <summary>
    /// 인벤토리 창(결정 2026-10-27, 2-3): Tab(패드 Y)으로 열고 닫음. 방향키(패드 십자키)로 칸을 고르고,
    /// F(패드 A)로 집은 뒤 다른 칸에서 F를 누르면 두 칸을 맞바꿈(같은 칸에서 다시 F = 내려놓기).
    /// 창이 열린 동안엔 이동 · 상호작용 · 도구를 멈춤. 칸 글자는 바뀐 칸만 고침.
    /// </summary>
    public class InventoryWindow : MonoBehaviour
    {
        public const int Columns = 5;

        public WorldSettings settings;
        public ShopData catalogSource;
        public PlayerInventory inventory;
        [Tooltip("창이 열린 동안 멈출 플레이어 동작(이동 · 상호작용 · 도구)")]
        public Behaviour[] pausedWhileOpen = Array.Empty<Behaviour>();
        public GameObject panel;
        public Image[] slotBackgrounds = Array.Empty<Image>();
        public TMP_Text[] slotTexts = Array.Empty<TMP_Text>();

        public bool IsOpen => panel && panel.activeSelf;
        public int Cursor { get; private set; }
        public int Picked { get; private set; } = -1;

        void Start()
        {
            if (!settings || !catalogSource || !inventory || !panel || inventory.Inventory == null
                || slotTexts.Length != inventory.Inventory.Slots.Count || slotBackgrounds.Length != slotTexts.Length)
            {
                Debug.LogError($"{name}: 인벤토리 창 연결이 비어 있거나 칸 수가 다릅니다.", this);
                enabled = false;
                return;
            }
            inventory.Inventory.SlotChanged += ShowSlot;
            for (int i = 0; i < slotTexts.Length; i++)
                ShowSlot(i);
            panel.SetActive(false);
        }

        void OnDestroy()
        {
            if (inventory && inventory.Inventory != null)
                inventory.Inventory.SlotChanged -= ShowSlot;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            if ((keyboard?.tabKey.wasPressedThisFrame ?? false) || (gamepad?.buttonNorth.wasPressedThisFrame ?? false))
            {
                SetOpen(!IsOpen);
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
                PickOrSwap();
        }

        void SetOpen(bool open)
        {
            panel.SetActive(open);
            foreach (var b in pausedWhileOpen)
                if (b) b.enabled = !open;
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
            var item = slot.IsEmpty ? null : Array.Find(catalogSource.catalog, i => i && i.id == slot.itemId);
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
