using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// 3-7b 확인 (게시판 F → '곤충 도감'(세 번째) → 8줄 ??? / 잡으면 기록 · '새 곤충' 알림 · 도감 줄 /
    /// 다음 아침 평가 = (물고기 + 곤충 종류) × 10).
    /// </summary>
    public class InsectEncyclopediaPlayTests : ToolSceneTest
    {
        protected override bool WithInsects => true;

        InsectEncyclopedia encyclopedia;
        FishEncyclopedia fishEncyclopedia;
        Insects insects;
        VillageRater rater;
        DialogueWindow dialogue;
        ListWindow window;
        HudView hud;

        [UnitySetUp]
        public IEnumerator FindParts()
        {
            encyclopedia = Object.FindAnyObjectByType<InsectEncyclopedia>();
            fishEncyclopedia = Object.FindAnyObjectByType<FishEncyclopedia>();
            insects = Object.FindAnyObjectByType<Insects>();
            rater = Object.FindAnyObjectByType<VillageRater>();
            dialogue = Object.FindAnyObjectByType<DialogueWindow>();
            window = Object.FindAnyObjectByType<ListWindow>();
            hud = Object.FindAnyObjectByType<HudView>();
            Assert.IsNotNull(encyclopedia);
            Assert.AreSame(encyclopedia, rater.insectEncyclopedia);
            Assert.AreSame(encyclopedia, insects.encyclopedia);
            yield break;
        }

        InsectDefinition Insect(string id) => insects.rules.insects.First(i => i.id == id);

        IEnumerator OpenFromBoard()
        {
            foreach (var i in insects.All.ToList()) insects.Remove(i);  // 게시판 가는 길에 곤충이 끼어들지 않게
            var board = rater.noticeBoard;
            Vector3 toBoard = board.transform.position - Plaza;
            toBoard.y = 0f;
            Vector3 dir = toBoard.normalized;
            Teleport(board.transform.position - dir * 1.6f, dir);
            yield return Frames(3);
            yield return Tap(keyboard.fKey);
            Assert.AreEqual(3, dialogue.Current.Choices.Count, "마을 평가 / 물고기 도감 / 곤충 도감");
            yield return Tap(keyboard.downArrowKey);
            yield return Tap(keyboard.downArrowKey);
            yield return Tap(keyboard.fKey);                     // 곤충 도감
            Assert.IsTrue(window.IsOpen);
            Assert.AreSame(encyclopedia, window.Source);
        }

        [UnityTest]
        public IEnumerator Board_InsectEncyclopedia_Unknown()
        {
            yield return OpenFromBoard();
            Assert.AreEqual("곤충 도감", window.titleText.text);
            Assert.AreEqual(8, window.listText.text.Split('\n').Count(l => l.Contains("???")));
            Assert.AreEqual("잡은 종류 0 / 8", window.footerText.text);
            yield return Tap(keyboard.tabKey);
        }

        [UnityTest]
        public IEnumerator CatchRecords_Notice_Line_RatingWithFish()
        {
            // 잠자리채로 실제로 잡기(3-7a 흐름)
            Assert.GreaterOrEqual(Inv.TryAddWithDurability("Net", tools.tools[0].durability), 0);
            yield return Tap(keyboard.digit1Key);
            foreach (var i in insects.All.ToList()) insects.Remove(i);
            yield return StandFacingNorth(FindOpenSpot());
            insects.SpawnAt(Insect("Grasshopper"), player.position + Vector3.forward * 1.1f + Vector3.up * 0.3f);
            yield return Frames(2);
            yield return Tap(keyboard.spaceKey);
            yield return WaitSeconds(settings.toolSwingTime + 0.1f);
            Assert.AreEqual(1, encyclopedia.Log.CaughtCount("Grasshopper"));
            StringAssert.Contains("새 곤충: 메뚜기! (1 / 8)", hud.noticeText.text + string.Join("|", hud.PendingNotices));

            fishEncyclopedia.Record(fishEncyclopedia.rules.fish[0]);
            yield return OpenFromBoard();
            StringAssert.Contains("메뚜기 · 풀밭 · 낮/저녁 · 1마리", window.listText.text);
            yield return Tap(keyboard.tabKey);

            yield return Tap(keyboard.f8Key);
            yield return null;
            Assert.AreEqual(2 * rater.rules.speciesPoint, rater.Current.encyclopediaPoints, "물고기 1 + 곤충 1 종류");
        }
    }
}
