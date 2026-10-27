using NUnit.Framework;
using UnityEngine;

namespace PogeunVillage.Tests
{
    public class CameraOrbitTests
    {
        static readonly Vector2 Range = new(9f, 24f);

        [Test]
        public void Zoom_ScrollUp_MovesCloser()
        {
            Assert.AreEqual(15f, CameraOrbit.Zoom(16f, 100f, 0.01f, Range), 1e-4f);
        }

        [Test]
        public void Zoom_ClampsToRange()
        {
            Assert.AreEqual(9f, CameraOrbit.Zoom(10f, 10000f, 0.01f, Range), 1e-4f);
            Assert.AreEqual(24f, CameraOrbit.Zoom(20f, -10000f, 0.01f, Range), 1e-4f);
        }

        [Test]
        public void Position_IsDistanceAwayAndLooksAtFocus()
        {
            var focus = new Vector3(3f, 1f, -2f);
            Vector3 position = CameraOrbit.Position(focus, 32f, 45f, 16f);
            Quaternion rotation = CameraOrbit.Rotation(32f, 45f);

            Assert.AreEqual(16f, Vector3.Distance(position, focus), 1e-3f);
            Assert.Greater(position.y, focus.y, "카메라가 위에서 내려다봐야 합니다.");
            Assert.Less(Vector3.Angle(rotation * Vector3.forward, focus - position), 0.01f);
        }

        [Test]
        public void Position_YawRotatesAroundFocus()
        {
            Vector3 front = CameraOrbit.Position(Vector3.zero, 32f, 0f, 16f);
            Vector3 side = CameraOrbit.Position(Vector3.zero, 32f, 90f, 16f);

            Assert.AreEqual(front.y, side.y, 1e-3f, "회전해도 높이는 같아야 합니다.");
            Assert.Less(front.z, 0f, "yaw 0이면 카메라는 -z쪽(뒤)에 있습니다.");
            Assert.Less(side.x, 0f, "yaw 90이면 카메라는 -x쪽에 있습니다.");
        }
    }
}
