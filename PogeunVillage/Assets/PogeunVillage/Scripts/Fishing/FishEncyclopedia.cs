using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>
    /// 물고기 도감(결정 2026-10-29, 3-6b): 낚을 때마다 기록(FishingRodUser). 줄 = 이름 · 크기 · 나오는 때 · 잡은 수. 공통 동작은 Encyclopedia.
    /// </summary>
    public class FishEncyclopedia : Encyclopedia
    {
        public FishingRules rules;

        public override string Title => "물고기 도감";
        protected override string KindName => "물고기";
        protected override IReadOnlyList<ItemDefinition> Entries => rules ? rules.fish : null;

        protected override string EntryLine(ItemDefinition item, int caught)
        {
            var f = (FishDefinition)item;
            return HudText.FishEntry(f.displayName, f.size, f.periods, caught);
        }
    }
}
