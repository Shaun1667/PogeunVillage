using System;
using System.Collections.Generic;

namespace PogeunVillage
{
    public enum StartChoice
    {
        None,
        Continue,
        NewGame,
    }

    /// <summary>
    /// 시작 화면 고르기(엔진 상태 없음, 결정 2026-10-29 · 6-1d): '이어하기(요약) / 새로 시작'.
    /// 새로 시작은 한 번 더 묻고(저장이 지워짐) 처음 커서는 '아니요'. 위아래는 끝에서 멈춤.
    /// </summary>
    public class StartMenu
    {
        readonly string continueLine;

        public StartMenu(string continueLine) => this.continueLine = continueLine;

        public bool Confirming { get; private set; }
        public int Cursor { get; private set; }

        public string Prompt => Confirming ? "저장이 지워져요. 정말 새로 시작할까요?" : "어떻게 시작할까요?";
        public IReadOnlyList<string> Lines => Confirming ? new[] { "아니요", "예, 새로 시작" } : new[] { continueLine, "새로 시작" };

        public void Move(int delta) => Cursor = Math.Clamp(Cursor + delta, 0, Lines.Count - 1);

        /// <summary>고르기(F)</summary>
        public StartChoice Press()
        {
            if (!Confirming)
            {
                if (Cursor == 0) return StartChoice.Continue;
                Confirming = true;
                Cursor = 0;  // '아니요'부터
                return StartChoice.None;
            }
            if (Cursor == 1) return StartChoice.NewGame;
            Back();
            return StartChoice.None;
        }

        /// <summary>확인에서 돌아가기(Esc · 아니요)</summary>
        public void Back()
        {
            if (!Confirming) return;
            Confirming = false;
            Cursor = 1;
        }

        /// <summary>"이어하기 · 3일차 · 3,450 모아 · ★★☆☆☆"</summary>
        public static string ContinueLine(int day, int moa, int stars) =>
            $"이어하기 · {day}일차 · {HudText.Moa(moa)} · {VillageRating.StarText(Math.Max(1, stars))}";
    }
}
