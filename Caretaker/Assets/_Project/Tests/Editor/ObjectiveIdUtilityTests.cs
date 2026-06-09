using Caretaker.Shared;
using NUnit.Framework;

namespace Caretaker.Tests.Editor
{
    public sealed class ObjectiveIdUtilityTests
    {
        [Test]
        public void ToStableId_ReturnsEnumNameForObjectiveId()
        {
            Assert.That(
                ObjectiveId.P1_Past_RestoreFuturePower.ToStableId(),
                Is.EqualTo("P1_Past_RestoreFuturePower"));
        }

        [Test]
        public void ToStableId_ReturnsEmptyStringForNone()
        {
            Assert.That(ObjectiveId.None.ToStableId(), Is.Empty);
        }
    }
}
