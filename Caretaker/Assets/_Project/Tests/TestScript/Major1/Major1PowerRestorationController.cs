using UnityEngine;
using UnityEngine.Events;

using Caretaker.Gameplay;
using Caretaker.World;

namespace Caretaker.Presentation
{
    [AddComponentMenu("Caretaker/Test/Major1/Power Restoration Controller")]
    [DisallowMultipleComponent]
    public sealed class Major1PowerRestorationController : MonoBehaviour
    {
        private const string PAST_SOLVED_CONDITION_KEY = "major1PastCircuit";
        private const string PAST_SOLVED_CONDITION_VALUE = "Solved";

        [Header("Causality")]
        [SerializeField] private CausalityManager _causalityManager;
        [SerializeField] private CausalTrigger _powerRestoredTrigger;

        [Header("Vision")]
        [SerializeField] private DarkVisionController _darkVisionReleaseController;

        [Header("Events")]
        [SerializeField] private UnityEvent _onPastCircuitSolved;
        [SerializeField] private UnityEvent _onBatteryInserted;
        [SerializeField] private UnityEvent _onCableConnected;
        [SerializeField] private UnityEvent _onPowerRestored;

        private bool _pastCircuitSolved;
        private bool _batteryInserted;
        private bool _cableConnected;
        private bool _futureSwitchSolved;
        private bool _powerRestored;

        /// <summary>Whether the past circuit puzzle has been solved.</summary>
        public bool IsPastCircuitSolved => _pastCircuitSolved;

        /// <summary>Whether the future breaker panel has battery power.</summary>
        public bool IsBatteryInserted => _batteryInserted;

        /// <summary>Whether the main power cable has been connected.</summary>
        public bool IsCableConnected => _cableConnected;

        /// <summary>Whether power has been restored.</summary>
        public bool IsPowerRestored => _powerRestored;

        /// <summary>Marks the past circuit puzzle solved and opens future cable progress.</summary>
        public void MarkPastCircuitSolved()
        {
            if (_pastCircuitSolved)
            {
                return;
            }

            _pastCircuitSolved = true;
            SetCausalityCondition(PAST_SOLVED_CONDITION_KEY, PAST_SOLVED_CONDITION_VALUE);
            Debug.Log("Major1: 과거 회로 퍼즐 성공. 미래의 메인 전력 케이블 연결이 가능해졌습니다.", this);
            _onPastCircuitSolved?.Invoke();
        }

        /// <summary>Applies battery power to the future breaker panel.</summary>
        /// <param name="actor">Player who used the battery.</param>
        /// <returns>True when the battery state is available.</returns>
        public bool InsertBattery(PlayerController actor)
        {
            if (!_batteryInserted)
            {
                _batteryInserted = true;
                Debug.Log("Major1: 배터리를 사용했습니다. 차단기 패널 전원이 켜졌습니다.", this);
                _onBatteryInserted?.Invoke();
            }

            return true;
        }

        /// <summary>Attempts to connect the main cable after the past fault is isolated.</summary>
        /// <param name="actor">Player who used the cable.</param>
        /// <returns>True when the cable is connected or already connected.</returns>
        public bool TryConnectCable(PlayerController actor)
        {
            if (!_pastCircuitSolved)
            {
                Debug.LogWarning("Major1: 고장난 곳과 연결되어 있습니다. 과거에서 회로를 먼저 분리해야 합니다.", this);
                return false;
            }

            if (!_cableConnected)
            {
                _cableConnected = true;
                Debug.Log("Major1: 케이블을 메인 전력 차단기에 연결했습니다.", this);
                _onCableConnected?.Invoke();
            }

            return true;
        }

        /// <summary>Attempts final power restoration from the accumulated Major 1 state.</summary>
        /// <param name="actor">Player who completed the last step.</param>
        /// <returns>True when power is restored or already restored.</returns>
        public bool TryRestorePower(PlayerController actor)
        {
            if (_powerRestored)
            {
                return true;
            }

            if (!CanRestorePower())
            {
                Debug.LogWarning(
                    $"Major1: 전력 복구 조건 미충족. battery={_batteryInserted}, cable={_cableConnected}, switches={_futureSwitchSolved}",
                    this);
                return false;
            }

            _powerRestored = true;
            Debug.Log("Major1: 보조 전압기 스위치 순서가 맞습니다. 전력이 복구되었습니다.", this);
            _powerRestoredTrigger?.Fire();
            _onPowerRestored?.Invoke();
            ReleaseDarkVision(actor);
            return true;
        }

        /// <summary>Marks the future auxiliary switch puzzle solved and tries to restore power.</summary>
        /// <param name="actor">Player who completed the switch sequence.</param>
        public void MarkFutureSwitchSolved(PlayerController actor)
        {
            if (!_futureSwitchSolved)
            {
                _futureSwitchSolved = true;
            }

            TryRestorePower(actor);
        }

        private bool CanRestorePower()
        {
            // Prototype: cable connection is temporarily optional.
            // return _batteryInserted && _cableConnected && _futureSwitchSolved;
            return _batteryInserted && _futureSwitchSolved;
        }

        private void SetCausalityCondition(string conditionKey, string conditionValue)
        {
            if (_causalityManager == null)
            {
                _causalityManager = FindAnyObjectByType<CausalityManager>();
            }

            if (_causalityManager != null)
            {
                _causalityManager.SetCondition(conditionKey, conditionValue);
            }
        }

        private void ReleaseDarkVision(PlayerController actor)
        {
            if (_darkVisionReleaseController == null)
            {
                return;
            }

            PlayerController releaseActor = actor != null ? actor : FindFirstObjectByType<PlayerController>();
            if (releaseActor != null)
            {
                _darkVisionReleaseController.Execute(releaseActor);
            }
        }
    }
}
