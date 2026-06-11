using UnityEngine;
using UnityEngine.Events;

namespace Caretaker.World
{
    /// <summary>
    /// Future 월드의 변경 대상. Receiver ID와 상태 적용 어댑터를 가진다.
    /// 인과 규칙 실행 결과를 받아 오브젝트 상태를 변경한다. (문 열림, 전원 공급 등)
    /// </summary>
    /// <remarks>
    /// DSD §3.1 — 시간 인과 시스템
    /// 계층: Unity Component
    ///
    /// 사용법:
    /// 1. Inspector에서 _causalRule에 CausalRuleSO를 드래그한다.
    /// 2. 다수의 Receiver Effect가 있으면 드롭다운에서 하나를 선택한다.
    /// 3. _onActivated / _onDeactivated UnityEvent에 Animator, Light2D, Collider2D 등을 연결한다.
    /// 4. CausalityManager가 Phase 씬 로드 시 자동으로 레지스트리에 등록한다.
    /// </remarks>
    public class CausalReceiver : MonoBehaviour
    {
        private static readonly Color DEBUG_COLOR_ACTIVATED = Color.green;
        private static readonly Color DEBUG_COLOR_DEACTIVATED = Color.red;
        private static readonly Color DEFAULT_SUCCESS_EFFECT_COLOR = new(0.2f, 1f, 0.85f, 1f);

        private const int SUCCESS_EFFECT_PARTICLE_COUNT = 18;
        private const float SUCCESS_EFFECT_DURATION = 0.7f;
        private const float SUCCESS_EFFECT_PARTICLE_SIZE = 0.18f;
        private const float SUCCESS_EFFECT_SPEED = 1.4f;
        private const string SUCCESS_EFFECT_NAME = "CausalitySuccessEffect";

        [Header("Causal Rule")]
        [Tooltip("CausalRuleSO를 드래그하면 Receiver ID가 자동 설정됩니다.")]
        [SerializeField] private CausalRuleSO _causalRule;

        [Tooltip("CausalRuleSO에 Receiver Effect가 여러 개일 경우, 이 컴포넌트가 수신할 효과의 인덱스입니다.")]
        [SerializeField] private int _receiverEffectIndex;

        [Header("Resolved ID")]
        [Tooltip("CausalRuleSO에서 자동 추출된 Receiver ID입니다.")]
        [SerializeField] private string _receiverId;

        [Header("State Change Events")]
        [Tooltip("인과 결과로 활성화될 때 호출됩니다. (문 열림, 전력 공급 등)")]
        [SerializeField] private UnityEvent _onActivated;

        [Tooltip("체크포인트 복원 등으로 비활성화될 때 호출됩니다.")]
        [SerializeField] private UnityEvent _onDeactivated;

        [Header("Debug")]
        [Tooltip("SpriteRenderer가 있으면 활성화/비활성화 시 색상을 자동 변경합니다.")]
        [SerializeField] private bool _debugColorFeedback = true;

        [Header("Success Effect")]
        [SerializeField] private bool _showSuccessEffect = true;
        [SerializeField] private Color _successEffectColor = DEFAULT_SUCCESS_EFFECT_COLOR;
        [SerializeField] [Min(0.01f)] private float _successEffectScale = 1f;

        private SpriteRenderer _spriteRenderer;
        private bool _isActivated;
        private string _lastStateKey;
        private string _lastStateValue;

        /// <summary>이 리시버의 고유 식별자. (예: RCV_P1_BREAKER_PANEL)</summary>
        public string ReceiverId => _receiverId;

        /// <summary>현재 활성화 상태.</summary>
        public bool IsActivated => _isActivated;

        /// <summary>마지막으로 적용된 상태 키.</summary>
        public string LastStateKey => _lastStateKey;

        /// <summary>마지막으로 적용된 상태 값.</summary>
        public string LastStateValue => _lastStateValue;

        /// <summary>연결된 CausalRuleSO 에셋.</summary>
        public CausalRuleSO CausalRule => _causalRule;

        /// <summary>선택된 Receiver Effect 인덱스.</summary>
        public int ReceiverEffectIndex => _receiverEffectIndex;

        private void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (_debugColorFeedback && _spriteRenderer != null)
            {
                _spriteRenderer.color = DEBUG_COLOR_DEACTIVATED;
            }
        }

        private void OnEnable()
        {
            RegisterSelf();
        }

        private void OnDisable()
        {
            UnregisterSelf();
        }

        private void OnValidate()
        {
            SyncReceiverIdFromRule();

            if (_receiverId != null)
            {
                _receiverId = _receiverId.Trim();
            }
        }

        /// <summary>
        /// 인과 결과에 따른 상태를 적용한다.
        /// CausalityManager의 ClientRpc에서 호출된다.
        /// </summary>
        /// <param name="stateKey">변경할 상태 키. (예: powerState, lockState)</param>
        /// <param name="stateValue">적용할 상태 값. (예: Powered, Unlocked)</param>
        public void ApplyState(string stateKey, string stateValue)
        {
            bool stateChanged = _lastStateKey != stateKey || _lastStateValue != stateValue;
            _lastStateKey = stateKey;
            _lastStateValue = stateValue;

            Debug.Log(
                $"CausalReceiver '{_receiverId}': {stateKey}={stateValue}", this);

            // 상태 값이 비어 있지 않으면 활성화로 판단
            bool shouldActivate = !string.IsNullOrWhiteSpace(stateValue)
                && stateValue != "Off"
                && stateValue != "Locked"
                && stateValue != "false"
                && stateValue != "Inactive";

            if (stateChanged)
            {
                ShowSuccessEffect();
            }

            SetActivated(shouldActivate);
        }

        /// <summary>
        /// 체크포인트 복원 시 상태를 초기화한다.
        /// </summary>
        public void ResetState()
        {
            _lastStateKey = null;
            _lastStateValue = null;
            SetActivated(false);
        }

        private void SetActivated(bool activated)
        {
            if (_isActivated == activated)
            {
                return;
            }

            _isActivated = activated;

            // 디버그: SpriteRenderer 색상 자동 변경 (빨강 → 초록)
            if (_debugColorFeedback)
            {
                if (_spriteRenderer == null)
                {
                    _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                }

                if (_spriteRenderer != null)
                {
                    Color targetColor = activated ? DEBUG_COLOR_ACTIVATED : DEBUG_COLOR_DEACTIVATED;
                    _spriteRenderer.color = targetColor;
                    Debug.Log(
                        $"CausalReceiver '{_receiverId}': color → {(activated ? "GREEN" : "RED")}", this);
                }
                else
                {
                    Debug.LogWarning(
                        $"CausalReceiver '{_receiverId}': SpriteRenderer not found in children.", this);
                }
            }

            if (activated)
            {
                _onActivated?.Invoke();
            }
            else
            {
                _onDeactivated?.Invoke();
            }
        }

        private void ShowSuccessEffect()
        {
            if (!_showSuccessEffect || !Application.isPlaying)
            {
                return;
            }

            GameObject effectObject = new(SUCCESS_EFFECT_NAME);
            effectObject.transform.position = GetEffectPosition();

            ParticleSystem particleSystem = effectObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particleSystem.main;
            main.duration = SUCCESS_EFFECT_DURATION;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = SUCCESS_EFFECT_DURATION;
            main.startSpeed = SUCCESS_EFFECT_SPEED * _successEffectScale;
            main.startSize = SUCCESS_EFFECT_PARTICLE_SIZE * _successEffectScale;
            main.startColor = _successEffectColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, SUCCESS_EFFECT_PARTICLE_COUNT)
            });

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f * _successEffectScale;
            shape.radiusThickness = 1f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient alphaGradient = new();
            alphaGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(_successEffectColor, 0f),
                    new GradientColorKey(_successEffectColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = alphaGradient;

            ParticleSystemRenderer particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            ConfigureEffectRenderer(particleRenderer);

            particleSystem.Play();
        }

        private Vector3 GetEffectPosition()
        {
            Renderer receiverRenderer = GetComponentInChildren<Renderer>();
            return receiverRenderer != null
                ? receiverRenderer.bounds.center
                : transform.position;
        }

        private void ConfigureEffectRenderer(ParticleSystemRenderer particleRenderer)
        {
            SpriteRenderer receiverRenderer = GetComponentInChildren<SpriteRenderer>();
            if (receiverRenderer != null)
            {
                particleRenderer.sortingLayerID = receiverRenderer.sortingLayerID;
                particleRenderer.sortingOrder = receiverRenderer.sortingOrder + 2;
            }
            else
            {
                particleRenderer.sortingOrder = 2;
            }

            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader == null)
            {
                return;
            }

            Material effectMaterial = new(spriteShader);
            particleRenderer.sharedMaterial = effectMaterial;
            Destroy(effectMaterial, SUCCESS_EFFECT_DURATION + 0.1f);
        }

        /// <summary>
        /// CausalRuleSO와 선택된 인덱스로부터 Receiver ID를 동기화한다.
        /// OnValidate 및 Custom Editor에서 호출된다.
        /// </summary>
        private void SyncReceiverIdFromRule()
        {
            if (_causalRule == null || _causalRule.ReceiverEffects == null)
            {
                return;
            }

            CausalRuleSO.CausalReceiverEffect[] effects = _causalRule.ReceiverEffects;

            if (effects.Length == 0)
            {
                return;
            }

            // 인덱스 범위 보정
            _receiverEffectIndex = Mathf.Clamp(_receiverEffectIndex, 0, effects.Length - 1);
            _receiverId = effects[_receiverEffectIndex].ReceiverId;
        }

        private void RegisterSelf()
        {
            CausalityManager manager = FindAnyObjectByType<CausalityManager>();
            if (manager != null)
            {
                manager.RegisterReceiver(this);
            }
        }

        private void UnregisterSelf()
        {
            CausalityManager manager = FindAnyObjectByType<CausalityManager>();
            if (manager != null)
            {
                manager.UnregisterReceiver(this);
            }
        }
    }
}
