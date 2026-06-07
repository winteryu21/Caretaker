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
        /// <summary>적용된 규칙 ID. (예: CR_P1_POWER_LEVER)</summary>
        public string RuleId;

        /// <summary>Receiver별 상태 변경 효과 목록.</summary>
        public ReceiverEffect[] Effects;

        /// <summary>Major Interaction 진행 갱신 여부.</summary>
        public bool IsMajorProgress;

        /// <summary>규칙이 성공적으로 적용되었는지 여부.</summary>
        public bool Success;

        /// <summary>실패 시 거부 사유.</summary>
        public string RejectionReason;

        /// <summary>
        /// 단일 Receiver에 적용할 상태 변경을 정의한다.
        /// CausalRuleSO.CausalReceiverEffect와 1:1 대응한다.
        /// </summary>
        [Serializable]
        public struct ReceiverEffect
        {
            /// <summary>변경 대상 리시버 ID. (예: RCV_P1_BREAKER_PANEL)</summary>
            public string ReceiverId;

            /// <summary>리시버에 적용할 상태 키. (예: powerState)</summary>
            public string StateKey;

            /// <summary>리시버에 적용할 상태 값. (예: Powered)</summary>
            public string StateValue;
        }
    }
}
