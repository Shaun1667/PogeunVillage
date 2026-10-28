using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// 2-5 확인 (첫 대화 +1 · 이름 옆 친밀도 · 대화 끝 고르기 · 선물하기 → 인벤토리에서 골라 주기 +2 · 도구는 못 줌 · 취소 ·
    /// 하루 상한 5 · 다음 날 다시 오름 · 주민이 구멍에 빠짐 → 3초 뒤 나오고 구멍 사라짐 · -2).
    /// </summary>
    public class FriendshipPlayTests : ToolSceneTest
    {
        protected override bool WithVillagers => true;

        Village village;
        DialogueWindow dialogue;
        InventoryWindow window;
        HudView hud;
        VillagerBehaviour villager;

        string Id => villager.Definition.id;
        int FriendshipNow => village.Friendship.Get(Id);

        [UnitySetUp]
        public IEnumerator FindVillage()
        {
            village = Object.FindAnyObjectByType<Village>();
            dialogue = Object.FindAnyObjectByType<DialogueWindow>();
            window = Object.FindAnyObjectByType<InventoryWindow>();
            hud = Object.FindAnyObjectByType<HudView>();
            villager = village.Villagers[0];
            yield break;
        }

        IEnumerator FaceVillager()
        {
            Vector3 front = villager.transform.forward;
            Teleport(villager.transform.position + front * 0.9f, -front);
            yield return Frames(3);
            Assert.AreSame(villager.GetComponent<Interactable>(), interactor.Target, "주민이 대상");
        }

        /// <summary>말 걸고 대사를 넘겨 고르기까지</summary>
        IEnumerator TalkUntilChoices()
        {
            yield return Tap(keyboard.fKey);
            Assert.IsTrue(dialogue.IsOpen);
            while (!dialogue.Current.IsChoosing)
                yield return Tap(keyboard.fKey);
        }

        /// <summary>말 걸기 → 선물하기 → 0번 칸 → 고맙다는 말 닫기</summary>
        IEnumerator GiveSlotZero()
        {
            yield return TalkUntilChoices();
            yield return Tap(keyboard.fKey);          // 선물하기(첫 번째)
            Assert.IsTrue(window.IsPicking, "선물 고르기 창");
            yield return Tap(keyboard.fKey);          // 0번 칸
            Assert.IsFalse(window.IsOpen);
            Assert.IsTrue(dialogue.IsOpen, "고맙다는 말");
            yield return Tap(keyboard.fKey);
            Assert.IsFalse(dialogue.IsOpen);
        }

        [UnityTest]
        public IEnumerator FirstTalk_PlusOne_ShownNextToName_OnlyOncePerDay()
        {
            yield return FaceVillager();
            yield return Tap(keyboard.fKey);
            Assert.AreEqual(1, FriendshipNow);
            Assert.AreEqual($"{villager.Definition.displayName}  친밀도 1", dialogue.speakerText.text);
            yield return Tap(keyboard.fKey);
            Assert.IsTrue(dialogue.Current.IsChoosing, "마지막 줄에 고르기");
            StringAssert.Contains("선물하기", dialogue.choiceText.text);
            StringAssert.Contains("안녕", dialogue.choiceText.text);
            yield return Tap(keyboard.sKey);          // 안녕
            yield return Tap(keyboard.fKey);
            Assert.IsFalse(dialogue.IsOpen);
            Assert.IsFalse(window.IsOpen, "안녕이면 선물 창 없음");

            yield return FaceVillager();
            yield return Tap(keyboard.fKey);
            Assert.AreEqual(1, FriendshipNow, "두 번째 대화는 그대로");
        }

        [UnityTest]
        public IEnumerator GiftApple_PlusTwo_TakesOne_SaysThanks()
        {
            Assert.IsTrue(playerInventory.TryAdd(Item("Apple"), 3));
            yield return FaceVillager();
            yield return TalkUntilChoices();
            yield return Tap(keyboard.fKey);
            Assert.IsTrue(window.IsPicking);
            Assert.AreEqual("선물할 물건 고르기", window.titleText.text);
            Assert.AreEqual(VillagerBehaviour.State.Talking, villager.Current, "고르는 동안 주민은 기다림");

            yield return Tap(keyboard.fKey);
            Assert.AreEqual(2, Inv.Count("Apple"), "하나 줌");
            Assert.AreEqual(3, FriendshipNow, "첫 대화 1 + 선물 2");
            Assert.IsTrue(dialogue.IsOpen);
            CollectionAssert.Contains(villager.Definition.thanksLines, dialogue.lineText.text);
            Assert.AreEqual($"{villager.Definition.displayName}  친밀도 3", dialogue.speakerText.text);
            StringAssert.Contains("사과", hud.noticeText.text, "화면 알림");

            yield return Tap(keyboard.fKey);
            Assert.IsFalse(dialogue.IsOpen);
            Assert.AreEqual(VillagerBehaviour.State.Pausing, villager.Current);
            Assert.AreEqual("인벤토리", window.titleText.text, "제목 되돌림");
            yield return null;
            Assert.IsTrue(player.GetComponent<PlayerController>().enabled, "다시 움직일 수 있음");
        }

        [UnityTest]
        public IEnumerator Tool_CannotBeGiven_PickAnother()
        {
            Assert.IsTrue(playerInventory.TryAddTool(tools.tools[Axe]));
            Assert.IsTrue(playerInventory.TryAdd(Item("Peach"), 1));
            yield return FaceVillager();
            yield return TalkUntilChoices();
            yield return Tap(keyboard.fKey);
            yield return Tap(keyboard.fKey);          // 0번 = 도끼
            Assert.IsTrue(window.IsPicking, "도구는 못 줘서 창 그대로");
            StringAssert.Contains("줄 수 없어요", hud.noticeText.text);
            Assert.AreEqual("Axe", Inv.Slots[0].itemId);

            yield return Tap(keyboard.rightArrowKey);
            yield return Tap(keyboard.fKey);          // 1번 = 복숭아
            Assert.AreEqual(0, Inv.Count("Peach"));
            Assert.AreEqual(3, FriendshipNow);
        }

        [UnityTest]
        public IEnumerator EmptySlot_CannotBeGiven()
        {
            yield return FaceVillager();
            yield return TalkUntilChoices();
            yield return Tap(keyboard.fKey);
            yield return Tap(keyboard.fKey);          // 빈 칸
            Assert.IsTrue(window.IsPicking);
            Assert.AreEqual(1, FriendshipNow);
        }

        [UnityTest]
        public IEnumerator CancelWithTab_NoGift_VillagerResumes()
        {
            Assert.IsTrue(playerInventory.TryAdd(Item("Apple"), 1));
            yield return FaceVillager();
            yield return TalkUntilChoices();
            yield return Tap(keyboard.fKey);
            Assert.IsTrue(window.IsPicking);
            yield return Tap(keyboard.tabKey);
            Assert.IsFalse(window.IsOpen);
            Assert.IsFalse(dialogue.IsOpen);
            Assert.AreEqual(1, Inv.Count("Apple"));
            Assert.AreEqual(1, FriendshipNow);
            Assert.AreEqual(VillagerBehaviour.State.Pausing, villager.Current);
            yield return null;
            Assert.IsFalse(window.IsOpen, "취소한 Tab으로 다시 열리지 않음");
        }

        [UnityTest]
        public IEnumerator DailyCapFive_ThenNextDayRisesAgain()
        {
            Assert.IsTrue(playerInventory.TryAdd(Item("Apple"), 5));
            yield return FaceVillager();
            yield return GiveSlotZero();             // 1 + 2 = 3
            yield return FaceVillager();
            yield return GiveSlotZero();             // 5
            yield return FaceVillager();
            yield return GiveSlotZero();             // 상한: 받기만
            Assert.AreEqual(5, FriendshipNow);
            Assert.AreEqual(2, Inv.Count("Apple"), "상한 뒤에도 받음");

            yield return Tap(keyboard.f8Key);
            yield return null;
            yield return FaceVillager();
            yield return Tap(keyboard.fKey);
            Assert.AreEqual(6, FriendshipNow, "다음 날 첫 대화 +1");
        }

        [UnityTest]
        public IEnumerator VillagerFallsIntoHole_ClimbsOut_HoleGone_MinusTwo()
        {
            // 주민 가까이의 놓을 수 있고 걸을 수 있는 칸에 구멍을 내고, 주민을 그 칸에 올려 둠
            Vector2Int at = ground.CellAt(villager.transform.position);
            Vector2Int cell = Enumerable.Range(-4, 9).SelectMany(dx => Enumerable.Range(-4, 9).Select(dy => at + new Vector2Int(dx, dy)))
                .Where(c => Grid.CanPlace(c) && village.WalkMap.IsWalkable(c, Grid))
                .OrderBy(c => (c - at).sqrMagnitude).First();
            Assert.IsNotNull(ground.Placer.Spawn(GroundObjectKind.Hole, cell));
            Vector2 center = Grid.CellCenter(cell);
            var cc = villager.GetComponent<CharacterController>();
            cc.enabled = false;
            villager.transform.position = new Vector3(center.x, GroundHeight(new Vector3(center.x, 0f, center.y)) + 0.05f, center.y);
            cc.enabled = true;

            float end = Time.time + 6f;
            while (Time.time < end && villager.Current != VillagerBehaviour.State.InHole)
                yield return null;
            Assert.AreEqual(VillagerBehaviour.State.InHole, villager.Current, "걷기 시작하면 빠짐");
            StringAssert.Contains("구멍에 빠졌어요", hud.noticeText.text);
            Assert.Less(villager.visual.localPosition.y, 0f, "몸이 내려감");
            Assert.AreEqual(GroundObjectKind.Hole, Grid.Get(cell), "빠져 있는 동안 구멍 그대로");
            Assert.AreEqual(0, FriendshipNow);

            yield return WaitSeconds(village.rules.holeStuckSeconds + 0.3f);
            Assert.AreNotEqual(VillagerBehaviour.State.InHole, villager.Current, "3초 뒤 나옴");
            Assert.AreEqual(GroundObjectKind.None, Grid.Get(cell), "구멍 사라짐");
            Assert.IsNull(ground.ObjectAt(cell));
            Assert.AreEqual(-2, FriendshipNow);
        }

        [UnityTest]
        public IEnumerator InHole_CannotTalk()
        {
            Vector2Int at = ground.CellAt(villager.transform.position);
            Vector2Int cell = Enumerable.Range(-4, 9).SelectMany(dx => Enumerable.Range(-4, 9).Select(dy => at + new Vector2Int(dx, dy)))
                .Where(c => Grid.CanPlace(c) && village.WalkMap.IsWalkable(c, Grid))
                .OrderBy(c => (c - at).sqrMagnitude).First();
            ground.Placer.Spawn(GroundObjectKind.Hole, cell);
            Vector2 center = Grid.CellCenter(cell);
            var cc = villager.GetComponent<CharacterController>();
            cc.enabled = false;
            villager.transform.position = new Vector3(center.x, GroundHeight(new Vector3(center.x, 0f, center.y)) + 0.05f, center.y);
            cc.enabled = true;
            float end = Time.time + 6f;
            while (Time.time < end && villager.Current != VillagerBehaviour.State.InHole)
                yield return null;

            Vector3 front = villager.transform.forward;
            Teleport(villager.transform.position + front * 0.9f, -front);
            yield return Frames(3);
            yield return Tap(keyboard.fKey);
            Assert.IsFalse(dialogue.IsOpen, "구멍 안 주민과는 대화 안 됨");
        }
    }
}
