using NUnit.Framework;
using UnityEngine;

namespace PogeunVillage.Tests
{
    // 3-7a 곤충: 놀라서 도망 · 시간대 (BALANCE 18번)
    public class InsectTests
    {
        const float Run = 3f, RunSensitive = 5f, TooClose = 0.8f;

        static bool Flee(float distance, bool running, bool sensitive) =>
            InsectLogic.ShouldFlee(distance, running, sensitive, Run, RunSensitive, TooClose);

        [Test]
        public void Walking_NeverStartlesNormalInsect()  // 18-1
        {
            Assert.IsFalse(Flee(0.3f, false, false));
            Assert.IsFalse(Flee(1.2f, false, false));
        }

        [Test]
        public void Running_WithinDistance_Flees()  // 18-2
        {
            Assert.IsTrue(Flee(2.9f, true, false));
            Assert.IsFalse(Flee(3.1f, true, false));
            Assert.IsTrue(Flee(4.9f, true, true), "예민하면 더 멀리서");
            Assert.IsFalse(Flee(5.1f, true, true));
        }

        [Test]
        public void Sensitive_TooClose_FleesEvenWalking()  // 18-3 (결정 2026-10-29: 0.8m)
        {
            Assert.IsTrue(Flee(0.7f, false, true));
            Assert.IsFalse(Flee(0.9f, false, true), "0.8 ~ 1.3m에서는 잡을 수 있음");
        }

        [Test]
        public void EncyclopediaLine()  // 3-7b
        {
            Assert.AreEqual("메뚜기 · 풀밭 · 낮/저녁 · 2마리", HudText.InsectEntry("메뚜기", InsectSite.Grass, new[] { DayPeriod.Daytime, DayPeriod.Evening }, 2));
            Assert.AreEqual("꽃 위", HudText.InsectSiteName(InsectSite.Flower));
            Assert.AreEqual("물가", HudText.InsectSiteName(InsectSite.Water));
        }

        [Test]
        public void Noon_NightInsectsDontAppear()  // 18-5
        {
            DayPeriod[][] periods =
            {
                new[] { DayPeriod.Morning, DayPeriod.Daytime }, new[] { DayPeriod.Daytime }, new[] { DayPeriod.Daytime, DayPeriod.Evening },
                new[] { DayPeriod.Evening, DayPeriod.Night }, new[] { DayPeriod.Morning, DayPeriod.Daytime, DayPeriod.Evening },
                new[] { DayPeriod.Daytime }, new[] { DayPeriod.Night }, new[] { DayPeriod.Night },
            };
            var rarities = new[] { Rarity.Common, Rarity.Common, Rarity.Common, Rarity.Common, Rarity.Uncommon, Rarity.Uncommon, Rarity.Rare, Rarity.Rare };
            var available = new bool[8];
            for (int i = 0; i < 8; i++)
            {
                var d = ScriptableObject.CreateInstance<InsectDefinition>();
                d.periods = periods[i];
                available[i] = d.AppearsIn(DayPeriod.Daytime);
                Object.DestroyImmediate(d);
            }
            CollectionAssert.AreEqual(new[] { true, true, true, false, true, true, false, false }, available, "귀뚜라미 · 반딧불이 · 사슴벌레 없음");
            var random = new SystemRandomSource(5);
            for (int i = 0; i < 300; i++)
                Assert.IsTrue(available[InsectLogic.PickInsect(rarities, available, new[] { 60, 30, 10 }, random)]);
        }
    }
}
