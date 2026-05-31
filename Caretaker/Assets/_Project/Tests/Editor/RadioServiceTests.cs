using NUnit.Framework;

using Caretaker.Gameplay;

namespace Caretaker.Tests.Editor
{
    public class RadioServiceTests
    {
        private const ulong PLAYER_A = 1UL;
        private const ulong PLAYER_B = 2UL;

        [Test]
        public void RequestTalk_WhenIdle_GrantsTalkLock()
        {
            RadioService radioService = new();

            bool wasGranted = radioService.RequestTalk(PLAYER_A);

            Assert.That(wasGranted, Is.True);
            Assert.That(radioService.CurrentTalkerId, Is.EqualTo(PLAYER_A));
            Assert.That(radioService.IsIdle, Is.False);
        }

        [Test]
        public void RequestTalk_WhenSamePlayerAlreadyTalks_RemainsGranted()
        {
            RadioService radioService = new();

            radioService.RequestTalk(PLAYER_A);
            bool wasGranted = radioService.RequestTalk(PLAYER_A);

            Assert.That(wasGranted, Is.True);
            Assert.That(radioService.CurrentTalkerId, Is.EqualTo(PLAYER_A));
        }

        [Test]
        public void RequestTalk_WhenOtherPlayerTalks_DeniesRequest()
        {
            RadioService radioService = new();

            radioService.RequestTalk(PLAYER_A);
            bool wasGranted = radioService.RequestTalk(PLAYER_B);

            Assert.That(wasGranted, Is.False);
            Assert.That(radioService.CurrentTalkerId, Is.EqualTo(PLAYER_A));
        }

        [Test]
        public void ReleaseTalk_WhenOwnerReleases_ReturnsToIdle()
        {
            RadioService radioService = new();

            radioService.RequestTalk(PLAYER_A);
            bool wasReleased = radioService.ReleaseTalk(PLAYER_A);

            Assert.That(wasReleased, Is.True);
            Assert.That(radioService.CurrentTalkerId, Is.Null);
            Assert.That(radioService.IsIdle, Is.True);
        }

        [Test]
        public void ReleaseTalk_WhenNonOwnerReleases_IgnoresRequest()
        {
            RadioService radioService = new();

            radioService.RequestTalk(PLAYER_A);
            bool wasReleased = radioService.ReleaseTalk(PLAYER_B);

            Assert.That(wasReleased, Is.False);
            Assert.That(radioService.CurrentTalkerId, Is.EqualTo(PLAYER_A));
        }
    }
}
