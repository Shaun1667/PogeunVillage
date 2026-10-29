using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>
    /// 곤충 도감(결정 2026-10-29, 3-7b): 잡을 때마다 기록(Insects). 줄 = 이름 · 자리 · 나오는 때 · 잡은 수. 공통 동작은 Encyclopedia.
    /// </summary>
    public class InsectEncyclopedia : Encyclopedia
    {
        public InsectRules rules;

        public override string Title => "곤충 도감";
        protected override string KindName => "곤충";
        protected override IReadOnlyList<ItemDefinition> Entries => rules ? rules.insects : null;

        protected override string EntryLine(ItemDefinition item, int caught)
        {
            var i = (InsectDefinition)item;
            return HudText.InsectEntry(i.displayName, i.site, i.periods, caught);
        }
    }
}
