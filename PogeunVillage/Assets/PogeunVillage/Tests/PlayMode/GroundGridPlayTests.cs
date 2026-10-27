using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>1-6 땅 칸 확인 (꽃이 칸에 등록 · 막힌 칸 · 꽃을 꺾으면 칸이 빔 · 삽/물뿌리개를 들면 앞 칸 표시).</summary>
    public class GroundGridPlayTests : VillageSceneTest
    {
        static readonly Vector3 Plaza = new(-6f, 0f, 2f);

        VillageGround ground;
        GroundGrid Grid => ground.Grid;
        GroundCursor cursor;
        PlayerTools tools;
        PlayerInteractor interactor;

        [UnitySetUp]
        public IEnumerator FindParts()
        {
            ground = Object.FindAnyObjectByType<VillageGround>();
            cursor = Object.FindAnyObjectByType<GroundCursor>();
            tools = player.GetComponent<PlayerTools>();
            interactor = player.GetComponent<PlayerInteractor>();
            Assert.IsNotNull(ground);
            Assert.IsNotNull(cursor);
            yield break;
        }

        IEnumerator Tap(ButtonControl button)
        {
            Press(button);
            yield return null;
            Release(button);
            yield return null;
        }

        Vector2Int CellOf(Component c) => ground.CellAt(c.transform.position);

        [UnityTest]
        public IEnumerator Flowers_AreOnCellCenters_OnePerCell()
        {
            var flowers = Object.FindObjectsByType<GridObject>().Where(g => g.kind == GroundObjectKind.Flower).ToArray();
            Assert.Greater(flowers.Length, 0);
            Assert.IsTrue(flowers.All(f => f.Registered), "모든 꽃이 칸에 등록");
            Assert.AreEqual(flowers.Length, flowers.Select(f => f.Cell).Distinct().Count(), "한 칸에 한 송이");
            foreach (var f in flowers)
            {
                Assert.AreEqual(GroundObjectKind.Flower, Grid.Get(f.Cell));
                Assert.IsFalse(Grid.IsBlocked(f.Cell), "꽃은 막힌 칸에 없음");
                Assert.Less(Vector2.Distance(Flat(f.transform.position), Grid.CellCenter(f.Cell)), 0.01f, "칸 가운데");
            }
            yield break;
        }

        [UnityTest]
        public IEnumerator KnownPlaces_AreBlocked()
        {
            Assert.IsTrue(Grid.IsBlocked(Grid.ToCell(Flat(Plaza))), "광장");
            Assert.IsTrue(Grid.IsBlocked(Grid.ToCell(new Vector2(9.5f, -12f))), "강");
            Assert.IsTrue(Grid.IsBlocked(Grid.ToCell(new Vector2(0f, -70f))), "바다");
            Assert.IsTrue(Grid.IsBlocked(CellOf(GameObject.Find("House_Rabbit").transform)), "집");
            Assert.IsTrue(Grid.IsBlocked(CellOf(Object.FindAnyObjectByType<FruitTreeBehaviour>())), "나무");
            Assert.IsTrue(Grid.IsBlocked(CellOf(Object.FindAnyObjectByType<SellBox>())), "상점 판매함");
            Assert.IsTrue(Grid.IsBlocked(new Vector2Int(1000, 0)), "맵 밖");
            yield break;
        }

        [UnityTest]
        public IEnumerator PickingFlower_FreesItsCell()
        {
            var flower = Object.FindObjectsByType<GridObject>().OrderBy(g => Vector3.Distance(g.transform.position, Plaza)).First();
            Teleport(flower.transform.position + Vector3.back * 0.7f, Vector3.forward);
            yield return Frames(3);
            var target = interactor.Target.GetComponent<GridObject>();
            Assert.IsNotNull(target, "꽃이 대상");
            Vector2Int cell = target.Cell;

            yield return Tap(keyboard.fKey);
            yield return Frames(2);
            Assert.AreEqual(GroundObjectKind.None, Grid.Get(cell), "꺾은 꽃의 칸이 빔");
            Assert.IsTrue(Grid.CanPlace(cell));
        }

        void GiveAllTools()
        {
            var inventory = player.GetComponent<PlayerInventory>();
            foreach (var tool in tools.tools)
                Assert.IsTrue(inventory.TryAddTool(tool));
        }

        [UnityTest]
        public IEnumerator Cursor_ShowsOnlyForShovelAndWateringCan()
        {
            GiveAllTools();
            yield return Frames(1);
            Assert.IsFalse(cursor.Visible, "빈손");

            yield return Tap(keyboard.digit4Key);
            Assert.IsTrue(cursor.Visible, "삽");
            Vector2Int expected = Grid.FrontCell(Flat(player.position), Flat(player.forward), settings.groundCursorReach);
            Assert.AreEqual(expected, cursor.Cell, "발 앞 칸");
            Assert.Less(Vector2.Distance(Flat(cursor.transform.position), Grid.CellCenter(expected)), 0.01f);

            yield return Tap(keyboard.digit3Key);
            Assert.IsFalse(cursor.Visible, "도끼");
            yield return Tap(keyboard.digit5Key);
            Assert.IsTrue(cursor.Visible, "물뿌리개");
            yield return Tap(keyboard.digit5Key);
            Assert.IsFalse(cursor.Visible, "내려놓음");
        }

        Color CursorColor()
        {
            var block = new MaterialPropertyBlock();
            cursor.outline.GetComponentInChildren<Renderer>().GetPropertyBlock(block);
            return block.GetColor("_BaseColor");
        }

        // 프로퍼티 블록을 거치면 아주 작은 부동소수 오차가 생김
        static void AssertColor(Color expected, Color actual, string message)
        {
            Assert.Less(Mathf.Abs(expected.r - actual.r) + Mathf.Abs(expected.g - actual.g) + Mathf.Abs(expected.b - actual.b), 0.01f,
                $"{message}: 기대 {expected}, 실제 {actual}");
        }

        [UnityTest]
        public IEnumerator Cursor_FollowsFacing_AndTintsBlockedCells()
        {
            GiveAllTools();
            yield return Tap(keyboard.digit4Key);

            // 꽃 앞에 서서 꽃 쪽을 봄 → 막히지 않은 칸
            var flower = Object.FindObjectsByType<GridObject>().OrderBy(g => Vector3.Distance(g.transform.position, Plaza)).First();
            Teleport(flower.transform.position + Vector3.back * 0.9f, Vector3.forward);
            yield return Frames(2);
            Assert.IsFalse(Grid.IsBlocked(cursor.Cell));
            AssertColor(settings.groundCursorColor, CursorColor(), "보통 색");

            // 광장 가장자리에서 광장 쪽을 봄 → 막힌 칸
            Vector3 edge = Plaza + Vector3.back * 7.5f;
            Teleport(edge, Vector3.forward);
            yield return Frames(2);
            Assert.IsTrue(Grid.IsBlocked(cursor.Cell), "광장 칸");
            AssertColor(settings.groundCursorBlockedColor, CursorColor(), "막힌 칸 색");

            // 돌아서면 칸이 바뀜
            Vector2Int before = cursor.Cell;
            Teleport(player.position, Vector3.right);
            yield return Frames(2);
            Assert.AreNotEqual(before, cursor.Cell);
        }
    }
}
