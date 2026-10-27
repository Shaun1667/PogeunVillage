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
    }
}
