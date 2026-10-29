using System;
using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>마을 평가 결과: 항목별 점수 · 합계 · 별(1 ~ 5) · 다음 별 기준(5성이면 -1)</summary>
    public readonly struct RatingResult
    {
        public readonly int flowerPoints, residentPoints, friendshipPoints, treePoints, furniturePoints, encyclopediaPoints;
        public readonly int total, stars, nextThreshold;

        public RatingResult(int flowerPoints, int residentPoints, int friendshipPoints, int treePoints, int stars, int nextThreshold, int furniturePoints = 0, int encyclopediaPoints = 0)
        {
            this.encyclopediaPoints = encyclopediaPoints;
            this.flowerPoints = flowerPoints;
            this.residentPoints = residentPoints;
            this.friendshipPoints = friendshipPoints;
            this.treePoints = treePoints;
            this.furniturePoints = furniturePoints;
            total = flowerPoints + residentPoints + friendshipPoints + treePoints + furniturePoints + encyclopediaPoints;
            this.stars = stars;
            this.nextThreshold = nextThreshold;
        }

        public int PointsToNext => nextThreshold < 0 ? 0 : Math.Max(0, nextThreshold - total);
    }

    /// <summary>
    /// 마을 평가 계산(엔진 상태 없음). 결정 (2026-10-28, 3-5): 시들지 않은 꽃 · 마을 주민 수 · 마을 주민 친밀도 합 · 묘목에서 자란 나무에
    /// 항목별 점수를 곱해 더하고, 합계가 별 기준(★2 ~ ★5)을 넘은 만큼 별. 최소 ★1. BALANCE 15번.
    /// 가구(3-4e, BALANCE 16번): 놓인 가구마다 점수(같은 가구는 종류당 N개까지) + 한 주민의 테마 가구를 모두 놓으면 세트 보너스.
    /// 도감(3-6b · 3-7b, BALANCE 17 · 18번): 잡은 물고기 · 곤충 종류마다 점수.
    /// </summary>
    public static class VillageRating
    {
        public const int MaxStars = 5;

        /// <param name="starThresholds">★2 · ★3 · ★4 · ★5가 되는 합계(오름차순)</param>
        /// <param name="furniturePoints">FurniturePoints로 계산한 가구 점수(3-4e)</param>
        /// <param name="encyclopediaPoints">도감에 잡은 종류(물고기 3-6b · 곤충 3-7b) × 종류당 점수</param>
        public static RatingResult Evaluate(int flowers, int residents, int friendshipSum, int grownTrees,
            int flowerPoint, int residentPoint, int friendshipPoint, int treePoint, IReadOnlyList<int> starThresholds, int furniturePoints = 0, int encyclopediaPoints = 0)
        {
            int f = Math.Max(0, flowers) * flowerPoint;
            int r = Math.Max(0, residents) * residentPoint;
            int fr = friendshipSum * friendshipPoint;   // 음수면 깎임
            int t = Math.Max(0, grownTrees) * treePoint;
            int fu = Math.Max(0, furniturePoints);
            int fi = Math.Max(0, encyclopediaPoints);
            int total = f + r + fr + t + fu + fi;

            int stars = 1;
            int next = -1;
            for (int i = 0; i < starThresholds.Count && stars < MaxStars; i++)
            {
                if (total >= starThresholds[i])
                    stars++;
                else
                {
                    next = starThresholds[i];
                    break;
                }
            }
            return new RatingResult(f, r, fr, t, stars, next, fu, fi);
        }

        /// <summary>
        /// 놓인 가구 점수(3-4e): 가구마다 점수, 같은 가구(id)는 kindCap개까지만. setOwner가 있는 테마 가구는
        /// 한 주민의 서로 다른 가구가 setSize가지 모두 놓여 있으면 그 주민마다 한 번 setBonus(주민이 떠났어도 놓여 있으면)
        /// </summary>
        public static int FurniturePoints(IEnumerable<(string id, int point, string setOwner)> placed, int kindCap, int setBonus, int setSize)
        {
            var perKind = new Dictionary<string, int>();
            var sets = new Dictionary<string, HashSet<string>>();
            int sum = 0;
            foreach (var (id, point, setOwner) in placed)
            {
                perKind.TryGetValue(id, out int n);
                perKind[id] = ++n;
                if (n <= kindCap)
                    sum += Math.Max(0, point);
                if (string.IsNullOrEmpty(setOwner))
                    continue;
                if (!sets.TryGetValue(setOwner, out var pieces))
                    sets[setOwner] = pieces = new HashSet<string>();
                pieces.Add(id);
            }
            foreach (var pieces in sets.Values)
                if (setSize > 0 && pieces.Count >= setSize)
                    sum += setBonus;
            return sum;
        }

        /// <summary>게시판 대사 3줄: 별 · 합계 / 항목별 점수 / 다음 별까지 · 첫 목표</summary>
        public static List<string> BoardLines(RatingResult r, int firstGoalStars)
        {
            string next = r.nextThreshold < 0 ? "최고 평가예요!" : $"다음 별까지 {r.PointsToNext}점";
            string goal = r.stars >= firstGoalStars ? $"첫 목표 ★{firstGoalStars} 달성!" : $"첫 목표 ★{firstGoalStars}";
            return new List<string>
            {
                $"마을 평가 {StarText(r.stars)}  ({r.total}점, 오늘 아침 기준)",
                $"꽃 {r.flowerPoints} · 주민 {r.residentPoints} · 친밀도 {r.friendshipPoints} · 키운 나무 {r.treePoints} · 가구 {r.furniturePoints} · 도감 {r.encyclopediaPoints}",
                $"{next} · {goal}",
            };
        }

        /// <summary>"★★☆☆☆"</summary>
        public static string StarText(int stars)
        {
            int s = Math.Clamp(stars, 0, MaxStars);
            return new string('★', s) + new string('☆', MaxStars - s);
        }
    }
}
