using UnityEngine;
using UnityEngine.InputSystem;

namespace PogeunVillage
{
    /// <summary>
    /// 동물의숲 스타일 탑다운 추적 카메라. 마우스 휠로 줌, Q/E로 회전. 수치는 WorldSettings.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public WorldSettings settings;
        public Transform target;

        // 플레이 중에 바뀌는 상태(에셋에는 쓰지 않음)
        float yaw;
        float distance;
        Vector3 focusPoint;
        Vector3 focusVelocity;

        void Awake()
        {
            if (!settings)
            {
                Debug.LogError($"{name}: WorldSettings가 연결되지 않았습니다.", this);
                enabled = false;
                return;
            }
            distance = settings.cameraDistance;
        }

        void Start()
        {
            if (target)
                focusPoint = FocusTarget();
            Apply();
        }

        void LateUpdate()
        {
            if (!target)
                return;

            var mouse = Mouse.current;
            if (mouse != null)
                distance = CameraOrbit.Zoom(distance, mouse.scroll.ReadValue().y, settings.cameraZoomSpeed, settings.cameraDistanceRange);

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.qKey.isPressed) yaw += settings.cameraRotateSpeed * Time.deltaTime;
                if (keyboard.eKey.isPressed) yaw -= settings.cameraRotateSpeed * Time.deltaTime;
            }

            focusPoint = Vector3.SmoothDamp(focusPoint, FocusTarget(), ref focusVelocity, settings.cameraSmoothTime);
            Apply();
        }

        Vector3 FocusTarget() => target.position + Vector3.up * settings.cameraHeightOffset;

        void Apply()
        {
            transform.SetPositionAndRotation(
                CameraOrbit.Position(focusPoint, settings.cameraPitch, yaw, distance),
                CameraOrbit.Rotation(settings.cameraPitch, yaw));
        }
    }
}
