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
        private ulong? _currentTalkerId;

        /// <summary>
        /// 현재 송신권 보유자 ID를 반환한다. 송신자가 없으면 null이다.
        /// </summary>
        public ulong? CurrentTalkerId => _currentTalkerId;

        /// <summary>
        /// 현재 송신권이 비어 있는지 반환한다.
        /// </summary>
        public bool IsIdle => !_currentTalkerId.HasValue;

        /// <summary>
        /// 송신권을 요청한다. 송신권 공석 여부를 확인한다.
        /// </summary>
        /// <param name="playerId">요청 플레이어 ID.</param>
        /// <returns>부여 여부.</returns>
        public bool RequestTalk(ulong playerId)
        {
            if (!_currentTalkerId.HasValue)
            {
                _currentTalkerId = playerId;
                return true;
            }

            return _currentTalkerId.Value == playerId;
        }

        /// <summary>
        /// 송신권을 해제한다. 현재 소유자를 확인 후 해제한다.
        /// </summary>
        /// <param name="playerId">해제 요청 플레이어 ID.</param>
        /// <returns>해제 여부.</returns>
        public bool ReleaseTalk(ulong playerId)
        {
            if (!_currentTalkerId.HasValue || _currentTalkerId.Value != playerId)
            {
                return false;
            }

            _currentTalkerId = null;
            return true;
        }

        /// <summary>
        /// 지정한 플레이어가 송신권을 보유 중이면 강제로 해제한다.
        /// </summary>
        /// <param name="playerId">송신권 해제 대상 플레이어 ID.</param>
        /// <returns>해제 여부.</returns>
        public bool ForceReleaseIfOwnedBy(ulong playerId)
        {
            return ReleaseTalk(playerId);
        }

        /// <summary>
        /// 송신권을 비운다.
        /// </summary>
        public void Clear()
        {
            _currentTalkerId = null;
        }
    }
}
