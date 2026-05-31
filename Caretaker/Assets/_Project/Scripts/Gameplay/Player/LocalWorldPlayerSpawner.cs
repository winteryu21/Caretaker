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
    /// This is the local gameplay avatar path. Network replication and host-authoritative spawn
    /// validation remain separate Sprint 2 work.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class LocalWorldPlayerSpawner : MonoBehaviour
    {
        [SerializeField] private SceneLoader _sceneLoader;
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private Vector3 _defaultSpawnPosition = new(0f, 1f, 0f);

        private GameObject _currentPlayer;

        /// <summary>Current local world player instance, if one has been spawned.</summary>
        public GameObject CurrentPlayer => _currentPlayer;

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
        }

        private void OnDisable()
        {
            if (_sceneLoader != null)
            {
                _sceneLoader.OnPhaseSceneLoaded -= HandlePhaseSceneLoaded;
            }
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

            if (_currentPlayer != null)
            {
                Destroy(_currentPlayer);
            }

            _currentPlayer = Instantiate(_playerPrefab, _defaultSpawnPosition, Quaternion.identity);
            _currentPlayer.name = $"LocalWorldPlayer_{timelineRole}_{phaseId}";
            MovePlayerToLoadedPhaseScene(_currentPlayer, sceneName);

            if (_currentPlayer.TryGetComponent(out RoomParticipant participant))
            {
                participant.SetPlayerId(GetLocalClientId());
            }

            return _currentPlayer;
        }

        private void HandlePhaseSceneLoaded(PhaseId phaseId, TimelineRole timelineRole, string sceneName)
        {
            SpawnLocalPlayer(phaseId, timelineRole, sceneName);
        }

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
        }

        private static ulong GetLocalClientId()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            return networkManager != null && networkManager.IsListening
                ? networkManager.LocalClientId
                : 0UL;
        }
    }
}
