using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PogeunVillage
{
    /// <summary>
    /// 화면 아래 대화창(결정 2026-10-28, 2-4): 이름 + 대사 한 줄. F(패드 A)로 다음 줄, 마지막 줄 다음에 닫힘.
    /// 열린 동안 이동 · 상호작용 · 도구 · 인벤토리 창을 멈춤. 여는 프레임의 F는 넘기기로 치지 않음.
    /// </summary>
    public class DialogueWindow : MonoBehaviour
    {
        public GameObject panel;
        public TMP_Text speakerText;
        public TMP_Text lineText;
        [Tooltip("대화 중 멈출 것(이동 · 상호작용 · 도구 · 인벤토리 창)")]
        public Behaviour[] pausedWhileOpen = Array.Empty<Behaviour>();

        public bool IsOpen => panel && panel.activeSelf;
        public Dialogue Current { get; private set; }

        Action onFinished;
        int openedFrame = -1;
        bool resumePending;

        void Start()
        {
            if (!panel || !speakerText || !lineText)
            {
                Debug.LogError($"{name}: 대화창 연결이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            if (Current == null)
                panel.SetActive(false);
        }

        public void Open(Dialogue dialogue, Action finished)
        {
            Current = dialogue;
            onFinished = finished;
            openedFrame = Time.frameCount;
            speakerText.text = dialogue.Speaker;
            lineText.text = dialogue.Current;
            SetOpen(true);
        }

        /// <summary>끝까지 보지 않고 닫음(잠에서 깸 등). 끝 알림은 보내지 않음</summary>
        public void Close()
        {
            if (Current == null)
                return;
            Current = null;
            onFinished = null;
            SetOpen(false);
        }

        void Update()
        {
            if (Current == null || Time.frameCount == openedFrame)
                return;
            bool pressed = (Keyboard.current?.fKey.wasPressedThisFrame ?? false)
                           || (Gamepad.current?.buttonSouth.wasPressedThisFrame ?? false);
            if (!pressed)
                return;
            if (Current.Advance())
            {
                lineText.text = Current.Current;
                return;
            }
            var finished = onFinished;
            Close();
            finished?.Invoke();
        }

        void SetOpen(bool open)
        {
            panel.SetActive(open);
            if (open)
                SetPaused(true);
            else
                resumePending = true;  // 닫는 프레임의 F가 바로 다시 말 걸기가 되지 않도록, 이 프레임 끝에 되살림
        }

        void LateUpdate()
        {
            if (!resumePending)
                return;
            resumePending = false;
            if (Current == null)
                SetPaused(false);
        }

        void SetPaused(bool paused)
        {
            foreach (var b in pausedWhileOpen)
                if (b) b.enabled = !paused;
        }
    }
}
