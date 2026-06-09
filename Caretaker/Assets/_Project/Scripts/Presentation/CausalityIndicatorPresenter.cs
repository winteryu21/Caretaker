using System.Collections;

using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// 실제 인과 변경 발생 시 양쪽 HUD에 추상 Pulse를 표시한다.
    /// Rule ID, Receiver ID, Room ID, 상태값 등 구체 정보는 표시하지 않는다.
    /// </summary>
    /// <remarks>
    /// DSD §3.10 — UI / HUD 시스템, §3.1 정보 공개 계약
    /// 계층: Unity Component
    /// </remarks>
    public class CausalityIndicatorPresenter : MonoBehaviour
    {
        private const float DEFAULT_VISIBLE_SECONDS = 1.5f;

        [SerializeField] private GameObject _root;
        [SerializeField] private Animator _animator;
        [SerializeField] private string _pulseTriggerName = "Pulse";
        [SerializeField] private float _visibleSeconds = DEFAULT_VISIBLE_SECONDS;

        private Coroutine _hideRoutine;
        private bool _isVisible;

        /// <summary>인과 Pulse 표시 여부.</summary>
        public bool IsVisible => _isVisible;

        private void Awake()
        {
            ResolveDefaultReferences();
            SetVisible(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveDefaultReferences();
            _visibleSeconds = Mathf.Max(0f, _visibleSeconds);
        }
#endif

        /// <summary>
        /// 인과 Pulse 애니메이션을 표시한다.
        /// </summary>
        public void ShowCausalityPulse()
        {
            ResolveDefaultReferences();
            SetVisible(true);

            if (_animator != null && !string.IsNullOrWhiteSpace(_pulseTriggerName))
            {
                _animator.SetTrigger(_pulseTriggerName);
            }

            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
            }

            if (_visibleSeconds > 0f)
            {
                _hideRoutine = StartCoroutine(HideAfterDelay());
            }
        }

        private void ResolveDefaultReferences()
        {
            if (_root == null)
            {
                _root = gameObject;
            }

            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>(true);
            }
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(_visibleSeconds);
            _hideRoutine = null;
            SetVisible(false);
        }

        private void SetVisible(bool isVisible)
        {
            _isVisible = isVisible;

            if (_root != null)
            {
                _root.SetActive(isVisible);
            }
        }
    }
}
