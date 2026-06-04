using System;

using Caretaker.Core;
using Caretaker.Shared;
using Caretaker.World;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 로컬 타임라인 Phase 씬이 로드된 뒤 로컬 월드 아바타를 생성한다.
    /// </summary>
    /// <remarks>
    /// 로컬 플레이용 아바타 생성 경로다. 네트워크 복제와 Host 권한 스폰 검증은
    /// Sprint 2의 별도 작업으로 남겨둔다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class LocalWorldPlayerSpawner : MonoBehaviour
    {
        [SerializeField] private SceneLoader _sceneLoader;
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private Vector3 _defaultSpawnPosition = new(0f, 1f, 0f);

        private GameObject _currentPlayer;

        /// <summary>생성된 로컬 월드 플레이어 인스턴스.</summary>
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
        }

        private void OnDisable()
        {
            if (_sceneLoader != null)
            {
                _sceneLoader.OnPhaseSceneLoaded -= HandlePhaseSceneLoaded;
            }
        }

        /// <summary>
        /// 로드된 Phase 씬의 로컬 월드 플레이어를 생성하거나 교체한다.
        /// </summary>
        /// <param name="phaseId">로드된 Phase.</param>
        /// <param name="timelineRole">로컬 타임라인 역할.</param>
        /// <param name="sceneName">로드된 Phase 씬 이름.</param>
        /// <returns>생성된 로컬 월드 플레이어. 프리팹이 없으면 null.</returns>
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

            OnCurrentPlayerChanged?.Invoke(_currentPlayer);

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
