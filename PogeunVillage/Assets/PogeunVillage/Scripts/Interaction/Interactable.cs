using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>상호작용 대상 종류(도구의 맞음 판정 등에 씀). enum은 끝에만 추가.</summary>
    public enum InteractableKind
    {
        None,
        Tree,
        NoticeBoard,
        Mailbox,
        DroppedItem,
        Flower,
        ToolBox,
        ForestTree,  // 맵 가장자리 숲: 흔들 수는 있지만 벨 수 없음
        Stump,
        Hole,
        Bed,
    }

    /// <summary>
    /// 플레이어가 다가가 상호작용 키로 쓸 수 있는 대상(나무 · 게시판 · 우체통 …).
    /// 켜져 있는 대상은 All 목록에 등록되어 PlayerInteractor가 고릅니다.
    /// 실제 동작은 각 기능(FruitTreeBehaviour · Pickup …)이 Interacted를 구독해 붙입니다.
    /// wiggleOnInteract면 상호작용 때 살짝 흔들립니다(나무 흔들기 · 아직 동작이 없는 대상의 임시 반응).
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        static readonly List<Interactable> all = new();
        public static IReadOnlyList<Interactable> All => all;

        [Tooltip("로그 · 안내에 쓰는 이름")]
        public string displayName = "대상";
        public InteractableKind kind;
        [Tooltip("바닥에서 본 대상의 크기(반지름, m, 로컬 기준). 상호작용 거리는 이 가장자리부터 잽니다.")]
        public float radius = 0.4f;
        [Tooltip("마커가 뜰 높이(m, 로컬 기준)")]
        public float markerHeight = 2f;
        public bool wiggleOnInteract = true;

        /// <summary>대상과 상호작용한 플레이어</summary>
        public event Action<Interactable, PlayerInteractor> Interacted;
        /// <summary>도구에 맞음(도구의 hitKinds에 든 대상만). 휘두른 플레이어와 도구</summary>
        public event Action<Interactable, PlayerTools, ToolDefinition> HitByTool;

        public Vector3 MarkerPosition => transform.position + Vector3.up * (markerHeight * transform.lossyScale.y);
        public float WorldRadius => radius * transform.lossyScale.x;

        Coroutine wiggle;
        Quaternion baseRotation;

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public void Interact(PlayerInteractor by)
        {
            Debug.Log($"[상호작용] {displayName}", this);
            Interacted?.Invoke(this, by);

            if (wiggleOnInteract)
                Wiggle(by.settings);
        }

        public void ReceiveToolHit(PlayerTools by, ToolDefinition tool)
        {
            HitByTool?.Invoke(this, by, tool);
        }

        /// <summary>살짝 흔들림(나무 흔들기 · 도끼에 맞음 · 아직 동작이 없는 대상의 임시 반응)</summary>
        public void Wiggle(WorldSettings settings)
        {
            if (!isActiveAndEnabled)
                return;
            if (wiggle != null)
            {
                StopCoroutine(wiggle);
                transform.localRotation = baseRotation;
            }
            wiggle = StartCoroutine(WiggleRoutine(settings.interactWiggleAngle, settings.interactWiggleTime));
        }

        IEnumerator WiggleRoutine(float angle, float duration)
        {
            baseRotation = transform.localRotation;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float swing = Mathf.Sin(t / duration * Mathf.PI * 3f) * (1f - t / duration) * angle;
                transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, swing);
                yield return null;
            }
            transform.localRotation = baseRotation;
            wiggle = null;
        }
    }
}
