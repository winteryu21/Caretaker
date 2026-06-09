using System;

using Caretaker.Shared;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// Netcode 소유 클라이언트에서만 로컬 플레이어 조작을 활성화한다.
    /// </summary>
    /// <remarks>DSD §3.2, §3.3 - 네트워크 플레이어 소유권 경계.</remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(InteractionProbe))]
    public sealed class NetworkPlayerOwnerGate : NetworkBehaviour
    {
        [SerializeField] private bool _restrictObserversToOwner = true;
        [SerializeField] private bool _hideNonOwnerPresentation = true;

        private readonly NetworkVariable<bool> _phase3RemoteVisible = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<TimelineRole> _timelineRole = new(
            TimelineRole.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private Collider2D[] _colliders;
        private InteractionProbe _interactionProbe;
        private PlayerController _playerController;
        private PlayerInput _playerInput;
        private PlayerInputReader _playerInputReader;
        private Renderer[] _renderers;
        private Rigidbody2D _rigidbody2D;
        private bool _hasConfiguredVisibilityOwner;
        private ulong _configuredVisibilityOwnerClientId;

        /// <summary>
        /// 이 클라이언트가 소유한 네트워크 플레이어가 생성될 때 발생한다.
        /// </summary>
        public static event Action<GameObject> OnLocalOwnerPlayerSpawned;

        /// <summary>
        /// 이 클라이언트가 소유한 네트워크 플레이어가 제거될 때 발생한다.
        /// </summary>
        public static event Action<GameObject> OnLocalOwnerPlayerDespawned;

        /// <summary>이 클라이언트가 관찰하는 네트워크 플레이어가 생성될 때 발생한다.</summary>
        public static event Action<NetworkPlayerOwnerGate> OnObservedPlayerSpawned;

        /// <summary>이 클라이언트가 관찰하는 네트워크 플레이어가 제거될 때 발생한다.</summary>
        public static event Action<NetworkPlayerOwnerGate> OnObservedPlayerDespawned;

        /// <summary>이 네트워크 플레이어가 속한 시간대 역할.</summary>
        public TimelineRole TimelineRole => _timelineRole.Value;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _playerInputReader = GetComponent<PlayerInputReader>();
            _interactionProbe = GetComponent<InteractionProbe>();
            _playerInput = GetComponent<PlayerInput>();
            _renderers = GetComponentsInChildren<Renderer>(true);
            _colliders = GetComponentsInChildren<Collider2D>(true);
            _rigidbody2D = GetComponent<Rigidbody2D>();

            NetworkObject.CheckObjectVisibility = ShouldShowToClient;
        }

        public override void OnNetworkSpawn()
        {
            ApplyOwnershipControl(IsOwner);
            OnObservedPlayerSpawned?.Invoke(this);

            if (IsOwner)
            {
                OnLocalOwnerPlayerSpawned?.Invoke(gameObject);
            }
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
            OnObservedPlayerDespawned?.Invoke(this);

            if (IsOwner)
            {
                OnLocalOwnerPlayerDespawned?.Invoke(gameObject);
            }

            ApplyOwnershipControl(false);
        }

        /// <summary>
        /// 네트워크 플레이어 생성 전에 소유자, 시간대 역할, 원격 관찰 허용 여부를 설정한다.
        /// </summary>
        /// <param name="ownerClientId">플레이어를 소유한 클라이언트 ID.</param>
        /// <param name="timelineRole">플레이어의 시간대 역할.</param>
        /// <param name="allowRemoteObservers">다른 클라이언트의 관찰 허용 여부.</param>
        public void ConfigureVisibility(
            ulong ownerClientId,
            TimelineRole timelineRole,
            bool allowRemoteObservers)
        {
            _configuredVisibilityOwnerClientId = ownerClientId;
            _hasConfiguredVisibilityOwner = true;
            _timelineRole.Value = timelineRole;
            _phase3RemoteVisible.Value = allowRemoteObservers;
            NetworkObject.CheckObjectVisibility = ShouldShowToClient;
        }

        private void ApplyOwnershipControl(bool isLocalOwner)
        {
            bool presentationEnabled = ShouldEnablePresentation(
                isLocalOwner,
                _phase3RemoteVisible.Value,
                _hideNonOwnerPresentation);
            bool localControlEnabled = ShouldEnableLocalControl(isLocalOwner);

            SetRendererVisibility(presentationEnabled);
            SetPhysicsEnabled(localControlEnabled);

            if (localControlEnabled)
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

        private bool ShouldShowToClient(ulong clientId)
        {
            ulong visibleOwnerClientId = _hasConfiguredVisibilityOwner
                ? _configuredVisibilityOwnerClientId
                : OwnerClientId;
            return ShouldObservePlayer(
                clientId,
                visibleOwnerClientId,
                _restrictObserversToOwner,
                _phase3RemoteVisible.Value);
        }

        /// <summary>클라이언트가 플레이어 NetworkObject를 관찰해야 하는지 반환한다.</summary>
        public static bool ShouldObservePlayer(
            ulong clientId,
            ulong ownerClientId,
            bool restrictObserversToOwner,
            bool phase3RemoteVisible)
        {
            return !restrictObserversToOwner
                || phase3RemoteVisible
                || clientId == ownerClientId;
        }

        /// <summary>클라이언트에서 플레이어 외형을 렌더링해야 하는지 반환한다.</summary>
        public static bool ShouldEnablePresentation(
            bool isLocalOwner,
            bool phase3RemoteVisible,
            bool hideNonOwnerPresentation)
        {
            return isLocalOwner
                || phase3RemoteVisible
                || !hideNonOwnerPresentation;
        }

        /// <summary>로컬 입력과 물리 시뮬레이션으로 플레이어를 제어해야 하는지 반환한다.</summary>
        public static bool ShouldEnableLocalControl(bool isLocalOwner)
        {
            return isLocalOwner;
        }

        private void SetRendererVisibility(bool isVisible)
        {
            if (_renderers != null)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    SetEnabled(_renderers[i], isVisible);
                }
            }
        }

        private void SetPhysicsEnabled(bool isEnabled)
        {
            if (_colliders != null)
            {
                for (int i = 0; i < _colliders.Length; i++)
                {
                    SetEnabled(_colliders[i], isEnabled);
                }
            }

            if (_rigidbody2D != null)
            {
                _rigidbody2D.simulated = isEnabled;
            }
        }

        private static void SetEnabled(Behaviour behaviour, bool isEnabled)
        {
            if (behaviour != null)
            {
                behaviour.enabled = isEnabled;
            }
        }

        private static void SetEnabled(Renderer renderer, bool isEnabled)
        {
            if (renderer != null)
            {
                renderer.enabled = isEnabled;
            }
        }
    }
}
