using System;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 아침 처리(자연). GDD 4-5 순서: 열매 채움 → 묘목 성장 → 꽃 사라짐 → 꽃 번식 → 꽃 시듦 → 도끼 친 횟수 초기화.
    /// 잠에서 깰 때 DayCycle이 부름(침대 · 개발용 F8). 주민 떠남 · 입주 · 캠핑 교체(GDD 4-5 ①~④)는 2-5 · 2-6에서 이 앞에 붙임.
    /// </summary>
    public class MorningRunner : MonoBehaviour
    {
        public GardenKeeper garden;

        public event Action MorningPassed;

        public void RunMorning()
        {
            foreach (var tree in FindObjectsByType<FruitTreeBehaviour>())
                tree.RefillForMorning();
            if (garden)
                garden.RunMorning();
            foreach (var tree in FindObjectsByType<ChoppableTreeBehaviour>())
                tree.ResetHitsForMorning();
            MorningPassed?.Invoke();
        }
    }
}
