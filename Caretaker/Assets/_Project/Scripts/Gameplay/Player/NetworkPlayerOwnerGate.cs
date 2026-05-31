using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// Enables local player control only on the owning Netcode client.
    /// </summary>
    /// <remarks>DSD §3.2, §3.3 - networked player ownership boundary.</remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(InteractionProbe))]
    public sealed class NetworkPlayerOwnerGate : NetworkBehaviour
    {
        private InteractionProbe _interactionProbe;
        private PlayerController _playerController;
        private PlayerInput _playerInput;
        private PlayerInputReader _playerInputReader;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _playerInputReader = GetComponent<PlayerInputReader>();
            _interactionProbe = GetComponent<InteractionProbe>();
            _playerInput = GetComponent<PlayerInput>();
        }

        public override void OnNetworkSpawn()
        {
            ApplyOwnershipControl(IsOwner);
        }

        public override void OnGainedOwnership()
        {
            ApplyOwnershipControl(true);
        }

        public override void OnLostOwnership()
        {
            ApplyOwnershipControl(false);
        }

        public override void OnNetworkDespawn()
        {
            ApplyOwnershipControl(false);
        }

        private void ApplyOwnershipControl(bool isLocalOwner)
        {
            if (isLocalOwner)
            {
                SetEnabled(_playerInput, true);
                SetEnabled(_playerInputReader, true);
                SetEnabled(_interactionProbe, true);
                SetEnabled(_playerController, true);
                return;
            }

            SetEnabled(_playerController, false);
            SetEnabled(_interactionProbe, false);
            SetEnabled(_playerInputReader, false);
            SetEnabled(_playerInput, false);
        }

        private static void SetEnabled(Behaviour behaviour, bool isEnabled)
        {
            if (behaviour != null)
            {
                behaviour.enabled = isEnabled;
            }
        }
    }
}
