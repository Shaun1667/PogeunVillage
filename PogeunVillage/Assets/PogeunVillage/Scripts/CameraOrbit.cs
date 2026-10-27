using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 탑다운 추적 카메라의 계산(엔진 상태 없음). CameraFollow가 사용합니다.
    /// </summary>
    public static class CameraOrbit
    {
        /// <summary>휠 입력만큼 거리를 바꾸고 줌 범위(x~y) 안으로 자릅니다.</summary>
        public static float Zoom(float distance, float scrollDelta, float zoomSpeed, Vector2 distanceRange)
        {
            return Mathf.Clamp(distance - scrollDelta * zoomSpeed, distanceRange.x, distanceRange.y);
        }

        public static Quaternion Rotation(float pitch, float yaw) => Quaternion.Euler(pitch, yaw, 0f);

        /// <summary>바라보는 점(focus)에서 distance만큼 뒤로 물러난 카메라 위치.</summary>
        public static Vector3 Position(Vector3 focus, float pitch, float yaw, float distance)
        {
            return focus - Rotation(pitch, yaw) * Vector3.forward * distance;
        }
    }
}
