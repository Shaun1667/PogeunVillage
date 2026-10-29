using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace PogeunVillage.Tests
{
    // 6-1b 마을 상태 되살리기 (순수 규칙 쪽)
    public class VillageRestoreTests
    {
        [Test]
        public void Residents_Restore_EmptyStringIsEmptyPlot()
        {
            var r = new VillageResidents(3);
            var changed = new List<int>();
            r.PlotChanged += changed.Add;
            r.Restore(new[] { "A", "", "C" });
            CollectionAssert.AreEqual(new[] { "A", null, "C" }, r.Plots);
            CollectionAssert.AreEqual(new[] { 0, 2 }, changed, "바뀐 집터만 알림");
        }

        [Test]
        public void Camp_Departures_Request_Theme_Restore()
        {
            var camp = new CampStay("V", 1, 2, 5, 5);
            camp.Restore(4, false);
            Assert.AreEqual(4, camp.Value);
            Assert.IsFalse(camp.Confirmed);
            camp.Gift();
            Assert.IsTrue(camp.Confirmed, "되살린 값에서 이어감");

            var d = new Departures(-20);
            d.Restore(new[] { "B", "A" });
            CollectionAssert.AreEqual(new[] { "A", "B" }, d.Preparing);

            var req = VillagerRequest.Restore("A", "Apple", 2, true);
            Assert.AreEqual(("A", "Apple", 2, true), (req.VillagerId, req.ItemId, req.Count, req.Done));

            var theme = new ThemeRewards(new[] { 10, 25 });
            theme.Restore("A", 1);
            Assert.AreEqual(1, theme.Due("A", 30), "받은 1개 다음은 벤치");
        }

        [Test]
        public void Garden_Restore_WiltedAndGrowth()
        {
            var grid = new GroundGrid(1f, Vector2Int.zero, 4, 4, new bool[16]);
            grid.TryPlace(new Vector2Int(0, 0), GroundObjectKind.Flower);
            grid.TryPlace(new Vector2Int(1, 0), GroundObjectKind.Sapling);
            var garden = new Garden(grid, new SeededRandom(1), 6, 1, 0.1);
            garden.Restore(new[] { new Vector2Int(0, 0) }, new[] { new KeyValuePair<Vector2Int, int>(new Vector2Int(1, 0), 4) });
            Assert.IsTrue(garden.IsWilted(new Vector2Int(0, 0)));
            Assert.AreEqual(4, garden.Growth(new Vector2Int(1, 0)));
            Assert.IsFalse(garden.IsWatered(new Vector2Int(0, 0)), "아침 기준이라 물 준 칸 없음");
        }

        [Test]
        public void Normalize_SortsAndRounds()
        {
            var d = new SaveData();
            d.ground.flowers.Add(new CellData { x = 5, y = 1 });
            d.ground.flowers.Add(new CellData { x = 2, y = 9 });
            d.ground.stumps.Add(new PointData { x = 1.23456f, z = 2f, yaw = -0.04f });
            d.village.friendship.Add(new CountData { id = "B", count = 1 });
            d.village.friendship.Add(new CountData { id = "A", count = 2 });
            SaveCodec.Normalize(d);
            CollectionAssert.AreEqual(new[] { 2, 5 }, d.ground.flowers.Select(f => f.x));
            Assert.AreEqual(1.235f, d.ground.stumps[0].x, 1e-6f);
            Assert.AreEqual(0f, d.ground.stumps[0].yaw, 1e-6f, "-0.04도 → 0");
            CollectionAssert.AreEqual(new[] { "A", "B" }, d.village.friendship.Select(f => f.id));
        }
    }
}
