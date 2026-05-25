namespace Caretaker.Gameplay
{
    /// <summary>
    /// Patrol/Alert/Chase/Search 상태 전이를 판정한다.
    /// MonoBehaviour에 의존하지 않는 순수 C# 클래스.
    /// </summary>
    /// <remarks>
    /// DSD §3.5 — AI / 경보 시스템
    /// 계층: Domain Service
    /// </remarks>
    public class EnemyStateMachine
    {
        /// <summary>
        /// 현재 적 상태, 감지 결과, 시간을 기반으로 상태 전이를 판정한다.
        /// </summary>
        public void TickState()
        {
            throw new System.NotImplementedException();
        }
    }
}
