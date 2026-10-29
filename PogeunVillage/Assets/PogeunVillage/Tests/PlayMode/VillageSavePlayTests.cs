using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// 6-1b 확인 (마을을 바꾸고 자면 저장 → 다시 켜면 같은 마을: 저장 내용이 그대로 다시 나옴(왕복) ·
    /// 주민 친밀도 · 이사 준비 · 캠핑 값 · 테마 보상 · 꽃 없앰 · 구멍 · 묘목 · 베어진 나무 → 밑동 · 밑동 치운 자리 · 가구 · 수조 · 떨어진 물건).
    /// </summary>
    public class VillageSavePlayTests : ToolSceneTest
    {
        SaveManager save;

        [UnitySetUp]
        public IEnumerator FindSave()
        {
            save = Object.FindAnyObjectByType<SaveManager>();
            yield break;
        }

        IEnumerator Reopen()
        {
            SceneManager.LoadScene("VillagePrototype");
            yield return null;
            yield return null;
            save = Object.FindAnyObjectByType<SaveManager>();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ChangedVillage_SaveReopen_SameVillage()
        {
            var village = Object.FindAnyObjectByType<Village>(FindObjectsInactive.Include);
            var keeper = Object.FindAnyObjectByType<FurnitureKeeper>();
            var saver = save.groundSaver;

            // 마을을 바꿈
            string friend = village.Residents.Plots.First(id => id != null);
            string leaver = village.Residents.Plots.Last(id => id != null);
            village.Friendship.Begin(friend, 30);
            village.Friendship.Begin(leaver, -20);                // 다음 아침 이사 준비
            village.ThemeRewards.MarkGiven(friend);
            village.CampStay?.Restore(3, false);

            var flower = ground.Objects().First(o => o.kind == GroundObjectKind.Flower);
            Vector2Int flowerCell = flower.Cell;
            flower.Unregister();
            Object.Destroy(flower.gameObject);                    // 꽃 하나 없앰
            var spot = FindOpenSpot();
            Assert.IsNotNull(ground.Placer.Spawn(GroundObjectKind.Hole, spot + new Vector2Int(-1, 0)));
            Assert.IsNotNull(ground.Placer.Spawn(GroundObjectKind.Sapling, spot + new Vector2Int(1, 0)));
            Assert.IsNotNull(keeper.Place((FurnitureDefinition)Item("Bench"), spot + Vector2Int.up, 1));
            Assert.IsNotNull(keeper.Place(((FishDefinition)Item("Catfish")).aquarium, spot + new Vector2Int(1, 2), 2));

            var trees = Object.FindObjectsByType<ChoppableTreeBehaviour>().Where(t => !t.transform.IsChildOf(ground.transform)).Take(2).ToArray();
            foreach (var tree in trees)
            {
                var stump = Object.Instantiate(saver.stumpPrefab, tree.transform.position, tree.transform.rotation);
                stump.name = "Stump";
                Object.Destroy(tree.gameObject);                  // 베어짐 → 밑동
                if (tree == trees[1])
                {
                    ground.Grid.Unblock(ground.CellAt(tree.transform.position));
                    Object.Destroy(stump);                        // 두 번째는 삽으로 밑동까지 치움
                }
            }
            var apple = Object.Instantiate(saver.pickupPrefab, player.position + Vector3.right * 2f, Quaternion.identity);
            apple.item = Item("Apple");
            yield return null;

            yield return Tap(keyboard.f8Key);                     // 자고 일어나 저장
            yield return null;
            var before = save.Capture();
            string json = SaveCodec.ToJson(before);
            Assert.IsTrue(before.ground.felledTrees.Count >= 2);
            Assert.AreEqual(before.ground.felledTrees.Count - 1, before.ground.stumps.Count(s => before.ground.felledTrees.Any(f => Mathf.Abs(f.x - s.x) < 0.01f && Mathf.Abs(f.z - s.z) < 0.01f)), "밑동은 하나만");
            Assert.IsTrue(before.ground.holes.Any());
            Assert.IsTrue(before.ground.saplings.Any());
            Assert.AreEqual(2, before.furniture.Count);
            Assert.AreEqual(1, before.ground.drops.Count(d => d.id == "Apple"));
            CollectionAssert.Contains(before.village.preparing, leaver);
            Assert.AreEqual(1, before.village.themeGiven.Single(t => t.id == friend).count);

            yield return Reopen();
            Assert.IsTrue(save.Loaded);
            Assert.AreEqual(json, SaveCodec.ToJson(save.Capture()), "다시 켜도 같은 마을(저장 내용 그대로)");

            var village2 = Object.FindAnyObjectByType<Village>();
            Assert.GreaterOrEqual(village2.Friendship.Get(friend), 30);
            Assert.IsTrue(village2.Departures.IsPreparing(leaver));
            Assert.AreEqual(2, Object.FindAnyObjectByType<FurnitureKeeper>().Placed.Count);
            var ground2 = Object.FindAnyObjectByType<VillageGround>();
            bool flowerSaved = before.ground.flowers.Any(f => f.x == flowerCell.x && f.y == flowerCell.y);
            Assert.AreEqual(flowerSaved, ground2.Grid.Get(flowerCell) == GroundObjectKind.Flower, "없앤 꽃 자리는 저장대로");
            Assert.AreEqual(before.ground.stumps.Count, Object.FindObjectsByType<Interactable>().Count(i => i.kind == InteractableKind.Stump));
        }
    }
}
