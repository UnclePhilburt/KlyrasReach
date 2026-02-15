/*
 * Ship Piloting System for Klyra's Reach
 *
 * PURPOSE:
 * Handles entering/exiting flight mode from the ship interior.
 * When player interacts with cockpit door:
 * - Player character disappears (or freezes)
 * - Camera switches to first-person cockpit view
 * - ShipController activates for flight controls
 * - Other players see pilot is flying (via network sync)
 *
 * HOW TO USE:
 * 1. Add this script to your player character prefab
 * 2. Make sure player has a camera component
 * 3. Ship must have CockpitDoorTrigger with tag "CockpitDoor"
 * 4. Press F when near cockpit door to enter flight mode
 * 5. Press F again to exit flight mode
 */

using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Camera;

namespace KlyrasReach.Player
{
    /// <summary>
    /// Manages entering/exiting ship piloting mode for networked players
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class ShipPilotingSystem : MonoBehaviourPunCallbacks
    {
        [Header("Input")]
        [Tooltip("Key to press to enter/exit piloting mode")]
        [SerializeField] private KeyCode _pilotKey = KeyCode.F;

        [Header("References")]
        [Tooltip("Player's camera (child of this character)")]
        [SerializeField] private Camera _playerCamera;

        [Tooltip("Character's UltimateCharacterLocomotion component")]
        [SerializeField] private UltimateCharacterLocomotion _characterController;

        [Tooltip("Opsive's Camera Controller (will be disabled during flight)")]
        [SerializeField] private CameraController _cameraController;

        // State
        private bool _isPiloting = false;
        private bool _isNearCockpitDoor = false;
        private GameObject _currentShip = null;
        private ShipController _shipController = null;
        private Vector3 _exitPosition; // Where to teleport back when exiting
        private Quaternion _exitRotation;

        // Character visuals (to hide when piloting)
        private Renderer[] _characterRenderers;

        // Camera parenting
        private Transform _originalCameraParent;

        private void Start()
        {
            // Only initialize for local player
            if (!photonView.IsMine)
            {
                return;
            }

            // Find the Main Camera - this is the only camera in the scene
            if (_playerCamera == null)
            {
                GameObject mainCamObj = GameObject.FindGameObjectWithTag("MainCamera");
                if (mainCamObj != null)
                {
                    _playerCamera = mainCamObj.GetComponent<Camera>();
                    Debug.Log($"[ShipPilotingSystem] Found Main Camera: {_playerCamera.name}");
                }
                else
                {
                    Debug.LogError("[ShipPilotingSystem] No Main Camera found in scene!");
                }
            }

            // Find character controller if not assigned
            if (_characterController == null)
            {
                _characterController = GetComponent<UltimateCharacterLocomotion>();
            }

            // Find Opsive camera controller (controls camera movement)
            if (_cameraController == null)
            {
                _cameraController = FindObjectOfType<CameraController>();
                if (_cameraController != null)
                {
                    Debug.Log($"[ShipPilotingSystem] Found CameraController: {_cameraController.name}");
                }
                else
                {
                    Debug.LogWarning("[ShipPilotingSystem] No CameraController found - camera may not switch properly");
                }
            }

            // Find all renderers to hide character (get all mesh/skinned mesh renderers)
            _characterRenderers = GetComponentsInChildren<Renderer>();
            Debug.Log($"[ShipPilotingSystem] Found {_characterRenderers.Length} renderers to hide when piloting");

            Debug.Log($"[ShipPilotingSystem] Initialized on {gameObject.name}");
        }

        private void Update()
        {
            // Only process input for local player
            if (!photonView.IsMine)
            {
                return;
            }

            // Check for keyboard availability
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            // Check for pilot key press (F key)
            if (keyboard.fKey.wasPressedThisFrame)
            {
                if (_isNearCockpitDoor && !_isPiloting)
                {
                    // Enter piloting mode
                    EnterPilotMode();
                }
                else if (_isPiloting)
                {
                    // Exit piloting mode
                    ExitPilotMode();
                }
            }
        }

        /// <summary>
        /// Called when player enters cockpit door trigger
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            // Only local player can pilot
            if (!photonView.IsMine)
            {
                return;
            }

            // Check if this is a cockpit door
            if (other.CompareTag("CockpitDoor"))
            {
                _isNearCockpitDoor = true;

                // Get the ship this cockpit belongs to
                _currentShip = other.transform.root.gameObject;
                _shipController = _currentShip.GetComponent<ShipController>();

                if (_shipController == null)
                {
                    Debug.LogError("[ShipPilotingSystem] Cockpit door's ship has no ShipController!");
                    _isNearCockpitDoor = false;
                    return;
                }

                Debug.Log("[ShipPilotingSystem] Near cockpit door - Press F to pilot ship");

                // TODO: Show UI prompt "Press F to Pilot Ship"
            }
        }

        /// <summary>
        /// Called when player leaves cockpit door trigger
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            if (!photonView.IsMine)
            {
                return;
            }

            if (other.CompareTag("CockpitDoor"))
            {
                _isNearCockpitDoor = false;

                // Only clear ship reference if not currently piloting
                if (!_isPiloting)
                {
                    _currentShip = null;
                    _shipController = null;
                }

                Debug.Log("[ShipPilotingSystem] Left cockpit door area");

                // TODO: Hide UI prompt
            }
        }

        /// <summary>
        /// Enter flight mode - hide character, activate ship controls
        /// </summary>
        private void EnterPilotMode()
        {
            if (_currentShip == null || _shipController == null)
            {
                Debug.LogError("[ShipPilotingSystem] Cannot enter pilot mode - no ship!");
                return;
            }

            Debug.Log("[ShipPilotingSystem] Entering pilot mode...");

            // IMPORTANT: Request ownership of ship so we can control it in multiplayer
            PhotonView shipPhotonView = _currentShip.GetComponent<PhotonView>();
            if (shipPhotonView != null)
            {
                shipPhotonView.RequestOwnership();
                Debug.Log($"[ShipPilotingSystem] Requested ownership of ship (current owner: {shipPhotonView.Owner?.NickName})");
            }
            else
            {
                Debug.LogWarning("[ShipPilotingSystem] Ship has no PhotonView - multiplayer sync may not work!");
            }

            // Save exit position (where character currently is)
            _exitPosition = transform.position;
            _exitRotation = transform.rotation;

            // IMPORTANT: Move player to a spawn point INSIDE ship interior before hiding them
            // This ensures they're in a safe position when they exit
            Transform[] spawnPoints = FindSpawnPointsInShip(_currentShip);
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform spawnPoint = spawnPoints[0];
                // Set position relative to ship (local space)
                transform.localPosition = spawnPoint.localPosition + Vector3.up * 0.5f;
                transform.localRotation = spawnPoint.localRotation;
                Debug.Log($"[ShipPilotingSystem] Moved character to spawn point: {spawnPoint.name}");
            }
            else
            {
                Debug.LogWarning("[ShipPilotingSystem] No spawn points found - character may be in unsafe position!");
            }

            // Disable Opsive's camera controller (so it stops following player)
            if (_cameraController != null)
            {
                _cameraController.enabled = false;
                Debug.Log("[ShipPilotingSystem] Disabled Opsive CameraController");
            }

            // NOTE: We keep the Main Camera enabled - it's the only camera!
            // Just disable the scripts that control it

            // Disable character controller to prevent walking during flight
            if (_characterController != null)
            {
                _characterController.enabled = false;
                Debug.Log("[ShipPilotingSystem] Disabled character controller (prevents walking during flight)");
            }

            // Hide all character renderers (make invisible)
            if (_characterRenderers != null)
            {
                foreach (Renderer renderer in _characterRenderers)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = false;
                    }
                }
                Debug.Log($"[ShipPilotingSystem] Hid {_characterRenderers.Length} renderers");
            }

            // Transfer Main Camera control to ship
            if (_playerCamera != null)
            {
                // Camera is already enabled - just take control of it
                Debug.Log($"[ShipPilotingSystem] Taking control of Main Camera: {_playerCamera.name}");

                // Unparent camera (Opsive might have it parented somewhere)
                _originalCameraParent = _playerCamera.transform.parent;
                if (_originalCameraParent != null)
                {
                    _playerCamera.transform.SetParent(null);
                    Debug.Log($"[ShipPilotingSystem] Unparented camera from {_originalCameraParent.name}");
                }

                // Activate ship controller with the camera
                _shipController.EnterShip(_playerCamera);
                Debug.Log("[ShipPilotingSystem] Transferred camera control to ship");
            }
            else
            {
                Debug.LogError("[ShipPilotingSystem] No camera found!");
            }

            // Sync with network (tell other players this player is piloting)
            photonView.RPC("RPC_SetPiloting", RpcTarget.AllBuffered, true);

            _isPiloting = true;

            Debug.Log("[ShipPilotingSystem] ✓ Now piloting ship!");
        }

        /// <summary>
        /// Exit flight mode - show character, deactivate ship controls
        /// </summary>
        private void ExitPilotMode()
        {
            if (_shipController == null)
            {
                Debug.LogError("[ShipPilotingSystem] Cannot exit pilot mode - no ship controller!");
                return;
            }

            Debug.Log("[ShipPilotingSystem] Exiting pilot mode...");

            // FIRST: Stop the ship completely (zero out all velocity)
            Rigidbody shipRigidbody = _currentShip.GetComponent<Rigidbody>();
            if (shipRigidbody != null)
            {
                shipRigidbody.linearVelocity = Vector3.zero;
                shipRigidbody.angularVelocity = Vector3.zero;
                shipRigidbody.isKinematic = true; // Make it kinematic so it stops moving
                Debug.Log("[ShipPilotingSystem] Stopped ship movement");
            }

            // Deactivate ship controller
            _shipController.ExitShip();

            Debug.Log($"[ShipPilotingSystem] BEFORE EXIT - Position: {transform.position}, Local Position: {transform.localPosition}");
            Debug.Log($"[ShipPilotingSystem] BEFORE EXIT - Parent: {(transform.parent != null ? transform.parent.name : "null")}");

            // Reparent camera back to character
            if (_playerCamera != null && _originalCameraParent != null)
            {
                _playerCamera.transform.SetParent(_originalCameraParent);
                Debug.Log($"[ShipPilotingSystem] Reparented camera to {_originalCameraParent.name}");
            }

            Debug.Log($"[ShipPilotingSystem] AFTER CAMERA REPARENT - Position: {transform.position}, Local Position: {transform.localPosition}");

            // Show character renderers (make visible again)
            if (_characterRenderers != null)
            {
                foreach (Renderer renderer in _characterRenderers)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = true;
                    }
                }
                Debug.Log($"[ShipPilotingSystem] Showed {_characterRenderers.Length} renderers");
            }

            Debug.Log($"[ShipPilotingSystem] AFTER SHOW RENDERERS - Position: {transform.position}, Local Position: {transform.localPosition}");

            // Re-enable character controller
            if (_characterController != null)
            {
                _characterController.enabled = true;
                Debug.Log("[ShipPilotingSystem] Re-enabled character controller");
            }

            // Re-enable Opsive's camera controller
            if (_cameraController != null)
            {
                _cameraController.enabled = true;
                Debug.Log("[ShipPilotingSystem] Re-enabled Opsive CameraController");
            }

            Debug.Log($"[ShipPilotingSystem] AFTER CAMERA CONTROLLER - Position: {transform.position}, Local Position: {transform.localPosition}");

            // Sync with network
            photonView.RPC("RPC_SetPiloting", RpcTarget.AllBuffered, false);

            _isPiloting = false;

            Debug.Log($"[ShipPilotingSystem] FINAL EXIT - Position: {transform.position}, Local Position: {transform.localPosition}");
            Debug.Log("[ShipPilotingSystem] ✓ Exited ship - back to walking mode");

            // Monitor position for next few frames to see what's resetting it
            StartCoroutine(MonitorPositionAfterExit());
        }

        /// <summary>
        /// Monitor position for a few frames after exiting to detect what's resetting it
        /// </summary>
        private System.Collections.IEnumerator MonitorPositionAfterExit()
        {
            Vector3 lastPosition = transform.position;
            Vector3 lastLocalPosition = transform.localPosition;

            for (int i = 0; i < 30; i++)
            {
                yield return null; // Wait one frame

                if (transform.position != lastPosition || transform.localPosition != lastLocalPosition)
                {
                    Debug.LogError($"[ShipPilotingSystem] ⚠️ POSITION CHANGED on frame {i + 1}!");
                    Debug.LogError($"[ShipPilotingSystem] Old Position: {lastPosition}, New Position: {transform.position}");
                    Debug.LogError($"[ShipPilotingSystem] Old Local: {lastLocalPosition}, New Local: {transform.localPosition}");
                    Debug.LogError($"[ShipPilotingSystem] Parent: {(transform.parent != null ? transform.parent.name : "null")}");

                    lastPosition = transform.position;
                    lastLocalPosition = transform.localPosition;
                }
            }

            Debug.Log("[ShipPilotingSystem] Position monitoring complete - no more changes detected");
        }


        /// <summary>
        /// Find spawn points inside a ship
        /// </summary>
        private Transform[] FindSpawnPointsInShip(GameObject ship)
        {
            // Look for objects named "SpawnPoint_" that are children of this ship
            Transform[] allChildren = ship.GetComponentsInChildren<Transform>();
            System.Collections.Generic.List<Transform> spawnPoints = new System.Collections.Generic.List<Transform>();

            foreach (Transform child in allChildren)
            {
                if (child.name.Contains("SpawnPoint_"))
                {
                    spawnPoints.Add(child);
                }
            }

            Debug.Log($"[ShipPilotingSystem] Found {spawnPoints.Count} spawn points in ship");
            return spawnPoints.ToArray();
        }

        /// <summary>
        /// Network RPC to sync piloting state across all clients
        /// </summary>
        [PunRPC]
        private void RPC_SetPiloting(bool isPiloting)
        {
            _isPiloting = isPiloting;

            // For remote players, just hide/show their character renderers
            if (!photonView.IsMine)
            {
                if (_characterRenderers != null)
                {
                    foreach (Renderer renderer in _characterRenderers)
                    {
                        if (renderer != null)
                        {
                            renderer.enabled = !isPiloting;
                        }
                    }
                }

                Debug.Log($"[ShipPilotingSystem] Player {photonView.Owner.NickName} is {(isPiloting ? "piloting" : "walking")}");
            }
        }

        /// <summary>
        /// Display piloting status on screen for debugging
        /// </summary>
        private void OnGUI()
        {
            if (!photonView.IsMine)
            {
                return;
            }

            if (_isNearCockpitDoor && !_isPiloting)
            {
                // Show prompt to enter pilot mode
                GUILayout.BeginArea(new Rect(Screen.width / 2 - 100, Screen.height - 100, 200, 50));
                GUILayout.Label("Press F to Pilot Ship", new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    alignment = TextAnchor.MiddleCenter,
                    normal = new GUIStyleState() { textColor = Color.white }
                });
                GUILayout.EndArea();
            }
            else if (_isPiloting)
            {
                // Show prompt to exit pilot mode
                GUILayout.BeginArea(new Rect(10, Screen.height - 50, 300, 50));
                GUILayout.Label("Press F to Exit Ship", new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    normal = new GUIStyleState() { textColor = Color.yellow }
                });
                GUILayout.EndArea();
            }
        }
    }
}
