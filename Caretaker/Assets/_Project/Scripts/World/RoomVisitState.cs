using System;
using System.Collections.Generic;

namespace Caretaker.World
{
    /// <summary>
    /// 플레이어별 방문 룸 기록을 저장하는 런타임 상태.
    /// 개인 미니맵과 체크포인트에 사용된다.
    /// </summary>
    /// <remarks>DSD §4.3 — 런타임 상태 스키마</remarks>
    [Serializable]
    public class RoomVisitState
    {
        public ulong PlayerId;
        public List<string> VisitedRoomIds = new();
        public string CurrentRoomId;
    }
}
