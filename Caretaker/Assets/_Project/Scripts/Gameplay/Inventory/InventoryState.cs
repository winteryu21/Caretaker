using System;
using System.Collections.Generic;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 플레이어별 아이템 목록, 선택 아이템, 소모 아이템을 저장하는 런타임 상태.
    /// </summary>
    /// <remarks>DSD §4.3 — 런타임 상태 스키마</remarks>
    [Serializable]
    public class InventoryState
    {
        public ulong PlayerId;
        public List<string> OwnedItemIds = new();
        public string SelectedItemId;
        public List<string> ConsumedItemIds = new();
    }
}
