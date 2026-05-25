using UnityEngine;

using Caretaker.Shared;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 플레이어 상호작용을 처리하는 데 필요한 데이터를 전달한다.
    /// </summary>
    /// <remarks>DSD §3.3 — BuildInteractionRequest 출력</remarks>
    public readonly struct InteractionRequest
    {
        /// <summary>
        /// 새 상호작용 요청을 생성한다.
        /// </summary>
        /// <param name="interactionType">실행할 상호작용 방식.</param>
        /// <param name="pointerScreenPosition">화면 좌표계 기준 포인터 위치.</param>
        /// <param name="selectedItemId">선택된 인벤토리 아이템 ID. 없으면 null.</param>
        public InteractionRequest(InteractionType interactionType, Vector2 pointerScreenPosition, string selectedItemId)
        {
            Type = interactionType;
            PointerScreenPosition = pointerScreenPosition;
            SelectedItemId = selectedItemId;
        }

        /// <summary>
        /// 실행할 상호작용 방식을 반환한다.
        /// </summary>
        public InteractionType Type { get; }

        /// <summary>
        /// 화면 좌표계 기준 포인터 위치를 반환한다.
        /// </summary>
        public Vector2 PointerScreenPosition { get; }

        /// <summary>
        /// 선택된 인벤토리 아이템 ID를 반환한다.
        /// </summary>
        public string SelectedItemId { get; }
    }
}
