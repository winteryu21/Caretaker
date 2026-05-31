using System;

namespace Caretaker.Core
{
    /// <summary>
    /// 공동 실패 시 복원에 사용되는 전체 게임 상태 스냅샷.
    /// 플레이어, 인벤토리, 인과, 룸, AI 상태를 포함한다.
    /// </summary>
    /// <remarks>DSD §4.3 — 런타임 상태 스키마 / §3.9 체크포인트 복귀</remarks>
    [Serializable]
    public class CheckpointSnapshot
    {
        public string CheckpointId;
        public int SnapshotVersion;

        // TODO [Checkpoint]: InteractableObject의 isRequiredItemSatisfied/isItemAcquired 상태를 objectId 기준으로 저장한다.
    }
}
