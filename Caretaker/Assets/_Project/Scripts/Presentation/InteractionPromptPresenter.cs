using TMPro;
using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// 근접/E키/클릭 상호작용 프롬프트를 표시한다.
    /// 범위 밖이면 비활성화한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.10 — UI / HUD 시스템
    /// 계층: Unity Component
    /// </remarks>
    public class InteractionPromptPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _promptText;

        private string _currentPrompt = string.Empty;
        private bool _isVisible;

        /// <summary>현재 표시 중인 프롬프트 문구.</summary>
        public string CurrentPrompt => _currentPrompt;

        /// <summary>프롬프트 표시 여부.</summary>
        public bool IsVisible => _isVisible;

        private void Awake()
        {
            ResolveDefaultReferences();
            Render();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveDefaultReferences();
        }
#endif

        /// <summary>
        /// 상호작용 프롬프트를 표시한다.
        /// </summary>
        /// <param name="promptText">표시할 프롬프트 텍스트. (예: "E — 조사")</param>
        public void ShowPrompt(string promptText)
        {
            _currentPrompt = promptText ?? string.Empty;
            _isVisible = !string.IsNullOrWhiteSpace(_currentPrompt);
            Render();
        }

        /// <summary>
        /// 프롬프트를 숨긴다.
        /// </summary>
        public void HidePrompt()
        {
            _currentPrompt = string.Empty;
            _isVisible = false;
            Render();
        }

        private void ResolveDefaultReferences()
        {
            if (_root == null)
            {
                _root = gameObject;
            }

            if (_promptText == null)
            {
                _promptText = GetComponentInChildren<TMP_Text>(true);
            }
        }

        private void Render()
        {
            if (_promptText != null)
            {
                _promptText.text = _currentPrompt;
            }

            if (_root != null)
            {
                _root.SetActive(_isVisible);
            }
        }
    }
}
