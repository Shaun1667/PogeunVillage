using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>1-1 이동 · 카메라 정리 확인 (WASD · Shift · 물 · 휠 줌 · Q/E · 따라가기 · 셰이더 전역값).</summary>
    public class MovementCameraTests : VillageSceneTest
    {
        const float SpeedTolerance = 0.15f; // 15%

        IEnumerator HoldAndMeasureSpeed(System.Action press, float seconds, System.Action<float> report)
        {
            press();
            yield return Frames(2);
            Vector2 start = Flat(player.position);
            float t0 = Time.time;
            yield return WaitSeconds(seconds);
            report(Vector2.Distance(start, Flat(player.position)) / (Time.time - t0));
        }

        [UnityTest]
        public IEnumerator Walk_MovesAtWalkSpeed()
        {
            float speed = 0f;
            yield return HoldAndMeasureSpeed(() => Press(keyboard.sKey), 0.5f, s => speed = s);
            Release(keyboard.sKey);
            Assert.AreEqual(settings.walkSpeed, speed, settings.walkSpeed * SpeedTolerance, "걷기 속도");
        }

        [UnityTest]
        public IEnumerator Run_MovesAtRunSpeed()
        {
            float speed = 0f;
            yield return HoldAndMeasureSpeed(() => { Press(keyboard.leftShiftKey); Press(keyboard.sKey); }, 0.5f, s => speed = s);
            Release(keyboard.sKey);
            Release(keyboard.leftShiftKey);
            Assert.AreEqual(settings.runSpeed, speed, settings.runSpeed * SpeedTolerance, "달리기 속도");
        }

        [UnityTest]
        public IEnumerator Move_IsCameraRelative_AndTurnsPlayer()
        {
            // 카메라 yaw 0 → D는 +x(화면 오른쪽)
            Vector2 start = Flat(player.position);
            Press(keyboard.dKey);
            yield return WaitSeconds(0.5f);
            Release(keyboard.dKey);
            Vector2 moved = Flat(player.position) - start;

            Assert.Greater(moved.x, 1f, "오른쪽으로 움직여야 합니다.");
            Assert.Less(Mathf.Abs(moved.y), 0.3f, "앞뒤로는 거의 움직이지 않아야 합니다.");
            Assert.Less(Vector3.Angle(player.forward, Vector3.right), 10f, "이동 방향을 바라봐야 합니다.");
        }

        [UnityTest]
        public IEnumerator Release_Stops()
        {
            Press(keyboard.sKey);
            yield return WaitSeconds(0.3f);
            Release(keyboard.sKey);
            yield return Frames(2);
            Vector2 stopped = Flat(player.position);
            yield return WaitSeconds(0.3f);
            Assert.Less(Vector2.Distance(stopped, Flat(player.position)), 0.02f);
        }

        [UnityTest]
        public IEnumerator Water_BlocksPlayer()
        {
            // z=-12 에서 강 중심은 x≈9.5. 그 서쪽 강둑에서 강 쪽(+x)으로 3초 걸음(막히지 않으면 12m 이동)
            Teleport(new Vector3(3.5f, 0f, -12f), Vector3.right);
            yield return Frames(3);
            float startX = player.position.x;
            float lowestGround = float.MaxValue;

            Press(keyboard.dKey);
            float end = Time.time + 3f;
            while (Time.time < end)
            {
                lowestGround = Mathf.Min(lowestGround, GroundHeight(player.position));
                yield return null;
            }
            Release(keyboard.dKey);

            Assert.Greater(player.position.x - startX, 1.5f, "물가까지는 걸어가야 합니다.");
            Assert.Less(player.position.x, 9.5f, "강을 건너면 안 됩니다.");
            Assert.Greater(lowestGround, settings.waterLevel, "물 높이 아래 땅을 밟으면 안 됩니다.");
        }

        float CameraDistance() =>
            Vector3.Distance(camera.transform.position, player.position + Vector3.up * settings.cameraHeightOffset);

        IEnumerator Scroll(float amount)
        {
            Set(mouse.scroll, new Vector2(0f, amount));
            yield return null;
            Set(mouse.scroll, Vector2.zero);
            yield return Frames(2);
        }

        [UnityTest]
        public IEnumerator Camera_StartsAtSettingsPose()
        {
            Assert.AreEqual(settings.cameraDistance, CameraDistance(), 0.05f, "기본 거리");
            Assert.AreEqual(settings.cameraPitch, camera.transform.eulerAngles.x, 0.1f, "내려다보는 각");
            yield break;
        }

        [UnityTest]
        public IEnumerator Zoom_WheelChangesDistance_WithinRange()
        {
            yield return Scroll(100f);
            Assert.AreEqual(settings.cameraDistance - 100f * settings.cameraZoomSpeed, CameraDistance(), 0.05f, "휠 한 번");

            yield return Scroll(100000f);
            Assert.AreEqual(settings.cameraDistanceRange.x, CameraDistance(), 0.05f, "최소 거리에서 멈춤");

            yield return Scroll(-100000f);
            Assert.AreEqual(settings.cameraDistanceRange.y, CameraDistance(), 0.05f, "최대 거리에서 멈춤");
        }

        IEnumerator HoldAndMeasureYaw(UnityEngine.InputSystem.Controls.KeyControl key, System.Action<float, float> report)
        {
            Vector3 f0 = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
            Press(key);
            float t0 = Time.time;
            yield return WaitSeconds(0.5f);
            Release(key);
            float elapsed = Time.time - t0;
            yield return null;
            Vector3 f1 = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
            report(Vector3.SignedAngle(f0, f1, Vector3.up), settings.cameraRotateSpeed * elapsed);
        }

        [UnityTest]
        public IEnumerator RotateQ_TurnsCameraClockwise_AtRotateSpeed()
        {
            float angle = 0f, expected = 0f;
            yield return HoldAndMeasureYaw(keyboard.qKey, (a, e) => { angle = a; expected = e; });
            Assert.AreEqual(expected, angle, 6f, "Q 회전 각도");
        }

        [UnityTest]
        public IEnumerator RotateE_TurnsCameraCounterClockwise()
        {
            float angle = 0f, expected = 0f;
            yield return HoldAndMeasureYaw(keyboard.eKey, (a, e) => { angle = a; expected = e; });
            Assert.AreEqual(-expected, angle, 6f, "E 회전 각도");
        }

        [UnityTest]
        public IEnumerator Camera_FollowsPlayer()
        {
            Teleport(player.position + new Vector3(4f, 0f, 3f), Vector3.forward);
            yield return WaitSeconds(1f);
            Vector3 expected = CameraOrbit.Position(player.position + Vector3.up * settings.cameraHeightOffset,
                settings.cameraPitch, 0f, settings.cameraDistance);
            Assert.Less(Vector3.Distance(expected, camera.transform.position), 0.1f, "1초 뒤 카메라가 따라와 있어야 합니다.");
        }

        [UnityTest]
        public IEnumerator Shader_GetsCurveAndOcclusionGlobals()
        {
            // 씬 뷰 카메라도 같은 전역값을 덮어쓰므로, 게임 카메라가 그릴 때의 값을 읽음
            Vector4 curve = default, occluder = default, occluderParams = default;
            bool captured = false;
            void Capture(UnityEngine.Rendering.ScriptableRenderContext _, Camera cam)
            {
                if (cam != camera) return;
                curve = Shader.GetGlobalVector("_PV_CurveParams");
                occluder = Shader.GetGlobalVector("_PV_OccluderTarget");
                occluderParams = Shader.GetGlobalVector("_PV_OccluderParams");
                captured = true;
            }
            UnityEngine.Rendering.RenderPipelineManager.endCameraRendering += Capture;
            for (int i = 0; i < 30 && !captured; i++)
                yield return null;
            UnityEngine.Rendering.RenderPipelineManager.endCameraRendering -= Capture;

            Assert.IsTrue(captured, "게임 카메라가 그려지지 않았습니다.");

            Assert.AreEqual(settings.curveRadius, curve.x, 1e-3f, "휨 반경");
            Assert.AreEqual(1f, curve.y, "휨 켜짐");
            Assert.AreEqual(1f, occluder.w, "가림 반투명 켜짐(게임 카메라)");
            Assert.Less(Vector3.Distance(player.position + Vector3.up * settings.occlusionTargetHeight, occluder), 0.05f, "가림 기준점 = 플레이어");
            Assert.AreEqual(settings.occlusionRadius, occluderParams.x, 1e-3f);
        }
    }
}
