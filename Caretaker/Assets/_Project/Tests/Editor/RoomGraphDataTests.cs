using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using UnityEditor;

using Caretaker.World;

namespace Caretaker.Tests.Editor
{
    public class RoomGraphDataTests
    {
        private const string ROOM_DATA_FOLDER = "Assets/_Project/Data/Rooms";
        private const int EXPECTED_ROOM_COUNT = 34;

        [Test]
        public void RoomGraphAssets_HaveExpectedCount()
        {
            IReadOnlyList<RoomGraphSO> roomGraphs = LoadRoomGraphs();

            Assert.That(roomGraphs.Count, Is.EqualTo(EXPECTED_ROOM_COUNT));
        }

        [Test]
        public void RoomGraphAssets_ReferenceExistingAdjacentRooms()
        {
            IReadOnlyList<RoomGraphSO> roomGraphs = LoadRoomGraphs();
            RoomService roomService = new(roomGraphs);

            foreach (RoomGraphSO roomGraph in roomGraphs)
            {
                foreach (string adjacentRoomId in roomGraph.AdjacentRoomIds)
                {
                    Assert.That(
                        roomService.ContainsRoom(adjacentRoomId),
                        Is.True,
                        $"{roomGraph.RoomId} references missing adjacent room {adjacentRoomId}.");
                }
            }
        }

        [Test]
        public void RoomGraphAssets_HaveBidirectionalAdjacentRooms()
        {
            IReadOnlyList<RoomGraphSO> roomGraphs = LoadRoomGraphs();
            RoomService roomService = new(roomGraphs);

            foreach (RoomGraphSO roomGraph in roomGraphs)
            {
                foreach (string adjacentRoomId in roomGraph.AdjacentRoomIds)
                {
                    Assert.That(
                        roomService.GetAdjacentRooms(adjacentRoomId),
                        Does.Contain(roomGraph.RoomId),
                        $"{roomGraph.RoomId} -> {adjacentRoomId} is not bidirectional.");
                }
            }
        }

        [Test]
        public void RoomGraphAssets_HaveReciprocalPairedTimelineRooms()
        {
            IReadOnlyList<RoomGraphSO> roomGraphs = LoadRoomGraphs();
            RoomService roomService = new(roomGraphs);

            foreach (RoomGraphSO roomGraph in roomGraphs)
            {
                string pairedRoomId = roomGraph.PairedTimelineRoomId;

                Assert.That(
                    roomService.ContainsRoom(pairedRoomId),
                    Is.True,
                    $"{roomGraph.RoomId} references missing paired room {pairedRoomId}.");
                Assert.That(
                    roomService.GetPairedTimelineRoomId(pairedRoomId),
                    Is.EqualTo(roomGraph.RoomId),
                    $"{roomGraph.RoomId} and {pairedRoomId} are not reciprocal paired rooms.");
            }
        }

        [Test]
        public void RoomGraphAssets_HaveDefaultSpawnPointIds()
        {
            IReadOnlyList<RoomGraphSO> roomGraphs = LoadRoomGraphs();

            foreach (RoomGraphSO roomGraph in roomGraphs)
            {
                Assert.That(roomGraph.SpawnPointIds, Is.Not.Empty, $"{roomGraph.RoomId} has no spawn point IDs.");
                Assert.That(
                    roomGraph.SpawnPointIds.All(spawnPointId => !string.IsNullOrWhiteSpace(spawnPointId)),
                    Is.True,
                    $"{roomGraph.RoomId} has an empty spawn point ID.");
            }
        }

        [Test]
        public void RoomService_UnknownRoom_ReturnsEmptyResults()
        {
            RoomService roomService = new(LoadRoomGraphs());

            Assert.That(roomService.ContainsRoom("UNKNOWN_ROOM"), Is.False);
            Assert.That(roomService.GetRoom("UNKNOWN_ROOM"), Is.Null);
            Assert.That(roomService.GetAdjacentRooms("UNKNOWN_ROOM"), Is.Empty);
            Assert.That(roomService.GetPairedTimelineRoomId("UNKNOWN_ROOM"), Is.Empty);
        }

        [Test]
        public void RoomService_ReturnsBHallConnections()
        {
            RoomService roomService = new(LoadRoomGraphs());

            Assert.That(
                roomService.GetAdjacentRooms("PAST_A_TO_B_SKYBRIDGE"),
                Does.Contain("PAST_B_2F_HALL"));
            Assert.That(
                roomService.GetAdjacentRooms("PAST_B_2F_HALL"),
                Is.EquivalentTo(new[] { "PAST_A_TO_B_SKYBRIDGE", "PAST_B_2F_ARCHIVE", "PAST_B_1F_HALL" }));
            Assert.That(
                roomService.GetAdjacentRooms("PAST_B_1F_HALL"),
                Is.EquivalentTo(new[] { "PAST_B_2F_HALL", "PAST_B_1F_LAB", "PAST_B_1F_SAMPLE_STORAGE" }));
        }

        private static IReadOnlyList<RoomGraphSO> LoadRoomGraphs()
        {
            string[] guids = AssetDatabase.FindAssets("t:RoomGraphSO", new[] { ROOM_DATA_FOLDER });
            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<RoomGraphSO>)
                .Where(roomGraph => roomGraph != null)
                .ToArray();
        }
    }
}
