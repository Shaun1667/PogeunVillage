using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// 3-4a 확인 (Tab 창에서 가구 F → 놓기 모드 · 걸을 수 있음 · 앞 칸 미리보기 · R / 패드 RB 돌리기 · F 놓기 ·
    /// 못 놓는 칸 · Tab 그만두기 · 놓인 가구 F → 들기 / 그대로 · 가득 차면 못 듦 · 한 칸에 5개).
    /// </summary>
    public class FurniturePlayTests : ToolSceneTest
    {
        FurniturePlacer placer;
        FurnitureKeeper keeper;
        InventoryWindow window;
        DialogueWindow dialogue;
        HudView hud;

        ItemDefinition Fence => Item("Fence");

        [UnitySetUp]
        public IEnumerator FindFurniture()
        {
            placer = player.GetComponent<FurniturePlacer>();
            keeper = Object.FindAnyObjectByType<FurnitureKeeper>();
            window = Object.FindAnyObjectByType<InventoryWindow>();
            dialogue = Object.FindAnyObjectByType<DialogueWindow>();
            hud = Object.FindAnyObjectByType<HudView>();
            Assert.IsNotNull(placer);
            Assert.IsNotNull(keeper);
            yield break;
        }

        /// <summary>빈 땅에 북쪽을 보고 서서, 인벤토리 0번 칸(가구)을 골라 놓기 모드 시작. 앞 칸 번호</summary>
        IEnumerator BeginPlacingAtOpenSpot(System.Action<Vector2Int> report)
        {
            var spot = FindOpenSpot();
            yield return StandFacingNorth(spot);
            yield return Tap(keyboard.tabKey);
            Assert.IsTrue(window.IsOpen);
            yield return Tap(keyboard.fKey);
            Assert.IsFalse(window.IsOpen, "가구를 고르면 창이 닫힘");
            Assert.IsTrue(placer.IsPlacing, "놓기 모드");
            report(spot + Vector2Int.up);
        }

        [UnityTest]
        public IEnumerator PlaceFromInventory_Rotate_Place()
        {
            Assert.IsTrue(playerInventory.TryAdd(Fence, 2));
            Vector2Int cell = default;
            yield return BeginPlacingAtOpenSpot(c => cell = c);
            Assert.AreEqual(cell, placer.Cell, "바라보는 앞 칸");
            Assert.IsTrue(placer.CanPlaceHere);
            Assert.IsTrue(player.GetComponent<PlayerController>().enabled, "놓기 모드에서도 걸을 수 있음");
            Assert.IsFalse(interactor.enabled, "상호작용은 멈춤");

            yield return Tap(keyboard.rKey);
            Assert.AreEqual(1, placer.Rotation);
            yield return Tap(gamepad.rightShoulder);
            Assert.AreEqual(2, placer.Rotation, "패드 RB도 돌림");

            yield return Tap(keyboard.fKey);
            Assert.IsFalse(placer.IsPlacing, "놓으면 모드 끝");
            Assert.AreEqual(GroundObjectKind.Furniture, Grid.Get(cell));
            Assert.AreEqual(1, Inv.Count("Fence"), "하나 빠짐");
            var placed = keeper.Placed.Single();
            Assert.AreSame(Fence, placed.Definition);
            Assert.AreEqual(2, placed.Rotation);
            Assert.AreEqual(180f, placed.transform.eulerAngles.y, 0.5f);
            Assert.AreEqual(cell, ground.CellAt(placed.transform.position));
            yield return null;
            Assert.IsTrue(interactor.enabled, "다시 상호작용");
            Assert.IsFalse(dialogue.IsOpen, "놓은 F로 가구에 말 걸지 않음");
        }

        [UnityTest]
        public IEnumerator BlockedCell_CannotPlace_TabCancels()
        {
            Assert.IsTrue(playerInventory.TryAdd(Fence, 1));
            Teleport(Plaza + Vector3.back * 4f, Vector3.forward);  // 광장 쪽을 봄
            yield return Frames(3);
            yield return Tap(keyboard.tabKey);
            yield return Tap(keyboard.fKey);
            Assert.IsTrue(placer.IsPlacing);
            Assert.IsFalse(placer.CanPlaceHere, "광장 칸");
            yield return Tap(keyboard.fKey);
            Assert.IsTrue(placer.IsPlacing, "못 놓으면 그대로");
            StringAssert.Contains("놓을 수 없어요", hud.noticeText.text);
            Assert.IsEmpty(keeper.Placed);

            yield return Tap(keyboard.tabKey);
            Assert.IsFalse(placer.IsPlacing);
            Assert.AreEqual(1, Inv.Count("Fence"), "그만두면 그대로");
            yield return null;
            Assert.IsFalse(window.IsOpen, "그만둔 Tab으로 창이 열리지 않음");
        }

        [UnityTest]
        public IEnumerator WalkWhilePlacing_PreviewFollows()
        {
            Assert.IsTrue(playerInventory.TryAdd(Fence, 1));
            Vector2Int cell = default;
            yield return BeginPlacingAtOpenSpot(c => cell = c);
            Vector2 before = Flat(player.position);
            Press(keyboard.wKey);
            yield return WaitSeconds(0.4f);
            Release(keyboard.wKey);
            yield return Frames(2);
            Assert.Greater(Vector2.Distance(before, Flat(player.position)), 0.5f, "걸어감");
            Assert.AreNotEqual(cell, placer.Cell, "미리보기 칸도 따라옴");
            yield return Tap(keyboard.tabKey);
        }

        [UnityTest]
        public IEnumerator PlacedFurniture_F_PickUp_OrLeave()
        {
            var spot = FindOpenSpot();
            var cell = spot + Vector2Int.up;
            var placed = keeper.Place((FurnitureDefinition)Fence, cell, 0);
            Assert.IsNotNull(placed);
            yield return StandFacingNorth(spot);
            Assert.AreSame(placed.GetComponent<Interactable>(), interactor.Target);

            yield return Tap(keyboard.fKey);
            Assert.IsTrue(dialogue.IsOpen);
            Assert.AreEqual("나무 울타리", dialogue.speakerText.text);
            CollectionAssert.AreEqual(new[] { "들기", "그대로" }, dialogue.Current.Choices.ToArray());
            yield return Tap(keyboard.downArrowKey);
            yield return Tap(keyboard.fKey);                     // 그대로
            Assert.AreEqual(GroundObjectKind.Furniture, Grid.Get(cell));

            yield return Frames(2);
            yield return Tap(keyboard.fKey);
            yield return Tap(keyboard.fKey);                     // 들기
            Assert.AreEqual(GroundObjectKind.None, Grid.Get(cell), "칸이 빔");
            Assert.AreEqual(1, Inv.Count("Fence"));
            Assert.IsEmpty(keeper.Placed);
        }

        [UnityTest]
        public IEnumerator FullInventory_CannotPickUp()
        {
            var spot = FindOpenSpot();
            var cell = spot + Vector2Int.up;
            keeper.Place((FurnitureDefinition)Fence, cell, 0);
            for (int i = 0; i < Inv.Slots.Count; i++)
                Assert.IsTrue(playerInventory.TryAdd(Item("Apple"), 10));
            yield return StandFacingNorth(spot);
            yield return Tap(keyboard.fKey);
            yield return Tap(keyboard.fKey);                     // 들기
            Assert.AreEqual(GroundObjectKind.Furniture, Grid.Get(cell), "그대로 남음");
            StringAssert.Contains("가득 찼어요", hud.noticeText.text);
        }

        [Test]
        public void SameFurniture_StacksToFive()
        {
            Assert.IsTrue(playerInventory.TryAdd(Fence, 6));
            Assert.AreEqual(5, Inv.Slots[0].count);
            Assert.AreEqual(1, Inv.Slots[1].count);
        }

        [Test]
        public void BasicFurniture_EightKinds_FromBalance()
        {
            var shop = Object.FindAnyObjectByType<Shop>();
            var furniture = shop.data.catalog.OfType<FurnitureDefinition>().Where(f => !f.themeOwner).ToList();
            CollectionAssert.AreEquivalent(new[] { "Fence", "Crate", "Planter", "LogStool", "Bench", "RoundTable", "StreetLamp", "Birdhouse" },
                furniture.Select(f => f.id).ToArray());
            var bench = furniture.Single(f => f.id == "Bench");
            Assert.AreEqual((8, 800, 400, 15, 2), (bench.woodCost, bench.buyPrice, bench.sellPrice, bench.ratingPoint, bench.unlockStars));
            Assert.IsTrue(furniture.All(f => f.prefab && f.maxStack == 5));
        }
    }
}
