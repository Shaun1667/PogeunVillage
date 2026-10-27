using NUnit.Framework;

namespace PogeunVillage.Tests
{
    // 0-3 샘플 테스트: 테스트 어셈블리가 런타임 어셈블리를 참조하는지 확인한다.
    public class AssemblySetupTests
    {
        [Test]
        public void RuntimeAssembly_IsReferenced()
        {
            Assert.AreEqual("PogeunVillage", typeof(CurvedWorld).Assembly.GetName().Name);
        }
    }
}
