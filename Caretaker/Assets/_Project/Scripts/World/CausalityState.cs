using System;
using System.Collections.Generic;

namespace Caretaker.World
{
    /// <summary>
    /// 활성화된 Rule ID, Receiver 상태, 완료한 Major Interaction 목록을 저장한다.
    /// 체크포인트 스냅샷과 네트워크 payload로 재사용된다.
    /// </summary>
    /// <remarks>DSD §4.3 — 런타임 상태 스키마</remarks>
    [Serializable]
    public class CausalityState
    {
        /// <summary>적용 완료된 인과 규칙 ID 목록.</summary>
        public List<string> AppliedRuleIds = new();

        /// <summary>완료된 Major Interaction ID 목록.</summary>
        public List<string> CompletedMajorIds = new();
    }
}
