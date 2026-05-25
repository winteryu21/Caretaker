namespace Caretaker.Shared
{
    /// <summary>
    /// 플레이어 상호작용 요청을 어떤 방식으로 처리할지 정의한다.
    /// </summary>
    /// <remarks>DSD §3.3 — 상호작용 모드</remarks>
    public enum InteractionType
    {
        /// <summary>마우스 클릭, 아이템 미선택 → 정보 UI 표시</summary>
        Examine,

        /// <summary>마우스 클릭, 아이템 선택 → Host 검증 후 상태 변화</summary>
        UseItem,

        /// <summary>E키, 반경 1 unit 내 문/레버 등 → Host 검증 후 조작 이벤트</summary>
        Operate
    }
}
