using UnityEngine;
using UnityEngine.InputSystem;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// PTT 키 입력을 감지하여 RadioService에 송신 요청을 전달한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.6 — 무전기 통신 시스템
    /// 계층: Unity Component
    /// </remarks>
    public class RadioInputController : MonoBehaviour
    {
        [SerializeField] private RadioNetworkBridge _radioNetworkBridge;
        [SerializeField] private MonoBehaviour _voiceTransportBehaviour;
        [SerializeField] private bool _autoFindDependencies = true;

        private IVoiceTransport _voiceTransport;
        private bool _isTalkKeyHeld;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void OnEnable()
        {
            if (_radioNetworkBridge != null)
            {
                _radioNetworkBridge.OnLocalRadioStateChanged += HandleLocalRadioStateChanged;
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || _radioNetworkBridge == null)
            {
                return;
            }

            if (keyboard.tKey.wasPressedThisFrame)
            {
                _isTalkKeyHeld = true;
                _radioNetworkBridge.RequestLocalTalk();
            }

            if (keyboard.tKey.wasReleasedThisFrame)
            {
                _isTalkKeyHeld = false;
                _radioNetworkBridge.ReleaseLocalTalk();
                _voiceTransport?.StopTransmit();
            }
        }

        private void OnDisable()
        {
            if (_radioNetworkBridge != null)
            {
                _radioNetworkBridge.OnLocalRadioStateChanged -= HandleLocalRadioStateChanged;
            }

            if (_isTalkKeyHeld)
            {
                if (_radioNetworkBridge != null)
                {
                    _radioNetworkBridge.ReleaseLocalTalk();
                }

                _isTalkKeyHeld = false;
            }

            _voiceTransport?.StopTransmit();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveVoiceTransport();
        }
#endif

        private void ResolveDependencies()
        {
            if (_autoFindDependencies && _radioNetworkBridge == null)
            {
                _radioNetworkBridge = FindAnyObjectByType<RadioNetworkBridge>();
            }

            ResolveVoiceTransport();
        }

        private void ResolveVoiceTransport()
        {
            _voiceTransport = _voiceTransportBehaviour as IVoiceTransport;

            if (_voiceTransport == null)
            {
                MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour behaviour in behaviours)
                {
                    if (behaviour is IVoiceTransport voiceTransport)
                    {
                        _voiceTransportBehaviour = behaviour;
                        _voiceTransport = voiceTransport;
                        return;
                    }
                }
            }

            if (_voiceTransport == null && _voiceTransportBehaviour != null && Application.isPlaying)
            {
                Debug.LogError("Assigned voice transport must implement IVoiceTransport.", this);
            }
        }

        private void HandleLocalRadioStateChanged(RadioState state, ulong talkerId)
        {
            if (state == RadioState.Transmitting && _isTalkKeyHeld)
            {
                _voiceTransport?.StartTransmit();
                return;
            }

            _voiceTransport?.StopTransmit();
        }
    }
}
