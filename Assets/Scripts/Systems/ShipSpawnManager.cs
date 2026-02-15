/*
 * Ship Spawn Manager for Klyra's Reach
 *
 * PURPOSE:
 * Tracks where the ship docked from and spawns it back at that location when returning.
 * Prevents ship from always spawning at the same spot regardless of where it came from.
 *
 * HOW IT WORKS:
 * 1. DockingStation saves ship position before loading interior scene
 * 2. ShipBoardingZone references which docking station to return to
 * 3. ShipSpawnManager spawns ship at the saved position when scene loads
 *
 * HOW TO USE:
 * 1. Add this script to an empty GameObject in your Space scene (e.g., "ShipSpawnManager")
 * 2. DockingStation will automatically save ship position before docking
 * 3. ShipBoardingZone will specify return location
 * 4. Ship will spawn at correct location when returning
 */

using UnityEngine;
using Photon.Pun;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Manages ship spawning based on where players came from
    /// </summary>
    public class ShipSpawnManager : MonoBehaviourPunCallbacks
    {
        [Header("Ship Settings")]
        [Tooltip("Path to ship prefab in Resources folder (e.g., 'Ships/SM_Ship_Massive_Transport_01')")]
        [SerializeField] private string _shipPrefabPath = "Ships/SM_Ship_Massive_Transport_01";

        [Header("Default Spawn")]
        [Tooltip("Default spawn position if no saved position exists (new game start)")]
        [SerializeField] private Transform _defaultSpawnPoint;

        [Header("Debug")]
        [Tooltip("Show debug logs")]
        [SerializeField] private bool _debugMode = true;

        // Track spawned ship
        private static GameObject _spawnedShip;

        // Static variables to persist across scene loads
        private static Vector3 _savedShipPosition;
        private static Quaternion _savedShipRotation;
        private static bool _hasSavedPosition = false;

        /// <summary>
        /// Saves ship position before leaving space scene
        /// Called by DockingStation before loading interior scene
        /// </summary>
        public static void SaveShipPosition(Vector3 position, Quaternion rotation)
        {
            _savedShipPosition = position;
            _savedShipRotation = rotation;
            _hasSavedPosition = true;

            Debug.Log($"[ShipSpawnManager] Saved ship position: {position}, rotation: {rotation.eulerAngles}");
        }

        /// <summary>
        /// Clears saved ship position (for new game start)
        /// </summary>
        public static void ClearSavedPosition()
        {
            _hasSavedPosition = false;
            Debug.Log("[ShipSpawnManager] Cleared saved ship position");
        }

        /// <summary>
        /// Called when leaving the space scene
        /// </summary>
        private void OnDestroy()
        {
            // Ship will be destroyed by Photon when leaving scene
            // Clear reference so it can be respawned when returning
            _spawnedShip = null;
            Debug.Log("[ShipSpawnManager] Cleared ship reference on scene unload");
        }

        /// <summary>
        /// Called when Space scene loads - positions ship at saved location
        /// </summary>
        private void Start()
        {
            // Wait a moment for ship to spawn, then position it
            StartCoroutine(PositionShipAfterSpawn());
        }

        /// <summary>
        /// Waits for ship to exist, then positions it
        /// </summary>
        private System.Collections.IEnumerator PositionShipAfterSpawn()
        {
            // Wait for Photon to be ready
            yield return new WaitForSeconds(0.5f);

            // Wait until connected and in room
            while (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            {
                Debug.Log("[ShipSpawnManager] Waiting for Photon connection...");
                yield return new WaitForSeconds(0.5f);
            }

            if (_debugMode)
            {
                Debug.Log($"[ShipSpawnManager] Checking for ship. HasSavedPosition: {_hasSavedPosition}");
            }

            // Only master client spawns the ship (prevents duplicates)
            if (PhotonNetwork.IsMasterClient)
            {
                // Check if ship already exists (from previous scene)
                GameObject existingShip = FindShipInScene();

                if (existingShip != null && _spawnedShip != null)
                {
                    Debug.Log($"[ShipSpawnManager] Ship already exists: {existingShip.name}");
                    PositionShip(existingShip);
                    yield break;
                }

                // Determine spawn position
                Vector3 spawnPosition;
                Quaternion spawnRotation;

                if (_hasSavedPosition)
                {
                    Debug.Log($"[ShipSpawnManager] Spawning ship at saved location: {_savedShipPosition}");
                    spawnPosition = _savedShipPosition;
                    spawnRotation = _savedShipRotation;
                }
                else if (_defaultSpawnPoint != null)
                {
                    Debug.Log($"[ShipSpawnManager] Spawning ship at default spawn point: {_defaultSpawnPoint.position}");
                    spawnPosition = _defaultSpawnPoint.position;
                    spawnRotation = _defaultSpawnPoint.rotation;
                }
                else
                {
                    Debug.LogWarning("[ShipSpawnManager] No saved position and no default spawn point - using origin");
                    spawnPosition = Vector3.zero;
                    spawnRotation = Quaternion.identity;
                }

                // Spawn the ship using PhotonNetwork
                Debug.Log($"[ShipSpawnManager] Spawning ship prefab: {_shipPrefabPath}");
                _spawnedShip = PhotonNetwork.Instantiate(
                    _shipPrefabPath,
                    spawnPosition,
                    spawnRotation,
                    0  // group 0 (default)
                );

                if (_spawnedShip != null)
                {
                    Debug.Log($"[ShipSpawnManager] ✓ Ship spawned successfully: {_spawnedShip.name}");

                    // Wait a moment for players to spawn, then parent them to ship
                    StartCoroutine(ParentPlayersToShip());
                }
                else
                {
                    Debug.LogError($"[ShipSpawnManager] Failed to spawn ship! Check that prefab exists at: Resources/{_shipPrefabPath}");
                }
            }
            else
            {
                Debug.Log("[ShipSpawnManager] Non-master client - waiting for master to spawn ship");

                // Non-master clients also need to wait for ship and parent their local player
                StartCoroutine(WaitForShipAndParent());
            }
        }

        /// <summary>
        /// Waits for ship to exist and parents all players to it
        /// </summary>
        private System.Collections.IEnumerator ParentPlayersToShip()
        {
            // Wait for players to spawn
            yield return new WaitForSeconds(2f);

            if (_spawnedShip == null)
            {
                Debug.LogWarning("[ShipSpawnManager] Ship was destroyed before parenting players!");
                yield break;
            }

            // Find all player characters
            PhotonView[] allPhotonViews = FindObjectsByType<PhotonView>(FindObjectsSortMode.None);
            int playersParented = 0;

            foreach (PhotonView pv in allPhotonViews)
            {
                // Check if this is a player character (not the ship itself)
                if (pv.gameObject.CompareTag("Player"))
                {
                    // Parent to ship
                    pv.transform.SetParent(_spawnedShip.transform);
                    playersParented++;
                    Debug.Log($"[ShipSpawnManager] Parented player '{pv.gameObject.name}' to ship");
                }
            }

            Debug.Log($"[ShipSpawnManager] Parented {playersParented} players to ship");
        }

        /// <summary>
        /// Non-master clients wait for ship to be spawned by master, then parent their player
        /// </summary>
        private System.Collections.IEnumerator WaitForShipAndParent()
        {
            // Wait for ship to be spawned by master client
            float waitTime = 0f;
            while (_spawnedShip == null && FindShipInScene() == null && waitTime < 5f)
            {
                yield return new WaitForSeconds(0.2f);
                waitTime += 0.2f;
            }

            GameObject ship = _spawnedShip ?? FindShipInScene();
            if (ship == null)
            {
                Debug.LogWarning("[ShipSpawnManager] Ship not found after waiting!");
                yield break;
            }

            // Wait a bit more for local player to spawn
            yield return new WaitForSeconds(1f);

            // Find local player and parent to ship
            PhotonView[] allPhotonViews = FindObjectsByType<PhotonView>(FindObjectsSortMode.None);

            foreach (PhotonView pv in allPhotonViews)
            {
                if (pv.IsMine && pv.gameObject.CompareTag("Player"))
                {
                    pv.transform.SetParent(ship.transform);
                    Debug.Log($"[ShipSpawnManager] Local player parented to ship");
                    break;
                }
            }
        }

        /// <summary>
        /// Positions an existing ship at the saved or default location
        /// </summary>
        private void PositionShip(GameObject ship)
        {
            if (_hasSavedPosition)
            {
                Debug.Log($"[ShipSpawnManager] Positioning ship at saved location: {_savedShipPosition}");
                ship.transform.position = _savedShipPosition;
                ship.transform.rotation = _savedShipRotation;
            }
            else if (_defaultSpawnPoint != null)
            {
                Debug.Log($"[ShipSpawnManager] Positioning ship at default spawn point: {_defaultSpawnPoint.position}");
                ship.transform.position = _defaultSpawnPoint.position;
                ship.transform.rotation = _defaultSpawnPoint.rotation;
            }
        }

        /// <summary>
        /// Finds the ship in the scene
        /// </summary>
        private GameObject FindShipInScene()
        {
            // Check static reference first
            if (_spawnedShip != null)
            {
                return _spawnedShip;
            }

            // Search for ships with Ship tag
            GameObject[] ships = GameObject.FindGameObjectsWithTag("Ship");

            if (ships.Length > 0)
            {
                Debug.Log($"[ShipSpawnManager] Found {ships.Length} ships, using first one: {ships[0].name}");
                return ships[0];
            }

            // Search by ShipController component
            Player.ShipController[] controllers = FindObjectsByType<Player.ShipController>(FindObjectsSortMode.None);
            if (controllers.Length > 0)
            {
                Debug.Log($"[ShipSpawnManager] Found ship by ShipController: {controllers[0].gameObject.name}");
                return controllers[0].gameObject;
            }

            return null;
        }

        /// <summary>
        /// Visualize default spawn point in editor
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (_defaultSpawnPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_defaultSpawnPoint.position, 10f);
                Gizmos.DrawLine(_defaultSpawnPoint.position, _defaultSpawnPoint.position + _defaultSpawnPoint.forward * 20f);

                #if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    _defaultSpawnPoint.position + Vector3.up * 15f,
                    "Default Ship Spawn",
                    new GUIStyle() {
                        normal = new GUIStyleState() { textColor = Color.yellow },
                        fontSize = 14,
                        alignment = TextAnchor.MiddleCenter
                    }
                );
                #endif
            }
        }
    }
}
