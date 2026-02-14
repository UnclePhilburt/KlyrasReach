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

namespace KlyrasReach.Combat
{
    /// <summary>
    /// Applies damage when fireball hits something
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
            _hasHit = true;

            // Check if we hit something on a damageable layer
            if ((_damageableLayers.value & (1 << collision.gameObject.layer)) == 0)
            {
                return;
            }

            // Try to damage the object using Opsive's Health system
            var health = collision.gameObject.GetComponent<Health>();
            if (health == null)
            {
                health = collision.gameObject.GetComponentInParent<Health>();
            }

            if (health != null)
            {
                // Create damage data using Opsive's system
                var damageData = new DamageData();
                damageData.SetDamage(
                    _damageAmount,                          // amount
                    collision.contacts[0].point,            // position
                    collision.contacts[0].normal * -1,      // direction (inward)
                    _impactForce,                           // forceMagnitude
                    1,                                      // frames
                    0,                                      // radius
                    gameObject,                             // attacker
                    this,                                   // attackerObject
                    collision.collider                      // hitCollider
                );

                // Apply damage
                health.Damage(damageData);
            }

            // Apply impact force to rigidbodies
            var rb = collision.rigidbody;
            if (rb != null)
            {
                Vector3 forceDirection = collision.contacts[0].normal * -1;
                rb.AddForce(forceDirection * _impactForce);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasHit) return;
            _hasHit = true;

            // Check if we hit something on a damageable layer
            if ((_damageableLayers.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            // Try to damage the object
            var health = other.GetComponent<Health>();
            if (health == null)
            {
                health = other.GetComponentInParent<Health>();
            }

            if (health != null)
            {
                // Create damage data using Opsive's system
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 hitDirection = (other.transform.position - transform.position).normalized;

                var damageData = new DamageData();
                damageData.SetDamage(
                    _damageAmount,                          // amount
                    hitPoint,                               // position
                    hitDirection,                           // direction
                    _impactForce,                           // forceMagnitude
                    1,                                      // frames
                    0,                                      // radius
                    gameObject,                             // attacker
                    this,                                   // attackerObject
                    other                                   // hitCollider
                );

                // Apply damage
                health.Damage(damageData);
            }
        }
    }
}
