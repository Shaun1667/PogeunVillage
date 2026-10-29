using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>
    /// VillagePrototype 씬을 열고 가상 키보드 · 마우스 · 패드로 조작하는 PlayMode 테스트의 공통 바탕.
    /// InputTestFixture가 테스트마다 입력 장치를 새로 만들어, 에디터 창 포커스와 상관없이 입력이 들어갑니다.
    /// </summary>
    public abstract class VillageSceneTest : InputTestFixture
    {
        protected Keyboard keyboard;
        protected Mouse mouse;
        protected Gamepad gamepad;

        protected Transform player;
        protected CharacterController controller;
        protected Camera camera;
        protected WorldSettings settings;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            gamepad = InputSystem.AddDevice<Gamepad>();
        }

        [UnitySetUp]
        public IEnumerator LoadVillage()
        {
            SceneManager.LoadScene("VillagePrototype");
            yield return null;
            yield return null;

            var playerController = Object.FindAnyObjectByType<PlayerController>();
            Assert.IsNotNull(playerController, "씬에 PlayerController가 없습니다.");
            player = playerController.transform;
            controller = player.GetComponent<CharacterController>();
            settings = playerController.settings;
            camera = Camera.main;

            // 플레이 기록(4-1)은 사람이 플레이할 때만 — 테스트에서는 끔(파일을 만들지 않음)
            var playLogger = Object.FindAnyObjectByType<PlayLogger>();
            if (playLogger) playLogger.enabled = false;

            // 주민(2-4)이 돌아다니며 대상 · 길을 가로채지 않도록, 주민 테스트가 아니면 마을 주민을 모두 숨김
            if (!WithVillagers)
            {
                var village = Object.FindAnyObjectByType<Village>();
                if (village) village.gameObject.SetActive(false);
            }

            // 곤충(3-7a)도 돌아다니는 대상 · 알림을 만들지 않도록, 곤충 테스트가 아니면 숨김
            if (!WithInsects)
            {
                var insects = Object.FindAnyObjectByType<Insects>();
                if (insects) insects.gameObject.SetActive(false);
            }

            // 처음 몇 프레임은 착지 · 카메라 자리잡기
            yield return WaitSeconds(0.3f);
        }

        /// <summary>주민을 그대로 둘지(주민 테스트만 true)</summary>
        protected virtual bool WithVillagers => false;

        /// <summary>곤충을 그대로 둘지(곤충 테스트만 true)</summary>
        protected virtual bool WithInsects => false;

        protected static IEnumerator WaitSeconds(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
                yield return null;
        }

        protected static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
                yield return null;
        }

        /// <summary>플레이어를 순간이동(지면 위)하고 방향을 정함.</summary>
        protected void Teleport(Vector3 flatPosition, Vector3 facing)
        {
            Vector3 p = flatPosition;
            if (Physics.Raycast(p + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 50f, ~(1 << 2), QueryTriggerInteraction.Ignore))
                p.y = hit.point.y + 0.05f;
            controller.enabled = false;
            player.SetPositionAndRotation(p, Quaternion.LookRotation(facing));
            controller.enabled = true;
        }

        protected static Vector2 Flat(Vector3 v) => new(v.x, v.z);

        /// <summary>그 지점 바로 아래 지면 높이</summary>
        protected static float GroundHeight(Vector3 position)
        {
            Physics.Raycast(position + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 50f, ~(1 << 2), QueryTriggerInteraction.Ignore);
            return hit.point.y;
        }
    }
}
