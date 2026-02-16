/*
 * Ship Interior Zone for Klyra's Reach
 *
 * PURPOSE:
 * Parents characters to the ship when they're inside, so they move with it.
 * Simple replacement for Opsive's MovingPlatform - no complex waypoints needed.
 *
 * HOW TO USE:
 * 1. Create a child GameObject in your ship called "InteriorZone"
 * 2. Add this script to it
 * 3. Add a BoxCollider (or other collider) set to "Is Trigger"
 * 4. Size the collider to cover your ship's interior walkable areas
 * 5. Characters entering the zone will be parented to the ship
 * 6. Characters exiting the zone will be unparented
 */

using UnityEngine;

namespace KlyrasReach.Player
{
    /// <summary>
    /// Parents characters to ship when they enter the interior
    /// This makes them move with the ship automatically
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ShipInterior : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Tag to identify characters (default: Player)")]
        [SerializeField] private string _characterTag = "Player";

        [Tooltip("Show debug messages")]
        [SerializeField] private bool _debugMode = true;

        private Transform _shipTransform;

        /// <summary>
        /// Initialize and validate setup
        /// </summary>
        private void Awake()
        {
            // Get ship transform (parent or root)
            _shipTransform = GetComponentInParent<ShipController>()?.transform;

            if (_shipTransform == null)
            {
                // If no ShipController found, use root parent
                _shipTransform = transform.root;
                Debug.LogWarning($"[ShipInterior] No ShipController found. Using root transform: {_shipTransform.name}");
            }

            // Ensure collider is trigger
            Collider col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                Debug.LogWarning("[ShipInterior] Collider must be a trigger! Auto-fixing...");
                col.isTrigger = true;
            }

            if (_debugMode)
            {
                Debug.Log($"[ShipInterior] Initialized on '{gameObject.name}' - Ship: {_shipTransform.name}");
            }
        }

        /// <summary>
        /// Character entered ship interior - parent them to ship
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            // Check if this is a character
            if (!other.CompareTag(_characterTag))
            {
                return;
            }

            // Get character's root transform
            Transform characterTransform = other.transform.root;

            // Don't parent the ship to itself!
            if (characterTransform == _shipTransform)
            {
                return;
            }

            // Parent character to ship
            characterTransform.SetParent(_shipTransform, true); // worldPositionStays = true

            if (_debugMode)
            {
                Debug.Log($"[ShipInterior] Character '{characterTransform.name}' entered ship - PARENTED to '{_shipTransform.name}'");
            }
        }

        /// <summary>
        /// Character exited ship interior - unparent them
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            // Check if this is a character
            if (!other.CompareTag(_characterTag))
            {
                return;
            }

            // Get character's root transform
            Transform characterTransform = other.transform.root;

            // Only unparent if they're currently parented to this ship
            if (characterTransform.parent == _shipTransform)
            {
                characterTransform.SetParent(null, true); // worldPositionStays = true

                if (_debugMode)
                {
                    Debug.Log($"[ShipInterior] Character '{characterTransform.name}' exited ship - UNPARENTED");
                }
            }
        }

        /// <summary>
        /// Visualize interior zone in editor
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f); // Green with transparency

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                // Draw the trigger zone
                Gizmos.matrix = transform.localToWorldMatrix;

                if (col is BoxCollider box)
                {
                    Gizmos.DrawCube(box.center, box.size);
                    Gizmos.DrawWireCube(box.center, box.size);
                }
                else if (col is SphereCollider sphere)
                {
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
                    Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                }
                else if (col is CapsuleCollider capsule)
                {
                    // Simplified capsule visualization
                    Gizmos.DrawWireSphere(capsule.center, capsule.radius);
                }
            }
        }
    }
}
