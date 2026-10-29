using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // 6-1d 시작 화면 고르기: 이어하기 / 새로 시작은 한 번 더 확인(처음 커서 '아니요')
    public class StartMenuTests
    {
        [Test]
        public void Continue_FirstLine()
        {
            var menu = new StartMenu("이어하기");
            Assert.AreEqual(StartChoice.Continue, menu.Press());
        }

        [Test]
        public void NewGame_AsksAgain_DefaultNo()
        {
            var menu = new StartMenu("이어하기");
            menu.Move(1);
            Assert.AreEqual(StartChoice.None, menu.Press());
            Assert.IsTrue(menu.Confirming);
            Assert.AreEqual(0, menu.Cursor, "처음은 '아니요'");
            Assert.AreEqual(StartChoice.None, menu.Press(), "아니요 → 돌아감");
            Assert.IsFalse(menu.Confirming);
            Assert.AreEqual(1, menu.Cursor, "'새로 시작'에 그대로");
            menu.Press();
            menu.Move(1);
            Assert.AreEqual(StartChoice.NewGame, menu.Press());
        }

        [Test]
        public void Back_And_ClampedMove()
        {
            var menu = new StartMenu("이어하기");
            menu.Move(-1);
            Assert.AreEqual(0, menu.Cursor);
            menu.Move(5);
            Assert.AreEqual(1, menu.Cursor);
            menu.Press();
            menu.Back();
            Assert.IsFalse(menu.Confirming);
            menu.Back();
            Assert.IsFalse(menu.Confirming, "고르기에서 Esc는 아무 일 없음");
        }

        [Test]
        public void ContinueLine_Summary()
        {
            Assert.AreEqual("이어하기 · 3일차 · 3,450 모아 · " + VillageRating.StarText(2), StartMenu.ContinueLine(3, 3450, 2));
            Assert.AreEqual(StartMenu.ContinueLine(1, 0, 1), StartMenu.ContinueLine(1, 0, 0), "별이 없던 저장도 1개로");
        }
    }
}
