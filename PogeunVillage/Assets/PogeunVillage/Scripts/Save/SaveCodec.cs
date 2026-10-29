using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 저장 파일 읽고 쓰기(결정 2026-10-29, 6-1a, 방법론 6번):
    /// - 쓰기: 임시 파일(.tmp)에 다 쓴 뒤 바꿔치기 — 쓰다 꺼져도 이전 저장은 그대로. 이전 파일은 백업으로 하나 남김.
    /// - 읽기: 저장 파일이 없거나 깨졌으면 백업. 형식 버전이 더 새것이면 읽지 않음.
    /// - 불러오기: 데이터에 없는 물건 id는 건너뛰고 이름을 알려 줌(Restore… 함수들).
    /// </summary>
    public static class SaveCodec
    {
        public static string ToJson(SaveData data) => JsonUtility.ToJson(data, true);

        /// <returns>읽은 저장, 비었거나 깨졌거나 더 새 형식이면 null</returns>
        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null || data.version <= 0 || data.version > SaveData.CurrentVersion)
                    return null;
                return data;  // 옛 버전을 옮기는 코드는 버전이 오를 때 여기에
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static void Write(string path, string backupPath, string json)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(path))
            {
                if (File.Exists(backupPath))
                    File.Delete(backupPath);
                File.Replace(tmp, path, backupPath);
            }
            else
                File.Move(tmp, path);
        }

        /// <returns>저장(없으면 백업)을 읽음. 둘 다 없거나 못 읽으면 null</returns>
        public static SaveData Read(string path, string backupPath)
        {
            foreach (string p in new[] { path, backupPath })
            {
                if (!File.Exists(p))
                    continue;
                var data = FromJson(File.ReadAllText(p));
                if (data != null)
                    return data;
            }
            return null;
        }

        /// <summary>인벤토리 칸들로 바꿈. 모르는 물건(데이터에서 없어짐)은 빈 칸으로 두고 skipped에 id를 더함</summary>
        public static InventorySlot[] RestoreSlots(IReadOnlyList<SlotData> saved, int slotCount, Func<string, bool> known, List<string> skipped)
        {
            var slots = new InventorySlot[slotCount];
            for (int i = 0; i < slotCount && i < saved.Count; i++)
            {
                var s = saved[i];
                if (string.IsNullOrEmpty(s.id) || s.count <= 0)
                    continue;
                if (!known(s.id))
                {
                    skipped.Add(s.id);
                    continue;
                }
                slots[i] = new InventorySlot(s.id, s.count, s.durability);
            }
            return slots;
        }

        public static List<SlotData> SaveSlots(IReadOnlyList<InventorySlot> slots)
        {
            var list = new List<SlotData>();
            foreach (var s in slots)
                list.Add(s.IsEmpty ? new SlotData() : new SlotData { id = s.itemId, count = s.count, durability = s.durability });
            return list;
        }
    }
}
