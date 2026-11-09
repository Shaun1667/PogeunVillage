using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// P-5f 확인: 인트로의 마을 화면 · 모니터 속에서 주민 한 명이 착지 자리 옆에 서서 카메라에 손을 흔들고, 플레이어가 떨어지기 시작하면 다시 돌아다님.
    /// 건너뛰어도 멈춤. 팔은 주민 체형과 무관하게 어깨부터 들어 올림.
    /// </summary>
    public class IntroWavePlayTests : ToolSceneTest
    {
        protected override bool WithIntro => true;
        protected override bool WithVillagers => true;

        IntroSequence intro;

        [UnitySetUp]
        public IEnumerator FindIntro()
        {
            intro = Object.FindAnyObjectByType<IntroSequence>();
            Assert.IsNotNull(intro);
            yield return null;
            yield return null;
        }

        // 오른손이 오른쪽 어깨보다 위로 올라감
        static float HandAboveShoulder(VillagerBehaviour villager)
        {
            var bones = villager.Model.GetComponentsInChildren<Transform>();
            return bones.First(t => t.name == "mixamorig:RightHand").position.y - bones.First(t => t.name == "mixamorig:RightArm").position.y;
        }

        [UnityTest]
        public IEnumerator Resident_WavesAtCamera_UntilPlayerFalls()
        {
            var waver = intro.Waver;
            Assert.IsNotNull(waver, "손 흔드는 주민");
            Assert.IsTrue(waver.Definition.canWave, "토끼는 안 뽑힘");
            Assert.AreEqual(VillagerBehaviour.State.Waving, waver.Current);
            yield return new WaitForSeconds(0.4f);
            float raised = HandAboveShoulder(waver);
            Assert.Greater(raised, 0.05f, "손을 어깨 위로 듦");

            Vector3 expected = intro.player.position + intro.settings.waverOffset;
            Assert.Less(Vector2.Distance(new Vector2(expected.x, expected.z), new Vector2(waver.transform.position.x, waver.transform.position.z)), 0.1f, "착지 자리 옆");
            Vector3 toCamera = Vector3.ProjectOnPlane(intro.mainCamera.transform.position - waver.transform.position, Vector3.up).normalized;
            Assert.Greater(Vector3.Dot(waver.transform.forward, toCamera), 0.95f, "카메라를 봄");

            intro.JumpTo(intro.StartOf(IntroPhase.Click) + 0.01f);
            Assert.AreSame(waver, intro.Waver, "모니터 속에서도 계속");
            Assert.AreEqual(VillagerBehaviour.State.Waving, waver.Current);

            intro.JumpTo(intro.StartOf(IntroPhase.Fall) + 0.01f);
            Assert.IsNull(intro.Waver);
            Assert.AreEqual(VillagerBehaviour.State.Pausing, waver.Current, "다시 돌아다님");
            yield return new WaitForSeconds(0.4f);
            Assert.Less(HandAboveShoulder(waver), raised - 0.05f, "팔을 내림");
        }

        [UnityTest]
        public IEnumerator Skip_StopsWaving()
        {
            var waver = intro.Waver;
            Assert.IsNotNull(waver);
            intro.JumpTo(intro.Total + 0.01f);
            yield return null;
            Assert.IsFalse(intro.IsPlaying);
            Assert.AreNotEqual(VillagerBehaviour.State.Waving, waver.Current);
        }

        [Test]
        public void EveryVillager_HasArmBones()
        {
            foreach (var villager in Object.FindObjectsByType<VillagerBehaviour>())
            {
                var bones = villager.Model.GetComponentsInChildren<Transform>();
                Assert.IsTrue(bones.Any(t => t.name == "mixamorig:RightArm") && bones.Any(t => t.name == "mixamorig:RightForeArm") && bones.Any(t => t.name == "mixamorig:RightHand"), villager.name);
            }
        }
    }
}
