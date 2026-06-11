using System.Collections;

using Unity.Netcode;
using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// Spawns the Major 4 storage puzzle bridge once for the network session.
    /// </summary>
    [AddComponentMenu("Caretaker/Puzzle/Major 4 Storage Puzzle Bridge Spawner")]
    [DisallowMultipleComponent]
    public sealed class Major4StoragePuzzleBridgeSpawner : MonoBehaviour
    {
        private static readonly WaitForSeconds SPAWN_RETRY_INTERVAL = new(0.25f);

        [SerializeField] private Major4StoragePuzzleBridge _bridgePrefab;
        [SerializeField] private bool _dontDestroySpawnedBridge = true;
        [SerializeField] private bool _createLocalBridgeWhenOffline = true;

        private Coroutine _spawnRoutine;

        private void OnEnable()
        {
            _spawnRoutine = StartCoroutine(EnsureBridgeRoutine());
        }

        private void OnDisable()
        {
            if (_spawnRoutine == null)
            {
                return;
            }

            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }

        private IEnumerator EnsureBridgeRoutine()
        {
            while (Major4StoragePuzzleBridge.ActiveBridge == null)
            {
                TryCreateBridge();
                yield return SPAWN_RETRY_INTERVAL;
            }

            _spawnRoutine = null;
        }

        private void TryCreateBridge()
        {
            if (Major4StoragePuzzleBridge.ActiveBridge != null || _bridgePrefab == null)
            {
                return;
            }

            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening)
            {
                if (_createLocalBridgeWhenOffline)
                {
                    CreateLocalBridge();
                }

                return;
            }

            if (!networkManager.IsServer)
            {
                return;
            }

            Major4StoragePuzzleBridge bridge = Instantiate(_bridgePrefab);
            if (_dontDestroySpawnedBridge)
            {
                DontDestroyOnLoad(bridge.gameObject);
            }

            NetworkObject networkObject = bridge.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Debug.LogError("Major4 bridge prefab needs a NetworkObject component.", bridge);
                Destroy(bridge.gameObject);
                return;
            }

            networkObject.Spawn();
        }

        private void CreateLocalBridge()
        {
            Major4StoragePuzzleBridge bridge = Instantiate(_bridgePrefab);
            if (_dontDestroySpawnedBridge)
            {
                DontDestroyOnLoad(bridge.gameObject);
            }
        }
    }
}
