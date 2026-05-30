using UnityEngine;

using Caretaker.Gameplay;
using Caretaker.Shared;

namespace Caretaker.World
{
    /// <summary>
    /// 필드 상호작용의 판정, 최종 타입 결정, 실행 요청을 담당합니다.
    /// </summary>
    /// <remarks>
    /// DSD §3.4 - 상호작용 시스템
    /// 계층: Domain Service
    /// </remarks>
    public sealed class InteractionService
    {
        /// <summary>
        /// 요청과 현재 후보 대상을 바탕으로 상호작용을 판정합니다.
        /// </summary>
        /// <param name="request">플레이어 입력으로 생성된 상호작용 요청입니다.</param>
        /// <param name="hoverTarget">현재 hover 대상입니다.</param>
        /// <param name="proximityTarget">현재 근접 대상입니다.</param>
        /// <param name="proximityInteractionType">현재 근접 대상에 대해 우선 적용될 상호작용 타입입니다.</param>
        /// <param name="actor">상호작용을 시도하는 플레이어입니다.</param>
        /// <param name="maxDistance">플레이어 근접 상호작용에 허용되는 최대 거리입니다.</param>
        /// <param name="resolvedTarget">검증이 끝난 최종 상호작용 대상입니다.</param>
        /// <param name="resolvedType">검증이 끝난 최종 상호작용 타입입니다.</param>
        /// <returns>유효한 상호작용이 확정되고 실행되었는지 여부입니다.</returns>
        public bool TryProcessInteraction(
            InteractionRequest request,
            InteractableObject hoverTarget,
            InteractableObject proximityTarget,
            InteractionType proximityInteractionType,
            PlayerController actor,
            float maxDistance,
            out InteractableObject resolvedTarget,
            out InteractionType resolvedType)
        {
            if (actor == null)
            {
                resolvedTarget = null;
                resolvedType = default;
                return false;
            }

            switch (request.Type)
            {
                case InteractionType.Examine:
                    return TryResolveAndRunInteraction(
                        hoverTarget,
                        InteractionType.Examine,
                        actor,
                        float.PositiveInfinity,
                        out resolvedTarget,
                        out resolvedType);

                case InteractionType.Operate:
                    if (proximityInteractionType == InteractionType.None)
                    {
                        resolvedTarget = null;
                        resolvedType = default;
                        return false;
                    }

                    return TryResolveAndRunInteraction(
                        proximityTarget,
                        proximityInteractionType,
                        actor,
                        maxDistance,
                        out resolvedTarget,
                        out resolvedType);

                default:
                    resolvedTarget = null;
                    resolvedType = default;
                    return false;
            }
        }

        /// <summary>
        /// 지정된 대상이 요청된 상호작용을 처리할 수 있는지 검증합니다.
        /// </summary>
        /// <param name="target">검증할 상호작용 대상입니다.</param>
        /// <param name="interactionType">요청된 상호작용 타입입니다.</param>
        /// <param name="distance">행동 주체와 대상 사이의 최근접 거리입니다.</param>
        /// <param name="maxDistance">허용되는 최대 상호작용 거리입니다.</param>
        /// <returns>상호작용 가능 여부입니다.</returns>
        public bool ValidateInteraction(
            InteractableObject target,
            InteractionType interactionType,
            float distance,
            float maxDistance)
        {
            if (target == null)
            {
                return false;
            }

            if (distance < 0f || distance > maxDistance)
            {
                return false;
            }

            // TODO [DEV-TBD]: target.RequiredItemId가 비어 있지 않으면 actor의 인벤토리 보유 여부를 검증한다.
            return target.IsInteractable(interactionType);
        }

        private bool TryResolveAndRunInteraction(
            InteractableObject target,
            InteractionType interactionType,
            PlayerController actor,
            float maxDistance,
            out InteractableObject resolvedTarget,
            out InteractionType resolvedType)
        {
            if (target == null)
            {
                resolvedTarget = null;
                resolvedType = default;
                return false;
            }

            Vector2 actorPosition = actor.transform.position;
            float distance = target.GetDistanceFrom(actorPosition);
            if (!ValidateInteraction(target, interactionType, distance, maxDistance))
            {
                resolvedTarget = null;
                resolvedType = default;
                return false;
            }

            if (!target.RunInteraction(interactionType, actor))
            {
                resolvedTarget = null;
                resolvedType = default;
                return false;
            }

            resolvedTarget = target;
            resolvedType = interactionType;
            return true;
        }
    }
}
