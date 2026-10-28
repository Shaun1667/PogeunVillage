using System;
using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>
    /// 캠핑 방문 주민 고르기(엔진 상태 없음). 결정 (2026-10-19): 매일 아침 마을에 살지 않는 주민(떠난 주민 포함) 중 무작위 1명,
    /// 어제 온 주민은 제외(이틀 연속 없음), 마을이 꽉 차면(확정 주민 포함 12명) 아무도 오지 않음. BALANCE 1번 검산 1-9 ~ 1-11.
    /// </summary>
    public static class CampVisits
    {
        /// <param name="residentCount">마을 인원(이주 확정 주민 포함 — 3-1)</param>
        /// <returns>오늘 올 주민 id, 아무도 오지 않으면 null</returns>
        public static string PickVisitor(IReadOnlyList<string> allIds, Func<string, bool> isResident, string yesterday,
            int residentCount, int maxResidents, IRandomSource random)
        {
            if (residentCount >= maxResidents)
                return null;
            var candidates = new List<string>();
            foreach (string id in allIds)
                if (!string.IsNullOrEmpty(id) && !isResident(id) && id != yesterday)
                    candidates.Add(id);
            return candidates.Count == 0 ? null : candidates[random.Next(candidates.Count)];
        }
    }
}
