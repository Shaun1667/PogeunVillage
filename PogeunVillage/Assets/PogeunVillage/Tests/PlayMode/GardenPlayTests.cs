using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>1-9 물뿌리개 · 묘목 · 꽃 확인 (F8 아침 · 물 주기 · 묘목 3단계 · 나무가 됨 · 꽃 번식 · 시듦 · 사라짐 · 열매 채움 · 도끼 횟수 초기화).</summary>
    public class GardenPlayTests : ToolSceneTest
    {
        DayCycle dayCycle;
        GardenKeeper keeper;
        GatheringRules rules;

        [UnitySetUp]
        public IEnumerator FindGarden()
        {
            dayCycle = Object.FindAnyObjectByType<DayCycle>();
            keeper = Object.FindAnyObjectByType<GardenKeeper>();
            rules = keeper.rules;
            Assert.IsNotNull(dayCycle);
            yield break;
        }

        IEnumerator NextMorning()
        {
            yield return Tap(keyboard.f8Key);
            yield return Frames(2);
        }

        IEnumerator PlantAhead(string itemId, System.Action<Vector2Int> report)
        {
            Vector2Int hole = default;
            yield return DigHoleAhead(c => hole = c);
            Assert.IsTrue(playerInventory.TryAdd(Item(itemId)));
            yield return Tap(keyboard.fKey);
            yield return Frames(2);
            report(hole);
        }

        static bool StageActive(GridObject sapling, int stage) => sapling.transform.Find("Stage" + stage).gameObject.activeSelf;

        [UnityTest]
        public IEnumerator F8_RunsMorning()
        {
            int day = dayCycle.Day;
            yield return NextMorning();
            Assert.AreEqual(day + 1, dayCycle.Day);
        }

        [UnityTest]
        public IEnumerator WateringCan_OnlyHitsPlants()
        {
            Vector2Int cell = default;
            yield return PlantAhead("Sapling", c => cell = c);
            yield return Hold(WateringCan);
            yield return Swing();
            Assert.IsTrue(keeper.Garden.IsWatered(cell), "묘목에 물");
            Assert.AreEqual(tools.tools[WateringCan].durability - 1, Durability);

            Teleport(player.position, Vector3.right);  // 옆은 빈 땅
            yield return Frames(2);
            yield return Swing();
            Assert.AreEqual(tools.tools[WateringCan].durability - 1, Durability, "빈 땅은 허공");
        }

        [UnityTest]
        public IEnumerator Sapling_WateredDaily_GrowsThroughStages_IntoTree()  // 8-2
        {
            Vector2Int cell = default;
            yield return PlantAhead("Sapling", c => cell = c);
            Assert.AreEqual(GroundObjectKind.Sapling, Grid.Get(cell));
            var sapling = ground.ObjectAt(cell);
            Assert.IsTrue(StageActive(sapling, 0));
            yield return Hold(WateringCan);

            yield return Swing();
            yield return NextMorning();  // 성장 2 → 단계 1
            Assert.IsTrue(StageActive(sapling, 1), "어린 나무");
            yield return Swing();
            yield return NextMorning();  // 4 → 단계 2
            Assert.IsTrue(StageActive(sapling, 2), "큰 나무");
            yield return Swing();
            yield return NextMorning();  // 6 → 다 자람

            Assert.IsTrue(sapling == null, "묘목은 사라짐");
            Assert.IsTrue(Grid.IsBlocked(cell), "나무 칸은 막힘");
            var tree = Object.FindObjectsByType<ChoppableTreeBehaviour>()
                .FirstOrDefault(t => ground.CellAt(t.transform.position) == cell);
            Assert.IsNotNull(tree, "벨 수 있는 나무가 섬");
            Assert.IsNull(tree.GetComponent<FruitTreeBehaviour>(), "열매 없는 나무");
        }

        [UnityTest]
        public IEnumerator Sapling_NoWater_NeedsSixMornings()  // 8-1
        {
            Vector2Int cell = default;
            yield return PlantAhead("Sapling", c => cell = c);
            for (int day = 1; day < rules.saplingGrowDays; day++)
                yield return NextMorning();
            Assert.AreEqual(GroundObjectKind.Sapling, Grid.Get(cell), "5번째 아침까지는 묘목");
            yield return NextMorning();
            Assert.IsTrue(Grid.IsBlocked(cell), "6번째 아침에 나무");
        }

        [UnityTest]
        public IEnumerator WateredFlower_SpreadsIntoCrossCell_SameColor()
        {
            Vector2Int cell = default;
            yield return PlantAhead("FlowerClump", c => cell = c);
            Assert.AreEqual(GroundObjectKind.Flower, Grid.Get(cell));
            var source = ground.ObjectAt(cell).transform.Find("Petals").GetComponent<Renderer>().sharedMaterial;
            yield return Hold(WateringCan);
            yield return Swing();
            yield return NextMorning();

            var cross = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left }.Select(d => cell + d).ToArray();
            var spread = cross.Where(c => Grid.Get(c) == GroundObjectKind.Flower).ToArray();
            Assert.AreEqual(1, spread.Length, "상하좌우 중 한 칸에 번짐");
            Assert.AreSame(source, ground.ObjectAt(spread[0]).transform.Find("Petals").GetComponent<Renderer>().sharedMaterial, "같은 꽃 색");
        }

        static int FlowerCount() => Object.FindObjectsByType<GridObject>().Count(g => g.kind == GroundObjectKind.Flower);

        [UnityTest]
        public IEnumerator UnwateredFlowers_Wilt_ThenDisappear_WaterRevives()  // 8-6, 8-7, 8-8
        {
            int before = FlowerCount();
            yield return NextMorning();
            var wilted = Object.FindObjectsByType<GridObject>().Where(g => g.kind == GroundObjectKind.Flower && keeper.Garden.IsWilted(g.Cell)).ToList();
            Assert.Greater(wilted.Count, 0, "10%면 200송이 넘게 중 몇 송이는 시듦");
            Assert.Less(wilted.Count, before / 4, "대부분은 그대로");
            Assert.AreEqual(before, FlowerCount(), "시든 첫날은 안 사라짐");

            var check = wilted[0];
            Assert.Less(check.transform.localScale.y, 1f, "시든 모습(낮아짐)");

            // 한 송이만 물을 주고 다음 아침
            var saved = wilted[1];
            Teleport(saved.transform.position + Vector3.back * 1.0f, Vector3.forward);
            yield return Hold(WateringCan);
            yield return Frames(2);
            Assert.AreEqual(saved.Cell, Grid.FrontCell(Flat(player.position), Flat(player.forward), settings.groundCursorReach));
            yield return Swing();
            yield return NextMorning();

            Assert.IsTrue(check == null, "물 못 받은 시든 꽃은 사라짐");
            Assert.IsTrue(saved != null, "물 받은 시든 꽃은 살아남");
            Assert.IsFalse(keeper.Garden.IsWilted(saved.Cell));
            Assert.AreEqual(1f, saved.transform.localScale.y, 1e-4f, "성한 모습으로");
        }

        [UnityTest]
        public IEnumerator Morning_RefillsFruit_AndResetsAxeHits()
        {
            var fruitTree = Object.FindObjectsByType<FruitTreeBehaviour>().OrderBy(t => Vector3.Distance(t.transform.position, Plaza)).First();
            Teleport(fruitTree.transform.position + Vector3.back * 0.9f, Vector3.forward);
            yield return Frames(3);

            // 먼저 도끼(흔들어 떨어진 열매가 나무보다 가까우면 열매가 대상이 되므로)
            var choppable = fruitTree.GetComponent<ChoppableTreeBehaviour>();
            yield return Hold(Axe);
            for (int i = 0; i < 3; i++)
                yield return Swing();
            Assert.AreEqual(3, choppable.Hits);

            yield return Tap(keyboard.fKey);  // 흔들기
            Assert.AreEqual(0, fruitTree.FruitCount);

            yield return NextMorning();
            Assert.AreEqual(rules.fruitMaxOnTree, fruitTree.FruitCount, "열매 다시 3개");
            Assert.AreEqual(0, choppable.Hits, "치다 만 횟수 초기화");
        }
    }
}
