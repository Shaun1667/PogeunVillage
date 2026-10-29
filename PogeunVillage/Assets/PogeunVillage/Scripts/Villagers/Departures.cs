using System;
using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>
    /// 주민 이사 준비 · 떠남(엔진 상태 없음). 결정 (2026-10-19): 아침에 친밀도가 기준(-20)인 주민은 그날 이사 준비(GDD 4-5 ③),
    /// 이사 준비 중에 기준 초과로 회복하면 취소, 아니면 다음 날 아침 떠남(①). BALANCE 2번 검산 2-9 ~ 2-11.
    /// </summary>
    public class Departures
    {
        readonly int leaveThreshold;
        readonly HashSet<string> preparing = new();

        public Departures(int leaveThreshold) => this.leaveThreshold = leaveThreshold;

        public bool IsPreparing(string id) => preparing.Contains(id);
        /// <summary>이사 준비 중인 주민(저장 6-1b, id 차례)</summary>
        public List<string> Preparing
        {
            get
            {
                var list = new List<string>(preparing);
                list.Sort(StringComparer.Ordinal);
                return list;
            }
        }

        /// <summary>불러오기(6-1b)</summary>
        public void Restore(IEnumerable<string> ids)
        {
            preparing.Clear();
            foreach (string id in ids) preparing.Add(id);
        }
        public int PreparingCount => preparing.Count;

        /// <summary>아침 ①: 이사 준비 중이던 주민 모두 떠남(목록을 돌려주고 비움, 같은 순서 = id 순)</summary>
        public List<string> TakeLeaving()
        {
            var leaving = new List<string>(preparing);
            leaving.Sort(StringComparer.Ordinal);
            preparing.Clear();
            return leaving;
        }

        /// <summary>아침 ③: 친밀도가 기준 이하인 주민이 이사 준비를 시작. 새로 시작한 주민</summary>
        public List<string> StartPreparing(IEnumerable<string> residents, Func<string, int> friendship)
        {
            var started = new List<string>();
            foreach (string id in residents)
                if (id != null && friendship(id) <= leaveThreshold && preparing.Add(id))
                    started.Add(id);
            started.Sort(StringComparer.Ordinal);
            return started;
        }

        /// <summary>친밀도가 바뀜: 이사 준비 중인데 기준을 넘으면 취소. 취소됐으면 true</summary>
        public bool OnFriendshipChanged(string id, int value) => value > leaveThreshold && preparing.Remove(id);
    }
}
