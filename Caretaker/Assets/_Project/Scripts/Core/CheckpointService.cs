namespace Caretaker.Core
{
    /// <summary>
    /// 체크포인트 스냅샷 생성과 복원을 담당한다.
    /// MonoBehaviour에 의존하지 않는 순수 C# 클래스.
    /// </summary>
    /// <remarks>
    /// DSD §3.9 — 게임 진행 / 체크포인트 시스템
    /// 계층: Domain Service
    /// </remarks>
    public class CheckpointService
    {
        /// <summary>
        /// 현재 RuntimeState를 스냅샷으로 저장한다.
        /// </summary>
        /// <param name="checkpointId">체크포인트 식별자.</param>
        /// <returns>생성된 스냅샷.</returns>
        public CheckpointSnapshot CreateCheckpoint(string checkpointId)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 가장 최근 스냅샷을 반환한다.
        /// </summary>
        /// <returns>최신 체크포인트 스냅샷.</returns>
        public CheckpointSnapshot LoadLatestSnapshot()
        {
            throw new System.NotImplementedException();
        }
    }
}
