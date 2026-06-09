using Caretaker.Shared;
using NUnit.Framework;

namespace Caretaker.Tests.Editor
{
    public sealed class MajorIdUtilityTests
    {
        [TestCase("M1", MajorId.M1)]
        [TestCase("M2", MajorId.M2)]
        [TestCase("M3", MajorId.M3)]
        [TestCase("M4", MajorId.M4)]
        [TestCase("CR_P1_POWER_PANEL", MajorId.M1)]
        [TestCase("CR_P1_SEC_HACK", MajorId.M2)]
        [TestCase("CR_P2_BLUEPRINT_ID", MajorId.M3)]
        [TestCase("CR_P2_VIRUS_PLANT", MajorId.M4)]
        public void TryResolve_ReturnsMajorIdForKnownIds(string value, MajorId expectedMajorId)
        {
            bool resolved = MajorIdUtility.TryResolve(value, out MajorId majorId);

            Assert.That(resolved, Is.True);
            Assert.That(majorId, Is.EqualTo(expectedMajorId));
        }

        [Test]
        public void ToStableId_ReturnsStableStringForMajorId()
        {
            Assert.That(MajorId.M2.ToStableId(), Is.EqualTo("M2"));
        }
    }
}
