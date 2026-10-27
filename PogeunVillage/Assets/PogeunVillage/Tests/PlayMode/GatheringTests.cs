using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>1-3 채집 확인 (F로 나무 흔들기 · 떨어진 열매 줍기 · 꽃 꺾기 · 임시 주머니).</summary>
    public class GatheringTests : VillageSceneTest
    {
        static readonly Vector3 Plaza = new(-6f, 0f, 2f);

        PlayerInteractor interactor;
        Pouch pouch;

        [UnitySetUp]
        public IEnumerator FindParts()
        {
            interactor = player.GetComponent<PlayerInteractor>();
            pouch = player.GetComponent<PlayerPouch>().Pouch;
            yield break;
        }

        IEnumerator TapF()
        {
            Press(keyboard.fKey);
            yield return null;
            Release(keyboard.fKey);
            yield return null;
        }

        FruitTreeBehaviour NearestFruitTree() => Object.FindObjectsByType<FruitTreeBehaviour>()
            .OrderBy(t => Vector3.Distance(t.transform.position, Plaza)).First();

        Pickup[] DroppedNear(FruitTreeBehaviour tree) => Object.FindObjectsByType<Pickup>()
            .Where(p => p.item == tree.fruitItem && Vector2.Distance(Flat(p.transform.position), Flat(tree.transform.position)) < 2f)
            .ToArray();

        IEnumerator ShakeNearestTree(System.Action<FruitTreeBehaviour> report)
        {
            var tree = NearestFruitTree();
            Teleport(tree.transform.position + Vector3.back * 0.9f, Vector3.forward);
            yield return Frames(3);
            Assert.AreSame(tree.GetComponent<Interactable>(), interactor.Target, "열매 나무가 대상이어야 합니다.");
            yield return TapF();
            yield return WaitSeconds(settings.fruitFallTime + 0.1f);
            report(tree);
        }

        [UnityTest]
        public IEnumerator FKey_ShakesFruitTree_DropsAllFruit()
        {
            FruitTreeBehaviour tree = null;
            yield return ShakeNearestTree(t => tree = t);

            Assert.AreEqual(0, tree.FruitCount, "나무에 남은 열매");
            Assert.IsTrue(tree.fruitVisuals.All(v => !v.gameObject.activeSelf), "달린 열매 모습이 사라져야 합니다.");
            var dropped = DroppedNear(tree);
            Assert.AreEqual(3, dropped.Length, "떨어진 열매 수");
            foreach (var p in dropped)
            {
                float d = Vector2.Distance(Flat(p.transform.position), Flat(tree.transform.position));
                Assert.AreEqual(settings.fruitDropRadius, d, 0.05f, "떨어진 거리");
                Assert.Less(Mathf.Abs(p.transform.position.y - GroundHeight(p.transform.position)), 0.05f, "땅에 닿아 있어야 합니다.");
            }

            yield return TapF();
            yield return WaitSeconds(settings.fruitFallTime + 0.1f);
            Assert.AreEqual(3, DroppedNear(tree).Length, "같은 날 다시 흔들면 더 떨어지지 않음");
        }

        [UnityTest]
        public IEnumerator FKey_PicksDroppedFruit_IntoPouch()
        {
            FruitTreeBehaviour tree = null;
            yield return ShakeNearestTree(t => tree = t);
            string id = tree.fruitItem.id;
            var fruit = DroppedNear(tree).OrderBy(p => Vector3.Distance(p.transform.position, player.position)).First();

            Teleport(player.position, Vector3.ProjectOnPlane(fruit.transform.position - player.position, Vector3.up));
            yield return Frames(3);
            var fruitTarget = fruit.GetComponent<Interactable>();
            Assert.AreSame(fruitTarget, interactor.Target, "떨어진 열매가 대상이어야 합니다.");

            yield return TapF();
            yield return Frames(2);
            Assert.AreEqual(1, pouch.Get(id), "주머니에 1개");
            Assert.IsTrue(fruit == null, "주운 열매는 사라져야 합니다.");
            Assert.AreEqual(2, DroppedNear(tree).Length);
            Assert.IsFalse(ReferenceEquals(fruitTarget, interactor.Target), "주워 사라진 열매가 대상으로 남으면 안 됩니다.");
        }

        [UnityTest]
        public IEnumerator FKey_PicksFlower_IntoPouch()
        {
            var flowers = Object.FindObjectsByType<Pickup>().Where(p => p.item && p.item.id == "Flower").ToArray();
            var flower = flowers.OrderBy(p => Vector3.Distance(p.transform.position, Plaza)).First();
            Teleport(flower.transform.position + Vector3.back * 0.7f, Vector3.forward);
            yield return Frames(3);
            // 꽃은 한 자리에 여러 송이라, 옆의 더 가까운 꽃이 대상일 수 있음 → 대상이 된 꽃을 꺾음
            Assert.IsNotNull(interactor.Target, "대상이 있어야 합니다.");
            flower = interactor.Target.GetComponent<Pickup>();
            Assert.IsNotNull(flower, "대상이 줍는 물건이어야 합니다.");
            Assert.AreEqual("Flower", flower.item.id, "꽃이 대상이어야 합니다.");

            yield return TapF();
            yield return Frames(2);
            Assert.AreEqual(1, pouch.Get("Flower"));
            Assert.IsTrue(flower == null, "꺾은 꽃은 사라져야 합니다.");
            Assert.AreEqual(flowers.Length - 1, Object.FindObjectsByType<Pickup>().Count(p => p.item && p.item.id == "Flower"));
        }
    }
}
