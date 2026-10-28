using System.Collections.Generic;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 창(인벤토리 · 대화 · 선물 고르기)이 열린 동안 플레이어 조작(이동 · 상호작용 · 도구)을 멈춤(2-5).
    /// 누가 잡고 있는지 세어, 하나라도 잡고 있으면 멈춤. 가구 놓기 모드(3-4a)처럼 걷기는 허용하고 잡을 수도 있음.
    /// 멈추기는 바로, 풀기는 그 프레임 끝에 — 창을 닫은 F · Tab이 같은 프레임에 다시 상호작용 · 창 열기가 되지 않도록.
    /// </summary>
    public class PlayerLock : MonoBehaviour
    {
        [Tooltip("멈출 플레이어 동작(상호작용 · 도구)")]
        public Behaviour[] controlled = { };
        [Tooltip("이동(걷기를 허용한 잡기만 있으면 켜 둠)")]
        public Behaviour movement;

        readonly Dictionary<object, bool> holders = new();  // 잡은 쪽 → 걷기 허용
        bool releasePending;

        /// <summary>아무도 잡지 않음. 방금 푼 프레임은 아직 잡힌 것으로 봄(그 프레임의 Tab · F가 새 창을 열지 않도록)</summary>
        public bool IsFree => holders.Count == 0 && !releasePending;
        public bool IsHeldBy(object holder) => holders.ContainsKey(holder);

        public void Acquire(object holder, bool allowMove = false)
        {
            holders[holder] = allowMove;
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
            bool free = holders.Count == 0;
            foreach (var b in controlled)
                if (b) b.enabled = free;
            if (!movement)
                return;
            bool canMove = true;
            foreach (bool allow in holders.Values)
                canMove &= allow;
            movement.enabled = canMove;
        }
    }
}
