using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>
    /// 테마 가구 친밀도 보상(엔진 상태 없음, 결정 2026-10-28 · 3-4d): 주민마다 받은 개수를 셈.
    /// 친밀도가 다음 기준(10 · 25 · 45 · 70)에 닿아 있으면 다음 보상이 "받을 차례". 한 번 준 것은 친밀도가 내려갔다 올라도 다시 주지 않음.
    /// 주는 때(대화 끝에 하나씩 · 가득 차면 다음 대화)는 Village가 정함.
    /// </summary>
    public class ThemeRewards
    {
        readonly int[] thresholds;
        readonly Dictionary<string, int> given = new();

        public ThemeRewards(int[] thresholds) => this.thresholds = thresholds;

        public int Given(string id) => given.TryGetValue(id, out int n) ? n : 0;

        /// <returns>받을 차례인 보상 번호(0 = 첫 번째), 없으면 -1</returns>
        public int Due(string id, int friendship)
        {
            int next = Given(id);
            return next < thresholds.Length && friendship >= thresholds[next] ? next : -1;
        }

        /// <summary>주민마다 받은 개수(저장 6-1b)</summary>
        public IEnumerable<KeyValuePair<string, int>> Entries => given;

        /// <summary>불러오기(6-1b)</summary>
        public void Restore(string id, int count)
        {
            if (count > 0) given[id] = count;
            else given.Remove(id);
        }

        /// <summary>보상 하나를 줌(다음 차례로)</summary>
        public void MarkGiven(string id) => given[id] = Given(id) + 1;
    }
}
