using UnityEngine;
using UnityEngine.InputSystem;

namespace PogeunVillage
{
    /// <summary>
    /// 낚싯대(결정 2026-10-29, 3-6a): 들고 Space(패드 X) = 바라보는 앞 castDistance의 물에 찌를 던짐(물이 아니면 알림).
    /// 찌 가까이 그림자가 있으면 다가와 속임수 뒤 입질(찌가 쑥) → 그때 Space = 낚음(인벤토리, 가득 차면 발밑에) · 내구도 -1.
    /// 입질 전에 누르거나 늦으면 놓침(그림자 도망). 그림자가 오지 않으면 Space = 거두기. 던진 동안은 걷지 않음(PlayerLock).
    /// 시간표는 BiteTimeline(순수), 수치는 FishingRules(BALANCE.md 17번).
    /// </summary>
    public class FishingRodUser : MonoBehaviour, IToolPress
    {
        const int IgnoreRaycastLayer = 2;

        public FishingRules rules;
        public WorldSettings settings;
        public FishShadows shadows;
        public VillageGround ground;
        public PlayerInventory inventory;
        public PlayerTools tools;
        public PlayerLock playerLock;
        [Tooltip("찌(장면 안에 하나, 던질 때만 보임)")]
        public Transform bobber;
        [Tooltip("인벤토리가 가득 찼을 때 발밑에 떨어뜨릴 물건")]
        public Pickup pickupPrefab;
        [Tooltip("찌가 속임수 · 입질 때 가라앉는 깊이(m)")]
        public float nibbleDip = 0.05f, biteDip = 0.2f;

        public bool IsCasting { get; private set; }
        public BiteTimeline Timeline { get; private set; }
        public FishShadow Target { get; private set; }
        public BitePhase Phase => Timeline?.PhaseAt(Time.time) ?? BitePhase.Waiting;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        IRandomSource random;
        Vector3 bobberRest;
        int castFrame = -1;
        ToolDefinition rod;

        void Start()
        {
            if (!rules || !settings || !shadows || !ground || !inventory || !tools || !playerLock || !bobber || !pickupPrefab)
            {
                Debug.LogError($"{name}: 낚싯대 연결이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            random = new SystemRandomSource(rules.seed + 2);
            bobber.gameObject.SetActive(false);
        }

        public bool TryPress(PlayerTools by, ToolDefinition tool)
        {
            if (tool.kind != ToolKind.FishingRod)
                return false;
            if (enabled && !IsCasting)
                Cast(tool);
            return true;
        }

        void Cast(ToolDefinition tool)
        {
            Vector3 point = transform.position + transform.forward * rules.castDistance;
            if (!ground.data.IsWater(ground.CellAt(point)))
            {
                GameNotice.Post("[낚시] 물가에서 앞쪽 물에 던져요", this);
                return;
            }
            rod = tool;
            IsCasting = true;
            castFrame = Time.frameCount;
            bobberRest = new Vector3(point.x, settings.waterLevel + 0.05f, point.z);
            bobber.position = bobberRest;
            bobber.gameObject.SetActive(true);
            playerLock.Acquire(this);

            Target = shadows.NearestWithin(point, rules.approachDistance);
            if (!Target)
                return;  // 아무것도 오지 않음 — Space로 거두기
            Target.Attract(point, rules.approachSeconds);
            Timeline = BiteTimeline.Create(Time.time, rules.approachSeconds, rules.nibbleCount.x, rules.nibbleCount.y,
                rules.nibbleInterval.x, rules.nibbleInterval.y, rules.biteWindow, random);
        }

        void Update()
        {
            if (!IsCasting)
                return;
            var phase = Phase;
            bobber.position = bobberRest + Vector3.down * (phase == BitePhase.Bite ? biteDip : phase == BitePhase.Nibble ? nibbleDip : 0f);
            if (Timeline != null && phase == BitePhase.Gone)
            {
                Fail("[낚시] 놓쳤어요… 찌가 가라앉을 때 바로 Space");
                return;
            }

            bool pressed = Time.frameCount != castFrame &&
                           ((Keyboard.current?.spaceKey.wasPressedThisFrame ?? false) || (Gamepad.current?.buttonWest.wasPressedThisFrame ?? false));
            if (!pressed)
                return;
            if (Timeline == null)
            {
                End();  // 거두기
                return;
            }
            switch (Timeline.Press(Time.time))
            {
                case CatchResult.Caught: Catch(); break;
                case CatchResult.Spooked: Fail("[낚시] 놓쳤어요… 너무 일찍 당겼어요"); break;
                default: Fail("[낚시] 놓쳤어요… 찌가 가라앉을 때 바로 Space"); break;
            }
        }

        void Catch()
        {
            var fish = Target.Fish;
            shadows.Remove(Target);
            if (inventory.Inventory.SpaceFor(fish.id, fish.maxStack) >= 1)
                inventory.TryAdd(fish);
            else
                DropAtFeet(fish);
            GameNotice.Post($"[낚시] {fish.displayName}을(를) 낚았어요! ({HudText.Moa(fish.sellPrice)})", this);
            tools.UseDurability(rod);
            End();
        }

        void Fail(string message)
        {
            shadows.Remove(Target);  // 도망
            GameNotice.Post(message, this);
            End();
        }

        void End()
        {
            IsCasting = false;
            Timeline = null;
            Target = null;
            rod = null;
            bobber.gameObject.SetActive(false);
            playerLock.Release(this);
        }

        // 인벤토리가 가득 찼을 때: 발밑 땅에 떨어진 물고기(F로 줍기)
        void DropAtFeet(FishDefinition fish)
        {
            Vector3 spot = transform.position - transform.forward * 0.6f;
            if (Physics.Raycast(spot + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f, ~(1 << IgnoreRaycastLayer), QueryTriggerInteraction.Ignore))
                spot = hit.point;
            var pickup = Instantiate(pickupPrefab, spot, Quaternion.identity);
            pickup.name = fish.id;
            pickup.item = fish;
            pickup.GetComponent<Interactable>().displayName = fish.displayName;
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, fish.color);
            pickup.GetComponentInChildren<Renderer>().SetPropertyBlock(block);
            GameNotice.Post($"[인벤토리] 가득 차서 {fish.displayName}을(를) 발밑에 내려놨어요", this);
        }
    }
}
