using System.Collections.Generic;

namespace PogeunVillage
{
    /// <summary>
    /// 곤충 규칙(엔진 상태 없음, 결정 2026-10-29 · 3-7a): 놀라서 도망가는지 · 지금 시간대에 나올 곤충 고르기(희귀도 가중치는 낚시와 같은 방식).
    /// 수치는 InsectRules(BALANCE.md 18번).
    /// </summary>
    public static class InsectLogic
    {
        /// <summary>
        /// 플레이어가 distance(m)에 있을 때 도망가는지: 달리면 놀라는 거리(예민하면 더 멀리) 안, 예민한 곤충은 걸어도 너무 가까이(tooClose 안)
        /// </summary>
        public static bool ShouldFlee(float distance, bool running, bool sensitive, float runDistance, float runDistanceSensitive, float tooCloseSensitive)
        {
            if (running && distance < (sensitive ? runDistanceSensitive : runDistance))
                return true;
            return sensitive && distance < tooCloseSensitive;
        }

        /// <returns>곤충 번호, 지금 나올 곤충이 없으면 -1</returns>
        public static int PickInsect(IReadOnlyList<Rarity> rarities, IReadOnlyList<bool> available, IReadOnlyList<int> weights, IRandomSource random) =>
            FishPicker.PickFish(rarities, available, weights, random);
    }
}
