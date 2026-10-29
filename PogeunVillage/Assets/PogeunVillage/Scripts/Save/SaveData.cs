using System;
using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>
    /// 저장 내용(결정 2026-10-29, GDD 4-13 · 6-1a): 아침 기준. JsonUtility로 읽고 씀(사람이 열어 볼 수 있음).
    /// 형식이 바뀌면 Version을 올리고 SaveCodec에서 옮김. 6-1b에서 마을 상태, 6-1c에서 난수 상태를 더함.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int day = 1;
        public int moa;
        public List<SlotData> slots = new();
        public List<CountData> fishCaught = new();
        public List<CountData> insectCaught = new();
        public bool firstGoalReached;
        public int bestStars;
        public VillageSave village = new();      // 6-1b
        public GroundSave ground = new();        // 6-1b
        public List<PlacedData> furniture = new();  // 6-1b: 가구 · 수조 · 채집통
        public TodaySave today = new();          // 6-1c: 그날 아침 정해진 것
        public List<RandomData> randoms = new(); // 6-1c: 난수 상태
    }

    /// <summary>그날 아침 난수로 정해진 것(6-1c): 오늘의 테마 가구 · 물고기 그림자 · 곤충</summary>
    [Serializable]
    public class TodaySave
    {
        public string theme = "";
        public List<CellData> shadows = new();   // look = 물고기 id
        public List<DropData> insects = new();   // id · 자리
    }

    /// <summary>난수 하나의 상태(6-1c)</summary>
    [Serializable]
    public class RandomData
    {
        public string key;
        public string state;

        public static void Add(List<RandomData> list, string key, SeededRandom random) => list.Add(new RandomData { key = key, state = random.State });

        /// <summary>저장된 상태가 있으면 되돌림(없으면 그대로)</summary>
        public static void Restore(IReadOnlyList<RandomData> list, string key, SeededRandom random)
        {
            foreach (var r in list)
                if (r.key == key && !string.IsNullOrEmpty(r.state)) { random.State = r.state; return; }
        }
    }

    /// <summary>마을 주민 · 캠핑 · 부탁 · 테마 보상(6-1b)</summary>
    [Serializable]
    public class VillageSave
    {
        public List<string> plots = new();        // 집터마다 주민 id(빈 집터는 "")
        public List<CountData> friendship = new();
        public List<string> preparing = new();    // 이사 준비 중
        public string camperId = "";
        public int campValue;
        public bool campConfirmed;
        public bool hasRequest;
        public string requestVillager = "", requestItem = "";
        public int requestCount;
        public bool requestDone;
        public List<CountData> themeGiven = new();
    }

    /// <summary>땅: 꽃 · 묘목 · 구멍 · 베어진 나무 · 밑동 · 키운 나무 · 떨어진 물건(6-1b)</summary>
    [Serializable]
    public class GroundSave
    {
        public List<CellData> flowers = new();     // value = 1이면 시듦, look = 꽃잎 재질 이름
        public List<CellData> saplings = new();    // value = 자란 날
        public List<CellData> holes = new();
        public List<PointData> felledTrees = new();  // 처음 맵에 있던 나무 중 베어진 것(자리)
        public List<PointData> stumps = new();       // 지금 있는 밑동(자리 · 크기)
        public List<CellData> grownTrees = new();    // 묘목에서 자란 나무 칸
        public List<DropData> drops = new();
    }

    [Serializable]
    public class CellData
    {
        public int x, y, value;
        public string look = "";
    }

    [Serializable]
    public class PointData
    {
        public float x, y, z, scale = 1f, yaw;
    }

    [Serializable]
    public class DropData
    {
        public string id;
        public float x, y, z;
        public float r = 1f, g = 1f, b = 1f;
    }

    /// <summary>놓인 가구 하나(수조 · 채집통 포함)</summary>
    [Serializable]
    public class PlacedData
    {
        public string id;
        public int x, y, rotation;
    }

    /// <summary>인벤토리 한 칸(빈 칸은 id가 비어 있음)</summary>
    [Serializable]
    public class SlotData
    {
        public string id = "";
        public int count;
        public int durability;
    }

    /// <summary>종류마다 수(도감)</summary>
    [Serializable]
    public class CountData
    {
        public string id;
        public int count;
    }
}
