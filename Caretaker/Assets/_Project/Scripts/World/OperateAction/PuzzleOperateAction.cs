using UnityEngine;

using Caretaker.Gameplay;
using Caretaker.Presentation;
using Caretaker.Shared;

namespace Caretaker.World
{
    /// <summary>
    /// 지정된 퍼즐 화면을 열고 닫는 일반 조작입니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InteractableObject))]
    public sealed class PuzzleOperateAction : MonoBehaviour, IOperateAction
    {
        [Header("Puzzle")]
        [SerializeField] private PuzzleUIBase _puzzleUi;

        /// <summary>
        /// 퍼즐 화면이 현재 열려 있는지 반환합니다.
        /// </summary>
        public bool IsOpen => _puzzleUi != null && _puzzleUi.IsOpen;

        private void Awake()
        {
            EnsureOperateInteractionType();
        }

        private void Reset()
        {
            EnsureOperateInteractionType();
        }

        private void OnValidate()
        {
            EnsureOperateInteractionType();
        }

        /// <summary>
        /// 지정된 퍼즐 화면을 엽니다.
        /// </summary>
        /// <param name="actor">퍼즐 조작을 실행한 플레이어입니다.</param>
        /// <returns>이번 요청으로 퍼즐 화면이 열렸는지 여부입니다.</returns>
        public bool Execute(PlayerController actor)
        {
            if (_puzzleUi == null || _puzzleUi.IsOpen)
            {
                return false;
            }

            _puzzleUi.Open();
            return true;
        }

        /// <summary>
        /// 현재 열린 퍼즐 화면을 닫습니다.
        /// </summary>
        public void Close()
        {
            if (_puzzleUi == null || !_puzzleUi.IsOpen)
            {
                return;
            }

            _puzzleUi.Close();
        }

        private void EnsureOperateInteractionType()
        {
            if (TryGetComponent(out InteractableObject interactableObject))
            {
                interactableObject.EnsureInteractionType(InteractionType.Operate);
            }
        }
    }
}

// todo : 퍼즐 화면이 열려 있는 동안 플레이어 이동을 제한하는 기능 추가 고려
// todo : 퍼즐 프레임워크 호출로 변경
