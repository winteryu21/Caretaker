using System;

namespace Caretaker.Shared
{
    /// <summary>
    /// 인과 규칙 실행 결과를 전달하는 데이터 구조체.
    /// CausalityService가 생성하고, CausalityManager와 GameFlowManager가 소비한다.
    /// </summary>
    /// <remarks>DSD §3.1 — SubmitTrigger 출력</remarks>
    [Serializable]
    public struct CausalResult
    {
        /// <summary>변경 대상 리시버 ID.</summary>
        public string ReceiverId;

        /// <summary>리시버에 적용할 상태 키.</summary>
        public string StateKey;

        /// <summary>리시버에 적용할 상태 값.</summary>
        public string StateValue;

        /// <summary>Major Interaction 진행 갱신 여부.</summary>
        public bool IsMajorProgress;

        /// <summary>규칙이 성공적으로 적용되었는지 여부.</summary>
        public bool Success;

        /// <summary>실패 시 거부 사유.</summary>
        public string RejectionReason;
    }
}
