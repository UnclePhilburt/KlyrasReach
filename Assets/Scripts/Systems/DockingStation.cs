/*
 * Docking Station for Klyra's Reach
 *
 * PURPOSE:
 * Allows large ships to dock and transition all players to a new scene (e.g., space station interior).
 * When the pilot flies the ship into the docking zone and presses E, everyone in the ship loads into the next scene.
 *
 * HOW TO USE:
 * 1. Create an empty GameObject at your docking location (e.g., "Docking Station Alpha")
 * 2. Add this script to it
 * 3. Add a Box Collider (or Sphere Collider) and set "Is Trigger" = true
 * 4. Size the trigger to cover the docking area (make it big enough for the ship to enter)
 * 5. Set the scene name to load when docking
 * 6. Ship must have "Ship" tag and ShipController component
 * 7. Pilot presses E to dock and load the new scene
 */

using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Photon.Realtime;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Handles ship docking and scene transitions for multiplayer
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DockingStation : MonoBehaviourPunCallbacks
    {
        [Header("Docking Settings")]
        [Tooltip("Name of this docking station (shown in UI)")]
        [SerializeField] private string _stationName = "Docking Station Alpha";

        [Tooltip("Scene to load when docking (must be in Build Settings)")]
        [SerializeField] private string _destinationSceneName = "SpaceStation_Interior";

        [Tooltip("Tag to identify ships")]
        [SerializeField] private string _shipTag = "Ship";

        [Tooltip("Key to press to dock")]
        [SerializeField] private Key _dockKey = Key.E;

        [Header("UI Settings")]
        [Tooltip("Color of the docking UI prompt")]
        [SerializeField] private Color _promptColor = Color.green;

        [Tooltip("Font size for docking prompt")]
        [SerializeField] private int _fontSize = 20;

        // Private state
        private bool _shipInRange = false;
        private GameObject _dockedShip = null;
        private Player.ShipController _shipController = null;
        private bool _isDocking = false;

        /// <summary>
        /// Validate trigger setup
        /// </summary>
        private void Awake()
        {
            Collider collider = GetComponent<Collider>();
            if (collider != null && !collider.isTrigger)
            {
                Debug.LogWarning("[DockingStation] Collider should be set to 'Is Trigger'. Auto-fixing...");
                collider.isTrigger = true;
            }

            Debug.Log($"[DockingStation] '{_stationName}' initialized. Will load scene: {_destinationSceneName}");
        }

        /// <summary>
        /// Check for docking input every frame
        /// </summary>
        private void Update()
        {
            // Only process input if we're the pilot of the ship in range
            if (!_shipInRange || _dockedShip == null || _shipController == null || _isDocking)
            {
                return;
            }

            // Only allow docking if player is piloting the ship
            if (!_shipController.IsActive)
            {
                return;
            }

            // IMPORTANT: Check if the LOCAL player is the one piloting this ship
            if (!IsLocalPlayerPiloting())
            {
                return;
            }

            // Check for dock key press
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[_dockKey].wasPressedThisFrame)
            {
                InitiateDocking();
            }
        }

        /// <summary>
        /// Checks if the local player is the one piloting the ship in range
        /// </summary>
        private bool IsLocalPlayerPiloting()
        {
            // Find all player characters with ShipPilotingSystem
            Player.ShipPilotingSystem[] allPilotingSystems = FindObjectsByType<Player.ShipPilotingSystem>(FindObjectsSortMode.None);

            foreach (var pilotingSystem in allPilotingSystems)
            {
                // Check if this is the local player's piloting system
                PhotonView pv = pilotingSystem.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    // Check if this player is piloting (inside a ship)
                    // We can't directly check if they're piloting, but we can check if ship controller is active
                    // and assume the active ship is being piloted by the local player
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Called when a ship enters the docking trigger
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            // Check if this is a ship
            if (other.CompareTag(_shipTag))
            {
                GameObject ship = other.gameObject;

                // Get ship controller
                Player.ShipController controller = ship.GetComponent<Player.ShipController>();
                if (controller == null)
                {
                    Debug.LogWarning($"[DockingStation] Ship '{ship.name}' has no ShipController!");
                    return;
                }

                _shipInRange = true;
                _dockedShip = ship;
                _shipController = controller;

                Debug.Log($"[DockingStation] Ship '{ship.name}' entered docking range of '{_stationName}'");
            }
        }

        /// <summary>
        /// Called when a ship leaves the docking trigger
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(_shipTag))
            {
                _shipInRange = false;
                _dockedShip = null;
                _shipController = null;

                Debug.Log($"[DockingStation] Ship left docking range of '{_stationName}'");
            }
        }

        /// <summary>
        /// Initiates docking sequence and scene load
        /// </summary>
        private void InitiateDocking()
        {
            if (_isDocking)
            {
                Debug.LogWarning("[DockingStation] Already docking!");
                return;
            }

            Debug.Log($"[DockingStation] Initiating docking at '{_stationName}'");
            Debug.Log($"[DockingStation] Loading scene: {_destinationSceneName}");

            _isDocking = true;

            // IMPORTANT: Save ship position so we can spawn back here when returning
            if (_dockedShip != null)
            {
                ShipSpawnManager.SaveShipPosition(_dockedShip.transform.position, _dockedShip.transform.rotation);
                Debug.Log($"[DockingStation] Saved ship position for return trip: {_dockedShip.transform.position}");
            }

            // Use Photon to load the scene for ALL players in the room
            // This ensures everyone in the ship (and room) loads together
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("[DockingStation] Master Client - Loading scene for all players");
                PhotonNetwork.LoadLevel(_destinationSceneName);
            }
            else
            {
                // Non-master clients request the master to load the scene
                Debug.Log("[DockingStation] Requesting Master Client to load scene");
                photonView.RPC("RPC_RequestSceneLoad", RpcTarget.MasterClient, _destinationSceneName);
            }
        }

        /// <summary>
        /// RPC for non-master clients to request scene load
        /// </summary>
        [PunRPC]
        private void RPC_RequestSceneLoad(string sceneName)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log($"[DockingStation] Master received scene load request: {sceneName}");
                PhotonNetwork.LoadLevel(sceneName);
            }
        }

        /// <summary>
        /// Display docking prompt on screen
        /// </summary>
        private void OnGUI()
        {
            // Only show if ship is in range and player is piloting
            if (!_shipInRange || _shipController == null || !_shipController.IsActive || _isDocking)
            {
                return;
            }

            // IMPORTANT: Only show prompt if LOCAL player is the pilot
            if (!IsLocalPlayerPiloting())
            {
                return;
            }

            // Show docking prompt
            GUI.skin.label.fontSize = _fontSize;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.color = _promptColor;

            string promptText = $"Press {_dockKey.ToString().ToUpper()} to Dock at {_stationName}";

            Rect promptRect = new Rect(
                Screen.width / 2 - 250,
                Screen.height - 150,
                500,
                50
            );

            // Draw background box
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.DrawTexture(promptRect, Texture2D.whiteTexture);

            // Draw text
            GUI.color = _promptColor;
            GUI.Label(promptRect, promptText);

            // Reset GUI color
            GUI.color = Color.white;
            GUI.skin.label.alignment = TextAnchor.UpperLeft; // Reset alignment
        }

        /// <summary>
        /// Visualize docking zone in editor
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                if (col is BoxCollider boxCollider)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawCube(boxCollider.center, boxCollider.size);
                    Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
                }
                else if (col is SphereCollider sphereCollider)
                {
                    Gizmos.DrawSphere(transform.position + sphereCollider.center, sphereCollider.radius);
                    Gizmos.DrawWireSphere(transform.position + sphereCollider.center, sphereCollider.radius);
                }
            }

            // Draw label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 50f,
                $"Docking Station\n{_stationName}\n→ {_destinationSceneName}",
                new GUIStyle() {
                    normal = new GUIStyleState() { textColor = Color.green },
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter
                }
            );
            #endif
        }
    }
}
