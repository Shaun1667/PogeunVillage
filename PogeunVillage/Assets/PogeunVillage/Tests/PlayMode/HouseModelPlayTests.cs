using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// P-3b 확인: 주민이 사는 집터에는 그 주민의 집 모델(VillagerDefinition.houseModel)이 서고 그레이박스 집은 렌더러만 꺼짐(충돌 그대로),
    /// 주민이 바뀌면 모델도 바뀜, 빈 집터 · 캠핑 구역(텐트)에는 집 모델이 없음.
    /// </summary>
    public class HouseModelPlayTests : ToolSceneTest
    {
        protected override bool WithVillagers => true;

        HousePlot[] Plots => Object.FindObjectsByType<HousePlot>().Where(p => p.showsResidentHouse).ToArray();

        [Test]
        public void ResidentPlots_ShowTheirHouseModel()
        {
            var lived = Plots.Where(p => p.Resident).ToArray();
            Assert.IsNotEmpty(lived);
            foreach (var plot in lived)
            {
                Assert.IsNotNull(plot.Model, plot.name);
                StringAssert.StartsWith(plot.Resident.houseModel.name, plot.Model.name, "그 주민의 집");
                Assert.IsTrue(plot.greyboxRenderers.All(r => !r.enabled), "그레이박스는 숨김");
                Assert.IsTrue(plot.house.GetComponentInChildren<Collider>(), "충돌은 그대로");
                Assert.IsTrue(plot.Model.GetComponentsInChildren<MeshRenderer>().All(r => r.sharedMaterial.shader.name == "PogeunVillage/CurvedToon"));
            }
        }

        [Test]
        public void ChangingResident_SwapsModel_EmptyHidesHouse()
        {
            var plot = Plots.First(p => p.Resident);
            var other = Resources.FindObjectsOfTypeAll<VillagerDefinition>().First(v => v != plot.Resident && v.houseModel);
            plot.Show(other);
            StringAssert.StartsWith(other.houseModel.name, plot.Model.name);
            Assert.AreEqual(1, plot.house.transform.Cast<Transform>().Count(t => t.name.StartsWith("House") && t.gameObject.activeSelf), "모델은 하나만");
            plot.Show(null);
            Assert.IsFalse(plot.house.activeSelf);
            Assert.IsTrue(plot.emptySign.activeSelf);
        }

        // P-3c: 주민은 자기 모델(VillagerDefinition.model)로 보이고 그레이박스 몸은 숨김, 충돌(CharacterController)은 그대로, 종족이 채워짐
        [Test]
        public void Villagers_ShowTheirModel()
        {
            var villagers = Object.FindObjectsByType<VillagerBehaviour>();
            Assert.IsNotEmpty(villagers);
            foreach (var v in villagers)
            {
                Assert.IsNotNull(v.Model, v.name);
                StringAssert.StartsWith(v.Definition.model.name, v.Model.name);
                Assert.IsTrue(v.Model.transform.IsChildOf(v.visual), "통통 튐 · 구멍 빠짐을 따라감");
                Assert.IsTrue(v.greyboxRenderers.All(r => !r.enabled), "그레이박스 몸 숨김");
                Assert.IsTrue(v.GetComponent<CharacterController>().enabled || !v.gameObject.activeInHierarchy);
                Assert.AreNotEqual("미정", v.Definition.species, "종족");
            }
        }

        // P-4j: 주민 모델은 Mixamo 리깅 — 애니메이터(자기 동작) · CurvedToon, 걸으면 Speed가 오르고 멈추면 0으로
        [UnityTest]
        public IEnumerator Villagers_Rigged_WalkAnimates()
        {
            var villagers = Object.FindObjectsByType<VillagerBehaviour>();
            foreach (var v in villagers)
            {
                var animator = v.Model.GetComponentInChildren<Animator>();
                Assert.IsTrue(animator && animator.runtimeAnimatorController, v.name);
                StringAssert.StartsWith(v.Definition.model.name, animator.runtimeAnimatorController.name, "자기 동작");
                var skin = v.Model.GetComponentInChildren<SkinnedMeshRenderer>();
                Assert.AreEqual("PogeunVillage/CurvedToon", skin.sharedMaterial.shader.name);
                Assert.IsTrue(skin.updateWhenOffscreen, "휜 화면에서 잘리지 않게");
            }

            float Speed(VillagerBehaviour v) => v.Model.GetComponentInChildren<Animator>().GetFloat("Speed");
            bool walked = false, rested = false;
            for (float until = Time.time + 20f; Time.time < until && !(walked && rested);)
            {
                yield return null;
                foreach (var v in villagers.Where(v => v.isActiveAndEnabled))
                {
                    walked |= v.Current == VillagerBehaviour.State.Walking && Speed(v) > 0.5f;
                    rested |= v.Current == VillagerBehaviour.State.Pausing && Speed(v) < 0.05f;
                }
            }
            Assert.IsTrue(walked, "걷는 주민은 걷기 동작");
            Assert.IsTrue(rested, "멈춘 주민은 서 있기 동작");
        }

        [Test]
        public void CampSite_KeepsTent()
        {
            var camp = Object.FindObjectsByType<HousePlot>().Single(p => !p.showsResidentHouse);
            Assert.IsNull(camp.Model, "캠핑은 집 모델 없음");
        }
    }
}
