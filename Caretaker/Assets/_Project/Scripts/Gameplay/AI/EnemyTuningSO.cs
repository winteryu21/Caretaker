using UnityEngine;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 적 유형별 감지 거리, FOV, 추적 속도, 해제 지연을 정의하는 데이터 에셋.
    /// </summary>
    /// <remarks>DSD §4.2 — ScriptableObject 스키마</remarks>
    [CreateAssetMenu(fileName = "SO_EnemyTuning", menuName = "Caretaker SO/AI/EnemyTuning")]
    public class EnemyTuningSO : ScriptableObject
    {
        [SerializeField] private string _enemyType;
        [SerializeField] private float _moveSpeed = 2f;
        [SerializeField] private float _patrolWaitTime = 1f;
        [SerializeField] private float _sightDistance = 10f;
        [SerializeField] private float _fovDegrees = 45f;
        [SerializeField] private float _chaseSpeed = 5f;
        [SerializeField] private float _loseSightSeconds = 10f;
        [SerializeField] private float _alertDuration = 30f;

        /// <summary>적 유형 식별자.</summary>
        public string EnemyType => _enemyType;

        /// <summary>순찰 이동 속도 (units/sec).</summary>
        public float MoveSpeed => _moveSpeed;

        /// <summary>순찰 Waypoint 도착 후 대기 시간 (seconds).</summary>
        public float PatrolWaitTime => _patrolWaitTime;

        /// <summary>시야 감지 거리 (units).</summary>
        public float SightDistance => _sightDistance;

        /// <summary>시야각 (degrees).</summary>
        public float FovDegrees => _fovDegrees;

        /// <summary>추적 이동 속도 (units/sec).</summary>
        public float ChaseSpeed => _chaseSpeed;

        /// <summary>시야 이탈 후 탐색 지속 시간 (seconds).</summary>
        public float LoseSightSeconds => _loseSightSeconds;

        /// <summary>경보 지속 시간 (seconds).</summary>
        public float AlertDuration => _alertDuration;
    }
}
