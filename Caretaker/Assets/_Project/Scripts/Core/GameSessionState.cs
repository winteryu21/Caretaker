using System;

using Caretaker.Shared;

namespace Caretaker.Core
{
    /// <summary>
    /// 현재 Phase, 체크포인트, 세션 상태를 저장하는 런타임 상태.
    /// 세션 복구 기준으로 사용된다.
    /// </summary>
    /// <remarks>DSD §4.3 — 런타임 상태 스키마</remarks>
    [Serializable]
    public class GameSessionState
    {
        public PhaseId PhaseId;
        public string CheckpointId;
        public string SessionStatus;
    }
}
