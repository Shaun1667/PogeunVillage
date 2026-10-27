using System;

namespace PogeunVillage
{
    /// <summary>도끼로 한 번 친 결과</summary>
    public readonly struct ChopResult
    {
        public readonly int wood;
        public readonly bool felled;

        public ChopResult(int wood, bool felled)
        {
            this.wood = wood;
            this.felled = felled;
        }
    }

    /// <summary>
    /// 도끼로 베는 나무 규칙(엔진 상태 없음). 수치는 BALANCE.md 5번 · GatheringRules.
    /// - 칠 때마다 목재 woodPerHit개
    /// - hitsToFell번째에 베어짐(그 뒤로는 칠 수 없음 → 밑동)
    /// - 치다 만 횟수는 아침에 0으로(2-1 하루 루프에서 ResetHits 호출)
    /// </summary>
    public class ChoppableTree
    {
        readonly int hitsToFell;
        readonly int woodPerHit;

        public int Hits { get; private set; }
        public bool Felled { get; private set; }

        public ChoppableTree(int hitsToFell, int woodPerHit)
        {
            if (hitsToFell <= 0) throw new ArgumentOutOfRangeException(nameof(hitsToFell));
            if (woodPerHit < 0) throw new ArgumentOutOfRangeException(nameof(woodPerHit));
            this.hitsToFell = hitsToFell;
            this.woodPerHit = woodPerHit;
        }

        public ChopResult Chop()
        {
            if (Felled)
                return new ChopResult(0, false);
            Hits++;
            Felled = Hits >= hitsToFell;
            return new ChopResult(woodPerHit, Felled);
        }

        public void ResetHits()
        {
            if (!Felled)
                Hits = 0;
        }
    }
}
