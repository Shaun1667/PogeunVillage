using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>1-2 상호작용 확인 (대상 고르기 · 마커 · F / 패드 A · 패드 B 달리기).</summary>
    public class InteractionTests : VillageSceneTest
    {
        PlayerInteractor interactor;
        InteractionMarker marker;
        Interactable board;

        [UnitySetUp]
        public IEnumerator FindParts()
        {
            interactor = player.GetComponent<PlayerInteractor>();
            marker = Object.FindAnyObjectByType<InteractionMarker>();
            board = GameObject.Find("NoticeBoard").GetComponent<Interactable>();
            Assert.IsNotNull(interactor);
            Assert.IsNotNull(marker);
            Assert.IsNotNull(board);
            yield break;
        }

        bool MarkerShown => marker.transform.GetChild(0).gameObject.activeSelf;

        Vector3 BoardFront
        {
            get
            {
                Vector3 normal = Vector3.ProjectOnPlane(board.transform.forward, Vector3.up).normalized;
                return board.transform.position + normal * 1.6f;
            }
        }

        IEnumerator StandFacingBoard()
        {
            Vector3 spot = BoardFront;
            Teleport(spot, Vector3.ProjectOnPlane(board.transform.position - spot, Vector3.up));
            yield return Frames(3);
        }

        IEnumerator Tap(ButtonControl button)
        {
            Press(button);
            yield return null;
            Release(button);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FacingBoard_SelectsItAndShowsMarker()
        {
            yield return StandFacingBoard();
            Assert.AreSame(board, interactor.Target, "게시판이 대상이어야 합니다.");
            Assert.IsTrue(MarkerShown, "마커가 보여야 합니다.");
            Assert.Less(Vector3.Distance(Flat(marker.transform.position), Flat(board.MarkerPosition)), 0.01f, "마커가 게시판 위에 있어야 합니다.");
            Assert.That(marker.transform.position.y - board.MarkerPosition.y, Is.InRange(-0.01f, settings.markerBobHeight + 0.01f));
        }

        [UnityTest]
        public IEnumerator TurningAway_ClearsTargetAndHidesMarker()
        {
            yield return StandFacingBoard();
            Vector3 away = Vector3.ProjectOnPlane(player.position - board.transform.position, Vector3.up);
            Teleport(player.position, away);
            yield return Frames(3);
            Assert.IsNull(interactor.Target, "등 뒤 게시판은 대상이 아니어야 합니다.");
            Assert.IsFalse(MarkerShown, "마커가 꺼져야 합니다.");
        }

        [UnityTest]
        public IEnumerator FKey_InteractsAndWiggles()
        {
            yield return StandFacingBoard();
            int count = 0;
            board.Interacted += (_, by) => count++;
            Quaternion before = board.transform.localRotation;

            // 프레임 간격에 따라 흔들림 중간을 놓칠 수 있으므로 매 프레임 가장 큰 기울기를 기록
            float swing = 0f;
            Press(keyboard.fKey);
            float end = Time.time + settings.interactWiggleTime + 0.2f;
            for (int frame = 0; Time.time < end || frame < 3; frame++)
            {
                if (frame == 1) Release(keyboard.fKey);
                swing = Mathf.Max(swing, Quaternion.Angle(before, board.transform.localRotation));
                yield return null;
            }

            Assert.AreEqual(1, count, "F 한 번 = 상호작용 한 번");
            Assert.Greater(swing, 0.5f, "흔들려야 합니다.");
            Assert.Less(Quaternion.Angle(before, board.transform.localRotation), 0.01f, "흔들림 뒤 원래 자세로");
        }

        [UnityTest]
        public IEnumerator GamepadA_Interacts()
        {
            yield return StandFacingBoard();
            int count = 0;
            board.Interacted += (_, by) => count++;
            yield return Tap(gamepad.buttonSouth);
            Assert.AreEqual(1, count);
        }

        [UnityTest]
        public IEnumerator NoTarget_FKeyDoesNothing()
        {
            yield return StandFacingBoard();
            Teleport(player.position, Vector3.ProjectOnPlane(player.position - board.transform.position, Vector3.up));
            yield return Frames(3);
            int count = 0;
            board.Interacted += (_, by) => count++;
            yield return Tap(keyboard.fKey);
            Assert.AreEqual(0, count);
        }

        IEnumerator StickSpeed(ButtonControl heldButton, System.Action<float> report)
        {
            if (heldButton != null) Press(heldButton);
            Set(gamepad.leftStick, new Vector2(0f, -1f));
            yield return Frames(2);
            Vector2 start = Flat(player.position);
            float t0 = Time.time;
            yield return WaitSeconds(0.4f);
            report(Vector2.Distance(start, Flat(player.position)) / (Time.time - t0));
            Set(gamepad.leftStick, Vector2.zero);
            if (heldButton != null) Release(heldButton);
            yield return Frames(2);
        }

        [UnityTest]
        public IEnumerator GamepadB_Runs_GamepadA_DoesNot()
        {
            float withB = 0f, withA = 0f;
            yield return StickSpeed(gamepad.buttonEast, s => withB = s);
            yield return StickSpeed(gamepad.buttonSouth, s => withA = s);
            Assert.AreEqual(settings.runSpeed, withB, settings.runSpeed * 0.15f, "패드 B + 스틱 = 달리기");
            Assert.AreEqual(settings.walkSpeed, withA, settings.walkSpeed * 0.15f, "패드 A는 달리기가 아님");
        }
    }
}
