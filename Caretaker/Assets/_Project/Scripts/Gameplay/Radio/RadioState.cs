namespace Caretaker.Gameplay
{
    /// <summary>
    /// 로컬 플레이어 기준 무전기 표시 상태를 정의한다.
    /// </summary>
    /// <remarks>DSD §3.6 — 무전기 통신 시스템</remarks>
    public enum RadioState
    {
        Idle = 0,
        Transmitting = 1,
        Receiving = 2,
        Blocked = 3
    }
}
