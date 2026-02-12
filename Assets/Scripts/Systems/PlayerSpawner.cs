/*
 * Player Spawner for Klyra's Reach
 *
 * PURPOSE:
 * Spawns the player character when a scene loads.
 * Handles different spawn scenarios (normal spawn, boarding ship, etc.)
 * Allows for character selection in the future.
 *
 * HOW TO USE:
 * 1. Create an empty GameObject in your scene called "PlayerSpawnPoint"
 * 2. Add this script to it
 * 3. Assign your player character prefab
 * 4. Position the spawn point where you want the player to appear
 * 5. Remove any existing player characters from the scene
 */

using UnityEngine;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Spawns player character at scene start
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("The player character prefab to spawn")]
        [SerializeField] private GameObject _playerPrefab;

        [Tooltip("Spawn at this transform's position and rotation")]
        [SerializeField] private bool _useTransformPosition = true;

        [Tooltip("Custom spawn position (if not using transform position)")]
        [SerializeField] private Vector3 _spawnPosition = Vector3.zero;

        [Tooltip("Custom spawn rotation (if not using transform rotation)")]
        [SerializeField] private Vector3 _spawnRotation = Vector3.zero;

        [Header("Spawn Conditions")]
        [Tooltip("Don't spawn player if they're boarding a ship")]
        [SerializeField] private bool _skipIfBoardingShip = true;

        // Reference to spawned player
        private static GameObject _spawnedPlayer = null;
        public static GameObject SpawnedPlayer => _spawnedPlayer;

        /// <summary>
        /// Spawn player when scene loads (using Awake to spawn before camera initializes)
        /// </summary>
        private void Awake()
        {
            Debug.Log("[PlayerSpawner] Starting player spawn check...");

            // Check if player is boarding a ship
            if (_skipIfBoardingShip)
            {
                int isBoardingShip = PlayerPrefs.GetInt("IsBoardingShip", 0);
                Debug.Log($"[PlayerSpawner] IsBoardingShip flag = {isBoardingShip}");

                if (isBoardingShip == 1)
                {
                    Debug.Log("[PlayerSpawner] Player is boarding ship - skipping player spawn");
                    return;
                }
            }

            Debug.Log("[PlayerSpawner] Proceeding to spawn player...");
            // Spawn the player
            SpawnPlayer();
        }

        /// <summary>
        /// Spawns the player character
        /// </summary>
        private void SpawnPlayer()
        {
            if (_playerPrefab == null)
            {
                Debug.LogError("[PlayerSpawner] No player prefab assigned!");
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

            // Spawn the player
            _spawnedPlayer = Instantiate(_playerPrefab, spawnPos, spawnRot);
            _spawnedPlayer.name = "Player"; // Clean up the "(Clone)" suffix

            Debug.Log($"[PlayerSpawner] Spawned player at {spawnPos}");

            // Make sure player has the correct tag
            if (!_spawnedPlayer.CompareTag("Player"))
            {
                Debug.LogWarning("[PlayerSpawner] Player prefab doesn't have 'Player' tag! Adding it now.");
                _spawnedPlayer.tag = "Player";
            }
        }

        /// <summary>
        /// Public method to spawn player manually (for character selection, etc.)
        /// </summary>
        public void SpawnPlayerManual(GameObject playerPrefab)
        {
            // Destroy old player if exists
            if (_spawnedPlayer != null)
            {
                Destroy(_spawnedPlayer);
            }

            // Update prefab reference
            _playerPrefab = playerPrefab;

            // Spawn new player
            SpawnPlayer();
        }

        /// <summary>
        /// Draw debug visualization
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Vector3 spawnPos = _useTransformPosition ? transform.position : _spawnPosition;

            // Draw spawn position
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPos, 1f);

            // Draw forward direction
            Quaternion spawnRot = _useTransformPosition ? transform.rotation : Quaternion.Euler(_spawnRotation);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(spawnPos, spawnRot * Vector3.forward * 2f);

            // Draw label
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(spawnPos + Vector3.up * 2f, Vector3.one * 0.2f);
        }
    }
}
