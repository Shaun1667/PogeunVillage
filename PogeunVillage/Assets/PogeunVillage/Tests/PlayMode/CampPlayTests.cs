using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>2-6 확인 (첫날부터 캠핑 주민 · 마을 주민이 아님 · 아침마다 바뀜(이틀 연속 없음) · 텐트 둘레 · 밤엔 텐트 안 · 대사만 · 꽉 차면 없음).</summary>
    public class CampPlayTests : ToolSceneTest
    {
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

        [Test]
        public void FirstDay_HasCamper_NotAResident_AtTent()
        {
            var camper = village.Camper;
            Assert.IsNotNull(camper, "1일차에도 옴");
            Assert.IsTrue(camper.IsCamper);
            Assert.IsFalse(village.Residents.IsResident(camper.Definition.id));
            Assert.AreSame(village.campSite, camper.Home);
            Assert.AreSame(camper.Definition, village.campSite.Resident);
            Assert.Less(Vector2.Distance(Flat(camper.transform.position), Flat(village.campSite.DoorPosition)), 1f);
            Assert.AreEqual(6, village.Villagers.Count, "캠핑 주민은 마을 주민 수에 안 듦");
        }

        [UnityTest]
        public IEnumerator EveryMorning_NewCamper_NeverTwoDaysInARow()
        {
            var seen = new List<string> { village.Camper.Definition.id };
            for (int day = 0; day < 4; day++)
            {
                yield return Tap(keyboard.f8Key);
                yield return null;
                var camper = village.Camper;
                Assert.IsNotNull(camper);
                Assert.AreNotEqual(seen[^1], camper.Definition.id, "어제 온 주민은 안 옴");
                Assert.IsFalse(village.Residents.IsResident(camper.Definition.id));
                Assert.AreEqual(1, Object.FindObjectsByType<VillagerBehaviour>().Count(v => v.IsCamper), "캠핑 주민은 한 명");
                seen.Add(camper.Definition.id);
            }
            StringAssert.Contains("캠핑 왔어요", hud.noticeText.text, "아침 알림");
        }

        [UnityTest]
        public IEnumerator Camper_StaysAroundTent()
        {
            yield return WaitSeconds(8f);
            var camper = village.Camper;
            Assert.LessOrEqual(Vector2.Distance(Flat(camper.transform.position), Flat(village.campSite.DoorPosition)),
                village.rules.campWanderRadius + 1f);
            Assert.Greater(camper.transform.position.y, settings.waterLevel);
        }

        [UnityTest]
        public IEnumerator Night_CamperGoesIntoTent()
        {
            var dayCycle = Object.FindAnyObjectByType<DayCycle>();
            dayCycle.Clock.Advance((20 - 6) * 60 * dayCycle.rules.realSecondsPerGameMinute);
            float end = Time.time + 15f;
            while (Time.time < end && village.Camper.gameObject.activeSelf)
                yield return null;
            Assert.IsFalse(village.Camper.gameObject.activeSelf, "밤엔 텐트 안");
        }

        [UnityTest]
        public IEnumerator TalkToCamper_CampFriendshipShown()
        {
            var camper = village.Camper;
            Vector3 front = camper.transform.forward;
            Teleport(camper.transform.position + front * 0.9f, -front);
            yield return Frames(3);
            Assert.AreSame(camper.GetComponent<Interactable>(), interactor.Target);

            yield return Tap(keyboard.fKey);
            Assert.IsTrue(dialogue.IsOpen);
            Assert.AreEqual($"{camper.Definition.displayName}  캠핑 중 · 친밀도 1/5", dialogue.speakerText.text, "첫 대화 +1 (3-1)");
            yield return Tap(keyboard.fKey);
            Assert.IsTrue(dialogue.Current.IsChoosing, "선물하기 / 안녕 (3-1)");
            CollectionAssert.Contains(camper.Definition.campLines, dialogue.lineText.text, "캠핑 대사");
            yield return Tap(keyboard.downArrowKey);
            yield return Tap(keyboard.fKey);
            Assert.IsFalse(dialogue.IsOpen);
            Assert.AreEqual(1, village.CampStay.Value);
            Assert.AreEqual(0, village.Friendship.Get(camper.Definition.id), "마을 친밀도와는 따로");
        }

        [UnityTest]
        public IEnumerator FullVillage_NoCamperNextMorning()
        {
            var random = new SystemRandomSource(5);
            foreach (var v in village.rules.villagers)
                if (village.Residents.Count < village.rules.maxResidents && village.Camper.Definition != v)
                    village.Residents.MoveIn(v.id, random);
            Assert.AreEqual(12, village.Residents.Count);

            yield return Tap(keyboard.f8Key);
            yield return null;
            Assert.IsNull(village.Camper, "12명이면 아무도 안 옴");
            Assert.IsNull(village.campSite.Resident);
            Assert.AreEqual(0, Object.FindObjectsByType<VillagerBehaviour>().Count(v => v.IsCamper));
        }
    }
}
