using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 임시 도구 상자(광장): 상호작용하면 도구 5종을 하나씩 인벤토리에 넣습니다(여러 번 가능).
    /// 결정 (2026-10-27): 2-2 상점이 생기면 없앰. "시작 도구 없음" 규칙은 그대로.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class ToolBox : MonoBehaviour
    {
        public ToolDefinition[] tools = new ToolDefinition[5];

        Interactable interactable;

        void Awake() => interactable = GetComponent<Interactable>();
        void OnEnable() => interactable.Interacted += OnOpened;
        void OnDisable() => interactable.Interacted -= OnOpened;

        void OnOpened(Interactable _, PlayerInteractor by)
        {
            var inventory = by.GetComponent<PlayerInventory>();
            if (!inventory)
            {
                Debug.LogError($"{name}: 플레이어에 PlayerInventory가 없습니다.", this);
                return;
            }
            foreach (var tool in tools)
                if (tool && !inventory.TryAddTool(tool))
                    break;
        }
    }
}
