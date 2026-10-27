using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// 도구 · 땅 칸을 쓰는 PlayMode 테스트의 공통 바탕: 도구 들기 · 휘두르기 · 빈 땅 찾기 · 구멍 파기.
    /// 키 입력이 필요한 준비는 [UnitySetUp]이 아니라 테스트 본문에서 부름([UnitySetUp]이 가상 장치보다 먼저 돎).
    /// </summary>
    public abstract class ToolSceneTest : VillageSceneTest
    {
        protected static readonly Vector3 Plaza = new(-6f, 0f, 2f);
        protected const int Axe = 2, Shovel = 3, WateringCan = 4;

        protected PlayerTools tools;
        protected PlayerInventory playerInventory;
        protected Inventory Inv => playerInventory.Inventory;
        protected PlayerInteractor interactor;
        protected VillageGround ground;
        protected GroundGrid Grid => ground.Grid;

        [UnitySetUp]
        public IEnumerator FindToolParts()
        {
            tools = player.GetComponent<PlayerTools>();
            playerInventory = player.GetComponent<PlayerInventory>();
            interactor = player.GetComponent<PlayerInteractor>();
            ground = Object.FindAnyObjectByType<VillageGround>();
            yield break;
        }

        protected IEnumerator Tap(ButtonControl button)
        {
            Press(button);
            yield return null;
            Release(button);
            yield return null;
        }

        protected IEnumerator Hold(int toolIndex)
        {
            if (ToolBelt.FindSlot(Inv.Slots, tools.tools[toolIndex].id) < 0)
                Assert.IsTrue(playerInventory.TryAddTool(tools.tools[toolIndex]));
            yield return Tap(keyboard[Key.Digit1 + toolIndex]);
            Assert.AreSame(tools.tools[toolIndex], tools.Equipped);
        }

        protected IEnumerator Swing()
        {
            yield return Tap(keyboard.spaceKey);
            yield return WaitSeconds(settings.toolSwingTime + 0.1f);
        }

        protected int Durability => Inv.Slots[tools.EquippedSlot].durability;

        protected static ItemDefinition Item(string id) =>
            Resources.FindObjectsOfTypeAll<ItemDefinition>().First(i => i.id == id);

        /// <summary>광장 근처에서 서 있을 칸 c와 앞으로 세 칸이 모두 빈 땅이고 주변에 다른 대상이 없는 곳</summary>
        protected Vector2Int FindOpenSpot()
        {
            Vector2Int start = Grid.ToCell(Flat(Plaza));
            var interactables = Object.FindObjectsByType<Interactable>();
            for (int r = 8; r < 40; r++)
            for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                var c = start + new Vector2Int(dx, dy);
                bool open = true;
                for (int k = -1; k <= 3 && open; k++)
                    open = Grid.CanPlace(c + Vector2Int.up * k) && Grid.CanPlace(c + Vector2Int.up * k + Vector2Int.right) && Grid.CanPlace(c + Vector2Int.up * k + Vector2Int.left);
                if (!open) continue;
                Vector2 ahead = Grid.CellCenter(c + Vector2Int.up);
                if (interactables.Any(i => Vector2.Distance(Flat(i.transform.position), ahead) < 2.5f))
                    continue;
                return c;
            }
            Assert.Fail("빈 땅을 찾지 못했습니다.");
            return default;
        }

        protected IEnumerator StandFacingNorth(Vector2Int cell)
        {
            Vector2 c = Grid.CellCenter(cell);
            Teleport(new Vector3(c.x, 0f, c.y), Vector3.forward);
            yield return Frames(3);
        }

        /// <summary>빈 땅에 서서 앞 칸에 구멍을 팜. 앞 칸 번호를 알려 줌</summary>
        protected IEnumerator DigHoleAhead(System.Action<Vector2Int> report)
        {
            yield return Hold(Shovel);
            var spot = FindOpenSpot();
            yield return StandFacingNorth(spot);
            yield return Swing();
            Assert.AreEqual(GroundObjectKind.Hole, Grid.Get(spot + Vector2Int.up));
            yield return Frames(2);
            Assert.AreEqual(InteractableKind.Hole, interactor.Target ? interactor.Target.kind : InteractableKind.None, "구멍이 대상");
            report(spot + Vector2Int.up);
        }
    }
}
