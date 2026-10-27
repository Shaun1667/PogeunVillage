using UnityEngine;

namespace PogeunVillage
{
    /// <summary>플레이어 지갑(Wallet) 어댑터. 시작 모아는 ShopData. 바뀔 때만 콘솔에 알림(화면 표시는 2-3 HUD).</summary>
    public class PlayerWallet : MonoBehaviour
    {
        public ShopData shop;

        public Wallet Wallet { get; private set; }

        void Awake()
        {
            if (!shop)
            {
                Debug.LogError($"{name}: ShopData가 연결되지 않았습니다.", this);
                enabled = false;
                return;
            }
            Wallet = new Wallet(shop.startingMoa);
            Wallet.Changed += moa => Debug.Log($"[모아] {moa}", this);
        }
    }
}
