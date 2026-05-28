using UnityEngine;

using Caretaker.Shared;

namespace Caretaker.Core
{
    /// <summary>
    /// Phase별 필요 Major ID 목록, 표시 모드, 체크포인트를 정의하는 데이터 에셋.
    /// </summary>
    /// <remarks>DSD §4.2 — ScriptableObject 스키마</remarks>
    [CreateAssetMenu(fileName = "SO_Phase", menuName = "Caretaker SO/Flow/PhaseDefinition")]
    public class PhaseDefinitionSO : ScriptableObject
    {
        [SerializeField] private PhaseId _phaseId;
        [SerializeField] private string[] _requiredMajorIds;
        [SerializeField] private string _presentationMode;
        [SerializeField] private string[] _checkpointIds;

        /// <summary>Phase 고유 식별자.</summary>
        public PhaseId PhaseId => _phaseId;

        /// <summary>이 Phase 완료에 필요한 Major Interaction ID 목록.</summary>
        public string[] RequiredMajorIds => _requiredMajorIds;

        /// <summary>연결된 체크포인트 ID 목록.</summary>
        public string[] CheckpointIds => _checkpointIds;
    }
}
