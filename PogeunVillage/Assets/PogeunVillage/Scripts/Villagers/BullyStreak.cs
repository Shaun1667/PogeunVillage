using System;
using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>
    /// 괴롭히기 연속 횟수(엔진 상태 없음). 결정 (2026-10-19): 잠자리채로 마을 주민을 연속으로 때린 횟수를 주민마다 따로 셈,
    /// N번마다 한 번 깎임. 연속은 그 주민에게 대화 · 선물하거나(Reset) 마지막으로 때린 뒤 timeout이 지나면 끊김
    /// (결정 2026-10-28: 실제 3분, 일시정지 제외 — 시간은 부르는 쪽이 넘김). BALANCE 2번 검산 2-1 ~ 2-7.
    /// </summary>
    public class BullyStreak
    {
        readonly int hitsPerPenalty;
        readonly double timeoutSeconds;
        readonly Dictionary<string, (int count, double lastHit)> streaks = new();

        public BullyStreak(int hitsPerPenalty, double timeoutSeconds)
        {
            if (hitsPerPenalty <= 0) throw new ArgumentOutOfRangeException(nameof(hitsPerPenalty));
            this.hitsPerPenalty = hitsPerPenalty;
            this.timeoutSeconds = timeoutSeconds;
        }

        /// <summary>지금 이어지고 있는 연속 횟수(시간이 지났으면 0)</summary>
        public int Count(string id, double now) =>
            streaks.TryGetValue(id, out var s) && now - s.lastHit < timeoutSeconds ? s.count : 0;

        /// <summary>한 번 때림. 이번 타로 깎일 차례가 되면 true(N번째, 2N번째 …)</summary>
        public bool Hit(string id, double now)
        {
            int count = Count(id, now) + 1;
            streaks[id] = (count, now);
            return count % hitsPerPenalty == 0;
        }

        /// <summary>대화 · 선물: 연속 끊김</summary>
        public void Reset(string id) => streaks.Remove(id);
    }
}
