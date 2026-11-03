using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PogeunVillage.EditorTools
{
    /// <summary>
    /// Varco3D 모델 가져오기(P-3, MODELING_SPEC.md). 여러 번 실행해도 결과가 같음.
    /// Art/Raw/{Props,Houses,Villagers}/*.fbx(원본, 고치지 않음) → 색 텍스처를 Textures/Models/이름/base_color.png로 꺼냄(노멀은 쓰지 않아 지움)
    /// → FBX 재질 가져오기 끔 → Materials/Models/이름.mat(CurvedToon + 색 텍스처) → Data/ModelLibrary.asset 항목(없을 때만 기본 높이로).
    /// </summary>
    public static class VarcoModelImport
    {
        const string Root = "Assets/PogeunVillage";
        const string RawDir = Root + "/Art/Raw";
        const string TextureDir = Root + "/Textures/Models";
        const string MaterialDir = Root + "/Materials/Models";
        public const string LibraryPath = Root + "/Data/ModelLibrary.asset";
        static readonly string[] Groups = { "Props", "Houses", "Villagers", "Furniture", "Room" };

        // 처음 항목의 게임 안 높이(m) · 정면 회전 — MODELING_SPEC.md 3번 표. 이미 있는 항목은 에셋 값을 씀
        static readonly Dictionary<string, (float height, float yaw)> Defaults = new()
        {
            ["TreeRound"] = (3.8f, 0f),
            ["TreePine"] = (4.2f, 0f),
            ["TreeApple"] = (3.8f, 0f),
            ["TreePeach"] = (3.8f, 0f),
            ["Rock"] = (0.7f, 0f),
            ["FlowerTulip"] = (0.5f, 0f),
            ["FlowerDaisy"] = (0.42f, 0f),
            ["FlowerPansy"] = (0.42f, 0f),
            ["Fountain"] = (3.6f, 0f),
            ["NoticeBoard"] = (2.3f, 0f),
            ["StreetLamp"] = (2.6f, 0f),
            ["Bridge"] = (2.2f, 90f),
            ["Mailbox"] = (1.3f, 0f),
            ["ShopStall"] = (2.6f, 0f),
            ["SellBox"] = (0.8f, 0f),
            ["Bed"] = (0.8f, 0f),
            ["Workbench"] = (1.05f, 0f),
            ["CampTent"] = (2.3f, 0f),
            ["LotSign"] = (1.25f, 0f),
            ["Shopkeeper"] = (1.6f, 0f),
            ["ToolNet"] = (0.6f, 0f),  // 도구(P-4h): 노점 매대 위에 세운 높이
            ["ToolFishingRod"] = (0.6f, 0f),
            ["ToolAxe"] = (0.45f, 0f),
            ["ToolShovel"] = (0.55f, 0f),
            ["ToolWateringCan"] = (0.35f, 0f),
            ["Stump"] = (0.35f, 0f),  // 땅 물건(P-4i)
            ["SaplingStage0"] = (0.5f, 0f),
            ["SaplingStage1"] = (0.9f, 0f),
            ["SaplingStage2"] = (1.4f, 0f),
            ["PickupWood"] = (0.3f, 0f),
            ["Aquarium"] = (0.8f, 0f),  // 수조 · 물고기(P-4d): 물고기 높이는 떨어진 모습 기준, 수조 안에서는 길이에 맞춤
            ["FishMedaka"] = (0.08f, 0f),
            ["FishPaleChub"] = (0.08f, 0f),
            ["FishLoach"] = (0.08f, 0f),
            ["FishCrucianCarp"] = (0.12f, 0f),
            ["FishMandarinfish"] = (0.12f, 0f),
            ["FishCarp"] = (0.15f, 0f),
            ["FishCatfish"] = (0.15f, 0f),
            ["FishGoldenCarp"] = (0.15f, 0f),
            ["InsectCage"] = (0.5f, 0f),  // 곤충 · 채집통(P-4e, 생성만 — 적용 때 다시 맞춤)
            ["InsectCabbageWhite"] = (0.08f, 0f),  // 나비 · 잠자리는 납작해 키를 낮게(폭이 키의 2 ~ 5배)
            ["InsectHoneyBee"] = (0.1f, 0f),
            ["InsectGrasshopper"] = (0.1f, 0f),
            ["InsectCricket"] = (0.08f, 0f),
            ["InsectRedDragonfly"] = (0.03f, 0f),
            ["InsectCicada"] = (0.1f, 0f),
            ["InsectFirefly"] = (0.06f, 0f),
            ["InsectStagBeetle"] = (0.1f, 0f),
        };

        static readonly (float height, float yaw) HouseDefault = (3.6f, 0f);  // 주민별 집 House01 ~ 20
        static readonly (float height, float yaw) VillagerDefault = (1.5f, 0f);  // 주민 Villager01 ~ 20(그레이박스 몸 높이)

        [MenuItem("Pogeun Village/Import Varco Models")]
        public static void ImportAll()
        {
            var library = AssetDatabase.LoadAssetAtPath<ModelLibrary>(LibraryPath);
            if (!library)
            {
                library = ScriptableObject.CreateInstance<ModelLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            var toon = Shader.Find("PogeunVillage/CurvedToon");
            EnsureFolder(TextureDir);
            EnsureFolder(MaterialDir);

            int count = 0;
            foreach (string group in Groups)
            {
                string dir = $"{RawDir}/{group}";
                if (!AssetDatabase.IsValidFolder(dir))
                    continue;
                foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { dir }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string id = Path.GetFileNameWithoutExtension(path);
                    var texture = ExtractColor(path, id);
                    var material = EnsureMaterial(id, toon, texture);

                    var entry = library.Find(id);
                    if (entry == null)
                    {
                        var (height, yaw) = Defaults.TryGetValue(id, out var d) ? d
                            : id.StartsWith("House") ? HouseDefault
                            : id.StartsWith("Villager") ? VillagerDefault
                            : (1f, 0f);
                        entry = new ModelLibrary.Entry { id = id, height = height, yaw = yaw };
                        library.entries.Add(entry);
                    }
                    entry.model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    entry.material = material;
                    count++;
                }
            }
            library.entries.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log($"[모델] Varco3D 모델 {count}개를 가져왔어요");
        }

        // 원본 FBX 안의 색 텍스처를 꺼내고(처음 한 번), FBX 재질 가져오기는 끔
        static Texture2D ExtractColor(string modelPath, string id)
        {
            string folder = $"{TextureDir}/{id}";
            string colorPath = $"{folder}/base_color.png";
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            if (!File.Exists(colorPath))
            {
                if (importer.materialImportMode == ModelImporterMaterialImportMode.None)
                {
                    importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                    importer.SaveAndReimport();
                }
                importer.ExtractTextures(folder);
                foreach (string extra in Directory.GetFiles(folder, "*.png"))
                    if (Path.GetFileName(extra) != "base_color.png")
                        File.Delete(extra);
                AssetDatabase.Refresh();
            }
            if (importer.materialImportMode != ModelImporterMaterialImportMode.None || importer.animationType != ModelImporterAnimationType.None)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.animationType = ModelImporterAnimationType.None;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath);
        }

        static Material EnsureMaterial(string id, Shader toon, Texture2D texture)
        {
            string path = $"{MaterialDir}/{id}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(toon);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = toon;
            mat.SetTexture("_BaseMap", texture);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_AmbientStrength", 0.25f);
            mat.SetFloat("_RimStrength", 0.12f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }
    }
}
