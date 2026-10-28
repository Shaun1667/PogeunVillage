using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PogeunVillage
{
    /// <summary>
    /// 화면 아래 대화창(결정 2026-10-28, 2-4): 이름 + 대사 한 줄. F(패드 A)로 다음 줄, 마지막 줄 다음에 닫힘.
    /// 고르기가 있으면(2-5: 선물하기 / 안녕) 마지막 줄에서 위아래(W/S · 방향키 · 십자키)로 고르고 F로 정함.
    /// 열린 동안 플레이어 조작을 멈춤(PlayerLock). 여는 프레임의 F는 넘기기로 치지 않음.
    /// </summary>
    public class DialogueWindow : MonoBehaviour
    {
        const string HighlightHex = "#C0607A";

        public GameObject panel;
        public TMP_Text speakerText;
        public TMP_Text lineText;
        public TMP_Text choiceText;
        public TMP_Text hintText;
        public PlayerLock playerLock;

        public bool IsOpen => panel && panel.activeSelf;
        public Dialogue Current { get; private set; }

        Action<Dialogue> onFinished;
        int openedFrame = -1;

        void Start()
        {
            if (!panel || !speakerText || !lineText || !choiceText || !hintText || !playerLock)
            {
                Debug.LogError($"{name}: 대화창 연결이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            if (Current == null)
                panel.SetActive(false);
        }

        /// <param name="finished">끝까지 넘겼을 때(고르기가 있으면 dialogue.Chosen에 고른 번호)</param>
        public void Open(Dialogue dialogue, Action<Dialogue> finished)
        {
            Current = dialogue;
            onFinished = finished;
            openedFrame = Time.frameCount;
            panel.SetActive(true);
            playerLock.Acquire(this);
            Show();
        }

        /// <summary>이름 줄만 다시 씀(친밀도가 바뀐 뒤)</summary>
        public void SetSpeaker(string speaker)
        {
            if (Current == null)
                return;
            Current.Speaker = speaker;
            speakerText.text = speaker;
        }

        /// <summary>끝까지 보지 않고 닫음(잠에서 깸 등). 끝 알림은 보내지 않음</summary>
        public void Close()
        {
            if (Current == null)
                return;
            Current = null;
            onFinished = null;
            panel.SetActive(false);
            playerLock.Release(this);
        }

        void Update()
        {
            if (Current == null || Time.frameCount == openedFrame)
                return;
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            if (Current.IsChoosing)
            {
                int dy = 0;
                if ((keyboard?.downArrowKey.wasPressedThisFrame ?? false) || (keyboard?.sKey.wasPressedThisFrame ?? false) || (gamepad?.dpad.down.wasPressedThisFrame ?? false)) dy++;
                if ((keyboard?.upArrowKey.wasPressedThisFrame ?? false) || (keyboard?.wKey.wasPressedThisFrame ?? false) || (gamepad?.dpad.up.wasPressedThisFrame ?? false)) dy--;
                if (dy != 0)
                {
                    Current.MoveChoice(dy);
                    Show();
                }
            }

            bool pressed = (keyboard?.fKey.wasPressedThisFrame ?? false) || (gamepad?.buttonSouth.wasPressedThisFrame ?? false);
            if (!pressed)
                return;
            if (Current.Advance())
            {
                Show();
                return;
            }
            var finished = onFinished;
            var dialogue = Current;
            Close();
            finished?.Invoke(dialogue);
        }

        void Show()
        {
            speakerText.text = Current.Speaker;
            lineText.text = Current.Current;
            bool choosing = Current.IsChoosing;
            choiceText.text = choosing ? HudText.Choices(Current.Choices, Current.ChoiceCursor, HighlightHex) : "";
            hintText.text = choosing ? "위아래로 고르고 F" : "F 다음";
        }
    }
}
