using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// 3-4d 확인 (친밀도 10에 닿은 대화 끝 → 답례 대사 + 테마 화분 · 다시 받지 않음 / 가득 차면 다음 대화 /
    /// 놓으면 주민 색 / 상점 목록 맨 위 오늘의 테마 가구 · 2000 모아 / 데이터 20명 × 4가지).
    /// </summary>
    public class ThemeFurniturePlayTests : ToolSceneTest
    {
        protected override bool WithVillagers => true;

        Village village;
        DialogueWindow dialogue;
        HudView hud;
        VillagerBehaviour villager;

        VillagerDefinition Who => villager.Definition;
        string Shown => hud.noticeText.text + "|" + string.Join("|", hud.PendingNotices);

        [UnitySetUp]
        public IEnumerator FindVillage()
        {
            village = Object.FindAnyObjectByType<Village>();
            dialogue = Object.FindAnyObjectByType<DialogueWindow>();
            hud = Object.FindAnyObjectByType<HudView>();
            villager = village.Villagers.First(x => village.Request == null || village.Request.VillagerId != x.Definition.id);
            yield break;
        }

        IEnumerator FaceVillager()
        {
            Vector3 front = villager.transform.forward;
            Teleport(villager.transform.position + front * 0.9f, -front);
            yield return Frames(3);
            Assert.AreSame(villager.GetComponent<Interactable>(), interactor.Target, "주민이 대상");
        }

        /// <summary>말 걸기 → 대사 넘기기 → '안녕'</summary>
        IEnumerator TalkAndSayBye()
        {
            yield return Tap(keyboard.fKey);
            Assert.IsTrue(dialogue.IsOpen);
            while (!dialogue.Current.IsChoosing)
                yield return Tap(keyboard.fKey);
            yield return Tap(keyboard.sKey);
            yield return Tap(keyboard.fKey);
        }

        [UnityTest]
        public IEnumerator Reach10_TalkEnd_GivesThemePlanter_Once()
        {
            village.Friendship.Begin(Who.id, 9);
            var planter = Who.themeFurniture[0];
            yield return FaceVillager();
            yield return TalkAndSayBye();                     // 첫 대화 +1 → 10
            Assert.AreEqual(10, village.Friendship.Get(Who.id));
            Assert.IsTrue(dialogue.IsOpen, "답례 대사");
            StringAssert.Contains(planter.displayName, dialogue.lineText.text);
            Assert.AreEqual(1, Inv.Count(planter.id));
            StringAssert.Contains("받았어요", Shown);
            yield return Tap(keyboard.fKey);
            Assert.IsFalse(dialogue.IsOpen);

            yield return Frames(2);
            yield return FaceVillager();
            yield return TalkAndSayBye();
            Assert.IsFalse(dialogue.IsOpen, "다시 받지 않음");
            Assert.AreEqual(1, Inv.Count(planter.id));
            Assert.AreEqual(1, village.ThemeRewards.Given(Who.id));
        }

        [UnityTest]
        public IEnumerator FullInventory_NextTalkGetsIt()
        {
            village.Friendship.Begin(Who.id, 25);              // 화분 차례(기준 두 개를 넘었어도 대화마다 하나)
            for (int i = 0; i < Inv.Slots.Count; i++)
                Assert.IsTrue(playerInventory.TryAdd(Item("Apple"), 10));
            yield return FaceVillager();
            yield return TalkAndSayBye();
            Assert.IsFalse(dialogue.IsOpen);
            StringAssert.Contains("다음에 받을게요", Shown);
            Assert.AreEqual(0, village.ThemeRewards.Given(Who.id));

            Inv.TakeAll(0);
            yield return Frames(2);
            yield return FaceVillager();
            yield return TalkAndSayBye();
            Assert.IsTrue(dialogue.IsOpen);
            yield return Tap(keyboard.fKey);
            Assert.AreEqual(1, Inv.Count(Who.themeFurniture[0].id));

            Inv.TakeAll(1);                                    // 벤치는 새 칸이 필요
            yield return Frames(2);
            yield return FaceVillager();
            yield return TalkAndSayBye();                     // 25도 닿아 있어 다음 대화에 벤치
            Assert.IsTrue(dialogue.IsOpen);
            yield return Tap(keyboard.fKey);
            Assert.AreEqual(1, Inv.Count(Who.themeFurniture[1].id));
        }

        [Test]
        public void PlacedThemeFurniture_WearsVillagerColors()
        {
            var keeper = Object.FindAnyObjectByType<FurnitureKeeper>();
            var bench = Who.themeFurniture[1];
            var placed = keeper.Place(bench, FindOpenSpot(), 0);
            Assert.IsNotNull(placed);
            Assert.AreEqual(bench.displayName, placed.GetComponent<Interactable>().displayName);
            var block = new MaterialPropertyBlock();
            placed.bodyRenderers[0].GetPropertyBlock(block);
            AssertColor(Who.bodyColor, block.GetColor("_BaseColor"));
            placed.accentRenderers[0].GetPropertyBlock(block);
            AssertColor(Who.accentColor, block.GetColor("_BaseColor"));
        }

        static void AssertColor(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 0.002f);
            Assert.AreEqual(expected.g, actual.g, 0.002f);
            Assert.AreEqual(expected.b, actual.b, 0.002f);
        }

        [Test]
        public void Shop_TodayTheme_TopRow_Buy2000()
        {
            var counter = Object.FindAnyObjectByType<FurnitureCounter>();
            var dayCycle = Object.FindAnyObjectByType<DayCycle>();
            var theme = counter.TodayTheme;
            Assert.IsNotNull(theme);
            Assert.IsTrue(village.Residents.IsResident(theme.themeOwner.id), "마을 주민의 테마 가구");
            Assert.AreEqual(9, counter.Count, "기본 8가지 + 오늘의 테마");
            StringAssert.StartsWith($"오늘의 테마 · {theme.displayName} · 2,000 모아", counter.Line(0));

            dayCycle.Clock.Advance((counter.shop.data.openHour - dayCycle.Clock.HourOfDay + 1) * 60);
            counter.Choose(0);
            Assert.AreEqual(1, Inv.Count(theme.id));
            Assert.AreEqual(1000, player.GetComponent<PlayerWallet>().Wallet.Moa);
        }

        [Test]
        public void Data_TwentyVillagers_FourEach_FromBalance()
        {
            var villagers = village.rules.villagers;
            Assert.AreEqual(20, villagers.Length);
            foreach (var v in villagers)
            {
                Assert.AreEqual(4, v.themeFurniture.Length, v.id);
                Assert.IsTrue(v.themeFurniture.All(f => f.themeOwner == v && f.prefab && f.buyPrice == 2000 && f.sellPrice == 1000 && f.ratingPoint == 20 && f.maxStack == 5));
                StringAssert.EndsWith("네 화분", v.themeFurniture[0].displayName);
                StringAssert.EndsWith("네 가로등", v.themeFurniture[3].displayName);
            }
            CollectionAssert.AreEqual(new[] { 10, 25, 45, 70 }, village.rules.themeRewardThresholds);
        }
    }
}
