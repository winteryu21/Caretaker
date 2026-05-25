using Unity.Netcode;

namespace Caretaker.Core
{
    /// <summary>
    /// 2인 Host-Client 세션 생성, 참가, Timeout, 연결 종료를 처리한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.2 — 네트워크 동기화 시스템
    /// 계층: Network Boundary
    /// </remarks>
    public class NetworkSessionController : NetworkBehaviour
    {
        // 1. 상수

        // 2. Serialize 필드

        // 3. private 필드

        // 4. 프로퍼티

        // 5. 이벤트

        // 6. Unity 생명주기

        // 7. public 메서드

        /// <summary>
        /// Host 세션을 생성한다.
        /// </summary>
        public void CreateSession()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Client가 세션에 참가한다. 10초 Timeout을 감시한다.
        /// </summary>
        public void JoinSession()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 연결 끊김을 처리한다. 일시 정지 또는 세션 종료.
        /// </summary>
        /// <param name="clientId">끊긴 클라이언트 ID.</param>
        public void HandleDisconnect(ulong clientId)
        {
            throw new System.NotImplementedException();
        }

        // 8. private 메서드
    }
}
