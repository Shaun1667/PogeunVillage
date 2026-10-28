using System.Collections.Generic;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 창(인벤토리 · 대화 · 선물 고르기)이 열린 동안 플레이어 조작(이동 · 상호작용 · 도구)을 멈춤(2-5).
    /// 누가 잡고 있는지 세어, 하나라도 잡고 있으면 멈춤. 멈추기는 바로, 풀기는 그 프레임 끝에 —
    /// 창을 닫은 F · Tab이 같은 프레임에 다시 상호작용 · 창 열기가 되지 않도록.
    /// </summary>
    public class PlayerLock : MonoBehaviour
    {
        [Tooltip("멈출 플레이어 동작(이동 · 상호작용 · 도구)")]
        public Behaviour[] controlled = { };

        readonly HashSet<object> holders = new();
        bool releasePending;

        public bool IsFree => holders.Count == 0;
        public bool IsHeldBy(object holder) => holders.Contains(holder);

        public void Acquire(object holder)
        {
            holders.Add(holder);
            Apply();
        }

        public void Release(object holder)
        {
            if (holders.Remove(holder))
                releasePending = true;
        }

        void LateUpdate()
        {
            if (!releasePending)
                return;
            releasePending = false;
            Apply();
        }

        void Apply()
        {
            foreach (var b in controlled)
                if (b) b.enabled = IsFree;
        }
    }
}
