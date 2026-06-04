using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Caretaker.Gameplay;

namespace Caretaker.World
{
    /// <summary>
    /// 룸 전환 이벤트 발행, 룸 활성화/비활성화, 스폰 위치 제공을 담당한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.4 — 룸 / 카메라 / 미니맵 시스템
    /// 계층: Unity Component
    /// </remarks>
    public class RoomManager : MonoBehaviour
    {
        private static readonly string[] EMPTY_ROOM_IDS = { };

        // 1. Serialize 필드
        [SerializeField] private RoomGraphSO[] _roomGraphs;
        [SerializeField] [Min(0f)] private float _alertDurationSeconds = 30f;

        // 2. private 필드
        private readonly AlertService _alertService = new();
        private readonly Dictionary<ulong, RoomVisitState> _roomStatesByPlayer = new();
        private RoomService _roomService = new();

        /// <summary>룸 전환 시 발생한다. (playerId, newRoomId)</summary>
        public event Action<ulong, string> OnRoomChanged;

        /// <summary>플레이어가 새 룸을 처음 방문할 때 발생한다. (playerId, roomId)</summary>
        public event Action<ulong, string> OnRoomVisited;

        /// <summary>활성 경보 방 목록이 변경될 때 발생한다.</summary>
        public event Action<IReadOnlyList<string>> OnAlertRoomsChanged;

        // 3. Unity 생명주기
        private void Awake()
        {
            RebuildRoomService();
        }

        private void Update()
        {
            TickRoomAlerts(Time.time);
        }

        /// <summary>
        /// 룸 그래프 데이터 목록을 설정하고 룸 조회 서비스를 갱신한다.
        /// </summary>
        /// <param name="roomGraphs">사용할 룸 그래프 데이터 목록.</param>
        public void ConfigureRoomGraphs(IEnumerable<RoomGraphSO> roomGraphs)
        {
            _roomGraphs = roomGraphs != null
                ? roomGraphs.Where(roomGraph => roomGraph != null).ToArray()
                : Array.Empty<RoomGraphSO>();

            RebuildRoomService();
        }

        /// <summary>
        /// 플레이어의 룸 진입을 처리한다.
        /// </summary>
        /// <param name="playerId">진입한 플레이어 ID.</param>
        /// <param name="roomId">진입한 룸 ID.</param>
        public void ReportRoomEnter(ulong playerId, string roomId)
        {
            if (!CanTrackRoom(roomId))
            {
                return;
            }

            RoomVisitState roomState = GetOrCreateRoomState(playerId);
            if (roomState.CurrentRoomId == roomId)
            {
                return;
            }

            roomState.CurrentRoomId = roomId;

            bool isFirstVisit = !roomState.VisitedRoomIds.Contains(roomId);
            if (isFirstVisit)
            {
                roomState.VisitedRoomIds.Add(roomId);
            }

            OnRoomChanged?.Invoke(playerId, roomId);

            if (isFirstVisit)
            {
                OnRoomVisited?.Invoke(playerId, roomId);
            }
        }

        /// <summary>
        /// 플레이어의 룸 이탈을 처리한다.
        /// </summary>
        /// <param name="playerId">이탈한 플레이어 ID.</param>
        /// <param name="roomId">이탈한 룸 ID.</param>
        public void ReportRoomExit(ulong playerId, string roomId)
        {
            if (string.IsNullOrWhiteSpace(roomId)
                || !_roomStatesByPlayer.TryGetValue(playerId, out RoomVisitState roomState)
                || roomState.CurrentRoomId != roomId)
            {
                return;
            }

            roomState.CurrentRoomId = string.Empty;
        }

        /// <summary>
        /// 플레이어의 현재 룸 ID를 조회한다.
        /// </summary>
        /// <param name="playerId">조회할 플레이어 ID.</param>
        /// <returns>현재 룸 ID. 없으면 빈 문자열.</returns>
        public string GetCurrentRoomId(ulong playerId)
        {
            return _roomStatesByPlayer.TryGetValue(playerId, out RoomVisitState roomState)
                ? roomState.CurrentRoomId ?? string.Empty
                : string.Empty;
        }

        /// <summary>
        /// 플레이어의 현재 룸 ID를 조회한다.
        /// </summary>
        /// <param name="playerId">조회할 플레이어 ID.</param>
        /// <param name="roomId">현재 룸 ID.</param>
        /// <returns>현재 룸이 있으면 true.</returns>
        public bool TryGetCurrentRoomId(ulong playerId, out string roomId)
        {
            roomId = GetCurrentRoomId(playerId);
            return !string.IsNullOrWhiteSpace(roomId);
        }

        /// <summary>
        /// 플레이어의 방문 룸 ID 목록을 조회한다.
        /// </summary>
        /// <param name="playerId">조회할 플레이어 ID.</param>
        /// <returns>방문한 룸 ID 목록.</returns>
        public IReadOnlyList<string> GetVisitedRooms(ulong playerId)
        {
            return _roomStatesByPlayer.TryGetValue(playerId, out RoomVisitState roomState)
                ? roomState.VisitedRoomIds
                : EMPTY_ROOM_IDS;
        }

        /// <summary>
        /// 룸 그래프에서 인접 룸 ID 목록을 조회한다.
        /// </summary>
        /// <param name="roomId">기준 룸 ID.</param>
        /// <returns>인접 룸 ID 목록.</returns>
        public IReadOnlyList<string> GetAdjacentRooms(string roomId)
        {
            return _roomService.GetAdjacentRooms(roomId);
        }

        /// <summary>
        /// 발생 방과 인접 방에 Alert 경보를 발생시킨다.
        /// </summary>
        /// <param name="sourceRoomId">경보가 발생한 방 ID.</param>
        /// <returns>발생 방을 포함한 영향 방 ID 목록.</returns>
        public IReadOnlyList<string> RaiseRoomAlert(string sourceRoomId)
        {
            return RaiseRoomAlert(sourceRoomId, AlertState.Alert, _alertDurationSeconds, Time.time);
        }

        /// <summary>
        /// 발생 방과 인접 방에 지정한 경보 상태를 발생시킨다.
        /// </summary>
        /// <param name="sourceRoomId">경보가 발생한 방 ID.</param>
        /// <param name="alertState">적용할 경보 상태.</param>
        /// <param name="alertDuration">초 단위 경보 지속 시간.</param>
        /// <param name="currentTime">초 단위 현재 게임 시간.</param>
        /// <returns>발생 방을 포함한 영향 방 ID 목록.</returns>
        public IReadOnlyList<string> RaiseRoomAlert(
            string sourceRoomId,
            AlertState alertState,
            float alertDuration,
            float currentTime)
        {
            if (!CanTrackRoom(sourceRoomId))
            {
                return EMPTY_ROOM_IDS;
            }

            IReadOnlyList<string> affectedRoomIds = _alertService.RaiseAlert(
                sourceRoomId,
                GetAdjacentRooms(sourceRoomId),
                alertState,
                alertDuration,
                currentTime);

            if (affectedRoomIds.Count > 0)
            {
                PublishAlertRoomsChanged();
            }

            return affectedRoomIds;
        }

        /// <summary>
        /// 만료 시간이 지난 방 경보를 감쇠시킨다.
        /// </summary>
        /// <param name="currentTime">초 단위 현재 게임 시간.</param>
        /// <returns>경보가 None으로 감쇠된 방 ID 목록.</returns>
        public IReadOnlyList<string> TickRoomAlerts(float currentTime)
        {
            IReadOnlyList<string> expiredRoomIds = _alertService.Tick(currentTime);
            if (expiredRoomIds.Count > 0)
            {
                PublishAlertRoomsChanged();
            }

            return expiredRoomIds;
        }

        /// <summary>
        /// 방의 활성 경보 상태를 조회한다.
        /// </summary>
        /// <param name="roomId">조회할 방 ID.</param>
        /// <returns>현재 경보 상태. 비활성 상태면 None.</returns>
        public AlertState GetRoomAlertState(string roomId)
        {
            return _alertService.GetAlertState(roomId);
        }

        /// <summary>
        /// 모든 활성 경보 방 ID를 조회한다.
        /// </summary>
        /// <returns>활성 경보 방 ID 목록.</returns>
        public IReadOnlyList<string> GetActiveAlertRoomIds()
        {
            return _alertService.GetActiveAlertRoomIds();
        }

        /// <summary>
        /// 룸 그래프에 룸 ID가 존재하는지 확인한다.
        /// </summary>
        /// <param name="roomId">확인할 룸 ID.</param>
        /// <returns>룸 ID가 존재하면 true.</returns>
        public bool ContainsRoom(string roomId)
        {
            return _roomService.ContainsRoom(roomId);
        }

        // private 메서드
        private void RebuildRoomService()
        {
            _roomService = new RoomService(_roomGraphs ?? Array.Empty<RoomGraphSO>());
        }

        private bool CanTrackRoom(string roomId)
        {
            if (string.IsNullOrWhiteSpace(roomId))
            {
                Debug.LogWarning("RoomManager ignored an empty room ID.", this);
                return false;
            }

            if (_roomGraphs is { Length: > 0 } && !_roomService.ContainsRoom(roomId))
            {
                Debug.LogWarning($"RoomManager ignored unknown room ID '{roomId}'.", this);
                return false;
            }

            return true;
        }

        private void PublishAlertRoomsChanged()
        {
            OnAlertRoomsChanged?.Invoke(GetActiveAlertRoomIds());
        }

        private RoomVisitState GetOrCreateRoomState(ulong playerId)
        {
            if (_roomStatesByPlayer.TryGetValue(playerId, out RoomVisitState roomState))
            {
                return roomState;
            }

            roomState = new RoomVisitState
            {
                PlayerId = playerId,
                CurrentRoomId = string.Empty
            };
            _roomStatesByPlayer.Add(playerId, roomState);

            return roomState;
        }
    }
}
