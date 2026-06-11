using UnityEngine;
using UnityEngine.UI;

using Caretaker.Gameplay;

namespace Caretaker.Presentation
{
    public sealed class SwitchPopupUI : PuzzleUIBase
    {
        [SerializeField] private SwitchPuzzleManager _puzzleManager;

        [Header("Switch Images")]
        [SerializeField] private Image _switchImage1;
        [SerializeField] private Image _switchImage2;

        [SerializeField] private Sprite _offSprite;
        [SerializeField] private Sprite _onSprite;

        private SwitchGroup _currentGroup;
        private PlayerController _currentActor;

        private void Awake()
        {
            AutoCacheReferences();
        }

        /// <summary>Opens the popup for a switch group without actor context.</summary>
        /// <param name="group">Switch group represented by this popup.</param>
        public void Open(SwitchGroup group)
        {
            Open(group, null);
        }

        /// <summary>Opens the popup for a switch group and tracks the operating actor.</summary>
        /// <param name="group">Switch group represented by this popup.</param>
        /// <param name="actor">Player who opened the popup.</param>
        public void Open(SwitchGroup group, PlayerController actor)
        {
            _currentGroup = group;
            _currentActor = actor;

            AutoCacheReferences();

            Open();
            Refresh();
        }

        /// <summary>Toggles the first switch in the current group.</summary>
        public void OnClickSwitch1()
        {
            if (_puzzleManager == null)
            {
                AutoCacheReferences();
            }

            if (_puzzleManager == null)
            {
                Debug.LogError("SwitchPopupUI: Puzzle Manager가 연결되지 않았습니다.", this);
                return;
            }

            _puzzleManager.ToggleSwitch(_currentGroup, 0, _currentActor);
            Refresh();
        }

        /// <summary>Toggles the second switch in the current group.</summary>
        public void OnClickSwitch2()
        {
            if (_puzzleManager == null)
            {
                AutoCacheReferences();
            }

            if (_puzzleManager == null)
            {
                Debug.LogError("SwitchPopupUI: Puzzle Manager가 연결되지 않았습니다.", this);
                return;
            }

            _puzzleManager.ToggleSwitch(_currentGroup, 1, _currentActor);
            Refresh();
        }

        protected override bool IsCorrectSolution()
        {
            return false;
        }

        private void Refresh()
        {
            if (_puzzleManager == null)
            {
                Debug.LogError("SwitchPopupUI: Puzzle Manager가 연결되지 않았습니다.", this);
                return;
            }

            if (_switchImage1 == null)
            {
                Debug.LogError("SwitchPopupUI: Switch Image 1이 연결되지 않았습니다.", this);
                return;
            }

            if (_switchImage2 == null)
            {
                Debug.LogError("SwitchPopupUI: Switch Image 2가 연결되지 않았습니다.", this);
                return;
            }

            if (_offSprite == null)
            {
                Debug.LogError("SwitchPopupUI: Off Sprite가 연결되지 않았습니다.", this);
                return;
            }

            if (_onSprite == null)
            {
                Debug.LogError("SwitchPopupUI: On Sprite가 연결되지 않았습니다.", this);
                return;
            }

            _switchImage1.sprite = _puzzleManager.GetSwitchState(_currentGroup, 0)
                ? _onSprite
                : _offSprite;

            _switchImage2.sprite = _puzzleManager.GetSwitchState(_currentGroup, 1)
                ? _onSprite
                : _offSprite;
        }

        private void AutoCacheReferences()
        {
            if (_puzzleManager == null)
            {
                _puzzleManager = FindFirstObjectByType<SwitchPuzzleManager>();
            }
        }
    }
}
