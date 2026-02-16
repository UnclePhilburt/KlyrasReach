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
        [SerializeField] private Key _dockKey = Key.R;

        [Header("UI Settings")]
        [Tooltip("Color of the docking UI prompt")]
        [SerializeField] private Color _promptColor = Color.green;

        [Tooltip("Font size for docking prompt")]
        [SerializeField] private int _fontSize = 20;

        [Header("Visual Outline")]
        [Tooltip("Show visual outline of docking zone")]
        [SerializeField] private bool _showOutline = true;

        [Tooltip("Color of the docking zone outline")]
        [SerializeField] private Color _outlineColor = Color.green;

        [Tooltip("Width of the outline")]
        [SerializeField] private float _outlineWidth = 0.5f;

        [Tooltip("Pulse outline when ship is in range")]
        [SerializeField] private bool _pulseWhenInRange = true;

        // Private state
        private bool _shipInRange = false;
        private GameObject _dockedShip = null;
        private Player.ShipController _shipController = null;
        private bool _isDocking = false;
        private LineRenderer _lineRenderer;

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

            // Create visual outline
            if (_showOutline)
            {
                CreateOutline(collider);
            }

            Debug.Log($"[DockingStation] '{_stationName}' initialized. Will load scene: {_destinationSceneName}");
        }

        /// <summary>
        /// Creates a visual outline of the docking zone
        /// </summary>
        private void CreateOutline(Collider collider)
        {
            if (collider == null)
                return;

            // Create child object for line renderer
            GameObject lineObj = new GameObject("DockingZoneOutline");
            lineObj.transform.SetParent(transform);
            lineObj.transform.localPosition = Vector3.zero;
            lineObj.transform.localRotation = Quaternion.identity;

            _lineRenderer = lineObj.AddComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = false;
            _lineRenderer.loop = true;
            _lineRenderer.startWidth = _outlineWidth;
            _lineRenderer.endWidth = _outlineWidth;
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.startColor = _outlineColor;
            _lineRenderer.endColor = _outlineColor;

            // Create outline points based on collider type
            if (collider is BoxCollider boxCollider)
            {
                CreateBoxOutline(boxCollider);
            }
            else if (collider is SphereCollider sphereCollider)
            {
                CreateSphereOutline(sphereCollider);
            }
        }

        /// <summary>
        /// Creates outline for BoxCollider
        /// </summary>
        private void CreateBoxOutline(BoxCollider boxCollider)
        {
            Vector3 center = boxCollider.center;
            Vector3 size = boxCollider.size;
            Vector3 halfSize = size / 2f;

            // 12 edges of a box = 24 points (each edge needs 2 points)
            // But we'll draw it as connected lines
            Vector3[] corners = new Vector3[8];
            corners[0] = center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
            corners[1] = center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
            corners[2] = center + new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            corners[3] = center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
            corners[4] = center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            corners[5] = center + new Vector3(halfSize.x, halfSize.y, -halfSize.z);
            corners[6] = center + new Vector3(halfSize.x, halfSize.y, halfSize.z);
            corners[7] = center + new Vector3(-halfSize.x, halfSize.y, halfSize.z);

            // Draw bottom face, then verticals, then top face
            _lineRenderer.positionCount = 16;

            // Bottom face
            _lineRenderer.SetPosition(0, corners[0]);
            _lineRenderer.SetPosition(1, corners[1]);
            _lineRenderer.SetPosition(2, corners[2]);
            _lineRenderer.SetPosition(3, corners[3]);
            _lineRenderer.SetPosition(4, corners[0]); // Close bottom face

            // Vertical edges
            _lineRenderer.SetPosition(5, corners[4]);
            _lineRenderer.SetPosition(6, corners[5]);
            _lineRenderer.SetPosition(7, corners[1]); // Connect to bottom
            _lineRenderer.SetPosition(8, corners[2]);
            _lineRenderer.SetPosition(9, corners[6]);
            _lineRenderer.SetPosition(10, corners[7]);
            _lineRenderer.SetPosition(11, corners[3]); // Connect to bottom

            // Top face
            _lineRenderer.SetPosition(12, corners[4]);
            _lineRenderer.SetPosition(13, corners[5]);
            _lineRenderer.SetPosition(14, corners[6]);
            _lineRenderer.SetPosition(15, corners[7]);

            _lineRenderer.loop = true;
        }

        /// <summary>
        /// Creates outline for SphereCollider
        /// </summary>
        private void CreateSphereOutline(SphereCollider sphereCollider)
        {
            Vector3 center = sphereCollider.center;
            float radius = sphereCollider.radius;

            // Draw 3 circles (XY, XZ, YZ planes)
            int segments = 32;
            _lineRenderer.positionCount = segments * 3;

            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float nextAngle = ((i + 1) / (float)segments) * Mathf.PI * 2f;

                // XY circle
                _lineRenderer.SetPosition(i, center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0
                ));

                // XZ circle
                _lineRenderer.SetPosition(segments + i, center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                ));

                // YZ circle
                _lineRenderer.SetPosition(segments * 2 + i, center + new Vector3(
                    0,
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius
                ));
            }

            _lineRenderer.loop = false;
        }

        /// <summary>
        /// Check for docking input every frame
        /// </summary>
        private void Update()
        {
            // Update outline pulse effect
            if (_lineRenderer != null && _pulseWhenInRange && _shipInRange)
            {
                // Pulse the outline color
                float pulse = (Mathf.Sin(Time.time * 3f) + 1f) / 2f; // 0 to 1
                Color pulseColor = Color.Lerp(_outlineColor * 0.5f, _outlineColor, pulse);
                _lineRenderer.startColor = pulseColor;
                _lineRenderer.endColor = pulseColor;
            }
            else if (_lineRenderer != null)
            {
                // Reset to normal color
                _lineRenderer.startColor = _outlineColor;
                _lineRenderer.endColor = _outlineColor;
            }

            // Debug logging every 60 frames
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[DockingStation] _shipInRange: {_shipInRange}, _dockedShip: {(_dockedShip != null ? _dockedShip.name : "null")}, _shipController: {(_shipController != null ? "exists" : "null")}, IsActive: {(_shipController != null ? _shipController.IsActive.ToString() : "N/A")}");
            }

            // Only process input if we're the pilot of the ship in range
            if (!_shipInRange || _dockedShip == null || _shipController == null || _isDocking)
            {
                return;
            }

            // Only allow docking if player is piloting the ship
            if (!_shipController.IsActive)
            {
                Debug.Log("[DockingStation] Ship controller not active - cannot dock");
                return;
            }

            // IMPORTANT: Check if the LOCAL player is the one piloting this ship
            bool isLocalPiloting = IsLocalPlayerPiloting();
            if (!isLocalPiloting)
            {
                Debug.Log("[DockingStation] Not local player piloting - cannot dock");
                return;
            }

            // Check for dock key press
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[_dockKey].wasPressedThisFrame)
            {
                Debug.Log("[DockingStation] R key pressed - initiating docking!");
                InitiateDocking();
            }
        }

        /// <summary>
        /// Checks if the local player is the one piloting the ship in range
        /// </summary>
        private bool IsLocalPlayerPiloting()
        {
            // METHOD 1: Check if there's a ShipPilotingSystem on local player (old system)
            Player.ShipPilotingSystem[] allPilotingSystems = FindObjectsByType<Player.ShipPilotingSystem>(FindObjectsSortMode.None);

            foreach (var pilotingSystem in allPilotingSystems)
            {
                // Check if this is the local player's piloting system
                PhotonView pv = pilotingSystem.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    return true;
                }
            }

            // METHOD 2: Check if ship is active (works with SimpleShipPiloting setup)
            // If the ship controller is active and no other players are piloting,
            // assume the local player is piloting
            if (_shipController != null && _shipController.IsActive)
            {
                // In single player or when we're the only one piloting, this is good enough
                return true;
            }

            return false;
        }

        /// <summary>
        /// Called when a ship enters the docking trigger
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[DockingStation] OnTriggerEnter - Something entered! Name: {other.gameObject.name}, Tag: {other.tag}");

            // Check if this is a ship
            if (other.CompareTag(_shipTag))
            {
                GameObject ship = other.gameObject;
                Debug.Log($"[DockingStation] ✓ Tag matches! Ship: {ship.name}");

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

                Debug.Log($"[DockingStation] ✓✓ Ship '{ship.name}' entered docking range of '{_stationName}'");
            }
            else
            {
                Debug.Log($"[DockingStation] Tag mismatch. Looking for '{_shipTag}', got '{other.tag}'");
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

            // IMPORTANT: Save ship position so we can reposition it when returning
            if (_dockedShip != null)
            {
                ShipRepositioner.SaveShipPosition(_dockedShip.transform.position, _dockedShip.transform.rotation);
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
