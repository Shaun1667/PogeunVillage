using UnityEngine;
using UnityEngine.InputSystem;

namespace PogeunVillage
{
    /// <summary>
    /// 동물의숲 스타일 탑다운 추적 카메라. 마우스 휠로 줌, Q/E로 회전.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float heightOffset = 0.8f;

        [Range(10f, 80f)] public float pitch = 32f;
        public float yaw;
        public float distance = 16f;
        public Vector2 distanceRange = new(9f, 24f);
        public float zoomSpeed = 0.01f;
        public float rotateSpeed = 90f;
        public float smoothTime = 0.15f;

        Vector3 focusPoint;
        Vector3 focusVelocity;

        void Start()
        {
            if (target)
                focusPoint = target.position + Vector3.up * heightOffset;
            Apply();
        }

        void LateUpdate()
        {
            if (!target)
                return;

            var mouse = Mouse.current;
            if (mouse != null)
                distance = Mathf.Clamp(distance - mouse.scroll.ReadValue().y * zoomSpeed, distanceRange.x, distanceRange.y);

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.qKey.isPressed) yaw += rotateSpeed * Time.deltaTime;
                if (keyboard.eKey.isPressed) yaw -= rotateSpeed * Time.deltaTime;
            }

            focusPoint = Vector3.SmoothDamp(focusPoint, target.position + Vector3.up * heightOffset, ref focusVelocity, smoothTime);
            Apply();
        }

        void Apply()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(focusPoint - rotation * Vector3.forward * distance, rotation);
        }
    }
}
