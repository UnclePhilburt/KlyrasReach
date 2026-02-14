/*
 * Simple Fireball Damage Script
 *
 * Add this to your FireballProjectile prefab
 * It will damage any enemy it hits
 */

using UnityEngine;
using Opsive.UltimateCharacterController.Traits;

namespace KlyrasReach.Combat
{
    public class SimpleFireballDamage : MonoBehaviour
    {
        [Header("Damage Settings")]
        [Tooltip("How much damage to deal")]
        [SerializeField] private float _damageAmount = 25f;

        [Tooltip("Force applied on impact")]
        [SerializeField] private float _impactForce = 500f;

        [Tooltip("Only damage these layers (set to 'Enemy' layer)")]
        [SerializeField] private LayerMask _damageableLayers = -1;

        private bool _hasHit = false;

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasHit) return;

            // Check if we hit something on a damageable layer
            if ((_damageableLayers.value & (1 << collision.gameObject.layer)) == 0)
            {
                return;
            }

            _hasHit = true;

            // Try to find Health component on the object or its parent
            var health = collision.gameObject.GetComponent<Health>();
            if (health == null)
            {
                health = collision.gameObject.GetComponentInParent<Health>();
            }

            if (health != null)
            {
                // Deal damage directly
                health.Damage(_damageAmount);
            }

            // Apply impact force
            var rb = collision.rigidbody;
            if (rb != null)
            {
                Vector3 forceDirection = collision.contacts[0].normal * -1;
                rb.AddForce(forceDirection * _impactForce);
            }

            // Let Opsive's TrajectoryObject handle destruction
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasHit) return;

            // Check if we hit something on a damageable layer
            if ((_damageableLayers.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            _hasHit = true;

            // Try to find Health component
            var health = other.GetComponent<Health>();
            if (health == null)
            {
                health = other.GetComponentInParent<Health>();
            }

            if (health != null)
            {
                // Deal damage directly
                health.Damage(_damageAmount);
            }

            // Let Opsive's TrajectoryObject handle destruction
        }
    }
}
