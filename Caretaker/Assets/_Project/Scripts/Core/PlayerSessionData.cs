using System;

using Caretaker.Shared;

namespace Caretaker.Core
{
    /// <summary>
    /// Stores the runtime lobby state for a connected player.
    /// </summary>
    [Serializable]
    public readonly struct PlayerSessionData
    {
        /// <summary>
        /// Creates player session data for a connected client.
        /// </summary>
        /// <param name="clientId">Netcode client identifier.</param>
        /// <param name="timelineRole">Assigned timeline role.</param>
        /// <param name="isReady">Whether the player is ready to start.</param>
        public PlayerSessionData(ulong clientId, TimelineRole timelineRole, bool isReady)
        {
            ClientId = clientId;
            TimelineRole = timelineRole;
            IsReady = isReady;
        }

        /// <summary>
        /// Gets the Netcode client identifier.
        /// </summary>
        public ulong ClientId { get; }

        /// <summary>
        /// Gets the assigned timeline role.
        /// </summary>
        public TimelineRole TimelineRole { get; }

        /// <summary>
        /// Gets whether the player is ready to start.
        /// </summary>
        public bool IsReady { get; }

        /// <summary>
        /// Creates a copy with an updated ready state.
        /// </summary>
        /// <param name="isReady">Updated ready state.</param>
        /// <returns>Updated player session data.</returns>
        public PlayerSessionData WithReady(bool isReady)
        {
            return new PlayerSessionData(ClientId, TimelineRole, isReady);
        }

        /// <summary>
        /// Creates a copy with an updated timeline role.
        /// </summary>
        /// <param name="timelineRole">Updated timeline role.</param>
        /// <returns>Updated player session data.</returns>
        public PlayerSessionData WithTimelineRole(TimelineRole timelineRole)
        {
            return new PlayerSessionData(ClientId, timelineRole, IsReady);
        }
    }
}
