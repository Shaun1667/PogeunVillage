using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PogeunVillage
{
    /// <summary>플레이 기록 한 줄(재미 검증 4-1 측정용)</summary>
    public readonly struct PlayLogRow
    {
        public readonly float realSeconds;
        public readonly int day;
        public readonly string time, kind;
        public readonly int moa, stars, score, residents, friendshipSum, slotsUsed, wood, placedFurniture;
        public readonly string text;

        public PlayLogRow(float realSeconds, int day, string time, string kind, int moa, int stars, int score, int residents,
            int friendshipSum, int slotsUsed, int wood, int placedFurniture, string text)
        {
            this.realSeconds = realSeconds;
            this.day = day;
            this.time = time;
            this.kind = kind;
            this.moa = moa;
            this.stars = stars;
            this.score = score;
            this.residents = residents;
            this.friendshipSum = friendshipSum;
            this.slotsUsed = slotsUsed;
            this.wood = wood;
            this.placedFurniture = placedFurniture;
            this.text = text;
        }
    }

    /// <summary>플레이 기록 CSV 글자(엔진 상태 없음). 엑셀에서 한글이 깨지지 않게 파일은 UTF-8 BOM으로 씀(PlayLogger)</summary>
    public static class PlayLog
    {
        public const string Header = "real_sec,day,time,kind,moa,stars,score,residents,friendship_sum,slots_used,wood,placed_furniture,text";

        public static string Row(PlayLogRow r)
        {
            var c = CultureInfo.InvariantCulture;
            var fields = new List<string>
            {
                r.realSeconds.ToString("0.0", c), r.day.ToString(c), r.time, r.kind,
                r.moa.ToString(c), r.stars.ToString(c), r.score.ToString(c), r.residents.ToString(c),
                r.friendshipSum.ToString(c), r.slotsUsed.ToString(c), r.wood.ToString(c), r.placedFurniture.ToString(c),
                Escape(r.text),
            };
            return string.Join(",", fields);
        }

        /// <summary>쉼표 · 따옴표 · 줄바꿈이 있으면 따옴표로 감쌈</summary>
        public static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            if (text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
                return text;
            var sb = new StringBuilder("\"");
            sb.Append(text.Replace("\"", "\"\""));
            sb.Append('"');
            return sb.ToString();
        }
    }
}
