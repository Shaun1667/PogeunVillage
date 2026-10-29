using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// 3-1 확인 (캠핑 주민에게 대화 + 선물 2개 → 이주 확정 대사 · 알림 · 이후 선물하기 없음 → 다음 날 아침 빈 집터에 입주 · 집이 생김 ·
    /// 친밀도 5 이어받음 · 미달이면 입주 없음 · 11명 + 확정 → 다음 날 캠핑 없음).
    /// </summary>
    public class CampMoveInPlayTests : ToolSceneTest
    {
        protected override bool WithVillagers => true;

        Village village;
        DialogueWindow dialogue;
        InventoryWindow window;
        HudView hud;

        VillagerBehaviour Camper => village.Camper;

        [UnitySetUp]
        public IEnumerator FindVillage()
        {
            village = Object.FindAnyObjectByType<Village>();
            dialogue = Object.FindAnyObjectByType<DialogueWindow>();
            window = Object.FindAnyObjectByType<InventoryWindow>();
            hud = Object.FindAnyObjectByType<HudView>();
            yield break;
        }

        IEnumerator FaceCamper()
        {
            Vector3 front = Camper.transform.forward;
            Teleport(Camper.transform.position + front * 0.9f, -front);
            yield return Frames(3);
            Assert.AreSame(Camper.GetComponent<Interactable>(), interactor.Target, "캠핑 주민이 대상");
        }

        /// <summary>말 걸기 → 선물하기 → 0번 칸 → 답례(또는 확정 대사) 닫기</summary>
        IEnumerator GiveSlotZero(bool checkLastLine = false, string[] expected = null)
        {
            yield return FaceCamper();
            yield return Tap(keyboard.fKey);
            while (!dialogue.Current.IsChoosing)
                yield return Tap(keyboard.fKey);
            yield return Tap(keyboard.fKey);          // 선물하기
            Assert.IsTrue(window.IsPicking);
            yield return Tap(keyboard.fKey);          // 0번 칸
            Assert.IsTrue(dialogue.IsOpen);
            if (checkLastLine)
                CollectionAssert.Contains(expected, dialogue.lineText.text);
            yield return Tap(keyboard.fKey);
            Assert.IsFalse(dialogue.IsOpen);
        }

        IEnumerator Confirm()
        {
            Assert.IsTrue(playerInventory.TryAdd(Item("Apple"), 5));
            yield return GiveSlotZero();                                      // 1 + 2 = 3
            Assert.AreEqual(3, village.CampStay.Value);
            Assert.IsFalse(village.CampStay.Confirmed);
            yield return GiveSlotZero(true, Camper.Definition.moveInLines);    // 5 → 확정 대사
            Assert.IsTrue(village.CampStay.Confirmed);
        }

        [UnityTest]
        public IEnumerator TalkAndTwoGifts_Confirmed_ThenOnlyMovingLines()
        {
            string id = Camper.Definition.id;
            yield return Confirm();
            StringAssert.Contains("내일 이사 와요", hud.noticeText.text);

            yield return FaceCamper();
            yield return Tap(keyboard.fKey);
            Assert.AreEqual($"{Camper.Definition.displayName}  캠핑 중 · 이사 오기로 함", dialogue.speakerText.text);
            CollectionAssert.Contains(Camper.Definition.movingLines, dialogue.lineText.text);
            Assert.IsFalse(dialogue.Current.IsChoosing, "확정 뒤엔 선물하기 없음");
            yield return Tap(keyboard.fKey);
            Assert.IsFalse(dialogue.IsOpen);
            Assert.IsFalse(window.IsOpen);
            Assert.AreEqual(5, village.CampStay.Value, "확정 뒤 변화 없음");
            Assert.IsFalse(village.Residents.IsResident(id), "입주는 다음 날 아침");
        }

        [UnityTest]
        public IEnumerator Confirmed_MovesInNextMorning_HouseAppears_KeepsFriendship()
        {
            var who = Camper.Definition;
            yield return Confirm();
            int before = village.Residents.Count;

            yield return Tap(keyboard.f8Key);
            yield return null;
            Assert.AreEqual(before + 1, village.Residents.Count);
            Assert.IsTrue(village.Residents.IsResident(who.id));
            var plot = village.plots[village.Residents.PlotOf(who.id)];
            Assert.AreSame(who, plot.Resident);
            Assert.IsTrue(plot.house.activeSelf, "집이 생김");
            Assert.IsFalse(plot.emptySign.activeSelf);
            var villager = village.Villagers.Single(v => v.Definition == who);
            Assert.IsFalse(villager.IsCamper);
            Assert.IsTrue(villager.gameObject.activeSelf, "아침에 새 집에서 나옴");
            Assert.Less(Vector2.Distance(Flat(villager.transform.position), Flat(plot.DoorPosition)), 0.6f);
            Assert.AreEqual(5, village.Friendship.Get(who.id), "캠핑 때 친밀도 이어받음");
            Assert.AreNotSame(who, Camper ? Camper.Definition : null, "새 캠핑 주민은 다른 주민");
            StringAssert.Contains("이사 왔어요", hud.noticeText.text, "입주 알림 먼저");
            Assert.IsTrue(hud.PendingNotices.Any(n => n.Contains("캠핑 왔어요")), "다음 차례에 캠핑 교체 알림(아침 순서 ② → ④)");
        }

        [UnityTest]
        public IEnumerator NotConfirmed_NoMoveIn_NextMorning()
        {
            var who = Camper.Definition;
            Assert.IsTrue(playerInventory.TryAdd(Item("Apple"), 1));
            yield return GiveSlotZero();             // 3, 미달
            int before = village.Residents.Count;

            yield return Tap(keyboard.f8Key);
            yield return null;
            Assert.AreEqual(before, village.Residents.Count);
            Assert.IsFalse(village.Residents.IsResident(who.id));
            Assert.AreEqual(0, village.CampStay.Value, "새 캠핑 주민은 0부터");
        }

        [UnityTest]
        public IEnumerator Case1_7_ElevenPlusConfirmed_NoCamperNextMorning()
        {
            var random = new SeededRandom(9);
            foreach (var v in village.rules.villagers)
                if (village.Residents.Count < village.rules.maxResidents - 1 && Camper.Definition != v)
                    village.Residents.MoveIn(v.id, random);
            Assert.AreEqual(11, village.Residents.Count);
            yield return Confirm();

            yield return Tap(keyboard.f8Key);
            yield return null;
            Assert.AreEqual(12, village.Residents.Count, "확정 주민 입주로 12명");
            Assert.IsNull(Camper, "꽉 차서 캠핑 주민 없음");
        }
    }
}
