/*
 * Ship Entry Point for Klyra's Reach
 *
 * PURPOSE:
 * Allows player to enter and exit ships by pressing F when nearby.
 * Handles the transition between on-foot character and ship piloting.
 *
 * HOW TO USE:
 * 1. Attach this script to a ship GameObject (same object with ShipController)
 * 2. Set the detection range (how close player needs to be)
 * 3. Optionally set an entry position (where player spawns when exiting)
 * 4. Press F when near the ship to enter/exit
 */

using UnityEngine;
using UnityEngine.InputSystem;

namespace KlyrasReach.Player
{
    /// <summary>
    /// Handles player entering and exiting ships
    /// </summary>
    public class ShipEntryPoint : MonoBehaviour
    {
        [Header("Entry Settings")]
        [Tooltip("How close the player needs to be to enter (in meters)")]
        [SerializeField] private float _entryRange = 3f;

        [Tooltip("Tag used to identify the player")]
        [SerializeField] private string _playerTag = "Player";

        [Header("Exit Settings")]
        [Tooltip("Where the player appears when exiting the ship (relative to ship)")]
        [SerializeField] private Vector3 _exitOffset = new Vector3(2f, 0f, 0f);

        [Header("UI (Optional)")]
        [Tooltip("Show interaction prompt on screen")]
        [SerializeField] private bool _showPrompt = true;

        // Private references
        private ShipController _shipController;
        private Transform _closestPlayerTransform;
        private GameObject _closestPlayerObject;
        private Camera _mainCamera;
        private bool _playerInRange = false;
        private bool _playerIsPiloting = false;

        // Store player's character controller for enabling/disabling
        private MonoBehaviour _playerController;
        private MonoBehaviour _opsiveCameraController;

        /// <summary>
        /// Initialize references
        /// </summary>
        private void Awake()
        {
            // Get the ship controller on this same GameObject
            _shipController = GetComponent<ShipController>();
            if (_shipController == null)
            {
                Debug.LogError($"[ShipEntryPoint] No ShipController found on '{gameObject.name}'! Please add one.");
                enabled = false;
                return;
            }

            // Find main camera
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogWarning("[ShipEntryPoint] No main camera found! Tag a camera as 'MainCamera'.");
            }

            Debug.Log($"[ShipEntryPoint] Entry point initialized on '{gameObject.name}'");
        }

        /// <summary>
        /// Called every frame - check for player proximity and input
        /// </summary>
        private void Update()
        {
            // Find closest player each frame (handles multiplayer)
            FindClosestPlayer();

            if (_closestPlayerTransform == null)
            {
                _playerInRange = false;
                return;
            }

            // Check if closest player is in range
            float distance = Vector3.Distance(transform.position, _closestPlayerTransform.position);
            _playerInRange = distance <= _entryRange;

            // Handle interaction input (F key using new Input System)
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
            {
                Debug.Log($"[ShipEntryPoint] F pressed - playerIsPiloting: {_playerIsPiloting}, playerInRange: {_playerInRange}, distance: {distance:F2}");

                if (_playerIsPiloting)
                {
                    // F key is disabled while piloting - must use landing zones to land/exit
                    Debug.Log("[ShipEntryPoint] Cannot exit ship while flying - find a landing zone!");
                }
                else if (_playerInRange)
                {
                    // Enter ship
                    EnterShip();
                }
                else
                {
                    Debug.Log($"[ShipEntryPoint] Player not in range (distance: {distance:F2}, required: {_entryRange})");
                }
            }
        }

        /// <summary>
        /// Finds the closest player to the ship (handles multiplayer)
        /// </summary>
        private void FindClosestPlayer()
        {
            GameObject[] allPlayers = GameObject.FindGameObjectsWithTag(_playerTag);

            if (allPlayers == null || allPlayers.Length == 0)
            {
                _closestPlayerObject = null;
                _closestPlayerTransform = null;
                return;
            }

            // Find closest player
            GameObject closestPlayer = null;
            float closestDistance = float.MaxValue;

            foreach (GameObject player in allPlayers)
            {
                if (player == null) continue;

                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player;
                }
            }

            if (closestPlayer != null)
            {
                _closestPlayerObject = closestPlayer;
                _closestPlayerTransform = closestPlayer.transform;
            }
        }

        /// <summary>
        /// Player enters the ship
        /// </summary>
        private void EnterShip()
        {
            if (_closestPlayerObject == null || _shipController == null)
            {
                return;
            }

            Debug.Log($"[ShipEntryPoint] Player '{_closestPlayerObject.name}' entering ship '{gameObject.name}'");

            // Disable player character controller
            DisablePlayerController();

            // Disable Opsive camera controller so it doesn't fight with ship camera
            DisableOpsiveCameraController();

            // Hide player model
            SetPlayerVisibility(false);

            // Activate ship controls (pass main camera)
            Camera cameraToUse = _mainCamera != null ? _mainCamera : Camera.main;
            _shipController.EnterShip(cameraToUse);

            _playerIsPiloting = true;

            Debug.Log($"[ShipEntryPoint] Ship activated, camera assigned: {cameraToUse != null}");
        }

        /// <summary>
        /// Public method to reset piloting state (called by landing pads)
        /// </summary>
        public void ResetPilotingState()
        {
            _playerIsPiloting = false;
            Debug.Log("[ShipEntryPoint] Piloting state reset by external system");
        }

        /// <summary>
        /// Player exits the ship
        /// </summary>
        private void ExitShip()
        {
            if (_closestPlayerObject == null || _shipController == null)
            {
                return;
            }

            Debug.Log($"[ShipEntryPoint] Player '{_closestPlayerObject.name}' exiting ship '{gameObject.name}'");

            // Deactivate ship controls
            _shipController.ExitShip();

            // Position player at exit point
            Vector3 exitPosition = transform.position + transform.TransformDirection(_exitOffset);
            _closestPlayerTransform.position = exitPosition;

            // Show player model
            SetPlayerVisibility(true);

            // Re-enable player character controller
            EnablePlayerController();

            // Re-enable Opsive camera controller
            EnableOpsiveCameraController();

            _playerIsPiloting = false;
        }

        /// <summary>
        /// Disables the player's character controller to prevent movement conflicts
        /// </summary>
        private void DisablePlayerController()
        {
            if (_closestPlayerObject == null) return;

            // Try to find Opsive character controller
            var locomotion = _closestPlayerObject.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
            if (locomotion != null)
            {
                _playerController = locomotion;
                locomotion.enabled = false;
                Debug.Log("[ShipEntryPoint] Disabled Opsive character controller");
                return;
            }

            // Fallback: disable any CharacterController component
            var charController = _closestPlayerObject.GetComponent<CharacterController>();
            if (charController != null)
            {
                charController.enabled = false;
                Debug.Log("[ShipEntryPoint] Disabled CharacterController");
            }
        }

        /// <summary>
        /// Re-enables the player's character controller
        /// </summary>
        private void EnablePlayerController()
        {
            if (_playerController != null)
            {
                _playerController.enabled = true;
                Debug.Log("[ShipEntryPoint] Re-enabled character controller");
                return;
            }

            // Fallback: re-enable CharacterController
            if (_closestPlayerObject != null)
            {
                var charController = _closestPlayerObject.GetComponent<CharacterController>();
                if (charController != null)
                {
                    charController.enabled = true;
                }
            }
        }

        /// <summary>
        /// Shows or hides the player model
        /// </summary>
        private void SetPlayerVisibility(bool visible)
        {
            if (_closestPlayerObject == null) return;

            // Find all renderers on player and children
            Renderer[] renderers = _closestPlayerObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = visible;
            }

            Debug.Log($"[ShipEntryPoint] Player visibility set to: {visible}");
        }

        /// <summary>
        /// Disables Opsive Camera Controller to prevent conflicts
        /// </summary>
        private void DisableOpsiveCameraController()
        {
            if (_mainCamera == null) return;

            // Try to find Opsive's Camera Controller component
            var cameraController = _mainCamera.GetComponent<Opsive.UltimateCharacterController.Camera.CameraController>();
            if (cameraController != null)
            {
                _opsiveCameraController = cameraController;
                cameraController.enabled = false;
                Debug.Log("[ShipEntryPoint] Disabled Opsive Camera Controller");
            }
        }

        /// <summary>
        /// Re-enables Opsive Camera Controller
        /// </summary>
        private void EnableOpsiveCameraController()
        {
            if (_opsiveCameraController != null)
            {
                _opsiveCameraController.enabled = true;
                Debug.Log("[ShipEntryPoint] Re-enabled Opsive Camera Controller");
            }
        }

        /// <summary>
        /// Display interaction prompt on screen
        /// </summary>
        private void OnGUI()
        {
            if (!_showPrompt)
            {
                return;
            }

            // Show prompt when player is in range but not piloting
            if (_playerInRange && !_playerIsPiloting)
            {
                // Calculate screen position
                Vector3 screenPos = _mainCamera.WorldToScreenPoint(transform.position);

                // Only show if ship is in front of camera
                if (screenPos.z > 0)
                {
                    // Draw prompt text
                    GUI.skin.label.fontSize = 18;
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.Label(
                        new Rect(screenPos.x - 100, Screen.height - screenPos.y - 50, 200, 30),
                        "Press F to Enter Ship"
                    );
                }
            }
            // Show exit prompt when piloting
            else if (_playerIsPiloting)
            {
                GUI.skin.label.fontSize = 16;
                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                GUI.Label(
                    new Rect(10, 10, 300, 30),
                    "Press F to Exit Ship"
                );
            }
        }

        /// <summary>
        /// Draw debug visuals in Scene view
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw entry range
            Gizmos.color = new Color(0, 1, 1, 0.3f); // Cyan
            Gizmos.DrawWireSphere(transform.position, _entryRange);

            // Draw exit position
            Gizmos.color = Color.green;
            Vector3 exitPos = transform.position + transform.TransformDirection(_exitOffset);
            Gizmos.DrawWireSphere(exitPos, 0.5f);
            Gizmos.DrawLine(transform.position, exitPos);
        }
    }
}
