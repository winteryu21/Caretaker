using System;

using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Caretaker.Shared;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// Input System 액션을 이동/점프/상호작용 입력으로 해석합니다.
    /// </summary>
    /// <remarks>
    /// DSD §3.3 플레이어 제어 및 상호작용 시스템
    /// 계층: Unity Component
    /// </remarks>
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputReader : MonoBehaviour
    {
        private const string INPUT_ACTIONS_ASSET_PATH = "Assets/InputSystem_Actions.inputactions";
        private const string PLAYER_ACTION_MAP = "Player";

        private InputAction _clickAction;
        private InputAction _crouchAction;
        private InputAction _interactAction;
        private InputAction _jumpAction;
        private InputAction _moveAction;
        private InputAction _sprintAction;
        private PlayerInput _playerInput;
        private bool _jumpPressedThisFrame;
        private string _selectedItemId;
        private int _lastKeyboardHorizontalDirection;
        private bool _wasKeyboardLeftPressed;
        private bool _wasKeyboardRightPressed;

        /// <summary>
        /// 플레이어가 상호작용을 요청했을 때 발생합니다.
        /// </summary>
        public event Action<InteractionRequest> OnInteractionRequested;

        /// <summary>
        /// 현재 이동 입력값을 반환합니다.
        /// </summary>
        public Vector2 MoveInput
        {
            get
            {
                Vector2 moveInput = _moveAction.ReadValue<Vector2>();
                moveInput.x = ResolveHorizontalInput(moveInput.x);
                return moveInput;
            }
        }

        /// <summary>
        /// 웅크리기 입력이 현재 유지 중인지 반환합니다.
        /// </summary>
        public bool IsCrouchPressed => _crouchAction.IsPressed();

        /// <summary>
        /// 점프 입력이 현재 유지 중인지 반환합니다.
        /// </summary>
        public bool IsJumpPressed => _jumpAction.IsPressed();

        /// <summary>
        /// 달리기 입력이 현재 유지 중인지 반환합니다.
        /// </summary>
        public bool IsSprintPressed => _sprintAction.IsPressed();

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _moveAction = _playerInput.actions["Move"];
            _jumpAction = _playerInput.actions["Jump"];
            _crouchAction = _playerInput.actions["Crouch"];
            _sprintAction = _playerInput.actions["Sprint"];
            _clickAction = _playerInput.actions["Attack"];
            _interactAction = _playerInput.actions["Interact"];
        }

        private void OnEnable()
        {
            _jumpAction.performed += HandleJumpPerformed;
            _clickAction.performed += HandleClickPerformed;
            _interactAction.performed += HandleInteractPerformed;
        }

        private void OnDisable()
        {
            _jumpAction.performed -= HandleJumpPerformed;
            _clickAction.performed -= HandleClickPerformed;
            _interactAction.performed -= HandleInteractPerformed;
        }

        private void Reset()
        {
            _playerInput = GetComponent<PlayerInput>();

#if UNITY_EDITOR
            _playerInput.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_ASSET_PATH);
            _playerInput.defaultActionMap = PLAYER_ACTION_MAP;
#endif
        }

        /// <summary>
        /// 현재 선택된 인벤토리 아이템 ID를 설정합니다.
        /// </summary>
        public void SetSelectedItem(string selectedItemId)
        {
            _selectedItemId = selectedItemId;
        }

        /// <summary>
        /// 현재 틱에서 소비할 점프 눌림 입력을 반환합니다.
        /// </summary>
        public bool ConsumeJumpPressed()
        {
            bool jumpPressed = _jumpPressedThisFrame;
            _jumpPressedThisFrame = false;
            return jumpPressed;
        }

        private void HandleJumpPerformed(InputAction.CallbackContext context)
        {
            _jumpPressedThisFrame = true;
        }

        private void HandleClickPerformed(InputAction.CallbackContext context)
        {
            InteractionType type = string.IsNullOrEmpty(_selectedItemId)
                ? InteractionType.Examine
                : InteractionType.UseItem;
            var request = new InteractionRequest(type, Mouse.current.position.ReadValue(), _selectedItemId);
            OnInteractionRequested?.Invoke(request);
        }

        private void HandleInteractPerformed(InputAction.CallbackContext context)
        {
            var request = new InteractionRequest(InteractionType.Operate, Vector2.zero, null);
            OnInteractionRequested?.Invoke(request);
        }

        // 키보드의 좌우 입력이 동시에 눌렸을 때, 마지막으로 눌린 방향을 우선시하도록 합니다.
        private float ResolveHorizontalInput(float actionHorizontalInput)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return actionHorizontalInput;
            }

            bool isLeftPressed = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
            bool isRightPressed = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;

            if (isLeftPressed && !_wasKeyboardLeftPressed)
            {
                _lastKeyboardHorizontalDirection = -1;
            }

            if (isRightPressed && !_wasKeyboardRightPressed)
            {
                _lastKeyboardHorizontalDirection = 1;
            }

            _wasKeyboardLeftPressed = isLeftPressed;
            _wasKeyboardRightPressed = isRightPressed;

            if (isLeftPressed && isRightPressed)
            {
                return _lastKeyboardHorizontalDirection;
            }

            if (isLeftPressed)
            {
                return -1f;
            }

            if (isRightPressed)
            {
                return 1f;
            }

            return actionHorizontalInput;
        }
    }
}
