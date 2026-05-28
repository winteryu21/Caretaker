using System;
using System.Collections;

using Caretaker.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Caretaker.Core
{
    /// <summary>
    /// Phase와 시간대 역할에 맞는 월드 씬을 Additive로 로드하고 이전 Phase 씬을 언로드한다.
    /// </summary>
    public sealed class SceneLoader : MonoBehaviour
    {
        private string _loadedPhaseSceneName;
        private bool _isTransitioning;

        /// <summary>
        /// 로컬 Phase 씬 로드가 완료되었을 때 발생한다.
        /// </summary>
        public event Action<PhaseId, TimelineRole, string> OnPhaseSceneLoaded;

        /// <summary>
        /// 지정한 Phase와 시간대 역할에 해당하는 씬 이름을 반환한다.
        /// </summary>
        /// <param name="phaseId">로드할 Phase.</param>
        /// <param name="timelineRole">로드할 시간대 역할.</param>
        /// <returns>Build Settings에 등록된 씬 이름.</returns>
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
        /// 현재 로컬 클라이언트에 필요한 Phase 씬을 Additive로 로드한다.
        /// </summary>
        /// <param name="phaseId">로드할 Phase.</param>
        /// <param name="timelineRole">로컬 시간대 역할.</param>
        /// <returns>로드 요청이 시작되었는지 여부.</returns>
        public bool TryLoadPhase(PhaseId phaseId, TimelineRole timelineRole)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("Phase scene transition is already running.", this);
                return false;
            }

            if (timelineRole is not (TimelineRole.Past or TimelineRole.Future))
            {
                Debug.LogWarning("Cannot load a phase scene before a timeline role is assigned.", this);
                return false;
            }

            string sceneName = GetPhaseSceneName(phaseId, timelineRole);
            if (_loadedPhaseSceneName == sceneName && SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                return true;
            }

            StartCoroutine(LoadPhaseRoutine(phaseId, timelineRole, sceneName));
            return true;
        }

        private IEnumerator LoadPhaseRoutine(PhaseId phaseId, TimelineRole timelineRole, string sceneName)
        {
            _isTransitioning = true;

            if (!string.IsNullOrEmpty(_loadedPhaseSceneName))
            {
                Scene loadedScene = SceneManager.GetSceneByName(_loadedPhaseSceneName);
                if (loadedScene.isLoaded)
                {
                    AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(loadedScene);
                    if (unloadOperation != null)
                    {
                        yield return unloadOperation;
                    }
                }
            }

            Scene targetScene = SceneManager.GetSceneByName(sceneName);
            if (!targetScene.isLoaded)
            {
                AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (loadOperation == null)
                {
                    Debug.LogError($"Failed to start loading phase scene '{sceneName}'.", this);
                    _isTransitioning = false;
                    yield break;
                }

                yield return loadOperation;
            }

            _loadedPhaseSceneName = sceneName;
            _isTransitioning = false;
            OnPhaseSceneLoaded?.Invoke(phaseId, timelineRole, sceneName);
        }
    }
}
