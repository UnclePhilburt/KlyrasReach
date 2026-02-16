/*
 * Manual Moving Platform for Klyra's Reach
 *
 * PURPOSE:
 * Extends Opsive's MovingPlatform to work with manual ship controls
 * Opsive requires MovingPlatform component to prevent camera jitter
 * This version disables waypoint automation - ship moves via ShipController
 *
 * HOW TO USE:
 * 1. Add to ship root GameObject (instead of regular Moving Platform)
 * 2. Ship must be on "Moving Platform" layer (Layer 27)
 * 3. Rigidbody must be kinematic
 */

using UnityEngine;
using Opsive.UltimateCharacterController.Objects;

namespace KlyrasReach.Player
{
    /// <summary>
    /// Extends Opsive's MovingPlatform for manual ship controls
    /// Syncs Transform movement (from ShipController) to Rigidbody
    /// </summary>
    public class ManualMovingPlatform : MovingPlatform
    {
        private Rigidbody m_Rigidbody;

        /// <summary>
        /// Override Awake() to prevent freezing rigidbody constraints
        /// This allows ShipController to move the ship freely
        /// </summary>
        protected override void Awake()
        {
            Debug.Log($"[ManualMovingPlatform] Awake() called on '{gameObject.name}'");

            // Call base Awake first
            base.Awake();

            // Cache rigidbody
            m_Rigidbody = GetComponent<Rigidbody>();

            // IMPORTANT: Opsive's MovingPlatform freezes ALL constraints
            // We need to unfreeze position so the rigidbody can be moved
            // Keep rotation frozen for stable flight control
            if (m_Rigidbody != null)
            {
                Debug.Log($"[ManualMovingPlatform] BEFORE: constraints = {m_Rigidbody.constraints}");
                m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                Debug.Log($"[ManualMovingPlatform] AFTER: constraints = {m_Rigidbody.constraints}");
                Debug.Log("[ManualMovingPlatform] Unfroze rigidbody position constraints!");
            }
            else
            {
                Debug.LogError("[ManualMovingPlatform] No Rigidbody found!");
            }
        }

        /// <summary>
        /// Override Move() to sync Transform position to Rigidbody
        /// ShipController moves the Transform, we sync it to Rigidbody
        /// This keeps characters stuck to the ship floor
        /// </summary>
        public override void Move()
        {
            // Sync Transform position/rotation to Rigidbody
            // This is what makes characters stick to the moving ship
            if (m_Rigidbody != null)
            {
                m_Rigidbody.position = Transform.position;
                m_Rigidbody.rotation = Transform.rotation;
            }
        }
    }
}
