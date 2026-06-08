namespace Caretaker.Shared
{
    /// <summary>
    /// 플레이어의 마우스 입력 해석 모드.
    /// </summary>
    public enum PlayerControlMode
    {
        /// <summary>좌클릭은 조사/획득, 우클릭은 아이템 사용으로 처리한다.</summary>
        Normal,

        /// <summary>좌클릭은 공격, 우클릭은 조준으로 처리한다.</summary>
        Combat
    }
}
