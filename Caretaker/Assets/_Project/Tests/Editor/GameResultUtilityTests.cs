using Caretaker.Shared;
using NUnit.Framework;

namespace Caretaker.Tests.Editor
{
    public sealed class GameResultUtilityTests
    {
        [Test]
        public void ToStableId_ReturnsEnumNameForResult()
        {
            Assert.That(GameResult.EscapeSuccess.ToStableId(), Is.EqualTo("EscapeSuccess"));
        }

        [Test]
        public void ToStableId_ReturnsEmptyStringForNone()
        {
            Assert.That(GameResult.None.ToStableId(), Is.Empty);
        }
    }
}
