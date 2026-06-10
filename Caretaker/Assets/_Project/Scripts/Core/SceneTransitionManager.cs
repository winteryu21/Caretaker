using System;
using System.Collections;

using Caretaker.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Caretaker.Core
{
    /// <summary>
    /// 일반 월드 씬 이동을 처리한다.
    /// Phase 진행 전환은 <see cref="GameFlowManager"/>가 담당하고, 이 컴포넌트는 명시된 씬과 스폰 지점으로
    /// 현재 플레이어를 옮기는 포털성 이동만 담당한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneTransitionManager : MonoBehaviour
    {
        private const string DEFAULT_SPAWN_POINT_NAME = "SpawnPoint";

        [SerializeField] private LocalWorldPlayerSpawner _playerSpawner;
        [SerializeField] private string _defaultSpawnPointName = DEFAULT_SPAWN_POINT_NAME;
        [SerializeField] private Vector3 _fallbackSpawnPosition = new(0f, 1f, 0f);

        private bool _isTransitioning;

        /// <summary>비동기 씬 이동이 진행 중인지 반환한다.</summary>
        public bool IsTransitioning => _isTransitioning;

        /// <summary>
        /// 로컬 플레이어를 지정한 씬으로 이동한다.
        /// </summary>
        /// <param name="targetSceneName">이동할 Unity 씬 이름.</param>
        /// <param name="spawnPointName">목표 씬에서 찾을 스폰 Transform 이름. 비어 있으면 기본값을 사용한다.</param>
        /// <param name="unloadActorScene">이동 전 플레이어가 속했던 씬을 언로드할지 여부.</param>
        /// <returns>이동 요청이 접수되었으면 true.</returns>
        public bool TryTransitionLocalPlayer(
            string targetSceneName,
            string spawnPointName = null,
            bool unloadActorScene = false)
        {
            ResolveDependencies();

            GameObject currentPlayer = _playerSpawner != null ? _playerSpawner.CurrentPlayer : null;
            if (currentPlayer == null || !currentPlayer.TryGetComponent(out PlayerController actor))
            {
                Debug.LogWarning("Scene transition failed: local player is missing.", this);
                return false;
            }

            return TryTransitionActor(actor, targetSceneName, spawnPointName, unloadActorScene);
        }

        /// <summary>
        /// 지정한 플레이어를 목표 씬으로 이동한다.
        /// </summary>
        /// <param name="actor">이동할 플레이어.</param>
        /// <param name="targetSceneName">이동할 Unity 씬 이름.</param>
        /// <param name="spawnPointName">목표 씬에서 찾을 스폰 Transform 이름. 비어 있으면 기본값을 사용한다.</param>
        /// <param name="unloadActorScene">이동 전 플레이어가 속했던 씬을 언로드할지 여부.</param>
        /// <returns>이동 요청이 접수되었으면 true.</returns>
        public bool TryTransitionActor(
            PlayerController actor,
            string targetSceneName,
            string spawnPointName = null,
            bool unloadActorScene = false)
        {
            if (actor == null)
            {
                Debug.LogWarning("Scene transition failed: actor is missing.", this);
                return false;
            }

            string resolvedSceneName = targetSceneName?.Trim();
            if (string.IsNullOrWhiteSpace(resolvedSceneName))
            {
                Debug.LogWarning("Scene transition failed: target scene name is missing.", this);
                return false;
            }

            if (_isTransitioning)
            {
                Debug.LogWarning($"Scene transition is already running. target={resolvedSceneName}", this);
                return false;
            }

            string resolvedSpawnPointName = ResolveSpawnPointName(spawnPointName);
            Scene actorScene = actor.gameObject.scene;
            Scene targetScene = SceneManager.GetSceneByName(resolvedSceneName);
            if (targetScene.IsValid() && targetScene.isLoaded)
            {
                MoveActorToLoadedScene(actor, targetScene, resolvedSpawnPointName);
                TryUnloadActorScene(actorScene, targetScene, unloadActorScene);
                return true;
            }

            StartCoroutine(LoadAndMoveActorRoutine(
                actor,
                resolvedSceneName,
                resolvedSpawnPointName,
                actorScene,
                unloadActorScene));
            return true;
        }

        private IEnumerator LoadAndMoveActorRoutine(
            PlayerController actor,
            string targetSceneName,
            string spawnPointName,
            Scene actorScene,
            bool unloadActorScene)
        {
            _isTransitioning = true;

            AsyncOperation loadOperation;
            try
            {
                loadOperation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"Scene transition failed to start loading '{targetSceneName}': {exception.Message}", this);
                _isTransitioning = false;
                yield break;
            }

            if (loadOperation == null)
            {
                Debug.LogError($"Scene transition failed to start loading '{targetSceneName}'.", this);
                _isTransitioning = false;
                yield break;
            }

            yield return loadOperation;

            if (actor == null)
            {
                Debug.LogWarning($"Scene transition cancelled because actor was destroyed. target={targetSceneName}", this);
                _isTransitioning = false;
                yield break;
            }

            Scene targetScene = SceneManager.GetSceneByName(targetSceneName);
            if (!targetScene.IsValid() || !targetScene.isLoaded)
            {
                Debug.LogError($"Scene transition failed: loaded scene is invalid. target={targetSceneName}", this);
                _isTransitioning = false;
                yield break;
            }

            MoveActorToLoadedScene(actor, targetScene, spawnPointName);
            TryUnloadActorScene(actorScene, targetScene, unloadActorScene);
            _isTransitioning = false;
        }

        private void MoveActorToLoadedScene(PlayerController actor, Scene targetScene, string spawnPointName)
        {
            Pose targetPose = ResolveSpawnPose(targetScene, spawnPointName);
            SceneManager.MoveGameObjectToScene(actor.gameObject, targetScene);

            if (actor.TryGetComponent(out Rigidbody2D rigidbody2D))
            {
                rigidbody2D.linearVelocity = Vector2.zero;
                rigidbody2D.angularVelocity = 0f;
                rigidbody2D.position = targetPose.position;
                rigidbody2D.rotation = targetPose.rotation.eulerAngles.z;
            }

            actor.transform.SetPositionAndRotation(targetPose.position, targetPose.rotation);
            Physics2D.SyncTransforms();
        }

        private void TryUnloadActorScene(Scene actorScene, Scene targetScene, bool unloadActorScene)
        {
            if (!unloadActorScene)
            {
                return;
            }

            if (!actorScene.IsValid() || !actorScene.isLoaded || actorScene == targetScene)
            {
                return;
            }

            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(actorScene);
            if (unloadOperation == null)
            {
                Debug.LogWarning($"Scene transition could not unload previous scene: {actorScene.name}", this);
            }
        }

        private Pose ResolveSpawnPose(Scene targetScene, string spawnPointName)
        {
            return TryFindSpawnPoint(targetScene, spawnPointName, out Transform spawnPoint)
                ? new Pose(spawnPoint.position, spawnPoint.rotation)
                : new Pose(_fallbackSpawnPosition, Quaternion.identity);
        }

        private static bool TryFindSpawnPoint(Scene targetScene, string spawnPointName, out Transform spawnPoint)
        {
            spawnPoint = null;
            if (!targetScene.IsValid() || !targetScene.isLoaded || string.IsNullOrWhiteSpace(spawnPointName))
            {
                return false;
            }

            GameObject[] rootObjects = targetScene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                Transform[] transforms = rootObjects[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j].name == spawnPointName)
                    {
                        spawnPoint = transforms[j];
                        return true;
                    }
                }
            }

            return false;
        }

        private string ResolveSpawnPointName(string spawnPointName)
        {
            if (!string.IsNullOrWhiteSpace(spawnPointName))
            {
                return spawnPointName.Trim();
            }

            return string.IsNullOrWhiteSpace(_defaultSpawnPointName)
                ? DEFAULT_SPAWN_POINT_NAME
                : _defaultSpawnPointName.Trim();
        }

        private void ResolveDependencies()
        {
            if (_playerSpawner == null)
            {
                _playerSpawner = FindAnyObjectByType<LocalWorldPlayerSpawner>();
            }
        }
    }
}
