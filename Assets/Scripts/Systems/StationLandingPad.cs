/*
 * Station Landing Pad for Klyra's Reach
 *
 * PURPOSE:
 * Allows landing at a space station without loading a new scene.
 * Ship parks itself and player exits on foot at the station.
 *
 * HOW TO USE:
 * 1. Add this script to a landing pad GameObject at your space station
 * 2. Set the landing range and player exit position
 * 3. When player flies close and presses F, ship parks and they exit on foot
 */

using UnityEngine;
using UnityEngine.InputSystem;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Handles landing at a space station without scene transitions
    /// </summary>
    public class StationLandingPad : MonoBehaviour
    {
        [Header("Landing Settings")]
        [Tooltip("How close ship needs to be to land (in units)")]
        [SerializeField] private float _landingRange = 50f;

        [Tooltip("Use a specific exit point transform instead of offset")]
        [SerializeField] private bool _useExitPointTransform = false;

        [Tooltip("Transform for player exit position (if using transform)")]
        [SerializeField] private Transform _playerExitPoint;

        [Tooltip("Where the player appears when exiting ship (relative offset if not using transform)")]
        [SerializeField] private Vector3 _playerExitPosition = new Vector3(0, 1, 5);

        [Tooltip("Where the ship parks (offset from landing pad)")]
        [SerializeField] private Vector3 _shipParkPosition = new Vector3(0, 2, 0);

        [Tooltip("How long the landing animation takes")]
        [SerializeField] private float _landingDuration = 2f;

        [Header("Detection")]
        [Tooltip("Tag to identify ships")]
        [SerializeField] private string _shipTag = "Ship";

        // Private variables
        private bool _shipInRange = false;
        private GameObject _nearbyShip = null;
        private Player.ShipController _shipController = null;
        private bool _isLanding = false;
        private GameObject _parkedShip = null;
        private float _shipActivatedTime = 0f;

        /// <summary>
        /// Check for nearby ships every frame
        /// </summary>
        private void Update()
        {
            if (_isLanding) return;

            // Find active ship
            FindNearbyShip();

            // Check for landing input - ONLY if already piloting (prevents triggering on ship entry)
            if (_shipInRange && _nearbyShip != null && _shipController != null && _shipController.IsActive)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
                {
                    // Make sure player has been piloting for at least 0.5 seconds
                    // This prevents landing immediately on ship entry
                    if (Time.time - _shipActivatedTime > 0.5f)
                    {
                        Debug.Log($"[StationLandingPad] Landing initiated");
                        StartCoroutine(LandingSequence());
                    }
                    else
                    {
                        Debug.Log($"[StationLandingPad] Too soon after entering ship - ignoring landing request");
                    }
                }
            }
        }

        /// <summary>
        /// Finds ships within landing range
        /// </summary>
        private void FindNearbyShip()
        {
            GameObject[] ships = GameObject.FindGameObjectsWithTag(_shipTag);

            bool wasInRange = _shipInRange;
            _shipInRange = false;
            _nearbyShip = null;
            _shipController = null;

            foreach (GameObject ship in ships)
            {
                float distance = Vector3.Distance(transform.position, ship.transform.position);

                if (distance <= _landingRange)
                {
                    var controller = ship.GetComponent<Player.ShipController>();
                    if (controller != null && controller.IsActive)
                    {
                        _shipInRange = true;
                        _nearbyShip = ship;
                        _shipController = controller;

                        // Track when ship was just activated
                        if (!wasInRange)
                        {
                            _shipActivatedTime = Time.time;
                        }

                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Landing sequence without scene change
        /// </summary>
        private System.Collections.IEnumerator LandingSequence()
        {
            _isLanding = true;

            // Exit the ship (deactivate ship controls)
            _shipController.ExitShip();

            // Disable ship physics to prevent bouncing during landing
            Rigidbody shipRb = _nearbyShip.GetComponent<Rigidbody>();
            if (shipRb != null)
            {
                shipRb.isKinematic = true;
                shipRb.linearVelocity = Vector3.zero;
                shipRb.angularVelocity = Vector3.zero;
            }

            // Tell ShipEntryPoint that player is no longer piloting
            var shipEntryPoint = _nearbyShip.GetComponent<Player.ShipEntryPoint>();
            if (shipEntryPoint != null)
            {
                shipEntryPoint.ResetPilotingState();
                // Re-enable ShipEntryPoint in case it was disabled by ShipSpawnPoint
                shipEntryPoint.enabled = true;
            }

            // Calculate exit position FIRST (before spawning player)
            Vector3 exitPos;
            Quaternion exitRot;

            if (_useExitPointTransform && _playerExitPoint != null)
            {
                exitPos = _playerExitPoint.position;
                exitRot = _playerExitPoint.rotation;
                Debug.Log($"[StationLandingPad] Using exit point transform at {exitPos}");
            }
            else
            {
                exitPos = transform.position + transform.TransformDirection(_playerExitPosition);
                exitRot = transform.rotation;
                Debug.Log($"[StationLandingPad] Using exit position offset at {exitPos}");
            }

            // Get player reference - might not exist if coming from planet
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            Debug.Log($"[StationLandingPad] Found existing player: {player != null} (name: {player?.name})");

            // If no player exists, spawn one directly at exit position (happens when coming from planet)
            if (player == null)
            {
                Debug.Log("[StationLandingPad] No player found - spawning directly at exit position");

                PlayerSpawnPoint spawner = FindObjectOfType<PlayerSpawnPoint>();
                if (spawner != null && spawner.PlayerPrefab != null)
                {
                    // Get the player prefab from the spawner
                    GameObject playerPrefab = spawner.PlayerPrefab;

                    // Spawn player directly at exit position
                    player = Instantiate(playerPrefab, exitPos, exitRot);
                    player.name = "Player";
                    player.tag = "Player"; // IMPORTANT: Set the Player tag

                    Debug.Log($"[StationLandingPad] Spawned player directly at exit position: {exitPos}");

                    // Enable player components
                    Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
                    foreach (Renderer renderer in renderers)
                    {
                        renderer.enabled = true;
                    }

                    var locomotion = player.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
                    if (locomotion != null)
                    {
                        locomotion.enabled = true;
                    }

                    var charController = player.GetComponent<CharacterController>();
                    if (charController != null)
                    {
                        charController.enabled = true;
                    }

                    // Wait a frame for player to fully initialize
                    yield return null;
                }
                else
                {
                    Debug.LogError("[StationLandingPad] No PlayerSpawnPoint found or no player prefab assigned!");
                }
            }

            // Move player to exit position (for existing players or final position check)
            if (player != null)
            {
                // Disable character controller before moving
                var charController = player.GetComponent<CharacterController>();
                if (charController != null)
                {
                    charController.enabled = false;
                }

                var locomotion = player.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
                if (locomotion != null)
                {
                    locomotion.enabled = false;
                }

                // Move player
                player.transform.position = exitPos;
                Debug.Log($"[StationLandingPad] Moved player to {exitPos}, actual position: {player.transform.position}");

                // Show player model
                Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.enabled = true;
                }

                // Re-enable character controller AFTER moving
                if (charController != null)
                {
                    charController.enabled = true;
                }

                if (locomotion != null)
                {
                    locomotion.enabled = true;
                }

                Debug.Log("[StationLandingPad] Player exited ship");
            }

            // Wait a frame before re-enabling camera
            yield return null;

            // Re-enable camera controller AFTER player is set up
            Camera mainCam = Camera.main;
            Debug.Log($"[StationLandingPad] Main camera found: {mainCam != null}, Player still exists: {player != null}");

            if (mainCam != null && player != null)
            {
                var cameraController = mainCam.GetComponent<Opsive.UltimateCharacterController.Camera.CameraController>();
                Debug.Log($"[StationLandingPad] Camera controller found: {cameraController != null}, enabled: {cameraController?.enabled}");

                if (cameraController != null)
                {
                    // Make sure it's disabled first, then re-enable
                    cameraController.enabled = false;
                    cameraController.Character = player;

                    // Force camera to player's position before enabling
                    mainCam.transform.position = player.transform.position + new Vector3(0, 2, -5);
                    Debug.Log($"[StationLandingPad] Moved camera to {mainCam.transform.position}");

                    cameraController.enabled = true;

                    Debug.Log($"[StationLandingPad] Camera controller re-enabled and linked to player '{player.name}'");
                }
                else
                {
                    Debug.LogWarning("[StationLandingPad] No Opsive Camera Controller found on main camera!");
                }
            }
            else
            {
                Debug.LogError($"[StationLandingPad] Cannot setup camera - mainCam: {mainCam != null}, player: {player != null}");
            }

            // Park the ship
            Vector3 parkPos = transform.position + transform.TransformDirection(_shipParkPosition);

            // Smoothly move ship to park position
            float elapsed = 0f;
            Vector3 startPos = _nearbyShip.transform.position;

            while (elapsed < _landingDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _landingDuration;
                _nearbyShip.transform.position = Vector3.Lerp(startPos, parkPos, t);
                yield return null;
            }

            _nearbyShip.transform.position = parkPos;
            _parkedShip = _nearbyShip;

            Debug.Log("[StationLandingPad] Ship parked");

            _isLanding = false;
        }

        /// <summary>
        /// Displays landing prompt
        /// </summary>
        private void OnGUI()
        {
            if (_shipInRange && !_isLanding && _shipController != null && _shipController.IsActive)
            {
                GUI.skin.label.fontSize = 20;
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.color = Color.green;

                Rect promptRect = new Rect(
                    Screen.width / 2 - 150,
                    Screen.height - 100,
                    300,
                    40
                );

                GUI.Label(promptRect, "Press F to Land");
                GUI.color = Color.white;
            }
        }

        /// <summary>
        /// Draw debug visualization
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw landing range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _landingRange);

            // Draw ship park position
            Gizmos.color = Color.blue;
            Vector3 parkPos = transform.position + transform.TransformDirection(_shipParkPosition);
            Gizmos.DrawWireCube(parkPos, new Vector3(10f, 5f, 15f));

            // Draw player exit position
            Gizmos.color = Color.green;
            Vector3 exitPos = transform.position + transform.TransformDirection(_playerExitPosition);
            Gizmos.DrawWireSphere(exitPos, 1f);
            Gizmos.DrawLine(transform.position, exitPos);
        }
    }
}
