using UnityEngine;

namespace Caretaker.World
{
    /// <summary>
    /// 룸 ID, 인접 룸, 시간대별 대응 룸, 스폰 위치를 정의하는 데이터 에셋.
    /// </summary>
    /// <remarks>DSD §4.2 — ScriptableObject 스키마</remarks>
    [CreateAssetMenu(fileName = "SO_RoomGraph", menuName = "Caretaker/Room/RoomGraph")]
    public class RoomGraphSO : ScriptableObject
    {
        [SerializeField] private string _roomId;
        [SerializeField] private string _timeline;
        [SerializeField] private string[] _adjacentRoomIds;
        [SerializeField] private string _pairedTimelineRoomId;
        [SerializeField] private string[] _spawnPointIds;

        /// <summary>룸 고유 식별자. (예: PAST_LAB_A01)</summary>
        public string RoomId => _roomId;

        /// <summary>이 룸이 속한 시간대.</summary>
        public string Timeline => _timeline;

        /// <summary>인접 룸 ID 목록.</summary>
        public string[] AdjacentRoomIds => _adjacentRoomIds;

        /// <summary>대응하는 다른 시간대 룸 ID.</summary>
        public string PairedTimelineRoomId => _pairedTimelineRoomId;

        /// <summary>스폰 포인트 ID 목록.</summary>
        public string[] SpawnPointIds => _spawnPointIds;
    }
}
