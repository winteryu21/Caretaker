using System;

namespace Caretaker.Shared
{
    /// <summary>
    /// 플레이어가 요청할 수 있는 상호작용 타입을 정의한다.
    /// </summary>
    [Flags]
    public enum InteractionType
    {
        None = 0,

        /// <summary>포인터로 오브젝트를 조사한다.</summary>
        Examine = 1 << 0,

        /// <summary>가까운 아이템 오브젝트를 획득한다.</summary>
        Acquire = 1 << 1,

        /// <summary>가까운 오브젝트를 즉시 조작한다.</summary>
        Operate = 1 << 2,

        /// <summary>현재 선택한 인벤토리 아이템을 가까운 대상에 사용한다.</summary>
        UseItem = 1 << 3
    }
}
