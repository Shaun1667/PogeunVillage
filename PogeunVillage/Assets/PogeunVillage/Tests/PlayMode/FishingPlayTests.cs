using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// 3-6a 확인 (강에 그림자 4개 · 땅에 던지면 알림 · 빈 물에 던지고 Space로 거두기 · 걷지 않음 /
    /// 그림자 앞에 던짐 → 입질에 Space = 낚음 · 내구도 -1 · 그림자 사라짐 → 게임 30분 뒤 다시 4개 /
    /// 너무 일찍 · 늦게 = 놓침 / 가득 차면 발밑 / 판매함 값).
    /// </summary>
    public class FishingPlayTests : ToolSceneTest
    {
        FishingRodUser rod;
        FishShadows shadows;
        HudView hud;
        DayCycle dayCycle;

        const int RodKey = 1;  // 숫자 2

        [UnitySetUp]
        public IEnumerator FindFishing()
        {
            rod = player.GetComponent<FishingRodUser>();
            shadows = Object.FindAnyObjectByType<FishShadows>();
            hud = Object.FindAnyObjectByType<HudView>();
            dayCycle = Object.FindAnyObjectByType<DayCycle>();
            Assert.IsNotNull(rod);
            Assert.IsNotNull(shadows);
            yield break;
        }

        int RodDurability => Inv.Slots[ToolBelt.FindSlot(Inv.Slots, "FishingRod")].durability;

        IEnumerator HoldRod()
        {
            Assert.GreaterOrEqual(playerInventory.Inventory.TryAddWithDurability("FishingRod", tools.tools[RodKey].durability), 0);
            yield return Tap(keyboard.digit2Key);
            Assert.AreEqual(ToolKind.FishingRod, tools.Equipped.kind);
        }

        /// <summary>강가 풀밭에 북쪽을 보고 서면 찌가 물 칸에 떨어지는 자리. 그 물 칸을 알려 줌(다른 그림자는 모두 치움)</summary>
        IEnumerator StandAtRiverbank(System.Action<Vector2Int> report)
        {
            foreach (var s in shadows.Shadows.ToList())
                shadows.Remove(s);
            var data = ground.data;
            foreach (var w in data.WaterCells())
            {
                Vector2 c = Grid.CellCenter(w);
                Vector3 stand = new(c.x, 0f, c.y - rod.rules.castDistance);
                var standCell = ground.CellAt(stand);
                if (data.IsWater(standCell) || !Grid.CanPlace(standCell) || !Grid.CanPlace(standCell + Vector2Int.down))
                    continue;
                Teleport(stand, Vector3.forward);
                yield return Frames(3);
                report(ground.CellAt(player.position + player.forward * rod.rules.castDistance));
                yield break;
            }
            Assert.Fail("강가 자리를 찾지 못했습니다.");
        }

        IEnumerator WaitPhase(BitePhase phase, float timeout = 10f)
        {
            float end = Time.time + timeout;
            while (rod.Phase != phase && Time.time < end)
                yield return null;
            Assert.AreEqual(phase, rod.Phase);
        }

        [Test]
        public void River_HasWater_FourShadowsOnWater()
        {
            Assert.Greater(shadows.WaterCellCount, 50, "강 물 칸");
            Assert.AreEqual(4, shadows.Shadows.Count);
            Assert.IsTrue(shadows.Shadows.All(s => ground.data.IsWater(s.Cell)));
            Assert.AreEqual(8, rod.rules.fish.Length);
        }

        [UnityTest]
        public IEnumerator CastOnLand_Notice_NotCasting()
        {
            yield return HoldRod();
            Teleport(Plaza + Vector3.back * 4f, Vector3.forward);
            yield return Frames(3);
            yield return Tap(keyboard.spaceKey);
            Assert.IsFalse(rod.IsCasting);
            StringAssert.Contains("물가에서", hud.noticeText.text);
            Assert.IsFalse(tools.IsSwinging, "낚싯대는 휘두르지 않음");
        }

        [UnityTest]
        public IEnumerator CastEmptyWater_CantWalk_SpaceReelsIn()
        {
            yield return HoldRod();
            yield return StandAtRiverbank(_ => { });
            yield return Tap(keyboard.spaceKey);
            Assert.IsTrue(rod.IsCasting, "던짐");
            Assert.IsTrue(rod.bobber.gameObject.activeSelf, "찌");
            Assert.IsNull(rod.Target, "그림자 없음");
            yield return null;
            Assert.IsFalse(player.GetComponent<PlayerController>().enabled, "던진 동안 걷지 않음");
            yield return Tap(keyboard.spaceKey);
            Assert.IsFalse(rod.IsCasting, "거두기");
            Assert.IsFalse(rod.bobber.gameObject.activeSelf);
            yield return null;
            Assert.IsTrue(player.GetComponent<PlayerController>().enabled);
        }

        [UnityTest]
        public IEnumerator BiteThenSpace_Caught_Durability_Respawn()
        {
            yield return HoldRod();
            Vector2Int water = default;
            yield return StandAtRiverbank(c => water = c);
            int removedAt = dayCycle.Clock.MinutesSinceDayStart;  // 원래 그림자 4개를 치운 게임 분
            var carp = rod.rules.fish.First(f => f.id == "CrucianCarp");
            shadows.SpawnAt(water, carp);
            int before = RodDurability;

            yield return Tap(keyboard.spaceKey);
            Assert.IsNotNull(rod.Target, "그림자가 다가옴");
            yield return WaitPhase(BitePhase.Bite);
            yield return Tap(keyboard.spaceKey);
            Assert.IsFalse(rod.IsCasting);
            Assert.AreEqual(1, Inv.Count("CrucianCarp"), "붕어를 낚음");
            Assert.AreEqual(before - 1, RodDurability, "낚았을 때 내구도 -1");
            Assert.AreEqual(0, shadows.Shadows.Count, "그림자 사라짐");
            StringAssert.Contains("낚았어요", hud.noticeText.text);

            AdvanceTo(removedAt + 29);
            Assert.AreEqual(0, shadows.Shadows.Count, "게임 29분엔 아직");
            AdvanceTo(removedAt + 30);
            Assert.AreEqual(4, shadows.Shadows.Count, "게임 30분 뒤 다시 4개(더 늘지 않음)");
        }

        void AdvanceTo(int minute)
        {
            int now = dayCycle.Clock.MinutesSinceDayStart;
            if (minute > now)
                dayCycle.Clock.Advance((minute - now) * dayCycle.rules.realSecondsPerGameMinute);
        }

        [UnityTest]
        public IEnumerator PressEarly_Spooked_NoDurabilityLoss()
        {
            yield return HoldRod();
            Vector2Int water = default;
            yield return StandAtRiverbank(c => water = c);
            shadows.SpawnAt(water, rod.rules.fish[0]);
            int before = RodDurability;
            yield return Tap(keyboard.spaceKey);
            yield return Frames(2);
            Assert.AreEqual(BitePhase.Approaching, rod.Phase);
            yield return Tap(keyboard.spaceKey);
            Assert.IsFalse(rod.IsCasting);
            StringAssert.Contains("너무 일찍", hud.noticeText.text);
            Assert.AreEqual(0, Inv.Count(rod.rules.fish[0].id));
            Assert.AreEqual(before, RodDurability);
            Assert.AreEqual(0, shadows.Shadows.Count, "도망");
        }

        [UnityTest]
        public IEnumerator TooLate_Missed()
        {
            yield return HoldRod();
            Vector2Int water = default;
            yield return StandAtRiverbank(c => water = c);
            shadows.SpawnAt(water, rod.rules.fish[0]);
            yield return Tap(keyboard.spaceKey);
            yield return WaitPhase(BitePhase.Bite);
            yield return WaitSeconds(rod.rules.biteWindow + 0.2f);
            Assert.IsFalse(rod.IsCasting, "늦으면 끝");
            StringAssert.Contains("놓쳤어요", hud.noticeText.text);
            Assert.AreEqual(0, Inv.Count(rod.rules.fish[0].id));
        }

        [UnityTest]
        public IEnumerator FullInventory_FishDroppedAtFeet()
        {
            yield return HoldRod();
            while (Inv.UsedSlots < Inv.Slots.Count)
                Assert.IsTrue(playerInventory.TryAdd(Item("Apple"), 10));
            Vector2Int water = default;
            yield return StandAtRiverbank(c => water = c);
            var loach = rod.rules.fish.First(f => f.id == "Loach");
            shadows.SpawnAt(water, loach);
            yield return Tap(keyboard.spaceKey);
            yield return WaitPhase(BitePhase.Bite);
            yield return Tap(keyboard.spaceKey);
            var dropped = Object.FindObjectsByType<Pickup>().FirstOrDefault(p => p.item == loach);
            Assert.IsNotNull(dropped, "발밑에 떨어짐");
            Assert.Less(Vector3.Distance(dropped.transform.position, player.position), 1.5f);
            StringAssert.Contains("발밑에", hud.noticeText.text + string.Join("|", hud.PendingNotices));
        }

        [Test]
        public void Fish_SellAtSellBox_Price()
        {
            var shop = Object.FindAnyObjectByType<Shop>();
            Assert.AreEqual(160, shop.SellPriceOf("CrucianCarp"));
            Assert.AreEqual(1500, shop.SellPriceOf("GoldenCarp"));
            Assert.AreEqual("황금잉어", shop.NameOf("GoldenCarp"));
        }
    }
}
