/*
 * Spawn Visibility Checker for Klyra's Reach
 *
 * PURPOSE:
 * Checks if a world position is visible to the player's camera.
 * Used by RavagerSpawner to avoid spawning enemies where the player can see them.
 * This prevents the immersion-breaking "pop-in" effect.
 *
 * HOW IT WORKS:
 * Uses Unity's camera frustum planes (the invisible pyramid shape of what the camera sees).
 * Every 0.5 seconds, it recalculates the frustum and can then test any point instantly.
 * The math is very cheap - just dot products, no raycasting.
 *
 * MULTIPLAYER NOTE:
 * Only checks the master client's camera (we can't access remote players' cameras).
 * This means enemies might pop in for non-master clients occasionally.
 * This is an acceptable tradeoff since the master client controls spawning.
 *
 * HOW TO USE:
 * 1. Add this script to the same GameObject as RavagerSpawner (or any persistent object)
 * 2. RavagerSpawner will automatically find and use it
 * 3. No configuration needed - it finds the camera automatically
 */

using UnityEngine;

namespace KlyrasReach.AI
{
    /// <summary>
    /// Utility that checks if world positions are visible to the player's camera.
    /// Uses camera frustum planes for very fast visibility testing.
    /// </summary>
    public class SpawnVisibilityChecker : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("How often to recalculate the camera frustum (seconds). Lower = more accurate but slightly more CPU.")]
        [SerializeField] private float _updateInterval = 0.5f;

        [Tooltip("Extra padding around the camera view (meters). Prevents spawning just barely off-screen.")]
        [SerializeField] private float _viewPadding = 5f;

        [Header("Debug")]
        [Tooltip("Show debug messages when checking visibility?")]
        [SerializeField] private bool _debugMode = false;

        // The six frustum planes that define what the camera can see
        // (left, right, top, bottom, near, far)
        private Plane[] _frustumPlanes;

        // Reference to the main camera (cached for performance)
        private Camera _mainCamera;

        // Timer to avoid recalculating every frame
        private float _lastUpdateTime = 0f;

        // Whether we have valid frustum data
        private bool _hasValidFrustum = false;

        // =====================================================
        //  UNITY LIFECYCLE
        // =====================================================

        /// <summary>
        /// Find the main camera on startup
        /// </summary>
        private void Start()
        {
            // Allocate the planes array once (6 planes define the frustum)
            _frustumPlanes = new Plane[6];

            // Try to find the camera immediately
            FindMainCamera();
        }

        /// <summary>
        /// Periodically update the frustum planes from the camera's current position/rotation
        /// </summary>
        private void Update()
        {
            // Only update at the configured interval (not every frame)
            if (Time.time < _lastUpdateTime + _updateInterval)
                return;

            _lastUpdateTime = Time.time;

            // Make sure we have a camera reference
            if (_mainCamera == null)
            {
                FindMainCamera();

                if (_mainCamera == null)
                {
                    // No camera found yet (player might not have spawned)
                    _hasValidFrustum = false;
                    return;
                }
            }

            // Recalculate frustum planes from the camera's current view
            // This is very cheap - just matrix math, no raycasting
            GeometryUtility.CalculateFrustumPlanes(_mainCamera, _frustumPlanes);
            _hasValidFrustum = true;
        }

        // =====================================================
        //  PUBLIC METHODS
        // =====================================================

        /// <summary>
        /// Check if a world position is currently visible to the player's camera.
        /// Returns true if the player can see that point, false if it's off-screen.
        ///
        /// Uses a small bounding box around the point (sized by _viewPadding) to add
        /// a safety margin so enemies don't spawn just barely off the edge of screen.
        /// </summary>
        /// <param name="worldPosition">The position to check (e.g., a spawn point)</param>
        /// <returns>True if visible to the camera, false if hidden</returns>
        public bool IsPositionVisible(Vector3 worldPosition)
        {
            // If we don't have valid frustum data, assume visible (safe default)
            // This prevents spawning in potentially visible locations when camera hasn't been found yet
            if (!_hasValidFrustum)
            {
                if (_debugMode)
                {
                    Debug.Log("[VisibilityChecker] No valid frustum - assuming position IS visible (safe default)");
                }
                return true;
            }

            // Create a small bounding box around the point
            // The padding makes the "visible" area slightly larger than the actual screen,
            // so enemies won't spawn right at the edge of view
            Bounds testBounds = new Bounds(worldPosition, Vector3.one * _viewPadding);

            // Test if the bounding box intersects with the camera's frustum
            // This is the core check - returns true if ANY part of the bounds is visible
            bool isVisible = GeometryUtility.TestPlanesAABB(_frustumPlanes, testBounds);

            if (_debugMode)
            {
                Debug.Log($"[VisibilityChecker] Position {worldPosition} is {(isVisible ? "VISIBLE" : "HIDDEN")}");
            }

            return isVisible;
        }

        /// <summary>
        /// Check if the visibility system is ready to use.
        /// Returns false if no camera has been found yet.
        /// </summary>
        public bool IsReady()
        {
            return _hasValidFrustum;
        }

        // =====================================================
        //  PRIVATE HELPERS
        // =====================================================

        /// <summary>
        /// Find the main camera in the scene.
        /// In multiplayer, this will be the local (master client) camera.
        /// </summary>
        private void FindMainCamera()
        {
            // Camera.main uses the "MainCamera" tag - should work for both
            // single player and multiplayer (local player's camera)
            _mainCamera = Camera.main;

            if (_mainCamera != null && _debugMode)
            {
                Debug.Log($"[VisibilityChecker] Found main camera: '{_mainCamera.name}'");
            }
        }
    }
}
