using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PogeunVillage
{
    /// <summary>
    /// 시작 화면(결정 2026-10-29, GDD 4-13 · 6-1d): 저장이 있으면 '이어하기(n일차 · 모아 · ★) / 새로 시작'(새로 시작은 한 번 더 확인),
    /// 없으면 보이지 않고 바로 새로 시작. 보이는 동안 시계가 멈추고 플레이어는 움직이지 않음.
    /// 위아래(W/S · 방향키 · 십자키)로 고르고 F · Space · Enter(패드 A), 확인에서 Esc(패드 B) = 돌아가기. 고르기 규칙은 StartMenu.
    /// </summary>
    [DefaultExecutionOrder(210)]  // SaveManager 뒤
    public class StartScreen : MonoBehaviour
    {
        const string HighlightHex = "#C0607A";

        public SaveManager save;
        public DayCycle dayCycle;
        public PlayerLock playerLock;
        public GameObject panel;
        public TMP_Text promptText;
        public TMP_Text listText;

        public bool IsOpen => panel && panel.activeSelf;
        public StartMenu Menu { get; private set; }

        int openedFrame = -1;

        void Start()
        {
            if (!save || !dayCycle || !playerLock || !panel || !promptText || !listText)
            {
                Debug.LogError($"{name}: 시작 화면 연결이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            var data = save.Peek();
            if (data == null)
            {
                panel.SetActive(false);  // 저장 없음 = 새로 시작
                return;
            }
            Menu = new StartMenu(StartMenu.ContinueLine(data.day, data.moa, data.stars));
            openedFrame = Time.frameCount;
            panel.SetActive(true);
            dayCycle.enabled = false;
            playerLock.Acquire(this);
            Show();
        }

        void Update()
        {
            if (!IsOpen || Menu == null || Time.frameCount == openedFrame)
                return;
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            int dy = 0;
            if ((keyboard?.downArrowKey.wasPressedThisFrame ?? false) || (keyboard?.sKey.wasPressedThisFrame ?? false) || (gamepad?.dpad.down.wasPressedThisFrame ?? false)) dy++;
            if ((keyboard?.upArrowKey.wasPressedThisFrame ?? false) || (keyboard?.wKey.wasPressedThisFrame ?? false) || (gamepad?.dpad.up.wasPressedThisFrame ?? false)) dy--;
            if (dy != 0)
            {
                Menu.Move(dy);
                Show();
            }
            if ((keyboard?.escapeKey.wasPressedThisFrame ?? false) || (gamepad?.buttonEast.wasPressedThisFrame ?? false))
            {
                Menu.Back();
                Show();
            }

            bool pressed = (keyboard?.fKey.wasPressedThisFrame ?? false) || (keyboard?.spaceKey.wasPressedThisFrame ?? false)
                           || (keyboard?.enterKey.wasPressedThisFrame ?? false) || (gamepad?.buttonSouth.wasPressedThisFrame ?? false);
            if (!pressed)
                return;
            switch (Menu.Press())
            {
                case StartChoice.Continue:
                    Close();
                    save.Continue();
                    break;
                case StartChoice.NewGame:
                    Close();
                    save.DeleteSave();
                    break;
                default:
                    Show();
                    break;
            }
        }

        void Close()
        {
            panel.SetActive(false);
            dayCycle.enabled = true;
            playerLock.Release(this);
        }

        void Show()
        {
            promptText.text = Menu.Prompt;
            listText.text = HudText.Choices(Menu.Lines, Menu.Cursor, HighlightHex);
        }
    }
}
