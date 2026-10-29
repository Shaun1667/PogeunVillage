using System;
using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>
    /// 마을 주민 · 집터 규칙(엔진 상태 없음). 결정 (2026-10-19): 전체 주민 중 무작위 N명으로 시작(같은 시드 = 같은 주민),
    /// 집은 빈 집터 중 무작위. 집터마다 사는 주민 id(빈 집터 = null). 바뀐 집터만 PlotChanged로 알림.
    /// 떠남 · 입주(캠핑 이주)는 2-5 · 2-6에서 MoveIn · MoveOut을 씀.
    /// </summary>
    public class VillageResidents
    {
        readonly string[] plots;

        public event Action<int> PlotChanged;

        public VillageResidents(int plotCount)
        {
            if (plotCount <= 0) throw new ArgumentOutOfRangeException(nameof(plotCount));
            plots = new string[plotCount];
        }

        public IReadOnlyList<string> Plots => plots;

        /// <summary>불러오기(6-1b): 집터마다 주민(빈 문자열 · null = 빈 집터). 바뀐 집터만 알림</summary>
        public void Restore(IReadOnlyList<string> saved)
        {
            for (int i = 0; i < plots.Length; i++)
            {
                string id = i < saved.Count && !string.IsNullOrEmpty(saved[i]) ? saved[i] : null;
                if (plots[i] == id) continue;
                plots[i] = id;
                PlotChanged?.Invoke(i);
            }
        }

        public int Count
        {
            get
            {
                int count = 0;
                foreach (string id in plots)
                    if (id != null) count++;
                return count;
            }
        }

        public int PlotOf(string id) => Array.IndexOf(plots, id);

        public bool IsResident(string id) => id != null && PlotOf(id) >= 0;

        /// <summary>빈 집터 중 무작위 한 곳으로 이사 옴. 들어간 집터 번호, 빈 집터가 없거나 이미 살면 -1</summary>
        public int MoveIn(string id, IRandomSource random)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("주민 id가 비어 있습니다.", nameof(id));
            if (IsResident(id))
                return -1;
            var empty = new List<int>();
            for (int i = 0; i < plots.Length; i++)
                if (plots[i] == null) empty.Add(i);
            if (empty.Count == 0)
                return -1;
            int plot = empty[random.Next(empty.Count)];
            plots[plot] = id;
            PlotChanged?.Invoke(plot);
            return plot;
        }

        /// <returns>살고 있어서 집터를 비웠으면 true</returns>
        public bool MoveOut(string id)
        {
            int plot = PlotOf(id);
            if (id == null || plot < 0)
                return false;
            plots[plot] = null;
            PlotChanged?.Invoke(plot);
            return true;
        }

        /// <summary>전체 주민 중 겹치지 않게 count명을 무작위로 고름(같은 난수 = 같은 결과). 전체보다 많으면 전체</summary>
        public static List<string> PickStarting(IReadOnlyList<string> allIds, int count, IRandomSource random)
        {
            var pool = new List<string>(allIds);
            int n = Math.Min(Math.Max(count, 0), pool.Count);
            for (int i = 0; i < n; i++)
            {
                int j = i + random.Next(pool.Count - i);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            pool.RemoveRange(n, pool.Count - n);
            return pool;
        }

        /// <summary>시작 주민: 무작위 count명을 고른 순서대로 빈 집터 중 무작위에 입주</summary>
        public static VillageResidents CreateStarting(int plotCount, IReadOnlyList<string> allIds, int count, IRandomSource random)
        {
            var residents = new VillageResidents(plotCount);
            foreach (string id in PickStarting(allIds, count, random))
                residents.MoveIn(id, random);
            return residents;
        }
    }
}
