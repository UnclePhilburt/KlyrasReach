/*
 * Patrol Route for Klyra's Reach
 *
 * PURPOSE:
 * Defines a patrol path that enemies can follow when idle.
 * Place this on an empty GameObject, then add child GameObjects as waypoints.
 * Enemies assigned this route will walk between waypoints instead of standing still.
 *
 * HOW TO USE:
 * 1. Create an empty GameObject (e.g., "PatrolRoute_Outpost_01")
 * 2. Add this script to it
 * 3. Create child GameObjects as waypoints (their positions define the path)
 * 4. The order of child objects = the order of the patrol path
 * 5. Assign this PatrolRoute to a RavagerSpawner's patrol routes list
 *
 * GIZMOS:
 * - Yellow spheres show each waypoint
 * - Yellow lines connect them in order
 * - Green line shows loop-back connection (if looping)
 * - Arrow shows patrol direction
 */

using UnityEngine;

namespace KlyrasReach.AI
{
    /// <summary>
    /// How the patrol route handles reaching the last waypoint
    /// </summary>
    public enum PatrolLoopMode
    {
        Loop,       // After last waypoint, go back to the first (circular path)
        PingPong    // After last waypoint, reverse direction (back and forth)
    }

    /// <summary>
    /// Defines a patrol route using child Transform waypoints.
    /// Place on an empty GameObject, add child objects as waypoints.
    /// </summary>
    public class PatrolRoute : MonoBehaviour
    {
        [Header("Patrol Settings")]
        [Tooltip("How to handle reaching the end of the route: Loop goes back to start, PingPong reverses direction")]
        [SerializeField] private PatrolLoopMode _loopMode = PatrolLoopMode.Loop;

        [Tooltip("How long the enemy waits at each waypoint before moving on (seconds)")]
        [SerializeField] private float _waitTimeAtWaypoint = 2f;

        [Tooltip("How close the enemy needs to be to a waypoint to count as 'reached' (meters)")]
        [SerializeField] private float _waypointReachedDistance = 1.5f;

        [Header("Gizmo Display")]
        [Tooltip("Color of the gizmo lines and spheres in Scene view")]
        [SerializeField] private Color _gizmoColor = Color.yellow;

        [Tooltip("Size of the waypoint spheres in Scene view")]
        [SerializeField] private float _gizmoSphereSize = 0.5f;

        // Cached array of waypoint positions built from child transforms
        private Transform[] _waypoints;

        // =====================================================
        //  PUBLIC PROPERTIES - Read by EnemyAIController
        // =====================================================

        /// <summary>
        /// How the route handles reaching the end (loop or ping-pong)
        /// </summary>
        public PatrolLoopMode LoopMode => _loopMode;

        /// <summary>
        /// How long (seconds) an enemy should pause at each waypoint
        /// </summary>
        public float WaitTimeAtWaypoint => _waitTimeAtWaypoint;

        /// <summary>
        /// How close (meters) an enemy must be to a waypoint for it to count as reached
        /// </summary>
        public float WaypointReachedDistance => _waypointReachedDistance;

        /// <summary>
        /// Number of waypoints in this route (0 if none or not initialized)
        /// </summary>
        public int WaypointCount => GetWaypoints().Length;

        // =====================================================
        //  PUBLIC METHODS
        // =====================================================

        /// <summary>
        /// Get the world position of a specific waypoint by index.
        /// Returns Vector3.zero if index is out of range.
        /// </summary>
        /// <param name="index">Which waypoint (0-based)</param>
        /// <returns>World position of the waypoint</returns>
        public Vector3 GetWaypointPosition(int index)
        {
            Transform[] waypoints = GetWaypoints();

            // Safety check - don't crash if index is bad
            if (waypoints.Length == 0 || index < 0 || index >= waypoints.Length)
            {
                return Vector3.zero;
            }

            return waypoints[index].position;
        }

        /// <summary>
        /// Check if this route has enough waypoints to be usable (need at least 2)
        /// </summary>
        /// <returns>True if the route has 2+ waypoints</returns>
        public bool IsValid()
        {
            return GetWaypoints().Length >= 2;
        }

        // =====================================================
        //  PRIVATE HELPERS
        // =====================================================

        /// <summary>
        /// Build waypoint array from child transforms.
        /// Cached after first call - only rebuilds if children change.
        /// </summary>
        private Transform[] GetWaypoints()
        {
            // Rebuild if cache is empty or child count changed
            if (_waypoints == null || _waypoints.Length != transform.childCount)
            {
                _waypoints = new Transform[transform.childCount];
                for (int i = 0; i < transform.childCount; i++)
                {
                    _waypoints[i] = transform.GetChild(i);
                }
            }

            return _waypoints;
        }

        // =====================================================
        //  GIZMO VISUALIZATION - Shows route in Scene view
        // =====================================================

        /// <summary>
        /// Always draw gizmos so patrol routes are visible even when not selected
        /// </summary>
        private void OnDrawGizmos()
        {
            // Draw a small diamond at the route's root position
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);

            DrawRouteGizmos(0.5f); // Half opacity when not selected
        }

        /// <summary>
        /// Draw brighter gizmos when selected
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            DrawRouteGizmos(1f); // Full opacity when selected
        }

        /// <summary>
        /// Draw the patrol route as connected spheres and lines
        /// </summary>
        /// <param name="opacity">How visible the gizmos should be (0-1)</param>
        private void DrawRouteGizmos(float opacity)
        {
            // Need at least one child waypoint to draw anything
            if (transform.childCount == 0)
                return;

            // Set color with opacity
            Color drawColor = _gizmoColor;
            drawColor.a = opacity;
            Gizmos.color = drawColor;

            // Draw each waypoint as a sphere and connect with lines
            Transform previousWaypoint = null;

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform waypoint = transform.GetChild(i);

                if (waypoint == null)
                    continue;

                // Draw sphere at waypoint position
                Gizmos.DrawWireSphere(waypoint.position, _gizmoSphereSize);

                // Draw line from previous waypoint to this one
                if (previousWaypoint != null)
                {
                    Gizmos.DrawLine(previousWaypoint.position, waypoint.position);
                }

                previousWaypoint = waypoint;
            }

            // Draw loop-back line if using Loop mode (connect last waypoint back to first)
            if (_loopMode == PatrolLoopMode.Loop && transform.childCount >= 2)
            {
                Color loopColor = Color.green;
                loopColor.a = opacity * 0.6f;
                Gizmos.color = loopColor;

                Transform firstWaypoint = transform.GetChild(0);
                Transform lastWaypoint = transform.GetChild(transform.childCount - 1);
                Gizmos.DrawLine(lastWaypoint.position, firstWaypoint.position);
            }

            // Draw waypoint numbers using handles (only in editor)
            #if UNITY_EDITOR
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform waypoint = transform.GetChild(i);
                if (waypoint != null)
                {
                    // Label each waypoint with its index number
                    UnityEditor.Handles.Label(
                        waypoint.position + Vector3.up * 0.8f,
                        $"WP {i}"
                    );
                }
            }
            #endif
        }
    }
}
