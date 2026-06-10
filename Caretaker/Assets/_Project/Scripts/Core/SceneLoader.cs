using System;
using System.Collections;
using System.Collections.Generic;

using Caretaker.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Caretaker.Core
{
    /// <summary>
    /// 로컬 시간대에 필요한 Phase 씬을 로드한다.
    /// Phase 3에서는 스플릿뷰 렌더링을 위해 Past와 Future 씬을 모두 로드한다.
    /// </summary>
    public sealed class SceneLoader : MonoBehaviour
    {
        private const float PHASE3_FUTURE_WORLD_Y_OFFSET = 1000f;

        private readonly HashSet<string> _loadedPhaseSceneNames = new();
        private readonly HashSet<string> _offsetPhaseSceneNames = new();

        private bool _isTransitioning;
        private bool _hasPendingPhaseRequest;
        private bool _pendingForceReload;
        private PhaseId _pendingPhaseId;
        private TimelineRole _pendingTimelineRole;

        /// <summary>필요한 각 Phase 씬의 로드가 완료될 때 발생한다.</summary>
        public event Action<PhaseId, TimelineRole, string> OnPhaseSceneLoaded;
        public event Action<PhaseId> OnPhaseScenesUnloaded;

        /// <summary>Phase와 시간대 역할에 해당하는 빌드 씬 이름을 반환한다.</summary>
        public static string GetPhaseSceneName(PhaseId phaseId, TimelineRole timelineRole)
        {
            string phaseName = phaseId switch
            {
                PhaseId.Phase1 => "Phase1",
                PhaseId.Phase2 => "Phase2",
                PhaseId.Phase3 => "Phase3",
                _ => throw new ArgumentOutOfRangeException(nameof(phaseId), phaseId, "Unknown phase.")
            };

            string roleName = timelineRole switch
            {
                TimelineRole.Past => "Past",
                TimelineRole.Future => "Future",
                _ => throw new ArgumentOutOfRangeException(nameof(timelineRole), timelineRole, "Timeline role is required.")
            };

            return $"{phaseName}_{roleName}";
        }

        /// <summary>
        /// 요청한 Phase에 필요한 씬을 로드한다.
        /// </summary>
        /// <returns>로드 요청이 접수되었는지 여부.</returns>
        public bool TryLoadPhase(PhaseId phaseId, TimelineRole timelineRole)
        {
            if (_isTransitioning)
            {
                QueuePendingRequest(phaseId, timelineRole, false);
                Debug.Log(
                    $"Phase scene transition is already running. Queued next load: phase={phaseId}, role={timelineRole}",
                    this);
                return true;
            }

            if (timelineRole is not (TimelineRole.Past or TimelineRole.Future))
            {
                Debug.LogWarning("Cannot load a phase scene before a timeline role is assigned.", this);
                return false;
            }

            if (AreRequiredScenesLoaded(phaseId, timelineRole))
            {
                Debug.Log($"Required phase scenes already loaded: phase={phaseId}, role={timelineRole}", this);
                return true;
            }

            Debug.Log($"Starting phase scene load: phase={phaseId}, role={timelineRole}", this);
            StartCoroutine(LoadPhaseRoutine(phaseId, timelineRole, false));
            return true;
        }

        /// <summary>
        /// 현재 Phase 씬을 모두 내린 뒤 다시 로드한다.
        /// </summary>
        /// <returns>재시작 요청을 접수했는지 여부.</returns>
        public bool TryReloadPhase(PhaseId phaseId, TimelineRole timelineRole)
        {
            if (timelineRole is not (TimelineRole.Past or TimelineRole.Future))
            {
                Debug.LogWarning("Cannot reload a phase scene before a timeline role is assigned.", this);
                return false;
            }

            if (_isTransitioning)
            {
                QueuePendingRequest(phaseId, timelineRole, true);
                Debug.Log(
                    $"Phase scene transition is already running. Queued reload: phase={phaseId}, role={timelineRole}",
                    this);
                return true;
            }

            Debug.Log($"Starting phase scene reload: phase={phaseId}, role={timelineRole}", this);
            StartCoroutine(LoadPhaseRoutine(phaseId, timelineRole, true));
            return true;
        }

        /// <summary>현재 로드된 Phase 씬을 모두 언로드한다.</summary>
        /// <returns>언로드 요청을 시작했는지 여부.</returns>
        public bool TryUnloadPhase(PhaseId phaseId)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning($"Cannot unload phase scenes during another transition: phase={phaseId}", this);
                return false;
            }

            StartCoroutine(UnloadPhaseRoutine(phaseId));
            return true;
        }

        private IEnumerator UnloadPhaseRoutine(PhaseId phaseId)
        {
            _isTransitioning = true;

            string[] loadedSceneNames = new string[_loadedPhaseSceneNames.Count];
            _loadedPhaseSceneNames.CopyTo(loadedSceneNames);
            for (int i = 0; i < loadedSceneNames.Length; i++)
            {
                string sceneName = loadedSceneNames[i];
                Scene loadedScene = SceneManager.GetSceneByName(sceneName);
                if (loadedScene.isLoaded)
                {
                    Debug.Log($"Unloading phase scene for restart: {sceneName}", this);
                    AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(loadedScene);
                    if (unloadOperation != null)
                    {
                        yield return unloadOperation;
                    }
                }

                _loadedPhaseSceneNames.Remove(sceneName);
                _offsetPhaseSceneNames.Remove(sceneName);
            }

            _isTransitioning = false;
            OnPhaseScenesUnloaded?.Invoke(phaseId);
        }

        private IEnumerator LoadPhaseRoutine(PhaseId phaseId, TimelineRole timelineRole, bool forceReload)
        {
            _isTransitioning = true;

            string[] requiredSceneNames = GetRequiredSceneNames(phaseId, timelineRole);
            string[] loadedSceneNames = new string[_loadedPhaseSceneNames.Count];
            _loadedPhaseSceneNames.CopyTo(loadedSceneNames);

            for (int i = 0; i < loadedSceneNames.Length; i++)
            {
                string loadedSceneName = loadedSceneNames[i];
                if (!forceReload && Contains(requiredSceneNames, loadedSceneName))
                {
                    continue;
                }

                Scene loadedScene = SceneManager.GetSceneByName(loadedSceneName);
                if (loadedScene.isLoaded)
                {
                    Debug.Log($"Unloading previous phase scene: {loadedSceneName}", this);
                    AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(loadedScene);
                    if (unloadOperation != null)
                    {
                        yield return unloadOperation;
                    }
                }

                _loadedPhaseSceneNames.Remove(loadedSceneName);
                _offsetPhaseSceneNames.Remove(loadedSceneName);
            }

            for (int i = 0; i < requiredSceneNames.Length; i++)
            {
                string sceneName = requiredSceneNames[i];
                TimelineRole sceneRole = GetTimelineRole(sceneName);
                Scene targetScene = SceneManager.GetSceneByName(sceneName);
                if (!targetScene.isLoaded)
                {
                    Debug.Log($"Loading phase scene additive: {sceneName}", this);
                    AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                    if (loadOperation == null)
                    {
                        Debug.LogError($"Failed to start loading phase scene '{sceneName}'.", this);
                        _isTransitioning = false;
                        yield break;
                    }

                    yield return loadOperation;
                    targetScene = SceneManager.GetSceneByName(sceneName);
                }

                ApplyPhase3WorldOffset(phaseId, sceneRole, targetScene);
                DisablePhaseScenePresentation(phaseId, targetScene);
                _loadedPhaseSceneNames.Add(sceneName);
                Debug.Log($"Phase scene load complete: phase={phaseId}, role={sceneRole}, scene={sceneName}", this);
                OnPhaseSceneLoaded?.Invoke(phaseId, sceneRole, sceneName);
            }

            _isTransitioning = false;
            if (!_hasPendingPhaseRequest)
            {
                yield break;
            }

            _hasPendingPhaseRequest = false;
            bool forcePendingReload = _pendingForceReload;
            _pendingForceReload = false;
            if (forcePendingReload)
            {
                TryReloadPhase(_pendingPhaseId, _pendingTimelineRole);
                yield break;
            }

            TryLoadPhase(_pendingPhaseId, _pendingTimelineRole);
        }

        private void QueuePendingRequest(PhaseId phaseId, TimelineRole timelineRole, bool forceReload)
        {
            bool preserveForceReload = _hasPendingPhaseRequest
                && _pendingForceReload
                && _pendingPhaseId == phaseId
                && _pendingTimelineRole == timelineRole;
            _hasPendingPhaseRequest = true;
            _pendingPhaseId = phaseId;
            _pendingTimelineRole = timelineRole;
            _pendingForceReload = forceReload || preserveForceReload;
        }

        private bool AreRequiredScenesLoaded(PhaseId phaseId, TimelineRole timelineRole)
        {
            string[] requiredSceneNames = GetRequiredSceneNames(phaseId, timelineRole);
            if (_loadedPhaseSceneNames.Count != requiredSceneNames.Length)
            {
                return false;
            }

            for (int i = 0; i < requiredSceneNames.Length; i++)
            {
                string sceneName = requiredSceneNames[i];
                if (!_loadedPhaseSceneNames.Contains(sceneName) || !SceneManager.GetSceneByName(sceneName).isLoaded)
                {
                    return false;
                }
            }

            return true;
        }

        private static string[] GetRequiredSceneNames(PhaseId phaseId, TimelineRole timelineRole)
        {
            if (phaseId == PhaseId.Phase3)
            {
                return new[]
                {
                    GetPhaseSceneName(phaseId, TimelineRole.Past),
                    GetPhaseSceneName(phaseId, TimelineRole.Future)
                };
            }

            return new[] { GetPhaseSceneName(phaseId, timelineRole) };
        }

        private static TimelineRole GetTimelineRole(string sceneName)
        {
            return sceneName.EndsWith("_Past", StringComparison.Ordinal)
                ? TimelineRole.Past
                : TimelineRole.Future;
        }

        private void ApplyPhase3WorldOffset(PhaseId phaseId, TimelineRole timelineRole, Scene scene)
        {
            if (phaseId != PhaseId.Phase3
                || timelineRole != TimelineRole.Future
                || !scene.IsValid()
                || !scene.isLoaded
                || !_offsetPhaseSceneNames.Add(scene.name))
            {
                return;
            }

            Vector3 offset = Vector3.up * PHASE3_FUTURE_WORLD_Y_OFFSET;
            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                rootObjects[i].transform.position += offset;
            }
        }

        private static void DisablePhaseScenePresentation(PhaseId phaseId, Scene scene)
        {
            if (phaseId != PhaseId.Phase3 || !scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                Canvas[] canvases = rootObjects[i].GetComponentsInChildren<Canvas>(true);
                for (int j = 0; j < canvases.Length; j++)
                {
                    canvases[j].gameObject.SetActive(false);
                }

                AudioListener[] listeners = rootObjects[i].GetComponentsInChildren<AudioListener>(true);
                for (int j = 0; j < listeners.Length; j++)
                {
                    listeners[j].enabled = false;
                }
            }
        }

        private static bool Contains(string[] values, string target)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == target)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
