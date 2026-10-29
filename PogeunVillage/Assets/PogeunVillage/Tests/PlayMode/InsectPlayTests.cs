using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// 3-7a 확인 (시작 6마리 · 지금 시간대 곤충 / 걸어서 다가가 Space = 잡음 · 내구도 -1 · 게임 30분 뒤 다시 /
    /// 달려오면 도망 / 예민한 곤충은 0.8m 안이면 도망 · 1m에서는 잡힘 / 헛스윙 = 근처 곤충 도망 · 내구도 그대로 / 가득 차면 발밑 / 판매함 값).
    /// </summary>
    public class InsectPlayTests : ToolSceneTest
    {
        protected override bool WithInsects => true;

        const int NetKey = 0;  // 숫자 1
        Insects insects;
        HudView hud;
        DayCycle dayCycle;

        [UnitySetUp]
        public IEnumerator FindInsects()
        {
            insects = Object.FindAnyObjectByType<Insects>();
            hud = Object.FindAnyObjectByType<HudView>();
            dayCycle = Object.FindAnyObjectByType<DayCycle>();
            Assert.IsNotNull(insects);
            yield break;
        }

        InsectDefinition Insect(string id) => insects.rules.insects.First(i => i.id == id);
        int NetDurability => Inv.Slots[ToolBelt.FindSlot(Inv.Slots, "Net")].durability;
        string Shown => hud.noticeText.text + "|" + string.Join("|", hud.PendingNotices);

        IEnumerator HoldNet()
        {
            Assert.GreaterOrEqual(Inv.TryAddWithDurability("Net", tools.tools[NetKey].durability), 0);
            yield return Tap(keyboard.digit1Key);
            Assert.AreEqual(ToolKind.Net, tools.Equipped.kind);
        }

        /// <summary>곤충을 모두 치우고 빈 풀밭에 북쪽을 보고 섬. 앞으로 distance(m)에 그 곤충을 놓음</summary>
        IEnumerator StandWithInsectAhead(InsectDefinition definition, float distance, System.Action<InsectBehaviour> report)
        {
            yield return StandClear();
            var at = player.position + Vector3.forward * distance + Vector3.up * 0.3f;
            report(insects.SpawnAt(definition, at));
            yield return Frames(2);
        }

        /// <summary>곤충을 모두(알림 없이) 치우고 빈 풀밭에 북쪽을 보고 섬</summary>
        IEnumerator StandClear()
        {
            foreach (var i in insects.All.ToList())
                insects.Remove(i);
            var spot = FindOpenSpot();
            yield return StandFacingNorth(spot);
        }

        [Test]
        public void Start_SixInsects_OfThisPeriod()
        {
            Assert.AreEqual(6, insects.All.Count);
            var period = dayCycle.Clock.Period;
            Assert.IsTrue(insects.All.All(i => i.Definition.AppearsIn(period)));
            Assert.AreEqual(8, insects.rules.insects.Length);
        }

        [UnityTest]
        public IEnumerator WalkUp_Swing_Caught_Durability_Respawn()
        {
            yield return HoldNet();
            InsectBehaviour hopper = null;
            yield return StandWithInsectAhead(Insect("Grasshopper"), 1.1f, b => hopper = b);
            int removedAt = dayCycle.Clock.MinutesSinceDayStart;
            Assert.AreSame(hopper.GetComponent<Interactable>(), interactor.Target, "메뚜기가 대상");
            int before = NetDurability;
            yield return Tap(keyboard.spaceKey);
            yield return WaitSeconds(settings.toolSwingTime + 0.1f);
            Assert.AreEqual(1, Inv.Count("Grasshopper"), "잡음");
            Assert.AreEqual(before - 1, NetDurability, "잡았을 때 내구도 -1");
            Assert.IsEmpty(insects.All);
            StringAssert.Contains("잡았어요", Shown);

            dayCycle.Clock.Advance((removedAt + 30 - dayCycle.Clock.MinutesSinceDayStart) * dayCycle.rules.realSecondsPerGameMinute);
            Assert.AreEqual(6, insects.All.Count, "게임 30분 뒤 다시 6마리");
        }

        [UnityTest]
        public IEnumerator RunningToward_Flees()
        {
            InsectBehaviour hopper = null;
            yield return StandWithInsectAhead(Insect("Grasshopper"), 4f, b => hopper = b);
            Assert.IsTrue(hopper);
            Press(keyboard.leftShiftKey);
            Press(keyboard.wKey);
            yield return WaitSeconds(1f);
            Release(keyboard.wKey);
            Release(keyboard.leftShiftKey);
            Assert.IsFalse(hopper, "달려오면 도망");
            StringAssert.Contains("도망갔어요", Shown);
        }

        [UnityTest]
        public IEnumerator Sensitive_TooClose_Flees_ButOneMeterCatches()
        {
            InsectBehaviour beetle = null;
            yield return StandWithInsectAhead(Insect("StagBeetle"), 0.6f, b => beetle = b);
            Assert.IsFalse(beetle, "예민한 사슴벌레 0.6m = 도망");

            yield return HoldNet();
            yield return StandWithInsectAhead(Insect("StagBeetle"), 1.0f, b => beetle = b);
            Assert.IsTrue(beetle, "1m에서는 그대로");
            yield return Tap(keyboard.spaceKey);
            yield return WaitSeconds(settings.toolSwingTime + 0.1f);
            Assert.AreEqual(1, Inv.Count("StagBeetle"));
        }

        [UnityTest]
        public IEnumerator MissSwing_StartlesNearby_NoDurabilityLoss()
        {
            yield return HoldNet();
            yield return StandClear();
            var side = insects.SpawnAt(Insect("HoneyBee"), player.position + Vector3.left * 1.5f + Vector3.up * 0.3f);  // 옆 = 대상 아님
            yield return Frames(2);
            Assert.IsNull(interactor.Target, "앞에 대상 없음");
            int before = NetDurability;
            yield return Tap(keyboard.spaceKey);
            yield return WaitSeconds(settings.toolSwingTime + 0.1f);
            Assert.IsFalse(side, "헛스윙에 놀라 도망");
            Assert.AreEqual(before, NetDurability);
        }

        [UnityTest]
        public IEnumerator FullInventory_DroppedAtFeet()
        {
            yield return HoldNet();
            while (Inv.UsedSlots < Inv.Slots.Count)
                Assert.IsTrue(playerInventory.TryAdd(Item("Apple"), 10));
            InsectBehaviour hopper = null;
            yield return StandWithInsectAhead(Insect("Grasshopper"), 1.1f, b => hopper = b);
            yield return Tap(keyboard.spaceKey);
            yield return WaitSeconds(settings.toolSwingTime + 0.1f);
            var dropped = Object.FindObjectsByType<Pickup>().FirstOrDefault(p => p.item == Insect("Grasshopper"));
            Assert.IsNotNull(dropped, "발밑에 떨어짐");
            StringAssert.Contains("발밑에", Shown);
        }

        [Test]
        public void Insect_SellAtSellBox_Price()
        {
            var shop = Object.FindAnyObjectByType<Shop>();
            Assert.AreEqual(1200, shop.SellPriceOf("StagBeetle"));
            Assert.AreEqual("배추흰나비", shop.NameOf("CabbageWhite"));
        }
    }
}
