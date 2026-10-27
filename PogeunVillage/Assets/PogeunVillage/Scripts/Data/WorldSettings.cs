using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 월드 · 조작 기본 수치(휨 반경, 물 높이, 플레이어 이동, 카메라). 값의 기준은 BALANCE.md 9번.
    /// 에셋: Assets/PogeunVillage/Data/WorldSettings.asset
    /// 아래 초기값은 맵 프로토타입(P-1)에서 쓰던 값이며, 에셋이 생긴 뒤에는 에셋 값이 기준입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldSettings", menuName = "Pogeun Village/World Settings")]
    public class WorldSettings : ScriptableObject
    {
        [Header("둥근 행성 연출")]
        [Tooltip("행성 반지름(m). 작을수록 더 둥글게 보입니다.")]
        [Range(20f, 400f)] public float curveRadius = 110f;

        [Tooltip("휘어짐 때문에 오브젝트가 잘려 사라지지 않도록 컬링 시야각을 넓히는 배율")]
        [Range(1f, 3f)] public float cullingFovScale = 1.8f;

        [Header("물")]
        [Tooltip("이 높이보다 낮은 지면(강/바다)으로는 이동하지 않습니다.")]
        public float waterLevel = -0.3f;

        [Header("플레이어")]
        public float walkSpeed = 4f;
        public float runSpeed = 7f;
        [Tooltip("회전 속도(도/초)")]
        public float turnSpeed = 720f;
        public float gravity = 20f;

        [Header("카메라")]
        public float cameraHeightOffset = 0.8f;
        [Range(10f, 80f)] public float cameraPitch = 32f;
        public float cameraDistance = 16f;
        public Vector2 cameraDistanceRange = new(9f, 24f);
        public float cameraZoomSpeed = 0.01f;
        [Tooltip("Q/E 회전 속도(도/초)")]
        public float cameraRotateSpeed = 90f;
        public float cameraSmoothTime = 0.15f;

        [Header("가림 반투명 (카메라와 플레이어 사이 물체)")]
        [Tooltip("플레이어 발에서 이 높이의 점과 카메라를 잇는 선을 기준으로 판정합니다.")]
        public float occlusionTargetHeight = 0.7f;
        [Tooltip("선에서 이 거리(m) 안에 있는 물체가 비쳐 보입니다.")]
        public float occlusionRadius = 1.2f;
        [Tooltip("플레이어에서 이 거리(m) 안쪽 물체(발밑 지면 등)는 비치지 않습니다.")]
        public float occlusionMargin = 2f;
        [Tooltip("반투명 경계를 부드럽게 넘기는 폭(m)")]
        public float occlusionSoftness = 0.4f;
        [Tooltip("가장 많이 비칠 때 빠지는 픽셀 비율(0~1)")]
        [Range(0f, 1f)] public float occlusionStrength = 0.75f;

        [Header("상호작용")]
        [Tooltip("플레이어에서 대상 가장자리까지 이 거리(m) 안이면 고를 수 있습니다.")]
        public float interactRange = 1f;
        [Tooltip("바라보는 방향에서 좌우 이 각도(도) 안의 대상만 고릅니다.")]
        [Range(10f, 180f)] public float interactHalfAngle = 60f;
        [Tooltip("대상 위 마커 크기(m)")]
        public float markerSize = 0.35f;
        [Tooltip("마커가 위아래로 통통 튀는 높이(m)와 속도(회/초)")]
        public float markerBobHeight = 0.12f;
        public float markerBobSpeed = 1.5f;
        [Tooltip("임시 반응: 상호작용하면 대상이 이 각도(도)로 이 시간(초) 동안 흔들립니다. 1-3 이후 실제 동작으로 교체")]
        public float interactWiggleAngle = 6f;
        public float interactWiggleTime = 0.3f;

        [Header("채집 연출")]
        [Tooltip("흔든 나무에서 열매가 떨어지는 거리(나무 중심에서, m)")]
        public float fruitDropRadius = 1.1f;
        [Tooltip("열매가 땅까지 떨어지는 시간(초)")]
        public float fruitFallTime = 0.35f;

        [Header("도구")]
        [Tooltip("한 번 휘두르는 시간(초). 휘두르는 동안에는 다시 휘두를 수 없습니다.")]
        public float toolSwingTime = 0.35f;
        [Tooltip("휘두를 때 도구가 앞으로 꺾이는 각도(도)")]
        public float toolSwingAngle = 70f;
    }
}
