namespace Caretaker.Gameplay
{
    /// <summary>
    /// 송신권 요청, 충돌, timeout 규칙을 판정한다.
    /// MonoBehaviour에 의존하지 않는 순수 C# 클래스.
    /// </summary>
    /// <remarks>
    /// DSD §3.6 — 무전기 통신 시스템
    /// 계층: Domain Service
    /// </remarks>
    public class RadioService
    {
        /// <summary>
        /// 송신권을 요청한다. 송신권 공석 여부를 확인한다.
        /// </summary>
        /// <param name="playerId">요청 플레이어 ID.</param>
        /// <returns>부여 여부.</returns>
        public bool RequestTalk(ulong playerId)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 송신권을 해제한다. 현재 소유자를 확인 후 해제한다.
        /// </summary>
        /// <param name="playerId">해제 요청 플레이어 ID.</param>
        public void ReleaseTalk(ulong playerId)
        {
            throw new System.NotImplementedException();
        }
    }
}
