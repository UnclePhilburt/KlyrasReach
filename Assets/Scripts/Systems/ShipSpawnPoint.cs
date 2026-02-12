/*
 * Ship Spawn Point for Klyra's Reach
 *
 * PURPOSE:
 * Spawns the player inside their ship when returning from a landing zone.
 * Works with ShipEntryTrigger - when player boards ship from ground,
 * this script detects it and puts them in the cockpit at the right location.
 *
 * HOW TO USE:
 * 1. Add this script to your landing pad GameObject in the FLIGHT/SPACE scene
 * 2. Set the "Ship Prefab" reference to your ship
 * 3. Set the "Spawn Offset" to position ship relative to landing pad
 * 4. When player boards from ground scene, they'll appear here in the ship
 */

using UnityEngine;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Handles spawning player's ship when returning from ground/landing zone
    /// </summary>
    public class ShipSpawnPoint : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("Unique ID for this landing pad (must match LandingZoneTrigger ID)")]
        [SerializeField] private string _landingPadID = "LandingPad1";

        [Tooltip("The ship prefab to spawn (should have ShipController)")]
        [SerializeField] private GameObject _shipPrefab;

        [Tooltip("Offset from landing pad where ship spawns")]
        [SerializeField] private Vector3 _spawnOffset = new Vector3(0, 10f, 0);

        [Tooltip("Initial rotation of spawned ship")]
        [SerializeField] private Vector3 _spawnRotation = Vector3.zero;

        /// <summary>
        /// Check if player is boarding and spawn ship
        /// </summary>
        private void Start()
        {
            // Check if player is boarding ship (coming from ground scene)
            int isBoardingShip = PlayerPrefs.GetInt("IsBoardingShip", 0);
            Debug.Log($"[ShipSpawnPoint-{_landingPadID}] Checking boarding flag: {isBoardingShip}");

            if (isBoardingShip == 1)
            {
                // Check if this is the correct landing pad
                string lastLandingPadID = PlayerPrefs.GetString("LastLandingPadID", "");
                Debug.Log($"[ShipSpawnPoint-{_landingPadID}] Last landing pad was: '{lastLandingPadID}'");

                if (lastLandingPadID == _landingPadID)
                {
                    Debug.Log($"[ShipSpawnPoint] Player is boarding at correct pad ({_landingPadID}) - spawning ship");

                    // Clear the flag ONLY when we're the correct pad
                    PlayerPrefs.SetInt("IsBoardingShip", 0);
                    PlayerPrefs.Save();

                    // Spawn the ship
                    SpawnShipWithPlayer();
                }
                else
                {
                    Debug.Log($"[ShipSpawnPoint] Wrong landing pad (this: '{_landingPadID}', last: '{lastLandingPadID}') - skipping spawn");
                }
            }
            else
            {
                Debug.Log($"[ShipSpawnPoint-{_landingPadID}] Not boarding - flag is 0");
            }
        }

        /// <summary>
        /// Spawns the ship at this landing pad with player inside
        /// </summary>
        private void SpawnShipWithPlayer()
        {
            if (_shipPrefab == null)
            {
                Debug.LogError("[ShipSpawnPoint] No ship prefab assigned!");
                return;
            }

            // Calculate spawn position and rotation
            Vector3 spawnPosition = transform.position + _spawnOffset;
            Quaternion spawnRotation = Quaternion.Euler(_spawnRotation);

            // Spawn the ship
            GameObject spawnedShip = Instantiate(_shipPrefab, spawnPosition, spawnRotation);
            Debug.Log($"[ShipSpawnPoint] Spawned ship at {spawnPosition}");

            // Get the ship controller
            var shipController = spawnedShip.GetComponent<Player.ShipController>();
            if (shipController == null)
            {
                Debug.LogError("[ShipSpawnPoint] Ship prefab doesn't have ShipController component!");
                return;
            }

            // Get the ShipEntryPoint and disable it (we're spawning directly into piloting mode)
            var shipEntryPoint = spawnedShip.GetComponent<Player.ShipEntryPoint>();
            if (shipEntryPoint != null)
            {
                shipEntryPoint.enabled = false;
                Debug.Log("[ShipSpawnPoint] Disabled ShipEntryPoint (spawning directly into ship)");
            }

            // Get the main camera
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[ShipSpawnPoint] No main camera found!");
                return;
            }

            // Disable Opsive camera controller (we're in ship mode, not character mode)
            var opsiveCameraController = mainCamera.GetComponent<Opsive.UltimateCharacterController.Camera.CameraController>();
            if (opsiveCameraController != null)
            {
                opsiveCameraController.enabled = false;
                Debug.Log("[ShipSpawnPoint] Disabled Opsive camera controller for ship flight");
            }

            // Position camera behind the ship
            mainCamera.transform.position = spawnedShip.transform.position + spawnedShip.transform.TransformDirection(new Vector3(0, 2, -10));
            mainCamera.transform.LookAt(spawnedShip.transform);

            // Activate the ship controls
            shipController.EnterShip(mainCamera);

            Debug.Log("[ShipSpawnPoint] Ship activated and ready to fly!");
        }

        /// <summary>
        /// Draw debug visualization in Scene view
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw spawn position
            Vector3 spawnPos = transform.position + _spawnOffset;

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(spawnPos, 2f);

            // Draw line from pad to spawn point
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, spawnPos);

            // Draw ship orientation
            Gizmos.color = Color.blue;
            Quaternion rot = Quaternion.Euler(_spawnRotation);
            Gizmos.DrawRay(spawnPos, rot * Vector3.forward * 10f);

            // Draw landing pad
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(10f, 0.5f, 10f));
        }
    }
}
