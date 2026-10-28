using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 주민 · 집터 · 배회 수치. 값의 기준은 BALANCE.md 13번. 에셋: Assets/PogeunVillage/Data/VillageRules.asset
    /// </summary>
    [CreateAssetMenu(fileName = "VillageRules", menuName = "Pogeun Village/Village Rules")]
    public class VillageRules : ScriptableObject
    {
        [Tooltip("전체 주민(20명)")]
        public VillagerDefinition[] villagers = { };
        [Tooltip("시작 주민 수")]
        [Min(0)] public int startingResidents = 6;
        [Tooltip("시작 주민 · 집터 고르기 시드(같은 시드 = 같은 주민 · 같은 집터)")]
        public int residentSeed = 2026;
        [Tooltip("배회 목표 고르기 시드(주민마다 집터 번호를 더함)")]
        public int wanderSeed = 77;
        [Tooltip("대사 고르기 시드")]
        public int dialogueSeed = 19;

        [Header("배회")]
        [Tooltip("걷는 속도(m/초)")]
        public float walkSpeed = 1.6f;
        [Tooltip("회전 속도(도/초)")]
        public float turnSpeed = 360f;
        [Tooltip("자기 집 현관에서 이 거리(m) 안을 돌아다님")]
        public float wanderRadius = 14f;
        [Tooltip("목표에 닿은 뒤 멈춰 있는 시간(초) 범위")]
        public Vector2 pauseSeconds = new(1.5f, 4f);
        [Tooltip("이 시간(초) 동안 0.2m도 못 가면 막힌 것으로 보고 목표를 바꿈")]
        public float stuckSeconds = 1.5f;
        [Tooltip("목표를 찾을 때 시도 횟수")]
        [Min(1)] public int wanderTries = 12;
        [Tooltip("곧게 걸을 수 있는지 확인하는 간격(m)")]
        public float walkCheckStep = 0.5f;
        [Tooltip("대화가 끝난 뒤 멈춰 있는 시간(초)")]
        public float afterTalkPause = 1.5f;

        [Header("친밀도 (BALANCE 3번 · 캠핑 주민도 같은 값, 2-5)")]
        [Tooltip("그날 첫 대화에서만")]
        public int talkGain = 1;
        [Tooltip("선물 1개당, 종류 무관")]
        public int giftGain = 2;
        [Tooltip("하루에 올릴 수 있는 양(넘는 만큼 버림, 감소와는 별개)")]
        public int dailyGainCap = 5;
        public int friendshipMin = -20;
        public int friendshipMax = 100;

        [Header("캠핑 구역 (BALANCE 1번 · 13번, 2-6)")]
        [Tooltip("마을 최대 인원(이주 확정 주민 포함). 꽉 차면 캠핑 주민이 오지 않음")]
        public int maxResidents = 12;
        [Tooltip("캠핑 방문 주민 고르기 시드")]
        public int campSeed = 404;
        [Tooltip("캠핑 주민이 텐트 입구에서 이 거리(m) 안을 돌아다님")]
        public float campWanderRadius = 5f;
        [Tooltip("캠핑 주민 친밀도가 이 값에 닿으면 이주 확정(3-1). 대화 · 선물 · 하루 상한은 위 친밀도 값과 같음")]
        public int moveInThreshold = 5;

        [Header("괴롭히기 · 주민 떠남 (BALANCE 2번, 3-2)")]
        [Tooltip("잠자리채로 연속 이만큼 때릴 때마다 깎임(주민마다 따로 셈)")]
        [Min(1)] public int bullyHitsPerPenalty = 5;
        [Tooltip("위 횟수마다 줄어드는 친밀도")]
        public int bullyPenalty = 2;
        [Tooltip("마지막으로 때린 뒤 이 시간(실제 초, 일시정지 제외)이 지나면 연속이 끊김")]
        public float bullyStreakTimeoutSeconds = 180f;
        [Tooltip("아침에 친밀도가 이 값 이하면 그날 이사 준비 → 다음 날 아침 떠남")]
        public int leaveThreshold = -20;

        [Header("주민 부탁 (BALANCE 14번, 3-3)")]
        [Tooltip("부탁할 수 있는 물건(맵 생성 메뉴가 사과 · 복숭아 · 꽃 · 목재로 채움)")]
        public ItemDefinition[] requestItems = { };
        [Tooltip("부탁하는 개수 범위(포함)")]
        public Vector2Int requestCount = new(1, 3);
        [Tooltip("들어주면 오르는 친밀도(하루 상한과 별개)")]
        public int requestFriendshipGain = 3;
        [Tooltip("보상 모아 = 팔 때 값 × 개수 × 이 배수")]
        public int requestMoaMultiplier = 2;
        [Tooltip("오늘의 부탁 고르기 시드")]
        public int requestSeed = 31;

        [Header("테마 가구 친밀도 보상 (BALANCE.md 16번, 3-4d)")]
        [Tooltip("그 주민 친밀도가 처음 닿을 때마다 테마 가구를 하나씩(차례: 화분 · 벤치 · 탁자 · 가로등)")]
        public int[] themeRewardThresholds = { 10, 25, 45, 70 };

        [Header("구멍에 빠진 주민 (BALANCE 5번 5-8, 2-5)")]
        [Tooltip("빠졌다 나오면 줄어드는 친밀도")]
        public int holePenalty = 2;
        [Tooltip("빠져서 못 움직이는 시간(초). 나오면 구멍이 사라짐")]
        public float holeStuckSeconds = 3f;
    }
}
