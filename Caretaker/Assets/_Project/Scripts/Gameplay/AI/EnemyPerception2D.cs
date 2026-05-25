using UnityEngine;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 10 unit 감지 거리, 45도 시야와 장애물 Raycast 기반으로
    /// 플레이어 감지 여부를 판정한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.5 — AI / 경보 시스템
    /// 계층: Unity Component
    /// </remarks>
    public class EnemyPerception2D : MonoBehaviour
    {
        // 1. Serialize 필드

        // 2. private 필드

        // 3. 프로퍼티

        /// <summary>
        /// 시야, 거리, 차폐를 검사하여 플레이어 감지 여부를 반환한다.
        /// </summary>
        /// <param name="playerPosition">플레이어 위치.</param>
        /// <param name="isCrouching">플레이어 웅크리기 상태.</param>
        /// <returns>감지 여부.</returns>
        public bool EvaluateSight(Vector2 playerPosition, bool isCrouching)
        {
            throw new System.NotImplementedException();
        }
    }
}
