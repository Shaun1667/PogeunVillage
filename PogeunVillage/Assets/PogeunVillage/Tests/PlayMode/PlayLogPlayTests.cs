using NUnit.Framework;
using UnityEngine;

namespace PogeunVillage.PlayModeTests
{
    /// <summary>4-1 플레이 기록: 씬에 연결돼 있고, 테스트에서는 꺼져 파일을 만들지 않음.</summary>
    public class PlayLogPlayTests : VillageSceneTest
    {
        [Test]
        public void Logger_Wired_DisabledInTests()
        {
            var logger = Object.FindAnyObjectByType<PlayLogger>();
            Assert.IsNotNull(logger);
            Assert.IsTrue(logger.dayCycle && logger.wallet && logger.inventory && logger.village && logger.rater && logger.furniture && logger.wood);
            Assert.IsFalse(logger.enabled, "테스트에서는 끔");
            Assert.IsNull(logger.FilePath, "파일을 만들지 않음");
            Assert.AreEqual(30f, logger.minSessionSeconds);
        }
    }
}
