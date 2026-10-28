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

        [Header("구멍에 빠진 주민 (BALANCE 5번 5-8, 2-5)")]
        [Tooltip("빠졌다 나오면 줄어드는 친밀도")]
        public int holePenalty = 2;
        [Tooltip("빠져서 못 움직이는 시간(초). 나오면 구멍이 사라짐")]
        public float holeStuckSeconds = 3f;
    }
}
