using System.Collections.Generic;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 도감 공통(결정 2026-10-29, 3-6b · 3-7b): 잡을 때마다 기록(CatchLog), 새 종류를 처음 잡으면 알림.
    /// 광장 게시판에서 고르면 목록 창(잡은 건 한 줄 정보 · 잡은 수, 못 잡은 건 '???', 아래 '잡은 종류 n / 전체').
    /// 잡은 종류 수는 다음 아침 마을 평가에 들어감(VillageRater). 물고기 도감 · 곤충 도감이 이어받음.
    /// </summary>
    public abstract class Encyclopedia : MonoBehaviour, IListSource
    {
        public ListWindow window;

        public CatchLog Log { get; } = new();

        public abstract string Title { get; }
        /// <summary>알림에 쓰는 이름("물고기" · "곤충")</summary>
        protected abstract string KindName { get; }
        protected abstract IReadOnlyList<ItemDefinition> Entries { get; }
        protected abstract string EntryLine(ItemDefinition item, int caught);

        public string Hint => "Tab 닫기";
        public int Count => Entries.Count;
        public string Footer => $"잡은 종류 {Log.Species} / {Entries.Count}";

        public string Line(int index)
        {
            var item = Entries[index];
            return Log.Has(item.id) ? EntryLine(item, Log.CaughtCount(item.id)) : "???";
        }

        public bool Usable(int index) => Log.Has(Entries[index].id);

        public void Choose(int index) { }  // 보기만

        protected virtual void Start()
        {
            if (!window || Entries == null || Entries.Count == 0)
            {
                Debug.LogError($"{name}: 도감 연결이 비어 있습니다.", this);
                enabled = false;
            }
        }

        public void Open()
        {
            if (enabled && !window.IsOpen)
                window.Open(this);
        }

        /// <summary>잡음. 처음 잡은 종류면 알림</summary>
        public void Record(ItemDefinition item)
        {
            if (Log.Record(item.id))
                GameNotice.Post($"[도감] 새 {KindName}: {item.displayName}! ({Log.Species} / {Entries.Count})", this);
        }
    }
}
