using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 주민 한 명의 어댑터(결정 2026-10-28): 집 현관에서 나와 걸을 수 있는 칸 중 무작위 지점으로 걸어가 잠시 멈추기를 반복하고,
    /// 밤이 되면 현관으로 돌아가 사라짐. F로 말을 걸면 멈추고 플레이어를 보며 대화창을 엶.
    /// 일과 시작 · 끝은 Village가 시계 이벤트로 부름. 목표 고르기는 WalkMap(순수).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class VillagerBehaviour : MonoBehaviour
    {
        public enum State
        {
            Home,
            Walking,
            Pausing,
            Talking,
            GoingHome,
        }

        [Tooltip("몸 · 머리(주민 몸 색)")]
        public Renderer[] bodyRenderers = { };
        [Tooltip("귀(주민 강조 색)")]
        public Renderer[] accentRenderers = { };

        public VillagerDefinition Definition { get; private set; }
        public HousePlot Home { get; private set; }
        public State Current { get; private set; } = State.Home;
        public Vector2? Target { get; private set; }

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        Village village;
        IRandomSource random;
        CharacterController controller;
        Interactable interactable;
        float pauseUntil;
        float stuckCheckAt;
        Vector3 stuckCheckFrom;
        float verticalVelocity;
        State beforeTalk;

        public void Setup(Village owner, VillagerDefinition definition, HousePlot home, IRandomSource wanderRandom)
        {
            village = owner;
            Definition = definition;
            Home = home;
            random = wanderRandom;
            name = "Villager_" + definition.id;
            controller = GetComponent<CharacterController>();
            interactable = GetComponent<Interactable>();
            if (interactable)
            {
                interactable.displayName = definition.displayName;
                interactable.Interacted += OnTalk;
            }
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, definition.bodyColor);
            foreach (var r in bodyRenderers) if (r) r.SetPropertyBlock(block);
            block.SetColor(BaseColorId, definition.accentColor);
            foreach (var r in accentRenderers) if (r) r.SetPropertyBlock(block);
        }

        void OnDestroy()
        {
            if (interactable) interactable.Interacted -= OnTalk;
        }

        /// <summary>현관에서 나옴(아침 · 잠에서 깬 뒤)</summary>
        public void ComeOut()
        {
            gameObject.SetActive(true);
            Place(Home.DoorPosition);
            transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(Home.door ? Home.door.forward : Vector3.forward, Vector3.up));
            Pause(village.rules.pauseSeconds.x);
        }

        /// <summary>집에 들어가 사라짐</summary>
        public void StayHome()
        {
            Current = State.Home;
            Target = null;
            gameObject.SetActive(false);
        }

        /// <summary>밤: 현관으로 걸어감(닿으면 사라짐). 대화 중이면 대화가 끝난 뒤</summary>
        public void GoHome()
        {
            if (Current == State.Home)
                return;
            if (Current == State.Talking)
            {
                beforeTalk = State.GoingHome;
                return;
            }
            StartGoingHome();
        }

        void StartGoingHome()
        {
            Vector3 door = Home.DoorPosition;
            Target = new Vector2(door.x, door.z);
            Current = State.GoingHome;
            ResetStuck();
        }

        void Place(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position + Vector3.up * 0.05f;
            controller.enabled = true;
            verticalVelocity = 0f;
        }

        void Update()
        {
            var rules = village.rules;
            switch (Current)
            {
                case State.Pausing:
                    Fall();
                    if (Time.time >= pauseUntil)
                        PickTarget();
                    break;
                case State.Walking:
                case State.GoingHome:
                    WalkToTarget(rules);
                    break;
                case State.Talking:
                    Fall();
                    break;
            }
        }

        void PickTarget()
        {
            Vector3 door = Home.DoorPosition;
            Target = village.PickWanderTarget(Flat(transform.position), new Vector2(door.x, door.z), random);
            if (Target == null)
            {
                Pause(village.rules.pauseSeconds.x);  // 갈 곳이 없으면 잠시 뒤 다시
                return;
            }
            Current = State.Walking;
            ResetStuck();
        }

        void WalkToTarget(VillageRules rules)
        {
            Vector2 to = Target.Value - Flat(transform.position);
            if (to.magnitude < 0.3f)
            {
                if (Current == State.GoingHome)
                    StayHome();
                else
                    Pause(Mathf.Lerp(rules.pauseSeconds.x, rules.pauseSeconds.y, (float)random.NextDouble()));
                return;
            }

            Vector3 direction = new Vector3(to.x, 0f, to.y).normalized;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), rules.turnSpeed * Time.deltaTime);
            verticalVelocity = controller.isGrounded ? -2f : verticalVelocity - village.settings.gravity * Time.deltaTime;
            controller.Move((direction * rules.walkSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);

            if (Time.time >= stuckCheckAt)
            {
                bool stuck = Vector2.Distance(Flat(stuckCheckFrom), Flat(transform.position)) < 0.2f;
                if (stuck && Current == State.GoingHome)
                    StayHome();  // 집에 가다 막히면 그냥 들어간 것으로
                else if (stuck)
                    Pause(rules.pauseSeconds.x);
                else
                    ResetStuck();
            }
        }

        void Fall()
        {
            if (!controller.enabled)
                return;
            verticalVelocity = controller.isGrounded ? -2f : verticalVelocity - village.settings.gravity * Time.deltaTime;
            controller.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
        }

        void Pause(float seconds)
        {
            Current = State.Pausing;
            Target = null;
            pauseUntil = Time.time + seconds;
        }

        void ResetStuck()
        {
            stuckCheckFrom = transform.position;
            stuckCheckAt = Time.time + village.rules.stuckSeconds;
        }

        void OnTalk(Interactable _, PlayerInteractor by)
        {
            if (Current == State.Talking || !village.dialogue)
                return;
            beforeTalk = Current == State.GoingHome ? State.GoingHome : State.Walking;
            Current = State.Talking;
            Vector3 toPlayer = Vector3.ProjectOnPlane(by.transform.position - transform.position, Vector3.up);
            if (toPlayer.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.LookRotation(toPlayer);
            village.dialogue.Open(village.ComposeTalk(Definition), EndTalk);
        }

        void EndTalk()
        {
            if (beforeTalk == State.GoingHome)
                StartGoingHome();
            else
                Pause(village.rules.afterTalkPause);
        }

        static Vector2 Flat(Vector3 v) => new(v.x, v.z);
    }
}
