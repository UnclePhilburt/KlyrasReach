/*
 * Ship Spawner for Klyra's Reach
 *
 * PURPOSE:
 * Spawns the player's ship at the space station when first loading the scene.
 * Does NOT spawn if player is boarding from a planet (ShipSpawnPoint handles that).
 *
 * HOW TO USE:
 * 1. Remove any pre-placed ships from your space scene
 * 2. Create an empty GameObject at the space station called "ShipSpawner"
 * 3. Add this script to it
 * 4. Position it where you want the ship to spawn initially
 * 5. Assign your ship prefab
 */

using UnityEngine;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Spawns player's ship at space station on scene start
    /// </summary>
    public class ShipSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("Ship prefab to spawn")]
        [SerializeField] private GameObject _shipPrefab;

        [Tooltip("Use this transform's position/rotation for spawn")]
        [SerializeField] private bool _useTransformPosition = true;

        [Tooltip("Custom spawn position (if not using transform)")]
        [SerializeField] private Vector3 _spawnPosition = Vector3.zero;

        [Tooltip("Custom spawn rotation (if not using transform)")]
        [SerializeField] private Vector3 _spawnRotation = Vector3.zero;

        [Header("Spawn Conditions")]
        [Tooltip("Don't spawn if player is boarding from a planet")]
        [SerializeField] private bool _skipIfBoardingShip = true;

        /// <summary>
        /// Spawn ship when scene loads
        /// </summary>
        private void Start()
        {
            // Check if player is boarding a ship (coming from planet)
            if (_skipIfBoardingShip)
            {
                int isBoardingShip = PlayerPrefs.GetInt("IsBoardingShip", 0);
                if (isBoardingShip == 1)
                {
                    Debug.Log("[ShipSpawner] Player is boarding from planet - skipping spawn (ShipSpawnPoint will handle it)");
                    return;
                }
            }

            // Spawn the ship
            SpawnShip();
        }

        /// <summary>
        /// Spawns the ship at this location
        /// </summary>
        private void SpawnShip()
        {
            if (_shipPrefab == null)
            {
                Debug.LogError("[ShipSpawner] No ship prefab assigned!");
                return;
            }

            // Calculate spawn position and rotation
            Vector3 spawnPos;
            Quaternion spawnRot;

            if (_useTransformPosition)
            {
                spawnPos = transform.position;
                spawnRot = transform.rotation;
            }
            else
            {
                spawnPos = _spawnPosition;
                spawnRot = Quaternion.Euler(_spawnRotation);
            }

            // Spawn the ship
            GameObject spawnedShip = Instantiate(_shipPrefab, spawnPos, spawnRot);
            spawnedShip.name = "PlayerShip"; // Clean up the "(Clone)" suffix

            Debug.Log($"[ShipSpawner] Spawned ship at {spawnPos}");

            // Make sure ship has the correct tag
            if (!spawnedShip.CompareTag("Ship"))
            {
                Debug.LogWarning("[ShipSpawner] Ship prefab doesn't have 'Ship' tag! Adding it now.");
                spawnedShip.tag = "Ship";
            }
        }

        /// <summary>
        /// Draw debug visualization
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Vector3 spawnPos = _useTransformPosition ? transform.position : _spawnPosition;

            // Draw spawn position
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(spawnPos, 2f);

            // Draw ship outline
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(spawnPos, new Vector3(10f, 5f, 15f));

            // Draw forward direction
            Quaternion spawnRot = _useTransformPosition ? transform.rotation : Quaternion.Euler(_spawnRotation);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(spawnPos, spawnRot * Vector3.forward * 10f);
        }
    }
}
