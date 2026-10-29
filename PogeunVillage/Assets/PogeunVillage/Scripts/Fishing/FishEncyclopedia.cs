using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 물고기 도감(결정 2026-10-29, 3-6b): 낚을 때마다 기록(FishLog), 새 종류를 처음 낚으면 알림.
    /// 광장 게시판 F → '물고기 도감' = 목록 창(잡은 물고기는 이름 · 크기 · 나오는 때 · 잡은 수, 못 잡은 건 '???').
    /// 잡은 종류 수는 다음 아침 마을 평가에 들어감(VillageRater).
    /// </summary>
    public class FishEncyclopedia : MonoBehaviour, IListSource
    {
        public FishingRules rules;
        public ListWindow window;

        public FishLog Log { get; } = new();

        public string Title => "물고기 도감";
        public string Hint => "Tab 닫기";
        public int Count => rules.fish.Length;
        public string Footer => $"잡은 종류 {Log.Species} / {rules.fish.Length}";

        public string Line(int index)
        {
            var f = rules.fish[index];
            return Log.Has(f.id) ? HudText.FishEntry(f.displayName, f.size, f.periods, Log.CaughtCount(f.id)) : "???";
        }

        public bool Usable(int index) => Log.Has(rules.fish[index].id);

        public void Choose(int index) { }  // 보기만

        void Start()
        {
            if (!rules || !window || rules.fish.Length == 0)
            {
                Debug.LogError($"{name}: 물고기 도감 연결이 비어 있습니다.", this);
                enabled = false;
            }
        }

        public void Open()
        {
            if (enabled && !window.IsOpen)
                window.Open(this);
        }

        /// <summary>낚음(FishingRodUser). 처음 낚은 종류면 알림</summary>
        public void Record(FishDefinition fish)
        {
            if (Log.Record(fish.id))
                GameNotice.Post($"[도감] 새 물고기: {fish.displayName}! ({Log.Species} / {rules.fish.Length})", this);
        }
    }
}
