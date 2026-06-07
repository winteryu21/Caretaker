using System;
using System.Collections.Generic;
using System.Linq;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 방 경보 상태 저장과 감쇠를 계산한다.
    /// </summary>
    /// <remarks>
    /// DSD 3.5 AI / 경보 시스템 도메인 서비스.
    /// </remarks>
    public class AlertService
    {
        private static readonly string[] EMPTY_ROOM_IDS = { };

        private readonly Dictionary<string, AlertEntry> _alertsByRoomId = new();

        /// <summary>
        /// 발생 방과 인접 방에 Alert 경보를 발생시킨다.
        /// </summary>
        /// <param name="sourceRoomId">경보가 발생한 방 ID.</param>
        /// <param name="adjacentRoomIds">경보를 함께 전파할 인접 방 ID 목록.</param>
        /// <param name="alertDuration">초 단위 경보 지속 시간.</param>
        /// <param name="currentTime">초 단위 현재 게임 시간.</param>
        /// <returns>발생 방을 포함한 영향 방 ID 목록.</returns>
        public IReadOnlyList<string> RaiseAlert(
            string sourceRoomId,
            IEnumerable<string> adjacentRoomIds,
            float alertDuration,
            float currentTime)
        {
            return RaiseAlert(sourceRoomId, adjacentRoomIds, AlertState.Alert, alertDuration, currentTime);
        }

        /// <summary>
        /// 발생 방과 인접 방에 지정한 경보 상태를 발생시킨다.
        /// </summary>
        /// <param name="sourceRoomId">경보가 발생한 방 ID.</param>
        /// <param name="adjacentRoomIds">경보를 함께 전파할 인접 방 ID 목록.</param>
        /// <param name="alertState">적용할 경보 상태.</param>
        /// <param name="alertDuration">초 단위 경보 지속 시간.</param>
        /// <param name="currentTime">초 단위 현재 게임 시간.</param>
        /// <returns>발생 방을 포함한 영향 방 ID 목록.</returns>
        public IReadOnlyList<string> RaiseAlert(
            string sourceRoomId,
            IEnumerable<string> adjacentRoomIds,
            AlertState alertState,
            float alertDuration,
            float currentTime)
        {
            if (string.IsNullOrWhiteSpace(sourceRoomId) || alertState == AlertState.None)
            {
                return EMPTY_ROOM_IDS;
            }

            IReadOnlyList<string> affectedRoomIds = BuildAffectedRoomIds(sourceRoomId, adjacentRoomIds);
            float expiresAtTime = currentTime + Math.Max(0f, alertDuration);

            foreach (string roomId in affectedRoomIds)
            {
                _alertsByRoomId[roomId] = new AlertEntry(alertState, expiresAtTime);
            }

            return affectedRoomIds;
        }

        /// <summary>
        /// 만료 시간이 지난 경보를 해제한다.
        /// </summary>
        /// <param name="currentTime">초 단위 현재 게임 시간.</param>
        /// <returns>경보가 None으로 감쇠된 방 ID 목록.</returns>
        public IReadOnlyList<string> Tick(float currentTime)
        {
            List<string> expiredRoomIds = new();

            foreach (KeyValuePair<string, AlertEntry> alertByRoomId in _alertsByRoomId.ToArray())
            {
                if (currentTime < alertByRoomId.Value.ExpiresAtTime)
                {
                    continue;
                }

                expiredRoomIds.Add(alertByRoomId.Key);
                _alertsByRoomId.Remove(alertByRoomId.Key);
            }

            return expiredRoomIds;
        }

        /// <summary>
        /// 방의 활성 경보 상태를 조회한다.
        /// </summary>
        /// <param name="roomId">조회할 방 ID.</param>
        /// <returns>현재 경보 상태. 비활성 상태면 None.</returns>
        public AlertState GetAlertState(string roomId)
        {
            if (string.IsNullOrWhiteSpace(roomId))
            {
                return AlertState.None;
            }

            return _alertsByRoomId.TryGetValue(roomId, out AlertEntry entry)
                ? entry.AlertState
                : AlertState.None;
        }

        /// <summary>
        /// 모든 활성 경보 방 ID를 조회한다.
        /// </summary>
        /// <returns>활성 경보 방 ID 목록.</returns>
        public IReadOnlyList<string> GetActiveAlertRoomIds()
        {
            return _alertsByRoomId.Keys.ToArray();
        }

        private static IReadOnlyList<string> BuildAffectedRoomIds(string sourceRoomId, IEnumerable<string> adjacentRoomIds)
        {
            List<string> affectedRoomIds = new() { sourceRoomId };

            if (adjacentRoomIds == null)
            {
                return affectedRoomIds;
            }

            foreach (string adjacentRoomId in adjacentRoomIds)
            {
                if (!string.IsNullOrWhiteSpace(adjacentRoomId) && !affectedRoomIds.Contains(adjacentRoomId))
                {
                    affectedRoomIds.Add(adjacentRoomId);
                }
            }

            return affectedRoomIds;
        }

        private readonly struct AlertEntry
        {
            public AlertEntry(AlertState alertState, float expiresAtTime)
            {
                AlertState = alertState;
                ExpiresAtTime = expiresAtTime;
            }

            public AlertState AlertState { get; }

            public float ExpiresAtTime { get; }
        }
    }
}
