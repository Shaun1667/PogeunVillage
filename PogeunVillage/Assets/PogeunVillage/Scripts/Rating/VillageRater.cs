using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PogeunVillage
{
    /// <summary>
    /// 마을 평가 어댑터(결정 2026-10-28, 3-5): 게임을 시작할 때와 매일 아침 처리(자연까지) 뒤에 계산해(놓인 가구도, 3-4e)
    /// HUD에 "마을 ★★☆☆☆"를 보이고, 별이 바뀌면 알림(첫 목표에 처음 닿으면 축하 알림). 광장 게시판 F = '마을 평가 / 물고기 도감' 고르기 → 항목별 점수(대화창) · 도감(목록 창).
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
        [Tooltip("놓인 가구(3-4e)")]
        public FurnitureKeeper furniture;
        [Tooltip("물고기 도감(3-6b): 잡은 종류 점수 · 게시판에서 열기")]
        public FishEncyclopedia encyclopedia;
        public Interactable noticeBoard;
        public DialogueWindow dialogue;
        public TMP_Text ratingText;

        public RatingResult Current { get; private set; }
        public bool GoalReached { get; private set; }
        public event Action<RatingResult> Changed;

        void Start()
        {
            if (!rules || !village || !ground || !garden || !morning || !furniture || !encyclopedia || !noticeBoard || !dialogue || !ratingText || village.Residents == null)
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
            var placed = new List<(string, int, string)>();
            foreach (var p in furniture.Placed)
                if (p && p.Definition)
                {
                    var owner = p.Definition.themeOwner;
                    placed.Add((p.Definition.id, p.Definition.ratingPoint, owner ? owner.id : null));
                }
            int furniturePoints = VillageRating.FurniturePoints(placed, rules.furnitureKindCap, rules.themeSetBonus, FurnitureRules.ThemePieces);

            Current = VillageRating.Evaluate(flowers, village.Residents.Count, friendshipSum, grownTrees,
                rules.flowerPoint, rules.residentPoint, rules.friendshipPoint, rules.grownTreePoint, rules.starThresholds, furniturePoints,
                encyclopedia.Log.Species * rules.fishSpeciesPoint);
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

        static readonly string[] BoardChoices = { "마을 평가", "물고기 도감" };

        // 게시판 F(3-6b): 무엇을 볼지 고르기 → 마을 평가(대화창 3줄) / 물고기 도감(목록 창)
        void ShowBoard(Interactable _, PlayerInteractor by)
        {
            if (dialogue.IsOpen)
                return;
            dialogue.Open(new Dialogue("게시판", new[] { "무엇을 볼까요?" }, BoardChoices), d =>
            {
                if (d.Chosen == 0)
                    dialogue.Open(new Dialogue("게시판", VillageRating.BoardLines(Current, rules.firstGoalStars)), null);
                else if (d.Chosen == 1)
                    encyclopedia.Open();
            });
        }
    }
}
