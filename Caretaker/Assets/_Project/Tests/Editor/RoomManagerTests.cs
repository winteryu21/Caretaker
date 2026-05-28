using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using UnityEditor;
using UnityEngine;

using Caretaker.World;

namespace Caretaker.Tests.Editor
{
    public class RoomManagerTests
    {
        private const string ROOM_DATA_FOLDER = "Assets/_Project/Data/Rooms";
        private const ulong PLAYER_ID = 42UL;
        private const string ROOM_A = "TEST_ROOM_A";
        private const string ROOM_B = "TEST_ROOM_B";

        [Test]
        public void ReportRoomEnter_UpdatesCurrentRoomAndRaisesEvents()
        {
            RoomManager roomManager = CreateRoomManager();
            List<string> changedRooms = new();
            List<string> visitedRooms = new();

            roomManager.OnRoomChanged += (_, roomId) => changedRooms.Add(roomId);
            roomManager.OnRoomVisited += (_, roomId) => visitedRooms.Add(roomId);

            roomManager.ReportRoomEnter(PLAYER_ID, ROOM_A);

            Assert.That(roomManager.GetCurrentRoomId(PLAYER_ID), Is.EqualTo(ROOM_A));
            Assert.That(roomManager.TryGetCurrentRoomId(PLAYER_ID, out string currentRoomId), Is.True);
            Assert.That(currentRoomId, Is.EqualTo(ROOM_A));
            Assert.That(roomManager.GetVisitedRooms(PLAYER_ID), Is.EquivalentTo(new[] { ROOM_A }));
            Assert.That(changedRooms, Is.EquivalentTo(new[] { ROOM_A }));
            Assert.That(visitedRooms, Is.EquivalentTo(new[] { ROOM_A }));

            Object.DestroyImmediate(roomManager.gameObject);
        }

        [Test]
        public void ReportRoomEnter_IgnoresDuplicateCurrentRoom()
        {
            RoomManager roomManager = CreateRoomManager();
            int changedCount = 0;
            int visitedCount = 0;

            roomManager.OnRoomChanged += (_, _) => changedCount++;
            roomManager.OnRoomVisited += (_, _) => visitedCount++;

            roomManager.ReportRoomEnter(PLAYER_ID, ROOM_A);
            roomManager.ReportRoomEnter(PLAYER_ID, ROOM_A);

            Assert.That(roomManager.GetCurrentRoomId(PLAYER_ID), Is.EqualTo(ROOM_A));
            Assert.That(roomManager.GetVisitedRooms(PLAYER_ID).Count, Is.EqualTo(1));
            Assert.That(changedCount, Is.EqualTo(1));
            Assert.That(visitedCount, Is.EqualTo(1));

            Object.DestroyImmediate(roomManager.gameObject);
        }

        [Test]
        public void ReportRoomExit_OnlyClearsMatchingCurrentRoom()
        {
            RoomManager roomManager = CreateRoomManager();
            int changedCount = 0;

            roomManager.OnRoomChanged += (_, _) => changedCount++;

            roomManager.ReportRoomEnter(PLAYER_ID, ROOM_A);
            roomManager.ReportRoomEnter(PLAYER_ID, ROOM_B);
            roomManager.ReportRoomExit(PLAYER_ID, ROOM_A);

            Assert.That(roomManager.GetCurrentRoomId(PLAYER_ID), Is.EqualTo(ROOM_B));

            roomManager.ReportRoomExit(PLAYER_ID, ROOM_B);

            Assert.That(roomManager.GetCurrentRoomId(PLAYER_ID), Is.Empty);
            Assert.That(changedCount, Is.EqualTo(2));

            Object.DestroyImmediate(roomManager.gameObject);
        }

        [Test]
        public void GetAdjacentRooms_UsesConfiguredRoomGraphData()
        {
            RoomManager roomManager = CreateRoomManager();

            roomManager.ConfigureRoomGraphs(LoadRoomGraphs());

            Assert.That(roomManager.ContainsRoom("PAST_B_2F_HALL"), Is.True);
            Assert.That(
                roomManager.GetAdjacentRooms("PAST_B_2F_HALL"),
                Is.EquivalentTo(new[] { "PAST_A_TO_B_SKYBRIDGE", "PAST_B_2F_ARCHIVE", "PAST_B_1F_HALL" }));

            Object.DestroyImmediate(roomManager.gameObject);
        }

        [Test]
        public void RoomVolume_ReportsParticipantEnterAndExit()
        {
            RoomManager roomManager = CreateRoomManager();
            RoomVolume roomVolume = CreateRoomVolume(roomManager, ROOM_A);
            Collider2D participantCollider = CreateParticipantCollider(PLAYER_ID);

            InvokeTrigger(roomVolume, "OnTriggerEnter2D", participantCollider);

            Assert.That(roomManager.GetCurrentRoomId(PLAYER_ID), Is.EqualTo(ROOM_A));

            InvokeTrigger(roomVolume, "OnTriggerExit2D", participantCollider);

            Assert.That(roomManager.GetCurrentRoomId(PLAYER_ID), Is.Empty);

            Object.DestroyImmediate(participantCollider.transform.root.gameObject);
            Object.DestroyImmediate(roomVolume.gameObject);
            Object.DestroyImmediate(roomManager.gameObject);
        }

        private static RoomManager CreateRoomManager()
        {
            GameObject gameObject = new("RoomManager");
            return gameObject.AddComponent<RoomManager>();
        }

        private static RoomVolume CreateRoomVolume(RoomManager roomManager, string roomId)
        {
            GameObject gameObject = new("RoomVolume");
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            RoomVolume roomVolume = gameObject.AddComponent<RoomVolume>();
            SerializedObject serializedObject = new(roomVolume);
            serializedObject.FindProperty("_roomId").stringValue = roomId;
            serializedObject.FindProperty("_roomManager").objectReferenceValue = roomManager;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return roomVolume;
        }

        private static Collider2D CreateParticipantCollider(ulong playerId)
        {
            GameObject root = new("RoomParticipant");
            RoomParticipant participant = root.AddComponent<RoomParticipant>();
            participant.SetPlayerId(playerId);

            GameObject child = new("ParticipantCollider");
            child.transform.SetParent(root.transform);
            return child.AddComponent<BoxCollider2D>();
        }

        private static void InvokeTrigger(RoomVolume roomVolume, string methodName, Collider2D other)
        {
            MethodInfo method = typeof(RoomVolume).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            method.Invoke(roomVolume, new object[] { other });
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
