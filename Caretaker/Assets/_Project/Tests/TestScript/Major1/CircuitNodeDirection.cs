using UnityEngine;

namespace Caretaker.Presentation
{

    public enum CircuitNodeDirection
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3
    }

    internal static class CircuitNodeDirectionUtility
    {
        private const float MIN_DIRECTION_DISTANCE = 0.0001f;

        public static CircuitNodeDirection GetNextClockwise(CircuitNodeDirection direction)
        {
            return direction == CircuitNodeDirection.Left
                ? CircuitNodeDirection.Up
                : (CircuitNodeDirection)((int)direction + 1);
        }

        public static CircuitNodeDirection GetOpposite(CircuitNodeDirection direction)
        {
            return direction switch
            {
                CircuitNodeDirection.Up => CircuitNodeDirection.Down,
                CircuitNodeDirection.Right => CircuitNodeDirection.Left,
                CircuitNodeDirection.Down => CircuitNodeDirection.Up,
                CircuitNodeDirection.Left => CircuitNodeDirection.Right,
                _ => CircuitNodeDirection.Up
            };
        }

        public static CircuitNodeDirection RotateClockwise(CircuitNodeDirection direction, int steps)
        {
            int normalizedSteps = ((steps % 4) + 4) % 4;
            return (CircuitNodeDirection)(((int)direction + normalizedSteps) % 4);
        }

        public static float GetVisualRotationZ(CircuitNodeDirection direction)
        {
            return direction switch
            {
                CircuitNodeDirection.Up => 0f,
                CircuitNodeDirection.Right => -90f,
                CircuitNodeDirection.Down => 180f,
                CircuitNodeDirection.Left => 90f,
                _ => 0f
            };
        }

        public static CircuitNodeDirection FromVector(Vector2 vector, CircuitNodeDirection fallback)
        {
            if (vector.sqrMagnitude <= MIN_DIRECTION_DISTANCE)
            {
                return fallback;
            }

            return Mathf.Abs(vector.x) >= Mathf.Abs(vector.y)
                ? vector.x >= 0f ? CircuitNodeDirection.Right : CircuitNodeDirection.Left
                : vector.y >= 0f ? CircuitNodeDirection.Up : CircuitNodeDirection.Down;
        }
    }
}
