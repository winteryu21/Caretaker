using UnityEngine;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 적의 이동과 애니메이션을 연결하는 컨트롤러.
    /// EnemyStateMachine의 결정에 따라 실제 이동을 수행한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.5 — AI / 경보 시스템
    /// 계층: Unity Component
    /// </remarks>
    public class EnemyController : MonoBehaviour
    {
        // 1. Serialize 필드
        [SerializeField] private Transform[] _patrolWaypoints;

        // 2. private 필드

        // 3. 프로퍼티

        // 4. Unity 생명주기

        // 5. public 메서드

        // 6. private 메서드
    }
}
