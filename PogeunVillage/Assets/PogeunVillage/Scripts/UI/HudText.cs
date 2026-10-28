using System.Globalization;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// HUD에 쓰는 글자 만들기(엔진 상태 없음) — 화면과 테스트가 같은 글자를 씀. 결정 (2026-10-27, 2-3).
    /// </summary>
    public static class HudText
    {
        public static string DayAndTime(int day, int hour, int minute) => $"{day}일차  {hour:00}:{minute:00}";

        public static string Moa(int moa) => $"{moa.ToString("N0", CultureInfo.InvariantCulture)} 모아";

        /// <summary>든 도구. 빈손이면 빈 글자</summary>
        public static string HeldTool(string displayName, int durability, int maxDurability) =>
            string.IsNullOrEmpty(displayName) ? "" : $"{displayName}  {durability}/{maxDurability}";

        /// <summary>화면 알림 글자: 콘솔용 머리말 "[상점] " 등을 뗌</summary>
        public static string Notice(string message)
        {
            if (string.IsNullOrEmpty(message)) return "";
            if (message[0] != '[') return message;
            int end = message.IndexOf(']');
            return end < 0 ? message : message.Substring(end + 1).TrimStart();
        }

        /// <summary>인벤토리 칸 글자: 빈 칸 "", 도구 "도끼 (49)", 겹친 물건 "사과 ×3", 하나 "사과"</summary>
        public static string Slot(string displayName, int count, int durability)
        {
            if (count <= 0) return "";
            if (durability > 0) return $"{displayName} ({durability})";
            return count > 1 ? $"{displayName} ×{count}" : displayName;
        }

        /// <summary>대화창 이름: "주민 01  친밀도 3" (결정 2026-10-28, 2-5: 그레이박스 동안 숫자로 보임)</summary>
        public static string Speaker(string displayName, int friendship) => $"{displayName}  친밀도 {friendship}";

        /// <summary>이사 준비 중인 주민: "주민 01  친밀도 -20 · 이사 준비 중" (3-2)</summary>
        public static string Speaker(string displayName, int friendship, bool preparingToLeave) =>
            preparingToLeave ? $"{Speaker(displayName, friendship)} · 이사 준비 중" : Speaker(displayName, friendship);

        /// <summary>오늘 부탁이 남은 주민: "주민 01  친밀도 3 · 부탁 있음" (3-3)</summary>
        public static string Speaker(string displayName, int friendship, bool preparingToLeave, bool hasRequest) =>
            hasRequest ? $"{Speaker(displayName, friendship, preparingToLeave)} · 부탁 있음" : Speaker(displayName, friendship, preparingToLeave);

        /// <summary>부탁 대사: "{0} {1}개" 자리에 물건 이름 · 개수(틀이 잘못돼도 깨지지 않게)</summary>
        public static string RequestLine(string template, string itemName, int count)
        {
            try { return string.Format(template, itemName, count); }
            catch (System.FormatException) { return $"{itemName} {count}개만 가져다줄 수 있어?"; }
        }

        /// <summary>캠핑 주민 대화창 이름: "주민 07  캠핑 중 · 친밀도 3/5", 확정 뒤 "주민 07  캠핑 중 · 이사 오기로 함" (3-1)</summary>
        public static string CampSpeaker(string displayName, int value, int threshold, bool confirmed) =>
            confirmed ? $"{displayName}  캠핑 중 · 이사 오기로 함" : $"{displayName}  캠핑 중 · 친밀도 {value}/{threshold}";

        /// <summary>대화창 고르기: 고른 것만 강조색 · 굵게, 한 줄에 하나</summary>
        public static string Choices(System.Collections.Generic.IReadOnlyList<string> choices, int cursor, string highlightHex)
        {
            var text = new System.Text.StringBuilder();
            for (int i = 0; i < choices.Count; i++)
            {
                if (i > 0) text.Append('\n');
                text.Append(i == cursor ? $"<color={highlightHex}><b>{choices[i]}</b></color>" : choices[i]);
            }
            return text.ToString();
        }

        /// <summary>제작 창 한 줄(3-4b): "나무 울타리 · 목재 2", 안 열렸으면 "통나무 의자 · ★2에 열려요"</summary>
        public static string Recipe(string displayName, int woodCost, int unlockStars, int stars) =>
            CraftRules.IsUnlocked(unlockStars, stars) ? $"{displayName} · 목재 {woodCost}" : $"{displayName} · ★{unlockStars}에 열려요";

        /// <summary>제작 창 목록: 고른 줄은 강조, 지금 못 만드는 줄은 흐리게</summary>
        public static string RecipeList(System.Collections.Generic.IReadOnlyList<string> lines, System.Collections.Generic.IReadOnlyList<bool> usable,
            int cursor, string highlightHex, string mutedHex)
        {
            var text = new System.Text.StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                if (i > 0) text.Append('\n');
                string line = i == cursor ? $"<b>{lines[i]}</b>" : lines[i];
                string color = i == cursor ? highlightHex : usable[i] ? null : mutedHex;
                text.Append(color == null ? line : $"<color={color}>{line}</color>");
            }
            return text.ToString();
        }
    }

    /// <summary>
    /// 인벤토리 창의 고른 칸 움직이기(엔진 상태 없음). 칸은 columns열로 줄지어 놓이고, 가장자리에서는 멈춤(감싸지 않음).
    /// </summary>
    public static class InventoryCursor
    {
        public static int Move(int index, int dx, int dy, int columns, int slotCount)
        {
            int rows = (slotCount + columns - 1) / columns;
            int x = Mathf.Clamp(index % columns + dx, 0, columns - 1);
            int y = Mathf.Clamp(index / columns + dy, 0, rows - 1);
            return Mathf.Min(y * columns + x, slotCount - 1);
        }
    }
}
