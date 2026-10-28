using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PogeunVillage
{
    /// <summary>
    /// 작업대 제작 창(결정 2026-10-28, 3-4b): 제작법 목록(가구 · 필요한 목재). 위아래(W/S · 방향키 · 십자키)로 고르고
    /// F(패드 A) = 만들기(창은 열린 채 — 연달아 만들 수 있음), Tab · Esc(패드 Y · B) = 닫기.
    /// 아직 안 열린 제작법 · 목재가 모자란 줄은 흐리게. 열린 동안 플레이어 조작을 멈춤(PlayerLock). 목재 수가 바뀌면 다시 그림.
    /// </summary>
    public class CraftWindow : MonoBehaviour
    {
        const string HighlightHex = "#C0607A";
        const string MutedHex = "#A89F9A";

        public GameObject panel;
        public TMP_Text listText;
        public TMP_Text woodText;
        public PlayerLock playerLock;

        public bool IsOpen => panel && panel.activeSelf;
        public int Cursor { get; private set; }
        public Workbench Bench { get; private set; }

        int openedFrame = -1;

        void Start()
        {
            if (!panel || !listText || !woodText || !playerLock)
            {
                Debug.LogError($"{name}: 제작 창 연결이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            if (!Bench)
                panel.SetActive(false);
        }

        void OnDestroy() => Unsubscribe();

        public void Open(Workbench bench)
        {
            Bench = bench;
            Cursor = 0;
            openedFrame = Time.frameCount;
            panel.SetActive(true);
            playerLock.Acquire(this);
            bench.inventory.Inventory.SlotChanged += OnSlotChanged;
            Show();
        }

        public void Close()
        {
            if (!Bench)
                return;
            Unsubscribe();
            Bench = null;
            panel.SetActive(false);
            playerLock.Release(this);
        }

        void Unsubscribe()
        {
            if (Bench && Bench.inventory && Bench.inventory.Inventory != null)
                Bench.inventory.Inventory.SlotChanged -= OnSlotChanged;
        }

        void Update()
        {
            if (!Bench || Time.frameCount == openedFrame)
                return;
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            if ((keyboard?.tabKey.wasPressedThisFrame ?? false) || (keyboard?.escapeKey.wasPressedThisFrame ?? false)
                || (gamepad?.buttonNorth.wasPressedThisFrame ?? false) || (gamepad?.buttonEast.wasPressedThisFrame ?? false))
            {
                Close();
                return;
            }

            int dy = 0;
            if ((keyboard?.downArrowKey.wasPressedThisFrame ?? false) || (keyboard?.sKey.wasPressedThisFrame ?? false) || (gamepad?.dpad.down.wasPressedThisFrame ?? false)) dy++;
            if ((keyboard?.upArrowKey.wasPressedThisFrame ?? false) || (keyboard?.wKey.wasPressedThisFrame ?? false) || (gamepad?.dpad.up.wasPressedThisFrame ?? false)) dy--;
            if (dy != 0)
            {
                Cursor = Mathf.Clamp(Cursor + dy, 0, Bench.recipes.Length - 1);
                Show();
            }

            if ((keyboard?.fKey.wasPressedThisFrame ?? false) || (gamepad?.buttonSouth.wasPressedThisFrame ?? false))
                Bench.Craft(Bench.recipes[Cursor]);  // 만들면 SlotChanged로 다시 그림
        }

        void OnSlotChanged(int _) => Show();

        void Show()
        {
            int stars = Bench.Stars, wood = Bench.Wood;
            var lines = new string[Bench.recipes.Length];
            var usable = new bool[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                var r = Bench.recipes[i];
                lines[i] = HudText.Recipe(r.displayName, r.woodCost, r.unlockStars, stars);
                usable[i] = CraftRules.IsUnlocked(r.unlockStars, stars) && wood >= r.woodCost;
            }
            listText.text = HudText.RecipeList(lines, usable, Cursor, HighlightHex, MutedHex);
            woodText.text = $"가진 목재 {wood}개";
        }
    }
}
