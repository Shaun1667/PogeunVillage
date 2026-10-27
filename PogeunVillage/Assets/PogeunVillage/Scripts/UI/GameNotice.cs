using System;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 플레이어에게 보여 줄 짧은 안내(문을 닫았어요 · 가득 찼어요 · 모아가 모자라요 · 샀어요 · 부서졌어요 …).
    /// 콘솔에도 남기고, HUD가 Posted를 구독해 화면 아래에 잠깐 띄웁니다(결정 2026-10-27, 2-3).
    /// </summary>
    public static class GameNotice
    {
        public static event Action<string> Posted;

        public static void Post(string message, UnityEngine.Object context = null)
        {
            Debug.Log(message, context);
            Posted?.Invoke(message);
        }
    }
}
