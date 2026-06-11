using UnityEngine;

using Caretaker.Gameplay;
using Caretaker.World;

namespace Caretaker.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InteractableObject))]
    public sealed class UIOpener : MonoBehaviour, IOperateAction
    {
        [Header("Puzzle")]
        [SerializeField] private PuzzleUIBase _puzzleUi;
        [SerializeField] private bool _closePuzzleOnAwake = true;
        // <summary>
        /// 미래 변전실 퍼즐 UI를 열 때, 해당 퍼즐이 어떤 스위치 그룹에 속하는지를 나타냅니다. (A, B, C, D)
        /// </summary>
        [Header("Switch Puzzle")]
        [SerializeField] private SwitchGroup _switchGroup;

        private void Awake()
        {
            CachePuzzleUi();

            if (_closePuzzleOnAwake && _puzzleUi != null)
            {
                _puzzleUi.Close();
            }
        }

        public bool Execute(PlayerController actor)
        {
            OpenPuzzleUI(actor);
            return true;
        }

        private void OpenPuzzleUI(PlayerController actor)
        {
            if (_puzzleUi == null)
            {
                CachePuzzleUi();

                if (_puzzleUi == null)
                {
                    Debug.LogWarning("Puzzle UI was not found.", this);
                    return;
                }
            }

            if (_puzzleUi is SwitchPopupUI switchPopupUI)
            {
                switchPopupUI.Open(_switchGroup, actor);
                return;
            }

            _puzzleUi.Open();
        }

        private void CachePuzzleUi()
        {
            if (_puzzleUi != null)
            {
                return;
            }

            _puzzleUi = FindFirstObjectByType<PuzzleUIBase>(FindObjectsInactive.Include);
        }
    }
} 
