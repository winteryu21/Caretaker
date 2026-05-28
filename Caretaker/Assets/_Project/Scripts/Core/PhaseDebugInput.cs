using UnityEngine;
using UnityEngine.InputSystem;

namespace Caretaker.Core
{
    /// <summary>
    /// 기능 검증을 위해 양쪽 플레이어의 Phase 전환 준비 입력을 전송한다.
    /// </summary>
    public sealed class PhaseDebugInput : MonoBehaviour
    {
        [SerializeField] private GameFlowManager _gameFlowManager;
        [SerializeField] private bool _enableDebugInput = true;

        private void Awake()
        {
            ResolveDependencies();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveDependencies();
        }
#endif

        private void Update()
        {
            if (!_enableDebugInput || Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
            {
                return;
            }

            ResolveDependencies();
            _gameFlowManager?.SubmitLocalPhaseAdvanceReady();
        }

        private void ResolveDependencies()
        {
            if (_gameFlowManager == null)
            {
                _gameFlowManager = FindAnyObjectByType<GameFlowManager>();
            }
        }
    }
}
