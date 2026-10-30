using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 인트로 연출 수치(P-5). 값의 기준은 BALANCE.md 20번. 에셋: Assets/PogeunVillage/Data/IntroSettings.asset
    /// </summary>
    [CreateAssetMenu(fileName = "IntroSettings", menuName = "Pogeun Village/Intro Settings")]
    public class IntroSettings : ScriptableObject
    {
        [Header("시간(초)")]
        [Tooltip("마을을 화면 가득 보여 주는 시간")]
        public float villageTime = 1.5f;
        [Tooltip("모니터 화면이 꽉 찬 자리 → 방 전체로 빠지는 시간")]
        public float pullOutTime = 1f;
        [Tooltip("방을 보여 주는 시간")]
        public float roomTime = 1.5f;

        [Header("방")]
        [Tooltip("방 위치(맵 바깥)")]
        public Vector3 roomOrigin = new(3000f, 0f, 0f);
        [Tooltip("모니터 화면 높이(m). 폭은 화면 비율에 맞춤")]
        public float screenHeight = 0.36f;
        [Tooltip("방 카메라 끝 자리: 모니터에서 뒤(m)")]
        public float roomCameraBack = 1.8f;
        [Tooltip("방 카메라 끝 자리: 모니터보다 위(m)")]
        public float roomCameraUp = 0.25f;
        [Tooltip("방 카메라가 보는 곳: 모니터 가운데보다 아래(m) — 키보드 · 마우스까지 보이게")]
        public float roomLookDown = 0.15f;
        [Tooltip("방 머물 때 카메라 흔들림(m)")]
        public float roomDrift = 0.02f;

        [Header("마을 카메라(인트로)")]
        [Range(5f, 80f)] public float villagePitch = 20f;
        public float villageDistance = 11f;
    }
}
