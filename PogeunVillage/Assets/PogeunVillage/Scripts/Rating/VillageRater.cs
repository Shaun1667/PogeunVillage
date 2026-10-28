using System;
using TMPro;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 마을 평가 어댑터(결정 2026-10-28, 3-5): 게임을 시작할 때와 매일 아침 처리(자연까지) 뒤에 계산해
    /// HUD에 "마을 ★★☆☆☆"를 보이고, 별이 바뀌면 알림(첫 목표에 처음 닿으면 축하 알림). 광장 게시판 F = 항목별 점수(대화창).
    /// 계산은 VillageRating(순수), 수치는 RatingRules.
    /// </summary>
    [DefaultExecutionOrder(100)]  // Village.Start(주민 입주) 뒤에 첫 계산
    public class VillageRater : MonoBehaviour
    {
        public RatingRules rules;
        public Village village;
        public VillageGround ground;
        public GardenKeeper garden;
        public MorningRunner morning;
        public Interactable noticeBoard;
        public DialogueWindow dialogue;
        public TMP_Text ratingText;

        public RatingResult Current { get; private set; }
        public bool GoalReached { get; private set; }
        public event Action<RatingResult> Changed;

        void Start()
        {
            if (!rules || !village || !ground || !garden || !morning || !noticeBoard || !dialogue || !ratingText || village.Residents == null)
            {
                Debug.LogError($"{name}: 마을 평가 연결이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            morning.MorningPassed += Evaluate;
            noticeBoard.Interacted += ShowBoard;
            Evaluate();
        }

        void OnDestroy()
        {
            if (morning) morning.MorningPassed -= Evaluate;
            if (noticeBoard) noticeBoard.Interacted -= ShowBoard;
        }

        /// <summary>지금 마을 상태로 다시 계산(아침 · 시작)</summary>
        public void Evaluate()
        {
            int flowers = 0;
            foreach (var obj in ground.Objects())
                if (obj && obj.kind == GroundObjectKind.Flower && !garden.Garden.IsWilted(obj.Cell))
                    flowers++;
            int friendshipSum = 0;
            foreach (string id in village.Residents.Plots)
                if (id != null) friendshipSum += village.Friendship.Get(id);
            int grownTrees = garden.GetComponentsInChildren<ChoppableTreeBehaviour>().Length;

            var before = Current;
            bool first = before.stars == 0;
            Current = VillageRating.Evaluate(flowers, village.Residents.Count, friendshipSum, grownTrees,
                rules.flowerPoint, rules.residentPoint, rules.friendshipPoint, rules.grownTreePoint, rules.starThresholds);
            ratingText.text = $"마을 {VillageRating.StarText(Current.stars)}";

            if (!first && Current.stars != before.stars)
            {
                if (!GoalReached && Current.stars >= rules.firstGoalStars)
                    GameNotice.Post($"[마을] 첫 목표 달성! 마을 평가 {VillageRating.StarText(Current.stars)}", this);
                else
                    GameNotice.Post($"[마을] 마을 평가가 {VillageRating.StarText(Current.stars)}이(가) 됐어요", this);
            }
            if (Current.stars >= rules.firstGoalStars)
                GoalReached = true;
            Changed?.Invoke(Current);
        }

        void ShowBoard(Interactable _, PlayerInteractor by)
        {
            if (!dialogue.IsOpen)
                dialogue.Open(new Dialogue("게시판", VillageRating.BoardLines(Current, rules.firstGoalStars)), null);
        }
    }
}
