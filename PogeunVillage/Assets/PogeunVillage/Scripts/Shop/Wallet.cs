using System;

namespace PogeunVillage
{
    /// <summary>
    /// 지갑(화폐 '모아', 엔진 상태 없음). 결정 (2026-10-19): 처음 마을에 이주하면 3000 모아. 바뀔 때만 Changed(바뀐 뒤 금액).
    /// </summary>
    public class Wallet
    {
        public int Moa { get; private set; }
        public event Action<int> Changed;

        public Wallet(int startingMoa)
        {
            if (startingMoa < 0) throw new ArgumentOutOfRangeException(nameof(startingMoa));
            Moa = startingMoa;
        }

        /// <returns>모아가 충분해서 냈으면 true, 모자라면 아무것도 안 하고 false</returns>
        public bool TrySpend(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount > Moa) return false;
            if (amount == 0) return true;
            Moa -= amount;
            Changed?.Invoke(Moa);
            return true;
        }

        /// <summary>불러오기(6-1a): 그 금액으로</summary>
        public void Restore(int moa)
        {
            if (moa < 0) throw new ArgumentOutOfRangeException(nameof(moa));
            if (moa == Moa) return;
            Moa = moa;
            Changed?.Invoke(Moa);
        }

        public void Earn(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0) return;
            Moa += amount;
            Changed?.Invoke(Moa);
        }
    }
}
