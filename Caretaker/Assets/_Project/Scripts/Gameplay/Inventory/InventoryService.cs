namespace Caretaker.Gameplay
{
    /// <summary>
    /// 아이템 획득, 선택, 사용, 소모 규칙을 판정한다.
    /// MonoBehaviour에 의존하지 않는 순수 C# 클래스.
    /// </summary>
    /// <remarks>
    /// DSD §3.7 — 인벤토리 시스템
    /// 계층: Domain Service
    /// </remarks>
    public class InventoryService
    {
        /// <summary>
        /// 아이템 획득을 시도한다. 중복/소지 제한을 확인한다.
        /// </summary>
        /// <param name="playerId">플레이어 식별자.</param>
        /// <param name="itemId">획득할 아이템 ID.</param>
        /// <returns>획득 성공 여부.</returns>
        public bool AcquireItem(ulong playerId, string itemId)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 아이템을 대상에 사용한다. 대상 태그와 필요 조건을 검증한다.
        /// </summary>
        /// <param name="playerId">플레이어 식별자.</param>
        /// <param name="itemId">사용할 아이템 ID.</param>
        /// <param name="targetId">사용 대상 ID.</param>
        /// <returns>사용 성공 여부.</returns>
        public bool UseItemOnTarget(ulong playerId, string itemId, string targetId)
        {
            throw new System.NotImplementedException();
        }
    }
}
