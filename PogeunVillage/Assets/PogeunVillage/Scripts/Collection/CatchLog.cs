using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>
    /// 도감 기록(엔진 상태 없음, 결정 2026-10-29 · 3-6b 물고기 · 3-7b 곤충): 종류마다 잡은 수. 처음 잡은 종류면 '새 종류'.
    /// </summary>
    public class CatchLog
    {
        readonly Dictionary<string, int> caught = new();

        /// <summary>잡은 종류 수</summary>
        public int Species => caught.Count;

        public bool Has(string id) => caught.ContainsKey(id);
        public int CaughtCount(string id) => caught.TryGetValue(id, out int n) ? n : 0;

        /// <summary>잡은 종류마다 수(저장 6-1a)</summary>
        public IEnumerable<KeyValuePair<string, int>> Entries => caught;

        /// <summary>불러오기(6-1a): 그 종류의 잡은 수(알림 없음)</summary>
        public void Restore(string id, int count)
        {
            if (count > 0) caught[id] = count;
            else caught.Remove(id);
        }

        /// <summary>잡음을 기록. 처음 잡은 종류면 true</summary>
        public bool Record(string id)
        {
            int n = CaughtCount(id);
            caught[id] = n + 1;
            return n == 0;
        }
    }
}
