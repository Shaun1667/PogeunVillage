using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 게임 시간 수치. 값의 기준은 BALANCE.md 12번. 에셋: Assets/PogeunVillage/Data/DayRules.asset
    /// </summary>
    [CreateAssetMenu(fileName = "DayRules", menuName = "Pogeun Village/Day Rules")]
    public class DayRules : ScriptableObject
    {
        [Tooltip("하루가 시작되는 시각(잠에서 깨는 시각)")]
        [Range(0, 23)] public int dayStartHour = 6;
        [Tooltip("게임 1분이 지나는 데 걸리는 실제 초. 1이면 게임 1시간 = 실제 1분, 6시~2시 = 실제 20분")]
        [Min(0.01f)] public float realSecondsPerGameMinute = 1f;
        [Tooltip("시간대 시작 시각: 아침 · 낮 · 저녁 · 밤 순서")]
        public int[] periodStartHours = { 6, 10, 17, 20 };
    }
}
