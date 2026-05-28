using UnityEngine;

using Caretaker.Shared;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 플레이어 상호작용을 처리하는 데 필요한 요청 정보를 전달합니다.
    /// </summary>
    /// <remarks>DSD §3.3 - BuildInteractionRequest 출력</remarks>
    public readonly struct InteractionRequest
    {
        /// <summary>
        /// 상호작용 요청을 생성합니다.
        /// </summary>
        /// <param name="interactionType">실행할 상호작용 방식입니다.</param>
        /// <param name="pointerScreenPosition">화면 좌표계 기준 포인터 위치입니다.</param>
        public InteractionRequest(InteractionType interactionType, Vector2 pointerScreenPosition)
        {
            Type = interactionType;
            PointerScreenPosition = pointerScreenPosition;
        }

        /// <summary>
        /// 실행할 상호작용 방식을 반환합니다.
        /// </summary>
        public InteractionType Type { get; }

        /// <summary>
        /// 화면 좌표계 기준 포인터 위치를 반환합니다.
        /// </summary>
        public Vector2 PointerScreenPosition { get; }
    }
}
