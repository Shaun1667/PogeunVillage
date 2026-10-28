using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>3-5 확인 (시작 평가 · HUD 별 · 게시판 F 항목별 · 아침에 다시 계산 · 첫 목표 축하 알림 한 번 · 키운 나무 점수).</summary>
    public class RatingPlayTests : ToolSceneTest
    {
        protected override bool WithVillagers => true;

        VillageRater rater;
        Village village;
        DialogueWindow dialogue;
        HudView hud;

        [UnitySetUp]
        public IEnumerator FindRater()
        {
            rater = Object.FindAnyObjectByType<VillageRater>();
            village = Object.FindAnyObjectByType<Village>();
            dialogue = Object.FindAnyObjectByType<DialogueWindow>();
            hud = Object.FindAnyObjectByType<HudView>();
            Assert.IsNotNull(rater);
            yield break;
        }

        [Test]
        public void Start_RatedFromVillage_ShownOnHud()
        {
            var r = rater.Current;
            int flowers = ground.Objects().Count(o => o.kind == GroundObjectKind.Flower);
            Assert.AreEqual(flowers * rater.rules.flowerPoint, r.flowerPoints, "첫날은 시든 꽃 없음");
            Assert.AreEqual(6 * rater.rules.residentPoint, r.residentPoints);
            Assert.AreEqual(0, r.friendshipPoints);
            Assert.AreEqual(0, r.treePoints);
            Assert.That(r.stars, Is.InRange(1, 5));
            Assert.AreEqual($"마을 {VillageRating.StarText(r.stars)}", rater.ratingText.text);
        }

        [UnityTest]
        public IEnumerator NoticeBoard_F_ShowsBreakdown()
        {
            var board = rater.noticeBoard;
            Vector3 toBoard = board.transform.position - Plaza;
            toBoard.y = 0f;
            Vector3 dir = toBoard.normalized;
            Teleport(board.transform.position - dir * 1.6f, dir);
            yield return Frames(3);
            Assert.AreSame(board, interactor.Target, "게시판이 대상");

            yield return Tap(keyboard.fKey);
            Assert.IsTrue(dialogue.IsOpen);
            Assert.AreEqual("게시판", dialogue.speakerText.text);
            StringAssert.Contains("마을 평가", dialogue.lineText.text);
            StringAssert.Contains($"{rater.Current.total}점", dialogue.lineText.text);
            yield return Tap(keyboard.fKey);
            StringAssert.StartsWith("꽃 ", dialogue.lineText.text);
            yield return Tap(keyboard.fKey);
            StringAssert.Contains("첫 목표 ★3", dialogue.lineText.text);
            yield return Tap(keyboard.fKey);
            Assert.IsFalse(dialogue.IsOpen);
        }

        [UnityTest]
        public IEnumerator Morning_Reevaluates_FirstGoalCelebratedOnce()
        {
            Assert.IsFalse(rater.GoalReached);
            foreach (string id in village.Residents.Plots.Where(id => id != null))
                village.Friendship.Begin(id, 100);          // 6명 × 100 = +600

            yield return Tap(keyboard.f8Key);
            yield return null;
            Assert.AreEqual(5, rater.Current.stars);
            Assert.IsTrue(rater.GoalReached);
            StringAssert.Contains("첫 목표 달성", hud.noticeText.text + string.Join("|", hud.PendingNotices));
            Assert.AreEqual("마을 ★★★★★", rater.ratingText.text);

            foreach (string id in village.Residents.Plots.Where(id => id != null))
                village.Friendship.Lose(id, 60);            // 별이 내려가도
            yield return WaitSeconds(settings.hudNoticeSeconds * 4);
            yield return Tap(keyboard.f8Key);
            yield return null;
            Assert.Less(rater.Current.stars, 5);
            string shown = hud.noticeText.text + string.Join("|", hud.PendingNotices);
            StringAssert.DoesNotContain("첫 목표 달성", shown, "축하는 처음 한 번만");
            StringAssert.Contains("마을 평가가", shown, "별이 바뀌면 알림");
        }

        [Test]
        public void GrownTree_Counts()
        {
            var keeper = ground.GetComponent<GardenKeeper>();
            Object.Instantiate(keeper.grownTreePrefab, new Vector3(0f, -50f, 0f), Quaternion.identity, keeper.transform);
            rater.Evaluate();
            Assert.AreEqual(rater.rules.grownTreePoint, rater.Current.treePoints);
        }
    }
}
