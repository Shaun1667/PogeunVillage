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
