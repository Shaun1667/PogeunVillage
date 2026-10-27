using NUnit.Framework;
using UnityEditor;

namespace PogeunVillage.Tests
{
    public class WorldSettingsTests
    {
        const string AssetPath = "Assets/PogeunVillage/Data/WorldSettings.asset";

        WorldSettings settings;

        [SetUp]
        public void LoadAsset()
        {
            settings = AssetDatabase.LoadAssetAtPath<WorldSettings>(AssetPath);
        }

        [Test]
        public void Asset_Exists()
        {
            Assert.IsNotNull(settings, $"{AssetPath} 가 없습니다.");
        }

        [Test]
        public void Values_AreConsistent()
        {
            Assert.IsNotNull(settings);
            Assert.Greater(settings.curveRadius, 0f);
            Assert.Greater(settings.walkSpeed, 0f);
            Assert.LessOrEqual(settings.walkSpeed, settings.runSpeed, "달리기가 걷기보다 느립니다.");
            Assert.LessOrEqual(settings.cameraDistanceRange.x, settings.cameraDistanceRange.y);
            Assert.That(settings.cameraDistance,
                Is.InRange(settings.cameraDistanceRange.x, settings.cameraDistanceRange.y),
                "카메라 기본 거리가 줌 범위 밖입니다.");
        }
    }
}
