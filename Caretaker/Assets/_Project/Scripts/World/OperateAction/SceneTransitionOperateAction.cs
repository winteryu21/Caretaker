using Caretaker.Core;
using Caretaker.Gameplay;
using Caretaker.Shared;
using UnityEngine;

namespace Caretaker.World
{
    /// <summary>
    /// E키 조작으로 플레이어를 다른 Unity 씬의 스폰 지점으로 이동시키는 일반 조작입니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InteractableObject))]
    public sealed class SceneTransitionOperateAction : MonoBehaviour, IOperateAction
    {
        [Header("Destination")]
        [SerializeField] private string _targetSceneName;
        [SerializeField] private string _spawnPointName = "SceneStartPoint";
        [SerializeField] private bool _unloadActorScene;
        [SerializeField] private SceneTransitionManager _transitionManager;

        private void Awake()
        {
            EnsureOperateInteractionType();
        }

        private void Reset()
        {
            EnsureOperateInteractionType();
        }

        private void OnValidate()
        {
            _targetSceneName = _targetSceneName?.Trim();
            _spawnPointName = _spawnPointName?.Trim();
            EnsureOperateInteractionType();
        }

        /// <summary>
        /// 퍼즐 완료 이벤트 등에서 Inspector로 연결해 호출할 수 있는 씬 이동 메서드입니다.
        /// </summary>
        public void Transition()
        {
            if (!ResolveTransitionManager())
            {
                return;
            }

            _transitionManager.TryTransitionLocalPlayer(_targetSceneName, _spawnPointName, _unloadActorScene);
        }

        /// <summary>
        /// 상호작용한 플레이어를 목표 씬으로 이동합니다.
        /// </summary>
        /// <param name="actor">이동할 플레이어입니다.</param>
        /// <returns>씬 이동 요청이 접수되었으면 true입니다.</returns>
        public bool Execute(PlayerController actor)
        {
            return ResolveTransitionManager() &&
                _transitionManager.TryTransitionActor(actor, _targetSceneName, _spawnPointName, _unloadActorScene);
        }

        private bool ResolveTransitionManager()
        {
            if (_transitionManager == null)
            {
                _transitionManager = FindAnyObjectByType<SceneTransitionManager>();
            }

            if (_transitionManager != null)
            {
                return true;
            }

            Debug.LogWarning("Scene transition failed: SceneTransitionManager is missing.", this);
            return false;
        }

        private void EnsureOperateInteractionType()
        {
            if (TryGetComponent(out InteractableObject interactableObject))
            {
                interactableObject.EnsureInteractionType(InteractionType.Operate);
            }
        }
    }
}
