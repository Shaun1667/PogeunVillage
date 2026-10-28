using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PogeunVillage
{
    /// <summary>목록 창에 보일 내용(작업대 제작 3-4b · 상점 가구 3-4c)</summary>
    public interface IListSource
    {
        string Title { get; }
        string Hint { get; }
        int Count { get; }
        string Line(int index);
        /// <summary>지금 고를 수 있음(아니면 흐리게)</summary>
        bool Usable(int index);
        string Footer { get; }
        /// <summary>F: 그 줄을 고름(만들기 · 사기). 결과는 알림으로</summary>
        void Choose(int index);
    }

    /// <summary>
    /// 가운데 목록 창(결정 2026-10-28, 3-4b · 3-4c): 위아래(W/S · 방향키 · 십자키)로 고르고
    /// F(패드 A) = 고르기(창은 열린 채 — 연달아 할 수 있음), Tab · Esc(패드 Y · B) = 닫기.
    /// 지금 고를 수 없는 줄은 흐리게. 열린 동안 플레이어 조작을 멈춤(PlayerLock). 고른 뒤 · 인벤토리가 바뀌면 다시 그림.
    /// </summary>
    public class ListWindow : MonoBehaviour
    {
        const string HighlightHex = "#C0607A";
        const string MutedHex = "#A89F9A";

        public GameObject panel;
        public TMP_Text titleText;
        public TMP_Text listText;
        public TMP_Text footerText;
        public TMP_Text hintText;
        public PlayerLock playerLock;
        public PlayerInventory inventory;

        public bool IsOpen => panel && panel.activeSelf;
        public int Cursor { get; private set; }
        public IListSource Source { get; private set; }

        int openedFrame = -1;

        void Start()
        {
            if (!panel || !titleText || !listText || !footerText || !hintText || !playerLock || !inventory || inventory.Inventory == null)
            {
                Debug.LogError($"{name}: 목록 창 연결이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            inventory.Inventory.SlotChanged += OnSlotChanged;
            if (Source == null)
                panel.SetActive(false);
        }

        void OnDestroy()
        {
            if (inventory && inventory.Inventory != null)
                inventory.Inventory.SlotChanged -= OnSlotChanged;
        }

        public void Open(IListSource source)
        {
            Source = source;
            Cursor = 0;
            openedFrame = Time.frameCount;
            panel.SetActive(true);
            playerLock.Acquire(this);
            Show();
        }

        public void Close()
        {
            if (Source == null)
                return;
            Source = null;
            panel.SetActive(false);
            playerLock.Release(this);
        }

        void Update()
        {
            if (Source == null || Time.frameCount == openedFrame)
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
                Cursor = Mathf.Clamp(Cursor + dy, 0, Source.Count - 1);
                Show();
            }

            if ((keyboard?.fKey.wasPressedThisFrame ?? false) || (gamepad?.buttonSouth.wasPressedThisFrame ?? false))
            {
                Source.Choose(Cursor);
                Show();  // 모아처럼 인벤토리 밖에서 바뀐 것도 보이게
            }
        }

        void OnSlotChanged(int _)
        {
            if (Source != null)
                Show();
        }

        void Show()
        {
            var lines = new string[Source.Count];
            var usable = new bool[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = Source.Line(i);
                usable[i] = Source.Usable(i);
            }
            titleText.text = Source.Title;
            listText.text = HudText.ListLines(lines, usable, Cursor, HighlightHex, MutedHex);
            footerText.text = Source.Footer;
            hintText.text = Source.Hint;
        }
    }
}
