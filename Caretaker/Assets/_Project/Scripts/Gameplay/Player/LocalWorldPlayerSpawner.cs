using System;

using System.Collections.Generic;

using Caretaker.Core;
using Caretaker.Shared;
using Caretaker.World;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// Spawns the local world avatar after the local timeline phase scene is loaded.
    /// </summary>
    /// <remarks>
    /// In a network session, the server spawns owner-observed network players.
    /// Outside a network session, this preserves the local-only editor/testing path.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class LocalWorldPlayerSpawner : MonoBehaviour
    {
        private const string DEFAULT_SPAWN_POINT_NAME = "SceneStartPoint";

        [SerializeField] private SceneLoader _sceneLoader;
        [SerializeField] private SessionRoleManager _roleManager;
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private Vector3 _defaultSpawnPosition = new(0f, 1f, 0f);
        [SerializeField] private string _spawnPointName = DEFAULT_SPAWN_POINT_NAME;

        private readonly Dictionary<ulong, NetworkObject> _spawnedNetworkPlayersByClientId = new();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly Dictionary<TimelineRole, GameObject> _debugPhase3PlayersByRole = new();
        private bool _spawnDebugPhase3Players;
#endif

        private GameObject _currentPlayer;
        private string _loadedPhaseSceneName;

        /// <summary>Current local world player instance, if one has been spawned.</summary>
        public GameObject CurrentPlayer => _currentPlayer;

        /// <summary>
        /// 로컬 월드 플레이어가 생성되거나 교체될 때 발생한다.
        /// </summary>
        public static event Action<GameObject> OnCurrentPlayerChanged;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void OnEnable()
        {
            ResolveDependencies();

            if (_sceneLoader != null)
            {
                _sceneLoader.OnPhaseSceneLoaded += HandlePhaseSceneLoaded;
            }

            NetworkPlayerOwnerGate.OnLocalOwnerPlayerSpawned += HandleLocalOwnerPlayerSpawned;
            NetworkPlayerOwnerGate.OnLocalOwnerPlayerDespawned += HandleLocalOwnerPlayerDespawned;
            RegisterNetworkCallbacks();
        }

        private void OnDisable()
        {
            if (_sceneLoader != null)
            {
                _sceneLoader.OnPhaseSceneLoaded -= HandlePhaseSceneLoaded;
            }

            NetworkPlayerOwnerGate.OnLocalOwnerPlayerSpawned -= HandleLocalOwnerPlayerSpawned;
            NetworkPlayerOwnerGate.OnLocalOwnerPlayerDespawned -= HandleLocalOwnerPlayerDespawned;
            UnregisterNetworkCallbacks();
        }

        /// <summary>
        /// Creates or replaces the local world player for the loaded phase scene.
        /// </summary>
        /// <param name="phaseId">Loaded phase.</param>
        /// <param name="timelineRole">Local timeline role.</param>
        /// <param name="sceneName">Loaded phase scene name.</param>
        /// <returns>The spawned local world player, or null if no prefab is configured.</returns>
        public GameObject SpawnLocalPlayer(PhaseId phaseId, TimelineRole timelineRole, string sceneName)
        {
            if (_playerPrefab == null)
            {
                Debug.LogWarning("LocalWorldPlayerSpawner requires a player prefab.", this);
                return null;
            }

            _loadedPhaseSceneName = sceneName;

            if (CanSpawnNetworkPlayers())
            {
                SpawnNetworkPlayersForRegisteredClients(phaseId, sceneName);
                return _currentPlayer;
            }

            if (IsNetworkClientWaitingForServerSpawn())
            {
                MovePlayerToLoadedPhaseScene(_currentPlayer, sceneName);
                MovePlayerToSpawnPoint(_currentPlayer, sceneName);
                return _currentPlayer;
            }

            if (_currentPlayer != null)
            {
                Destroy(_currentPlayer);
            }

            Pose spawnPose = ResolveSpawnPose(sceneName);
            _currentPlayer = Instantiate(_playerPrefab, spawnPose.position, spawnPose.rotation);
            _currentPlayer.name = $"LocalWorldPlayer_{timelineRole}_{phaseId}";
            MovePlayerToLoadedPhaseScene(_currentPlayer, sceneName);

            if (_currentPlayer.TryGetComponent(out RoomParticipant participant))
            {
                participant.SetPlayerId(GetLocalClientId());
            }

            if (_currentPlayer.TryGetComponent(out InventoryController inventoryController))
            {
                inventoryController.SetPlayerId(GetLocalClientId());
            }

            OnCurrentPlayerChanged?.Invoke(_currentPlayer);

            return _currentPlayer;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// 네트워크 세션 없이 Phase 3 스킵을 검증할 때 Past/Future 플레이어를 모두 생성하도록 허용합니다.
        /// </summary>
        public void EnablePhase3DebugSandbox()
        {
            _spawnDebugPhase3Players = true;
        }

        /// <summary>
        /// Phase 3 디버그 플레이어 생성 상태를 해제하고 남은 디버그 플레이어를 정리합니다.
        /// </summary>
        public void DisablePhase3DebugSandbox()
        {
            _spawnDebugPhase3Players = false;

            foreach (GameObject player in _debugPhase3PlayersByRole.Values)
            {
                if (player != null)
                {
                    Destroy(player);
                }
            }

            _debugPhase3PlayersByRole.Clear();
        }
#endif

        private void HandlePhaseSceneLoaded(PhaseId phaseId, TimelineRole timelineRole, string sceneName)
        {
            if (phaseId == PhaseId.Phase3)
            {
                HandlePhase3SceneLoaded(timelineRole, sceneName);
                return;
            }

            SpawnLocalPlayer(phaseId, timelineRole, sceneName);
        }

        private void HandlePhase3SceneLoaded(TimelineRole timelineRole, string sceneName)
        {
            ResolveDependencies();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ShouldSpawnDebugPhase3Players())
            {
                SpawnDebugPhase3Player(timelineRole, sceneName);
                return;
            }
#endif

            if (CanSpawnNetworkPlayers())
            {
                if (_roleManager == null)
                {
                    return;
                }

                foreach (PlayerSessionData player in _roleManager.Players.Values)
                {
                    if (player.TimelineRole != timelineRole)
                    {
                        continue;
                    }

                    if (player.ClientId == NetworkManager.Singleton.LocalClientId)
                    {
                        _loadedPhaseSceneName = sceneName;
                    }

                    SpawnNetworkPlayerForClient(player.ClientId, player.TimelineRole, PhaseId.Phase3, sceneName);
                }

                return;
            }

            TimelineRole localRole = _roleManager != null ? _roleManager.LocalTimelineRole : timelineRole;
            if (localRole == timelineRole)
            {
                SpawnLocalPlayer(PhaseId.Phase3, timelineRole, sceneName);
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool ShouldSpawnDebugPhase3Players()
        {
            return _spawnDebugPhase3Players && !IsNetworkSessionActive();
        }

        private void SpawnDebugPhase3Player(TimelineRole timelineRole, string sceneName)
        {
            if (_playerPrefab == null)
            {
                Debug.LogWarning("LocalWorldPlayerSpawner requires a player prefab for Phase 3 debug sandbox.", this);
                return;
            }

            if (timelineRole is not (TimelineRole.Past or TimelineRole.Future))
            {
                return;
            }

            if (_debugPhase3PlayersByRole.TryGetValue(timelineRole, out GameObject existingPlayer)
                && existingPlayer != null)
            {
                Destroy(existingPlayer);
            }

            Pose spawnPose = ResolveSpawnPose(sceneName);
            GameObject player = Instantiate(_playerPrefab, spawnPose.position, spawnPose.rotation);
            player.name = $"DebugPhase3Player_{timelineRole}";
            MovePlayerToLoadedPhaseScene(player, sceneName);
            _debugPhase3PlayersByRole[timelineRole] = player;

            ulong debugPlayerId = timelineRole == TimelineRole.Past ? 0UL : 1UL;
            if (player.TryGetComponent(out RoomParticipant participant))
            {
                participant.SetPlayerId(debugPlayerId);
            }

            if (player.TryGetComponent(out InventoryController inventoryController))
            {
                inventoryController.SetPlayerId(debugPlayerId);
            }

            bool enableLocalControl = timelineRole == Phase3DebugBootstrap.OfflineSandboxLocalRole;
            if (enableLocalControl)
            {
                _loadedPhaseSceneName = sceneName;
            }

            if (player.TryGetComponent(out NetworkPlayerOwnerGate ownerGate))
            {
                ownerGate.ConfigureLocalDebugInstance(timelineRole, enableLocalControl);
            }
            else if (enableLocalControl)
            {
                _currentPlayer = player;
                OnCurrentPlayerChanged?.Invoke(_currentPlayer);
            }
        }
#endif

        private static void MovePlayerToLoadedPhaseScene(GameObject player, string sceneName)
        {
            if (player == null || string.IsNullOrWhiteSpace(sceneName))
            {
                return;
            }

            Scene phaseScene = SceneManager.GetSceneByName(sceneName);
            if (phaseScene.IsValid() && phaseScene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(player, phaseScene);
            }
        }

        private void ResolveDependencies()
        {
            if (_sceneLoader == null)
            {
                _sceneLoader = FindAnyObjectByType<SceneLoader>();
            }

            if (_roleManager == null)
            {
                _roleManager = FindAnyObjectByType<SessionRoleManager>();
            }
        }

        private static ulong GetLocalClientId()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            return networkManager != null && networkManager.IsListening
                ? networkManager.LocalClientId
                : 0UL;
        }

        private bool CanSpawnNetworkPlayers()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            return networkManager != null && networkManager.IsListening && networkManager.IsServer;
        }

        private static bool IsNetworkClientWaitingForServerSpawn()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            return networkManager != null && networkManager.IsListening && !networkManager.IsServer;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static bool IsNetworkSessionActive()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            return networkManager != null && networkManager.IsListening;
        }
#endif

        private void SpawnNetworkPlayersForRegisteredClients(PhaseId phaseId, string sceneName)
        {
            ResolveDependencies();

            if (_roleManager == null)
            {
                Debug.LogWarning("LocalWorldPlayerSpawner requires SessionRoleManager to spawn network players.", this);
                return;
            }

            foreach (PlayerSessionData player in _roleManager.Players.Values)
            {
                SpawnNetworkPlayerForClient(player.ClientId, player.TimelineRole, phaseId, sceneName);
            }
        }

        private void SpawnNetworkPlayerForClient(
            ulong clientId,
            TimelineRole timelineRole,
            PhaseId phaseId,
            string sceneName)
        {
            DespawnNetworkPlayer(clientId);

            Pose spawnPose = ResolveSpawnPose(sceneName);
            GameObject player = Instantiate(_playerPrefab, spawnPose.position, spawnPose.rotation);
            player.name = $"NetworkPlayer_{timelineRole}_{phaseId}_{clientId}";
            MovePlayerToLoadedPhaseScene(player, sceneName);

            if (player.TryGetComponent(out RoomParticipant participant))
            {
                participant.SetPlayerId(clientId);
            }

            if (player.TryGetComponent(out InventoryController inventoryController))
            {
                inventoryController.SetPlayerId(clientId);
            }

            if (!player.TryGetComponent(out NetworkObject networkObject))
            {
                Debug.LogError("Network player prefab requires NetworkObject.", player);
                Destroy(player);
                return;
            }

            if (player.TryGetComponent(out NetworkPlayerOwnerGate ownerGate))
            {
                ownerGate.ConfigureVisibility(
                    clientId,
                    timelineRole,
                    phaseId == PhaseId.Phase3);
            }

            networkObject.SpawnWithOwnership(clientId, true);
            _spawnedNetworkPlayersByClientId[clientId] = networkObject;
        }

        private void HandleLocalOwnerPlayerSpawned(GameObject player)
        {
            _currentPlayer = player;
            MovePlayerToLoadedPhaseScene(_currentPlayer, _loadedPhaseSceneName);
            MovePlayerToSpawnPoint(_currentPlayer, _loadedPhaseSceneName);
            OnCurrentPlayerChanged?.Invoke(_currentPlayer);
        }

        private void HandleLocalOwnerPlayerDespawned(GameObject player)
        {
            if (_currentPlayer == player)
            {
                _currentPlayer = null;
                OnCurrentPlayerChanged?.Invoke(null);
            }
        }

        private void RegisterNetworkCallbacks()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null)
            {
                networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
                networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            }
        }

        private void UnregisterNetworkCallbacks()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null)
            {
                networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                DespawnNetworkPlayer(clientId);
            }
        }

        private void DespawnNetworkPlayer(ulong clientId)
        {
            if (!_spawnedNetworkPlayersByClientId.Remove(clientId, out NetworkObject networkObject))
            {
                return;
            }

            if (networkObject == null)
            {
                return;
            }

            if (networkObject.IsSpawned)
            {
                networkObject.Despawn(true);
                return;
            }

            Destroy(networkObject.gameObject);
        }

        private Pose ResolveSpawnPose(string sceneName)
        {
            return TryFindSpawnPoint(sceneName, out Transform spawnPoint)
                ? new Pose(spawnPoint.position, spawnPoint.rotation)
                : new Pose(_defaultSpawnPosition, Quaternion.identity);
        }

        private void MovePlayerToSpawnPoint(GameObject player, string sceneName)
        {
            if (player == null || !TryFindSpawnPoint(sceneName, out Transform spawnPoint))
            {
                return;
            }

            player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        }

        private bool TryFindSpawnPoint(string sceneName, out Transform spawnPoint)
        {
            spawnPoint = null;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            Scene phaseScene = SceneManager.GetSceneByName(sceneName);
            if (!phaseScene.IsValid() || !phaseScene.isLoaded)
            {
                return false;
            }

            string targetName = string.IsNullOrWhiteSpace(_spawnPointName) ? DEFAULT_SPAWN_POINT_NAME : _spawnPointName;
            GameObject[] rootObjects = phaseScene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                Transform[] transforms = rootObjects[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j].name == targetName)
                    {
                        spawnPoint = transforms[j];
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
