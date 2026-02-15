/*
 * Planet Landing Zone for Klyra's Reach
 *
 * PURPOSE:
 * Allows ships to land on planets by detecting when they get close.
 * Shows "Press E to Land" UI when in range, then loads planet surface scene.
 * Planets are huge, so detection range is very large.
 *
 * HOW TO USE:
 * 1. Add this script to your planet GameObject
 * 2. Add a Sphere Collider, set "Is Trigger" = true
 * 3. Make the sphere radius HUGE (e.g., 10000 units) so ships can detect from far away
 * 4. Set the planet name and surface scene to load
 * 5. Create an "Orbit Arrival Point" empty GameObject positioned in orbit (far from planet surface)
 * 6. When ship enters range and pilot presses E, loads surface scene
 * 7. When returning to space, ship spawns at the Orbit Arrival Point
 */

using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Handles planet landing detection and scene transitions
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PlanetLandingZone : MonoBehaviourPunCallbacks
    {
        [Header("Planet Settings")]
        [Tooltip("Name of this planet (shown in UI)")]
        [SerializeField] private string _planetName = "Klyra Prime";

        [Tooltip("Scene to load when landing (planet surface scene)")]
        [SerializeField] private string _surfaceSceneName = "KlyraPrime_Surface";

        [Tooltip("Tag to identify ships")]
        [SerializeField] private string _shipTag = "Ship";

        [Tooltip("Key to press to land")]
        [SerializeField] private Key _landKey = Key.E;

        [Header("Orbit Settings")]
        [Tooltip("Where ship spawns when returning to space (should be in orbit, far from planet)")]
        [SerializeField] private Transform _orbitArrivalPoint;

        [Tooltip("Default orbit distance if no arrival point is set")]
        [SerializeField] private float _defaultOrbitDistance = 5000f;

        [Header("UI Settings")]
        [Tooltip("Color of the landing UI prompt")]
        [SerializeField] private Color _promptColor = new Color(0.3f, 1f, 0.3f); // Green

        [Tooltip("Font size for landing prompt")]
        [SerializeField] private int _fontSize = 20;

        // Private state
        private bool _shipInRange = false;
        private GameObject _nearbyShip = null;
        private Player.ShipController _shipController = null;
        private bool _isLanding = false;

        /// <summary>
        /// Validate trigger setup
        /// </summary>
        private void Awake()
        {
            // Find or create a sphere collider for the landing trigger
            SphereCollider sphereTrigger = GetComponent<SphereCollider>();

            if (sphereTrigger == null)
            {
                Debug.LogWarning($"[PlanetLandingZone] No SphereCollider found on '{_planetName}'. Adding one...");
                sphereTrigger = gameObject.AddComponent<SphereCollider>();
                sphereTrigger.radius = 10000f; // Default large radius
                sphereTrigger.isTrigger = true;
                Debug.Log($"[PlanetLandingZone] Added SphereCollider with radius 10000 to '{_planetName}'");
            }
            else
            {
                // Make sure it's set to trigger
                if (!sphereTrigger.isTrigger)
                {
                    sphereTrigger.isTrigger = true;
                    Debug.Log($"[PlanetLandingZone] Set SphereCollider to trigger on '{_planetName}'");
                }

                Debug.Log($"[PlanetLandingZone] '{_planetName}' landing zone radius: {sphereTrigger.radius} units");
            }

            Debug.Log($"[PlanetLandingZone] '{_planetName}' initialized. Will load scene: {_surfaceSceneName}");
        }

        /// <summary>
        /// Check for landing input every frame
        /// </summary>
        private void Update()
        {
            // Only process input if we're the pilot of the ship in range
            if (!_shipInRange || _nearbyShip == null || _shipController == null || _isLanding)
            {
                return;
            }

            // Only allow landing if player is piloting the ship
            if (!_shipController.IsActive)
            {
                return;
            }

            // IMPORTANT: Check if the LOCAL player is the one piloting
            if (!IsLocalPlayerPiloting())
            {
                return;
            }

            // Check for land key press
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[_landKey].wasPressedThisFrame)
            {
                InitiateLanding();
            }
        }

        /// <summary>
        /// Called when a ship enters the landing zone
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
                    Debug.LogWarning($"[PlanetLandingZone] Ship '{ship.name}' has no ShipController!");
                    return;
                }

                _shipInRange = true;
                _nearbyShip = ship;
                _shipController = controller;

                Debug.Log($"[PlanetLandingZone] Ship '{ship.name}' entered landing range of '{_planetName}'");
            }
        }

        /// <summary>
        /// Called when a ship leaves the landing zone
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(_shipTag))
            {
                _shipInRange = false;
                _nearbyShip = null;
                _shipController = null;

                Debug.Log($"[PlanetLandingZone] Ship left landing range of '{_planetName}'");
            }
        }

        /// <summary>
        /// Checks if the local player is the one piloting the ship
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
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Initiates landing sequence and scene load
        /// </summary>
        private void InitiateLanding()
        {
            if (_isLanding)
            {
                Debug.LogWarning($"[PlanetLandingZone] Already landing on '{_planetName}'!");
                return;
            }

            Debug.Log($"[PlanetLandingZone] Initiating landing on '{_planetName}'");
            Debug.Log($"[PlanetLandingZone] Loading scene: {_surfaceSceneName}");

            _isLanding = true;

            // IMPORTANT: Save orbit arrival position so we can spawn back here when returning to space
            Vector3 orbitPosition;
            Quaternion orbitRotation;

            if (_orbitArrivalPoint != null)
            {
                orbitPosition = _orbitArrivalPoint.position;
                orbitRotation = _orbitArrivalPoint.rotation;
                Debug.Log($"[PlanetLandingZone] Using orbit arrival point: {orbitPosition}");
            }
            else
            {
                // Calculate orbit position based on planet center and default orbit distance
                Vector3 toPlanet = (transform.position - _nearbyShip.transform.position).normalized;
                orbitPosition = transform.position - (toPlanet * _defaultOrbitDistance);
                orbitRotation = Quaternion.LookRotation(-toPlanet); // Face away from planet
                Debug.Log($"[PlanetLandingZone] No orbit arrival point set - calculated position: {orbitPosition}");
            }

            // Save ship position for return trip
            ShipSpawnManager.SaveShipPosition(orbitPosition, orbitRotation);
            Debug.Log($"[PlanetLandingZone] Saved orbit position for return trip: {orbitPosition}");

            // Use Photon to load the scene for ALL players in the room
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log($"[PlanetLandingZone] Master Client - Loading planet surface scene for all players");
                PhotonNetwork.LoadLevel(_surfaceSceneName);
            }
            else
            {
                // Non-master clients request the master to load the scene
                Debug.Log($"[PlanetLandingZone] Requesting Master Client to load scene");
                photonView.RPC("RPC_RequestSceneLoad", RpcTarget.MasterClient, _surfaceSceneName);
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
                Debug.Log($"[PlanetLandingZone] Master received scene load request: {sceneName}");
                PhotonNetwork.LoadLevel(sceneName);
            }
        }

        /// <summary>
        /// Display landing prompt on screen
        /// </summary>
        private void OnGUI()
        {
            // Only show if ship is in range and player is piloting
            if (!_shipInRange || _shipController == null || !_shipController.IsActive || _isLanding)
            {
                return;
            }

            // IMPORTANT: Only show prompt if LOCAL player is the pilot
            if (!IsLocalPlayerPiloting())
            {
                return;
            }

            // Show landing prompt
            GUI.skin.label.fontSize = _fontSize;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.color = _promptColor;

            string promptText = $"Press {_landKey.ToString().ToUpper()} to Land on {_planetName}";

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
        /// Visualize landing zone in editor
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.15f);

            Collider col = GetComponent<Collider>();
            if (col is SphereCollider sphereCollider)
            {
                Gizmos.DrawSphere(transform.position + sphereCollider.center, sphereCollider.radius);
                Gizmos.color = new Color(0, 1, 0, 0.5f);
                Gizmos.DrawWireSphere(transform.position + sphereCollider.center, sphereCollider.radius);
            }

            // Draw orbit arrival point
            if (_orbitArrivalPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(_orbitArrivalPoint.position, 50f);
                Gizmos.DrawLine(transform.position, _orbitArrivalPoint.position);
                Gizmos.DrawRay(_orbitArrivalPoint.position, _orbitArrivalPoint.forward * 100f);
            }

            // Draw label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 1000f,
                $"Planet Landing Zone\n{_planetName}\n→ {_surfaceSceneName}",
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
