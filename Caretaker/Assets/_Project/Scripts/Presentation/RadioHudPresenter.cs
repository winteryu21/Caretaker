using Caretaker.Gameplay;
using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// RadioNetworkBridge 상태를 HUD 표시로 연결한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.6 — RadioHudPresenter, §3.10 — UI / HUD 시스템
    /// </remarks>
    public sealed class RadioHudPresenter : MonoBehaviour
    {
        [SerializeField] private RadioNetworkBridge _radioNetworkBridge;
        [SerializeField] private HudPresenter _hudPresenter;
        [SerializeField] private bool _autoFindDependencies = true;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void OnEnable()
        {
            ResolveDependencies();

            if (_radioNetworkBridge != null)
            {
                _radioNetworkBridge.OnLocalRadioStateChanged += HandleLocalRadioStateChanged;
            }
        }

        private void OnDisable()
        {
            if (_radioNetworkBridge != null)
            {
                _radioNetworkBridge.OnLocalRadioStateChanged -= HandleLocalRadioStateChanged;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveDependencies();
        }
#endif

        private void ResolveDependencies()
        {
            if (!_autoFindDependencies)
            {
                return;
            }

            if (_radioNetworkBridge == null)
            {
                _radioNetworkBridge = FindAnyObjectByType<RadioNetworkBridge>();
            }

            if (_hudPresenter == null)
            {
                _hudPresenter = FindAnyObjectByType<HudPresenter>();
            }
        }

        private void HandleLocalRadioStateChanged(RadioState state, ulong talkerId)
        {
            if (_hudPresenter != null)
            {
                _hudPresenter.SetRadioState(state, talkerId);
            }
        }
    }
}
