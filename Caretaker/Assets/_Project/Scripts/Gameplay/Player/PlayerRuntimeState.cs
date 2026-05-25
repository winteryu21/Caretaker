using System;

using UnityEngine;

using Caretaker.Shared;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 플레이어 복원과 UI 표시에 사용되는 런타임 상태.
    /// </summary>
    /// <remarks>DSD §4.3 — 런타임 상태 스키마</remarks>
    [Serializable]
    public class PlayerRuntimeState
    {
        public ulong PlayerId;
        public TimelineRole TimelineRole;
        public string CurrentRoomId;
        public Vector2 Position;
        public bool IsCrouching;
        public bool IsCaught;
    }
}
