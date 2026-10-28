using System;
using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>주민 일과 · 대사 고르기 규칙(엔진 상태 없음).</summary>
    public static class VillagerTalk
    {
        /// <summary>결정 (2026-10-28): 밤이 아니면 밖(아침 집에서 나와 배회), 밤이 되면 집으로</summary>
        public static bool IsOutside(DayPeriod period) => period != DayPeriod.Night;

        /// <summary>
        /// 말 걸 때 보여 줄 대사: 시간대 인사 1줄 + 잡담 1줄(각각 무작위). 둘 다 비면 "…".
        /// 밤 인사는 따로 없어서 저녁 인사를 씀(밤에는 집에 가는 길에만 만남).
        /// </summary>
        public static List<string> Compose(DayPeriod period, IReadOnlyList<string> morning, IReadOnlyList<string> daytime,
            IReadOnlyList<string> evening, IReadOnlyList<string> smallTalk, IRandomSource random)
        {
            var greetings = period switch
            {
                DayPeriod.Morning => morning,
                DayPeriod.Daytime => daytime,
                _ => evening,
            };
            var lines = new List<string>();
            AddRandom(lines, greetings, random);
            AddRandom(lines, smallTalk, random);
            if (lines.Count == 0)
                lines.Add("…");
            return lines;
        }

        static void AddRandom(List<string> lines, IReadOnlyList<string> pool, IRandomSource random)
        {
            if (pool == null || pool.Count == 0)
                return;
            string line = pool[random.Next(pool.Count)];
            if (!string.IsNullOrEmpty(line))
                lines.Add(line);
        }
    }

    /// <summary>대화창 한 번(엔진 상태 없음): 줄을 차례로 넘기고 마지막 줄 다음에 끝남.</summary>
    public class Dialogue
    {
        readonly IReadOnlyList<string> lines;

        public Dialogue(string speaker, IReadOnlyList<string> lines)
        {
            if (lines == null || lines.Count == 0) throw new ArgumentException("대사가 없습니다.", nameof(lines));
            Speaker = speaker;
            this.lines = lines;
        }

        public string Speaker { get; }
        public int Index { get; private set; }
        public int LineCount => lines.Count;
        public bool IsFinished => Index >= lines.Count;
        public string Current => IsFinished ? null : lines[Index];

        /// <returns>다음 줄이 있으면 true, 끝났으면 false</returns>
        public bool Advance()
        {
            if (!IsFinished)
                Index++;
            return !IsFinished;
        }
    }
}
