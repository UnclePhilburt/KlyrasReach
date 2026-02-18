/*
 * Fireball Damage Script
 *
 * PURPOSE:
 * Handles damage when fireball hits enemies or objects.
 * Works with Opsive's damage system.
 *
 * HOW TO USE:
 * 1. Add this script to your Fireball_Projectile prefab
 * 2. Set the damage amount
 * 3. Fireball will automatically deal damage on impact
 */

using UnityEngine;
using Opsive.UltimateCharacterController.Traits;
using Opsive.UltimateCharacterController.Traits.Damage;
using KlyrasReach.AI;

namespace KlyrasReach.Combat
{
    /// <summary>
    /// Applies damage when fireball hits something.
    /// In multiplayer, routes damage through NetworkEnemySync for proper sync.
    /// </summary>
    public class FireballDamage : MonoBehaviour
    {
        [Header("Damage Settings")]
        [Tooltip("How much damage the fireball deals")]
        [SerializeField] private float _damageAmount = 25f;

        [Tooltip("Force applied to objects hit by fireball")]
        [SerializeField] private float _impactForce = 500f;

        [Tooltip("Layers that can take damage (e.g., Enemy layer)")]
        [SerializeField] private LayerMask _damageableLayers = -1;

        private bool _hasHit = false;

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasHit)
            {
                return;
            }

            // Check if we hit something on a damageable layer BEFORE marking as hit
            // Otherwise the fireball gets "spent" on non-damageable objects (ground, walls)
            // and can never damage the actual enemy
            if ((_damageableLayers.value & (1 << collision.gameObject.layer)) == 0)
            {
                return;
            }

            _hasHit = true;

            Vector3 hitPoint = collision.contacts[0].point;
            Vector3 hitDirection = collision.contacts[0].normal * -1;

            // MULTIPLAYER: Route damage through NetworkEnemySync if available
            var networkSync = collision.gameObject.GetComponent<NetworkEnemySync>();
            if (networkSync == null)
            {
                networkSync = collision.gameObject.GetComponentInParent<NetworkEnemySync>();
            }

            if (networkSync != null)
            {
                networkSync.RequestDamage(_damageAmount, hitPoint, hitDirection, gameObject);
            }
            else
            {
                // Single player / non-networked target: use Opsive Health directly
                var health = collision.gameObject.GetComponent<Health>();
                if (health == null)
                {
                    health = collision.gameObject.GetComponentInParent<Health>();
                }

                if (health != null)
                {
                    var damageData = new DamageData();
                    damageData.SetDamage(
                        _damageAmount, hitPoint, hitDirection,
                        _impactForce, 1, 0,
                        gameObject, this, collision.collider
                    );
                    health.Damage(damageData);
                }
            }

            // Apply impact force to rigidbodies
            var rb = collision.rigidbody;
            if (rb != null)
            {
                rb.AddForce(hitDirection * _impactForce);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasHit) return;

            // Check layer BEFORE marking as hit (same reason as OnCollisionEnter)
            if ((_damageableLayers.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            _hasHit = true;

            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitDirection = (other.transform.position - transform.position).normalized;

            // MULTIPLAYER: Route damage through NetworkEnemySync if available
            var networkSync = other.GetComponent<NetworkEnemySync>();
            if (networkSync == null)
            {
                networkSync = other.GetComponentInParent<NetworkEnemySync>();
            }

            if (networkSync != null)
            {
                networkSync.RequestDamage(_damageAmount, hitPoint, hitDirection, gameObject);
            }
            else
            {
                // Single player / non-networked target: use Opsive Health directly
                var health = other.GetComponent<Health>();
                if (health == null)
                {
                    health = other.GetComponentInParent<Health>();
                }

                if (health != null)
                {
                    var damageData = new DamageData();
                    damageData.SetDamage(
                        _damageAmount, hitPoint, hitDirection,
                        _impactForce, 1, 0,
                        gameObject, this, other
                    );
                    health.Damage(damageData);
                }
            }
        }
    }
}
