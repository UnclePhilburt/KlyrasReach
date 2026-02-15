/*
 * Ship Gravity Zone for Klyra's Reach
 *
 * PURPOSE:
 * Creates artificial gravity inside the ship that always pulls toward the ship's floor,
 * regardless of how the ship is oriented in space. This prevents players from sliding
 * around when the ship tilts or rotates.
 *
 * HOW TO USE:
 * 1. Add this script to a child GameObject of the ship prefab (e.g., "ShipGravityZone")
 * 2. Add a Box Collider (or other trigger) covering the ship interior
 * 3. Set the collider to "Is Trigger" = true
 * 4. Player character must have the "Align To Gravity Zone" ability enabled
 * 5. Gravity will always pull toward the ship's -Y axis (floor)
 */

using UnityEngine;
using Opsive.UltimateCharacterController.Objects.CharacterAssist;

namespace KlyrasReach.Player
{
    /// <summary>
    /// Overrides gravity for characters inside the ship to pull toward ship's floor.
    /// Uses Opsive's built-in GravityZone system.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ShipGravityZone : GravityZone
    {
        [Header("Gravity Settings")]
        [Tooltip("Gravity direction relative to ship (-1 = toward floor)")]
        [SerializeField] private Vector3 _localGravityDirection = new Vector3(0, -1, 0);

        [Tooltip("Gravity strength multiplier (1 = normal Earth gravity)")]
        [SerializeField] private float _gravityStrength = 1f;

        /// <summary>
        /// Initialize and validate trigger collider
        /// </summary>
        private void Awake()
        {
            // Ensure we have a trigger collider
            Collider collider = GetComponent<Collider>();
            if (collider != null && !collider.isTrigger)
            {
                Debug.LogWarning("[ShipGravityZone] Collider should be set to 'Is Trigger'. Auto-fixing...");
                collider.isTrigger = true;
            }
        }

        /// <summary>
        /// Determines the direction of gravity that should be applied.
        /// Called by Opsive's AlignToGravityZone ability.
        /// </summary>
        /// <param name="position">The position of the character.</param>
        /// <returns>The direction of gravity that should be applied.</returns>
        public override Vector3 DetermineGravityDirection(Vector3 position)
        {
            // Convert ship's local down direction to world space
            // This ensures gravity always points toward the ship floor,
            // regardless of how the ship is rotated
            Vector3 worldGravityDirection = transform.TransformDirection(_localGravityDirection).normalized;

            // Apply strength multiplier
            return worldGravityDirection * _gravityStrength;
        }

        /// <summary>
        /// Visualize gravity direction in editor
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw gravity direction arrow
            Gizmos.color = Color.green;
            Vector3 worldGravityDir = transform.TransformDirection(_localGravityDirection).normalized;
            Gizmos.DrawRay(transform.position, worldGravityDir * 5f);

            // Draw label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + worldGravityDir * 6f,
                "Ship Gravity Direction",
                new GUIStyle() { normal = new GUIStyleState() { textColor = Color.green } }
            );
            #endif

            // Draw trigger volume outline
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                if (col is BoxCollider boxCollider)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawCube(boxCollider.center, boxCollider.size);
                }
            }
        }
    }
}
