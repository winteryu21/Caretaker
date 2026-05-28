using UnityEngine;

using Unity.Netcode;

namespace Caretaker.World
{
    /// <summary>
    /// Identifies a player or actor that can enter RoomVolume triggers.
    /// </summary>
    [DisallowMultipleComponent]
    public class RoomParticipant : MonoBehaviour
    {
        [SerializeField] private ulong _playerId;

        private NetworkObject _networkObject;

        /// <summary>Gets the player ID reported to the room system.</summary>
        public ulong PlayerId => _networkObject != null ? _networkObject.OwnerClientId : _playerId;

        private void Awake()
        {
            _networkObject = GetComponentInParent<NetworkObject>();
        }

        /// <summary>
        /// Sets the fallback player ID used when no NetworkObject is available.
        /// </summary>
        /// <param name="playerId">Player ID to report to RoomManager.</param>
        public void SetPlayerId(ulong playerId)
        {
            _playerId = playerId;
        }
    }
}
