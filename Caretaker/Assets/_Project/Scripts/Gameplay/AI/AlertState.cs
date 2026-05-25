using System;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 방별 경보 상태와 만료 시간을 저장하는 런타임 상태.
    /// </summary>
    /// <remarks>DSD §4.3 — 런타임 상태 스키마</remarks>
    [Serializable]
    public class AlertState
    {
        public string RoomId;
        public int AlertLevel;
        public float ExpiresAtTick;
    }
}
