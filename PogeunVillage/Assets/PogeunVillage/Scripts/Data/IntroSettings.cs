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
        [Tooltip("화면 속 커서가 '게임 시작'으로 가서 누르는 시간(P-5b)")]
        public float clickTime = 1f;
        [Tooltip("모니터로 돌진하는 시간(P-5b)")]
        public float diveTime = 0.8f;
        [Tooltip("마을로 컷한 뒤 섬광이 걷히는 시간(P-5b · P-5c 낙하)")]
        public float fallTime = 0.5f;

        [Header("클릭 · 돌진 (P-5b)")]
        [Tooltip("클릭 장면에서 누르는 순간(0~1) — 그 전까지 커서가 움직임")]
        [Range(0.1f, 1f)] public float clickPressAt = 0.75f;
        [Tooltip("커서 출발 자리(버튼 기준, 화면 UI 1920×1080)")]
        public Vector2 cursorStartOffset = new(380f, -240f);
        [Tooltip("커서 도착 자리(버튼 기준)")]
        public Vector2 cursorEndOffset = new(40f, -20f);
        [Tooltip("눌린 버튼 크기 · 밝기")]
        public float buttonPressScale = 0.92f;
        [Range(0f, 1f)] public float buttonPressTint = 0.8f;
        [Tooltip("돌진 끝 자리 = 화면이 꽉 찬 자리의 몇 배까지(1보다 크면 화면을 뚫고 들어감)")]
        public float diveOvershoot = 1.15f;
        [Tooltip("돌진 중 섬광이 시작하는 순간(0~1)")]
        [Range(0f, 1f)] public float flashStart = 0.55f;

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
