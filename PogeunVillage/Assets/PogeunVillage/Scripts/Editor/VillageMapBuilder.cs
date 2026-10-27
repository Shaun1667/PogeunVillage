using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
        const string ItemDir = Root + "/Data/Items";
        const string PrefabDir = Root + "/Prefabs";
        const string ToolDir = ItemDir + "/Tools";

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
            public string wall;
            public string roof;
            public bool onPlateau;
        }

        static readonly HouseInfo[] Houses =
        {
            new() { name = "House_Rabbit", position = new(-22f, 10f), wall = "#FFE4EC", roof = "#F49AB0" },
            new() { name = "House_Bear", position = new(-24f, -6f), wall = "#E3F6EA", roof = "#7CC7A0" },
            new() { name = "House_Cat", position = new(-8f, 15f), wall = "#FFF6D6", roof = "#F2B84B" },
            new() { name = "House_Fox", position = new(-14f, -16f), wall = "#EEE6FA", roof = "#A88BD8" },
            new() { name = "House_Raccoon", position = new(0f, -16f), wall = "#FFE9DC", roof = "#F08F6E" },
            new() { name = "House_Sheep", position = new(28f, 4f), wall = "#E2F2FB", roof = "#6FAFD8" },
            new() { name = "House_Owl", position = new(-16f, 36f), wall = "#FFF8EC", roof = "#C99A6B", onPlateau = true },
            new() { name = "House_Deer", position = new(4f, 38f), wall = "#FFEDE6", roof = "#E06F6F", onPlateau = true },
        };

        static readonly float[] BridgeZs = { 4f, -24f };

        static Shader toonShader;
        static WorldSettings settings;
        static GatheringRules gatheringRules;
        static InventoryRules inventoryRules;
        static ItemDefinition appleItem, peachItem, flowerItem;
        static Pickup pickupPrefab;
        static ToolDefinition[] tools;
        static ItemDefinition woodItem, flowerClumpItem, saplingItem;
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

            // 채집 데이터 · 프리팹 (없을 때만 만들고, 있으면 값을 건드리지 않음)
            EnsureFolder(ItemDir);
            EnsureFolder(PrefabDir);
            gatheringRules = EnsureAsset<GatheringRules>(GatheringRulesPath);
            inventoryRules = EnsureAsset<InventoryRules>(InventoryRulesPath);
            appleItem = EnsureItem("Apple", "사과");
            peachItem = EnsureItem("Peach", "복숭아");
            flowerItem = EnsureItem("Flower", "꽃");
            pickupPrefab = BuildPickupPrefab();
            woodItem = EnsureItem("Wood", "목재");
            flowerClumpItem = EnsureItem("FlowerClump", "꽃 포기", GroundObjectKind.Flower);
            saplingItem = EnsureItem("Sapling", "묘목", GroundObjectKind.Sapling);
            stumpPrefab = BuildStumpPrefab();
            EnsureFolder(ToolDir);
            tools = new[]
            {
                EnsureTool("Net", "잠자리채", ToolKind.Net, false),
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
            foreach (var house in Houses)
                BuildHouse(props, house);
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
            var cam = BuildCamera(player.transform);
            player.GetComponent<PlayerController>().cameraTransform = cam.transform;

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
            Prim(PrimitiveType.Cylinder, "FountainWater", plaza, new Vector3(0f, 0.32f, 0f), new Vector3(3.9f, 0.05f, 3.9f), Mat("FountainWater", "#8EDCF0", rim: 0.4f));
            Prim(PrimitiveType.Cylinder, "FountainPillar", plaza, new Vector3(0f, 0.9f, 0f), new Vector3(0.6f, 0.7f, 0.6f), stone);
            Prim(PrimitiveType.Cylinder, "FountainBowl", plaza, new Vector3(0f, 1.6f, 0f), new Vector3(1.8f, 0.12f, 1.8f), stone);
            Prim(PrimitiveType.Sphere, "FountainTop", plaza, new Vector3(0f, 1.95f, 0f), new Vector3(0.5f, 0.6f, 0.5f), Mat("FountainWater", "#8EDCF0", rim: 0.4f));

            // 광장 게시판
            var wood = Mat("Wood", "#B98A5E");
            var board = new GameObject("NoticeBoard").transform;
            board.SetParent(parent, false);
            board.localPosition = OnGround(PlazaCenter.x + 4f, PlazaCenter.y + 4.5f);
            board.localRotation = Quaternion.Euler(0f, 215f, 0f);
            Prim(PrimitiveType.Cube, "Post_L", board, new Vector3(-0.8f, 0.7f, 0f), new Vector3(0.15f, 1.4f, 0.15f), wood);
            Prim(PrimitiveType.Cube, "Post_R", board, new Vector3(0.8f, 0.7f, 0f), new Vector3(0.15f, 1.4f, 0.15f), wood);
            Prim(PrimitiveType.Cube, "Board", board, new Vector3(0f, 1.15f, 0f), new Vector3(1.9f, 0.9f, 0.12f), Mat("BoardPaper", "#FFF4D8"), collider: true);
            AddInteractable(board.gameObject, "게시판", InteractableKind.NoticeBoard, radius: 1f, markerHeight: 2f);

            // 임시 도구 상자 (2-2 상점이 생기면 없앰)
            var toolBox = Prim(PrimitiveType.Cube, "ToolBox", parent, OnGround(PlazaCenter.x - 4f, PlazaCenter.y - 3f, 0.3f), new Vector3(1f, 0.6f, 0.7f), Mat("ToolBox", "#E07A5F"), collider: true);
            toolBox.transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
            AddInteractable(toolBox, "도구 상자", InteractableKind.ToolBox, radius: 0.5f, markerHeight: 1.5f); // 높이 배율 0.6 → 상자 중심 위 0.9m
            var toolBoxGiver = toolBox.AddComponent<ToolBox>();
            toolBoxGiver.tools = tools;
            toolBoxGiver.sapling = saplingItem;
            toolBoxGiver.saplingCount = 3;
        }

        static void BuildHouse(Transform parent, HouseInfo info)
        {
            var house = new GameObject(info.name).transform;
            house.SetParent(parent, false);
            house.localPosition = OnGround(info.position.x, info.position.y, -0.05f);
            Vector2 forward = FacingTarget(info) - info.position;
            house.localRotation = Quaternion.LookRotation(new Vector3(forward.x, 0f, forward.y));
            Occupied.Add(new Vector3(info.position.x, info.position.y, 4f));

            var wall = Mat("Wall_" + info.name, info.wall);
            var roof = Mat("Roof_" + info.name, info.roof, rim: 0.2f);
            var wood = Mat("Wood", "#B98A5E");
            var glass = Mat("Window", "#BFE7F7", rim: 0.4f);

            Prim(PrimitiveType.Cube, "Body", house, new Vector3(0f, 1f, 0f), new Vector3(4f, 3f, 3.6f), wall, collider: true);
            var roofObj = MeshObject("Roof", house, RoofMesh(), roof);
            roofObj.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            roofObj.transform.localScale = new Vector3(4.8f, 1.7f, 4.4f);
            Prim(PrimitiveType.Cube, "Chimney", house, new Vector3(1.2f, 3.6f, -0.6f), new Vector3(0.5f, 1f, 0.5f), wall);
            Prim(PrimitiveType.Cube, "Door", house, new Vector3(0f, 0.8f, 1.82f), new Vector3(1f, 1.6f, 0.08f), wood);
            Prim(PrimitiveType.Sphere, "DoorKnob", house, new Vector3(0.3f, 0.8f, 1.88f), Vector3.one * 0.12f, Mat("Gold", "#F2C94C", rim: 0.4f));
            Prim(PrimitiveType.Cube, "Window_L", house, new Vector3(-1.3f, 1.4f, 1.82f), new Vector3(0.8f, 0.8f, 0.08f), glass);
            Prim(PrimitiveType.Cube, "Window_R", house, new Vector3(1.3f, 1.4f, 1.82f), new Vector3(0.8f, 0.8f, 0.08f), glass);
            Prim(PrimitiveType.Cube, "Step", house, new Vector3(0f, -0.05f, 2.15f), new Vector3(1.6f, 0.2f, 0.7f), Mat("Stone", "#E8E2D6"));

            // 우체통 (흔들림이 우체통에만 걸리도록 묶음)
            var mailbox = new GameObject("Mailbox").transform;
            mailbox.SetParent(house, false);
            mailbox.localPosition = new Vector3(1.9f, 0f, 2.9f);
            Prim(PrimitiveType.Cylinder, "MailboxPost", mailbox, new Vector3(0f, 0.45f, 0f), new Vector3(0.12f, 0.45f, 0.12f), wood);
            Prim(PrimitiveType.Cube, "MailboxBox", mailbox, new Vector3(0f, 1f, 0f), new Vector3(0.45f, 0.35f, 0.6f), roof);
            AddInteractable(mailbox.gameObject, "우체통", InteractableKind.Mailbox, radius: 0.3f, markerHeight: 1.5f);
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

            var wood = Mat("Wood", "#B98A5E");
            var plank = Mat("Plank", "#D6AE7B");
            Prim(PrimitiveType.Cube, "Deck", bridge, new Vector3(0f, 0.05f, 0f), new Vector3(10f, 0.3f, 2.6f), plank, collider: true);
            for (int side = -1; side <= 1; side += 2)
            {
                Prim(PrimitiveType.Cube, "Rail", bridge, new Vector3(0f, 0.75f, side * 1.2f), new Vector3(9f, 0.12f, 0.12f), wood);
                for (int i = 0; i < 4; i++)
                {
                    float px = -4.5f + i * 3f;
                    var post = Prim(PrimitiveType.Cube, "Post", bridge, new Vector3(px, 0.45f, side * 1.2f), new Vector3(0.2f, 0.8f, 0.2f), wood, collider: true);
                    post.GetComponent<BoxCollider>().size = new Vector3(1f, 2f, 1f);
                }
            }
        }

        static void BuildLamps(Transform parent)
        {
            var lamps = new GameObject("Lamps").transform;
            lamps.SetParent(parent, false);
            var pole = Mat("LampPole", "#5E5A66");
            var glow = Mat("LampGlow", "#FFF1B8", rim: 0.3f);
            glow.SetColor("_EmissionColor", Hex("#FFD27A") * 0.6f);

            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f; // 정남쪽(카메라 쪽)은 비워 두기
                Vector2 p = PlazaCenter + (Vector2)(Quaternion.Euler(0f, 0f, angle) * Vector2.right) * (PlazaRadius + 0.3f);
                if (PathDistance(p.x, p.y) < 1.5f)
                    continue;
                var lamp = new GameObject("Lamp").transform;
                lamp.SetParent(lamps, false);
                lamp.localPosition = OnGround(p.x, p.y);
                Prim(PrimitiveType.Cylinder, "Pole", lamp, new Vector3(0f, 1f, 0f), new Vector3(0.14f, 1f, 0.14f), pole, collider: true);
                Prim(PrimitiveType.Sphere, "Light", lamp, new Vector3(0f, 2.2f, 0f), Vector3.one * 0.5f, glow);
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

            var trunk = Mat("Trunk", "#A57A52");
            int kind = random.Next(10);
            if (kind < 3)
            {
                // 침엽수
                var pine = Mat("Pine", "#5FAE7A");
                Prim(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0f, 0.6f, 0f), new Vector3(0.35f, 0.6f, 0.35f), trunk, collider: true);
                var cone = ConeMesh();
                for (int i = 0; i < 3; i++)
                {
                    var layer = MeshObject("Leaves", tree, cone, pine);
                    layer.transform.localPosition = new Vector3(0f, 1.1f + i * 0.85f, 0f);
                    layer.transform.localScale = new Vector3(2.4f - i * 0.6f, 1.6f - i * 0.2f, 2.4f - i * 0.6f);
                }
                AddInteractable(tree.gameObject, "나무", inVillage ? InteractableKind.Tree : InteractableKind.ForestTree, radius: 0.4f, markerHeight: 4f);
            }
            else
            {
                // 둥근 활엽수 (일부는 과일나무)
                var leaves = Mat("Leaves", "#6FC27A");
                Prim(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0f, 0.8f, 0f), new Vector3(0.4f, 0.8f, 0.4f), trunk, collider: true);
                Prim(PrimitiveType.Sphere, "Crown", tree, new Vector3(0f, 2.4f, 0f), new Vector3(2.4f, 2.1f, 2.4f), leaves);
                Prim(PrimitiveType.Sphere, "Crown2", tree, new Vector3(0.6f, 2.0f, 0.3f), new Vector3(1.5f, 1.3f, 1.5f), leaves);
                Prim(PrimitiveType.Sphere, "Crown3", tree, new Vector3(-0.5f, 2.1f, -0.4f), new Vector3(1.4f, 1.2f, 1.4f), leaves);

                AddInteractable(tree.gameObject, "나무", inVillage ? InteractableKind.Tree : InteractableKind.ForestTree, radius: 0.4f, markerHeight: 3.7f);

                if (kind >= 8)
                {
                    bool apple = kind == 8;
                    var fruit = Mat(apple ? "Apple" : "Peach", apple ? "#F0645A" : "#FFB38A", rim: 0.3f);
                    var visuals = new Transform[gatheringRules.fruitMaxOnTree];
                    for (int i = 0; i < visuals.Length; i++)
                    {
                        float a = i * 360f / visuals.Length * Mathf.Deg2Rad;
                        visuals[i] = Prim(PrimitiveType.Sphere, "Fruit", tree, new Vector3(Mathf.Cos(a) * 1.05f, 2.2f + (i % 2) * 0.5f, Mathf.Sin(a) * 1.05f), Vector3.one * 0.3f, fruit).transform;
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
            var rock = Mat("Rock", "#B9B4AE");
            int placed = 0;
            for (int attempt = 0; attempt < 800 && placed < 14; attempt++)
            {
                float x = Range(random, -48f, 48f);
                float z = Range(random, -34f, 48f);
                if (!CanPlaceNature(x, z, 1f))
                    continue;
                Occupied.Add(new Vector3(x, z, 1f));
                float s = Range(random, 0.7f, 1.3f);
                var go = Prim(PrimitiveType.Sphere, "Rock", rocks, OnGround(x, z, 0.1f), new Vector3(1.3f * s, 0.8f * s, 1.1f * s), rock, collider: true);
                go.transform.localRotation = Quaternion.Euler(0f, Range(random, 0f, 360f), Range(random, -8f, 8f));
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
            string[] palette = { "#FF9EC4", "#FFFFFF", "#FFE066", "#C9A7F2", "#FF8C7A" };
            var stem = Mat("Stem", "#5BAA5E");
            var center = Mat("FlowerCenter", "#FFD34D");
            var grid = ground.data.CreateGrid();

            int placed = 0;
            for (int attempt = 0; attempt < 1500 && placed < 55; attempt++)
            {
                float x = Range(random, -45f, 45f);
                float z = Range(random, -32f, 46f);
                if (!CanPlaceNature(x, z, 0.8f))
                    continue;
                var petal = Mat("Flower_" + placed % palette.Length, palette[placed % palette.Length], rim: 0.25f);
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
                    Prim(PrimitiveType.Cylinder, "Stem", flower, new Vector3(0f, 0.15f, 0f), new Vector3(0.05f, 0.15f, 0.05f), stem, castShadows: false);
                    Prim(PrimitiveType.Sphere, "Petals", flower, new Vector3(0f, 0.33f, 0f), new Vector3(0.32f, 0.14f, 0.32f), petal, castShadows: false);
                    Prim(PrimitiveType.Sphere, "Center", flower, new Vector3(0f, 0.4f, 0f), Vector3.one * 0.1f, center, castShadows: false);
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

            var interactor = player.AddComponent<PlayerInteractor>();
            interactor.settings = settings;
            BuildInteractionMarker(interactor);
            var inventory = player.AddComponent<PlayerInventory>();
            inventory.rules = inventoryRules;
            BuildPlayerTools(player, visual, inventory, interactor);
            return player;
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
                hold.SetActive(false);

            var playerTools = player.AddComponent<PlayerTools>();
            playerTools.settings = settings;
            playerTools.inventory = inventory;
            playerTools.interactor = interactor;
            playerTools.tools = tools;
            playerTools.holdVisuals = holds;
            playerTools.swingPivot = pivot;
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
            var halfExtents = new Vector3(size * 0.45f, 0.9f, size * 0.45f);

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
                    int n = Physics.OverlapBoxNonAlloc(new Vector3(x, h + 1.05f, z), halfExtents, hits, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                    for (int k = 0; k < n && !isBlocked; k++)
                        isBlocked = hits[k] != groundCollider;
                }
                blocked[j * count + i] = isBlocked;
            }

            data.Bake(origin, count, count, blocked);
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
            var trunk = Mat("Trunk", "#A57A52");
            var leaves = Mat("Leaves", "#6FC27A");
            float[] heights = { 0.5f, 0.9f, 1.4f };
            for (int i = 0; i < heights.Length; i++)
            {
                var stage = new GameObject("Stage" + i).transform;
                stage.SetParent(root.transform, false);
                float h = heights[i];
                Prim(PrimitiveType.Cylinder, "Stem", stage, new Vector3(0f, h * 0.5f, 0f), new Vector3(0.06f + i * 0.04f, h * 0.5f, 0.06f + i * 0.04f), trunk, castShadows: false);
                Prim(PrimitiveType.Sphere, "Leaf", stage, new Vector3(0f, h, 0f), Vector3.one * (0.3f + i * 0.3f), leaves, castShadows: i > 0);
                stage.gameObject.SetActive(i == 0);
            }
            root.AddComponent<GridObject>().kind = GroundObjectKind.Sapling;
            return SavePrefab(root, "Sapling");
        }

        // 묘목이 다 자라 생기는 나무: 열매 없는 활엽수, 흔들기 · 도끼로 베기 가능 (결정 2026-10-27)
        static GameObject BuildGrownTreePrefab()
        {
            var root = new GameObject("GrownTree");
            var trunk = Mat("Trunk", "#A57A52");
            var leaves = Mat("Leaves", "#6FC27A");
            Prim(PrimitiveType.Cylinder, "Trunk", root.transform, new Vector3(0f, 0.8f, 0f), new Vector3(0.4f, 0.8f, 0.4f), trunk, collider: true);
            Prim(PrimitiveType.Sphere, "Crown", root.transform, new Vector3(0f, 2.4f, 0f), new Vector3(2.4f, 2.1f, 2.4f), leaves);
            Prim(PrimitiveType.Sphere, "Crown2", root.transform, new Vector3(0.6f, 2.0f, 0.3f), new Vector3(1.5f, 1.3f, 1.5f), leaves);
            Prim(PrimitiveType.Sphere, "Crown3", root.transform, new Vector3(-0.5f, 2.1f, -0.4f), new Vector3(1.4f, 1.2f, 1.4f), leaves);
            AddInteractable(root, "나무", InteractableKind.Tree, radius: 0.4f, markerHeight: 3.7f);
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
            Prim(PrimitiveType.Cylinder, "Stem", root.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.05f, 0.15f, 0.05f), Mat("Stem", "#5BAA5E"), castShadows: false);
            Prim(PrimitiveType.Sphere, "Petals", root.transform, new Vector3(0f, 0.33f, 0f), new Vector3(0.32f, 0.14f, 0.32f), Mat("Flower_0", "#FF9EC4", rim: 0.25f), castShadows: false);
            Prim(PrimitiveType.Sphere, "Center", root.transform, new Vector3(0f, 0.4f, 0f), Vector3.one * 0.1f, Mat("FlowerCenter", "#FFD34D"), castShadows: false);
            AddInteractable(root, "꽃", InteractableKind.Flower, radius: 0.15f, markerHeight: 0.75f, wiggle: false);
            root.AddComponent<Pickup>().item = flowerItem;
            root.AddComponent<GridObject>().kind = GroundObjectKind.Flower;
            return SavePrefab(root, "PlantedFlower");
        }

        static void BuildGroundCursor(PlayerTools playerTools, VillageGround ground)
        {
            var cursor = new GameObject("GroundCursor").AddComponent<GroundCursor>();
            var outline = new GameObject("Outline");
            outline.transform.SetParent(cursor.transform, false);
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
            cursor.settings = settings;
            cursor.ground = ground;
            cursor.tools = playerTools;
            cursor.outline = outline;
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

        static ItemDefinition EnsureItem(string id, string displayName, GroundObjectKind plantsAs = GroundObjectKind.None)
        {
            string path = $"{ItemDir}/{id}.asset";
            bool isNew = !AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            var item = EnsureAsset<ItemDefinition>(path);
            if (isNew)
            {
                item.id = id;
                item.displayName = displayName;
                item.plantsAs = plantsAs;
                EditorUtility.SetDirty(item);
            }
            return item;
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
                EditorUtility.SetDirty(tool);
            }
            return tool;
        }

        // 베어진 나무의 밑동(삽으로만 없앰 → 1-8). 크기는 베어진 나무 배율을 따름
        static GameObject BuildStumpPrefab()
        {
            var root = new GameObject("Stump");
            Prim(PrimitiveType.Cylinder, "Trunk", root.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.42f, 0.15f, 0.42f), Mat("Trunk", "#A57A52"), collider: true);
            Prim(PrimitiveType.Cylinder, "Top", root.transform, new Vector3(0f, 0.305f, 0f), new Vector3(0.36f, 0.01f, 0.36f), Mat("StumpTop", "#E8C99A"), castShadows: false);
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
