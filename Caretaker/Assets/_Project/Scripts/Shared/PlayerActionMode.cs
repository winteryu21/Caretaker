namespace Caretaker.Shared
{
    /// <summary>
    /// 로컬 플레이어 입력이 사용하는 행동 모드입니다.
    /// </summary>
    public enum PlayerActionMode
    {
        /// <summary>습득, 조작, 조사, 아이템 사용이 가능한 모드입니다.</summary>
        Investigation = 0,

        /// <summary>전투 입력만 전달하는 모드입니다.</summary>
        Combat = 1
    }
}
