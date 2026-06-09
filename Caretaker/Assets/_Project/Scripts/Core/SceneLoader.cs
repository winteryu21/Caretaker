using System;
using System.Collections;
using System.Collections.Generic;

using Caretaker.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Caretaker.Core
{
    /// <summary>
    /// Loads the phase scenes required by the local timeline.
    /// Phase 3 loads both timelines so split view can render them locally.
    /// </summary>
    public sealed class SceneLoader : MonoBehaviour
    {
        private const float PHASE3_FUTURE_WORLD_Y_OFFSET = 1000f;

        private readonly HashSet<string> _loadedPhaseSceneNames = new();
        private readonly HashSet<string> _offsetPhaseSceneNames = new();

        private bool _isTransitioning;
        private bool _hasPendingPhaseRequest;
        private PhaseId _pendingPhaseId;
        private TimelineRole _pendingTimelineRole;

        /// <summary>Raised after each required phase scene finishes loading.</summary>
        public event Action<PhaseId, TimelineRole, string> OnPhaseSceneLoaded;

        /// <summary>Returns the build scene name for a phase and timeline role.</summary>
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
        /// Loads the scenes needed for the requested phase.
        /// </summary>
        /// <returns>Whether the load request was accepted.</returns>
        public bool TryLoadPhase(PhaseId phaseId, TimelineRole timelineRole)
        {
            if (_isTransitioning)
            {
                _hasPendingPhaseRequest = true;
                _pendingPhaseId = phaseId;
                _pendingTimelineRole = timelineRole;
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
            StartCoroutine(LoadPhaseRoutine(phaseId, timelineRole));
            return true;
        }

        private IEnumerator LoadPhaseRoutine(PhaseId phaseId, TimelineRole timelineRole)
        {
            _isTransitioning = true;

            string[] requiredSceneNames = GetRequiredSceneNames(phaseId, timelineRole);
            string[] loadedSceneNames = new string[_loadedPhaseSceneNames.Count];
            _loadedPhaseSceneNames.CopyTo(loadedSceneNames);

            for (int i = 0; i < loadedSceneNames.Length; i++)
            {
                string loadedSceneName = loadedSceneNames[i];
                if (Contains(requiredSceneNames, loadedSceneName))
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
            TryLoadPhase(_pendingPhaseId, _pendingTimelineRole);
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
