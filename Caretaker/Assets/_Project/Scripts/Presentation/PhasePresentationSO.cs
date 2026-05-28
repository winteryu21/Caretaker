using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// Phase별 카메라 및 연출 설정을 정의하는 데이터 에셋.
    /// </summary>
    /// <remarks>DSD §4.2 — ScriptableObject 스키마</remarks>
    [CreateAssetMenu(fileName = "SO_PhasePresentation", menuName = "Caretaker SO/Presentation/PhasePresentation")]
    public class PhasePresentationSO : ScriptableObject
    {
        [SerializeField] private string _phaseId;
        [SerializeField] private bool _enableSplitView;

        /// <summary>대상 Phase 식별자.</summary>
        public string PhaseId => _phaseId;

        /// <summary>이 Phase에서 스플릿뷰 활성화 여부.</summary>
        public bool EnableSplitView => _enableSplitView;
    }
}
