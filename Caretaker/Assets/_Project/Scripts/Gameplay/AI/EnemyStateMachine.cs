namespace Caretaker.Gameplay
{
    /// <summary>
    /// 적의 Patrol, Chase, Search 상태 전이를 판정한다.
    /// </summary>
    /// <remarks>
    /// DSD 3.5 AI / 경보 시스템의 도메인 서비스.
    /// </remarks>
    public class EnemyStateMachine
    {
        /// <summary>
        /// 적 컨트롤러가 사용하는 런타임 행동 상태.
        /// </summary>
        public enum EnemyState
        {
            Patrol,
            Alert,
            Chase,
            Search
        }

        /// <summary>
        /// 현재 적 행동 상태.
        /// </summary>
        public EnemyState CurrentState { get; private set; } = EnemyState.Patrol;

        /// <summary>
        /// 초 단위로 남은 Search 지속 시간.
        /// </summary>
        public float SearchTimeRemaining { get; private set; }

        private float _playerVisibleTime;

        /// <summary>
        /// 상태 머신을 Patrol 상태로 초기화한다.
        /// </summary>
        public void Reset()
        {
            CurrentState = EnemyState.Patrol;
            SearchTimeRemaining = 0f;
            _playerVisibleTime = 0f;
        }

        /// <summary>
        /// 감지 결과와 경과 시간을 기반으로 상태 머신을 진행한다.
        /// </summary>
        /// <param name="canSeePlayer">현재 플레이어가 보이는지 여부.</param>
        /// <param name="deltaTime">초 단위 경과 시간.</param>
        /// <param name="searchDuration">시야 이탈 후 탐색을 유지할 시간.</param>
        /// <returns>갱신된 적 상태.</returns>
        public EnemyState TickState(bool canSeePlayer, float deltaTime, float searchDuration)
        {
            return TickState(canSeePlayer, false, deltaTime, searchDuration);
        }

        /// <summary>
        /// 감지 결과, 방 경보, 경과 시간을 기준으로 상태 머신을 진행한다.
        /// </summary>
        /// <param name="canSeePlayer">현재 적이 플레이어를 볼 수 있는지 여부.</param>
        /// <param name="hasRoomAlert">적이 속한 방에 Alert 상태가 활성화되어 있는지 여부.</param>
        /// <param name="deltaTime">초 단위 경과 시간.</param>
        /// <param name="searchDuration">시야 이탈 후 탐색 지속 시간.</param>
        /// <returns>갱신된 적 상태.</returns>
        public EnemyState TickState(bool canSeePlayer, bool hasRoomAlert, float deltaTime, float searchDuration)
        {
            return TickState(canSeePlayer, hasRoomAlert, deltaTime, searchDuration, 0f);
        }

        /// <summary>
        /// 감지 결과와 연속 감지 시간을 기반으로 상태 머신을 진행한다.
        /// </summary>
        /// <param name="canSeePlayer">현재 적이 플레이어를 볼 수 있는지 여부.</param>
        /// <param name="hasRoomAlert">적이 속한 방에 경보가 활성화되어 있는지 여부.</param>
        /// <param name="deltaTime">초 단위 경과 시간.</param>
        /// <param name="searchDuration">시야 이탈 후 탐색 지속 시간.</param>
        /// <param name="chaseStartDelay">추격 전환에 필요한 연속 감지 시간.</param>
        /// <returns>갱신된 적 상태.</returns>
        public EnemyState TickState(
            bool canSeePlayer,
            bool hasRoomAlert,
            float deltaTime,
            float searchDuration,
            float chaseStartDelay)
        {
            if (canSeePlayer)
            {
                if (CurrentState == EnemyState.Chase)
                {
                    return CurrentState;
                }

                _playerVisibleTime += deltaTime;
                if (_playerVisibleTime >= chaseStartDelay)
                {
                    CurrentState = EnemyState.Chase;
                    SearchTimeRemaining = 0f;
                }
                else if (hasRoomAlert && CurrentState == EnemyState.Patrol)
                {
                    CurrentState = EnemyState.Alert;
                }

                return CurrentState;
            }

            _playerVisibleTime = 0f;

            if (hasRoomAlert && CurrentState != EnemyState.Chase && CurrentState != EnemyState.Search)
            {
                CurrentState = EnemyState.Alert;
                SearchTimeRemaining = 0f;
                return CurrentState;
            }

            if (CurrentState == EnemyState.Chase)
            {
                CurrentState = EnemyState.Search;
                SearchTimeRemaining = searchDuration;
                return CurrentState;
            }

            if (CurrentState == EnemyState.Search)
            {
                SearchTimeRemaining -= deltaTime;
                if (SearchTimeRemaining <= 0f)
                {
                    Reset();
                }
            }
            else if (CurrentState == EnemyState.Alert && !hasRoomAlert)
            {
                Reset();
            }

            return CurrentState;
        }
    }
}
