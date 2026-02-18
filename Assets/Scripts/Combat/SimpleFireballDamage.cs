/*
 * Simple Fireball Damage Script
 *
 * Add this to your FireballProjectile prefab
 * It will damage any enemy it hits
 */

using UnityEngine;
using Opsive.UltimateCharacterController.Traits;
using KlyrasReach.AI;

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

            Vector3 hitPosition = collision.contacts[0].point;
            Vector3 hitDirection = collision.contacts[0].normal * -1;

            // MULTIPLAYER: Route damage through NetworkEnemySync if available
            // This ensures damage is applied on the Master Client and synced to all
            var networkSync = collision.gameObject.GetComponent<NetworkEnemySync>();
            if (networkSync == null)
            {
                networkSync = collision.gameObject.GetComponentInParent<NetworkEnemySync>();
            }

            if (networkSync != null)
            {
                networkSync.RequestDamage(_damageAmount, hitPosition, hitDirection, gameObject);
            }
            else
            {
                // Single player fallback: apply damage directly via Opsive Health
                var health = collision.gameObject.GetComponent<Health>();
                if (health == null)
                {
                    health = collision.gameObject.GetComponentInParent<Health>();
                }

                if (health != null)
                {
                    health.Damage(_damageAmount);
                }
            }

            // Apply impact force
            var rb = collision.rigidbody;
            if (rb != null)
            {
                rb.AddForce(hitDirection * _impactForce);
            }
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

            Vector3 hitPosition = other.ClosestPoint(transform.position);
            Vector3 hitDirection = (other.transform.position - transform.position).normalized;

            // MULTIPLAYER: Route damage through NetworkEnemySync if available
            var networkSync = other.GetComponent<NetworkEnemySync>();
            if (networkSync == null)
            {
                networkSync = other.GetComponentInParent<NetworkEnemySync>();
            }

            if (networkSync != null)
            {
                networkSync.RequestDamage(_damageAmount, hitPosition, hitDirection, gameObject);
            }
            else
            {
                // Single player fallback: apply damage directly via Opsive Health
                var health = other.GetComponent<Health>();
                if (health == null)
                {
                    health = other.GetComponentInParent<Health>();
                }

                if (health != null)
                {
                    health.Damage(_damageAmount);
                }
            }
        }
    }
}
