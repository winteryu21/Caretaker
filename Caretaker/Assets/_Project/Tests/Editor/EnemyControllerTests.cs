using System.Reflection;

using NUnit.Framework;

using UnityEditor;
using UnityEngine;

using Caretaker.Gameplay;

namespace Caretaker.Tests.Editor
{
    public class EnemyControllerTests
    {
        private const BindingFlags INSTANCE_PRIVATE = BindingFlags.Instance | BindingFlags.NonPublic;
        private const int FLYING_MOVEMENT_MODE_INDEX = 1;

        [Test]
        public void TickPatrol_SetsHorizontalVelocityTowardNextWaypointAfterInitialArrival()
        {
            EnemyController controller = CreateController(
                Vector3.zero,
                CreateTuning(2f, 0f),
                CreateWaypoint("WaypointA", Vector3.zero),
                CreateWaypoint("WaypointB", Vector3.right));

            controller.TickPatrol(0.1f);
            controller.TickPatrol(0.25f);

            Assert.That(controller.CurrentPatrolWaypointIndex, Is.EqualTo(1));
            Assert.That(controller.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(2f).Within(0.001f));
            Assert.That(controller.transform.localScale.x, Is.LessThan(0f));

            DestroyController(controller);
        }

        [Test]
        public void TickPatrol_WrapsFromLastWaypointToFirst()
        {
            EnemyController controller = CreateController(
                Vector3.zero,
                CreateTuning(1f, 0f),
                CreateWaypoint("WaypointA", Vector3.zero),
                CreateWaypoint("WaypointB", new Vector3(2f, 0f, 0f)));

            controller.TickPatrol(0.1f);
            controller.GetComponent<Rigidbody2D>().position = new Vector2(2f, 0f);
            controller.TickPatrol(0.1f);

            Assert.That(controller.CurrentPatrolWaypointIndex, Is.EqualTo(0));

            DestroyController(controller);
        }

        [Test]
        public void TickPatrol_WaitsAtWaypointBeforeMovingAgain()
        {
            EnemyController controller = CreateController(
                Vector3.zero,
                CreateTuning(1f, 0.5f),
                CreateWaypoint("WaypointA", Vector3.zero),
                CreateWaypoint("WaypointB", Vector3.right));

            controller.TickPatrol(0.1f);
            controller.TickPatrol(0.25f);

            Assert.That(controller.IsWaitingAtWaypoint, Is.True);
            Assert.That(controller.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(0f).Within(0.001f));

            controller.TickPatrol(0.25f);
            controller.TickPatrol(0.25f);

            Assert.That(controller.IsWaitingAtWaypoint, Is.False);
            Assert.That(controller.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(1f).Within(0.001f));

            DestroyController(controller);
        }

        [Test]
        public void TickPatrol_SkipsWaypointAfterTimeout()
        {
            EnemyController controller = CreateController(
                Vector3.zero,
                CreateTuning(1f, 0f),
                0,
                0.5f,
                CreateWaypoint("WaypointA", Vector3.zero),
                CreateWaypoint("WaypointB", new Vector3(10f, 0f, 0f)));

            controller.TickPatrol(0.1f);
            controller.TickPatrol(0.25f);
            controller.TickPatrol(0.25f);
            controller.TickPatrol(0.25f);

            Assert.That(controller.CurrentPatrolWaypointIndex, Is.EqualTo(0));

            DestroyController(controller);
        }

        [Test]
        public void GroundedMode_StopsBeforePlatformEdge()
        {
            EnemyController controller = CreateController(
                new Vector3(0f, 0.55f, 0f),
                CreateTuning(2f, 0f),
                CreateWaypoint("WaypointA", new Vector3(0f, 0.55f, 0f)),
                CreateWaypoint("WaypointB", new Vector3(2f, 0.55f, 0f)));
            BoxCollider2D enemyCollider = controller.GetComponent<BoxCollider2D>();
            enemyCollider.size = Vector2.one;
            GameObject ground = new("ShortPlatform");
            BoxCollider2D groundCollider = ground.AddComponent<BoxCollider2D>();
            groundCollider.size = new Vector2(1f, 0.1f);
            ground.transform.position = Vector3.zero;
            InvokeOnValidate(controller);

            controller.TickPatrol(0.1f);
            controller.TickPatrol(0.1f);

            Assert.That(controller.GetComponent<Rigidbody2D>().linearVelocity.x, Is.EqualTo(0f).Within(0.001f));

            Object.DestroyImmediate(ground);
            DestroyController(controller);
        }

        [Test]
        public void TickPatrol_MissingConfigurationDoesNotMoveOrThrow()
        {
            GameObject gameObject = new("Enemy");
            EnemyController controller = gameObject.AddComponent<EnemyController>();

            Assert.DoesNotThrow(() => controller.TickPatrol(1f));
            Assert.That(gameObject.GetComponent<Rigidbody2D>().position, Is.EqualTo(Vector2.zero));

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void FlyingMode_UsesKinematicBodyWithoutGravity()
        {
            EnemyController controller = CreateController(
                Vector3.zero,
                CreateTuning(2f, 0f),
                FLYING_MOVEMENT_MODE_INDEX,
                CreateWaypoint("WaypointA", Vector3.zero),
                CreateWaypoint("WaypointB", Vector3.up));

            Rigidbody2D body = controller.GetComponent<Rigidbody2D>();

            Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
            Assert.That(body.gravityScale, Is.EqualTo(0f).Within(0.001f));

            DestroyController(controller);
        }

        private static EnemyController CreateController(
            Vector3 position,
            EnemyTuningSO tuning,
            params Transform[] waypoints)
        {
            return CreateController(position, tuning, 0, 5f, waypoints);
        }

        private static EnemyController CreateController(
            Vector3 position,
            EnemyTuningSO tuning,
            int movementModeIndex,
            params Transform[] waypoints)
        {
            return CreateController(position, tuning, movementModeIndex, 5f, waypoints);
        }

        private static EnemyController CreateController(
            Vector3 position,
            EnemyTuningSO tuning,
            int movementModeIndex,
            float waypointTimeoutSeconds,
            params Transform[] waypoints)
        {
            GameObject gameObject = new("Enemy");
            gameObject.transform.position = position;
            EnemyController controller = gameObject.AddComponent<EnemyController>();

            SerializedObject serializedObject = new(controller);
            serializedObject.FindProperty("_movementMode").enumValueIndex = movementModeIndex;
            serializedObject.FindProperty("_tuning").objectReferenceValue = tuning;
            serializedObject.FindProperty("_waypointTimeoutSeconds").floatValue = waypointTimeoutSeconds;
            SerializedProperty waypointProperty = serializedObject.FindProperty("_patrolWaypoints");
            waypointProperty.arraySize = waypoints.Length;
            for (int i = 0; i < waypoints.Length; i++)
            {
                waypoints[i].SetParent(gameObject.transform, true);
                waypointProperty.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            InvokeOnValidate(controller);

            return controller;
        }

        private static EnemyTuningSO CreateTuning(float moveSpeed, float patrolWaitTime)
        {
            EnemyTuningSO tuning = ScriptableObject.CreateInstance<EnemyTuningSO>();
            SerializedObject serializedObject = new(tuning);
            serializedObject.FindProperty("_moveSpeed").floatValue = moveSpeed;
            serializedObject.FindProperty("_patrolWaitTime").floatValue = patrolWaitTime;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return tuning;
        }

        private static Transform CreateWaypoint(string name, Vector3 position)
        {
            GameObject gameObject = new(name);
            gameObject.transform.position = position;
            return gameObject.transform;
        }

        private static void InvokeOnValidate(EnemyController controller)
        {
            MethodInfo methodInfo = typeof(EnemyController).GetMethod("OnValidate", INSTANCE_PRIVATE);
            methodInfo.Invoke(controller, null);
        }

        private static void DestroyController(EnemyController controller)
        {
            foreach (Transform waypoint in controller.GetComponentsInChildren<Transform>())
            {
                if (waypoint != controller.transform)
                {
                    Object.DestroyImmediate(waypoint.gameObject);
                }
            }

            Object.DestroyImmediate(controller.gameObject);
        }
    }
}
