using UnityEngine;

using Caretaker.Shared;

namespace Caretaker.Core
{
    /// <summary>
    /// 체크포인트 ID, 스폰 위치, 복원 정책을 정의하는 데이터 에셋.
    /// </summary>
    /// <remarks>DSD §4.2 — ScriptableObject 스키마</remarks>
    [CreateAssetMenu(fileName = "SO_Checkpoint", menuName = "Caretaker SO/Flow/CheckpointDefinition")]
    public class CheckpointDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _checkpointId;
        [SerializeField] private PhaseId _phaseId;
        [SerializeField] private string _pastSpawnId;
        [SerializeField] private string _futureSpawnId;
        [SerializeField] private string _restorePolicy;

        /// <summary>체크포인트 고유 식별자.</summary>
        public string CheckpointId => _checkpointId;

        /// <summary>이 체크포인트가 속한 Phase.</summary>
        public PhaseId PhaseId => _phaseId;

        /// <summary>Past 플레이어 스폰 위치 ID.</summary>
        public string PastSpawnId => _pastSpawnId;

        /// <summary>Future 플레이어 스폰 위치 ID.</summary>
        public string FutureSpawnId => _futureSpawnId;
    }
}
