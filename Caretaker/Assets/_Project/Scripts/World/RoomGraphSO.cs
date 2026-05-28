using UnityEngine;

using Caretaker.Shared;

namespace Caretaker.World
{
    /// <summary>
    /// 룸 ID, 인접 룸, 시간대별 대응 룸, 스폰 위치를 정의하는 데이터 에셋.
    /// </summary>
    /// <remarks>DSD §4.2 — ScriptableObject 스키마</remarks>
    [CreateAssetMenu(fileName = "SO_RoomGraph", menuName = "Caretaker SO/Room/RoomGraph")]
    public class RoomGraphSO : ScriptableObject
    {
        [SerializeField] private string _roomId;
        [SerializeField] private string _displayName;
        [SerializeField] private TimelineRole _timeline = TimelineRole.None;
        [SerializeField] private string[] _adjacentRoomIds;
        [SerializeField] private string _pairedTimelineRoomId;
        [SerializeField] private string[] _spawnPointIds;

        /// <summary>룸 고유 식별자. (예: PAST_LAB_A01)</summary>
        public string RoomId => _roomId;

        /// <summary>팀 커뮤니케이션과 UI 표시용 룸 이름.</summary>
        public string DisplayName => _displayName;

        /// <summary>이 룸이 속한 시간대.</summary>
        public TimelineRole Timeline => _timeline;

        /// <summary>인접 룸 ID 목록.</summary>
        public string[] AdjacentRoomIds => _adjacentRoomIds;

        /// <summary>대응하는 다른 시간대 룸 ID.</summary>
        public string PairedTimelineRoomId => _pairedTimelineRoomId;

        /// <summary>스폰 포인트 ID 목록.</summary>
        public string[] SpawnPointIds => _spawnPointIds;
    }
}
