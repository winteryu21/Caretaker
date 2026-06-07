using Caretaker.Gameplay;

namespace Caretaker.World
{
    /// <summary>
    /// 일반 조작 상호작용이 실행할 기능의 공통 계약입니다.
    /// </summary>
    public interface IOperateAction
    {
        /// <summary>
        /// 플레이어의 일반 조작 요청을 실행합니다.
        /// </summary>
        /// <param name="actor">조작을 실행한 플레이어입니다.</param>
        /// <returns>조작이 실행되었는지 여부입니다.</returns>
        bool Execute(PlayerController actor);
    }
}
