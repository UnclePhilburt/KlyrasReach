/*
 * Ship Passenger Sync for Klyra's Reach
 *
 * PURPOSE:
 * Makes characters stick to the ship floor when it moves.
 * Applies the ship's movement delta to all passengers each frame.
 * Simple custom solution - no Opsive MovingPlatform needed!
 *
 * HOW TO USE:
 * 1. Attach this script to your ship GameObject (same object with ShipController)
 * 2. It automatically tracks and syncs all passengers
 * 3. Works with Opsive's character controller
 */

using UnityEngine;
using System.Collections.Generic;

namespace KlyrasReach.Player
{
    /// <summary>
    /// Syncs passengers to ship movement so they stick to the floor
    /// </summary>
    public class ShipPassengerSync : MonoBehaviour
    {
        [Header("Detection")]
        [Tooltip("Tag to identify characters (default: Player)")]
        [SerializeField] private string _characterTag = "Player";

        [Tooltip("Show debug messages")]
        [SerializeField] private bool _debugMode = true;

        // Track ship movement
        private Vector3 _previousPosition;
        private Quaternion _previousRotation;

        // Track passengers and their Opsive components
        private Dictionary<Transform, Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion> _passengers =
            new Dictionary<Transform, Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();

        /// <summary>
        /// Initialize ship position tracking
        /// </summary>
        private void Start()
        {
            _previousPosition = transform.position;
            _previousRotation = transform.rotation;

            if (_debugMode)
            {
                Debug.Log($"[ShipPassengerSync] Initialized on '{gameObject.name}'");
            }
        }

        /// <summary>
        /// Apply ship movement to all passengers
        /// Called in FixedUpdate to work with physics and Opsive
        /// </summary>
        private void FixedUpdate()
        {
            // Calculate ship movement delta
            Vector3 positionDelta = transform.position - _previousPosition;
            Quaternion rotationDelta = transform.rotation * Quaternion.Inverse(_previousRotation);

            // Only apply movement if ship actually moved
            if (positionDelta.sqrMagnitude > 0.0001f || Quaternion.Angle(rotationDelta, Quaternion.identity) > 0.01f)
            {
                // Apply movement to all passengers
                foreach (var kvp in _passengers)
                {
                    Transform passenger = kvp.Key;
                    var locomotion = kvp.Value;

                    if (passenger == null) continue;

                    // Calculate new position
                    Vector3 newPosition = passenger.position + positionDelta;

                    // Apply rotation around ship's pivot point
                    if (Quaternion.Angle(rotationDelta, Quaternion.identity) > 0.01f)
                    {
                        Vector3 localPos = passenger.position - transform.position;
                        localPos = rotationDelta * localPos;
                        newPosition = transform.position + localPos;
                    }

                    // If Opsive character controller exists, use SetPosition
                    if (locomotion != null)
                    {
                        locomotion.SetPosition(newPosition);
                    }
                    else
                    {
                        // Fallback: direct transform movement
                        passenger.position = newPosition;
                    }
                }

                if (_debugMode && _passengers.Count > 0)
                {
                    Debug.Log($"[ShipPassengerSync] Ship moved - synced {_passengers.Count} passengers. PosDelta: {positionDelta.magnitude:F3}");
                }
            }

            // Store current transform for next frame
            _previousPosition = transform.position;
            _previousRotation = transform.rotation;
        }

        /// <summary>
        /// Register a passenger (called by ShipEntryPoint or trigger zones)
        /// </summary>
        public void AddPassenger(Transform passenger)
        {
            if (passenger == null) return;

            // Check for Opsive character locomotion
            var locomotion = passenger.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();

            if (!_passengers.ContainsKey(passenger))
            {
                _passengers.Add(passenger, locomotion);

                if (_debugMode)
                {
                    Debug.Log($"[ShipPassengerSync] Added passenger: {passenger.name} (Opsive: {locomotion != null})");
                }
            }
        }

        /// <summary>
        /// Unregister a passenger
        /// </summary>
        public void RemovePassenger(Transform passenger)
        {
            if (passenger == null) return;

            if (_passengers.Remove(passenger))
            {
                if (_debugMode)
                {
                    Debug.Log($"[ShipPassengerSync] Removed passenger: {passenger.name}");
                }
            }
        }

        /// <summary>
        /// Get passenger count for debugging
        /// </summary>
        public int PassengerCount => _passengers.Count;
    }
}
