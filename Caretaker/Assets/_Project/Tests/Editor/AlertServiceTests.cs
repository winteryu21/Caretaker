using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

using UnityEditor;
using UnityEngine;

using Caretaker.Gameplay;
using Caretaker.World;

namespace Caretaker.Tests.Editor
{
    public class AlertServiceTests
    {
        private const BindingFlags INSTANCE_PRIVATE = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void RaiseAlert_AffectsCurrentRoomAndAdjacentRooms()
        {
            AlertService alertService = new();

            IReadOnlyList<string> affectedRoomIds = alertService.RaiseAlert(
                "ROOM_A",
                new[] { "ROOM_B", "ROOM_C" },
                10f,
                0f);

            Assert.That(affectedRoomIds, Is.EquivalentTo(new[] { "ROOM_A", "ROOM_B", "ROOM_C" }));
            Assert.That(alertService.GetAlertState("ROOM_A"), Is.EqualTo(AlertState.Alert));
            Assert.That(alertService.GetAlertState("ROOM_B"), Is.EqualTo(AlertState.Alert));
            Assert.That(alertService.GetAlertState("ROOM_C"), Is.EqualTo(AlertState.Alert));
        }

        [Test]
        public void Tick_DecaysExpiredAlertToNone()
        {
            AlertService alertService = new();

            alertService.RaiseAlert("ROOM_A", new[] { "ROOM_B" }, 2f, 10f);

            Assert.That(alertService.GetAlertState("ROOM_A"), Is.EqualTo(AlertState.Alert));
            Assert.That(alertService.GetAlertState("ROOM_B"), Is.EqualTo(AlertState.Alert));
            Assert.That(alertService.Tick(11.9f), Is.Empty);
            Assert.That(alertService.Tick(12f), Is.EquivalentTo(new[] { "ROOM_A", "ROOM_B" }));
            Assert.That(alertService.GetAlertState("ROOM_A"), Is.EqualTo(AlertState.None));
            Assert.That(alertService.GetAlertState("ROOM_B"), Is.EqualTo(AlertState.None));
        }

        [Test]
        public void RoomManagerAlertEvent_SwitchesCurrentAndAdjacentRoomEnemiesToAlert()
        {
            RoomGraphSO roomA = CreateRoomGraph("ROOM_A", "ROOM_B");
            RoomGraphSO roomB = CreateRoomGraph("ROOM_B", "ROOM_A");
            RoomGraphSO roomC = CreateRoomGraph("ROOM_C");
            RoomManager roomManager = CreateRoomManager(roomA, roomB, roomC);
            EnemyController currentRoomEnemy = CreateEnemy("ROOM_A");
            EnemyController adjacentRoomEnemy = CreateEnemy("ROOM_B");
            EnemyController distantRoomEnemy = CreateEnemy("ROOM_C");
            currentRoomEnemy.SetRoomManager(roomManager);
            adjacentRoomEnemy.SetRoomManager(roomManager);
            distantRoomEnemy.SetRoomManager(roomManager);

            IReadOnlyList<string> affectedRoomIds = roomManager.RaiseRoomAlert("ROOM_A", AlertState.Alert, 10f, 0f);
            TickEnemy(currentRoomEnemy, 0.1f);
            TickEnemy(adjacentRoomEnemy, 0.1f);
            TickEnemy(distantRoomEnemy, 0.1f);

            Assert.That(affectedRoomIds, Is.EquivalentTo(new[] { "ROOM_A", "ROOM_B" }));
            Assert.That(currentRoomEnemy.CurrentState, Is.EqualTo(EnemyStateMachine.EnemyState.Alert));
            Assert.That(adjacentRoomEnemy.CurrentState, Is.EqualTo(EnemyStateMachine.EnemyState.Alert));
            Assert.That(distantRoomEnemy.CurrentState, Is.EqualTo(EnemyStateMachine.EnemyState.Patrol));

            Object.DestroyImmediate(currentRoomEnemy.gameObject);
            Object.DestroyImmediate(adjacentRoomEnemy.gameObject);
            Object.DestroyImmediate(distantRoomEnemy.gameObject);
            Object.DestroyImmediate(roomManager.gameObject);
            Object.DestroyImmediate(roomA);
            Object.DestroyImmediate(roomB);
            Object.DestroyImmediate(roomC);
        }

        private static RoomManager CreateRoomManager(params RoomGraphSO[] roomGraphs)
        {
            GameObject gameObject = new("RoomManager");
            RoomManager roomManager = gameObject.AddComponent<RoomManager>();
            roomManager.ConfigureRoomGraphs(roomGraphs);
            return roomManager;
        }

        private static RoomGraphSO CreateRoomGraph(string roomId, params string[] adjacentRoomIds)
        {
            RoomGraphSO roomGraph = ScriptableObject.CreateInstance<RoomGraphSO>();
            SerializedObject serializedObject = new(roomGraph);
            serializedObject.FindProperty("_roomId").stringValue = roomId;
            SerializedProperty adjacentRoomIdsProperty = serializedObject.FindProperty("_adjacentRoomIds");
            adjacentRoomIdsProperty.arraySize = adjacentRoomIds.Length;

            for (int i = 0; i < adjacentRoomIds.Length; i++)
            {
                adjacentRoomIdsProperty.GetArrayElementAtIndex(i).stringValue = adjacentRoomIds[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return roomGraph;
        }

        private static EnemyController CreateEnemy(string roomId)
        {
            GameObject gameObject = new("Enemy");
            EnemyController enemy = gameObject.AddComponent<EnemyController>();
            enemy.SetRoomId(roomId);
            return enemy;
        }

        private static void TickEnemy(EnemyController enemy, float deltaTime)
        {
            MethodInfo methodInfo = typeof(EnemyController).GetMethod("TickEnemy", INSTANCE_PRIVATE);
            methodInfo.Invoke(enemy, new object[] { deltaTime });
        }
    }
}
