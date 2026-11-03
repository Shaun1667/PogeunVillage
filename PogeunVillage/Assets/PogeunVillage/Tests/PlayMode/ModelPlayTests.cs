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
            foreach (var entry in library.entries.Where(e => e.id != "Player"))  // 플레이어는 스킨 메시(Player_UsesModel_AnimatesWithSpeed에서 확인)
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
                    // 다리는 판에 맞춰 늘림, 바위는 크기를 섞음, 인트로 방 소품(Room…)은 폭에 맞춤(IntroPlayTests에서 확인),
                    // 도구(Tool…)는 누운 모델을 세워 높이가 그릇 기준과 다름(ShopStands_ShowToolModels에서 확인)
                    if (entry.id != "Bridge" && entry.id != "Rock" && !entry.id.StartsWith("Room") && !entry.id.StartsWith("Tool"))
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

        // P-4f: 시설 · 상점 주인은 모델, 기본 도형은 충돌만(충돌 크기 · 상호작용 자리는 그대로)
        [Test]
        public void Facilities_UseModels_KeepColliders()
        {
            foreach (var id in new[] { "ShopStall", "Shopkeeper", "SellBox", "Bed", "Workbench", "CampTent" })
            {
                var fit = GameObject.Find("Fit_" + id);
                Assert.IsTrue(fit, id + " 모델");
                Assert.IsNotEmpty(fit.GetComponentsInChildren<MeshRenderer>(), id + " 보임");
            }
            Assert.IsTrue(Object.FindObjectsByType<HousePlot>(FindObjectsInactive.Include)
                .Where(p => p.emptySign).All(p => p.emptySign.transform.Find("Fit_LotSign")), "빈 집터 표지판");

            var keeper = Object.FindAnyObjectByType<Shop>().keeper.transform;
            Assert.IsTrue(keeper.Find("Body").GetComponent<Collider>() && !keeper.Find("Body").GetComponent<MeshRenderer>(), "주인 몸은 충돌만");
            Assert.IsNull(keeper.Find("Head"), "그레이박스 머리 없음");
            var sellBox = Object.FindAnyObjectByType<SellBox>().transform.Find("Box");
            Assert.IsTrue(sellBox.GetComponent<Collider>() && !sellBox.GetComponent<MeshRenderer>(), "판매함은 충돌만");
            var bench = Object.FindAnyObjectByType<Workbench>().transform.Find("Top");
            Assert.IsTrue(bench.GetComponent<Collider>() && !bench.GetComponent<MeshRenderer>(), "작업대는 충돌만");
            var bed = Object.FindAnyObjectByType<Bed>().transform.Find("Frame");
            Assert.IsTrue(bed.GetComponent<Collider>() && !bed.GetComponent<MeshRenderer>(), "침대는 충돌만");
            var tent = GameObject.Find("CampSite").transform.Find("Tent");
            Assert.IsTrue(tent.GetComponent<Collider>() && !tent.GetComponent<MeshRenderer>(), "텐트는 충돌만");
            Assert.IsNull(GameObject.Find("CampSite").transform.Find("Fire"), "그레이박스 모닥불 없음");

            // 물건은 매대(노점 모델) 위에 보임
            var stall = GameObject.Find("Fit_ShopStall").GetComponentsInChildren<MeshRenderer>();
            float counterTop = Object.FindObjectsByType<ShopStand>().First().GetComponentsInChildren<Renderer>().Min(r => r.bounds.min.y);
            Assert.Greater(stall.Max(r => r.bounds.max.y), counterTop + 1f, "차양이 물건보다 위");
        }

        // P-4h: 매대의 도구 5종은 도구 모델(세워서 매대 위), 묘목은 아직 공
        [Test]
        public void ShopStands_ShowToolModels()
        {
            var stands = Object.FindObjectsByType<ShopStand>();
            foreach (var stand in stands.Where(s => s.item is ToolDefinition))
            {
                var fit = stand.transform.Find("Fit_Tool" + stand.item.id);
                Assert.IsTrue(fit, stand.item.id + " 도구 모델");
                Assert.IsNull(stand.transform.Find("Goods"), stand.item.id + " 공 없음");
                var bounds = fit.GetComponentsInChildren<Renderer>().Select(r => r.bounds).ToArray();
                float height = bounds.Max(b => b.max.y) - bounds.Min(b => b.min.y);
                float depth = bounds.Max(b => b.max.z) - bounds.Min(b => b.min.z);
                float width = bounds.Max(b => b.max.x) - bounds.Min(b => b.min.x);
                Assert.Greater(height, Mathf.Max(width, depth) * 0.6f, stand.item.id + " 세워 놓음");
                Assert.Greater(bounds.Min(b => b.min.y), stand.transform.position.y + 0.7f, stand.item.id + " 매대 위");
            }
            Assert.IsTrue(stands.Any(s => s.transform.Find("Goods")), "묘목은 공");
        }

        // P-4i: 묘목 3단계 · 밑동은 모델, 떨어진 목재는 공 대신 통나무 단 모델
        [UnityTest]
        public IEnumerator GroundItems_UseModels()
        {
            var tree = Object.FindObjectsByType<ChoppableTreeBehaviour>().First();
            var stump = tree.stumpPrefab;
            Assert.IsTrue(stump.transform.Find("Fit_Stump"), "밑동 모델");
            Assert.IsFalse(stump.transform.Find("Trunk").GetComponent<MeshRenderer>(), "밑동 원기둥은 충돌만");

            var sapling = Object.FindAnyObjectByType<GroundPlacer>().saplingPrefab;
            for (int i = 0; i < 3; i++)
                Assert.IsTrue(sapling.transform.Find($"Stage{i}/Model_SaplingStage{i}"), "묘목 " + i + "단계 모델");

            var wood = Object.Instantiate(tree.pickupPrefab, ground.transform.position + Vector3.up * 50f, Quaternion.identity);
            wood.item = tree.woodItem;
            yield return null;
            var model = wood.transform.Find("Model");
            Assert.IsTrue(model, "목재 모델");
            Assert.IsTrue(model.GetComponentsInChildren<Renderer>().All(r => r.enabled && r.sharedMaterial == tree.woodItem.pickupMaterial));
            Assert.IsFalse(wood.transform.Find("Ball").GetComponent<Renderer>().enabled, "공은 숨김");
            float height = model.GetComponentsInChildren<Renderer>().Max(r => r.bounds.max.y) - wood.transform.position.y;
            Assert.AreEqual(0.3f, height, 0.05f, "목재 높이");
            Object.Destroy(wood.gameObject);
        }

        // P-4a · P-4b: 과일나무는 둥근 나무 + 열매 모델, 떨어진 열매도 모델, 기본 가구 8은 모델(충돌 상자 · 상호작용은 그대로)
        [Test]
        public void FruitAndFurniture_UseModels()
        {
            foreach (var fruitTree in Object.FindObjectsByType<FruitTreeBehaviour>())
            {
                Assert.IsTrue(fruitTree.transform.Find("Model_TreeRound"), "과일나무는 둥근 나무 모델");
                string fruit = "Fit_Fruit" + fruitTree.fruitItem.id;
                Assert.IsTrue(fruitTree.fruitVisuals.All(v => v.name == fruit), fruit);
                Assert.IsNotNull(fruitTree.fruitItem.pickupModel, "떨어진 열매 모델");
            }
            foreach (var id in new[] { "Fence", "Crate", "Planter", "LogStool", "Bench", "RoundTable", "StreetLamp", "Birdhouse" })
            {
                var definition = (FurnitureDefinition)Item(id);
                var prefab = definition.prefab;
                Assert.IsTrue(prefab.transform.Find("Fit_Furniture" + id), id + " 모델");
                Assert.IsTrue(prefab.GetComponent<BoxCollider>(), id + " 충돌 상자");
                var bounds = prefab.GetComponentsInChildren<Renderer>().Select(r => r.bounds).ToArray();
                Assert.LessOrEqual(bounds.Max(b => b.max.x) - bounds.Min(b => b.min.x), 0.96f, id + " 한 칸 안");
            }
        }

        // P-4f: 플레이어는 리깅 모델 + 애니메이터(이동 속도 Speed로 서 있기 · 걷기 · 달리기), 손에 든 도구는 도구 모델
        [UnityTest]
        public IEnumerator Player_UsesModel_AnimatesWithSpeed()
        {
            var controller = player.GetComponent<PlayerController>();
            Assert.IsNotNull(controller.animator, "애니메이터");
            Assert.AreEqual("Model_Player", controller.animator.name);
            Assert.IsTrue(controller.animator.parameters.Any(p => p.name == "Speed"), "Speed 파라미터");
            Assert.IsNotNull(controller.animator.GetComponentInChildren<SkinnedMeshRenderer>(), "스킨 메시");
            Assert.IsFalse(player.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("Ear")), "그레이박스 토끼 없음");
            foreach (var hold in player.GetComponent<PlayerTools>().holdVisuals)
                Assert.IsTrue(hold.transform.Find("Fit_Tool" + hold.name), hold.name + " 도구 모델");

            Press(keyboard.wKey);
            yield return WaitSeconds(0.4f);
            Assert.Greater(controller.animator.GetFloat("Speed"), 0.5f, "걸으면 Speed");
            Release(keyboard.wKey);
            yield return WaitSeconds(0.2f);
            Assert.AreEqual(0f, controller.animator.GetFloat("Speed"), 0.01f, "멈추면 0");
        }

        // P-4e: 마을을 도는 곤충은 그 곤충 모델(그레이박스 몸 · 날개는 숨김)
        [UnityTest]
        public IEnumerator Insects_UseModels()
        {
            yield return null;
            var insects = Object.FindAnyObjectByType<Insects>(FindObjectsInactive.Include);
            Assert.IsNotNull(insects, "곤충 관리자");
            foreach (var definition in insects.rules.insects)
            {
                var insect = insects.SpawnAt(definition, ground.transform.position + Vector3.up * 50f);
                var model = insect.transform.Find("Model");
                Assert.IsTrue(model, definition.id + " 모델");
                Assert.IsTrue(model.GetComponentsInChildren<Renderer>().All(r => r.enabled && r.sharedMaterial == definition.pickupMaterial), definition.id);
                Assert.IsTrue(insect.bodyRenderers.All(r => !r.enabled), definition.id + " 그레이박스 숨김");
            }
            yield break;
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
