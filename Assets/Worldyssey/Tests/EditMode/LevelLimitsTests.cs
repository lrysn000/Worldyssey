using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    /// <summary>
    /// M0 示例测试：除了验证世界数量限制，也用来确认测试程序集能正确引用 Core 程序集。
    /// </summary>
    public class LevelLimitsTests
    {
        [Test]
        public void MinWorldCount_IsNotGreaterThanMax()
        {
            Assert.LessOrEqual(LevelLimits.MinWorldCount, LevelLimits.MaxWorldCount);
        }

        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(4, true)]
        [TestCase(0, false)]
        [TestCase(5, false)]
        [TestCase(-1, false)]
        public void IsValidWorldCount_ReturnsExpected(int worldCount, bool expected)
        {
            Assert.AreEqual(expected, LevelLimits.IsValidWorldCount(worldCount));
        }
    }
}
