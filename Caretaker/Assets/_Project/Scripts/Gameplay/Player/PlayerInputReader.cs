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
    /// Input System 액션을 이동, 점프, 상호작용 입력으로 해석합니다.
    /// </summary>
    /// <remarks>
    /// DSD §3.3 - 플레이어 제어 및 상호작용 시스템
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
        private PlayerInput _playerInput;
        private InputAction _sprintAction;
        private PlayerControlMode _controlMode = PlayerControlMode.Normal;
        private bool _isCombatAimPressed;
        private bool _jumpPressedThisFrame;
        private int _lastKeyboardHorizontalDirection;
        private bool _wasKeyboardLeftPressed;
        private bool _wasKeyboardRightPressed;

        /// <summary>
        /// 플레이어가 상호작용을 요청했을 때 발생합니다.
        /// </summary>
        public event Action<InteractionRequest> OnInteractionRequested;

        /// <summary>
        /// 플레이어가 인벤토리 슬롯을 선택했을 때 발생합니다. 슬롯 인덱스는 0부터 시작합니다.
        /// </summary>
        public event Action<int> OnInventorySlotSelected;

        /// <summary>
        /// Tab 입력으로 조작 모드가 변경되었을 때 발생합니다.
        /// </summary>
        public event Action<PlayerControlMode> OnControlModeChanged;

        /// <summary>
        /// 전투 모드에서 좌클릭 공격 입력이 발생했을 때 발생합니다.
        /// </summary>
        public event Action OnCombatAttackRequested;

        /// <summary>
        /// 전투 모드에서 우클릭 조준 유지 상태가 바뀌었을 때 발생합니다.
        /// </summary>
        public event Action<bool> OnCombatAimChanged;

        /// <summary>
        /// 현재 마우스 입력 해석 모드입니다.
        /// </summary>
        public PlayerControlMode ControlMode => _controlMode;

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
            SetCombatAimPressed(false);
        }

        private void Reset()
        {
            _playerInput = GetComponent<PlayerInput>();

#if UNITY_EDITOR
            _playerInput.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_ASSET_PATH);
            _playerInput.defaultActionMap = PLAYER_ACTION_MAP;
#endif
        }

        private void Update()
        {
            HandleControlModeInput();
            HandleInventorySlotInput();
            HandleUseItemInput();
            HandleCombatAimInput();
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
            if (_controlMode == PlayerControlMode.Combat)
            {
                OnCombatAttackRequested?.Invoke();
                return;
            }

            var request = new InteractionRequest(InteractionType.Examine, Vector2.zero);
            OnInteractionRequested?.Invoke(request);
        }

        private void HandleInteractPerformed(InputAction.CallbackContext context)
        {
            var request = new InteractionRequest(InteractionType.Operate, Vector2.zero);
            OnInteractionRequested?.Invoke(request);
        }

        private void HandleUseItemInput()
        {
            if (Mouse.current == null || !Mouse.current.rightButton.wasPressedThisFrame)
            {
                return;
            }

            if (_controlMode == PlayerControlMode.Combat)
            {
                return;
            }

            var request = new InteractionRequest(InteractionType.UseItem, Vector2.zero);
            OnInteractionRequested?.Invoke(request);
        }

        private void HandleControlModeInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                ToggleControlMode();
            }
        }

        private void ToggleControlMode()
        {
            PlayerControlMode targetMode = _controlMode == PlayerControlMode.Normal
                ? PlayerControlMode.Combat
                : PlayerControlMode.Normal;
            SetControlMode(targetMode);
        }

        private void SetControlMode(PlayerControlMode controlMode)
        {
            if (_controlMode == controlMode)
            {
                return;
            }

            if (controlMode == PlayerControlMode.Normal)
            {
                SetCombatAimPressed(false);
            }

            _controlMode = controlMode;
            OnControlModeChanged?.Invoke(_controlMode);
        }

        private void HandleCombatAimInput()
        {
            bool isPressed =
                _controlMode == PlayerControlMode.Combat &&
                Mouse.current != null &&
                Mouse.current.rightButton.isPressed;
            SetCombatAimPressed(isPressed);
        }

        private void SetCombatAimPressed(bool isPressed)
        {
            if (_isCombatAimPressed == isPressed)
            {
                return;
            }

            _isCombatAimPressed = isPressed;
            OnCombatAimChanged?.Invoke(_isCombatAimPressed);
        }

        private void HandleInventorySlotInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            {
                OnInventorySlotSelected?.Invoke(0);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            {
                OnInventorySlotSelected?.Invoke(1);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
            {
                OnInventorySlotSelected?.Invoke(2);
            }
            else if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame)
            {
                OnInventorySlotSelected?.Invoke(3);
            }
            else if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame)
            {
                OnInventorySlotSelected?.Invoke(4);
            }
        }

        // 좌우 입력이 동시에 들어오면 가장 마지막으로 눌린 방향을 우선합니다.
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
