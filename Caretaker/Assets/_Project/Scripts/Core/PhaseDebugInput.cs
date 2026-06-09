using UnityEngine;
using UnityEngine.InputSystem;

using Caretaker.Shared;

namespace Caretaker.Core
{
    /// <summary>
    /// 기능 검증을 위해 Phase 전환과 Major 완료 입력을 전송한다.
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
            if (!_enableDebugInput || Keyboard.current == null)
            {
                return;
            }

            ResolveDependencies();

            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                SubmitPhaseDebugAdvance();
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                SubmitMajorComplete(MajorId.M1);
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                SubmitMajorComplete(MajorId.M2);
            }

            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                SubmitMajorComplete(MajorId.M3);
            }

            if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                SubmitMajorComplete(MajorId.M4);
            }
        }

        private void SubmitMajorComplete(MajorId majorId)
        {
            if (_gameFlowManager == null)
            {
                return;
            }

            _gameFlowManager.NotifyMajorComplete(_gameFlowManager.CurrentPhase, majorId);
        }

        private void SubmitPhaseDebugAdvance()
        {
            if (_gameFlowManager == null)
            {
                return;
            }

            if (_gameFlowManager.CurrentPhase == PhaseId.Phase3)
            {
                _gameFlowManager.ReportExitReached(TimelineRole.Past);
                _gameFlowManager.ReportExitReached(TimelineRole.Future);
                return;
            }

            _gameFlowManager.SubmitLocalPhaseAdvanceReady();
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
