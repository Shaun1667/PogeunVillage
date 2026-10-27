using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>1-7 도끼 확인 (치면 목재 2 · 5번이면 벌목 · 열매는 베어질 때만 · 밑동 · 가장자리 숲은 안 베임 · 가득 차면 목재가 땅에).</summary>
    public class AxeTests : VillageSceneTest
    {
        static readonly Vector3 Plaza = new(-6f, 0f, 2f);
        const int Axe = 2;

        PlayerTools tools;
        PlayerInventory playerInventory;
        Inventory Inv => playerInventory.Inventory;
        PlayerInteractor interactor;
        GatheringRules rules;

        [UnitySetUp]
        public IEnumerator FindParts()
        {
            tools = player.GetComponent<PlayerTools>();
            playerInventory = player.GetComponent<PlayerInventory>();
            interactor = player.GetComponent<PlayerInteractor>();
            rules = Object.FindAnyObjectByType<ChoppableTreeBehaviour>().rules;
            yield break;
        }

        // [UnitySetUp]은 가상 장치를 만드는 [SetUp]보다 먼저 돌므로, 키 입력이 필요한 준비는 테스트 본문에서
        IEnumerator HoldAxe()
        {
            Assert.IsTrue(playerInventory.TryAddTool(tools.tools[Axe]));
            Press(keyboard.digit3Key);
            yield return null;
            Release(keyboard.digit3Key);
            yield return null;
            Assert.AreEqual(ToolKind.Axe, tools.Equipped.kind);
        }

        int AxeDurability => Inv.Slots[tools.EquippedSlot].durability;

        IEnumerator Swing()
        {
            Press(keyboard.spaceKey);
            yield return null;
            Release(keyboard.spaceKey);
            yield return WaitSeconds(settings.toolSwingTime + 0.1f);
        }

        IEnumerator StandAt(Component target)
        {
            Teleport(target.transform.position + Vector3.back * 0.9f, Vector3.forward);
            yield return Frames(3);
            Assert.AreSame(target.GetComponent<Interactable>(), interactor.Target, "그 나무가 대상이어야 합니다.");
        }

        ChoppableTreeBehaviour NearestFruitTree() => Object.FindObjectsByType<ChoppableTreeBehaviour>()
            .Where(t => t.GetComponent<FruitTreeBehaviour>())
            .OrderBy(t => Vector3.Distance(t.transform.position, Plaza)).First();

        [UnityTest]
        public IEnumerator OneHit_GivesTwoWood_TreeStands()  // 5-1
        {
            yield return HoldAxe();
            var tree = NearestFruitTree();
            yield return StandAt(tree);
            yield return Swing();

            Assert.AreEqual(rules.axeWoodPerHit, Inv.Count("Wood"), "목재 2");
            Assert.IsTrue(tree != null, "나무는 그대로");
            Assert.AreEqual(1, tree.Hits);
            Assert.AreEqual(tools.tools[Axe].durability - 1, AxeDurability);
        }

        [UnityTest]
        public IEnumerator FourHits_FruitStaysOnTree()
        {
            yield return HoldAxe();
            var tree = NearestFruitTree();
            var fruit = tree.GetComponent<FruitTreeBehaviour>();
            int before = fruit.FruitCount;
            yield return StandAt(tree);
            for (int i = 0; i < rules.axeHitsToFell - 1; i++)
                yield return Swing();
            Assert.IsTrue(tree != null, "4번으로는 안 베어짐");
            Assert.AreEqual(before, fruit.FruitCount, "베어지기 전엔 열매가 안 떨어짐");
        }

        [UnityTest]
        public IEnumerator FifthHit_Fells_DropsFruit_LeavesStump()  // 5-2, 5-3
        {
            yield return HoldAxe();
            var tree = NearestFruitTree();
            Vector3 spot = tree.transform.position;
            string fruitId = tree.GetComponent<FruitTreeBehaviour>().fruitItem.id;
            int fruitBefore = tree.GetComponent<FruitTreeBehaviour>().FruitCount;
            yield return StandAt(tree);
            for (int i = 0; i < rules.axeHitsToFell; i++)
                yield return Swing();
            yield return WaitSeconds(settings.fruitFallTime + 0.1f);

            Assert.IsTrue(tree == null, "베어진 나무는 사라짐");
            Assert.AreEqual(rules.axeWoodPerHit * rules.axeHitsToFell, Inv.Count("Wood"), "목재 10");
            Assert.AreEqual(tools.tools[Axe].durability - rules.axeHitsToFell, AxeDurability);

            var stump = Object.FindObjectsByType<Interactable>().FirstOrDefault(i => i.kind == InteractableKind.Stump);
            Assert.IsNotNull(stump, "밑동이 남음");
            Assert.Less(Vector3.Distance(stump.transform.position, spot), 0.01f, "나무 자리에");

            int dropped = Object.FindObjectsByType<Pickup>()
                .Count(p => p.item && p.item.id == fruitId && Vector2.Distance(Flat(p.transform.position), Flat(spot)) < 2f);
            Assert.AreEqual(fruitBefore, dropped, "달려 있던 열매가 전부 땅에");
        }

        [UnityTest]
        public IEnumerator StumpAndForestTree_AreNotAxeHits()
        {
            yield return HoldAxe();
            // 밑동 만들기 — 열매 없는 나무로(떨어진 열매가 밑동보다 가까우면 열매가 대상이 되므로)
            var tree = Object.FindObjectsByType<ChoppableTreeBehaviour>()
                .Where(t => !t.GetComponent<FruitTreeBehaviour>())
                .OrderBy(t => Vector3.Distance(t.transform.position, Plaza)).First();
            yield return StandAt(tree);
            for (int i = 0; i < rules.axeHitsToFell; i++)
                yield return Swing();
            yield return Frames(2);
            Assert.AreEqual(InteractableKind.Stump, interactor.Target ? interactor.Target.kind : InteractableKind.None, "밑동이 대상");
            int durability = AxeDurability;
            yield return Swing();
            Assert.AreEqual(durability, AxeDurability, "밑동은 도끼에 맞지 않음");

            // 가장자리 숲 나무
            var forest = Object.FindObjectsByType<Interactable>().Where(i => i.kind == InteractableKind.ForestTree)
                .OrderBy(i => i.transform.position.magnitude).First();
            yield return StandAt(forest);
            int wood = Inv.Count("Wood");
            yield return Swing();
            Assert.AreEqual(durability, AxeDurability, "가장자리 숲은 도끼에 맞지 않음");
            Assert.AreEqual(wood, Inv.Count("Wood"));
        }

        [UnityTest]
        public IEnumerator FullInventory_WoodDropsOnGround()
        {
            yield return HoldAxe();
            for (int i = 0; Inv.UsedSlots < Inv.Slots.Count; i++)
                Assert.IsTrue(Inv.TryAdd("Filler" + i, 1));

            var tree = NearestFruitTree();
            yield return StandAt(tree);
            yield return Swing();

            Assert.AreEqual(0, Inv.Count("Wood"));
            var woods = Object.FindObjectsByType<Pickup>().Where(p => p.item && p.item.id == "Wood").ToArray();
            Assert.AreEqual(rules.axeWoodPerHit, woods.Length, "목재 2개가 땅에");
            foreach (var w in woods)
            {
                Assert.Less(Vector2.Distance(Flat(w.transform.position), Flat(tree.transform.position)), 2f, "나무 근처");
                Assert.Less(Mathf.Abs(w.transform.position.y - GroundHeight(w.transform.position)), 0.05f, "땅 위");
            }
        }
    }
}
