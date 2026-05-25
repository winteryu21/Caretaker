using Unity.Netcode;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 송신권 RPC와 상태 동기화를 담당한다.
    /// Host가 송신권을 부여/회수한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.6 — 무전기 통신 시스템
    /// 계층: Network Boundary
    /// </remarks>
    public class RadioNetworkBridge : NetworkBehaviour
    {
        // 1. NetworkVariable (송신권 상태)

        // 2. private 필드

        // 3. Unity 생명주기

        // 4. ServerRpc / ClientRpc 메서드
    }
}
