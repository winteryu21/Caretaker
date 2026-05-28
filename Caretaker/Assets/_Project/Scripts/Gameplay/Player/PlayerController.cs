using System;

using UnityEngine;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 입력과 모터를 연결하고, 상호작용 요청을 게임플레이 계층으로 전달합니다.
    /// </summary>
    /// <remarks>DSD §3.3 플레이어 제어 시스템 조정자</remarks>
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerMotor2D))]
    public class PlayerController : MonoBehaviour
    {
        private PlayerInputReader _inputReader;
        private PlayerMotor2D _motor2D;

        /// <summary>
        /// 플레이어의 상호작용 요청을 게임플레이 계층으로 전달할 때 발생합니다.
        /// </summary>
        public event Action<InteractionRequest> OnInteractionRequested;

        private void Awake()
        {
            _inputReader = GetComponent<PlayerInputReader>();
            _motor2D = GetComponent<PlayerMotor2D>();
        }

        private void OnEnable()
        {
            _inputReader.OnInteractionRequested += HandleInteractionRequested;
        }

        // 플레이어의 이동과 점프, 웅크리기, 달리기 입력을 모터2D에 전달합니다.
        private void FixedUpdate()
        {
            _motor2D.TickMotor(
                _inputReader.MoveInput,
                _inputReader.ConsumeJumpPressed(),
                _inputReader.IsJumpPressed,
                _inputReader.IsCrouchPressed,
                _inputReader.IsSprintPressed);
        }

        private void OnDisable()
        {
            _inputReader.OnInteractionRequested -= HandleInteractionRequested;
        }

        // 플레이어의 상호작용 키 입력을 이벤트로 전달합니다.
        private void HandleInteractionRequested(InteractionRequest request)
        {
            OnInteractionRequested?.Invoke(request);
        }
    }
}
