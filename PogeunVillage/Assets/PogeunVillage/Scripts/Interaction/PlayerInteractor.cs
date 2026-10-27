using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PogeunVillage
{
    /// <summary>
    /// 플레이어 주변에서 상호작용 대상을 고르고(InteractionTargeting), F / 패드 A로 상호작용합니다.
    /// 대상이 바뀔 때만 TargetChanged를 알립니다. 수치는 WorldSettings.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public WorldSettings settings;

        public Interactable Target { get; private set; }
        public event Action<Interactable> TargetChanged;

        readonly List<InteractionCandidate> candidates = new();

        void Awake()
        {
            if (!settings)
            {
                Debug.LogError($"{name}: WorldSettings가 연결되지 않았습니다.", this);
                enabled = false;
            }
        }

        void OnDisable() => SetTarget(null);

        void Update()
        {
            SetTarget(FindTarget());

            bool pressed = (Keyboard.current?.fKey.wasPressedThisFrame ?? false)
                           || (Gamepad.current?.buttonSouth.wasPressedThisFrame ?? false);
            if (pressed && Target)
                Target.Interact(settings);
        }

        Interactable FindTarget()
        {
            var all = Interactable.All;
            candidates.Clear();
            for (int i = 0; i < all.Count; i++)
            {
                Vector3 p = all[i].transform.position;
                candidates.Add(new InteractionCandidate(new Vector2(p.x, p.z), all[i].WorldRadius));
            }

            Vector3 position = transform.position;
            Vector3 forward = transform.forward;
            int index = InteractionTargeting.Select(candidates, new Vector2(position.x, position.z),
                new Vector2(forward.x, forward.z), settings.interactRange, settings.interactHalfAngle);
            return index >= 0 ? all[index] : null;
        }

        void SetTarget(Interactable target)
        {
            if (Target == target)
                return;
            Target = target;
            TargetChanged?.Invoke(target);
        }
    }
}
