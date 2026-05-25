namespace Caretaker.Shared
{
    /// <summary>
    /// 플레이어의 게임 시간대 역할을 정의한다.
    /// 네트워크 역할(Host/Client)과 독립적이다.
    /// </summary>
    /// <remarks>DSD §1.2 — 시간대 역할</remarks>
    public enum TimelineRole
    {
        None = 0,
        Past = 1,
        Future = 2
    }
}
