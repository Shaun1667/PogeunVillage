using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 상점 · 모아 수치. 값의 기준은 BALANCE.md 7번. 에셋: Assets/PogeunVillage/Data/ShopData.asset
    /// 물건별 가격은 ItemDefinition.buyPrice · sellPrice.
    /// </summary>
    [CreateAssetMenu(fileName = "ShopData", menuName = "Pogeun Village/Shop Data")]
    public class ShopData : ScriptableObject
    {
        [Tooltip("처음 마을에 이주할 때 받는 모아")]
        [Min(0)] public int startingMoa = 3000;
        [Tooltip("문 여는 시각")]
        [Range(0, 23)] public int openHour = 8;
        [Tooltip("문 닫는 시각(이 시각부터 닫힘)")]
        [Range(0, 24)] public int closeHour = 22;
        [Tooltip("판매함이 값을 알아보는 물건 목록(팔 수 있는 물건은 여기 있어야 함)")]
        public ItemDefinition[] catalog = new ItemDefinition[0];
        [Tooltip("오늘의 테마 가구(3-4d)를 고르는 난수 시드")]
        public int themeSeed = 2028;
    }
}
