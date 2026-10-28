using System;
using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>
    /// 주민 친밀도 규칙(엔진 상태 없음). 결정 (2026-10-19): 그날 첫 대화 +1, 선물 1개당 +2(종류 무관), 하루에 올릴 수 있는 양 5(넘는 만큼 버림),
    /// 범위 -20 ~ 100. 감소(구멍에 빠짐 · 괴롭히기)는 하루 상한과 별개이고 하한에서 멈춤. 값이 바뀐 주민만 Changed로 알림.
    /// </summary>
    public class Friendship
    {
        readonly int talkGain, giftGain, dailyCap, min, max;
        readonly Dictionary<string, int> values = new();
        readonly Dictionary<string, int> gainedToday = new();
        readonly HashSet<string> talkedToday = new();

        /// <summary>주민 id, 새 값</summary>
        public event Action<string, int> Changed;

        public Friendship(int talkGain, int giftGain, int dailyCap, int min, int max)
        {
            if (min > max) throw new ArgumentException("하한이 최댓값보다 큽니다.");
            this.talkGain = talkGain;
            this.giftGain = giftGain;
            this.dailyCap = dailyCap;
            this.min = min;
            this.max = max;
        }

        public int Get(string id) => values.TryGetValue(id, out int v) ? v : 0;

        public int GainedToday(string id) => gainedToday.TryGetValue(id, out int g) ? g : 0;

        public bool HasTalkedToday(string id) => talkedToday.Contains(id);

        /// <summary>말 걸기: 그날 첫 대화만 오름. 실제로 오른 양</summary>
        public int Talk(string id) => talkedToday.Add(id) ? Gain(id, talkGain) : 0;

        /// <summary>선물 1개. 상한을 채웠으면 받기만 하고 0</summary>
        public int Gift(string id) => Gain(id, giftGain);

        /// <summary>감소(하루 상한과 별개, 하한에서 멈춤). 실제로 바뀐 양(0 이하)</summary>
        public int Lose(string id, int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            int before = Get(id);
            return Set(id, Math.Max(min, before - amount)) - before;
        }

        /// <summary>새 날: 첫 대화 · 하루 상한을 다시 셈(값은 그대로)</summary>
        public void NewDay()
        {
            gainedToday.Clear();
            talkedToday.Clear();
        }

        int Gain(string id, int amount)
        {
            int before = Get(id);
            int allowed = Math.Min(amount, Math.Min(dailyCap - GainedToday(id), max - before));
            if (allowed <= 0)
                return 0;
            gainedToday[id] = GainedToday(id) + allowed;
            return Set(id, before + allowed) - before;
        }

        int Set(string id, int value)
        {
            if (Get(id) == value)
                return value;
            values[id] = value;
            Changed?.Invoke(id, value);
            return value;
        }
    }
}
