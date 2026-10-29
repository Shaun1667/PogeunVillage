using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace PogeunVillage.Tests
{
    // 6-1a 세이브: 형식 · 원자적 쓰기 · 백업 · 없어진 물건 (BALANCE 19번)
    public class SaveCodecTests
    {
        string folder;
        string FilePath => Path.Combine(folder, "save.json");
        string BackupPath => Path.Combine(folder, "save.bak.json");

        [SetUp]
        public void MakeFolder()
        {
            folder = Path.Combine(Application.temporaryCachePath, "PogeunVillageSaveCodecTests");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        [TearDown]
        public void DeleteFolder()
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        static SaveData Sample(int day) => new()
        {
            day = day,
            moa = 2800,
            slots = new List<SlotData> { new() { id = "Axe", count = 1, durability = 49 }, new(), new() { id = "Apple", count = 3 } },
            fishCaught = new List<CountData> { new() { id = "CrucianCarp", count = 2 } },
            firstGoalReached = true,
            bestStars = 3,
        };

        [Test]
        public void Json_RoundTrip()
        {
            var back = SaveCodec.FromJson(SaveCodec.ToJson(Sample(4)));
            Assert.AreEqual(SaveData.CurrentVersion, back.version);
            Assert.AreEqual((4, 2800, true, 3), (back.day, back.moa, back.firstGoalReached, back.bestStars));
            Assert.AreEqual("Axe", back.slots[0].id);
            Assert.AreEqual(49, back.slots[0].durability);
            Assert.AreEqual("", back.slots[1].id);
            Assert.AreEqual(2, back.fishCaught[0].count);
        }

        [Test]
        public void BrokenOrNewerVersion_IsNull()
        {
            Assert.IsNull(SaveCodec.FromJson(""));
            Assert.IsNull(SaveCodec.FromJson("{ 깨진"));
            var newer = Sample(1);
            newer.version = SaveData.CurrentVersion + 1;
            Assert.IsNull(SaveCodec.FromJson(SaveCodec.ToJson(newer)), "더 새 형식은 읽지 않음");
        }

        [Test]
        public void Write_KeepsBackup_ReadFallsBack()
        {
            SaveCodec.Write(FilePath, BackupPath, SaveCodec.ToJson(Sample(2)));
            Assert.IsFalse(File.Exists(BackupPath), "처음엔 백업 없음");
            SaveCodec.Write(FilePath, BackupPath, SaveCodec.ToJson(Sample(3)));
            Assert.AreEqual(3, SaveCodec.Read(FilePath, BackupPath).day);
            Assert.AreEqual(2, SaveCodec.FromJson(File.ReadAllText(BackupPath)).day, "덮어쓰기 전 파일이 백업");
            Assert.IsFalse(File.Exists(FilePath + ".tmp"), "임시 파일은 남지 않음");

            File.WriteAllText(FilePath, "깨진 파일");
            Assert.AreEqual(2, SaveCodec.Read(FilePath, BackupPath).day, "저장이 깨졌으면 백업");
        }

        [Test]
        public void CrashMidWrite_PreviousSaveIntact()  // 19-4
        {
            SaveCodec.Write(FilePath, BackupPath, SaveCodec.ToJson(Sample(5)));
            File.WriteAllText(FilePath + ".tmp", "{ 쓰다 만");   // 바꿔치기 전에 꺼짐
            Assert.AreEqual(5, SaveCodec.Read(FilePath, BackupPath).day);
            Assert.IsNull(SaveCodec.Read(Path.Combine(folder, "none.json"), Path.Combine(folder, "none.bak.json")), "저장 없음");
        }

        [Test]
        public void MissingItem_SkippedSlotEmpty()  // 19-3
        {
            var skipped = new List<string>();
            var saved = new List<SlotData> { new() { id = "Apple", count = 3 }, new() { id = "OldThing", count = 1 }, new() };
            var slots = SaveCodec.RestoreSlots(saved, 4, id => id == "Apple", skipped);
            Assert.AreEqual(4, slots.Length);
            Assert.AreEqual(("Apple", 3), (slots[0].itemId, slots[0].count));
            Assert.IsTrue(slots[1].IsEmpty, "없어진 물건 칸은 비움");
            CollectionAssert.AreEqual(new[] { "OldThing" }, skipped);
            var back = SaveCodec.SaveSlots(slots);
            Assert.AreEqual(4, back.Count);
            Assert.AreEqual("", back[1].id);
        }
    }
}
