using System;

using UnityEngine;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 입력과 모터를 연결하고, 상호작용 요청을 게임플레이 계층으로 릴레이하는 조율자.
    /// </summary>
    /// <remarks>DSD §3.3 — 플레이어 제어 시스템 조율자</remarks>
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerMotor2D))]
    public class PlayerController : MonoBehaviour
    {
        private PlayerInputReader _inputReader;
        private PlayerMotor2D _motor2D;

        /// <summary>
        /// 플레이어의 상호작용 요청이 게임플레이 계층으로 전달될 때 발생한다.
        /// </summary>
        public event Action<InteractionRequest> OnInteractionRequested;

        private void Awake()
        {
            _inputReader = GetComponent<PlayerInputReader>();
            _motor2D = GetComponent<PlayerMotor2D>();
        }

        private void OnEnable()
        {
            _inputReader.OnJumpPressed += HandleJumpPressed;
            _inputReader.OnInteractionRequested += HandleInteractionRequested;
        }

        private void FixedUpdate()
        {
            _motor2D.TickMotor(_inputReader.MoveInput, _inputReader.IsCrouchPressed, _inputReader.IsSprintPressed);
        }

        private void OnDisable()
        {
            _inputReader.OnJumpPressed -= HandleJumpPressed;
            _inputReader.OnInteractionRequested -= HandleInteractionRequested;
        }

        private void HandleJumpPressed()
        {
            _motor2D.QueueJump();
        }

        private void HandleInteractionRequested(InteractionRequest request)
        {
            OnInteractionRequested?.Invoke(request);
        }
    }
}
