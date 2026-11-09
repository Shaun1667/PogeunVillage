using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using TMPro;

namespace PogeunVillage.EditorTools
{
    /// <summary>
    /// 포근 마을 맵 프로토타입 생성기. 메뉴: Pogeun Village > Build Map Prototype
    /// 모든 오브젝트는 임시(프리미티브) 모델이며, 나중에 Varco3D 에셋으로 교체합니다.
    /// </summary>
    public static class VillageMapBuilder
    {
        const string Root = "Assets/PogeunVillage";
        const string MaterialDir = Root + "/Materials";
        const string MeshDir = Root + "/Meshes";
        const string SettingsDir = Root + "/Settings";
        const string TextureDir = Root + "/Textures";
        const string ScenePath = Root + "/Scenes/VillagePrototype.unity";
        const string WorldSettingsPath = Root + "/Data/WorldSettings.asset";
        const string GatheringRulesPath = Root + "/Data/GatheringRules.asset";
        const string InventoryRulesPath = Root + "/Data/InventoryRules.asset";
        const string GroundGridPath = Root + "/Data/GroundGrid.asset";
        const string DayRulesPath = Root + "/Data/DayRules.asset";
        const string ShopDataPath = Root + "/Data/ShopData.asset";
        const string VillageRulesPath = Root + "/Data/VillageRules.asset";
        const string RatingRulesPath = Root + "/Data/RatingRules.asset";
        const string FishingRulesPath = Root + "/Data/FishingRules.asset";
        const string InsectRulesPath = Root + "/Data/InsectRules.asset";
        const string SaveRulesPath = Root + "/Data/SaveRules.asset";
        const string IntroSettingsPath = Root + "/Data/IntroSettings.asset";
        const string PlayerAnimatorPath = Root + "/Data/PlayerAnimator.controller";
        const string VillagerAnimatorPath = Root + "/Data/VillagerAnimator.controller";
        const string VillagerAnimatorDir = Root + "/Data/VillagerAnimators";
        const string VillagerDir = Root + "/Data/Villagers";
        const string FontSourcePath = Root + "/Fonts/Cafe24Ssurround-v2.0.ttf";
        const string FontAssetPath = Root + "/Fonts/Cafe24Ssurround SDF.asset";
        const string ItemDir = Root + "/Data/Items";
        const string PrefabDir = Root + "/Prefabs";
        const string ToolDir = ItemDir + "/Tools";
        const string FurnitureDir = ItemDir + "/Furniture";

        const int GroundHalfSize = 90;   // 지면 메시 범위(±m), 1m 격자
        const int WaterHalfSize = 130;   // 물 메시 범위(±m), 2m 격자

        static readonly Vector2 PlazaCenter = new(-6f, 2f);
        const float PlazaRadius = 6.5f;
        static readonly Vector2 RampBottom = new(-2f, 19f);
        static readonly Vector2 RampTop = new(-2f, 28f);

        struct HouseInfo
        {
            public string name;
            public Vector2 position;
            public bool onPlateau;
        }

        // 집터 12곳(결정 2026-10-28, 2-4): 예전 집 자리 8곳 + 새 4곳. 주민이 사는 집터에만 집이 보이고 색은 주민 색
        static readonly HouseInfo[] Houses =
        {
            new() { name = "HousePlot_01", position = new(-22f, 10f) },
            new() { name = "HousePlot_02", position = new(-24f, -6f) },
            new() { name = "HousePlot_03", position = new(-8f, 15f) },
            new() { name = "HousePlot_04", position = new(-14f, -16f) },
            new() { name = "HousePlot_05", position = new(0f, -16f) },
            new() { name = "HousePlot_06", position = new(28f, 4f) },
            new() { name = "HousePlot_07", position = new(-16f, 36f), onPlateau = true },
            new() { name = "HousePlot_08", position = new(4f, 38f), onPlateau = true },
            new() { name = "HousePlot_09", position = new(-36f, 14f) },
            new() { name = "HousePlot_10", position = new(-28f, -24f) },
            new() { name = "HousePlot_11", position = new(30f, -14f) },
            new() { name = "HousePlot_12", position = new(34f, 18f) },
        };

        static readonly float[] BridgeZs = { 4f, -24f };

        // 캠핑 구역(결정 2026-10-28, 2-6): 마을 중간 좌측 = 광장 서쪽 끝, 텐트 입구는 광장 쪽
        static readonly Vector2 CampPosition = new(-38f, -4f);
        const float CampTentOffset = 0.66f;  // CampTent 모델 안에서 텐트 가운데가 +x로 치우친 거리(m, 높이 2.3 기준)
        const float FruitSize = 0.38f;  // 나무에 달린 열매 키(m, 예전 공 지름)
        const float FruitCrownRadius = 0.78f, FruitCrownLift = 0.2f;  // 열매 자리: 잎 덩어리 반지름 비율 · 가운데에서 위로(반지름 비율)
        const float FurnitureCellLimit = 0.95f;  // 가구 모델 가로 · 세로 최대(m, 한 칸 1m)
        const float CageInsectSize = 0.2f, CageInsectCenter = 0.42f;  // 채집통 앞 곤충: 가장 긴 변(m) · 높이(채집통 키 비율)
        const float AquariumWaterCenter = 0.72f;  // 수조 모델 높이 중 물(유리) 가운데 비율 — 물고기 높이
        const float CounterTop = 0.85f;  // 노점 매대(모델) 윗면 높이 — 물건을 이 위에 놓음
        const float StandSaplingHeight = 0.55f;  // 매대 위 묘목 높이(심은 묘목보다 작게)
        const float StandSaplingForward = 0.28f;  // 묘목 자리는 노점 기둥 앞이라 손님 쪽으로 당김
        // 누워서 생성된 도구 모델을 세우는 x축 회전(P-4h): 삽은 날이 -z로 누움, 잠자리채는 45° 기움
        static readonly Dictionary<string, float> ToolPitch = new() { ["ToolShovel"] = -90f, ["ToolNet"] = 45f };
        static Vector2 CampDoor => CampPosition + (PlazaCenter - CampPosition).normalized * 2.4f;

        static Shader toonShader;
        static WorldSettings settings;
        static GatheringRules gatheringRules;
        static InventoryRules inventoryRules;
        static ItemDefinition appleItem, peachItem, flowerItem;
        static Pickup pickupPrefab;
        static ToolDefinition[] tools;
        static ItemDefinition woodItem, flowerClumpItem, saplingItem;
        static FurnitureDefinition[] furniture;
        static FurnitureDefinition[] themeFurniture;  // 주민 20명 × 4가지(3-4d)
        static FishDefinition[] fish;                 // 물고기 8종(3-6a)
        static InsectDefinition[] insects;            // 곤충 8종(3-7a)
        static ShopData shopData;
        static GameObject stumpPrefab;
        static readonly List<(Vector2 a, Vector2 b)> Paths = new();
        static readonly List<Vector3> Occupied = new(); // x, z, radius

        [MenuItem("Pogeun Village/Build Map Prototype")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsureFolder(MaterialDir);
            EnsureFolder(MeshDir);
            EnsureFolder(SettingsDir);
            EnsureFolder(Root + "/Scenes");

            toonShader = Shader.Find("PogeunVillage/CurvedToon");
            if (!toonShader)
            {
                Debug.LogError("PogeunVillage/CurvedToon 셰이더를 찾을 수 없습니다.");
                return;
            }

            settings = AssetDatabase.LoadAssetAtPath<WorldSettings>(WorldSettingsPath);
            if (!settings)
            {
                Debug.LogError($"{WorldSettingsPath} 를 찾을 수 없습니다.");
                return;
            }

            // Varco3D 모델(P-3): 가져오기 메뉴를 먼저 돌려 목록을 최신으로
            VarcoModelImport.ImportAll();
            models = AssetDatabase.LoadAssetAtPath<ModelLibrary>(VarcoModelImport.LibraryPath);

            // 채집 데이터 · 프리팹 (없을 때만 만들고, 있으면 값을 건드리지 않음)
            EnsureFolder(ItemDir);
            EnsureFolder(PrefabDir);
            gatheringRules = EnsureAsset<GatheringRules>(GatheringRulesPath);
            inventoryRules = EnsureAsset<InventoryRules>(InventoryRulesPath);
            appleItem = EnsureItem("Apple", "사과", sellPrice: 100);
            peachItem = EnsureItem("Peach", "복숭아", sellPrice: 100);
            SetPickupLook(appleItem, "FruitApple");  // 떨어진 열매도 열매 모델(P-4a)
            SetPickupLook(peachItem, "FruitPeach");
            flowerItem = EnsureItem("Flower", "꽃", sellPrice: 40);
            pickupPrefab = BuildPickupPrefab();
            woodItem = EnsureItem("Wood", "목재", sellPrice: 20);
            SetPickupLook(woodItem, "PickupWood");  // 떨어진 목재는 통나무 단 모델(P-4i)
            flowerClumpItem = EnsureItem("FlowerClump", "꽃 포기", GroundObjectKind.Flower, sellPrice: 60);
            saplingItem = EnsureItem("Sapling", "묘목", GroundObjectKind.Sapling, buyPrice: 300, sellPrice: 150);
            furniture = EnsureFurniture();
            var villagers = EnsureVillagers();
            themeFurniture = EnsureThemeFurniture(villagers);
            BuildHousePrefabs(villagers);
            BuildVillagerModels(villagers);
            fish = EnsureFish();
            insects = EnsureInsects();
            shopData = EnsureAsset<ShopData>(ShopDataPath);
            stumpPrefab = BuildStumpPrefab();
            EnsureFolder(ToolDir);
            tools = new[]
            {
                EnsureHitKinds(EnsureTool("Net", "잠자리채", ToolKind.Net, false, InteractableKind.Villager), InteractableKind.Insect),  // 주민 괴롭히기(3-2) · 곤충 잡기(3-7a)
                EnsureTool("FishingRod", "낚싯대", ToolKind.FishingRod, false),
                EnsureTool("Axe", "도끼", ToolKind.Axe, false, InteractableKind.Tree),
                EnsureTool("Shovel", "삽", ToolKind.Shovel, true, InteractableKind.Flower),
                EnsureTool("WateringCan", "물뿌리개", ToolKind.WateringCan, true, InteractableKind.Flower),
            };

            BuildPaths();
            Occupied.Clear();
            var random = new System.Random(1004);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupLighting();
            SetupVolume();

            var world = new GameObject("World").transform;
            BuildGround(world);
            BuildWater(world);

            var props = new GameObject("Props").transform;
            props.SetParent(world, false);
            BuildPlaza(props);
            var plots = Houses.Select(house => BuildHouse(props, house)).ToArray();
            var campSite = BuildCamp(props);
            foreach (float z in BridgeZs)
                BuildBridge(props, z);
            BuildLamps(props);
            BuildTrees(props, random);
            BuildRocks(props, random);
            var ground = BuildGroundGrid(world);
            BuildFlowers(props, random, ground);

            var player = BuildPlayer();
            BuildGroundCursor(player.GetComponent<PlayerTools>(), ground);
            BuildShovel(player, ground);
            BuildDayCycle(props);
            BuildShop(props);
            BuildWorkbench(props);
            BakeWalkable(world, ground);
            var dialogue = BuildHud(player);
            BuildVillage(plots, campSite, ground, dialogue);
            BuildRating(ground, dialogue);
            BuildFurniture(player, ground, dialogue);
            BuildFishing(player, ground);
            BuildInsects(player, ground);
            BuildPlayLogger(player);
            BuildSave(player);
            var cam = BuildCamera(player.transform);
            player.GetComponent<PlayerController>().cameraTransform = cam.transform;
            BuildIntro(player, cam);

            var curved = new GameObject("CurvedWorld").AddComponent<CurvedWorld>();
            curved.settings = settings;
            curved.focus = player.transform;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"포근 마을 맵 프로토타입 생성 완료: {ScenePath}");
        }

        // ---------------------------------------------------------------- terrain shape

        static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        static float RiverCenterX(float z) => 14f + Mathf.Sin(z * 0.07f) * 6f;

        static float RiverDistance(float x, float z) => Mathf.Abs(x - RiverCenterX(z));

        static float RiverMask(float x, float z) => 1f - Smooth01((RiverDistance(x, z) - 2.2f) / 1.6f);

        static float PlazaMask(float x, float z) =>
            1f - Smooth01((Vector2.Distance(new Vector2(x, z), PlazaCenter) - PlazaRadius) / 2f);

        static float BeachMask(float z) => Smooth01((-z - 38f) / 9f);

        static float Height(float x, float z)
        {
            float h = (Mathf.PerlinNoise(x * 0.06f + 10f, z * 0.06f + 3f) - 0.5f) * 0.4f;

            // 북쪽 언덕(2층 지대). 경사로 부분만 완만하고 나머지는 절벽
            float rampMask = 1f - Smooth01((Mathf.Abs(x - RampBottom.x) - 2.5f) / 2f);
            float tierWidth = Mathf.Lerp(1.2f, 8f, rampMask);
            h += Smooth01((z - (23.5f - tierWidth * 0.5f)) / tierWidth) * 1.6f;

            h = Mathf.Lerp(h, 0.02f, PlazaMask(x, z));

            // 맵 가장자리(동/서/북)는 높은 언덕으로 막기
            float edge = Mathf.Max(Mathf.Abs(x), z);
            h += Smooth01((edge - 50f) / 3f) * 4f + Smooth01((edge - 58f) / 10f) * 5f;

            h = Mathf.Lerp(h, -1.6f, BeachMask(z));
            h = Mathf.Lerp(h, -1.1f, RiverMask(x, z));
            return h;
        }

        static float Slope(float x, float z)
        {
            const float e = 0.5f;
            float dx = (Height(x + e, z) - Height(x - e, z)) / (2f * e);
            float dz = (Height(x, z + e) - Height(x, z - e)) / (2f * e);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        static void BuildPaths()
        {
            Paths.Clear();
            Paths.Add((PlazaCenter, CampDoor));
            foreach (var house in Houses)
            {
                Vector2 door = DoorPoint(house);
                if (house.onPlateau)
                {
                    Paths.Add((PlazaCenter, RampBottom));
                    Paths.Add((RampBottom, RampTop));
                    Paths.Add((RampTop, door));
                }
                else if (IsEastOfRiver(house))
                {
                    Paths.Add((PlazaCenter, BridgeEnd(BridgeZs[0], -1f)));
                    Paths.Add((BridgeEnd(BridgeZs[0], 1f), door));
                }
                else
                {
                    Paths.Add((PlazaCenter, door));
                }
            }
        }

        static bool IsEastOfRiver(HouseInfo house) => house.position.x > RiverCenterX(house.position.y);

        static Vector2 BridgeEnd(float z, float side) => new(RiverCenterX(z) + side * 5f, z);

        // 집 현관이 바라보는 지점
        static Vector2 FacingTarget(HouseInfo house)
        {
            if (house.onPlateau)
                return RampTop;
            return IsEastOfRiver(house) ? BridgeEnd(BridgeZs[0], 1f) : PlazaCenter;
        }

        static Vector2 DoorPoint(HouseInfo house)
        {
            Vector2 forward = (FacingTarget(house) - house.position).normalized;
            return house.position + forward * 2.8f;
        }

        static float PathDistance(float x, float z)
        {
            var p = new Vector2(x, z);
            float best = float.MaxValue;
            foreach (var (a, b) in Paths)
            {
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }

        // ---------------------------------------------------------------- ground & water

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        static Color GroundColor(float x, float z, float h)
        {
            // 경계를 또렷하게(동물의숲 느낌), 대신 경계선은 노이즈로 살짝 구불구불하게
            float wobble = (Mathf.PerlinNoise(x * 0.35f + 7f, z * 0.35f + 7f) - 0.5f) * 0.5f;

            float patch = Mathf.PerlinNoise(x * 0.07f + 50f, z * 0.07f + 50f);
            Color grass = Color.Lerp(Hex("#9BD872"), Hex("#86CB64"), Smooth01((patch - 0.5f) / 0.08f));
            grass = Color.Lerp(grass, Hex("#ADDC7A"), Smooth01((h - 0.9f) / 0.2f) * 0.5f);

            Color c = grass;
            c = Color.Lerp(c, Hex("#E9D3A2"), 1f - Smooth01((PathDistance(x, z) - 1f - wobble * 0.4f) / 0.12f));
            float plazaDist = Vector2.Distance(new Vector2(x, z), PlazaCenter);
            c = Color.Lerp(c, Hex("#EAD9AE"), 1f - Smooth01((plazaDist - PlazaRadius - wobble) / 0.15f));
            c = Color.Lerp(c, Hex("#F4E6BE"), Smooth01((-z - 34f + wobble * 2f) / 0.3f));

            float bank = (1f - Smooth01((RiverDistance(x, z) - 3.3f - wobble) / 0.2f)) * (1f - Smooth01((h - 0.2f) / 0.2f));
            c = Color.Lerp(c, Hex("#D8C79B"), bank);

            c = Color.Lerp(c, Hex("#B79B76"), Smooth01((Slope(x, z) - 0.85f) / 0.15f));
            return c;
        }

        static void BuildGround(Transform parent)
        {
            int n = GroundHalfSize * 2;
            int stride = n + 1;
            var vertices = new Vector3[stride * stride];
            var uvs = new Vector2[vertices.Length];

            for (int iz = 0; iz <= n; iz++)
            for (int ix = 0; ix <= n; ix++)
            {
                float x = ix - GroundHalfSize;
                float z = iz - GroundHalfSize;
                int i = iz * stride + ix;
                vertices[i] = new Vector3(x, Height(x, z), z);
                uvs[i] = new Vector2((float)ix / n, (float)iz / n);
            }

            var mesh = new Mesh { name = "VillageGround" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = GridTriangles(n);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh = SaveMesh(mesh, "VillageGround");

            var mat = Mat("Ground", "#FFFFFF", rim: 0f);
            mat.SetTexture("_BaseMap", BakeGroundTexture());
            var ground = MeshObject("Ground", parent, mesh, mat);
            ground.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        // 길/모래/절벽 색을 1m 격자보다 훨씬 촘촘하게 칠하기 위해 텍스처로 굽기 (약 0.18m/픽셀)
        static Texture2D BakeGroundTexture()
        {
            const int size = 2048;
            float worldSize = GroundHalfSize * 2f;
            var pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
            for (int px = 0; px < size; px++)
            {
                float x = (px + 0.5f) / size * worldSize - GroundHalfSize;
                float z = (py + 0.5f) / size * worldSize - GroundHalfSize;
                Color c = GroundColor(x, z, Height(x, z));
                float speckle = (Mathf.PerlinNoise(x * 0.9f + 200f, z * 0.9f + 200f) - 0.5f) * 0.06f;
                pixels[py * size + px] = new Color(c.r + speckle, c.g + speckle, c.b + speckle);
            }

            var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            tex.SetPixels32(pixels);
            tex.Apply();

            EnsureFolder(TextureDir);
            string path = TextureDir + "/GroundColor.png";
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.anisoLevel = 4;
            importer.maxTextureSize = size;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void BuildWater(Transform parent)
        {
            const float step = 2f;
            int n = Mathf.RoundToInt(WaterHalfSize * 2 / step);
            int stride = n + 1;
            var vertices = new Vector3[stride * stride];
            for (int iz = 0; iz <= n; iz++)
            for (int ix = 0; ix <= n; ix++)
                vertices[iz * stride + ix] = new Vector3(ix * step - WaterHalfSize, 0f, iz * step - WaterHalfSize);

            var mesh = new Mesh { name = "VillageWater" };
            mesh.vertices = vertices;
            mesh.triangles = GridTriangles(n);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh = SaveMesh(mesh, "VillageWater");

            var mat = Mat("Water", "#7FD4EA", rim: 0.35f);
            mat.SetColor("_ShadowTint", Hex("#9FB8E8"));
            mat.SetFloat("_WaveAmp", 0.06f);
            mat.SetFloat("_WaveSpeed", 1.2f);
            mat.SetFloat("_WaveScale", 0.6f);

            var water = MeshObject("Water", parent, mesh, mat);
            water.transform.localPosition = new Vector3(0f, settings.waterLevel, 0f);
            water.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        static int[] GridTriangles(int n)
        {
            int stride = n + 1;
            var tris = new int[n * n * 6];
            int t = 0;
            for (int iz = 0; iz < n; iz++)
            for (int ix = 0; ix < n; ix++)
            {
                int i = iz * stride + ix;
                tris[t++] = i;
                tris[t++] = i + stride;
                tris[t++] = i + 1;
                tris[t++] = i + 1;
                tris[t++] = i + stride;
                tris[t++] = i + stride + 1;
            }
            return tris;
        }

        // ---------------------------------------------------------------- props

        static Vector3 OnGround(float x, float z, float yOffset = 0f) => new(x, Height(x, z) + yOffset, z);

        static bool IsFree(float x, float z, float radius)
        {
            foreach (var o in Occupied)
            {
                float dx = x - o.x, dz = z - o.y;
                float r = radius + o.z;
                if (dx * dx + dz * dz < r * r)
                    return false;
            }
            return true;
        }

        static void BuildPlaza(Transform parent)
        {
            var plaza = new GameObject("Plaza").transform;
            plaza.SetParent(parent, false);
            plaza.localPosition = OnGround(PlazaCenter.x, PlazaCenter.y);
            Occupied.Add(new Vector3(PlazaCenter.x, PlazaCenter.y, PlazaRadius + 0.5f));

            var stone = Mat("Stone", "#E8E2D6");
            var fountainBase = Prim(PrimitiveType.Cylinder, "FountainBase", plaza, new Vector3(0f, 0.15f, 0f), new Vector3(4.6f, 0.3f, 4.6f), stone);
            var box = fountainBase.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 6f, 1f);
            ColliderOnly(fountainBase);
            ModelVisual("Fountain", plaza, Vector3.zero);

            // 광장 게시판
            var wood = Mat("Wood", "#B98A5E");
            var board = new GameObject("NoticeBoard").transform;
            board.SetParent(parent, false);
            board.localPosition = OnGround(PlazaCenter.x + 4f, PlazaCenter.y + 4.5f);
            board.localRotation = Quaternion.Euler(0f, 215f, 0f);
            ColliderOnly(Prim(PrimitiveType.Cube, "Board", board, new Vector3(0f, 1.15f, 0f), new Vector3(1.7f, 2.3f, 0.4f), null, collider: true));
            ModelVisual("NoticeBoard", board, Vector3.zero);
            AddInteractable(board.gameObject, "게시판", InteractableKind.NoticeBoard, radius: 1f, markerHeight: 2.7f);
        }

        static HousePlot BuildHouse(Transform parent, HouseInfo info)
        {
            var plotRoot = new GameObject(info.name).transform;
            plotRoot.SetParent(parent, false);
            plotRoot.localPosition = OnGround(info.position.x, info.position.y, -0.05f);
            Vector2 forward = FacingTarget(info) - info.position;
            plotRoot.localRotation = Quaternion.LookRotation(new Vector3(forward.x, 0f, forward.y));
            Occupied.Add(new Vector3(info.position.x, info.position.y, 4f));

            // 벽 · 지붕은 공용 재질(흰 바탕) + 주민 색 프로퍼티 블록(HousePlot.Show)
            var wall = Mat("HouseWall", "#FFFFFF");
            var roof = Mat("HouseRoof", "#FFFFFF", rim: 0.2f);
            var wood = Mat("Wood", "#B98A5E");
            var glass = Mat("Window", "#BFE7F7", rim: 0.4f);

            var house = new GameObject("House").transform;
            house.SetParent(plotRoot, false);
            var body = Prim(PrimitiveType.Cube, "Body", house, new Vector3(0f, 1f, 0f), new Vector3(4f, 3f, 3.6f), wall, collider: true);
            var roofObj = MeshObject("Roof", house, RoofMesh(), roof);
            roofObj.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            roofObj.transform.localScale = new Vector3(4.8f, 1.7f, 4.4f);
            var chimney = Prim(PrimitiveType.Cube, "Chimney", house, new Vector3(1.2f, 3.6f, -0.6f), new Vector3(0.5f, 1f, 0.5f), wall);
            var greybox = new List<Renderer> { body.GetComponent<Renderer>(), roofObj.GetComponent<Renderer>(), chimney.GetComponent<Renderer>() };
            greybox.Add(Prim(PrimitiveType.Cube, "Door", house, new Vector3(0f, 0.8f, 1.82f), new Vector3(1f, 1.6f, 0.08f), wood).GetComponent<Renderer>());
            greybox.Add(Prim(PrimitiveType.Sphere, "DoorKnob", house, new Vector3(0.3f, 0.8f, 1.88f), Vector3.one * 0.12f, Mat("Gold", "#F2C94C", rim: 0.4f)).GetComponent<Renderer>());
            greybox.Add(Prim(PrimitiveType.Cube, "Window_L", house, new Vector3(-1.3f, 1.4f, 1.82f), new Vector3(0.8f, 0.8f, 0.08f), glass).GetComponent<Renderer>());
            greybox.Add(Prim(PrimitiveType.Cube, "Window_R", house, new Vector3(1.3f, 1.4f, 1.82f), new Vector3(0.8f, 0.8f, 0.08f), glass).GetComponent<Renderer>());
            greybox.Add(Prim(PrimitiveType.Cube, "Step", house, new Vector3(0f, -0.05f, 2.15f), new Vector3(1.6f, 0.2f, 0.7f), Mat("Stone", "#E8E2D6")).GetComponent<Renderer>());

            // 우체통 (흔들림이 우체통에만 걸리도록 묶음) — 모델(P-3b)
            var mailbox = new GameObject("Mailbox").transform;
            mailbox.SetParent(house, false);
            mailbox.localPosition = new Vector3(1.9f, 0f, 2.9f);
            ModelVisual("Mailbox", mailbox, Vector3.zero);
            AddInteractable(mailbox.gameObject, "우체통", InteractableKind.Mailbox, radius: 0.3f, markerHeight: 1.5f);

            // 빈 집터: 낮고 둥근 흙더미(P-4l, 충돌 없음) + 말뚝 표지판
            var sign = new GameObject("EmptySign").transform;
            sign.SetParent(plotRoot, false);
            MeshObject("Lot", sign, LotMesh(), Mat("EmptyLot", "#D9C49A"));
            FitModel("LotSign", sign, new Vector3(0f, 0f, 2.3f), new Vector3(0f, 1.25f, 0f));
            sign.gameObject.SetActive(false);

            var door = new GameObject("Door").transform;
            door.SetParent(plotRoot, false);
            door.localPosition = new Vector3(0f, 0.05f, 2.8f);

            var plot = plotRoot.gameObject.AddComponent<HousePlot>();
            plot.door = door;
            plot.house = house.gameObject;
            plot.emptySign = sign.gameObject;
            plot.wallRenderers = new Renderer[] { body.GetComponent<Renderer>(), chimney.GetComponent<Renderer>() };
            plot.roofRenderers = new Renderer[] { roofObj.GetComponent<Renderer>() };
            plot.greyboxRenderers = greybox.ToArray();
            return plot;
        }

        static void BuildBridge(Transform parent, float z)
        {
            float x = RiverCenterX(z);
            var bridge = new GameObject($"Bridge_{z:0}").transform;
            bridge.SetParent(parent, false);
            bridge.localPosition = new Vector3(x, 0f, z);
            float slope = Mathf.Cos(z * 0.07f) * 6f * 0.07f; // 강 방향에 수직으로 놓기
            bridge.localRotation = Quaternion.Euler(0f, Mathf.Atan(slope) * Mathf.Rad2Deg, 0f);
            Occupied.Add(new Vector3(x, z, 5.5f));

            // 걷는 판 · 난간 기둥은 보이지 않는 충돌만(평평한 판 기준), 모습은 모델 — 길이 · 폭을 판에 맞춤
            ColliderOnly(Prim(PrimitiveType.Cube, "Deck", bridge, new Vector3(0f, 0.05f, 0f), new Vector3(10f, 0.3f, 2.6f), null, collider: true));
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 4; i++)
                {
                    float px = -4.5f + i * 3f;
                    var post = Prim(PrimitiveType.Cube, "Post", bridge, new Vector3(px, 0.45f, side * 1.2f), new Vector3(0.2f, 0.8f, 0.2f), null, collider: true);
                    post.GetComponent<BoxCollider>().size = new Vector3(1f, 2f, 1f);
                    ColliderOnly(post);
                }
            var model = ModelVisual("Bridge", bridge, new Vector3(0f, -0.1f, 0f));
            FitFootprint(model, 10.8f, 3.0f);
        }

        static void BuildLamps(Transform parent)
        {
            var lamps = new GameObject("Lamps").transform;
            lamps.SetParent(parent, false);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f; // 정남쪽(카메라 쪽)은 비워 두기
                Vector2 p = PlazaCenter + (Vector2)(Quaternion.Euler(0f, 0f, angle) * Vector2.right) * (PlazaRadius + 0.3f);
                if (PathDistance(p.x, p.y) < 1.5f)
                    continue;
                var lamp = new GameObject("Lamp").transform;
                lamp.SetParent(lamps, false);
                lamp.localPosition = OnGround(p.x, p.y);
                ColliderOnly(Prim(PrimitiveType.Cylinder, "Pole", lamp, new Vector3(0f, 1f, 0f), new Vector3(0.14f, 1f, 0.14f), null, collider: true));
                ModelVisual("StreetLamp", lamp, Vector3.zero);
            }
        }

        static bool CanPlaceNature(float x, float z, float radius)
        {
            if (RiverDistance(x, z) < 4.5f || PathDistance(x, z) < 1.8f || PlazaMask(x, z) > 0.01f)
                return false;
            float h = Height(x, z);
            if (h < 0.05f || Slope(x, z) > 0.6f)
                return false;
            return IsFree(x, z, radius);
        }

        static void BuildTrees(Transform parent, System.Random random)
        {
            var trees = new GameObject("Trees").transform;
            trees.SetParent(parent, false);

            // 마을 안쪽
            int placed = 0;
            for (int attempt = 0; attempt < 2000 && placed < 70; attempt++)
            {
                float x = Range(random, -48f, 48f);
                float z = Range(random, -34f, 48f);
                if (!CanPlaceNature(x, z, 1.6f))
                    continue;
                PlaceTree(trees, random, x, z, inVillage: true);
                placed++;
            }

            // 가장자리 숲 (맵 경계)
            for (int attempt = 0; attempt < 3000 && placed < 260; attempt++)
            {
                float x = Range(random, -80f, 80f);
                float z = Range(random, -40f, 80f);
                if (Mathf.Max(Mathf.Abs(x), z) < 51f || Height(x, z) < 0.3f || !IsFree(x, z, 1.4f))
                    continue;
                PlaceTree(trees, random, x, z, inVillage: false);
                placed++;
            }
        }

        // inVillage: 마을 안쪽 나무만 도끼로 벨 수 있음(결정 2026-10-27). 가장자리 숲은 경계
        static void PlaceTree(Transform parent, System.Random random, float x, float z, bool inVillage)
        {
            Occupied.Add(new Vector3(x, z, 1.4f));
            var tree = new GameObject("Tree").transform;
            tree.SetParent(parent, false);
            tree.localPosition = OnGround(x, z, -0.1f);
            tree.localRotation = Quaternion.Euler(0f, Range(random, 0f, 360f), 0f);
            tree.localScale = Vector3.one * Range(random, 0.85f, 1.25f);

            int kind = random.Next(10);
            if (kind < 3)
            {
                // 침엽수
                ColliderOnly(Prim(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0f, 0.6f, 0f), new Vector3(0.35f, 0.6f, 0.35f), null, collider: true));
                ModelVisual("TreePine", tree, Vector3.zero);
                AddInteractable(tree.gameObject, "나무", inVillage ? InteractableKind.Tree : InteractableKind.ForestTree, radius: 0.4f, markerHeight: 4.6f);
            }
            else
            {
                // 둥근 활엽수 (일부는 과일나무 — 결정 2026-10-30: 열매 없는 둥근 나무 모델 + 딸 수 있는 열매 모델을 따로)
                bool apple = kind == 8, peach = kind == 9;
                ColliderOnly(Prim(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0f, 0.8f, 0f), new Vector3(0.4f, 0.8f, 0.4f), null, collider: true));
                var crown = ModelVisual("TreeRound", tree, Vector3.zero);

                AddInteractable(tree.gameObject, "나무", inVillage ? InteractableKind.Tree : InteractableKind.ForestTree, radius: 0.4f, markerHeight: 4.2f);

                if (apple || peach)
                {
                    var fruit = Mat(apple ? "Apple" : "Peach", apple ? "#F0645A" : "#FFB38A", rim: 0.3f);
                    var visuals = new Transform[gatheringRules.fruitMaxOnTree];
                    // 열매는 잎 덩어리 겉면에(P-4a): 나무 모델 크기(나무 기준 좌표)에서 반지름 · 가운데 높이를 잼
                    var crownBounds = ModelBounds(crown);
                    var scale = tree.lossyScale;
                    float radius = Mathf.Max(crownBounds.extents.x / scale.x, crownBounds.extents.z / scale.z);
                    float crownY = (crownBounds.max.y - tree.position.y) / scale.y - radius * (1f - FruitCrownLift);  // 둥근 잎 덩어리의 가운데 = 꼭대기에서 반지름만큼 아래
                    for (int i = 0; i < visuals.Length; i++)
                    {
                        float a = (i * 360f / visuals.Length - 90f) * Mathf.Deg2Rad;  // 첫 열매는 카메라 쪽
                        float y = crownY - (i % 2) * radius * 0.15f;
                        var spot = new Vector3(Mathf.Cos(a) * radius * FruitCrownRadius, y, Mathf.Sin(a) * radius * FruitCrownRadius);
                        var fruitModel = models ? models.Find(apple ? "FruitApple" : "FruitPeach") : null;
                        visuals[i] = fruitModel != null && fruitModel.model
                            ? FitModel(fruitModel.id, tree, spot - Vector3.up * FruitSize * 0.5f, new Vector3(0f, FruitSize, 0f)).transform  // 열매 모델(P-4a), 가운데가 예전 공 자리
                            : Prim(PrimitiveType.Sphere, "Fruit", tree, spot, Vector3.one * 0.38f, fruit).transform;
                    }

                    var fruitTree = tree.gameObject.AddComponent<FruitTreeBehaviour>();
                    fruitTree.rules = gatheringRules;
                    fruitTree.settings = settings;
                    fruitTree.fruitItem = apple ? appleItem : peachItem;
                    fruitTree.pickupPrefab = pickupPrefab;
                    fruitTree.fruitVisuals = visuals;
                }
            }

            if (inVillage)
            {
                var choppable = tree.gameObject.AddComponent<ChoppableTreeBehaviour>();
                choppable.rules = gatheringRules;
                choppable.settings = settings;
                choppable.woodItem = woodItem;
                choppable.woodMaterial = Mat("Wood", "#B98A5E");
                choppable.pickupPrefab = pickupPrefab;
                choppable.stumpPrefab = stumpPrefab;
            }
        }

        static void BuildRocks(Transform parent, System.Random random)
        {
            var rocks = new GameObject("Rocks").transform;
            rocks.SetParent(parent, false);
            int placed = 0;
            for (int attempt = 0; attempt < 800 && placed < 14; attempt++)
            {
                float x = Range(random, -48f, 48f);
                float z = Range(random, -34f, 48f);
                if (!CanPlaceNature(x, z, 1f))
                    continue;
                Occupied.Add(new Vector3(x, z, 1f));
                float s = Range(random, 0.7f, 1.3f);
                var go = Prim(PrimitiveType.Sphere, "Rock", rocks, OnGround(x, z, 0.1f), new Vector3(1.3f * s, 0.8f * s, 1.1f * s), null, collider: true);
                go.transform.localRotation = Quaternion.Euler(0f, Range(random, 0f, 360f), Range(random, -8f, 8f));
                ColliderOnly(go);
                var model = ModelVisual("Rock", rocks, OnGround(x, z, -0.1f));
                model.transform.localRotation = go.transform.localRotation * model.transform.localRotation;
                model.transform.localScale *= s;
                placed++;
            }
        }

        // 꽃 무리: 무리 중심 칸 + 이웃 칸 중 놓을 수 있는 칸 3~5개에 한 송이씩 (결정 2026-10-27: 칸에 맞춰 배치)
        static readonly Vector2Int[] PatchCells =
        {
            new(0, 0), new(1, 0), new(0, 1), new(-1, 0), new(0, -1), new(1, 1), new(-1, 1), new(1, -1), new(-1, -1),
        };

        static void BuildFlowers(Transform parent, System.Random random, VillageGround ground)
        {
            var flowers = new GameObject("Flowers").transform;
            flowers.SetParent(parent, false);
            var grid = ground.data.CreateGrid();

            int placed = 0;
            for (int attempt = 0; attempt < 1500 && placed < 55; attempt++)
            {
                float x = Range(random, -45f, 45f);
                float z = Range(random, -32f, 46f);
                if (!CanPlaceNature(x, z, 0.8f))
                    continue;
                string look = FlowerModels[placed % FlowerModels.Length];  // 무리마다 한 종류
                int count = random.Next(3, 6);
                Vector2Int middle = grid.ToCell(new Vector2(x, z));
                int planted = 0;
                for (int i = 0; i < PatchCells.Length && planted < count; i++)
                {
                    Vector2Int cell = middle + PatchCells[i];
                    if (!grid.TryPlace(cell, GroundObjectKind.Flower))
                        continue;
                    planted++;
                    Vector2 c = grid.CellCenter(cell);
                    var flower = new GameObject("Flower").transform;
                    flower.SetParent(flowers, false);
                    flower.localPosition = OnGround(c.x, c.y);
                    FlowerVisual(flower, look, random);
                    AddInteractable(flower.gameObject, "꽃", InteractableKind.Flower, radius: 0.15f, markerHeight: 0.75f, wiggle: false);
                    flower.gameObject.AddComponent<Pickup>().item = flowerItem;
                    var gridObject = flower.gameObject.AddComponent<GridObject>();
                    gridObject.ground = ground;
                    gridObject.kind = GroundObjectKind.Flower;
                }
                Occupied.Add(new Vector3(x, z, 0.8f));
                placed++;
            }
        }

        // 꽃 모델 3종(P-3): 모습 이름 = 재질 이름(저장 look)
        static readonly string[] FlowerModels = { "FlowerTulip", "FlowerDaisy", "FlowerPansy" };

        static void FlowerVisual(Transform flower, string id, System.Random random)
        {
            var petals = ModelVisual(id, flower, Vector3.zero, castShadows: false);
            petals.name = "Petals";
            petals.transform.localRotation = Quaternion.Euler(0f, random == null ? 180f : Range(random, 0f, 360f), 0f);
        }

        static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

        // ---------------------------------------------------------------- player & camera

        static GameObject BuildPlayer()
        {
            var player = new GameObject("Player");
            player.layer = 2; // Ignore Raycast: 물 감지 레이캐스트가 자기 자신에 맞지 않도록
            player.transform.position = OnGround(PlazaCenter.x, PlazaCenter.y - 5f, 0.1f);

            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.2f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.6f, 0f);
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 45f;

            // 임시 토끼 모양 플레이어 (나중에 Varco3D 캐릭터로 교체)
            var visual = new GameObject("Visual").transform;
            visual.SetParent(player.transform, false);
            var fur = Mat("PlayerFur", "#FFF7EE", rim: 0.25f);
            var pink = Mat("PlayerPink", "#FFB7C8", rim: 0.25f);
            Prim(PrimitiveType.Capsule, "Body", visual, new Vector3(0f, 0.4f, 0f), new Vector3(0.6f, 0.4f, 0.55f), Mat("PlayerShirt", "#8CC8F0"));
            Prim(PrimitiveType.Sphere, "Head", visual, new Vector3(0f, 0.95f, 0f), new Vector3(0.75f, 0.7f, 0.7f), fur);
            Prim(PrimitiveType.Capsule, "Ear_L", visual, new Vector3(-0.16f, 1.45f, -0.02f), new Vector3(0.14f, 0.3f, 0.1f), fur).transform.localRotation = Quaternion.Euler(0f, 0f, 12f);
            Prim(PrimitiveType.Capsule, "Ear_R", visual, new Vector3(0.16f, 1.45f, -0.02f), new Vector3(0.14f, 0.3f, 0.1f), fur).transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
            Prim(PrimitiveType.Sphere, "Nose", visual, new Vector3(0f, 0.92f, 0.35f), Vector3.one * 0.09f, pink);
            Prim(PrimitiveType.Sphere, "Eye_L", visual, new Vector3(-0.15f, 1.02f, 0.31f), Vector3.one * 0.08f, Mat("Eye", "#3A3340"));
            Prim(PrimitiveType.Sphere, "Eye_R", visual, new Vector3(0.15f, 1.02f, 0.31f), Vector3.one * 0.08f, Mat("Eye", "#3A3340"));

            var pc = player.AddComponent<PlayerController>();
            pc.settings = settings;
            pc.visual = visual;
            pc.animator = BuildPlayerModel(visual);

            var interactor = player.AddComponent<PlayerInteractor>();
            interactor.settings = settings;
            BuildInteractionMarker(interactor);
            var inventory = player.AddComponent<PlayerInventory>();
            inventory.rules = inventoryRules;
            BuildPlayerTools(player, visual, inventory, interactor);
            player.AddComponent<PlayerWallet>().shop = shopData;
            return player;
        }

        // 플레이어 모델(P-4f): Mixamo 리깅 Humanoid + 서 있기 · 걷기 · 달리기를 이동 속도로 섞는 애니메이터. 그레이박스 토끼는 지움.
        // 모델이 없으면 null(토끼 그대로, 통통 튀기)
        static Animator BuildPlayerModel(Transform visual)
        {
            var entry = models ? models.Find("Player") : null;
            var controller = EnsurePlayerAnimator();
            if (entry == null || !entry.model || !controller)
                return null;
            foreach (Transform part in visual.Cast<Transform>().ToArray())
                Object.DestroyImmediate(part.gameObject);
            return RigModel(entry.model, entry, visual, controller);
        }

        // 리깅 모델을 parent 아래에 세움(플레이어 · 주민): CurvedToon 재질, 발바닥이 0 · 키 entry.height, 애니메이터(뿌리 움직임 끔)
        static Animator RigModel(GameObject source, ModelLibrary.Entry entry, Transform parent, RuntimeAnimatorController controller)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            model.name = "Model_" + entry.id;
            var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
            skin.sharedMaterial = entry.material;
            skin.updateWhenOffscreen = true;  // 휘어진 화면에서 경계 상자로 잘려 사라지지 않게
            var baked = new Mesh();
            skin.BakeMesh(baked, true);
            model.transform.localRotation = Quaternion.Euler(0f, entry.yaw, 0f);
            model.transform.localScale = Vector3.one * (entry.height / Mathf.Max(baked.bounds.size.y, 0.01f));
            model.transform.localPosition = Vector3.down * baked.bounds.min.y * model.transform.localScale.y;
            Object.DestroyImmediate(baked);
            var animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            return animator;
        }

        // 플레이어 애니메이터(처음 한 번만 만듦): Speed(m/s)로 서 있기(0) · 걷기(walkSpeed) · 달리기(runSpeed) 블렌드
        static RuntimeAnimatorController EnsurePlayerAnimator()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerAnimatorPath);
            if (existing)
                return existing;
            var clips = VarcoModelImport.PlayerClips
                .Select(c => AssetDatabase.LoadAllAssetsAtPath(VarcoModelImport.PlayerClipPath(c)).OfType<AnimationClip>().FirstOrDefault(a => !a.name.StartsWith("__")))
                .ToArray();
            if (clips.Any(c => !c))
                return null;
            var animator = AnimatorController.CreateAnimatorControllerAtPath(PlayerAnimatorPath);
            animator.AddParameter("Speed", AnimatorControllerParameterType.Float);
            animator.CreateBlendTreeInController("Move", out var tree);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(clips[0], 0f);
            tree.AddChild(clips[1], settings.walkSpeed);
            tree.AddChild(clips[2], settings.runSpeed);
            AssetDatabase.SaveAssets();
            return animator;
        }

        // 손에 든 도구(그레이박스). 축은 오른손 위치, 휘두르면 앞으로 꺾임
        static void BuildPlayerTools(GameObject player, Transform visual, PlayerInventory inventory, PlayerInteractor interactor)
        {
            var pivot = new GameObject("ToolPivot").transform;
            pivot.SetParent(visual, false);
            pivot.localPosition = new Vector3(0.38f, 0.55f, 0.05f);
            pivot.localRotation = Quaternion.Euler(-35f, 0f, 0f);

            var handle = Mat("ToolHandle", "#B98A5E");
            var metal = Mat("ToolMetal", "#C9D2DC", rim: 0.3f);
            GameObject Tool(string name) { var go = new GameObject(name); go.transform.SetParent(pivot, false); return go; }

            var net = Tool("Net");
            Prim(PrimitiveType.Cylinder, "Handle", net.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.05f, 0.5f, 0.05f), handle, castShadows: false);
            Prim(PrimitiveType.Sphere, "Hoop", net.transform, new Vector3(0f, 1.1f, 0f), new Vector3(0.4f, 0.4f, 0.1f), Mat("ToolNet", "#F4F1EA", rim: 0.3f), castShadows: false);

            var rod = Tool("FishingRod");
            Prim(PrimitiveType.Cylinder, "Rod", rod.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.035f, 0.6f, 0.035f), handle, castShadows: false);
            Prim(PrimitiveType.Sphere, "Reel", rod.transform, new Vector3(0f, 0.2f, 0.06f), Vector3.one * 0.12f, metal, castShadows: false);

            var axe = Tool("Axe");
            Prim(PrimitiveType.Cylinder, "Handle", axe.transform, new Vector3(0f, 0.35f, 0f), new Vector3(0.06f, 0.35f, 0.06f), handle, castShadows: false);
            Prim(PrimitiveType.Cube, "Head", axe.transform, new Vector3(0f, 0.65f, 0.1f), new Vector3(0.06f, 0.2f, 0.25f), metal, castShadows: false);

            var shovel = Tool("Shovel");
            Prim(PrimitiveType.Cylinder, "Handle", shovel.transform, new Vector3(0f, 0.4f, 0f), new Vector3(0.05f, 0.4f, 0.05f), handle, castShadows: false);
            Prim(PrimitiveType.Cube, "Blade", shovel.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.25f, 0.3f, 0.04f), metal, castShadows: false);

            var can = Tool("WateringCan");
            Prim(PrimitiveType.Cylinder, "Body", can.transform, new Vector3(0f, 0.15f, 0.1f), new Vector3(0.25f, 0.15f, 0.25f), Mat("ToolCan", "#7FB8E6", rim: 0.3f), castShadows: false);
            Prim(PrimitiveType.Cylinder, "Spout", can.transform, new Vector3(0f, 0.22f, 0.3f), new Vector3(0.04f, 0.15f, 0.04f), metal, castShadows: false)
                .transform.localRotation = Quaternion.Euler(60f, 0f, 0f);

            var holds = new[] { net, rod, axe, shovel, can };
            foreach (var hold in holds)
            {
                HoldToolModel(hold);
                hold.SetActive(false);
            }

            var playerTools = player.AddComponent<PlayerTools>();
            playerTools.settings = settings;
            playerTools.inventory = inventory;
            playerTools.interactor = interactor;
            playerTools.tools = tools;
            playerTools.holdVisuals = holds;
            playerTools.swingPivot = pivot;
            // 리깅 모델이면 도구 축이 손바닥을 따라감(P-4k), 없으면(토끼) 몸 옆 고정
            var hand = FindBone(visual, RightHandBone);
            var palm = FindBone(visual, RightPalmBone);
            if (hand && palm)
            {
                playerTools.handBone = hand;
                playerTools.handGrip = hand.InverseTransformPoint(palm.position);
            }
        }

        const string RightHandBone = "mixamorig:RightHand";
        const string RightPalmBone = "mixamorig:RightHandMiddle1";
        // 손이 쥐는 자리(P-4k): 도구 아래 끝에서 길이의 몇 배 위. 축이 손에 오도록 도구를 그만큼 내림
        static readonly Dictionary<string, float> ToolGrip = new()
        {
            ["Net"] = 0.1f,
            ["FishingRod"] = 0.12f,
            ["Axe"] = 0.2f,
            ["Shovel"] = 0.35f,
            ["WateringCan"] = 0.7f,
        };

        static Transform FindBone(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == name);

        // 손에 든 도구도 도구 모델(P-4h): 그레이박스와 같은 길이로(손잡이 아래가 축), 누운 모델은 ToolPitch로 세움
        static void HoldToolModel(GameObject hold)
        {
            string id = "Tool" + hold.name;
            var entry = models ? models.Find(id) : null;
            if (entry == null || !entry.model)
                return;
            float length = 0f;  // 그레이박스 도구의 길이(도구 축 기준 위쪽 끝)
            foreach (var filter in hold.GetComponentsInChildren<MeshFilter>())
            {
                var toHold = hold.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                var b = filter.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var c = new Vector3((corner & 1) == 0 ? b.min.x : b.max.x, (corner & 2) == 0 ? b.min.y : b.max.y, (corner & 4) == 0 ? b.min.z : b.max.z);
                    length = Mathf.Max(length, toHold.MultiplyPoint3x4(c).y);
                }
            }
            foreach (Transform part in hold.transform.Cast<Transform>().ToArray())
                Object.DestroyImmediate(part.gameObject);
            var fit = FitModel(id, hold.transform, Vector3.zero, new Vector3(0f, length, 0f), pitch: ToolPitch.TryGetValue(id, out float pitch) ? pitch : 0f);
            hold.transform.localPosition = Vector3.down * (length * ToolGrip[hold.name]);
            foreach (var r in fit.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = ShadowCastingMode.Off;
        }

        static void BuildInteractionMarker(PlayerInteractor interactor)
        {
            var marker = new GameObject("InteractionMarker").AddComponent<InteractionMarker>();
            marker.settings = settings;
            marker.interactor = interactor;
            var ball = Prim(PrimitiveType.Sphere, "Ball", marker.transform, Vector3.zero, Vector3.one, Mat("Marker", "#FFD86B", rim: 0.4f), castShadows: false);
            ball.SetActive(false); // 대상이 생기면 InteractionMarker가 켬
        }

        static void AddInteractable(GameObject go, string displayName, InteractableKind kind, float radius, float markerHeight, bool wiggle = true)
        {
            var interactable = go.AddComponent<Interactable>();
            interactable.displayName = displayName;
            interactable.kind = kind;
            interactable.radius = radius;
            interactable.markerHeight = markerHeight;
            interactable.wiggleOnInteract = wiggle;
        }

        // ---------------------------------------------------------------- gathering data

        // ---------------------------------------------------------------- ground grid

        // 막힌 칸 굽기: 물(물 높이 근처 이하) · 길 · 광장 · 충돌체(집 · 나무 줄기 · 바위 · 다리 · 가로등 · 분수 · 게시판 · 도구 상자)
        const float PathBlockDistance = 1.2f;     // 길 중심선에서 이 거리 안의 칸 중심 = 길
        const float WaterBlockMargin = 0.1f;      // 물 높이 + 이 값 아래 = 물
        const float FishWaterDepth = 0.3f;        // 물 높이 - 이 값 아래 = 낚시하는 물(3-6a)

        static VillageGround BuildGroundGrid(Transform world)
        {
            var data = EnsureAsset<GroundGridData>(GroundGridPath);
            Collider groundCollider = world.Find("Ground").GetComponent<Collider>();
            Physics.SyncTransforms();

            float size = data.cellSize;
            var origin = new Vector2Int(Mathf.FloorToInt(-GroundHalfSize / size), Mathf.FloorToInt(-GroundHalfSize / size));
            int count = Mathf.CeilToInt(GroundHalfSize * 2 / size);
            var blocked = new bool[count * count];
            var hits = new Collider[8];
            var halfExtents = new Vector3(size * 0.45f, 1.9f, size * 0.45f);  // 위아래로 넉넉히: 비탈에서는 칸 가운데 높이와 줄기 밑동 높이가 다름

            for (int j = 0; j < count; j++)
            for (int i = 0; i < count; i++)
            {
                float x = (origin.x + i + 0.5f) * size;
                float z = (origin.y + j + 0.5f) * size;
                float h = Height(x, z);
                bool isBlocked = h < settings.waterLevel + WaterBlockMargin
                                 || PathDistance(x, z) < PathBlockDistance
                                 || PlazaMask(x, z) > 0.01f;
                if (!isBlocked)
                {
                    int n = Physics.OverlapBoxNonAlloc(new Vector3(x, h + 1f, z), halfExtents, hits, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                    for (int k = 0; k < n && !isBlocked; k++)
                        isBlocked = hits[k] != groundCollider;
                }
                blocked[j * count + i] = isBlocked;
            }

            data.Bake(origin, count, count, blocked);

            // 낚시할 수 있는 물(3-6a): 강만 — 물 높이보다 충분히 깊은 칸, 남쪽 바닷가는 빼고(바다 낚시는 나중)
            var water = new bool[count * count];
            for (int j = 0; j < count; j++)
            for (int i = 0; i < count; i++)
            {
                float x = (origin.x + i + 0.5f) * size;
                float z = (origin.y + j + 0.5f) * size;
                water[j * count + i] = Height(x, z) < settings.waterLevel - FishWaterDepth && BeachMask(z) < 0.01f;
            }
            data.BakeWater(water);
            EditorUtility.SetDirty(data);

            var ground = new GameObject("GroundGrid").AddComponent<VillageGround>();
            ground.transform.SetParent(world, false);
            ground.data = data;
            var placer = ground.gameObject.AddComponent<GroundPlacer>();
            placer.holePrefab = BuildHolePrefab();
            placer.saplingPrefab = BuildSaplingPrefab();
            placer.flowerPrefab = BuildPlantedFlowerPrefab();
            var keeper = ground.gameObject.AddComponent<GardenKeeper>();
            keeper.rules = gatheringRules;
            keeper.settings = settings;
            keeper.grownTreePrefab = BuildGrownTreePrefab();
            var morning = new GameObject("MorningRunner").AddComponent<MorningRunner>();
            morning.garden = keeper;
            return ground;
        }

        // ---------------------------------------------------------------- HUD (2-3)

        // 한글 TMP 글꼴(동적 아틀라스: 쓰는 글자만 그때그때 채움). 없을 때만 만듦
        static TMP_FontAsset EnsureFontAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing)
                return existing;
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontSourcePath);
            if (!font)
            {
                Debug.LogError($"{FontSourcePath} 글꼴이 없습니다.");
                return null;
            }
            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = "Cafe24Ssurround SDF";
            AssetDatabase.CreateAsset(asset, FontAssetPath);
            asset.material.name = asset.name + " Material";
            asset.atlasTexture.name = asset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        static TMP_Text HudLabel(Transform parent, string name, TMP_FontAsset font, float size, TextAlignmentOptions align,
            Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 boxSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = boxSize;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.alignment = align;
            text.color = Hex("#4A3F55");
            text.raycastTarget = false;  // 외곽선은 쓰지 않음: 편집 중 외곽선을 바꾸면 저장 안 되는 재질 복사본이 생김 → 받침판으로 대신
            return text;
        }

        // 글자 뒤 반투명 받침판(읽기 쉽게)
        static void HudPlate(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 0.98f, 0.94f, 0.82f);
            image.raycastTarget = false;
        }

        static DialogueWindow BuildHud(GameObject player)
        {
            var font = EnsureFontAsset();
            var canvasGo = new GameObject("HUD", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.transform;

            canvasGo.AddComponent<RuntimeFont>().sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontSourcePath);
            var hud = canvasGo.AddComponent<HudView>();
            hud.settings = settings;
            hud.dayCycle = Object.FindAnyObjectByType<DayCycle>();
            hud.wallet = player.GetComponent<PlayerWallet>();
            hud.tools = player.GetComponent<PlayerTools>();
            hud.inventory = player.GetComponent<PlayerInventory>();
            HudPlate(root, "ClockPlate", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -18f), new Vector2(380f, 140f));
            HudPlate(root, "ToolPlate", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 18f), new Vector2(360f, 76f));
            hud.clockText = HudLabel(root, "Clock", font, 44, TextAlignmentOptions.TopRight, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(600f, 60f));
            hud.moaText = HudLabel(root, "Moa", font, 40, TextAlignmentOptions.TopRight, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -92f), new Vector2(600f, 56f));
            hud.toolText = HudLabel(root, "HeldTool", font, 38, TextAlignmentOptions.BottomLeft, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 30f), new Vector2(700f, 56f));
            HudPlate(root, "Notice", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(1100f, 72f));
            var noticePlate = root.Find("Notice");
            hud.noticeRoot = noticePlate.gameObject;
            hud.noticeText = HudLabel(noticePlate, "Text", font, 40, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1080f, 64f));

            // 인벤토리 창: 가운데 5열 × 4줄
            var panel = new GameObject("InventoryPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root, false);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
            const float cell = 190f, gap = 14f;
            int slotCount = inventoryRules.slotCount;
            int rows = (slotCount + InventoryWindow.Columns - 1) / InventoryWindow.Columns;
            prt.sizeDelta = new Vector2(InventoryWindow.Columns * (cell + gap) + gap, rows * (cell * 0.5f + gap) + gap + 70f);
            panel.GetComponent<Image>().color = new Color(1f, 0.97f, 0.92f, 0.92f);
            var title = HudLabel(panel.transform, "Title", font, 40, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(800f, 56f));
            title.text = "인벤토리";

            var backgrounds = new Image[slotCount];
            var texts = new TMP_Text[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                int col = i % InventoryWindow.Columns, row = i / InventoryWindow.Columns;
                var slotGo = new GameObject("Slot" + i, typeof(RectTransform), typeof(Image));
                slotGo.transform.SetParent(panel.transform, false);
                var srt = (RectTransform)slotGo.transform;
                srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0f, 1f);
                srt.anchoredPosition = new Vector2(gap + col * (cell + gap), -(70f + row * (cell * 0.5f + gap)));
                srt.sizeDelta = new Vector2(cell, cell * 0.5f);
                backgrounds[i] = slotGo.GetComponent<Image>();
                texts[i] = HudLabel(slotGo.transform, "Label", font, 28, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(cell - 10f, cell * 0.5f - 6f));
            }

            var window = canvasGo.AddComponent<InventoryWindow>();
            window.settings = settings;
            window.catalogSource = shopData;
            window.inventory = player.GetComponent<PlayerInventory>();
            // 창이 열린 동안 플레이어 조작 멈춤(2-5: 인벤토리 · 대화 · 선물 고르기가 함께 씀)
            var playerLock = player.AddComponent<PlayerLock>();
            playerLock.controlled = new Behaviour[] { player.GetComponent<PlayerInteractor>(), player.GetComponent<PlayerTools>() };
            playerLock.movement = player.GetComponent<PlayerController>();  // 가구 놓기 모드는 걷기 허용(3-4a)
            window.playerLock = playerLock;
            window.titleText = title;
            window.panel = panel;
            window.slotBackgrounds = backgrounds;
            window.slotTexts = texts;

            // 대화창(2-4): 화면 아래 이름 + 대사
            var talkPanel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image));
            talkPanel.transform.SetParent(root, false);
            var trt = (RectTransform)talkPanel.transform;
            trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0.5f, 0f);
            trt.anchoredPosition = new Vector2(0f, 110f);  // 왼쪽 아래 든 도구 받침판 위로
            trt.sizeDelta = new Vector2(1300f, 230f);
            talkPanel.GetComponent<Image>().color = new Color(1f, 0.97f, 0.92f, 0.95f);
            var talk = canvasGo.AddComponent<DialogueWindow>();
            talk.panel = talkPanel;
            talk.speakerText = HudLabel(talkPanel.transform, "Speaker", font, 40, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -20f), new Vector2(800f, 56f));
            talk.speakerText.color = Hex("#C0607A");
            talk.lineText = HudLabel(talkPanel.transform, "Line", font, 40, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -86f), new Vector2(1220f, 130f));
            talk.choiceText = HudLabel(talkPanel.transform, "Choices", font, 40, TextAlignmentOptions.TopRight, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -20f), new Vector2(320f, 130f));
            talk.hintText = HudLabel(talkPanel.transform, "Hint", font, 28, TextAlignmentOptions.BottomRight, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 16f), new Vector2(400f, 40f));
            talk.hintText.text = "F 다음";  // ▶ 같은 기호는 글꼴에 없음
            talk.playerLock = playerLock;
            return talk;
        }

        // ---------------------------------------------------------------- villagers (2-4)

        // 주민 20명(임시 이름 · 색 · 공통 대사). 없을 때만 만들고, 있으면 값을 건드리지 않음(이름 · 대사는 에셋에서 고침)
        static VillagerDefinition[] EnsureVillagers()
        {
            EnsureFolder(VillagerDir);
            var all = new VillagerDefinition[20];
            for (int i = 0; i < all.Length; i++)
            {
                string id = $"Villager{i + 1:00}";
                string path = $"{VillagerDir}/{id}.asset";
                bool isNew = !AssetDatabase.LoadAssetAtPath<VillagerDefinition>(path);
                var v = EnsureAsset<VillagerDefinition>(path);
                if (isNew)
                {
                    float hue = (i * 7 % 20) / 20f;  // 이웃 번호끼리 색이 비슷하지 않게 섞음
                    v.id = id;
                    v.displayName = $"주민 {i + 1:00}";
                    v.species = "미정";
                    v.bodyColor = Color.HSVToRGB(hue, 0.28f, 0.98f);
                    v.accentColor = Color.HSVToRGB(hue, 0.5f, 0.82f);
                    v.morningLines = new[] { "좋은 아침! 오늘도 포근한 하루가 될 것 같아.", "아침 공기가 상쾌하다~" };
                    v.daytimeLines = new[] { "안녕! 산책하기 딱 좋은 날씨야.", "어디 가는 길이야?" };
                    v.eveningLines = new[] { "벌써 저녁이네. 오늘 하루는 어땠어?", "노을이 참 예쁘다." };
                    v.smallTalk = new[]
                    {
                        "광장 분수 소리를 들으면 마음이 편해져.",
                        "강가에 꽃이 많이 피었더라.",
                        "묘목을 심으면 언젠가 큰 나무가 된대.",
                        "나무를 흔들면 열매가 떨어지는 거 알아?",
                    };
                    EditorUtility.SetDirty(v);
                }
                all[i] = v;
            }
            return all;
        }

        // 주민별 집 모델(P-3b): 주민 n = House n(Varco3D 플로우 순서, MODELING_SPEC). 프리팹은 매번 다시 만들고, 주민 데이터에는 비어 있을 때만 연결
        static void BuildHousePrefabs(VillagerDefinition[] villagers)
        {
            EnsureFolder(PrefabDir + "/Houses");
            for (int i = 0; i < villagers.Length; i++)
            {
                string id = $"House{i + 1:00}";
                if (models.Find(id) == null)
                    continue;
                var root = ModelVisual(id, null, Vector3.zero);
                root.name = id;
                var prefab = SavePrefab(root, "Houses/" + id);
                if (!villagers[i].houseModel)
                {
                    villagers[i].houseModel = prefab;
                    EditorUtility.SetDirty(villagers[i]);
                }
            }
        }

        // 주민 모델(P-3c): 주민 n = Villager n(Varco3D 플로우 순서). 종족은 그 모델(플로우 글)에 맞춰 — 비어 있거나 '미정'일 때만 채움
        static readonly string[] ModelSpecies =
        {
            "토끼", "곰", "고양이", "여우", "너구리", "양", "고슴도치", "사슴", "부엉이", "오리",
            "돼지", "다람쥐", "개구리", "펭귄", "강아지", "판다", "생쥐", "수달", "햄스터", "카피바라",
        };

        static void BuildVillagerModels(VillagerDefinition[] villagers)
        {
            EnsureFolder(PrefabDir + "/Villagers");
            for (int i = 0; i < villagers.Length; i++)
            {
                var v = villagers[i];
                bool changed = false;
                if ((string.IsNullOrEmpty(v.species) || v.species == "미정") && i < ModelSpecies.Length)
                {
                    v.species = ModelSpecies[i];
                    changed = true;
                }
                string id = $"Villager{i + 1:00}";
                var entry = models.Find(id);
                bool canWave = id != "Villager01";  // 토끼: 자동 리깅이 긴 귀를 팔 뼈에 묶어 팔을 들면 귀가 늘어남(P-5f)
                if (v.canWave != canWave)
                {
                    v.canWave = canWave;
                    changed = true;
                }
                if (entry != null)
                {
                    var rig = AssetDatabase.LoadAssetAtPath<GameObject>(VarcoModelImport.RigPath(id));
                    var controller = rig ? EnsureVillagerAnimator(id) : null;
                    GameObject root;
                    if (controller)
                    {
                        root = new GameObject(id);  // 리깅 모델(P-4j): 서 있기 · 걷기를 걷는 속도로 섞음
                        RigModel(rig, entry, root.transform, controller);
                    }
                    else
                    {
                        root = ModelVisual(id, null, Vector3.zero);  // 정적 모델(P-3c)
                        root.name = id;
                    }
                    var prefab = SavePrefab(root, "Villagers/" + id);
                    if (!v.model)
                    {
                        v.model = prefab;
                        changed = true;
                    }
                }
                if (changed)
                    EditorUtility.SetDirty(v);
            }
        }

        // 주민 애니메이터(P-4j): 공통 틀 VillagerAnimator(Speed m/s: 0 서 있기 · 걷는 속도 걷기, 처음 한 번 Villager01 동작으로 만듦)
        // + 주민마다 자기 동작으로 바꾼 덮어쓰기 컨트롤러(VillagerAnimators/주민.overrideController). 동작이 없으면 null
        static RuntimeAnimatorController EnsureVillagerAnimator(string id)
        {
            var idle = VarcoModelImport.LoadClip(id, "Idle");
            var walk = VarcoModelImport.LoadClip(id, "Walk");
            if (!idle || !walk)
                return null;
            var baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(VillagerAnimatorPath);
            if (!baseController)
            {
                baseController = AnimatorController.CreateAnimatorControllerAtPath(VillagerAnimatorPath);
                baseController.AddParameter("Speed", AnimatorControllerParameterType.Float);
                baseController.CreateBlendTreeInController("Move", out var tree);
                tree.blendParameter = "Speed";
                tree.useAutomaticThresholds = false;
                tree.AddChild(idle, 0f);
                tree.AddChild(walk, EnsureAsset<VillageRules>(VillageRulesPath).walkSpeed);
                AssetDatabase.SaveAssets();
            }
            EnsureFolder(VillagerAnimatorDir);
            string path = $"{VillagerAnimatorDir}/{id}.overrideController";
            var overrides = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (!overrides)
            {
                overrides = new AnimatorOverrideController(baseController);
                AssetDatabase.CreateAsset(overrides, path);
            }
            overrides.runtimeAnimatorController = baseController;
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrides.GetOverrides(pairs);  // 틀의 클립 이름: Idle · Walk(동작 파일 이름의 @ 뒤)
            overrides.ApplyOverrides(pairs.Select(p => new KeyValuePair<AnimationClip, AnimationClip>(p.Key, p.Key.name == walk.name ? walk : idle)).ToList());
            EditorUtility.SetDirty(overrides);
            return overrides;
        }

        // 주민 그레이박스(동그란 몸 + 머리 + 귀). 색은 VillagerBehaviour가 프로퍼티 블록으로
        static VillagerBehaviour BuildVillagerPrefab()
        {
            var root = new GameObject("Villager");
            root.layer = 2;  // 물 감지 · 순간이동 레이캐스트가 주민 머리에 맞지 않도록(플레이어와 같음)
            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.3f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.65f, 0f);
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 45f;

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            var body = Mat("VillagerBody", "#FFFFFF", rim: 0.25f);
            var eye = Mat("Eye", "#3A3340");
            var b = Prim(PrimitiveType.Capsule, "Body", visual, new Vector3(0f, 0.42f, 0f), new Vector3(0.62f, 0.42f, 0.58f), body);
            var h = Prim(PrimitiveType.Sphere, "Head", visual, new Vector3(0f, 1.02f, 0f), new Vector3(0.8f, 0.72f, 0.72f), body);
            var el = Prim(PrimitiveType.Sphere, "Ear_L", visual, new Vector3(-0.26f, 1.38f, 0f), new Vector3(0.26f, 0.26f, 0.14f), body);
            var er = Prim(PrimitiveType.Sphere, "Ear_R", visual, new Vector3(0.26f, 1.38f, 0f), new Vector3(0.26f, 0.26f, 0.14f), body);
            Prim(PrimitiveType.Sphere, "Eye_L", visual, new Vector3(-0.15f, 1.08f, 0.33f), Vector3.one * 0.08f, eye);
            Prim(PrimitiveType.Sphere, "Eye_R", visual, new Vector3(0.15f, 1.08f, 0.33f), Vector3.one * 0.08f, eye);

            AddInteractable(root, "주민", InteractableKind.Villager, radius: 0.4f, markerHeight: 1.9f, wiggle: false);
            var villager = root.AddComponent<VillagerBehaviour>();
            villager.bodyRenderers = new Renderer[] { b.GetComponent<Renderer>(), h.GetComponent<Renderer>() };
            villager.accentRenderers = new Renderer[] { el.GetComponent<Renderer>(), er.GetComponent<Renderer>() };
            villager.greyboxRenderers = visual.GetComponentsInChildren<Renderer>();
            villager.visual = visual;
            return SavePrefab(root, "Villager").GetComponent<VillagerBehaviour>();
        }

        // 캠핑 구역: 그레이박스 텐트(삼각 지붕 모양) + 입구 천 + 모닥불. 집 대신 HousePlot을 써서 캠핑 주민이 입구에서 나오고 들어감(집 · 표지판 없음)
        static HousePlot BuildCamp(Transform parent)
        {
            var camp = new GameObject("CampSite").transform;
            camp.SetParent(parent, false);
            camp.localPosition = OnGround(CampPosition.x, CampPosition.y, -0.05f);
            Vector2 forward = PlazaCenter - CampPosition;
            camp.localRotation = Quaternion.LookRotation(new Vector3(forward.x, 0f, forward.y));
            Occupied.Add(new Vector3(CampPosition.x, CampPosition.y, 4f));

            // 지붕 메시는 용마루가 X축 → 90° 돌려 삼각 박공이 입구(+Z)를 보게 함. 길이(로컬 X) 3.2 → 앞뒤 ±1.6
            var tent = MeshObject("Tent", camp, RoofMesh(), Mat("Tent", "#F4B9A0", rim: 0.2f));
            tent.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            tent.transform.localScale = new Vector3(3.2f, 2.2f, 2.8f);
            tent.AddComponent<BoxCollider>();
            ColliderOnly(tent);
            // 텐트 + 모닥불 모델(P-4f): 모델 안에서 텐트가 +x 쪽이라 그만큼 옮겨 텐트를 가운데에, 모닥불은 -x 앞쪽
            FitModel("CampTent", camp, new Vector3(-CampTentOffset, 0f, 0f), new Vector3(0f, 2.3f, 0f));

            var door = new GameObject("Door").transform;
            door.SetParent(camp, false);
            door.localPosition = new Vector3(0f, 0.05f, 2.4f);

            var site = camp.gameObject.AddComponent<HousePlot>();
            site.door = door;
            site.showsResidentHouse = false;  // 캠핑은 텐트 그대로(P-3b)
            return site;
        }

        // 마을 평가(3-5): HUD 시계 · 모아 아래 "마을 ★☆☆☆☆", 광장 게시판 F = 항목별 점수
        static void BuildRating(VillageGround ground, DialogueWindow dialogue)
        {
            var canvas = dialogue.transform;
            ((RectTransform)canvas.Find("ClockPlate")).sizeDelta = new Vector2(380f, 200f);
            var label = HudLabel(canvas, "Rating", EnsureFontAsset(), 40, TextAlignmentOptions.TopRight, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-40f, -150f), new Vector2(600f, 56f));

            var rater = new GameObject("VillageRater").AddComponent<VillageRater>();
            rater.rules = EnsureAsset<RatingRules>(RatingRulesPath);
            rater.village = Object.FindAnyObjectByType<Village>();
            rater.ground = ground;
            rater.garden = ground.GetComponent<GardenKeeper>();
            rater.morning = Object.FindAnyObjectByType<MorningRunner>();
            rater.noticeBoard = GameObject.Find("NoticeBoard").GetComponent<Interactable>();
            rater.dialogue = dialogue;
            rater.ratingText = label;
        }

        static void BuildVillage(HousePlot[] plots, HousePlot campSite, VillageGround ground, DialogueWindow dialogue)
        {
            var rules = EnsureAsset<VillageRules>(VillageRulesPath);
            rules.villagers = EnsureVillagers();
            rules.requestItems = new ItemDefinition[] { appleItem, peachItem, flowerItem, woodItem }
                .Concat(fish.Where(f => f.id == "Medaka" || f.id == "CrucianCarp"))
                .Concat(insects.Where(i => i.id == "CabbageWhite" || i.id == "Grasshopper")).ToArray();  // 부탁할 수 있는 물건(3-3), 흔한 물고기 2종(3-6c) · 곤충 2종(3-7c)
            EditorUtility.SetDirty(rules);

            var village = new GameObject("Village").AddComponent<Village>();
            village.rules = rules;
            village.settings = settings;
            village.dayCycle = Object.FindAnyObjectByType<DayCycle>();
            village.ground = ground;
            village.dialogue = dialogue;
            village.inventoryWindow = dialogue.GetComponent<InventoryWindow>();
            village.playerInventory = Object.FindAnyObjectByType<PlayerInventory>();
            village.wallet = Object.FindAnyObjectByType<PlayerWallet>();
            village.plots = plots;
            village.campSite = campSite;
            village.villagerPrefab = BuildVillagerPrefab();
        }

        // 주민이 걸을 수 있는 칸 굽기: 위에서 내려 쏜 맨 위가 지면(물 · 가파른 비탈 아님) 또는 다리 바닥이고, 그 위에 다른 충돌체가 없으면 걸을 수 있음.
        // 길 · 광장 · 다리는 걸을 수 있음(놓기 막힘과 다름). 집터의 집은 모두 켜 둔 채 구워 빈 집터도 피함
        const float WalkMaxSlope = 0.9f;

        static void BakeWalkable(Transform world, VillageGround ground)
        {
            var data = ground.data;
            Collider groundCollider = world.Find("Ground").GetComponent<Collider>();
            Physics.SyncTransforms();

            float size = data.cellSize;
            var origin = new Vector2Int(Mathf.FloorToInt(-GroundHalfSize / size), Mathf.FloorToInt(-GroundHalfSize / size));
            int count = Mathf.CeilToInt(GroundHalfSize * 2 / size);
            var walkable = new bool[count * count];
            var hits = new Collider[8];
            var halfExtents = new Vector3(size * 0.45f, 0.6f, size * 0.45f);
            int mask = ~(1 << 2);

            for (int j = 0; j < count; j++)
            for (int i = 0; i < count; i++)
            {
                float x = (origin.x + i + 0.5f) * size;
                float z = (origin.y + j + 0.5f) * size;
                if (!Physics.Raycast(new Vector3(x, 40f, z), Vector3.down, out RaycastHit top, 80f, mask, QueryTriggerInteraction.Ignore))
                    continue;
                bool ok = top.collider == groundCollider
                    ? top.point.y >= settings.waterLevel + WaterBlockMargin && Slope(x, z) < WalkMaxSlope
                    : top.collider.name == "Deck";
                if (ok)
                {
                    int n = Physics.OverlapBoxNonAlloc(new Vector3(x, top.point.y + 0.75f, z), halfExtents, hits, Quaternion.identity, mask, QueryTriggerInteraction.Ignore);
                    for (int k = 0; k < n && ok; k++)
                        ok = hits[k] == groundCollider || hits[k] == top.collider;
                }
                walkable[j * count + i] = ok;
            }

            data.BakeWalkable(walkable);
            EditorUtility.SetDirty(data);
        }

        // 상점(2-2): 광장 서쪽 노점 — 지붕 · 상점 주인 · 매대 6개(도구 5종 · 묘목) · 판매함. 광장 쪽(동쪽)을 봄
        static void BuildShop(Transform props)
        {
            shopData.catalog = new[] { appleItem, peachItem, flowerItem, woodItem, flowerClumpItem, saplingItem }
                .Concat(furniture)
                .Concat(themeFurniture)
                .Concat(fish)
                .Concat(insects)
                .Concat(tools).ToArray();
            EditorUtility.SetDirty(shopData);

            var stall = new GameObject("Shop").transform;
            stall.SetParent(props, false);
            stall.localPosition = OnGround(PlazaCenter.x - 4.8f, PlazaCenter.y);
            stall.localRotation = Quaternion.Euler(0f, 90f, 0f);  // 앞(+z)이 동쪽 = 광장

            var wood = Mat("Wood", "#B98A5E");
            // 노점 모델(P-4f): 매대 + 차양. 매대 뒤(-z)에 주인, 매대 위에 물건
            FitModel("ShopStall", stall, new Vector3(0f, 0f, -0.75f), new Vector3(5.6f, 2.6f, 1.4f), alignBack: true);

            var keeper = new GameObject("ShopKeeper").transform;
            keeper.SetParent(stall, false);
            keeper.localPosition = new Vector3(0f, 0f, -1.2f);
            ColliderOnly(Prim(PrimitiveType.Capsule, "Body", keeper, new Vector3(0f, 0.55f, 0f), new Vector3(0.6f, 0.55f, 0.55f), wood, collider: true));
            FitModel("Shopkeeper", keeper, Vector3.zero, new Vector3(0f, 1.6f, 0f));  // 나무늘보(P-4f, 정적 자세)
            AddInteractable(keeper.gameObject, "상점 주인", InteractableKind.ShopKeeper, radius: 0.4f, markerHeight: 1.9f);

            var shop = stall.gameObject.AddComponent<Shop>();
            shop.data = shopData;
            shop.dayCycle = Object.FindAnyObjectByType<DayCycle>();
            shop.keeper = keeper.GetComponent<Interactable>();

            // 매대: 도구 5종 + 묘목, 1m 간격. 물건은 모델을 매대 위에 세움(도구 P-4h, 묘목은 작은 묘목 단계 P-4l)
            var goods = tools.Cast<ItemDefinition>().Append(saplingItem).ToArray();
            string[] colors = { "#F4F1EA", "#8E6B4A", "#C9D2DC", "#9AA6B2", "#7FB8E6", "#6FC27A" };
            for (int i = 0; i < goods.Length; i++)
            {
                var stand = new GameObject("Stand_" + goods[i].id).transform;
                stand.SetParent(stall, false);
                stand.localPosition = new Vector3(-2.5f + i * 1.0f, 0f, 0f);
                ColliderOnly(Prim(PrimitiveType.Cube, "Table", stand, new Vector3(0f, 0.4f, 0f), new Vector3(0.8f, 0.8f, 0.6f), wood, collider: true));  // 매대는 노점 모델
                var goodsModel = !models ? null : models.Find(goods[i] is ToolDefinition ? "Tool" + goods[i].id : "SaplingStage1");
                if (goodsModel != null)
                    FitModel(goodsModel.id, stand, new Vector3(0f, CounterTop, goods[i] is ToolDefinition ? 0f : StandSaplingForward), new Vector3(0f, goods[i] is ToolDefinition ? goodsModel.height : StandSaplingHeight, 0f),
                        pitch: ToolPitch.TryGetValue(goodsModel.id, out float pitch) ? pitch : 0f);
                else
                    Prim(PrimitiveType.Sphere, "Goods", stand, new Vector3(0f, CounterTop + 0.15f, 0f), Vector3.one * 0.3f, Mat("ShopGoods_" + goods[i].id, colors[i], rim: 0.3f), castShadows: false);
                AddInteractable(stand.gameObject, goods[i].displayName, InteractableKind.ShopStand, radius: 0.4f, markerHeight: 1.4f, wiggle: false);
                var shopStand = stand.gameObject.AddComponent<ShopStand>();
                shopStand.shop = shop;
                shopStand.item = goods[i];
            }

            var sellBox = new GameObject("SellBox").transform;
            sellBox.SetParent(stall, false);
            sellBox.localPosition = new Vector3(3.7f, 0f, 0.2f);
            ColliderOnly(Prim(PrimitiveType.Cube, "Box", sellBox, new Vector3(0f, 0.35f, 0f), new Vector3(0.8f, 0.7f, 0.8f), wood, collider: true));
            FitModel("SellBox", sellBox, Vector3.zero, new Vector3(0.85f, 0f, 0f));
            AddInteractable(sellBox.gameObject, "판매함", InteractableKind.SellBox, radius: 0.45f, markerHeight: 1.2f, wiggle: false);
            sellBox.gameObject.AddComponent<SellBox>().shop = shop;
        }

        // 게임 시계 · 잠자기(2-1): 광장 임시 침대(F) → 다음 날 아침, 아침 처리는 MorningRunner
        static void BuildDayCycle(Transform props)
        {
            var dayCycle = new GameObject("DayCycle").AddComponent<DayCycle>();
            dayCycle.rules = EnsureAsset<DayRules>(DayRulesPath);
            dayCycle.morning = Object.FindAnyObjectByType<MorningRunner>();

            var bed = new GameObject("Bed").transform;
            bed.SetParent(props, false);
            bed.localPosition = OnGround(PlazaCenter.x + 4f, PlazaCenter.y - 3.5f);
            bed.localRotation = Quaternion.Euler(0f, -30f, 0f);
            ColliderOnly(Prim(PrimitiveType.Cube, "Frame", bed, new Vector3(0f, 0.2f, 0f), new Vector3(1f, 0.4f, 1.9f), Mat("Wood", "#B98A5E"), collider: true));
            FitModel("Bed", bed, Vector3.zero, new Vector3(1.05f, 0f, 1.95f), yaw: 180f);  // 모델은 머리가 -z → 돌려 베개가 +z
            AddInteractable(bed.gameObject, "침대", InteractableKind.Bed, radius: 0.9f, markerHeight: 1.2f, wiggle: false);
            bed.gameObject.AddComponent<Bed>().dayCycle = dayCycle;
        }

        // 삽 동작 · 구멍에 빠지기 (1-8)
        static void BuildShovel(GameObject player, VillageGround ground)
        {
            var shovel = player.AddComponent<ShovelUser>();
            shovel.settings = settings;
            shovel.ground = ground;
            shovel.interactor = player.GetComponent<PlayerInteractor>();
            shovel.inventory = player.GetComponent<PlayerInventory>();
            shovel.flowerClumpItem = flowerClumpItem;

            var can = player.AddComponent<WateringCanUser>();
            can.settings = settings;
            can.ground = ground;
            can.garden = ground.GetComponent<GardenKeeper>();

            var fall = player.AddComponent<PlayerHoleFall>();
            fall.settings = settings;
            fall.ground = ground;
            fall.controller = player.GetComponent<PlayerController>();
        }

        static GameObject SavePrefab(GameObject root, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        // 삽으로 판 구멍: F로 심기
        static GameObject BuildHolePrefab()
        {
            var root = new GameObject("Hole");
            Prim(PrimitiveType.Cylinder, "Pit", root.transform, new Vector3(0f, 0.015f, 0f), new Vector3(0.75f, 0.01f, 0.75f), Mat("HolePit", "#5B4430", rim: 0f), castShadows: false);
            Prim(PrimitiveType.Cylinder, "Rim", root.transform, new Vector3(0f, 0.01f, 0f), new Vector3(0.9f, 0.01f, 0.9f), Mat("HoleRim", "#9C7A55", rim: 0f), castShadows: false);
            AddInteractable(root, "구멍", InteractableKind.Hole, radius: 0.35f, markerHeight: 0.6f, wiggle: false);
            root.AddComponent<GridObject>().kind = GroundObjectKind.Hole;
            root.AddComponent<HolePlanter>().plantables = new[] { saplingItem, flowerClumpItem };
            return SavePrefab(root, "Hole");
        }

        // 심은 묘목: 성장 3단계(Stage0 ~ 2)는 GardenKeeper가 켬
        static GameObject BuildSaplingPrefab()
        {
            var root = new GameObject("Sapling");
            for (int i = 0; i < 3; i++)
            {
                var stage = new GameObject("Stage" + i).transform;
                stage.SetParent(root.transform, false);
                ModelVisual("SaplingStage" + i, stage, Vector3.zero, castShadows: i > 0);  // 새싹 → 작은 묘목 → 큰 묘목(P-4i), 높이는 ModelLibrary
                stage.gameObject.SetActive(i == 0);
            }
            root.AddComponent<GridObject>().kind = GroundObjectKind.Sapling;
            return SavePrefab(root, "Sapling");
        }

        // 묘목이 다 자라 생기는 나무: 열매 없는 활엽수, 흔들기 · 도끼로 베기 가능 (결정 2026-10-27)
        static GameObject BuildGrownTreePrefab()
        {
            var root = new GameObject("GrownTree");
            ColliderOnly(Prim(PrimitiveType.Cylinder, "Trunk", root.transform, new Vector3(0f, 0.8f, 0f), new Vector3(0.4f, 0.8f, 0.4f), null, collider: true));
            ModelVisual("TreeRound", root.transform, Vector3.zero);
            AddInteractable(root, "나무", InteractableKind.Tree, radius: 0.4f, markerHeight: 4.2f);
            var choppable = root.AddComponent<ChoppableTreeBehaviour>();
            choppable.rules = gatheringRules;
            choppable.settings = settings;
            choppable.woodItem = woodItem;
            choppable.woodMaterial = Mat("Wood", "#B98A5E");
            choppable.pickupPrefab = pickupPrefab;
            choppable.stumpPrefab = stumpPrefab;
            return SavePrefab(root, "GrownTree");
        }

        // 꽃 포기를 심어 난 꽃(꺾을 수 있음)
        static GameObject BuildPlantedFlowerPrefab()
        {
            var root = new GameObject("Flower");
            FlowerVisual(root.transform, FlowerModels[0], null);
            AddInteractable(root, "꽃", InteractableKind.Flower, radius: 0.15f, markerHeight: 0.75f, wiggle: false);
            root.AddComponent<Pickup>().item = flowerItem;
            root.AddComponent<GridObject>().kind = GroundObjectKind.Flower;
            return SavePrefab(root, "PlantedFlower");
        }

        static void BuildGroundCursor(PlayerTools playerTools, VillageGround ground)
        {
            var cursor = new GameObject("GroundCursor").AddComponent<GroundCursor>();
            var outline = CellOutline(cursor.transform, ground);
            cursor.settings = settings;
            cursor.ground = ground;
            cursor.tools = playerTools;
            cursor.outline = outline;
        }

        // 칸 테두리(얇은 사각형 4변): 앞 칸 표시 · 가구 놓기 미리보기가 씀
        static GameObject CellOutline(Transform parent, VillageGround ground)
        {
            var outline = new GameObject("Outline");
            outline.transform.SetParent(parent, false);
            var mat = Mat("GroundCursor", "#FFFFFF", rim: 0f);
            float half = ground.data.cellSize * 0.5f;
            const float thickness = 0.06f, lift = 0.03f;
            for (int side = 0; side < 4; side++)
            {
                bool alongX = side < 2;
                float sign = side % 2 == 0 ? 1f : -1f;
                Vector3 position = alongX ? new Vector3(0f, lift, sign * half) : new Vector3(sign * half, lift, 0f);
                Vector3 scale = alongX ? new Vector3(half * 2f, 0.02f, thickness) : new Vector3(thickness, 0.02f, half * 2f);
                Prim(PrimitiveType.Cube, "Edge", outline.transform, position, scale, mat, castShadows: false);
            }
            return outline;
        }

        // ---------------------------------------------------------------- furniture (3-4a)

        // 기본 가구 8가지(BALANCE 16번). 없을 때만 만들고 값은 건드리지 않음. 모습(프리팹)은 매번 다시 만듦
        static FurnitureDefinition[] EnsureFurniture()
        {
            EnsureFolder(FurnitureDir);
            var list = new[]
            {
                EnsureFurnitureItem("Fence", "나무 울타리", wood: 2, buy: 200, sell: 100, points: 5, stars: 1),
                EnsureFurnitureItem("Crate", "나무 상자", wood: 3, buy: 300, sell: 150, points: 5, stars: 1),
                EnsureFurnitureItem("Planter", "화분", wood: 0, buy: 400, sell: 200, points: 10, stars: 1),
                EnsureFurnitureItem("LogStool", "통나무 의자", wood: 4, buy: 400, sell: 200, points: 10, stars: 2),
                EnsureFurnitureItem("Bench", "나무 벤치", wood: 8, buy: 800, sell: 400, points: 15, stars: 2),
                EnsureFurnitureItem("RoundTable", "둥근 탁자", wood: 6, buy: 600, sell: 300, points: 15, stars: 3),
                EnsureFurnitureItem("StreetLamp", "가로등", wood: 0, buy: 1200, sell: 600, points: 15, stars: 3),
                EnsureFurnitureItem("Birdhouse", "새집", wood: 0, buy: 1500, sell: 750, points: 15, stars: 4),
            };
            foreach (var f in list)
            {
                f.prefab = BuildFurniturePrefab(f);
                EditorUtility.SetDirty(f);
            }
            return list;
        }

        static FurnitureDefinition EnsureFurnitureItem(string id, string displayName, int wood, int buy, int sell, int points, int stars)
        {
            string path = $"{FurnitureDir}/{id}.asset";
            bool isNew = !AssetDatabase.LoadAssetAtPath<FurnitureDefinition>(path);
            var item = EnsureAsset<FurnitureDefinition>(path);
            if (isNew)
            {
                item.id = id;
                item.displayName = displayName;
                item.maxStack = 5;  // 결정 2026-10-28: 한 칸에 5개
                item.woodCost = wood;
                item.buyPrice = buy;
                item.sellPrice = sell;
                item.ratingPoint = points;
                item.unlockStars = stars;
                EditorUtility.SetDirty(item);
            }
            return item;
        }

        // 그레이박스 가구(1칸 안, 앞 = +Z). 충돌 · 상호작용 · 칸 등록 · 놓인 가구
        static GameObject BuildFurniturePrefab(FurnitureDefinition def)
        {
            var root = new GameObject(def.id);
            var t = root.transform;
            var wood = Mat("Wood", "#B98A5E");
            var plank = Mat("Plank", "#D6AE7B");
            float markerHeight = 1.2f;
            Vector3 box = new(0.8f, 1f, 0.8f);
            switch (def.id)
            {
                case "Fence":
                    foreach (float x in new[] { -0.4f, 0.4f })
                        Prim(PrimitiveType.Cube, "Post", t, new Vector3(x, 0.4f, 0f), new Vector3(0.1f, 0.8f, 0.1f), wood);
                    Prim(PrimitiveType.Cube, "RailTop", t, new Vector3(0f, 0.6f, 0f), new Vector3(0.9f, 0.08f, 0.06f), plank);
                    Prim(PrimitiveType.Cube, "RailLow", t, new Vector3(0f, 0.3f, 0f), new Vector3(0.9f, 0.08f, 0.06f), plank);
                    box = new Vector3(0.9f, 0.8f, 0.2f);
                    break;
                case "Crate":
                    Prim(PrimitiveType.Cube, "Box", t, new Vector3(0f, 0.3f, 0f), new Vector3(0.6f, 0.6f, 0.6f), plank);
                    box = new Vector3(0.6f, 0.6f, 0.6f);
                    markerHeight = 1f;
                    break;
                case "Planter":
                    Prim(PrimitiveType.Cylinder, "Pot", t, new Vector3(0f, 0.2f, 0f), new Vector3(0.45f, 0.2f, 0.45f), Mat("Pot", "#D9825B"));
                    Prim(PrimitiveType.Sphere, "Bush", t, new Vector3(0f, 0.55f, 0f), new Vector3(0.5f, 0.4f, 0.5f), Mat("Leaves", "#6FC27A"));
                    box = new Vector3(0.5f, 0.75f, 0.5f);
                    break;
                case "LogStool":
                    Prim(PrimitiveType.Cylinder, "Log", t, new Vector3(0f, 0.22f, 0f), new Vector3(0.4f, 0.22f, 0.4f), Mat("Trunk", "#A57A52"));
                    Prim(PrimitiveType.Cylinder, "Top", t, new Vector3(0f, 0.445f, 0f), new Vector3(0.36f, 0.01f, 0.36f), Mat("StumpTop", "#E8C99A"));
                    box = new Vector3(0.4f, 0.45f, 0.4f);
                    markerHeight = 0.9f;
                    break;
                case "Bench":
                    Prim(PrimitiveType.Cube, "Seat", t, new Vector3(0f, 0.4f, 0f), new Vector3(0.9f, 0.08f, 0.38f), plank);
                    Prim(PrimitiveType.Cube, "Back", t, new Vector3(0f, 0.65f, -0.17f), new Vector3(0.9f, 0.35f, 0.06f), plank);
                    foreach (float x in new[] { -0.38f, 0.38f })
                        Prim(PrimitiveType.Cube, "Leg", t, new Vector3(x, 0.2f, 0f), new Vector3(0.08f, 0.4f, 0.32f), wood);
                    box = new Vector3(0.9f, 0.85f, 0.4f);
                    break;
                case "RoundTable":
                    Prim(PrimitiveType.Cylinder, "Top", t, new Vector3(0f, 0.6f, 0f), new Vector3(0.8f, 0.04f, 0.8f), plank);
                    Prim(PrimitiveType.Cylinder, "Leg", t, new Vector3(0f, 0.3f, 0f), new Vector3(0.12f, 0.3f, 0.12f), wood);
                    box = new Vector3(0.8f, 0.65f, 0.8f);
                    break;
                case "StreetLamp":
                    Prim(PrimitiveType.Cylinder, "Pole", t, new Vector3(0f, 0.9f, 0f), new Vector3(0.1f, 0.9f, 0.1f), Mat("LampPole", "#5E5A66"));
                    Prim(PrimitiveType.Sphere, "Light", t, new Vector3(0f, 1.9f, 0f), Vector3.one * 0.3f, Mat("LampGlow", "#FFF1B8", rim: 0.3f));
                    box = new Vector3(0.25f, 2f, 0.25f);
                    markerHeight = 2.4f;
                    break;
                case "Birdhouse":
                    Prim(PrimitiveType.Cylinder, "Pole", t, new Vector3(0f, 0.6f, 0f), new Vector3(0.08f, 0.6f, 0.08f), wood);
                    Prim(PrimitiveType.Cube, "House", t, new Vector3(0f, 1.35f, 0f), new Vector3(0.35f, 0.35f, 0.35f), Mat("Birdhouse", "#8FC1E3"));
                    Prim(PrimitiveType.Sphere, "Hole", t, new Vector3(0f, 1.38f, 0.17f), new Vector3(0.1f, 0.1f, 0.04f), Mat("Eye", "#3A3340"));
                    var roof = MeshObject("Roof", t, RoofMesh(), Mat("BirdhouseRoof", "#E07A5F"));
                    roof.transform.localPosition = new Vector3(0f, 1.52f, 0f);
                    roof.transform.localScale = new Vector3(0.45f, 0.2f, 0.45f);
                    box = new Vector3(0.2f, 1.7f, 0.2f);
                    markerHeight = 2f;
                    break;
            }
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, box.y * 0.5f, 0f);
            collider.size = box;
            AddInteractable(root, def.displayName, InteractableKind.Furniture, radius: 0.4f, markerHeight: markerHeight, wiggle: false);
            root.AddComponent<GridObject>().kind = GroundObjectKind.Furniture;
            var placed = root.AddComponent<PlacedFurniture>();
            SwapToModel(placed, "Furniture" + def.id);  // 기본 가구 모델(P-4b)
            return SavePrefab(root, "Furniture_" + def.id);
        }

        // 테마 가구(3-4d): 주민마다 화분 · 벤치 · 탁자 · 가로등(친밀도 보상 차례). 데이터는 없을 때만, 모양 4가지는 매번(주민 색은 놓을 때 입힘)
        static readonly (string id, string name)[] ThemeKinds = { ("Planter", "화분"), ("Bench", "벤치"), ("Table", "탁자"), ("Lamp", "가로등") };

        static FurnitureDefinition[] EnsureThemeFurniture(VillagerDefinition[] villagers)
        {
            string dir = FurnitureDir + "/Theme";
            EnsureFolder(dir);
            var prefabs = ThemeKinds.Select(k => BuildThemePrefab(k.id)).ToArray();
            var all = new List<FurnitureDefinition>();
            foreach (var v in villagers)
            {
                var set = new FurnitureDefinition[ThemeKinds.Length];
                for (int k = 0; k < set.Length; k++)
                {
                    string id = $"{v.id}_{ThemeKinds[k].id}";
                    string path = $"{dir}/{id}.asset";
                    bool isNew = !AssetDatabase.LoadAssetAtPath<FurnitureDefinition>(path);
                    var item = EnsureAsset<FurnitureDefinition>(path);
                    if (isNew)
                    {
                        item.id = id;
                        item.displayName = $"{v.displayName}네 {ThemeKinds[k].name}";  // 결정 2026-10-28: 이름은 에셋에서 고침
                        item.maxStack = 5;
                        item.buyPrice = 2000;   // BALANCE 16번: 오늘의 테마 가구
                        item.sellPrice = 1000;
                        item.ratingPoint = 20;
                        item.unlockStars = 1;
                    }
                    item.prefab = BuildThemeModelPrefab(prefabs[k], v, ThemeKinds[k].id) ?? prefabs[k];  // 주민마다 특색 있는 모델(P-4c), 없으면 그레이박스 + 주민 색
                    item.themeOwner = v;
                    EditorUtility.SetDirty(item);
                    set[k] = item;
                }
                v.themeFurniture = set;
                EditorUtility.SetDirty(v);
                all.AddRange(set);
            }
            return all.ToArray();
        }

        // 주민 테마 가구 모델 프리팹(P-4c): 그레이박스 프리팹(충돌 · 상호작용)을 복사해 모델로 바꿈 — 모델이 제 색이라 주민 색은 칠하지 않음
        static GameObject BuildThemeModelPrefab(GameObject greybox, VillagerDefinition villager, string kind)
        {
            string modelId = $"Theme{villager.id}_{kind}";
            var entry = models ? models.Find(modelId) : null;
            if (entry == null || !entry.model)
                return null;
            var root = (GameObject)PrefabUtility.InstantiatePrefab(greybox);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = $"Theme_{villager.id}_{kind}";
            SwapToModel(root.GetComponent<PlacedFurniture>(), modelId);
            return SavePrefab(root, $"Furniture_Theme_{villager.id}_{kind}");
        }

        // 테마 가구 모양(그레이박스): 흰 재질 부분에 주민 몸 색 · 포인트 색을 입힘(PlacedFurniture.Tint)
        static GameObject BuildThemePrefab(string kind)
        {
            var root = new GameObject("Theme_" + kind);
            var t = root.transform;
            var white = Mat("ThemeFurniture", "#FFFFFF");
            var body = new List<Renderer>();
            var accent = new List<Renderer>();
            Renderer Part(PrimitiveType type, string name, Vector3 pos, Vector3 scale, List<Renderer> into)
            {
                var r = Prim(type, name, t, pos, scale, white).GetComponent<Renderer>();
                into.Add(r);
                return r;
            }
            float markerHeight = 1.2f;
            Vector3 box = new(0.8f, 1f, 0.8f);
            switch (kind)
            {
                case "Planter":
                    Part(PrimitiveType.Cylinder, "Pot", new Vector3(0f, 0.22f, 0f), new Vector3(0.5f, 0.22f, 0.5f), body);
                    Part(PrimitiveType.Cylinder, "Rim", new Vector3(0f, 0.45f, 0f), new Vector3(0.56f, 0.04f, 0.56f), accent);
                    Prim(PrimitiveType.Sphere, "Bush", t, new Vector3(0f, 0.65f, 0f), new Vector3(0.5f, 0.42f, 0.5f), Mat("Leaves", "#6FC27A"));
                    Part(PrimitiveType.Sphere, "Bloom", new Vector3(0.1f, 0.85f, 0.1f), Vector3.one * 0.16f, accent);
                    box = new Vector3(0.56f, 0.86f, 0.56f);
                    break;
                case "Bench":
                    Part(PrimitiveType.Cube, "Seat", new Vector3(0f, 0.4f, 0f), new Vector3(0.9f, 0.1f, 0.4f), body);
                    Part(PrimitiveType.Cube, "Back", new Vector3(0f, 0.68f, -0.17f), new Vector3(0.9f, 0.4f, 0.07f), body);
                    foreach (float x in new[] { -0.38f, 0.38f })
                        Part(PrimitiveType.Cube, "Leg", new Vector3(x, 0.18f, 0f), new Vector3(0.08f, 0.36f, 0.34f), accent);
                    box = new Vector3(0.9f, 0.88f, 0.4f);
                    break;
                case "Table":
                    Part(PrimitiveType.Cylinder, "Top", new Vector3(0f, 0.6f, 0f), new Vector3(0.82f, 0.05f, 0.82f), body);
                    Part(PrimitiveType.Cylinder, "Leg", new Vector3(0f, 0.3f, 0f), new Vector3(0.14f, 0.3f, 0.14f), accent);
                    Part(PrimitiveType.Cylinder, "Foot", new Vector3(0f, 0.03f, 0f), new Vector3(0.45f, 0.03f, 0.45f), accent);
                    box = new Vector3(0.82f, 0.65f, 0.82f);
                    break;
                case "Lamp":
                    Part(PrimitiveType.Cylinder, "Pole", new Vector3(0f, 0.85f, 0f), new Vector3(0.1f, 0.85f, 0.1f), accent);
                    Part(PrimitiveType.Cylinder, "Cap", new Vector3(0f, 2.08f, 0f), new Vector3(0.42f, 0.05f, 0.42f), body);
                    Prim(PrimitiveType.Sphere, "Light", t, new Vector3(0f, 1.85f, 0f), Vector3.one * 0.32f, Mat("LampGlow", "#FFF1B8", rim: 0.3f));
                    box = new Vector3(0.3f, 2.1f, 0.3f);
                    markerHeight = 2.5f;
                    break;
            }
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, box.y * 0.5f, 0f);
            collider.size = box;
            AddInteractable(root, "테마 가구", InteractableKind.Furniture, radius: 0.4f, markerHeight: markerHeight, wiggle: false);
            root.AddComponent<GridObject>().kind = GroundObjectKind.Furniture;
            var placed = root.AddComponent<PlacedFurniture>();
            placed.bodyRenderers = body.ToArray();
            placed.accentRenderers = accent.ToArray();
            return SavePrefab(root, "Furniture_Theme_" + kind);
        }

        // 작업대(3-4b): 광장 동쪽(동쪽 길 입구 북쪽), 광장 쪽(서쪽)을 봄
        static void BuildWorkbench(Transform props)
        {
            var bench = new GameObject("Workbench").transform;
            bench.SetParent(props, false);
            bench.localPosition = OnGround(PlazaCenter.x + 5f, PlazaCenter.y + 2.3f);
            bench.localRotation = Quaternion.Euler(0f, 270f, 0f);  // 앞(+z)이 서쪽 = 광장
            var wood = Mat("Wood", "#B98A5E");
            ColliderOnly(Prim(PrimitiveType.Cube, "Top", bench, new Vector3(0f, 0.76f, 0f), new Vector3(1.6f, 0.12f, 0.8f), wood, collider: true));
            FitModel("Workbench", bench, Vector3.zero, new Vector3(1.6f, 0f, 0.9f));
            AddInteractable(bench.gameObject, "작업대", InteractableKind.Workbench, radius: 0.8f, markerHeight: 1.5f, wiggle: false);
            bench.gameObject.AddComponent<Workbench>().recipes = furniture.Where(f => CraftRules.IsRecipe(f.woodCost)).ToArray();
        }

        // 가구 놓기 · 들기(3-4a): 놓인 가구 관리자, 플레이어 놓기 모드(+ 칸 테두리), 인벤토리 창 연결
        static void BuildFurniture(GameObject player, VillageGround ground, DialogueWindow dialogue)
        {
            var inventory = player.GetComponent<PlayerInventory>();
            var keeper = new GameObject("Furniture").AddComponent<FurnitureKeeper>();
            keeper.ground = ground;
            keeper.inventory = inventory;
            keeper.dialogue = dialogue;

            var placer = player.AddComponent<FurniturePlacer>();
            placer.settings = settings;
            placer.keeper = keeper;
            placer.inventory = inventory;
            placer.playerLock = player.GetComponent<PlayerLock>();
            var holder = new GameObject("FurniturePreview").transform;
            placer.outline = CellOutline(holder, ground);
            dialogue.GetComponent<InventoryWindow>().furniturePlacer = placer;

            // 목록 창(3-4b 작업대 제작 · 3-4c 상점 가구): 가운데 제목 · 목록(8줄) · 아래 가진 것 · 안내
            var font = EnsureFontAsset();
            var panel = new GameObject("ListPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(dialogue.transform, false);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(760f, 620f);
            panel.GetComponent<Image>().color = new Color(1f, 0.97f, 0.92f, 0.92f);
            var window = dialogue.gameObject.AddComponent<ListWindow>();
            window.panel = panel;
            window.titleText = HudLabel(panel.transform, "Title", font, 40, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(700f, 56f));
            window.listText = HudLabel(panel.transform, "List", font, 38, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -90f), new Vector2(660f, 420f));
            window.footerText = HudLabel(panel.transform, "Footer", font, 34, TextAlignmentOptions.BottomLeft, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(50f, 24f), new Vector2(400f, 48f));
            window.hintText = HudLabel(panel.transform, "Hint", font, 28, TextAlignmentOptions.BottomRight, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 28f), new Vector2(300f, 40f));
            window.playerLock = player.GetComponent<PlayerLock>();
            window.inventory = inventory;
            var rater = Object.FindAnyObjectByType<VillageRater>();
            rater.furniture = keeper;  // 놓인 가구 점수(3-4e)

            var workbench = Object.FindAnyObjectByType<Workbench>();
            workbench.wood = woodItem;
            workbench.inventory = inventory;
            workbench.rater = rater;
            workbench.window = window;

            // 상점 가구(3-4c): 상점 주인 F → 목록 창
            var shop = Object.FindAnyObjectByType<Shop>();
            var counter = shop.gameObject.AddComponent<FurnitureCounter>();
            counter.shop = shop;
            counter.furniture = furniture;
            counter.inventory = inventory;
            counter.wallet = player.GetComponent<PlayerWallet>();
            counter.rater = rater;
            counter.window = window;
            counter.village = Object.FindAnyObjectByType<Village>();
            counter.morning = Object.FindAnyObjectByType<MorningRunner>();
            shop.furnitureCounter = counter;
        }

        // 물고기 8종(3-6a, BALANCE 17번): 없을 때만 만들고, 있으면 값을 건드리지 않음
        static FishDefinition[] EnsureFish()
        {
            string dir = ItemDir + "/Fish";
            EnsureFolder(dir);
            var any = new DayPeriod[0];
            var dayOnly = new[] { DayPeriod.Morning, DayPeriod.Daytime };
            var nightOnly = new[] { DayPeriod.Evening, DayPeriod.Night };
            var list = new[]
            {
                EnsureFishItem(dir, "Medaka", "송사리", FishSize.Small, Rarity.Common, any, 100, 5, "#C9D6DF"),
                EnsureFishItem(dir, "PaleChub", "피라미", FishSize.Small, Rarity.Common, dayOnly, 120, 5, "#9FC7E8"),
                EnsureFishItem(dir, "CrucianCarp", "붕어", FishSize.Medium, Rarity.Common, any, 160, 5, "#B8A27A"),
                EnsureFishItem(dir, "Loach", "미꾸라지", FishSize.Small, Rarity.Common, nightOnly, 150, 5, "#8A7A5C"),
                EnsureFishItem(dir, "Carp", "잉어", FishSize.Large, Rarity.Uncommon, any, 300, 10, "#D99A5B"),
                EnsureFishItem(dir, "Catfish", "메기", FishSize.Large, Rarity.Uncommon, nightOnly, 400, 10, "#5E5A66"),
                EnsureFishItem(dir, "Mandarinfish", "쏘가리", FishSize.Medium, Rarity.Rare, new[] { DayPeriod.Daytime }, 900, 20, "#C8B04A"),
                EnsureFishItem(dir, "GoldenCarp", "황금잉어", FishSize.Large, Rarity.Rare, new[] { DayPeriod.Evening }, 1500, 20, "#F2C14E"),
            };
            var rules = EnsureAsset<FishingRules>(FishingRulesPath);
            rules.fish = list;
            EditorUtility.SetDirty(rules);
            EnsureAquariums(list);
            return list;
        }

        // 수조(3-6c): 물고기마다 수조 가구(데이터는 없을 때만), 모양은 하나를 함께 쓰고 물고기 색을 입힘. 상점에는 없음(물고기를 놓으면 수조)
        static void EnsureAquariums(FishDefinition[] fishList)
        {
            string dir = FurnitureDir + "/Aquarium";
            EnsureFolder(dir);
            foreach (var f in fishList)
            {
                var prefab = BuildAquariumPrefab(f);
                SetPickupLook(f, "Fish" + f.id);  // 떨어진 물고기도 그 물고기 모델(P-4d)
                string path = $"{dir}/Aquarium_{f.id}.asset";
                bool isNew = !AssetDatabase.LoadAssetAtPath<FurnitureDefinition>(path);
                var tank = EnsureAsset<FurnitureDefinition>(path);
                if (isNew)
                {
                    tank.id = "Aquarium_" + f.id;
                    tank.displayName = $"{f.displayName} 수조";
                    tank.maxStack = 1;
                }
                tank.prefab = prefab;
                tank.aquariumFish = f;
                EditorUtility.SetDirty(tank);
                f.aquarium = tank;
                EditorUtility.SetDirty(f);
            }
        }

        // 물고기마다 수조 프리팹(P-4d): 수조 모델 + 그 물고기 모델(옆모습이 앞을 봄). 유리 모델이 불투명이라 물고기는 앞 유리 바로 앞에 붙임
        static GameObject BuildAquariumPrefab(FishDefinition fish)
        {
            var tankModel = models ? models.Find("Aquarium") : null;
            var fishModel = models ? models.Find("Fish" + fish.id) : null;
            if (tankModel == null || fishModel == null)
                return BuildAquariumPrefab();
            var root = new GameObject("Aquarium");
            var t = root.transform;
            var tank = FitModel("Aquarium", t, Vector3.zero, new Vector3(0.75f, 0f, 0.5f));
            var tankBounds = ModelBounds(tank);
            float length = fish.size == FishSize.Small ? 0.2f : fish.size == FishSize.Medium ? 0.27f : 0.34f;
            var body = FitModel("Fish" + fish.id, t, Vector3.zero, new Vector3(length, 0f, 0f), yaw: 90f);  // 모델은 머리가 +z → 90° 돌려 옆모습
            var fishBounds = ModelBounds(body);
            body.transform.localPosition = new Vector3(0f, tankBounds.size.y * AquariumWaterCenter - fishBounds.size.y * 0.5f, tankBounds.max.z + fishBounds.size.z * 0.5f - 0.02f);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.4f, 0f);
            collider.size = new Vector3(0.75f, 0.8f, 0.5f);
            AddInteractable(root, "수조", InteractableKind.Furniture, radius: 0.4f, markerHeight: 1.2f, wiggle: false);
            root.AddComponent<GridObject>().kind = GroundObjectKind.Furniture;
            root.AddComponent<PlacedFurniture>();  // 물고기 모델이 제 색이라 칠하지 않음
            return SavePrefab(root, "Furniture_Aquarium_" + fish.id);
        }

        // 그레이박스 수조(모델이 없을 때): 물고기 모양을 물고기 색으로 칠함
        static GameObject BuildAquariumPrefab()
        {
            var root = new GameObject("Aquarium");
            var t = root.transform;
            Prim(PrimitiveType.Cube, "Stand", t, new Vector3(0f, 0.15f, 0f), new Vector3(0.75f, 0.3f, 0.5f), Mat("Wood", "#B98A5E"));
            Prim(PrimitiveType.Cube, "Glass", t, new Vector3(0f, 0.55f, 0f), new Vector3(0.7f, 0.5f, 0.45f), Mat("AquariumWater", "#BFE9F2", rim: 0.4f));
            Prim(PrimitiveType.Cube, "Sand", t, new Vector3(0f, 0.32f, 0f), new Vector3(0.68f, 0.05f, 0.43f), Mat("AquariumSand", "#EAD9AE"));
            var white = Mat("ThemeFurniture", "#FFFFFF");
            var body = Prim(PrimitiveType.Sphere, "Fish", t, new Vector3(0f, 0.56f, 0.24f), new Vector3(0.3f, 0.16f, 0.06f), white).GetComponent<Renderer>();
            var tail = Prim(PrimitiveType.Cube, "Tail", t, new Vector3(-0.18f, 0.56f, 0.24f), new Vector3(0.08f, 0.12f, 0.04f), white).GetComponent<Renderer>();
            Prim(PrimitiveType.Sphere, "Eye", t, new Vector3(0.09f, 0.58f, 0.27f), Vector3.one * 0.03f, Mat("Eye", "#3A3340"));
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.4f, 0f);
            collider.size = new Vector3(0.75f, 0.8f, 0.5f);
            AddInteractable(root, "수조", InteractableKind.Furniture, radius: 0.4f, markerHeight: 1.2f, wiggle: false);
            root.AddComponent<GridObject>().kind = GroundObjectKind.Furniture;
            root.AddComponent<PlacedFurniture>().bodyRenderers = new[] { body, tail };
            return SavePrefab(root, "Furniture_Aquarium");
        }

        static FishDefinition EnsureFishItem(string dir, string id, string displayName, FishSize size, Rarity rarity, DayPeriod[] periods, int sell, int aquarium, string color)
        {
            string path = $"{dir}/{id}.asset";
            bool isNew = !AssetDatabase.LoadAssetAtPath<FishDefinition>(path);
            var item = EnsureAsset<FishDefinition>(path);
            if (isNew)
            {
                item.id = id;
                item.displayName = displayName;
                item.maxStack = 10;
                item.sellPrice = sell;
                item.size = size;
                item.rarity = rarity;
                item.periods = periods;
                item.aquariumPoint = aquarium;
                item.color = Hex(color);
                EditorUtility.SetDirty(item);
            }
            return item;
        }

        // 낚시(3-6a): 강의 물고기 그림자 · 찌 · 플레이어 낚싯대
        static void BuildFishing(GameObject player, VillageGround ground)
        {
            var shadowRoot = new GameObject("FishShadow");
            var disc = Prim(PrimitiveType.Sphere, "Shadow", shadowRoot.transform, Vector3.zero, new Vector3(1f, 0.02f, 1f), Mat("FishShadow", "#2E3B52", rim: 0f), castShadows: false);
            disc.layer = 2;  // 레이캐스트에 맞지 않게
            shadowRoot.AddComponent<FishShadow>();
            var shadowPrefab = SavePrefab(shadowRoot, "FishShadow").GetComponent<FishShadow>();

            var shadows = new GameObject("FishShadows").AddComponent<FishShadows>();
            shadows.rules = EnsureAsset<FishingRules>(FishingRulesPath);
            shadows.settings = settings;
            shadows.ground = ground;
            shadows.dayCycle = Object.FindAnyObjectByType<DayCycle>();
            shadows.shadowPrefab = shadowPrefab;

            var bobber = new GameObject("Bobber").transform;
            Prim(PrimitiveType.Sphere, "Float", bobber, new Vector3(0f, 0.05f, 0f), Vector3.one * 0.16f, Mat("BobberRed", "#E05A4F", rim: 0.3f), castShadows: false).layer = 2;
            Prim(PrimitiveType.Sphere, "Top", bobber, new Vector3(0f, 0.13f, 0f), Vector3.one * 0.08f, Mat("BobberWhite", "#FFFFFF", rim: 0.3f), castShadows: false).layer = 2;
            bobber.gameObject.layer = 2;

            var rod = player.AddComponent<FishingRodUser>();
            rod.rules = shadows.rules;
            rod.settings = settings;
            rod.shadows = shadows;
            rod.ground = ground;
            rod.inventory = player.GetComponent<PlayerInventory>();
            rod.tools = player.GetComponent<PlayerTools>();
            rod.playerLock = player.GetComponent<PlayerLock>();
            rod.bobber = bobber;
            rod.pickupPrefab = pickupPrefab;

            // 물고기 도감(3-6b): 게시판에서 여는 목록 창, 잡은 종류는 마을 평가에
            var encyclopedia = new GameObject("FishEncyclopedia").AddComponent<FishEncyclopedia>();
            encyclopedia.rules = shadows.rules;
            encyclopedia.window = Object.FindAnyObjectByType<ListWindow>();
            rod.encyclopedia = encyclopedia;
            Object.FindAnyObjectByType<VillageRater>().fishEncyclopedia = encyclopedia;
            Debug.Log($"[낚시] 강 물 칸 {ground.data.WaterCells().Count}개");
        }

        // 곤충 8종(3-7a, BALANCE 18번): 없을 때만 만들고, 있으면 값을 건드리지 않음
        static InsectDefinition[] EnsureInsects()
        {
            string dir = ItemDir + "/Insects";
            EnsureFolder(dir);
            var any = new DayPeriod[0];
            var list = new[]
            {
                EnsureInsectItem(dir, "CabbageWhite", "배추흰나비", InsectSite.Flower, Rarity.Common, new[] { DayPeriod.Morning, DayPeriod.Daytime }, false, 80, 5, "#F4F1EA"),
                EnsureInsectItem(dir, "HoneyBee", "꿀벌", InsectSite.Flower, Rarity.Common, new[] { DayPeriod.Daytime }, false, 100, 5, "#F2C14E"),
                EnsureInsectItem(dir, "Grasshopper", "메뚜기", InsectSite.Grass, Rarity.Common, new[] { DayPeriod.Daytime, DayPeriod.Evening }, false, 100, 5, "#8BC34A"),
                EnsureInsectItem(dir, "Cricket", "귀뚜라미", InsectSite.Grass, Rarity.Common, new[] { DayPeriod.Evening, DayPeriod.Night }, false, 120, 5, "#6B4F3A"),
                EnsureInsectItem(dir, "RedDragonfly", "고추잠자리", InsectSite.Water, Rarity.Uncommon, new[] { DayPeriod.Morning, DayPeriod.Daytime, DayPeriod.Evening }, false, 200, 10, "#E05A4F"),
                EnsureInsectItem(dir, "Cicada", "매미", InsectSite.Tree, Rarity.Uncommon, new[] { DayPeriod.Daytime }, false, 250, 10, "#7A8A5C"),
                EnsureInsectItem(dir, "Firefly", "반딧불이", InsectSite.Water, Rarity.Rare, new[] { DayPeriod.Night }, true, 800, 20, "#E8F57A"),
                EnsureInsectItem(dir, "StagBeetle", "사슴벌레", InsectSite.Tree, Rarity.Rare, new[] { DayPeriod.Night }, true, 1200, 20, "#3A2A22"),
            };
            var rules = EnsureAsset<InsectRules>(InsectRulesPath);
            rules.insects = list;
            EditorUtility.SetDirty(rules);
            EnsureCages(list);
            return list;
        }

        static InsectDefinition EnsureInsectItem(string dir, string id, string displayName, InsectSite site, Rarity rarity, DayPeriod[] periods, bool sensitive, int sell, int cage, string color)
        {
            string path = $"{dir}/{id}.asset";
            bool isNew = !AssetDatabase.LoadAssetAtPath<InsectDefinition>(path);
            var item = EnsureAsset<InsectDefinition>(path);
            if (isNew)
            {
                item.id = id;
                item.displayName = displayName;
                item.maxStack = 10;
                item.sellPrice = sell;
                item.site = site;
                item.rarity = rarity;
                item.periods = periods;
                item.sensitive = sensitive;
                item.cagePoint = cage;
                item.color = Hex(color);
                EditorUtility.SetDirty(item);
            }
            return item;
        }

        // 채집통(3-7c): 곤충마다 채집통 가구(데이터는 없을 때만), 모양은 하나를 함께 쓰고 곤충 색을 입힘. 상점에는 없음(곤충을 놓으면 채집통)
        static void EnsureCages(InsectDefinition[] insectList)
        {
            string dir = FurnitureDir + "/Cage";
            EnsureFolder(dir);
            foreach (var insect in insectList)
            {
                var prefab = BuildCagePrefab(insect);
                SetPickupLook(insect, "Insect" + insect.id);  // 떨어진 곤충 · 마을을 도는 곤충도 그 곤충 모델(P-4e)
                string path = $"{dir}/Cage_{insect.id}.asset";
                bool isNew = !AssetDatabase.LoadAssetAtPath<FurnitureDefinition>(path);
                var cage = EnsureAsset<FurnitureDefinition>(path);
                if (isNew)
                {
                    cage.id = "Cage_" + insect.id;
                    cage.displayName = $"{insect.displayName} 채집통";
                    cage.maxStack = 1;
                }
                cage.prefab = prefab;
                cage.cageInsect = insect;
                EditorUtility.SetDirty(cage);
                insect.cage = cage;
                EditorUtility.SetDirty(insect);
            }
        }

        // 곤충마다 채집통 프리팹(P-4e): 채집통 모델 + 그 곤충 모델. 망이 불투명이라 곤충은 앞 망 바로 앞에(수조의 물고기와 같은 방식), 가장 긴 변을 맞춤
        static GameObject BuildCagePrefab(InsectDefinition insect)
        {
            var cageModel = models ? models.Find("InsectCage") : null;
            var insectModel = models ? models.Find("Insect" + insect.id) : null;
            if (cageModel == null || insectModel == null)
                return BuildCagePrefab();
            var root = new GameObject("Cage");
            var t = root.transform;
            var cage = FitModel("InsectCage", t, Vector3.zero, new Vector3(0.5f, 0f, 0f));
            var cageBounds = ModelBounds(cage);
            var body = FitModel("Insect" + insect.id, t, Vector3.zero, new Vector3(0f, CageInsectSize, 0f));
            var size = ModelBounds(body).size;
            body.transform.localScale *= CageInsectSize / Mathf.Max(size.x, size.y, size.z);  // 납작한 잠자리 · 나비도 같은 크기로
            size = ModelBounds(body).size;
            body.transform.localPosition = new Vector3(0f, cageBounds.size.y * CageInsectCenter - size.y * 0.5f, cageBounds.max.z + size.z * 0.5f - 0.01f);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.3f, 0f);
            collider.size = new Vector3(0.5f, 0.62f, 0.5f);
            AddInteractable(root, "채집통", InteractableKind.Furniture, radius: 0.35f, markerHeight: 1f, wiggle: false);
            root.AddComponent<GridObject>().kind = GroundObjectKind.Furniture;
            root.AddComponent<PlacedFurniture>();  // 곤충 모델이 제 색이라 칠하지 않음
            return SavePrefab(root, "Furniture_Cage_" + insect.id);
        }

        // 그레이박스 채집통(모델이 없을 때): 곤충 모양을 곤충 색으로 칠함
        static GameObject BuildCagePrefab()
        {
            var root = new GameObject("Cage");
            var t = root.transform;
            var wood = Mat("Wood", "#B98A5E");
            Prim(PrimitiveType.Cylinder, "Base", t, new Vector3(0f, 0.04f, 0f), new Vector3(0.5f, 0.04f, 0.5f), wood);
            Prim(PrimitiveType.Cylinder, "Mesh", t, new Vector3(0f, 0.32f, 0f), new Vector3(0.46f, 0.24f, 0.46f), Mat("CageMesh", "#E9F3E3", rim: 0.4f));
            Prim(PrimitiveType.Cylinder, "Lid", t, new Vector3(0f, 0.58f, 0f), new Vector3(0.5f, 0.03f, 0.5f), Mat("CageLid", "#7FB069"));
            Prim(PrimitiveType.Cube, "Leaf", t, new Vector3(-0.08f, 0.2f, 0.18f), new Vector3(0.18f, 0.02f, 0.08f), Mat("Leaves", "#6FC27A"));
            var white = Mat("ThemeFurniture", "#FFFFFF");
            var body = Prim(PrimitiveType.Capsule, "Insect", t, new Vector3(0.04f, 0.3f, 0.24f), new Vector3(0.1f, 0.08f, 0.1f), white).GetComponent<Renderer>();
            body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.3f, 0f);
            collider.size = new Vector3(0.5f, 0.62f, 0.5f);
            AddInteractable(root, "채집통", InteractableKind.Furniture, radius: 0.35f, markerHeight: 1f, wiggle: false);
            root.AddComponent<GridObject>().kind = GroundObjectKind.Furniture;
            root.AddComponent<PlacedFurniture>().bodyRenderers = new[] { body };
            return SavePrefab(root, "Furniture_Cage");
        }

        // 곤충(3-7a): 곤충 모양(몸 + 날개, 곤충 색은 놓일 때 입힘) · 마을 곤충 관리
        static void BuildInsects(GameObject player, VillageGround ground)
        {
            var root = new GameObject("Insect");
            var t = root.transform;
            var white = Mat("ThemeFurniture", "#FFFFFF");
            var body = Prim(PrimitiveType.Capsule, "Body", t, Vector3.zero, new Vector3(0.12f, 0.1f, 0.12f), white, castShadows: false);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var wings = Mat("InsectWing", "#F7FBFF", rim: 0.4f);
            Prim(PrimitiveType.Cube, "WingL", t, new Vector3(-0.09f, 0.04f, 0f), new Vector3(0.14f, 0.01f, 0.1f), wings, castShadows: false);
            Prim(PrimitiveType.Cube, "WingR", t, new Vector3(0.09f, 0.04f, 0f), new Vector3(0.14f, 0.01f, 0.1f), wings, castShadows: false);
            AddInteractable(root, "곤충", InteractableKind.Insect, radius: 0.3f, markerHeight: 0.5f, wiggle: false);
            root.AddComponent<InsectBehaviour>().bodyRenderers = new[] { body.GetComponent<Renderer>() };
            root.transform.localScale = Vector3.one * 1.7f;  // 기본 카메라 거리에서도 보이게(그레이박스, 상호작용 반경도 함께 커짐)
            var prefab = SavePrefab(root, "Insect").GetComponent<InsectBehaviour>();

            var manager = new GameObject("Insects").AddComponent<Insects>();
            manager.rules = EnsureAsset<InsectRules>(InsectRulesPath);
            manager.settings = settings;
            manager.ground = ground;
            manager.dayCycle = Object.FindAnyObjectByType<DayCycle>();
            manager.player = player.transform;
            manager.controller = player.GetComponent<PlayerController>();
            manager.tools = player.GetComponent<PlayerTools>();
            manager.inventory = player.GetComponent<PlayerInventory>();
            manager.pickupPrefab = pickupPrefab;
            manager.insectPrefab = prefab;

            // 곤충 도감(3-7b): 게시판에서 여는 목록 창, 잡은 종류는 마을 평가에
            var encyclopedia = new GameObject("InsectEncyclopedia").AddComponent<InsectEncyclopedia>();
            encyclopedia.rules = manager.rules;
            encyclopedia.window = Object.FindAnyObjectByType<ListWindow>();
            manager.encyclopedia = encyclopedia;
            Object.FindAnyObjectByType<VillageRater>().insectEncyclopedia = encyclopedia;
        }

        // 세이브(6-1a): 자면 자동 저장 · 시작할 때 불러오기
        static void BuildSave(GameObject player)
        {
            var save = new GameObject("SaveManager").AddComponent<SaveManager>();
            save.rules = EnsureAsset<SaveRules>(SaveRulesPath);
            save.dayCycle = Object.FindAnyObjectByType<DayCycle>();
            save.morning = Object.FindAnyObjectByType<MorningRunner>();
            save.wallet = player.GetComponent<PlayerWallet>();
            save.inventory = player.GetComponent<PlayerInventory>();
            save.catalogSource = shopData;
            save.fishEncyclopedia = Object.FindAnyObjectByType<FishEncyclopedia>();
            save.insectEncyclopedia = Object.FindAnyObjectByType<InsectEncyclopedia>();
            save.rater = Object.FindAnyObjectByType<VillageRater>();
            save.furnitureCounter = Object.FindAnyObjectByType<FurnitureCounter>();
            save.village = Object.FindAnyObjectByType<Village>();
            save.fishingRules = EnsureAsset<FishingRules>(FishingRulesPath);
            save.insectRules = EnsureAsset<InsectRules>(InsectRulesPath);

            // 땅 · 가구 저장(6-1b)
            var groundSaver = save.gameObject.AddComponent<GroundSaver>();
            groundSaver.ground = Object.FindAnyObjectByType<VillageGround>();
            groundSaver.garden = groundSaver.ground.GetComponent<GardenKeeper>();
            groundSaver.furniture = Object.FindAnyObjectByType<FurnitureKeeper>();
            groundSaver.stumpPrefab = stumpPrefab;
            groundSaver.pickupPrefab = pickupPrefab;
            save.groundSaver = groundSaver;
            save.fishShadows = Object.FindAnyObjectByType<FishShadows>();
            save.insects = Object.FindAnyObjectByType<Insects>();
            save.fishingRod = player.GetComponent<FishingRodUser>();

            // 시작 화면(6-1d): HUD 맨 위 화면 전체 — 저장이 있을 때만 보임
            var font = EnsureFontAsset();
            var hud = Object.FindAnyObjectByType<HudView>().transform;
            var panel = new GameObject("StartPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(hud, false);
            panel.transform.SetAsLastSibling();
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = Hex("#F7EFE6");
            var screen = save.gameObject.AddComponent<StartScreen>();
            screen.save = save;
            screen.dayCycle = save.dayCycle;
            screen.playerLock = player.GetComponent<PlayerLock>();
            screen.panel = panel;
            var center = new Vector2(0.5f, 0.5f);
            HudLabel(panel.transform, "Title", font, 96, TextAlignmentOptions.Center, center, center, new Vector2(0f, 220f), new Vector2(900f, 130f)).text = "포근 마을";
            screen.promptText = HudLabel(panel.transform, "Prompt", font, 40, TextAlignmentOptions.Center, center, center, new Vector2(0f, 80f), new Vector2(1100f, 60f));
            screen.listText = HudLabel(panel.transform, "List", font, 40, TextAlignmentOptions.TopLeft, center, new Vector2(0.5f, 1f), new Vector2(60f, 20f), new Vector2(900f, 160f));
            HudLabel(panel.transform, "Hint", font, 28, TextAlignmentOptions.Center, center, center, new Vector2(0f, -220f), new Vector2(900f, 40f)).text = "위아래 고르기 · F 확인 · Esc 돌아가기";
            panel.SetActive(false);  // 저장이 있으면 StartScreen이 켬
        }

        // 저장 지우기(6-1a, 개발용): 직접 플레이한 저장을 지움 — 게임 안에서는 시작 화면(6-1d)의 '새로 시작'
        [MenuItem("Pogeun Village/Delete Save")]
        static void DeleteSave()
        {
            var rules = AssetDatabase.LoadAssetAtPath<SaveRules>(SaveRulesPath);
            if (!rules)
                return;
            int deleted = 0;
            foreach (string name in new[] { rules.fileName, rules.backupFileName, rules.fileName + ".tmp" })
            {
                string path = System.IO.Path.Combine(Application.persistentDataPath, name);
                if (!System.IO.File.Exists(path)) continue;
                System.IO.File.Delete(path);
                deleted++;
            }
            Debug.Log($"[세이브] 저장 파일 {deleted}개를 지웠어요: {Application.persistentDataPath}");
        }

        // 플레이 기록(4-1 재미 검증 측정 도구, 에디터 · 개발 빌드만): Temp/PlayLogs/*.csv
        static void BuildPlayLogger(GameObject player)
        {
            var logger = new GameObject("PlayLogger").AddComponent<PlayLogger>();
            logger.dayCycle = Object.FindAnyObjectByType<DayCycle>();
            logger.wallet = player.GetComponent<PlayerWallet>();
            logger.inventory = player.GetComponent<PlayerInventory>();
            logger.village = Object.FindAnyObjectByType<Village>();
            logger.rater = Object.FindAnyObjectByType<VillageRater>();
            logger.furniture = Object.FindAnyObjectByType<FurnitureKeeper>();
            logger.wood = woodItem;
        }

        static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static ItemDefinition EnsureItem(string id, string displayName, GroundObjectKind plantsAs = GroundObjectKind.None, int buyPrice = 0, int sellPrice = 0)
        {
            string path = $"{ItemDir}/{id}.asset";
            bool isNew = !AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            var item = EnsureAsset<ItemDefinition>(path);
            if (isNew)
            {
                item.id = id;
                item.displayName = displayName;
                item.plantsAs = plantsAs;
                item.buyPrice = buyPrice;
                item.sellPrice = sellPrice;
                EditorUtility.SetDirty(item);
            }
            return item;
        }

        // 물건이 땅에 떨어졌을 때 보일 모델(Pickup이 공 대신 보임). 크기는 ModelLibrary 높이에 맞춤
        static void SetPickupLook(ItemDefinition item, string modelId)
        {
            var entry = models ? models.Find(modelId) : null;
            if (entry == null || !entry.model)
            {
                Debug.LogError($"모델 '{modelId}'이 ModelLibrary에 없습니다 — 메뉴 Pogeun Village/Import Varco Models");
                return;
            }
            var probe = (GameObject)Object.Instantiate(entry.model);
            float height = probe.GetComponentsInChildren<MeshRenderer>().Max(r => r.bounds.max.y);
            Object.DestroyImmediate(probe);
            item.pickupModel = entry.model;
            item.pickupMaterial = entry.material;
            item.pickupScale = entry.height / Mathf.Max(height, 0.01f);
            EditorUtility.SetDirty(item);
        }

        static ToolDefinition EnsureTool(string id, string displayName, ToolKind kind, bool usesGroundCell, params InteractableKind[] hitKinds)
        {
            string path = $"{ToolDir}/{id}.asset";
            bool isNew = !AssetDatabase.LoadAssetAtPath<ToolDefinition>(path);
            var tool = EnsureAsset<ToolDefinition>(path);
            if (isNew)
            {
                tool.id = id;
                tool.displayName = displayName;
                tool.kind = kind;
                tool.maxStack = 1;
                tool.hitKinds = hitKinds;
                tool.usesGroundCell = usesGroundCell;
                tool.buyPrice = 500;
                EditorUtility.SetDirty(tool);
            }
            return tool;
        }

        // 이미 있는 도구 데이터에도 맞는 대상을 더함(여러 번 실행해도 같음)
        static ToolDefinition EnsureHitKinds(ToolDefinition tool, params InteractableKind[] kinds)
        {
            var list = tool.hitKinds.ToList();
            foreach (var k in kinds)
                if (!list.Contains(k)) list.Add(k);
            if (list.Count != tool.hitKinds.Length)
            {
                tool.hitKinds = list.ToArray();
                EditorUtility.SetDirty(tool);
            }
            return tool;
        }

        // 베어진 나무의 밑동(삽으로만 없앰 → 1-8). 크기는 베어진 나무 배율을 따름
        static GameObject BuildStumpPrefab()
        {
            var root = new GameObject("Stump");
            ColliderOnly(Prim(PrimitiveType.Cylinder, "Trunk", root.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.42f, 0.15f, 0.42f), Mat("Trunk", "#A57A52"), collider: true));
            FitModel("Stump", root.transform, Vector3.zero, new Vector3(0.5f, 0f, 0f));  // 밑동 모델(P-4i): 폭 0.5(뿌리 포함), 높이는 비율대로
            AddInteractable(root, "밑동", InteractableKind.Stump, radius: 0.35f, markerHeight: 0.9f, wiggle: false);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/Stump.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        // 떨어진 열매용 줍기 프리팹. 재질은 떨어뜨리는 나무가 자기 열매 재질로 바꿈
        static Pickup BuildPickupPrefab()
        {
            var root = new GameObject("Pickup");
            Prim(PrimitiveType.Sphere, "Ball", root.transform, new Vector3(0f, 0.15f, 0f), Vector3.one * 0.3f, Mat("Apple", "#F0645A", rim: 0.3f));
            AddInteractable(root, "열매", InteractableKind.DroppedItem, radius: 0.2f, markerHeight: 0.7f, wiggle: false);
            root.AddComponent<Pickup>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/Pickup.prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<Pickup>();
        }

        static Camera BuildCamera(Transform target)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 35f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 250f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("#A9DDF5");
            go.AddComponent<AudioListener>();

            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            var follow = go.AddComponent<CameraFollow>();
            follow.settings = settings;
            follow.target = target;
            go.transform.SetPositionAndRotation(
                CameraOrbit.Position(target.position + Vector3.up * settings.cameraHeightOffset, settings.cameraPitch, 0f, settings.cameraDistance),
                CameraOrbit.Rotation(settings.cameraPitch, 0f));
            return cam;
        }

        // 인트로(P-5): 맵 바깥 아이 방(그레이박스, 저녁) · 모니터 화면 · 방 카메라(휨 없음) · 시작 화면 연결
        static void BuildIntro(GameObject player, Camera mainCamera)
        {
            var intro = EnsureAsset<IntroSettings>(IntroSettingsPath);
            var room = new GameObject("IntroRoom").transform;
            room.position = intro.roomOrigin;

            // 벽 · 바닥은 큰 평면이라 색 상자 그대로(저녁 톤), 소품은 Varco3D 모델(P-5d, 정면 +Z → 카메라(-Z)를 보게 yaw 180)
            var floor = Mat("RoomFloor", "#9C7458");
            var wall = Mat("RoomWall", "#7F70A0");
            Prim(PrimitiveType.Cube, "Floor", room, new Vector3(0f, -0.05f, 0f), new Vector3(5f, 0.1f, 5f), floor);
            Prim(PrimitiveType.Cube, "WallBack", room, new Vector3(0f, 1.4f, 2.2f), new Vector3(5f, 2.8f, 0.1f), wall);
            Prim(PrimitiveType.Cube, "WallLeft", room, new Vector3(-2.2f, 1.4f, 0f), new Vector3(0.1f, 2.8f, 5f), wall);
            Prim(PrimitiveType.Cube, "WallRight", room, new Vector3(2.2f, 1.4f, 0f), new Vector3(0.1f, 2.8f, 5f), wall);
            var window = RoomModel("RoomWindow", room, new Vector3(-1.1f, 1.0f, 2.15f), 1.1f, againstWall: true);

            // 책상 · 모니터(화면 앞 = -Z 쪽 카메라를 봄) · 키보드 · 마우스 · 스탠드 · 머그 — 폭(m)에 맞추고 책상 윗면에 올림
            var desk = RoomModel("RoomDesk", room, new Vector3(0f, 0f, 1.3f), 1.6f, height: 0.76f, depth: 0.8f, yaw: 90f);  // 긴 변이 모델 Z축
            float deskTop = ModelBounds(desk).max.y - room.position.y;
            var monitor = RoomModel("RoomMonitor", room, new Vector3(0f, deskTop, 1.5f), intro.screenHeight * MonitorWidthPerScreenHeight);
            RoomModel("RoomKeyboard", room, new Vector3(-0.05f, deskTop, 1.05f), 0.44f);
            RoomModel("RoomMouse", room, new Vector3(0.32f, deskTop, 1.05f), 0.07f, yaw: 0f);  // 버튼(모델 +Z)이 모니터 쪽
            var lamp = RoomModel("RoomLamp", room, new Vector3(-0.6f, deskTop, 1.45f), 0.26f);
            RoomModel("RoomMug", room, new Vector3(0.55f, deskTop, 1.25f), 0.1f);

            // 모니터 화면 쿼드: 모델 앞면 바로 앞, 화면 영역 가운데(MonitorScreenCenter = 모델 높이 비율). 크기는 인트로가 정함(screenHeight × 화면 비율)
            var mb = ModelBounds(monitor);
            var screenCenter = new Vector3(mb.min.x + mb.size.x * MonitorScreenCenterX, mb.min.y + mb.size.y * MonitorScreenCenter, MonitorScreenSurfaceZ(monitor, mb) - 0.003f) - room.position;
            var screen = Prim(PrimitiveType.Quad, "MonitorScreen", room, screenCenter, Vector3.one, ScreenMaterial(), castShadows: false);

            // 저녁 조명(P-5d): 방 물건은 해를 거의 받지 않고(재질 _MainLightScale), 스탠드 · 모니터 · 노을 창 점 조명으로 밝힘
            foreach (var renderer in room.GetComponentsInChildren<Renderer>())
                if (renderer.sharedMaterial && renderer.sharedMaterial.HasProperty("_MainLightScale"))
                {
                    renderer.sharedMaterial.SetFloat("_MainLightScale", intro.roomSunlight);
                    renderer.sharedMaterial.SetFloat("_AmbientStrength", intro.roomAmbient);
                    EditorUtility.SetDirty(renderer.sharedMaterial);
                }
            var lb = ModelBounds(lamp);
            RoomLight("LampLight", room, new Vector3(lb.center.x, lb.max.y - lb.size.y * 0.35f, lb.min.z - 0.03f), intro.lampLightColor, intro.lampLightIntensity, intro.lampLightRange);
            RoomLight("MonitorLight", room, screen.transform.position + new Vector3(0f, 0f, -0.35f), intro.monitorLightColor, intro.monitorLightIntensity, intro.monitorLightRange);
            var wb = ModelBounds(window);
            RoomLight("WindowLight", room, new Vector3(wb.center.x, wb.center.y, wb.min.z - 0.4f), intro.windowLightColor, intro.windowLightIntensity, intro.windowLightRange);

            // 방 카메라(처음엔 꺼 둠, 휨 없음) — 마을 카메라보다 나중에 그림
            var camGo = new GameObject("IntroRoomCamera");
            camGo.transform.SetParent(room, false);
            var roomCamera = camGo.AddComponent<Camera>();
            roomCamera.depth = mainCamera.depth + 1f;
            roomCamera.nearClipPlane = 0.05f;
            roomCamera.farClipPlane = 30f;
            roomCamera.clearFlags = CameraClearFlags.SolidColor;
            roomCamera.backgroundColor = Hex("#2E2A3A");
            roomCamera.enabled = false;
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;  // 화면 속 마을은 이미 후처리됨 — 두 번 하면 컷에서 어두워짐
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camGo.AddComponent<FlatWorldCamera>();

            var sequence = room.gameObject.AddComponent<IntroSequence>();
            sequence.settings = intro;
            sequence.mainCamera = mainCamera;
            sequence.follow = mainCamera.GetComponent<CameraFollow>();
            sequence.roomCamera = roomCamera;
            sequence.screen = screen.GetComponent<Renderer>();
            sequence.player = player.transform;
            sequence.playerVisual = player.transform.Find("Visual").gameObject;
            sequence.playerLock = player.GetComponent<PlayerLock>();
            sequence.dayCycle = Object.FindAnyObjectByType<DayCycle>();
            sequence.village = Object.FindAnyObjectByType<Village>();
            sequence.hud = Object.FindAnyObjectByType<HudView>().GetComponent<Canvas>();
            Object.FindAnyObjectByType<StartScreen>().intro = sequence;
            BuildIntroScreenUi(sequence, mainCamera);

            // 착지 먼지(P-5c, P-4l): 작은 구름 6개가 둥글게 퍼지고 떠오르며 사라짐 — 자리 · 크기는 인트로가 정함
            var dust = new GameObject("LandingDust").transform;
            var puff = Mat("Dust", "#F3E6CF", rim: 0.3f);
            for (int i = 0; i < 6; i++)
            {
                var cloud = new GameObject("Puff").transform;
                cloud.SetParent(dust, false);
                cloud.localRotation = Quaternion.Euler(0f, i * 73f, 0f);
                Prim(PrimitiveType.Sphere, "Lump", cloud, Vector3.zero, Vector3.one, puff, castShadows: false);
                Prim(PrimitiveType.Sphere, "Lump", cloud, new Vector3(0.55f, -0.1f, 0.1f), Vector3.one * 0.7f, puff, castShadows: false);
                Prim(PrimitiveType.Sphere, "Lump", cloud, new Vector3(-0.4f, 0.1f, -0.3f), Vector3.one * 0.6f, puff, castShadows: false);
            }
            dust.gameObject.SetActive(false);
            sequence.landingDust = dust;
        }

        // 인트로 방 모니터 모델(P-5d): 모델 정면을 렌더해 잰 어두운 화면 칸(모델 폭 0.10 ~ 0.91 · 높이 0.42 ~ 0.88)에
        // 16:9 화면이 위아래 4% 여백을 두고 들어가는 값 — 화면 높이 대비 모델 폭, 화면 가운데(모델 폭 · 높이 비율). 모델을 바꾸면 다시 잼
        const float MonitorWidthPerScreenHeight = 2.49f;
        const float MonitorScreenCenterX = 0.505f;
        const float MonitorScreenCenter = 0.651f;

        // 인트로 방 소품(P-5d): 모델을 yaw만큼 돌려(기본 180 = 정면이 방 카메라 쪽 -Z) 폭(m)에 맞추고 바닥을 자리에 둠.
        // height · depth를 주면 그 축만 따로 맞춤(책상처럼 비율이 어긋난 모델). againstWall이면 뒷면을 그 Z(벽 앞)에 붙임
        static GameObject RoomModel(string id, Transform room, Vector3 localPosition, float width, float height = 0f, float depth = 0f,
            float yaw = 180f, bool againstWall = false)
        {
            var fit = new GameObject("Model_" + id).transform;  // 돌리지 않은 그릇 — 축마다 크기를 맞춤
            fit.SetParent(room, false);
            fit.localPosition = localPosition;
            var turn = ModelVisual(id, fit, Vector3.zero, castShadows: !againstWall).transform;
            turn.name = "Turn";
            turn.localRotation = Quaternion.Euler(0f, yaw, 0f);

            var size = ModelBounds(fit.gameObject).size;
            float sx = width / Mathf.Max(size.x, 0.0001f);
            fit.localScale = new Vector3(sx, height > 0f ? height / Mathf.Max(size.y, 0.0001f) : sx, depth > 0f ? depth / Mathf.Max(size.z, 0.0001f) : sx);

            var bounds = ModelBounds(fit.gameObject);
            var target = room.TransformPoint(localPosition);
            fit.position += new Vector3(target.x - bounds.center.x, target.y - bounds.min.y, againstWall ? target.z - bounds.max.z : target.z - bounds.center.z);
            return fit.gameObject;
        }

        // 모델을 부모 축 기준 크기(m)에 맞춰 놓음(P-4f 시설). size의 0인 축은 정한 축의 비율을 따름(기준은 y → x → z 순).
        // 바닥 높이 · 가로 가운데 · 앞뒤 가운데(alignBack이면 뒤 끝)가 localPosition. 모델 정면(+z)은 yaw만큼, 누운 모델은 pitch(x축)만큼 세움
        static GameObject FitModel(string id, Transform parent, Vector3 localPosition, Vector3 size, float yaw = 0f, bool alignBack = false, float pitch = 0f)
        {
            var fit = new GameObject("Fit_" + id).transform;  // 원점에서 재고 맞춘 뒤 부모에 붙임
            var turn = ModelVisual(id, fit, Vector3.zero).transform;
            turn.localRotation = Quaternion.Euler(pitch, yaw, 0f) * turn.localRotation;
            var raw = ModelBounds(fit.gameObject).size;
            float ratio = size.y > 0f ? size.y / raw.y : size.x > 0f ? size.x / raw.x : size.z / raw.z;
            fit.localScale = new Vector3(size.x > 0f ? size.x / raw.x : ratio, size.y > 0f ? size.y / raw.y : ratio, size.z > 0f ? size.z / raw.z : ratio);
            var b = ModelBounds(fit.gameObject);
            var offset = new Vector3(b.center.x, b.min.y, alignBack ? b.min.z : b.center.z);
            turn.localPosition -= new Vector3(offset.x / fit.localScale.x, offset.y / fit.localScale.y, offset.z / fit.localScale.z);
            fit.SetParent(parent, false);
            fit.localPosition = localPosition;
            return fit.gameObject;
        }

        // 가구 그레이박스를 모델로(P-4b · P-4c): 도형 렌더러는 지우고(충돌 상자 · 상호작용은 그대로), 모델을 충돌 상자 높이에 맞춤.
        // 한 칸을 넘지 않게 가로 · 세로는 FurnitureCellLimit 안으로 줄임. 모델은 제 색이라 주민 색 칠하기(PlacedFurniture.Tint)는 끔
        static bool SwapToModel(PlacedFurniture placed, string modelId)
        {
            var entry = models ? models.Find(modelId) : null;
            if (entry == null || !entry.model)
                return false;
            var root = placed.gameObject;
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
                Object.DestroyImmediate(r.gameObject);
            float height = root.GetComponent<BoxCollider>().size.y;
            var fit = FitModel(modelId, root.transform, Vector3.zero, new Vector3(0f, height, 0f));
            var size = ModelBounds(fit).size;
            fit.transform.localScale *= Mathf.Min(1f, FurnitureCellLimit / size.x, FurnitureCellLimit / size.z);
            placed.bodyRenderers = new Renderer[0];
            placed.accentRenderers = new Renderer[0];
            return true;
        }

        // 모니터 화면 칸의 표면 깊이(Z): 화면 칸 가운데 영역의 정점 중 가장 앞(-Z) — 테두리보다 들어가 있어 쿼드를 테두리 앞에 띄우지 않음
        static float MonitorScreenSurfaceZ(GameObject monitor, Bounds bounds)
        {
            float z = float.MaxValue;
            foreach (var filter in monitor.GetComponentsInChildren<MeshFilter>())
            {
                var toWorld = filter.transform.localToWorldMatrix;
                foreach (var v in filter.sharedMesh.vertices)
                {
                    var p = toWorld.MultiplyPoint3x4(v);
                    float fx = (p.x - bounds.min.x) / bounds.size.x, fy = (p.y - bounds.min.y) / bounds.size.y;
                    if (fx > 0.25f && fx < 0.75f && fy > 0.5f && fy < 0.8f)
                        z = Mathf.Min(z, p.z);
                }
            }
            return z == float.MaxValue ? bounds.min.z : z;
        }

        static void RoomLight(string name, Transform room, Vector3 worldPosition, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(room, false);
            go.transform.position = worldPosition;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        static Bounds ModelBounds(GameObject go)
        {
            var bounds = new Bounds(go.transform.position, Vector3.zero);
            bool first = true;
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                if (first) { bounds = renderer.bounds; first = false; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        // 인트로 화면 속 UI(P-5b): 마을 카메라에 그려져 모니터에 보이는 제목 · '게임 시작' 버튼 · 커서, 맨 위 흰 섬광
        static void BuildIntroScreenUi(IntroSequence sequence, Camera mainCamera)
        {
            var font = EnsureFontAsset();
            var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var center = new Vector2(0.5f, 0.5f);

            var uiGo = new GameObject("IntroScreenUI", typeof(RectTransform));
            var ui = uiGo.AddComponent<Canvas>();
            ui.renderMode = RenderMode.ScreenSpaceCamera;
            ui.worldCamera = mainCamera;
            ui.planeDistance = 1f;
            var scaler = uiGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            HudPlate(uiGo.transform, "TitlePlate", center, center, new Vector2(0f, 170f), new Vector2(760f, 170f));
            var title = HudLabel(uiGo.transform, "Title", font, 120, TextAlignmentOptions.Center, center, center, new Vector2(0f, 170f), new Vector2(1200f, 160f));
            title.text = "포근 마을";

            var button = new GameObject("StartButton", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            button.SetParent(uiGo.transform, false);
            button.anchorMin = button.anchorMax = button.pivot = center;
            button.anchoredPosition = new Vector2(0f, -120f);
            button.sizeDelta = new Vector2(440f, 120f);
            var buttonImage = button.GetComponent<Image>();
            buttonImage.sprite = rounded;
            buttonImage.type = Image.Type.Sliced;
            buttonImage.color = Hex("#F49AB4");
            buttonImage.raycastTarget = false;
            var label = HudLabel(button, "Label", font, 56, TextAlignmentOptions.Center, center, center, Vector2.zero, new Vector2(420f, 110f));
            label.text = "게임 시작";
            label.color = Color.white;

            // 커서(P-4l): 흰 화살표 + 테두리, 피벗 = 화살 끝
            var cursor = new GameObject("Cursor", typeof(RectTransform), typeof(CanvasRenderer), typeof(CursorArrow), typeof(Outline)).GetComponent<RectTransform>();
            cursor.SetParent(uiGo.transform, false);
            cursor.anchorMin = cursor.anchorMax = center;
            cursor.pivot = new Vector2(0f, 1f);
            cursor.sizeDelta = new Vector2(64f, 76f);
            var cursorImage = cursor.GetComponent<CursorArrow>();
            cursorImage.color = Color.white;
            cursorImage.raycastTarget = false;
            var outline = cursor.GetComponent<Outline>();
            outline.effectColor = Hex("#3A3340");
            outline.effectDistance = new Vector2(3f, -3f);
            cursor.gameObject.SetActive(false);
            ui.enabled = false;

            var flashGo = new GameObject("IntroFlash", typeof(RectTransform));
            var flashCanvas = flashGo.AddComponent<Canvas>();
            flashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            flashCanvas.sortingOrder = 100;
            var flash = new GameObject("Flash", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            flash.transform.SetParent(flashGo.transform, false);
            var frt = flash.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = frt.offsetMax = Vector2.zero;
            flash.color = new Color(1f, 0.98f, 0.94f, 0f);
            flash.raycastTarget = false;
            flash.enabled = false;

            sequence.screenUi = ui;
            sequence.startButton = button;
            sequence.startButtonImage = buttonImage;
            sequence.cursor = cursor;
            sequence.flash = flash;
        }

        // 모니터 화면 재질(빛을 받지 않음, 텍스처는 인트로가 프로퍼티 블록으로)
        static Material ScreenMaterial()
        {
            string path = $"{MaterialDir}/IntroScreen.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("PogeunVillage/CurvedScreen");
            if (!mat)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ---------------------------------------------------------------- lighting & post

        static void SetupLighting()
        {
            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Hex("#FFF3DE");
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.85f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            RenderSettings.sun = light;

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("#D3E9FF");
            RenderSettings.ambientEquatorColor = Hex("#EAF2DA");
            RenderSettings.ambientGroundColor = Hex("#B9CBA2");

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Hex("#CDEBF8");
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 95f;

            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.shadowDistance = 60f;
                EditorUtility.SetDirty(urp);
            }
        }

        static void SetupVolume()
        {
            string path = SettingsDir + "/VillageVolumeProfile.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var tonemap = profile.Add<Tonemapping>(true);
            tonemap.mode.Override(TonemappingMode.Neutral);
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.35f);
            bloom.threshold.Override(0.95f);
            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(15f);
            color.postExposure.Override(0f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.18f);

            foreach (var component in profile.components)
            {
                component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            EditorUtility.SetDirty(profile);

            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        // ---------------------------------------------------------------- helpers

        static Material Mat(string name, string hex, float rim = 0.12f, bool vertexColor = false)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(toonShader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = toonShader;
            mat.SetColor("_BaseColor", Hex(hex));
            mat.SetFloat("_RimStrength", rim);
            mat.SetFloat("_AmbientStrength", 0.25f);
            mat.SetFloat("_UseVertexColor", vertexColor ? 1f : 0f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale,
            Material material, bool collider = false, bool castShadows = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            if (!castShadows)
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            if (!collider)
                Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        // Varco3D 모델(P-3, MODELING_SPEC.md): 원본을 그대로 자식으로 두고 바깥 그릇에서 높이(ModelLibrary) · 정면 회전을 맞춤. 충돌 없음
        static ModelLibrary models;

        static GameObject ModelVisual(string id, Transform parent, Vector3 localPosition, bool castShadows = true)
        {
            var entry = models ? models.Find(id) : null;
            var holder = new GameObject("Model_" + id);  // 원점에서 크기를 잰 뒤 부모에 붙임(이름은 원래 물체와 겹치지 않게)
            if (entry == null || !entry.model)
            {
                Debug.LogError($"모델 '{id}'이 ModelLibrary에 없습니다 — 메뉴 Pogeun Village/Import Varco Models");
                holder.transform.SetParent(parent, false);
                return holder;
            }
            var model = (GameObject)PrefabUtility.InstantiatePrefab(entry.model, holder.transform);
            float height = 0f;
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.sharedMaterial = entry.material;
                if (!castShadows)
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                height = Mathf.Max(height, renderer.bounds.max.y);
            }
            holder.transform.SetParent(parent, false);
            holder.transform.localPosition = localPosition;
            holder.transform.localRotation = Quaternion.Euler(0f, entry.yaw, 0f);
            holder.transform.localScale = Vector3.one * (entry.height / Mathf.Max(height, 0.01f));
            return holder;
        }

        // 모델 바닥 크기를 길이(부모 X) · 폭(부모 Z)에 맞춤 — 높이는 폭과 같은 비율. 그릇 회전(yaw)은 90도 단위
        static void FitFootprint(GameObject holder, float length, float width)
        {
            var t = holder.transform;
            var (position, rotation, parent) = (t.localPosition, t.localRotation, t.parent);
            t.SetParent(null, false);
            t.position = Vector3.zero;
            t.rotation = rotation;
            t.localScale = Vector3.one;
            var bounds = new Bounds();
            bool first = true;
            foreach (var renderer in holder.GetComponentsInChildren<MeshRenderer>())
            {
                if (first) { bounds = renderer.bounds; first = false; }
                else bounds.Encapsulate(renderer.bounds);
            }
            // 그릇 축으로 본 크기: 회전 뒤 월드 X · Z가 부모의 길이 · 폭
            Vector3 local = Quaternion.Inverse(rotation) * new Vector3(length / bounds.size.x, 0f, width / bounds.size.z);
            float sw = width / bounds.size.z;
            t.localScale = new Vector3(Mathf.Abs(local.x), sw, Mathf.Abs(local.z));
            t.SetParent(parent, false);
            t.localPosition = position;
            t.localRotation = rotation;
        }

        // 보이는 몸통은 모델이 맡고 기본 도형은 충돌만 남김
        static void ColliderOnly(GameObject go)
        {
            Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(go.GetComponent<MeshFilter>());
        }

        static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        static Mesh SaveMesh(Mesh mesh, string name)
        {
            string path = $"{MeshDir}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // 빈 집터 흙더미: 모서리 둥근 판, 가장자리가 부드럽게 땅으로 내려옴(점을 공유해 음영이 둥글게)
        const float LotWidth = 4.2f, LotDepth = 3.8f, LotHeight = 0.09f, LotCorner = 0.7f, LotSlope = 0.35f;
        const int LotCornerSegments = 6;
        static Mesh lotMesh;
        static Mesh LotMesh()
        {
            if (lotMesh && AssetDatabase.Contains(lotMesh))
                return lotMesh;
            (float inset, float y)[] rings = { (LotSlope, LotHeight), (LotSlope * 0.35f, LotHeight * 0.7f), (0f, 0f) };  // 위 → 바닥
            int perRing = 4 * (LotCornerSegments + 1);
            var vertices = new List<Vector3> { new(0f, LotHeight, 0f) };
            foreach (var (inset, y) in rings)
                for (int corner = 0; corner < 4; corner++)
                {
                    float cx = (corner is 0 or 3 ? 1f : -1f) * (LotWidth * 0.5f - LotCorner);
                    float cz = (corner < 2 ? 1f : -1f) * (LotDepth * 0.5f - LotCorner);
                    for (int k = 0; k <= LotCornerSegments; k++)
                    {
                        float a = (corner + (float)k / LotCornerSegments) * Mathf.PI * 0.5f;
                        vertices.Add(new Vector3(cx + Mathf.Cos(a) * (LotCorner - inset), y, cz + Mathf.Sin(a) * (LotCorner - inset)));
                    }
                }
            var tris = new List<int>();
            for (int i = 0; i < perRing; i++)
            {
                int next = (i + 1) % perRing;
                tris.AddRange(new[] { 0, 1 + next, 1 + i });
                for (int ring = 0; ring < rings.Length - 1; ring++)
                {
                    int a = 1 + ring * perRing, b = a + perRing;
                    tris.AddRange(new[] { a + i, a + next, b + i, a + next, b + next, b + i });
                }
            }
            var mesh = new Mesh { name = "EmptyLot", vertices = vertices.ToArray(), triangles = tris.ToArray() };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            lotMesh = SaveMesh(mesh, "EmptyLot");
            return lotMesh;
        }

        static Mesh roofMesh;
        static Mesh RoofMesh()
        {
            if (roofMesh && AssetDatabase.Contains(roofMesh))
                return roofMesh;

            // 단면이 삼각형인 지붕, 용마루는 X축 방향. 바닥 중심이 원점.
            Vector3 fl = new(-0.5f, 0f, 0.5f), fr = new(0.5f, 0f, 0.5f);
            Vector3 bl = new(-0.5f, 0f, -0.5f), br = new(0.5f, 0f, -0.5f);
            Vector3 tl = new(-0.5f, 1f, 0f), tr = new(0.5f, 1f, 0f);
            var vertices = new List<Vector3>();
            var tris = new List<int>();
            void Face(params Vector3[] v)
            {
                int start = vertices.Count;
                vertices.AddRange(v);
                for (int i = 1; i < v.Length - 1; i++)
                {
                    tris.Add(start);
                    tris.Add(start + i + 1);
                    tris.Add(start + i);
                }
            }
            Face(fl, tl, tr, fr);   // 앞 경사면
            Face(br, tr, tl, bl);   // 뒤 경사면
            Face(bl, tl, fl);       // 왼쪽 박공
            Face(fr, tr, br);       // 오른쪽 박공
            Face(bl, fl, fr, br);   // 바닥

            var mesh = new Mesh { name = "Roof", vertices = vertices.ToArray(), triangles = tris.ToArray() };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            roofMesh = SaveMesh(mesh, "Roof");
            return roofMesh;
        }

        static Mesh coneMesh;
        static Mesh ConeMesh()
        {
            if (coneMesh && AssetDatabase.Contains(coneMesh))
                return coneMesh;

            const int segments = 10;
            var vertices = new List<Vector3>();
            var tris = new List<int>();
            Vector3 apex = Vector3.up;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments;
                float a1 = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 p0 = new(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f);
                Vector3 p1 = new(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f);
                int s = vertices.Count;
                vertices.Add(p0); vertices.Add(apex); vertices.Add(p1);
                tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
                s = vertices.Count;
                vertices.Add(p1); vertices.Add(Vector3.zero); vertices.Add(p0);
                tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
            }

            var mesh = new Mesh { name = "Cone", vertices = vertices.ToArray(), triangles = tris.ToArray() };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            coneMesh = SaveMesh(mesh, "Cone");
            return coneMesh;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        static void AddSceneToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
