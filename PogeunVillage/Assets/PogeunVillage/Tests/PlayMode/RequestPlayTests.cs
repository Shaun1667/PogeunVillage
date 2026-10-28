using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// 3-3 확인 (첫날부터 마을 주민 1명 부탁 · 이름 옆 '부탁 있음' · 부탁 대사 / 모자라면 '부탁한 것 주기' 없음 /
    /// 다 있으면 주기 → 물건 빠짐 · 모아 · 친밀도 +3 · 답례 · 알림 / 다음 아침 새 부탁).
    /// </summary>
    public class RequestPlayTests : ToolSceneTest
    {
        protected override bool WithVillagers => true;

        Village village;
        DialogueWindow dialogue;
        HudView hud;
        Wallet Wallet => player.GetComponent<PlayerWallet>().Wallet;

        VillagerBehaviour Requester => village.Villagers.First(v => v.Definition.id == village.Request.VillagerId);
        ItemDefinition RequestItem => Item(village.Request.ItemId);

        [UnitySetUp]
        public IEnumerator FindVillage()
        {
            village = Object.FindAnyObjectByType<Village>();
            dialogue = Object.FindAnyObjectByType<DialogueWindow>();
            hud = Object.FindAnyObjectByType<HudView>();
            yield break;
        }

        IEnumerator TalkUntilChoices(VillagerBehaviour v)
        {
            Vector3 front = v.transform.forward;
            Teleport(v.transform.position + front * 0.9f, -front);
            yield return Frames(3);
            Assert.AreSame(v.GetComponent<Interactable>(), interactor.Target);
            yield return Tap(keyboard.fKey);
            while (!dialogue.Current.IsChoosing)
                yield return Tap(keyboard.fKey);
        }

        [UnityTest]
        public IEnumerator FirstDay_Request_ShownInTalk_NoGiveChoiceWithoutItems()
        {
            var request = village.Request;
            Assert.IsNotNull(request, "1일차에도 부탁");
            Assert.IsTrue(village.Residents.IsResident(request.VillagerId));
            Assert.That(request.Count, Is.InRange(1, 3));
            CollectionAssert.Contains(village.rules.requestItems.Select(i => i.id).ToArray(), request.ItemId);

            yield return TalkUntilChoices(Requester);
            StringAssert.EndsWith("부탁 있음", dialogue.speakerText.text);
            StringAssert.Contains(RequestItem.displayName, dialogue.lineText.text);
            StringAssert.Contains($"{request.Count}개", dialogue.lineText.text);
            CollectionAssert.DoesNotContain(dialogue.Current.Choices.ToArray(), "부탁한 것 주기", "물건이 없으면 주기 없음");
        }

        [UnityTest]
        public IEnumerator Fulfill_TakesItems_GivesMoaAndFriendship_ThenGone()
        {
            var request = village.Request;
            var who = Requester.Definition;
            Assert.IsTrue(playerInventory.TryAdd(RequestItem, request.Count + 1));
            int moaBefore = Wallet.Moa;
            int reward = RequestItem.sellPrice * request.Count * village.rules.requestMoaMultiplier;

            yield return TalkUntilChoices(Requester);
            Assert.AreEqual("부탁한 것 주기", dialogue.Current.Choices[0]);
            yield return Tap(keyboard.fKey);            // 첫 번째 = 부탁한 것 주기
            Assert.IsTrue(request.Done);
            Assert.AreEqual(1, Inv.Count(request.ItemId), "부탁한 개수만 빠짐");
            Assert.AreEqual(moaBefore + reward, Wallet.Moa);
            Assert.AreEqual(1 + village.rules.requestFriendshipGain, village.Friendship.Get(who.id), "첫 대화 1 + 부탁 3");
            Assert.IsTrue(dialogue.IsOpen);
            CollectionAssert.Contains(who.requestThanksLines, dialogue.lineText.text);
            StringAssert.Contains("부탁을 들어줬어요", hud.noticeText.text);
            yield return Tap(keyboard.fKey);
            Assert.IsFalse(dialogue.IsOpen);

            StringAssert.DoesNotContain("부탁 있음", village.SpeakerOf(who), "들어준 뒤엔 없음");
            var again = village.ComposeTalk(who);
            CollectionAssert.DoesNotContain(again.Choices.ToArray(), "부탁한 것 주기");
        }

        [UnityTest]
        public IEnumerator NextMorning_NewRequest()
        {
            var first = village.Request;
            yield return Tap(keyboard.f8Key);
            yield return null;
            Assert.IsNotNull(village.Request);
            Assert.AreNotSame(first, village.Request, "아침마다 새 부탁, 어제 부탁은 사라짐");
            Assert.IsTrue(village.Residents.IsResident(village.Request.VillagerId));
        }

        [Test]
        public void RequestTexts_AllGlyphsInFont()
        {
            var font = dialogue.lineText.font;
            string all = string.Concat(village.rules.villagers.SelectMany(v => v.requestLines.Concat(v.requestThanksLines)
                    .Concat(v.leavingLines).Concat(v.moveInLines).Concat(v.movingLines).Concat(v.campLines)))
                + "부탁한 것 주기 부탁 있음 이사 준비 중 캠핑 중 이사 오기로 함"
                + string.Concat(village.rules.requestItems.Select(i => i.displayName));
            Assert.IsTrue(font.HasCharacters(all, out uint[] missing, false, true),
                $"글꼴에 없는 글자: {(missing == null ? "" : string.Join(",", missing.Select(u => (char)u)))}");
        }
    }
}
