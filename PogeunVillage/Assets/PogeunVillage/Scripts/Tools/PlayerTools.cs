using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PogeunVillage
{
    /// <summary>
    /// 도구 들기 · 바꾸기 · 휘두르기 · 내구도. 결정 (2026-10-27):
    /// 숫자 1~5로 들기(같은 키 = 내려놓기), Space로 휘두르기. 패드는 십자키 좌우로 바꾸고 X로 휘두르기.
    /// 휘둘렀을 때 앞의 상호작용 대상(PlayerInteractor.Target)이 도구의 hitKinds면 '맞음' → 대상에 HitByTool, 내구도 -1, 0이면 부서짐.
    /// 칸을 다루는 도구(usesGroundCell: 삽 · 물뿌리개)는 대신 플레이어의 IToolAction들에 묻고, 무언가를 했으면 '맞음'.
    /// </summary>
    public class PlayerTools : MonoBehaviour
    {
        public WorldSettings settings;
        public PlayerInventory inventory;
        public PlayerInteractor interactor;
        [Tooltip("ToolKind 순서(숫자키 1~5)대로")]
        public ToolDefinition[] tools = new ToolDefinition[5];
        [Tooltip("손에 든 모습. tools와 같은 순서")]
        public GameObject[] holdVisuals = new GameObject[5];
        [Tooltip("휘두를 때 돌리는 축")]
        public Transform swingPivot;

        /// <summary>든 도구가 바뀜(빈손이면 null)</summary>
        public event Action<ToolDefinition> EquippedChanged;
        /// <summary>휘두름. 맞은 대상(없으면 null)</summary>
        public event Action<ToolDefinition, Interactable> ToolUsed;

        public int EquippedSlot { get; private set; } = -1;
        public ToolDefinition Equipped { get; private set; }
        public bool IsSwinging { get; private set; }

        string[] toolIds;
        Quaternion pivotRest;
        IToolAction[] actions;

        void Awake()
        {
            if (!settings || !inventory || !interactor || tools == null || Array.Exists(tools, t => !t))
            {
                Debug.LogError($"{name}: 도구 설정(settings · inventory · interactor · tools)이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            toolIds = Array.ConvertAll(tools, t => t.id);
            actions = GetComponents<IToolAction>();
            if (swingPivot)
                pivotRest = swingPivot.localRotation;
            ShowHeld();
        }

        void Start()
        {
            if (enabled)
                inventory.Inventory.SlotChanged += OnSlotChanged;
        }

        void OnDestroy()
        {
            if (inventory && inventory.Inventory != null)
                inventory.Inventory.SlotChanged -= OnSlotChanged;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            var slots = inventory.Inventory.Slots;

            if (keyboard != null)
            {
                for (int i = 0; i < tools.Length; i++)
                    if (keyboard[Key.Digit1 + i].wasPressedThisFrame)
                        SelectKey(i);
            }
            if (gamepad != null)
            {
                if (gamepad.dpad.right.wasPressedThisFrame) Equip(ToolBelt.Cycle(slots, toolIds, EquippedSlot, +1));
                if (gamepad.dpad.left.wasPressedThisFrame) Equip(ToolBelt.Cycle(slots, toolIds, EquippedSlot, -1));
            }

            bool use = (keyboard?.spaceKey.wasPressedThisFrame ?? false) || (gamepad?.buttonWest.wasPressedThisFrame ?? false);
            if (use && Equipped && !IsSwinging)
                StartCoroutine(Swing());
        }

        void SelectKey(int index)
        {
            int slot = ToolBelt.Select(inventory.Inventory.Slots, toolIds[index], EquippedSlot);
            if (slot == EquippedSlot && ToolBelt.FindSlot(inventory.Inventory.Slots, toolIds[index]) < 0)
                Debug.Log($"[도구] {tools[index].displayName}이(가) 없어요", this);
            Equip(slot);
        }

        void Equip(int slot)
        {
            if (slot == EquippedSlot)
                return;
            EquippedSlot = slot;
            Equipped = slot >= 0 ? ToolAt(slot) : null;
            ShowHeld();
            EquippedChanged?.Invoke(Equipped);
        }

        ToolDefinition ToolAt(int slot)
        {
            string id = inventory.Inventory.Slots[slot].itemId;
            return Array.Find(tools, t => t.id == id);
        }

        void ShowHeld()
        {
            for (int i = 0; i < holdVisuals.Length; i++)
                if (holdVisuals[i])
                    holdVisuals[i].SetActive(Equipped && tools[i] == Equipped);
        }

        // 든 칸이 바뀌면(부서짐 등) 그 칸에 아직 같은 도구가 있는지 확인
        void OnSlotChanged(int index)
        {
            if (index != EquippedSlot)
                return;
            var slot = inventory.Inventory.Slots[index];
            if (slot.IsEmpty || !Equipped || slot.itemId != Equipped.id)
                Equip(-1);
        }

        IEnumerator Swing()
        {
            IsSwinging = true;
            ToolDefinition tool = Equipped;
            int slot = EquippedSlot;

            float half = settings.toolSwingTime * 0.5f;
            yield return RotatePivot(0f, 1f, half);

            bool hit;
            Interactable target = null;
            if (tool.usesGroundCell)
            {
                // 칸을 다루는 도구: 동작 중 하나라도 무언가를 했으면 맞음
                hit = false;
                foreach (var action in actions)
                    if (action.TryUse(this, tool)) { hit = true; break; }
                if (!hit) Debug.Log($"[도구] {tool.displayName} 허공", this);
            }
            else
            {
                target = interactor.Target;
                hit = target && Array.IndexOf(tool.hitKinds, target.kind) >= 0;
                Debug.Log(hit ? $"[도구] {tool.displayName} → {target.displayName}" : $"[도구] {tool.displayName} 허공", this);
            }
            ToolUsed?.Invoke(tool, hit ? target : null);
            if (hit && target)
                target.ReceiveToolHit(this, tool);
            bool stillHeld = inventory.Inventory.Slots[slot].itemId == tool.id;
            if (hit && stillHeld && inventory.Inventory.UseDurability(slot))
                Debug.Log($"[도구] {tool.displayName}이(가) 부서졌어요", this);

            yield return RotatePivot(1f, 0f, half);
            IsSwinging = false;
        }

        IEnumerator RotatePivot(float from, float to, float duration)
        {
            if (!swingPivot)
                yield break;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.Lerp(from, to, t / duration);
                swingPivot.localRotation = pivotRest * Quaternion.Euler(settings.toolSwingAngle * k, 0f, 0f);
                yield return null;
            }
            swingPivot.localRotation = pivotRest * Quaternion.Euler(settings.toolSwingAngle * to, 0f, 0f);
        }
    }
}
