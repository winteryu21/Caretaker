using Caretaker.Shared;

namespace Caretaker.Core
{
    public readonly struct PlayerSessionData
    {
        public PlayerSessionData(ulong clientId, TimelineRole timelineRole, bool isReady)
        {
            ClientId = clientId;
            TimelineRole = timelineRole;
            IsReady = isReady;
        }

        public ulong ClientId { get; }
        public TimelineRole TimelineRole { get; }
        public bool IsReady { get; }

        public PlayerSessionData WithReady(bool isReady)
        {
            return new PlayerSessionData(ClientId, TimelineRole, isReady);
        }

        public PlayerSessionData WithTimelineRole(TimelineRole timelineRole, bool isReady = false)
        {
            return new PlayerSessionData(ClientId, timelineRole, isReady);
        }
    }
}
