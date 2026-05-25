using Unity.Netcode;

namespace Caretaker.World
{
    /// <summary>
    /// 인과 시스템의 Host RPC 진입점과 ClientRpc 결과 적용을 담당한다.
    /// CausalityService에 판정을 위임하고 네트워크로 결과를 전파한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.1 — 시간 인과 시스템
    /// 계층: Unity Component / Network Boundary
    /// </remarks>
    public class CausalityManager : NetworkBehaviour
    {
        // 1. 상수

        // 2. Serialize 필드

        // 3. private 필드

        // 4. 프로퍼티

        // 5. 이벤트

        // 6. Unity 생명주기

        // 7. public 메서드

        // 8. private 메서드 (RPC handlers)
    }
}
