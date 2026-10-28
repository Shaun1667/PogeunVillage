namespace PogeunVillage
{
    /// <summary>
    /// 캠핑 주민의 하루(엔진 상태 없음). 결정 (2026-10-19): 그날 첫 대화 +1, 선물 1개당 +2, 하루 상한 5, 기준(5)에 닿으면 이주 확정,
    /// 확정 뒤에는 변하지 않음. 그날 기준에 못 미치면 리셋 — 캠핑 주민은 매일 바뀌므로 방문마다 새로 만들어 0부터 시작.
    /// BALANCE 1번 검산 1-1 ~ 1-6, 1-12.
    /// </summary>
    public class CampStay
    {
        readonly Friendship friendship;

        public CampStay(string visitorId, int talkGain, int giftGain, int dailyCap, int moveInThreshold)
        {
            VisitorId = visitorId;
            Threshold = moveInThreshold;
            friendship = new Friendship(talkGain, giftGain, dailyCap, 0, int.MaxValue);
        }

        public string VisitorId { get; }
        public int Threshold { get; }
        public int Value => friendship.Get(VisitorId);
        public bool Confirmed { get; private set; }

        /// <summary>말 걸기. 실제로 오른 양(확정 뒤 · 두 번째 대화는 0)</summary>
        public int Talk() => Confirmed ? 0 : Check(friendship.Talk(VisitorId));

        /// <summary>선물 1개. 실제로 오른 양(확정 뒤 · 상한 뒤는 0)</summary>
        public int Gift() => Confirmed ? 0 : Check(friendship.Gift(VisitorId));

        int Check(int gained)
        {
            if (Value >= Threshold)
                Confirmed = true;
            return gained;
        }
    }
}
