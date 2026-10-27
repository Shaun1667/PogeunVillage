using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PogeunVillage
{
    /// <summary>
    /// 아침 처리(자연). GDD 4-5 순서: 열매 채움 → 묘목 성장 → 꽃 사라짐 → 꽃 번식 → 꽃 시듦 → 도끼 친 횟수 초기화.
    /// 2-1 하루 루프가 잠자기 뒤에 RunMorning을 부르고, 그 전까지는 개발용 F8(에디터 · 개발 빌드만)로 부름(결정 2026-10-27).
    /// 주민 떠남 · 입주 · 캠핑 교체(GDD 4-5 ①~④)는 2-4 이후 이 앞에 붙임.
    /// </summary>
    public class MorningRunner : MonoBehaviour
    {
        public GardenKeeper garden;

        public int Day { get; private set; } = 1;
        public event Action<int> MorningPassed;

        public void RunMorning()
        {
            Day++;
            foreach (var tree in FindObjectsByType<FruitTreeBehaviour>())
                tree.RefillForMorning();
            if (garden)
                garden.RunMorning();
            foreach (var tree in FindObjectsByType<ChoppableTreeBehaviour>())
                tree.ResetHitsForMorning();
            Debug.Log($"[아침] {Day}일차", this);
            MorningPassed?.Invoke(Day);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
                RunMorning();
        }
#endif
    }
}
