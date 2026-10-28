using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// 3-2 확인 (잠자리채로 5번 → -2 · 화남 알림 · 내구도 / 캠핑 주민은 그대로 / -20 → 아침 이사 준비 · 대사 · 이름 옆 표시 →
    /// 다음 아침 떠남 · 집터 빔 / 대화로 -19 → 취소 / 12명 중 1명 떠나면 그 아침 캠핑 주민 옴).
    /// </summary>
    public class BullyPlayTests : ToolSceneTest
    {
        const int Net = 0;
        protected override bool WithVillagers => true;

        Village village;
        DialogueWindow dialogue;
        HudView hud;

        [UnitySetUp]
        public IEnumerator FindVillage()
        {
            village = Object.FindAnyObjectByType<Village>();
            dialogue = Object.FindAnyObjectByType<DialogueWindow>();
            hud = Object.FindAnyObjectByType<HudView>();
            yield break;
        }

        IEnumerator Face(VillagerBehaviour v)
        {
            Vector3 front = v.transform.forward;
            Teleport(v.transform.position + front * 0.9f, -front);
            yield return Frames(3);
            Assert.AreSame(v.GetComponent<Interactable>(), interactor.Target, "주민이 대상");
        }

        IEnumerator SwingAt(VillagerBehaviour v, int times)
        {
            for (int i = 0; i < times; i++)
            {
                yield return Face(v);
                yield return Swing();
            }
        }

        [UnityTest]
        public IEnumerator FiveNetHits_Minus2_AngryNotice_UsesDurability()
        {
            var v = village.Villagers.First(x => village.Request == null || village.Request.VillagerId != x.Definition.id);
            yield return Hold(Net);
            yield return SwingAt(v, 4);
            Assert.AreEqual(0, village.Friendship.Get(v.Definition.id), "4번은 그대로");
            Assert.AreEqual(4, village.Bully.Count(v.Definition.id, Time.time));
            yield return SwingAt(v, 1);
            Assert.AreEqual(-2, village.Friendship.Get(v.Definition.id));
            StringAssert.Contains("화가 났어요", hud.noticeText.text);
            Assert.AreEqual(45, Durability, "맞을 때마다 내구도 -1");
        }

        [UnityTest]
        public IEnumerator TalkBreaksStreak()
        {
            var v = village.Villagers.First(x => village.Request == null || village.Request.VillagerId != x.Definition.id);
            yield return Hold(Net);
            yield return SwingAt(v, 4);
            yield return Face(v);
            yield return Tap(keyboard.fKey);
            Assert.IsTrue(dialogue.IsOpen);
            Assert.AreEqual(0, village.Bully.Count(v.Definition.id, Time.time), "대화하면 연속 끊김");
            dialogue.Close();
            yield return null;
            yield return SwingAt(v, 1);
            Assert.AreEqual(1, village.Friendship.Get(v.Definition.id), "검산 2-5: +1, 연속 1");
        }

        [UnityTest]
        public IEnumerator Case2_13_Camper_NotBullied()
        {
            var camper = village.Camper;
            yield return Hold(Net);
            yield return SwingAt(camper, 5);
            Assert.AreEqual(0, village.CampStay.Value);
            Assert.AreEqual(0, village.Friendship.Get(camper.Definition.id));
            Assert.AreEqual(45, Durability, "맞기는 함");
            StringAssert.DoesNotContain("화가 났어요", hud.noticeText.text ?? "");
        }

        [UnityTest]
        public IEnumerator Minus20_PrepareThenLeave_PlotEmpties()
        {
            var v = village.Villagers.First(x => village.Request == null || village.Request.VillagerId != x.Definition.id);
            var who = v.Definition;
            var plot = v.Home;
            village.Friendship.Lose(who.id, 20);

            yield return Tap(keyboard.f8Key);                  // 2일차: 이사 준비
            yield return null;
            Assert.IsTrue(village.Departures.IsPreparing(who.id));
            StringAssert.Contains("이사를 준비", hud.noticeText.text);
            StringAssert.EndsWith("이사 준비 중", village.SpeakerOf(who));
            var talk = village.ComposeTalk(who);
            talk.Advance();
            CollectionAssert.Contains(who.leavingLines, talk.Current, "떠날까 하는 말");

            yield return Tap(keyboard.f8Key);                  // 3일차: 떠남
            yield return null;
            Assert.IsFalse(village.Residents.IsResident(who.id));
            Assert.AreEqual(5, village.Residents.Count);
            Assert.IsFalse(plot.house.activeSelf, "집 사라짐");
            Assert.IsTrue(plot.emptySign.activeSelf, "빈 집터");
            Assert.IsFalse(village.Villagers.Any(x => x.Definition == who));
            Assert.IsFalse(Object.FindObjectsByType<VillagerBehaviour>().Any(x => !x.IsCamper && x.Definition == who));
            StringAssert.Contains("떠났어요", hud.noticeText.text);
        }

        [UnityTest]
        public IEnumerator Case2_11_TalkWhilePreparing_Cancels()
        {
            var v = village.Villagers.First(x => village.Request == null || village.Request.VillagerId != x.Definition.id);
            var who = v.Definition;
            village.Friendship.Lose(who.id, 20);
            yield return Tap(keyboard.f8Key);
            yield return null;
            Assert.IsTrue(village.Departures.IsPreparing(who.id));

            yield return Face(v);
            yield return Tap(keyboard.fKey);                   // 첫 대화 +1 → -19
            Assert.AreEqual(-19, village.Friendship.Get(who.id));
            Assert.IsFalse(village.Departures.IsPreparing(who.id), "취소");
            StringAssert.Contains("그만두기로", hud.noticeText.text);
            Assert.IsFalse(dialogue.speakerText.text.Contains("이사 준비 중"));
            dialogue.Close();

            yield return Tap(keyboard.f8Key);
            yield return null;
            Assert.IsTrue(village.Residents.IsResident(who.id), "다음 날에도 마을에 삶");
        }

        [UnityTest]
        public IEnumerator Case2_12_FullVillage_OneLeaves_CamperComesThatMorning()
        {
            var leaver = village.Villagers.First(x => village.Request == null || village.Request.VillagerId != x.Definition.id).Definition;
            var random = new SystemRandomSource(3);
            foreach (var def in village.rules.villagers)
                if (village.Residents.Count < village.rules.maxResidents && village.Camper.Definition != def)
                    village.Residents.MoveIn(def.id, random);
            Assert.AreEqual(12, village.Residents.Count);
            village.Friendship.Lose(leaver.id, 20);

            yield return Tap(keyboard.f8Key);                  // 이사 준비, 12명이라 캠핑 없음
            yield return null;
            Assert.IsNull(village.Camper);
            yield return Tap(keyboard.f8Key);                  // ① 떠남 → ④ 캠핑 교체
            yield return null;
            Assert.AreEqual(11, village.Residents.Count);
            Assert.IsNotNull(village.Camper, "떠난 그 아침에 캠핑 주민 옴");
        }
    }
}
