using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// 3-6b 확인 (게시판 F → '물고기 도감' → 목록 창 8줄 · 못 잡은 건 ??? / 처음 낚으면 '새 물고기' 알림 · 도감 줄 · 잡은 종류 /
    /// 잡은 종류는 다음 아침 평가 +10).
    /// </summary>
    public class FishEncyclopediaPlayTests : ToolSceneTest
    {
        FishEncyclopedia encyclopedia;
        VillageRater rater;
        DialogueWindow dialogue;
        ListWindow window;
        HudView hud;

        [UnitySetUp]
        public IEnumerator FindEncyclopedia()
        {
            encyclopedia = Object.FindAnyObjectByType<FishEncyclopedia>();
            rater = Object.FindAnyObjectByType<VillageRater>();
            dialogue = Object.FindAnyObjectByType<DialogueWindow>();
            window = Object.FindAnyObjectByType<ListWindow>();
            hud = Object.FindAnyObjectByType<HudView>();
            Assert.IsNotNull(encyclopedia);
            Assert.AreSame(encyclopedia, rater.fishEncyclopedia);
            Assert.AreSame(encyclopedia, player.GetComponent<FishingRodUser>().encyclopedia);
            yield break;
        }

        FishDefinition Fish(string id) => encyclopedia.rules.fish.First(f => f.id == id);

        IEnumerator OpenFromBoard()
        {
            var board = rater.noticeBoard;
            Vector3 toBoard = board.transform.position - Plaza;
            toBoard.y = 0f;
            Vector3 dir = toBoard.normalized;
            Teleport(board.transform.position - dir * 1.6f, dir);
            yield return Frames(3);
            Assert.AreSame(board, interactor.Target);
            yield return Tap(keyboard.fKey);
            Assert.IsTrue(dialogue.Current.IsChoosing);
            yield return Tap(keyboard.downArrowKey);
            yield return Tap(keyboard.fKey);                     // 물고기 도감
            Assert.IsFalse(dialogue.IsOpen);
            Assert.IsTrue(window.IsOpen, "도감 목록 창");
            Assert.AreSame(encyclopedia, window.Source);
        }

        [UnityTest]
        public IEnumerator Board_OpensEncyclopedia_UnknownIsQuestionMarks()
        {
            yield return OpenFromBoard();
            Assert.AreEqual("물고기 도감", window.titleText.text);
            var lines = window.listText.text.Split('\n');
            Assert.AreEqual(8, lines.Length);
            Assert.IsTrue(lines.All(l => l.Contains("???")), "아직 못 잡음");
            Assert.AreEqual("잡은 종류 0 / 8", window.footerText.text);
            yield return Tap(keyboard.fKey);
            Assert.IsTrue(window.IsOpen, "F는 아무 일 없음(보기만)");
            yield return Tap(keyboard.tabKey);
            Assert.IsFalse(window.IsOpen);
        }

        [UnityTest]
        public IEnumerator FirstCatch_Notice_ShownInEncyclopedia_RatingNextMorning()
        {
            int encyclopediaPointsBefore = rater.Current.encyclopediaPoints;
            encyclopedia.Record(Fish("CrucianCarp"));
            StringAssert.Contains("새 물고기: 붕어! (1 / 8)", hud.noticeText.text);
            yield return WaitSeconds(0.1f);
            encyclopedia.Record(Fish("CrucianCarp"));
            Assert.AreEqual(1, encyclopedia.Log.Species);
            Assert.AreEqual(2, encyclopedia.Log.CaughtCount("CrucianCarp"));

            yield return OpenFromBoard();
            StringAssert.Contains("붕어 · 중 · 언제나 · 2마리", window.listText.text);
            Assert.AreEqual("잡은 종류 1 / 8", window.footerText.text);
            yield return Tap(keyboard.tabKey);

            Assert.AreEqual(encyclopediaPointsBefore, rater.Current.encyclopediaPoints, "평가는 아침 기준");
            yield return Tap(keyboard.f8Key);
            yield return null;
            Assert.AreEqual(rater.rules.speciesPoint, rater.Current.encyclopediaPoints, "다음 아침 +10");
        }
    }
}
