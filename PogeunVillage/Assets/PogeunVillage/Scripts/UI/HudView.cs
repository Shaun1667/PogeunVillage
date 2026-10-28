using TMPro;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 항상 보이는 HUD(결정 2026-10-27, 2-3): 오른쪽 위 날짜 · 시각 · 모아, 왼쪽 아래 든 도구 · 내구도, 아래 가운데 짧은 알림.
    /// 시계 · 지갑 · 도구 · 인벤토리 · 알림 이벤트를 구독해 바뀔 때만 글자를 고칩니다.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        public WorldSettings settings;
        public DayCycle dayCycle;
        public PlayerWallet wallet;
        public PlayerTools tools;
        public PlayerInventory inventory;

        public TMP_Text clockText;
        public TMP_Text moaText;
        public TMP_Text toolText;
        public TMP_Text noticeText;
        [Tooltip("알림 받침판(켜고 끄는 대상). 비어 있으면 글자만 켜고 끔")]
        public GameObject noticeRoot;

        float noticeUntil;
        int noticeFrame = -1;
        readonly System.Collections.Generic.Queue<string> pending = new();

        void Start()
        {
            if (!settings || !dayCycle || !wallet || !tools || !inventory || !clockText || !moaText || !toolText || !noticeText)
            {
                Debug.LogError($"{name}: HUD 연결이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            dayCycle.Clock.MinuteChanged += OnMinute;
            dayCycle.Clock.DayStarted += OnDay;
            wallet.Wallet.Changed += OnMoa;
            tools.EquippedChanged += OnEquipped;
            inventory.Inventory.SlotChanged += OnSlot;
            GameNotice.Posted += OnNotice;

            ShowClock();
            OnMoa(wallet.Wallet.Moa);
            ShowTool();
            Notice.SetActive(false);
        }

        GameObject Notice => noticeRoot ? noticeRoot : noticeText.gameObject;
        public bool NoticeVisible => Notice.activeSelf;

        void OnDestroy()
        {
            GameNotice.Posted -= OnNotice;
            if (dayCycle && dayCycle.Clock != null)
            {
                dayCycle.Clock.MinuteChanged -= OnMinute;
                dayCycle.Clock.DayStarted -= OnDay;
            }
            if (wallet && wallet.Wallet != null) wallet.Wallet.Changed -= OnMoa;
            if (tools) tools.EquippedChanged -= OnEquipped;
            if (inventory && inventory.Inventory != null) inventory.Inventory.SlotChanged -= OnSlot;
        }

        void Update()
        {
            if (!Notice.activeSelf || Time.time < noticeUntil)
                return;
            if (pending.Count > 0)
                Show(pending.Dequeue());
            else
                Notice.SetActive(false);
        }

        void OnMinute(int _) => ShowClock();
        void OnDay(int _) => ShowClock();
        void ShowClock() => clockText.text = HudText.DayAndTime(dayCycle.Clock.Day, dayCycle.Clock.HourOfDay, dayCycle.Clock.Minute);

        void OnMoa(int moa) => moaText.text = HudText.Moa(moa);

        void OnEquipped(ToolDefinition _) => ShowTool();

        void OnSlot(int index)
        {
            if (index == tools.EquippedSlot)
                ShowTool();
        }

        void ShowTool()
        {
            var tool = tools.Equipped;
            int durability = tool && tools.EquippedSlot >= 0 ? inventory.Inventory.Slots[tools.EquippedSlot].durability : 0;
            toolText.text = tool ? HudText.HeldTool(tool.displayName, durability, tool.durability) : "";
        }

        // 새 알림은 바로 바꿔 보여 줌. 단, 같은 프레임에 여러 개가 오면(아침 처리: 떠남 · 입주 · 이사 준비 · 캠핑) 차례로 하나씩 (3-2)
        void OnNotice(string message)
        {
            if (Notice.activeSelf && Time.frameCount == noticeFrame)
            {
                pending.Enqueue(message);
                return;
            }
            pending.Clear();
            Show(message);
        }

        void Show(string message)
        {
            noticeText.text = HudText.Notice(message);
            Notice.SetActive(true);
            noticeUntil = Time.time + settings.hudNoticeSeconds;
            noticeFrame = Time.frameCount;
        }

        /// <summary>같은 때 와서 차례를 기다리는 알림</summary>
        public System.Collections.Generic.IReadOnlyCollection<string> PendingNotices => pending;
    }
}
