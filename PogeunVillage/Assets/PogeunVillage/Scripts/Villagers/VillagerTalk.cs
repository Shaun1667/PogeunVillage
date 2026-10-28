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

        /// <summary>대사 한 줄 무작위(선물 답례 등). 비어 있으면 fallback</summary>
        public static string PickLine(IReadOnlyList<string> pool, IRandomSource random, string fallback)
        {
            var lines = new List<string>();
            AddRandom(lines, pool, random);
            return lines.Count > 0 ? lines[0] : fallback;
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

    /// <summary>
    /// 대화창 한 번(엔진 상태 없음): 줄을 차례로 넘기고 마지막 줄 다음에 끝남.
    /// 고르기(choices)가 있으면 마지막 줄에서 고르기가 함께 뜨고, 넘기면 고른 번호(Chosen)를 남기고 끝남(2-5).
    /// </summary>
    public class Dialogue
    {
        readonly IReadOnlyList<string> lines;
        readonly IReadOnlyList<string> choices;

        public Dialogue(string speaker, IReadOnlyList<string> lines, IReadOnlyList<string> choices = null)
        {
            if (lines == null || lines.Count == 0) throw new ArgumentException("대사가 없습니다.", nameof(lines));
            Speaker = speaker;
            this.lines = lines;
            this.choices = choices ?? Array.Empty<string>();
        }

        public string Speaker { get; set; }
        public int Index { get; private set; }
        public int LineCount => lines.Count;
        public bool IsFinished => Index >= lines.Count;
        public string Current => IsFinished ? null : lines[Index];

        public IReadOnlyList<string> Choices => choices;
        /// <summary>마지막 줄에서 고르기가 떠 있음</summary>
        public bool IsChoosing => choices.Count > 0 && Index == lines.Count - 1;
        public int ChoiceCursor { get; private set; }
        /// <summary>고르고 끝났으면 고른 번호, 아니면 -1</summary>
        public int Chosen { get; private set; } = -1;

        /// <summary>고르기 커서 이동(끝에서 멈춤)</summary>
        public void MoveChoice(int delta)
        {
            if (IsChoosing)
                ChoiceCursor = Math.Clamp(ChoiceCursor + delta, 0, choices.Count - 1);
        }

        /// <returns>다음 줄이 있으면 true, 끝났으면 false</returns>
        public bool Advance()
        {
            if (IsChoosing)
                Chosen = ChoiceCursor;
            if (!IsFinished)
                Index++;
            return !IsFinished;
        }
    }
}
