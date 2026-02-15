/*
 * Player Spawn Point for Klyra's Reach
 *
 * PURPOSE:
 * Spawns the player at a designated location when a scene loads.
 * Used for planet surface scenes - player spawns on foot at a landing zone.
 *
 * NETWORK READY:
 * - Single-player: Spawns local player with Instantiate()
 * - Multiplayer: Will spawn networked player with PhotonNetwork.Instantiate()
 * - Only local player gets camera/UI setup
 *
 * HOW TO USE:
 * 1. Create an empty GameObject at your spawn location
 * 2. Add this script to it
 * 3. Assign your player prefab in the Inspector
 * 4. When scene loads, player spawns here
 */

using UnityEngine;
using System.Collections;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Spawns the player character at a specific location when scene loads
    /// NETWORK READY: Supports both local and networked spawning
    /// </summary>
    public class PlayerSpawnPoint : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("Player prefab to spawn (must have Opsive character controller)")]
        [SerializeField] private GameObject _playerPrefab;

        // Public property to access player prefab
        public GameObject PlayerPrefab => _playerPrefab;

        [Tooltip("Automatically spawn player when scene loads?")]
        [SerializeField] private bool _spawnOnStart = true;

        [Tooltip("Don't spawn player if they're boarding a ship")]
        [SerializeField] private bool _skipIfBoardingShip = true;

        [Header("Network Settings")]
        [Tooltip("Network spawn mode (will be auto-detected when PUN is installed)")]
        [SerializeField] private NetworkSpawnMode _spawnMode = NetworkSpawnMode.Local;

        [Header("Camera Setup")]
        [Tooltip("Camera prefab to spawn (or leave empty to use existing Main Camera)")]
        [SerializeField] private GameObject _cameraPrefab;

        // Reference to spawned player
        private GameObject _spawnedPlayer;

        // Public property to access spawned player
        public GameObject SpawnedPlayer => _spawnedPlayer;

        /// <summary>
        /// Spawn player when scene loads (Awake runs before camera initialization)
        /// </summary>
        private void Awake()
        {
            Debug.Log($"[PlayerSpawnPoint] Awake called - _spawnOnStart: {_spawnOnStart}");

            if (!_spawnOnStart)
            {
                Debug.Log("[PlayerSpawnPoint] _spawnOnStart is false - exiting");
                return;
            }

            // Check if a player already exists (e.g., spawned by landing pad)
            GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
            if (existingPlayer != null)
            {
                Debug.Log($"[PlayerSpawnPoint] Player already exists at {existingPlayer.transform.position} - skipping spawn");
                return;
            }

            // Check if player is boarding a ship (using network-ready state manager)
            if (_skipIfBoardingShip)
            {
                bool isBoardingShip = GameStateManager.Instance.IsBoardingShip;
                Debug.Log($"[PlayerSpawnPoint] IsBoardingShip flag: {isBoardingShip}");
                if (isBoardingShip)
                {
                    Debug.Log("[PlayerSpawnPoint] Player is boarding ship - but no player exists! Clearing flag and spawning.");
                    // If the flag is set but no player exists, clear it and spawn normally
                    GameStateManager.Instance.IsBoardingShip = false;
                }
            }

            Debug.Log("[PlayerSpawnPoint] All checks passed - spawning player");
            SpawnPlayer();
        }

        /// <summary>
        /// Spawns the player at this spawn point (public so other systems can call it)
        /// </summary>
        /// <param name="skipCameraSetup">If true, camera setup is skipped (for external callers)</param>
        public void SpawnPlayer(bool skipCameraSetup = false)
        {
            if (_playerPrefab == null)
            {
                Debug.LogError("[PlayerSpawnPoint] No player prefab assigned!");
                return;
            }

            // Spawn player at this position and rotation
            _spawnedPlayer = Instantiate(_playerPrefab, transform.position, transform.rotation);
            _spawnedPlayer.name = "Player";

            Debug.Log($"[PlayerSpawnPoint] Spawned player at position: {transform.position}");

            // Check if player has renderers and enable them
            Renderer[] renderers = _spawnedPlayer.GetComponentsInChildren<Renderer>();
            Debug.Log($"[PlayerSpawnPoint] Player has {renderers.Length} renderers");

            // Force enable all renderers (in case prefab has them disabled)
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = true;
            }
            Debug.Log($"[PlayerSpawnPoint] Enabled all player renderers");

            // Check if Opsive components are present and enable them
            var locomotion = _spawnedPlayer.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
            if (locomotion != null)
            {
                locomotion.enabled = true;
                Debug.Log($"[PlayerSpawnPoint] Opsive locomotion found and enabled");
            }
            else
            {
                Debug.LogError("[PlayerSpawnPoint] NO Opsive UltimateCharacterLocomotion component found on player!");
            }

            var characterController = _spawnedPlayer.GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = true;
                Debug.Log($"[PlayerSpawnPoint] CharacterController found and enabled");
            }

            // Remove AudioListener from player if camera has one (prevents duplicate listener warning)
            RemoveDuplicateAudioListener();

            // Setup camera (unless caller wants to handle it themselves)
            if (!skipCameraSetup)
            {
                SetupCamera();
            }
            else
            {
                Debug.Log("[PlayerSpawnPoint] Skipping camera setup - external caller will handle it");
            }
        }

        /// <summary>
        /// Sets up the camera for the spawned player
        /// </summary>
        private void SetupCamera()
        {
            Camera mainCamera = null;

            // If camera prefab is assigned, spawn it
            if (_cameraPrefab != null)
            {
                GameObject cameraObj = Instantiate(_cameraPrefab);
                mainCamera = cameraObj.GetComponent<Camera>();

                if (mainCamera != null)
                {
                    mainCamera.tag = "MainCamera";
                }
            }
            else
            {
                // Use existing Main Camera
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                Debug.LogWarning("[PlayerSpawnPoint] No camera found! Player may not work correctly.");
                return;
            }

            // Link camera to Opsive character
            LinkCameraToCharacter(mainCamera);
        }

        /// <summary>
        /// Removes duplicate AudioListener to prevent warnings
        /// </summary>
        private void RemoveDuplicateAudioListener()
        {
            if (_spawnedPlayer == null) return;

            // Check if main camera has an AudioListener
            Camera mainCam = Camera.main;
            if (mainCam != null && mainCam.GetComponent<AudioListener>() != null)
            {
                // Remove AudioListener from player (camera should have it)
                AudioListener playerListener = _spawnedPlayer.GetComponentInChildren<AudioListener>();
                if (playerListener != null)
                {
                    Destroy(playerListener);
                    Debug.Log("[PlayerSpawnPoint] Removed duplicate AudioListener from player");
                }
            }
        }

        /// <summary>
        /// Links the camera to the Opsive character controller
        /// </summary>
        private void LinkCameraToCharacter(Camera camera)
        {
            if (_spawnedPlayer == null) return;

            // Get Opsive Camera Controller component
            var cameraController = camera.GetComponent<Opsive.UltimateCharacterController.Camera.CameraController>();
            if (cameraController != null)
            {
                // Assign character to camera (this triggers initialization)
                cameraController.Character = _spawnedPlayer;
                Debug.Log("[PlayerSpawnPoint] Assigned character to camera controller");
            }

            // Get character locomotion component
            var locomotion = _spawnedPlayer.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
            if (locomotion != null && cameraController != null)
            {
                // This ensures the character knows about the camera
                Debug.Log("[PlayerSpawnPoint] Character and camera linked successfully");
            }
            else
            {
                Debug.LogWarning("[PlayerSpawnPoint] Could not fully link character and camera - some components missing");
            }

            // Link CrosshairsMonitor to character
            LinkCrosshairsToCharacter();
        }

        /// <summary>
        /// Links the CrosshairsMonitor to the spawned character
        /// NOTE: If CrosshairsMonitor has "Attach To Camera" enabled, this happens automatically!
        /// This method is a backup in case that setting is disabled.
        /// </summary>
        private void LinkCrosshairsToCharacter()
        {
            if (_spawnedPlayer == null) return;

            // Find the CrosshairsMonitor in the scene (should be on Canvas)
            var crosshairsMonitor = FindObjectOfType<Opsive.UltimateCharacterController.UI.CrosshairsMonitor>();

            if (crosshairsMonitor != null)
            {
                // Set the character (this triggers the proper initialization)
                // Note: This usually happens automatically via camera events if "Attach To Camera" is enabled
                crosshairsMonitor.Character = _spawnedPlayer;
                Debug.Log("[PlayerSpawnPoint] Linked CrosshairsMonitor to character");
            }
            else
            {
                Debug.LogWarning("[PlayerSpawnPoint] CrosshairsMonitor not found in scene - crosshair may not work");
            }
        }

        /// <summary>
        /// Draw debug visualization
        /// </summary>
        private void OnDrawGizmos()
        {
            // Draw spawn point marker
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // Draw direction arrow
            Gizmos.color = Color.blue;
            Vector3 forward = transform.forward * 2f;
            Gizmos.DrawRay(transform.position, forward);
            Gizmos.DrawSphere(transform.position + forward, 0.2f);

            // Draw player preview
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(0.5f, 1.8f, 0.5f));
        }
    }

    /// <summary>
    /// Network spawn mode options
    /// </summary>
    public enum NetworkSpawnMode
    {
        /// <summary>Local single-player spawning</summary>
        Local,
        /// <summary>Networked multiplayer spawning (requires PUN)</summary>
        Networked
    }
}
