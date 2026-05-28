using System.Collections.Generic;
using System.Linq;

namespace Caretaker.World
{
    /// <summary>
    /// 현재 룸, 방문 룸, 인접 룸 계산을 담당한다.
    /// MonoBehaviour에 의존하지 않는 순수 C# 클래스.
    /// </summary>
    /// <remarks>
    /// DSD §3.4 — 룸 / 카메라 / 미니맵 시스템
    /// 계층: Domain Service
    /// </remarks>
    public class RoomService
    {
        private static readonly string[] EMPTY_ROOM_IDS = { };

        private readonly Dictionary<string, RoomGraphSO> _roomsById;

        /// <summary>
        /// 빈 룸 그래프 서비스 인스턴스를 생성한다.
        /// </summary>
        public RoomService()
            : this(Enumerable.Empty<RoomGraphSO>())
        {
        }

        /// <summary>
        /// 룸 그래프 데이터 에셋 목록을 기반으로 서비스 인스턴스를 생성한다.
        /// </summary>
        /// <param name="roomGraphs">조회할 룸 그래프 데이터 에셋 목록.</param>
        public RoomService(IEnumerable<RoomGraphSO> roomGraphs)
        {
            _roomsById = roomGraphs
                .Where(roomGraph => roomGraph != null && !string.IsNullOrWhiteSpace(roomGraph.RoomId))
                .GroupBy(roomGraph => roomGraph.RoomId)
                .ToDictionary(group => group.Key, group => group.First());
        }

        /// <summary>
        /// 룸 ID가 그래프에 존재하는지 확인한다.
        /// </summary>
        /// <param name="roomId">확인할 룸 ID.</param>
        /// <returns>룸이 존재하면 true, 아니면 false.</returns>
        public bool ContainsRoom(string roomId)
        {
            return !string.IsNullOrWhiteSpace(roomId) && _roomsById.ContainsKey(roomId);
        }

        /// <summary>
        /// 룸 그래프에서 룸 데이터 에셋을 조회한다.
        /// </summary>
        /// <param name="roomId">조회할 룸 ID.</param>
        /// <returns>룸 데이터 에셋. 존재하지 않으면 null.</returns>
        public RoomGraphSO GetRoom(string roomId)
        {
            if (string.IsNullOrWhiteSpace(roomId))
            {
                return null;
            }

            return _roomsById.TryGetValue(roomId, out RoomGraphSO roomGraph) ? roomGraph : null;
        }

        /// <summary>
        /// 룸 그래프에서 인접 룸 ID 목록을 조회한다.
        /// </summary>
        /// <param name="roomId">기준 룸 ID.</param>
        /// <returns>인접 룸 ID 목록.</returns>
        public IReadOnlyList<string> GetAdjacentRooms(string roomId)
        {
            RoomGraphSO roomGraph = GetRoom(roomId);
            return roomGraph != null ? roomGraph.AdjacentRoomIds ?? EMPTY_ROOM_IDS : EMPTY_ROOM_IDS;
        }

        /// <summary>
        /// 병렬 시간대의 대응 룸 ID를 조회한다.
        /// </summary>
        /// <param name="roomId">기준 룸 ID.</param>
        /// <returns>대응 룸 ID. 존재하지 않으면 빈 문자열.</returns>
        public string GetPairedTimelineRoomId(string roomId)
        {
            RoomGraphSO roomGraph = GetRoom(roomId);
            return roomGraph != null ? roomGraph.PairedTimelineRoomId ?? string.Empty : string.Empty;
        }
    }
}
