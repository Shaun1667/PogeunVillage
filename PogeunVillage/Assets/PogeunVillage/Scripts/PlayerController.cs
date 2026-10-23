using UnityEngine;
using UnityEngine.InputSystem;

namespace PogeunVillage
{
    /// <summary>
    /// 프로토타입용 플레이어 이동. WASD/방향키/왼쪽 스틱으로 이동, Shift/A버튼으로 달리기.
    /// 물(waterLevel 아래)로는 들어가지 않습니다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float walkSpeed = 4f;
        public float runSpeed = 7f;
        public float turnSpeed = 720f;
        public float gravity = 20f;

        [Tooltip("이 높이보다 낮은 지면(강/바다)으로는 이동하지 않습니다.")]
        public float waterLevel = -0.3f;

        public Transform cameraTransform;

        [Tooltip("걸을 때 통통 튀는 연출을 줄 외형 오브젝트")]
        public Transform visual;
        public float bobHeight = 0.08f;
        public float bobFrequency = 12f;

        CharacterController controller;
        float verticalVelocity;
        float bobTime;
        Vector3 visualBasePosition;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (!cameraTransform && Camera.main)
                cameraTransform = Camera.main.transform;
            if (visual)
                visualBasePosition = visual.localPosition;
        }

        void Update()
        {
            Vector2 input = ReadMoveInput();
            bool running = (Keyboard.current?.leftShiftKey.isPressed ?? false)
                           || (Gamepad.current?.buttonSouth.isPressed ?? false);

            Vector3 move = Vector3.zero;
            if (input.sqrMagnitude > 0.01f)
            {
                Vector3 forward = cameraTransform
                    ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized
                    : Vector3.forward;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                Vector3 desired = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);

                Quaternion targetRotation = Quaternion.LookRotation(desired);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

                move = KeepOutOfWater(desired * (running ? runSpeed : walkSpeed));
            }

            verticalVelocity = controller.isGrounded ? -2f : verticalVelocity - gravity * Time.deltaTime;
            controller.Move((move + Vector3.up * verticalVelocity) * Time.deltaTime);

            UpdateBob(move.magnitude);
        }

        static Vector2 ReadMoveInput()
        {
            Vector2 value = Vector2.zero;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) value.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) value.y -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) value.x += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) value.x -= 1f;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
                value += gamepad.leftStick.ReadValue();

            return Vector2.ClampMagnitude(value, 1f);
        }

        Vector3 KeepOutOfWater(Vector3 move)
        {
            if (IsWalkable(move))
                return move;

            // 물가에서는 막힌 축만 빼고 미끄러지듯 이동
            var alongX = new Vector3(move.x, 0f, 0f);
            if (alongX.sqrMagnitude > 1e-4f && IsWalkable(alongX))
                return alongX;
            var alongZ = new Vector3(0f, 0f, move.z);
            if (alongZ.sqrMagnitude > 1e-4f && IsWalkable(alongZ))
                return alongZ;
            return Vector3.zero;
        }

        bool IsWalkable(Vector3 move)
        {
            Vector3 probe = transform.position + move.normalized * (controller.radius + 0.25f) + Vector3.up * 10f;
            if (Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return hit.point.y > waterLevel + 0.05f;
            return false;
        }

        void UpdateBob(float speed)
        {
            if (!visual)
                return;

            if (speed > 0.1f)
                bobTime += Time.deltaTime * bobFrequency * Mathf.Lerp(0.8f, 1.2f, speed / runSpeed);
            else
                bobTime = Mathf.MoveTowards(bobTime, Mathf.Round(bobTime / Mathf.PI) * Mathf.PI, Time.deltaTime * 6f);

            float bob = Mathf.Abs(Mathf.Sin(bobTime));
            visual.localPosition = visualBasePosition + Vector3.up * (bob * bobHeight);
            float squash = 1f + (bob - 0.5f) * 0.06f;
            visual.localScale = new Vector3(1f / Mathf.Sqrt(squash), squash, 1f / Mathf.Sqrt(squash));
        }
    }
}
