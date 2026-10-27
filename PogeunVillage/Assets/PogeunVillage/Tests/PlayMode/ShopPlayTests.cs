using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>2-2 상점 · 모아 확인 (시작 3000 · 매대 F로 사기 · 판매함 F로 팔기 · 영업 시간 · 모아 부족 · 인벤토리 가득 참).</summary>
    public class ShopPlayTests : ToolSceneTest
    {
        Shop shop;
        Wallet Wallet => player.GetComponent<PlayerWallet>().Wallet;
        DayCycle dayCycle;

        [UnitySetUp]
        public IEnumerator FindShop()
        {
            shop = Object.FindAnyObjectByType<Shop>();
            dayCycle = Object.FindAnyObjectByType<DayCycle>();
            Assert.IsNotNull(shop);
            yield break;
        }

        void OpenHours() => dayCycle.Clock.Advance((shop.data.openHour - dayCycle.Clock.HourOfDay + 1) * 60);  // 문 연 지 1시간

        ShopStand Stand(string itemId) => Object.FindObjectsByType<ShopStand>().First(s => s.item.id == itemId);

        IEnumerator FaceFromFront(Component target)
        {
            Vector3 front = shop.transform.forward;  // 노점 앞(광장 쪽)
            Teleport(target.transform.position + front * 0.95f, -front);
            yield return Frames(3);
            Assert.AreSame(target.GetComponent<Interactable>(), interactor.Target, $"{target.name}가 대상");
        }

        IEnumerator BuyAt(string itemId)
        {
            yield return FaceFromFront(Stand(itemId));
            yield return Tap(keyboard.fKey);
        }

        [UnityTest]
        public IEnumerator Start_Has3000Moa_NoTools()
        {
            Assert.AreEqual(shop.data.startingMoa, Wallet.Moa);
            Assert.AreEqual(3000, Wallet.Moa);
            Assert.AreEqual(0, Inv.UsedSlots, "시작 도구 없음");
            yield break;
        }

        [UnityTest]
        public IEnumerator BuyAxe_Costs500_ToolWithFullDurability()
        {
            OpenHours();
            yield return BuyAt("Axe");
            Assert.AreEqual(2500, Wallet.Moa);
            int slot = ToolBelt.FindSlot(Inv.Slots, "Axe");
            Assert.GreaterOrEqual(slot, 0);
            Assert.AreEqual(tools.tools[Axe].durability, Inv.Slots[slot].durability);
        }

        [UnityTest]
        public IEnumerator FiveTools_Leave500_ThenNotEnoughForAnother()  // 7-1
        {
            OpenHours();
            foreach (var id in new[] { "Net", "FishingRod", "Axe", "Shovel", "WateringCan" })
                yield return BuyAt(id);
            Assert.AreEqual(500, Wallet.Moa, "5종 다 사면 500");

            yield return BuyAt("Sapling");
            Assert.AreEqual(200, Wallet.Moa);
            Assert.AreEqual(1, Inv.Count("Sapling"));

            yield return BuyAt("Axe");
            Assert.AreEqual(200, Wallet.Moa, "모아 부족 → 아무것도 안 바뀜");
            Assert.AreEqual(1, Inv.Count("Axe"));
        }

        [UnityTest]
        public IEnumerator Closed_BeforeEight_AndFromTenPm()
        {
            Assert.AreEqual(6, dayCycle.Clock.HourOfDay);
            Assert.IsFalse(shop.IsOpen);
            yield return BuyAt("Axe");
            Assert.AreEqual(3000, Wallet.Moa, "아침 6시엔 닫힘");

            dayCycle.Clock.Advance((22 - 6) * 60);  // 22시
            Assert.IsFalse(shop.IsOpen);
            yield return Tap(keyboard.fKey);
            Assert.AreEqual(3000, Wallet.Moa, "밤 10시부터 닫힘");
            Assert.AreEqual(0, Inv.Count("Axe"));
        }

        [UnityTest]
        public IEnumerator SellBox_SellsFirstSellableStack_SkipsTools()
        {
            OpenHours();
            Assert.IsTrue(playerInventory.TryAddTool(tools.tools[Axe]));  // 0번 칸 도구
            Assert.IsTrue(playerInventory.TryAdd(Item("Apple"), 3));
            Assert.IsTrue(playerInventory.TryAdd(Item("Flower"), 2));

            yield return FaceFromFront(Object.FindAnyObjectByType<SellBox>());
            yield return Tap(keyboard.fKey);
            Assert.AreEqual(3000 + 3 * Item("Apple").sellPrice, Wallet.Moa, "사과 3개 = 300");
            Assert.AreEqual(0, Inv.Count("Apple"));
            Assert.AreEqual(2, Inv.Count("Flower"), "한 번에 한 칸");
            Assert.AreEqual(1, Inv.Count("Axe"), "도구는 안 팔림");

            yield return Tap(keyboard.fKey);
            Assert.AreEqual(3000 + 300 + 2 * Item("Flower").sellPrice, Wallet.Moa, "꽃 2개 = 80");

            yield return Tap(keyboard.fKey);
            Assert.AreEqual(3380, Wallet.Moa, "팔 것이 없으면 그대로");
        }

        [UnityTest]
        public IEnumerator FullInventory_CannotBuy_MoaUnchanged()
        {
            OpenHours();
            for (int i = 0; Inv.UsedSlots < Inv.Slots.Count; i++)
                Assert.IsTrue(Inv.TryAdd("Filler" + i, 1));
            yield return BuyAt("Sapling");
            Assert.AreEqual(3000, Wallet.Moa);
            Assert.AreEqual(0, Inv.Count("Sapling"));
        }

        [UnityTest]
        public IEnumerator BoughtSapling_CanBePlanted()
        {
            OpenHours();
            yield return BuyAt("Shovel");
            yield return BuyAt("Sapling");
            Assert.AreEqual(3000 - 500 - 300, Wallet.Moa);

            Vector2Int hole = default;
            yield return DigHoleAhead(c => hole = c);
            yield return Tap(keyboard.fKey);
            yield return Frames(2);
            Assert.AreEqual(GroundObjectKind.Sapling, Grid.Get(hole));
        }
    }
}
