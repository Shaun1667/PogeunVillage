using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// P-3 확인(MODELING_SPEC.md): 씬의 Varco3D 모델은 모두 CurvedToon + 색 텍스처(핵심 설계 규칙 1 — 휨), FBX 기본 재질(URP Lit)은 없음,
    /// 놓인 모델 높이는 ModelLibrary 높이(부모 크기 반영), 충돌은 그대로(나무 줄기 · 바위 · 분수대 · 다리 판).
    /// </summary>
    public class ModelPlayTests : ToolSceneTest
    {
        [Test]
        public void AllRenderers_UseCurvedShader()
        {
            var wrong = Object.FindObjectsByType<MeshRenderer>()
                .Where(r => r.sharedMaterials.Any(m => !m || !m.shader.name.StartsWith("PogeunVillage/")))
                .Select(r => r.name).Distinct().ToArray();
            Assert.IsEmpty(wrong, "CurvedToon이 아닌 재질: " + string.Join(", ", wrong));
        }

        [Test]
        public void PlacedModels_TexturedAndSized()
        {
#if UNITY_EDITOR
            var library = UnityEditor.AssetDatabase.LoadAssetAtPath<ModelLibrary>("Assets/PogeunVillage/Data/ModelLibrary.asset");
#else
            ModelLibrary library = null;
            Assert.Ignore("에디터에서만");
#endif
            Assert.IsNotNull(library, "모델 목록이 로드되지 않음");
            int checkedCount = 0;
            foreach (var entry in library.entries)
            {
                foreach (var holder in Object.FindObjectsByType<Transform>().Where(t => t.name == "Model_" + entry.id).Take(3))
                {
                    var renderers = holder.GetComponentsInChildren<MeshRenderer>();
                    Assert.IsNotEmpty(renderers, entry.id);
                    foreach (var r in renderers)
                    {
                        Assert.AreEqual(entry.material.name, r.sharedMaterial.name, entry.id);
                        Assert.IsNotNull(r.sharedMaterial.GetTexture("_BaseMap"), entry.id + " 텍스처");
                    }
                    float top = renderers.Max(r => r.bounds.max.y) - holder.position.y;
                    float expected = entry.height * holder.parent.lossyScale.y;
                    if (entry.id != "Bridge" && entry.id != "Rock")  // 다리는 판에 맞춰 늘림, 바위는 크기를 섞음
                        Assert.AreEqual(expected, top, expected * 0.05f, entry.id + " 높이");
                    checkedCount++;
                }
            }
            Assert.Greater(checkedCount, 10, "소품 모델이 씬에 놓임");
        }

        [Test]
        public void Colliders_Kept()
        {
            Assert.IsTrue(GameObject.Find("FountainBase").GetComponent<Collider>(), "분수대");
            Assert.IsTrue(Object.FindObjectsByType<ChoppableTreeBehaviour>().All(t => t.GetComponentInChildren<Collider>()), "나무 줄기");
            Assert.IsTrue(Object.FindObjectsByType<Collider>().Any(c => c.name == "Deck"), "다리 판");
            Assert.IsFalse(GameObject.Find("FountainBase").GetComponent<MeshRenderer>(), "도형은 충돌만");
        }

        [UnityTest]
        public IEnumerator Flower_UsesModelLook()
        {
            var flower = ground.Objects().First(o => o.kind == GroundObjectKind.Flower);
            var look = FlowerLook.Of(flower);
            Assert.IsTrue(look.IsValid);
            StringAssert.StartsWith("Flower", look.Name, "꽃 모델 재질 이름 = 모습");
            Assert.IsNotNull(look.mesh);
            yield break;
        }
    }
}
