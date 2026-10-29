using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // 4-1 플레이 기록 CSV 글자
    public class PlayLogTests
    {
        [Test]
        public void Row_AllColumns_InOrder()
        {
            var row = new PlayLogRow(65.24f, 2, "07:00", "hour", 2800, 1, 280, 6, 12, 5, 10, 3, "");
            Assert.AreEqual("65.2,2,07:00,hour,2800,1,280,6,12,5,10,3,", PlayLog.Row(row));
            Assert.AreEqual(13, PlayLog.Header.Split(',').Length, "열 수가 같음");
        }

        [Test]
        public void Text_WithComma_OrQuote_IsQuoted()
        {
            Assert.AreEqual("[상점] 목재 10개를 200 모아에 팔았어요", PlayLog.Escape("[상점] 목재 10개를 200 모아에 팔았어요"));
            Assert.AreEqual("\"사과, 복숭아\"", PlayLog.Escape("사과, 복숭아"));
            Assert.AreEqual("\"\"\"안녕\"\"\"", PlayLog.Escape("\"안녕\""));
            Assert.AreEqual("", PlayLog.Escape(null));
        }
    }
}
