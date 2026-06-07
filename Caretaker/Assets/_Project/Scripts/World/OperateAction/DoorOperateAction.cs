using UnityEngine;

using Caretaker.Gameplay;

namespace Caretaker.World
{
    /// <summary>
    /// 일반 조작 요청을 대상 문의 열기 동작으로 전달합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DoorOperateAction : MonoBehaviour, IOperateAction
    {
        private static readonly int IS_OPEN = Animator.StringToHash("IsOpen");

        [Header("Target Door")]
        [SerializeField] private GameObject _targetDoor;

        private Animator _targetAnimator;
        private Collider2D _blockingCollider;
        private bool _isOpen;

        private void Awake()
        {
            ResolveTargetComponents();
        }

        /// <summary>
        /// 대상 문을 엽니다.
        /// </summary>
        /// <param name="actor">문 조작을 실행한 플레이어입니다.</param>
        /// <returns>이번 요청으로 문이 열렸는지 여부입니다.</returns>
        public bool Execute(PlayerController actor)
        {
            if (_targetDoor == null || _isOpen)
            {
                return false;
            }

            _isOpen = true;

            if (_targetAnimator != null)
            {
                _targetAnimator.SetBool(IS_OPEN, true);
            }

            if (_blockingCollider != null)
            {
                _blockingCollider.enabled = false;
            }

            return true;
        }

        private void ResolveTargetComponents()
        {
            if (_targetDoor == null)
            {
                return;
            }

            _targetAnimator = _targetDoor.GetComponentInChildren<Animator>(true);

            Collider2D[] colliders = _targetDoor.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (!colliders[i].isTrigger)
                {
                    _blockingCollider = colliders[i];
                    break;
                }
            }
        }
    }
}
