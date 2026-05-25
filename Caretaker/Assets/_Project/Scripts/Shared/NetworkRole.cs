namespace Caretaker.Shared
{
    /// <summary>
    /// 세션 권한 역할을 정의한다.
    /// 시간대 역할(Past/Future)과 독립적이다.
    /// </summary>
    /// <remarks>DSD §1.2 — 네트워크 역할</remarks>
    public enum NetworkRole
    {
        Host,
        Client
    }
}
