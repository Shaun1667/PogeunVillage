using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>2-4 주민 확인 (시작 6명 · 집터 12곳 · 같은 시드 = 같은 마을 · 배회 · 밤에 집으로 · 아침에 다시 나옴 · F로 대화 · 대화 중 멈춤).</summary>
    public class VillagerPlayTests : ToolSceneTest
    {
        protected override bool WithVillagers => true;

        Village village;
        DialogueWindow dialogue;
        DayCycle dayCycle;

        [UnitySetUp]
        public IEnumerator FindVillage()
        {
            village = Object.FindAnyObjectByType<Village>();
            dialogue = Object.FindAnyObjectByType<DialogueWindow>();
            dayCycle = Object.FindAnyObjectByType<DayCycle>();
            Assert.IsNotNull(village);
            Assert.IsNotNull(dialogue);
            yield break;
        }

        [Test]
        public void Start_SixVillagers_TwelvePlots_HousesOnlyWhereSomeoneLives()
        {
            Assert.AreEqual(12, village.plots.Length);
            Assert.AreEqual(6, village.Villagers.Count);
            Assert.AreEqual(6, village.Residents.Count);
            foreach (var plot in village.plots)
            {
                bool lived = plot.Resident;
                Assert.AreEqual(lived, plot.house.activeSelf, plot.name);
                Assert.AreEqual(!lived, plot.emptySign.activeSelf, plot.name);
            }
            foreach (var v in village.Villagers)
                Assert.AreSame(v.Definition, v.Home.Resident, "자기 집터에 삶");
            Assert.AreEqual(6, village.Villagers.Select(v => v.Definition.id).Distinct().Count());
        }

        [Test]
        public void SameSeed_SameVillage()
        {
            var ids = village.rules.villagers.Select(v => v.id).ToList();
            var expected = VillageResidents.CreateStarting(12, ids, village.rules.startingResidents, new SeededRandom(village.rules.residentSeed));
            CollectionAssert.AreEqual(expected.Plots, village.Residents.Plots);
        }

        [Test]
        public void HouseWall_HasResidentColor()
        {
            var plot = village.plots.First(p => p.Resident);
            var block = new MaterialPropertyBlock();
            plot.wallRenderers[0].GetPropertyBlock(block);
            Color c = block.GetColor("_BaseColor");
            Assert.AreEqual(plot.Resident.bodyColor.r, c.r, 0.01f);
            Assert.AreEqual(plot.Resident.bodyColor.g, c.g, 0.01f);
            Assert.AreEqual(plot.Resident.bodyColor.b, c.b, 0.01f);
        }

        [UnityTest]
        public IEnumerator Villagers_WanderAwayFromDoor_StayOnWalkableGround()
        {
            yield return WaitSeconds(6f);
            int moved = village.Villagers.Count(v => Vector2.Distance(Flat(v.transform.position), Flat(v.Home.DoorPosition)) > 1.5f);
            Assert.Greater(moved, 0, "누군가는 현관을 떠나 돌아다님");
            foreach (var v in village.Villagers)
            {
                Assert.IsTrue(v.gameObject.activeSelf, "낮에는 밖");
                Assert.Greater(v.transform.position.y, settings.waterLevel, $"{v.name} 물에 빠지지 않음");
                Assert.LessOrEqual(Vector2.Distance(Flat(v.transform.position), Flat(v.Home.DoorPosition)), village.rules.wanderRadius + 1.5f, $"{v.name} 집 근처");
            }
        }

        [UnityTest]
        public IEnumerator Night_GoHome_ThenMorningComeOutAgain()
        {
            dayCycle.Clock.Advance((20 - 6) * 60 * dayCycle.rules.realSecondsPerGameMinute);  // 20시 = 밤
            Assert.AreEqual(DayPeriod.Night, dayCycle.Clock.Period);
            Assert.IsTrue(village.Villagers.All(v => v.Current is VillagerBehaviour.State.GoingHome or VillagerBehaviour.State.Home));

            float end = Time.time + 25f;
            while (Time.time < end && (village.Villagers.Any(v => v.gameObject.activeSelf) || (village.Camper && village.Camper.gameObject.activeSelf)))
                yield return null;
            Assert.IsTrue(village.Villagers.All(v => !v.gameObject.activeSelf), "밤에는 모두 집 안");
            Assert.IsTrue(Interactable.All.All(i => i.kind != InteractableKind.Villager), "집 안 주민에게는 말을 걸 수 없음");

            yield return Tap(keyboard.f8Key);
            Assert.AreEqual(2, dayCycle.Day);
            foreach (var v in village.Villagers)
            {
                Assert.IsTrue(v.gameObject.activeSelf, "아침에 다시 나옴");
                Assert.Less(Vector2.Distance(Flat(v.transform.position), Flat(v.Home.DoorPosition)), 0.6f, "현관에서 시작");
            }
        }

        [UnityTest]
        public IEnumerator SleepInDaytime_VillagersRestartAtDoor()
        {
            yield return WaitSeconds(4f);
            yield return Tap(keyboard.f8Key);
            yield return null;
            foreach (var v in village.Villagers)
                Assert.Less(Vector2.Distance(Flat(v.transform.position), Flat(v.Home.DoorPosition)), 0.6f);
        }

        VillagerBehaviour StandInFrontOfVillager()
        {
            var v = village.Villagers.First(x => village.Request == null || village.Request.VillagerId != x.Definition.id);
            Vector3 front = v.transform.forward;
            Teleport(v.transform.position + front * 0.9f, -front);
            return v;
        }

        [UnityTest]
        public IEnumerator TalkWithF_ShowsName_AdvancesLines_PausesBoth()
        {
            var v = StandInFrontOfVillager();
            yield return Frames(3);
            Assert.AreSame(v.GetComponent<Interactable>(), interactor.Target, "주민이 대상");

            yield return Tap(keyboard.fKey);
            Assert.IsTrue(dialogue.IsOpen);
            Assert.AreEqual(VillagerBehaviour.State.Talking, v.Current);
            StringAssert.StartsWith(v.Definition.displayName, dialogue.speakerText.text, "이름(뒤에 친밀도, 2-5)");
            CollectionAssert.Contains(v.Definition.morningLines, dialogue.lineText.text, "6시 = 아침 인사");
            Assert.AreEqual(2, dialogue.Current.LineCount);

            Vector3 villagerAt = v.transform.position;
            Vector2 before = Flat(player.position);
            Press(keyboard.wKey);
            yield return WaitSeconds(0.4f);
            Release(keyboard.wKey);
            Assert.Less(Vector2.Distance(before, Flat(player.position)), 0.01f, "대화 중 플레이어 멈춤");
            Assert.Less(Vector2.Distance(Flat(villagerAt), Flat(v.transform.position)), 0.01f, "대화 중 주민 멈춤");

            yield return Tap(keyboard.fKey);
            Assert.IsTrue(dialogue.IsOpen);
            CollectionAssert.Contains(v.Definition.smallTalk, dialogue.lineText.text, "둘째 줄 = 잡담");
            yield return Tap(keyboard.downArrowKey);  // 고르기: 안녕(2-5)
            yield return Tap(keyboard.fKey);
            Assert.IsFalse(dialogue.IsOpen, "마지막 줄 다음에 닫힘");
            Assert.AreEqual(VillagerBehaviour.State.Pausing, v.Current);
            yield return null;
            Assert.IsFalse(dialogue.IsOpen, "닫는 F로 다시 말 걸지 않음");

            Press(keyboard.wKey);
            yield return WaitSeconds(0.3f);
            Release(keyboard.wKey);
            Assert.Greater(Vector2.Distance(before, Flat(player.position)), 0.2f, "닫으면 다시 움직임");
        }

        [Test]
        public void DialogueTexts_AllGlyphsInFont()
        {
            var font = dialogue.lineText.font;
            var all = string.Concat(village.rules.villagers.SelectMany(v =>
                v.morningLines.Concat(v.daytimeLines).Concat(v.eveningLines).Concat(v.smallTalk).Append(v.displayName)));
            var hint = dialogue.panel.transform.Find("Hint").GetComponent<TMPro.TMP_Text>().text;
            Assert.IsTrue(font.HasCharacters(all + hint, out uint[] missing, false, true),
                $"글꼴에 없는 글자: {(missing == null ? "" : string.Join(",", missing.Select(u => (char)u)))}");
        }

        [UnityTest]
        public IEnumerator TalkWithGamepadA()
        {
            StandInFrontOfVillager();
            yield return Frames(3);
            yield return Tap(gamepad.buttonSouth);
            Assert.IsTrue(dialogue.IsOpen);
            yield return Tap(gamepad.buttonSouth);
            yield return Tap(gamepad.dpad.down);      // 고르기: 안녕(2-5)
            yield return Tap(gamepad.buttonSouth);
            Assert.IsFalse(dialogue.IsOpen);
        }

        [UnityTest]
        public IEnumerator Talking_TabDoesNotOpenInventory()
        {
            StandInFrontOfVillager();
            yield return Frames(3);
            yield return Tap(keyboard.fKey);
            yield return Tap(keyboard.tabKey);
            Assert.IsFalse(Object.FindAnyObjectByType<InventoryWindow>().IsOpen);
        }

        [UnityTest]
        public IEnumerator SleepDuringTalk_ClosesDialogue_PlayerFree()
        {
            StandInFrontOfVillager();
            yield return Frames(3);
            yield return Tap(keyboard.fKey);
            Assert.IsTrue(dialogue.IsOpen);
            yield return Tap(keyboard.f8Key);
            yield return null;
            Assert.IsFalse(dialogue.IsOpen);
            Assert.IsTrue(player.GetComponent<PlayerController>().enabled, "잠에서 깨면 다시 움직일 수 있음");
        }
    }
}
