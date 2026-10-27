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
