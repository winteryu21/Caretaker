namespace Caretaker.Shared
{
    /// <summary>
    /// 플레이어 상호작용 요청이 어떤 방식으로 처리되어야 하는지 정의합니다.
    /// </summary>
    public enum InteractionType
    {
        /// <summary>마우스 클릭, 아이템 미선택 → 정보 UI 표시</summary>
        Examine,
        
        Acquire,

        UseItem,

        /// <summary>E키, 반경 1 unit 내 문/레버 등 → Host 검증 후 조작 이벤트</summary>
        Operate
    }
}
