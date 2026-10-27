using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>1-5 도구 확인 (도구 상자 · 숫자키 들기 · Space 휘두르기 · 맞았을 때만 내구도 · 부서짐 · 패드).</summary>
    public class ToolUseTests : VillageSceneTest
    {
        static readonly Vector3 Plaza = new(-6f, 0f, 2f);
        const int Axe = 2;  // ToolKind 순서

        PlayerTools tools;
        PlayerInventory playerInventory;
        Inventory Inv => playerInventory.Inventory;
        PlayerInteractor interactor;

        [UnitySetUp]
        public IEnumerator FindParts()
        {
            tools = player.GetComponent<PlayerTools>();
            playerInventory = player.GetComponent<PlayerInventory>();
            interactor = player.GetComponent<PlayerInteractor>();
            Assert.IsNotNull(tools);
            yield break;
        }

        IEnumerator Tap(ButtonControl button)
        {
            Press(button);
            yield return null;
            Release(button);
            yield return null;
        }

        void GiveAllTools()
        {
            foreach (var tool in tools.tools)
                Assert.IsTrue(playerInventory.TryAddTool(tool));
        }

        IEnumerator SwingAndWait(ButtonControl button)
        {
            yield return Tap(button);
            yield return WaitSeconds(settings.toolSwingTime + 0.1f);
            Assert.IsFalse(tools.IsSwinging);
        }

        int AxeDurability => Inv.Slots[ToolBelt.FindSlot(Inv.Slots, tools.tools[Axe].id)].durability;

        IEnumerator StandAtTree()
        {
            var tree = Object.FindObjectsByType<FruitTreeBehaviour>().OrderBy(t => Vector3.Distance(t.transform.position, Plaza)).First();
            Teleport(tree.transform.position + Vector3.back * 0.9f, Vector3.forward);
            yield return Frames(3);
            Assert.AreEqual(InteractableKind.Tree, interactor.Target.kind, "나무가 대상이어야 합니다.");
        }

        [UnityTest]
        public IEnumerator ToolBox_GivesFiveTools_WithFullDurability()
        {
            var box = Object.FindAnyObjectByType<ToolBox>();
            Vector3 spot = box.transform.position + Vector3.back * 1.0f;
            Teleport(spot, Vector3.ProjectOnPlane(box.transform.position - spot, Vector3.up));
            yield return Frames(3);
            Assert.AreSame(box.GetComponent<Interactable>(), interactor.Target, "도구 상자가 대상이어야 합니다.");

            yield return Tap(keyboard.fKey);
            for (int i = 0; i < tools.tools.Length; i++)
            {
                Assert.AreEqual(tools.tools[i].id, Inv.Slots[i].itemId, $"{i + 1}번 칸");
                Assert.AreEqual(tools.tools[i].durability, Inv.Slots[i].durability);
            }
        }

        [UnityTest]
        public IEnumerator NumberKeys_Equip_SameKeyUnequips()
        {
            GiveAllTools();
            yield return Tap(keyboard.digit3Key);
            Assert.AreSame(tools.tools[Axe], tools.Equipped, "3 = 도끼");
            Assert.IsTrue(tools.holdVisuals[Axe].activeSelf, "도끼가 손에 보여야 합니다.");

            yield return Tap(keyboard.digit5Key);
            Assert.AreEqual(ToolKind.WateringCan, tools.Equipped.kind, "5 = 물뿌리개");
            Assert.IsFalse(tools.holdVisuals[Axe].activeSelf);

            yield return Tap(keyboard.digit5Key);
            Assert.IsNull(tools.Equipped, "같은 키 = 내려놓기");
            Assert.IsTrue(tools.holdVisuals.All(v => !v.activeSelf));
        }

        [UnityTest]
        public IEnumerator NoTool_KeyDoesNothing_SpaceDoesNothing()
        {
            int used = 0;
            tools.ToolUsed += (_, __) => used++;
            yield return Tap(keyboard.digit3Key);
            Assert.IsNull(tools.Equipped);
            yield return Tap(keyboard.spaceKey);
            Assert.AreEqual(0, used);
        }

        [UnityTest]
        public IEnumerator Axe_OnTree_LosesOneDurability()
        {
            GiveAllTools();
            yield return StandAtTree();
            yield return Tap(keyboard.digit3Key);

            Interactable hitTarget = null;
            tools.ToolUsed += (_, target) => hitTarget = target;
            yield return SwingAndWait(keyboard.spaceKey);

            Assert.AreEqual(InteractableKind.Tree, hitTarget ? hitTarget.kind : InteractableKind.None, "나무에 맞음");
            Assert.AreEqual(tools.tools[Axe].durability - 1, AxeDurability);
        }

        [UnityTest]
        public IEnumerator SwingAtAir_KeepsDurability()  // 5-7
        {
            GiveAllTools();
            yield return Tap(keyboard.digit3Key);
            Teleport(new Vector3(-6f, 0f, -8f), Vector3.back);  // 광장 남쪽 길, 앞이 빔
            yield return Frames(3);
            Assert.IsNull(interactor.Target, "앞에 대상이 없어야 합니다.");

            for (int i = 0; i < 3; i++)
                yield return SwingAndWait(keyboard.spaceKey);
            Assert.AreEqual(tools.tools[Axe].durability, AxeDurability);
        }

        [UnityTest]
        public IEnumerator Axe_OnNoticeBoard_KeepsDurability()
        {
            GiveAllTools();
            yield return Tap(keyboard.digit3Key);
            var board = GameObject.Find("NoticeBoard").transform;
            Vector3 normal = Vector3.ProjectOnPlane(board.forward, Vector3.up).normalized;
            Vector3 spot = board.position + normal * 1.6f;
            Teleport(spot, Vector3.ProjectOnPlane(board.position - spot, Vector3.up));
            yield return Frames(3);
            Assert.AreEqual(InteractableKind.NoticeBoard, interactor.Target.kind);

            yield return SwingAndWait(keyboard.spaceKey);
            Assert.AreEqual(tools.tools[Axe].durability, AxeDurability, "도끼가 다루지 않는 대상은 맞음이 아님");
        }

        [UnityTest]
        public IEnumerator FiftiethHit_BreaksAndUnequips()  // 5-6
        {
            GiveAllTools();
            yield return StandAtTree();
            yield return Tap(keyboard.digit3Key);
            int slot = tools.EquippedSlot;
            for (int i = 0; i < tools.tools[Axe].durability - 1; i++)
                Inv.UseDurability(slot);
            Assert.AreEqual(1, Inv.Slots[slot].durability);

            yield return SwingAndWait(keyboard.spaceKey);
            Assert.IsTrue(Inv.Slots[slot].IsEmpty, "부서진 도끼는 인벤토리에서 사라짐");
            Assert.IsNull(tools.Equipped, "빈손");
            Assert.IsFalse(tools.holdVisuals[Axe].activeSelf);
        }

        [UnityTest]
        public IEnumerator SpaceDuringSwing_IsIgnored()
        {
            GiveAllTools();
            yield return Tap(keyboard.digit3Key);
            int used = 0;
            tools.ToolUsed += (_, __) => used++;
            yield return Tap(keyboard.spaceKey);
            yield return Tap(keyboard.spaceKey);
            yield return WaitSeconds(settings.toolSwingTime + 0.1f);
            Assert.AreEqual(1, used);
        }

        [UnityTest]
        public IEnumerator Gamepad_DpadCycles_XSwings()
        {
            GiveAllTools();
            yield return Tap(gamepad.dpad.right);
            Assert.AreEqual(ToolKind.Net, tools.Equipped.kind, "오른쪽 = 첫 도구");
            yield return Tap(gamepad.dpad.left);
            Assert.IsNull(tools.Equipped, "왼쪽 = 빈손");
            yield return Tap(gamepad.dpad.left);
            Assert.AreEqual(ToolKind.WateringCan, tools.Equipped.kind, "빈손에서 왼쪽 = 마지막 도구");

            int used = 0;
            tools.ToolUsed += (_, __) => used++;
            yield return SwingAndWait(gamepad.buttonWest);
            Assert.AreEqual(1, used);
        }
    }
}
