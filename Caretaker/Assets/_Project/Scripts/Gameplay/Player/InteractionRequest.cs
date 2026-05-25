using UnityEngine;

/// <summary>
/// 플레이어 상호작용을 처리하는 데 필요한 데이터를 전달합니다.
/// </summary>
public readonly struct InteractionRequest
{
    /// <summary>
    /// 새 상호작용 요청을 생성합니다.
    /// </summary>
    /// <param name="mode">실행할 상호작용 방식입니다.</param>
    /// <param name="pointerScreenPosition">화면 좌표계 기준 포인터 위치입니다.</param>
    /// <param name="selectedItemId">선택된 인벤토리 아이템 ID입니다. 없으면 null입니다.</param>
    public InteractionRequest(InteractionMode mode, Vector2 pointerScreenPosition, string selectedItemId)
    {
        Mode = mode;
        PointerScreenPosition = pointerScreenPosition;
        SelectedItemId = selectedItemId;
    }

    /// <summary>
    /// 실행할 상호작용 방식을 반환합니다.
    /// </summary>
    public InteractionMode Mode { get; }

    /// <summary>
    /// 화면 좌표계 기준 포인터 위치를 반환합니다.
    /// </summary>
    public Vector2 PointerScreenPosition { get; }

    /// <summary>
    /// 선택된 인벤토리 아이템 ID를 반환합니다.
    /// </summary>
    public string SelectedItemId { get; }
}
