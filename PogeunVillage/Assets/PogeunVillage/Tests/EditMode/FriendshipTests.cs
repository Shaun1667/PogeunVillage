using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PogeunVillage.Tests
{
    // 2-5 친밀도 · 선물 · 대화 고르기 (BALANCE 3번 검산 3-1 ~ 3-4, 5-8)
    public class FriendshipTests
    {
        static Friendship Rules() => new(talkGain: 1, giftGain: 2, dailyCap: 5, min: -20, max: 100);

        static Friendship At(int value)
        {
            // 하루 상한 5씩 여러 날에 걸쳐 올린 뒤, 넘친 만큼 깎아 맞춤
            var f = Rules();
            while (f.Get("A") < value)
            {
                f.Gift("A"); f.Gift("A"); f.Gift("A");
                f.NewDay();
            }
            f.Lose("A", f.Get("A") - value);
            f.NewDay();
            return f;
        }

        [Test]
        public void StartsAtZero()
        {
            Assert.AreEqual(0, Rules().Get("A"));
        }

        [Test]
        public void Case3_1_FromMinus20_TalkAndThreeGifts_IsMinus15()
        {
            var f = At(-20);
            Assert.AreEqual(-20, f.Get("A"));
            f.Talk("A");
            f.Gift("A"); f.Gift("A"); f.Gift("A");
            Assert.AreEqual(-15, f.Get("A"));
        }

        [Test]
        public void Case3_2_ThreeGifts_CappedAtFive()
        {
            var f = Rules();
            Assert.AreEqual(2, f.Gift("A"));
            Assert.AreEqual(2, f.Gift("A"));
            Assert.AreEqual(1, f.Gift("A"), "상한까지 1만");
            Assert.AreEqual(5, f.Get("A"));
            Assert.AreEqual(0, f.Gift("A"), "상한 뒤 선물은 받기만");
            Assert.AreEqual(5, f.Get("A"));
        }

        [Test]
        public void Case3_3_From98_TalkAndTwoGifts_StopsAt100()
        {
            var f = At(98);
            Assert.AreEqual(98, f.Get("A"));
            f.Talk("A");
            f.Gift("A");
            f.Gift("A");
            Assert.AreEqual(100, f.Get("A"));
        }

        [Test]
        public void Case3_4_FromMinus20To100_TakesAtLeast24Days()
        {
            var f = At(-20);
            int days = 0;
            while (f.Get("A") < 100)
            {
                f.Talk("A");
                for (int i = 0; i < 10; i++) f.Gift("A");
                f.NewDay();
                days++;
            }
            Assert.AreEqual(24, days);
        }

        [Test]
        public void Talk_OnlyFirstOfTheDayCounts_ResetsNextDay()
        {
            var f = Rules();
            Assert.AreEqual(1, f.Talk("A"));
            Assert.AreEqual(0, f.Talk("A"));
            Assert.IsTrue(f.HasTalkedToday("A"));
            f.NewDay();
            Assert.IsFalse(f.HasTalkedToday("A"));
            Assert.AreEqual(1, f.Talk("A"));
            Assert.AreEqual(2, f.Get("A"));
        }

        [Test]
        public void TalkAfterCap_StillMarksTalked_NoGain()
        {
            var f = Rules();
            f.Gift("A"); f.Gift("A"); f.Gift("A");
            Assert.AreEqual(0, f.Talk("A"));
            Assert.IsTrue(f.HasTalkedToday("A"));
            Assert.AreEqual(5, f.Get("A"));
        }

        [Test]
        public void VillagersAreSeparate()
        {
            var f = Rules();
            f.Gift("A"); f.Gift("A"); f.Gift("A");
            Assert.AreEqual(2, f.Gift("B"), "다른 주민 상한은 따로");
            Assert.AreEqual(1, f.Talk("B"));
            Assert.AreEqual(3, f.Get("B"));
        }

        [Test]
        public void Case5_8_HolePenalty_SeparateFromDailyCap()
        {
            var f = Rules();
            f.Gift("A"); f.Gift("A"); f.Gift("A");   // 오늘 5 다 씀
            Assert.AreEqual(-2, f.Lose("A", 2));
            Assert.AreEqual(3, f.Get("A"));
            Assert.AreEqual(0, f.Gift("A"), "깎였어도 오늘 올릴 양은 이미 다 씀");
        }

        [Test]
        public void Lose_StopsAtMinus20()
        {
            var f = Rules();
            f.Lose("A", 19);
            Assert.AreEqual(-1, f.Lose("A", 2));
            Assert.AreEqual(-20, f.Get("A"));
            Assert.AreEqual(0, f.Lose("A", 2), "하한");
            Assert.Throws<System.ArgumentOutOfRangeException>(() => f.Lose("A", -1));
        }

        [Test]
        public void Changed_OnlyWhenValueChanges()
        {
            var f = Rules();
            var seen = new List<(string, int)>();
            f.Changed += (id, v) => seen.Add((id, v));
            f.Talk("A");
            f.Talk("A");                 // 두 번째 대화: 변화 없음
            f.Gift("A"); f.Gift("A");    // 3, 5
            f.Gift("A");                 // 상한: 변화 없음
            CollectionAssert.AreEqual(new[] { ("A", 1), ("A", 3), ("A", 5) }, seen);
        }

        // ---------------------------------------------------------------- 대화 고르기 · 이름 줄

        [Test]
        public void Dialogue_ChoicesAppearOnLastLine_AndAreChosen()
        {
            var d = new Dialogue("주민 01", new[] { "안녕", "날씨 좋다" }, new[] { "선물하기", "안녕" });
            Assert.IsFalse(d.IsChoosing, "첫 줄에는 고르기 없음");
            d.MoveChoice(1);
            Assert.AreEqual(0, d.ChoiceCursor, "고르기 전에는 안 움직임");
            Assert.IsTrue(d.Advance());
            Assert.IsTrue(d.IsChoosing);
            d.MoveChoice(1);
            d.MoveChoice(1);
            Assert.AreEqual(1, d.ChoiceCursor, "끝에서 멈춤");
            d.MoveChoice(-5);
            Assert.AreEqual(0, d.ChoiceCursor);
            Assert.IsFalse(d.Advance());
            Assert.IsTrue(d.IsFinished);
            Assert.AreEqual(0, d.Chosen);
        }

        [Test]
        public void Dialogue_WithoutChoices_ChosenStaysMinusOne()
        {
            var d = new Dialogue("주민 01", new[] { "고마워!" });
            Assert.IsFalse(d.IsChoosing);
            Assert.IsFalse(d.Advance());
            Assert.AreEqual(-1, d.Chosen);
        }

        [Test]
        public void SpeakerAndChoiceText()
        {
            Assert.AreEqual("주민 01  친밀도 3", HudText.Speaker("주민 01", 3));
            Assert.AreEqual("주민 01  친밀도 -2", HudText.Speaker("주민 01", -2));
            Assert.AreEqual("<color=#C0607A><b>선물하기</b></color>\n안녕", HudText.Choices(new[] { "선물하기", "안녕" }, 0, "#C0607A"));
            Assert.AreEqual("선물하기\n<color=#C0607A><b>안녕</b></color>", HudText.Choices(new[] { "선물하기", "안녕" }, 1, "#C0607A"));
        }

        [Test]
        public void PickLine_EmptyPool_UsesFallback()
        {
            Assert.AreEqual("고마워!", VillagerTalk.PickLine(new string[0], new SeededRandom(1), "고마워!"));
            Assert.AreEqual("잘 쓸게", VillagerTalk.PickLine(new[] { "잘 쓸게" }, new SeededRandom(1), "고마워!"));
        }

        // ---------------------------------------------------------------- 구멍 칸은 걸을 수 있음(빠지러)

        [Test]
        public void WalkMap_HoleCellIsWalkable_FlowerIsNot()
        {
            var walkable = new bool[9];
            for (int i = 0; i < 9; i++) walkable[i] = true;
            var grid = new GroundGrid(1f, Vector2Int.zero, 3, 3, new bool[9]);
            var walk = new WalkMap(Vector2Int.zero, 3, 3, walkable, new bool[9]);
            grid.TryPlace(new Vector2Int(1, 1), GroundObjectKind.Hole);
            grid.TryPlace(new Vector2Int(2, 2), GroundObjectKind.Flower);
            Assert.IsTrue(walk.IsWalkable(new Vector2Int(1, 1), grid), "구멍");
            Assert.IsFalse(walk.IsWalkable(new Vector2Int(2, 2), grid), "꽃");
        }
    }
}
