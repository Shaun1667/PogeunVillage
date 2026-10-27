using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>1-8 삽 확인 (구멍 파기 · 메우기 · 막힌 칸 · 꽃 통째 회수 · 구멍에 F로 심기 · 밑동 파내기 · 구멍에 빠지기 · 도구 상자 묘목).</summary>
    public class ShovelPlayTests : ToolSceneTest
    {
        [UnityTest]
        public IEnumerator DigHole_ThenFillIt()
        {
            yield return Hold(Shovel);
            var spot = FindOpenSpot();
            var front = spot + Vector2Int.up;
            yield return StandFacingNorth(spot);

            yield return Swing();
            Assert.AreEqual(GroundObjectKind.Hole, Grid.Get(front), "구멍");
            var hole = ground.ObjectAt(front);
            Assert.IsNotNull(hole);
            Assert.Less(Vector2.Distance(Flat(hole.transform.position), Grid.CellCenter(front)), 0.01f, "칸 가운데");
            Assert.AreEqual(tools.tools[Shovel].durability - 1, Durability);

            yield return Swing();
            Assert.AreEqual(GroundObjectKind.None, Grid.Get(front), "메움");
            Assert.IsTrue(hole == null);
            Assert.AreEqual(tools.tools[Shovel].durability - 2, Durability);
        }

        [UnityTest]
        public IEnumerator BlockedCell_DoesNothing()
        {
            yield return Hold(Shovel);
            Teleport(Plaza + Vector3.back * 7.5f, Vector3.forward);
            yield return Frames(3);
            var front = Grid.FrontCell(Flat(player.position), Flat(player.forward), settings.groundCursorReach);
            Assert.IsTrue(Grid.IsBlocked(front));
            yield return Swing();
            Assert.AreEqual(GroundObjectKind.None, Grid.Get(front));
            Assert.AreEqual(tools.tools[Shovel].durability, Durability, "막힌 칸은 맞음이 아님");
        }

        [UnityTest]
        public IEnumerator Flower_LiftedAsClump()
        {
            yield return Hold(Shovel);
            var flower = Object.FindObjectsByType<GridObject>().Where(g => g.kind == GroundObjectKind.Flower)
                .OrderBy(g => Vector3.Distance(g.transform.position, Plaza)).First();
            Vector2Int cell = flower.Cell;
            Teleport(flower.transform.position + Vector3.back * 1.0f, Vector3.forward);
            yield return Frames(3);
            Assert.AreEqual(cell, Grid.FrontCell(Flat(player.position), Flat(player.forward), settings.groundCursorReach), "꽃 칸이 앞 칸");

            yield return Swing();
            Assert.AreEqual(1, Inv.Count("FlowerClump"), "꽃 포기 1");
            Assert.AreEqual(0, Inv.Count("Flower"), "꺾은 꽃이 아님");
            Assert.IsTrue(flower == null);
            Assert.AreEqual(GroundObjectKind.None, Grid.Get(cell));
        }

        [UnityTest]
        public IEnumerator FOnHole_PlantsFirstPlantableInInventory()
        {
            Vector2Int hole = default;
            yield return DigHoleAhead(c => hole = c);
            Assert.IsTrue(playerInventory.TryAdd(Item("Sapling")));
            Assert.IsTrue(playerInventory.TryAdd(Item("FlowerClump")));

            yield return Tap(keyboard.fKey);
            yield return Frames(2);
            Assert.AreEqual(GroundObjectKind.Sapling, Grid.Get(hole), "앞쪽 칸의 묘목을 심음");
            Assert.AreEqual(0, Inv.Count("Sapling"));
            Assert.AreEqual(1, Inv.Count("FlowerClump"), "꽃 포기는 그대로");
            Assert.IsNotNull(ground.ObjectAt(hole));
        }

        [UnityTest]
        public IEnumerator PlantedClump_BecomesPickableFlower()
        {
            Vector2Int hole = default;
            yield return DigHoleAhead(c => hole = c);
            Assert.IsTrue(playerInventory.TryAdd(Item("FlowerClump")));

            yield return Tap(keyboard.fKey);
            yield return Frames(2);
            Assert.AreEqual(GroundObjectKind.Flower, Grid.Get(hole));
            Assert.AreEqual(0, Inv.Count("FlowerClump"));

            yield return Tap(keyboard.fKey);  // 심은 꽃 꺾기
            yield return Frames(2);
            Assert.AreEqual(1, Inv.Count("Flower"));
            Assert.AreEqual(GroundObjectKind.None, Grid.Get(hole));
        }

        [UnityTest]
        public IEnumerator FOnHole_NothingToPlant_HoleStays()
        {
            Vector2Int hole = default;
            yield return DigHoleAhead(c => hole = c);
            yield return Tap(keyboard.fKey);
            yield return Frames(2);
            Assert.AreEqual(GroundObjectKind.Hole, Grid.Get(hole));
        }

        [UnityTest]
        public IEnumerator Stump_DugOut_CellBecomesPlantable()
        {
            var tree = Object.FindObjectsByType<ChoppableTreeBehaviour>()
                .Where(t => !t.GetComponent<FruitTreeBehaviour>())
                .OrderBy(t => Vector3.Distance(t.transform.position, Plaza)).First();
            Vector2Int cell = ground.CellAt(tree.transform.position);
            Assert.IsTrue(Grid.IsBlocked(cell), "나무 칸은 막혀 있음");
            Teleport(tree.transform.position + Vector3.back * 0.9f, Vector3.forward);
            yield return Frames(3);

            yield return Hold(Axe);
            for (int i = 0; i < tree.rules.axeHitsToFell; i++)
                yield return Swing();
            yield return Frames(2);
            var stump = interactor.Target;
            Assert.AreEqual(InteractableKind.Stump, stump ? stump.kind : InteractableKind.None);

            yield return Hold(Shovel);
            yield return Swing();
            Assert.IsTrue(stump == null, "밑동이 사라짐");
            Assert.IsFalse(Grid.IsBlocked(cell), "그 칸은 빈 땅");
            Assert.AreEqual(tools.tools[Shovel].durability - 1, Durability);
        }

        [UnityTest]
        public IEnumerator Player_FallsIntoHole_StuckThenOut_HoleStays()
        {
            Vector2Int hole = default;
            yield return DigHoleAhead(c => hole = c);
            var fall = player.GetComponent<PlayerHoleFall>();

            Press(keyboard.wKey);
            float t0 = Time.time;
            while (!fall.IsStuck && Time.time - t0 < 1.5f)
                yield return null;
            Assert.IsTrue(fall.IsStuck, "구멍에 빠짐");
            Assert.AreEqual(hole, ground.CellAt(player.position));

            Vector2 stuckAt = Flat(player.position);
            yield return WaitSeconds(settings.holeStuckTime * 0.8f);
            Assert.IsTrue(fall.IsStuck);
            Assert.Less(Vector2.Distance(stuckAt, Flat(player.position)), 0.01f, "갇혀 있는 동안 못 움직임");

            yield return WaitSeconds(settings.holeStuckTime * 0.4f + 0.3f);
            Release(keyboard.wKey);
            Assert.IsFalse(fall.IsStuck, "나옴");
            Assert.Greater(Vector2.Distance(stuckAt, Flat(player.position)), 0.3f, "다시 걸어감");
            Assert.AreEqual(GroundObjectKind.Hole, Grid.Get(hole), "구멍은 그대로");
        }
    }
}
