using System;

namespace Caretaker.Shared
{
    /// <summary>
    /// 플레이어 상호작용 요청이 어떤 방식으로 처리되어야 하는지 정의합니다.
    /// </summary>
    [Flags]
    public enum InteractionType
    {
        None = 0,

        /// <summary>마우스 좌클릭으로 조사 텍스트나 반응을 확인합니다.</summary>
        Examine = 1 << 0,

        /// <summary>E키로 가까운 획득 가능한 오브젝트를 습득합니다.</summary>
        Acquire = 1 << 1,

        /// <summary>E키로 가까운 조작 오브젝트를 즉시 작동합니다.</summary>
        Operate = 1 << 2
    }
}
