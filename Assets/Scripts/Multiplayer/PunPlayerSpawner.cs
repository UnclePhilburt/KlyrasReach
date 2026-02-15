/*
 * PUN Player Spawner for Klyra's Reach
 *
 * PURPOSE:
 * Spawns the player character when scene loads, avoiding the WebGL encoding error
 * that Opsive's spawn manager causes with TransferOwnership()
 * Player persists across scene changes using DontDestroyOnLoad
 *
 * HOW TO USE:
 * 1. Add this to a GameObject in your game scene (NOT lobby)
 * 2. Remove/disable Opsive's SingleCharacterSpawnManager
 * 3. Set spawn points in Inspector
 * 4. Make sure your player prefab is in Resources/Characters/
 */

using UnityEngine;
using Photon.Pun;
using KlyrasReach.Player;

namespace KlyrasReach.Multiplayer
{
    /// <summary>
    /// Spawns player using PhotonNetwork.Instantiate to avoid WebGL ownership transfer errors
    /// Player persists across scenes for independent travel
    /// </summary>
    public class PunPlayerSpawner : MonoBehaviourPunCallbacks
    {
        [Header("Player Settings")]
        [Tooltip("Path to player prefab in Resources folder (e.g., 'Characters/SM_Chr_Psionic_01')")]
        [SerializeField] private string _playerPrefabPath = "Characters/SM_Chr_Psionic_01";

        [Header("Spawn Settings")]
        [Tooltip("Spawn players inside ship interior (finds PlayerSpawnPoints in ship)")]
        [SerializeField] private bool _spawnInShipInterior = true;

        [Tooltip("List of spawn points - will pick one randomly (if not spawning in ship)")]
        [SerializeField] private Transform[] _spawnPoints;

        [Tooltip("Default spawn position if no spawn points are set")]
        [SerializeField] private Vector3 _defaultSpawnPosition = Vector3.zero;

        [Tooltip("Default spawn rotation if no spawn points are set")]
        [SerializeField] private Vector3 _defaultSpawnRotation = Vector3.zero;

        // Static so other scripts can access the local player
        private static GameObject _localPlayer;
        private static bool _hasSpawned = false;

        private void Start()
        {
            // IMPORTANT: Reset spawn state when entering a new scene
            // This allows players to respawn in the new scene
            Debug.Log($"[PunPlayerSpawner] Start - Checking if player exists from previous scene");

            // Check if player still exists from previous scene
            if (_localPlayer != null)
            {
                Debug.Log($"[PunPlayerSpawner] Player exists from previous scene: {_localPlayer.name}");
                // Player persists across scenes, no need to spawn again
                return;
            }
            else
            {
                Debug.Log($"[PunPlayerSpawner] No existing player found - will spawn new player");
                // Reset spawn flag so we can spawn in this scene
                _hasSpawned = false;
            }

            // Wait a bit for Photon to be fully ready, then try spawning
            StartCoroutine(WaitAndSpawn());

            // Debug: Log all PhotonViews in scene when we start
            PhotonView[] allViews = FindObjectsByType<PhotonView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"[PunPlayerSpawner] Start - Found {allViews.Length} PhotonViews in scene");
            foreach (PhotonView pv in allViews)
            {
                Debug.Log($"[PunPlayerSpawner] - PhotonView: {pv.gameObject.name}, ViewID: {pv.ViewID}, IsMine: {pv.IsMine}, Owner: {pv.Owner?.NickName}");
            }
        }

        /// <summary>
        /// Wait for Photon to be ready, then spawn player
        /// </summary>
        private System.Collections.IEnumerator WaitAndSpawn()
        {
            // Wait a few frames for scene to fully load
            yield return new WaitForSeconds(0.5f);

            // Wait until connected and in room
            float timeout = 10f;
            float elapsed = 0f;

            while ((!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom) && elapsed < timeout)
            {
                Debug.Log("[PunPlayerSpawner] Waiting for Photon... Connected: " + PhotonNetwork.IsConnected + " InRoom: " + PhotonNetwork.InRoom);
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }

            // Check if we timed out
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            {
                Debug.LogError("[PunPlayerSpawner] Timeout! Not connected to Photon or not in a room!");
                Debug.LogError("[PunPlayerSpawner] Make sure you connect via the Lobby scene first!");
                yield break;
            }

            // Only spawn if we haven't already spawned
            if (_hasSpawned || _localPlayer != null)
            {
                Debug.LogWarning("[PunPlayerSpawner] Player already spawned!");
                yield break;
            }

            // IMPORTANT: If spawning in ship interior, wait for ship to be spawned by ShipSpawnManager
            if (_spawnInShipInterior)
            {
                Debug.Log("[PunPlayerSpawner] Waiting for ship to be spawned...");
                float shipWaitTime = 0f;
                float shipTimeout = 5f;

                while (FindShipInScene() == null && shipWaitTime < shipTimeout)
                {
                    yield return new WaitForSeconds(0.2f);
                    shipWaitTime += 0.2f;
                }

                GameObject ship = FindShipInScene();
                if (ship != null)
                {
                    Debug.Log($"[PunPlayerSpawner] Ship found: {ship.name}");
                }
                else
                {
                    Debug.LogWarning("[PunPlayerSpawner] Ship not found after waiting - will spawn at default position");
                }
            }

            _hasSpawned = true;
            SpawnPlayer();
        }

        /// <summary>
        /// Spawns the local player at a spawn point
        /// </summary>
        private void SpawnPlayer()
        {
            Vector3 spawnPosition;
            Quaternion spawnRotation;

            // Option 1: Spawn inside ship interior
            if (_spawnInShipInterior)
            {
                Transform[] shipSpawnPoints = FindShipInteriorSpawnPoints();

                if (shipSpawnPoints != null && shipSpawnPoints.Length > 0)
                {
                    // Use player's ActorNumber to pick spawn point (ensures unique spots)
                    int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % shipSpawnPoints.Length;
                    Transform spawnPoint = shipSpawnPoints[spawnIndex];
                    spawnPosition = spawnPoint.position;
                    spawnRotation = spawnPoint.rotation;

                    Debug.Log($"[PunPlayerSpawner] Spawning in ship interior at: {spawnPoint.name}");
                }
                else
                {
                    Debug.LogError("[PunPlayerSpawner] Could not find ship interior spawn points! Using default.");
                    spawnPosition = _defaultSpawnPosition;
                    spawnRotation = Quaternion.Euler(_defaultSpawnRotation);
                }
            }
            // Option 2: Use manual spawn points
            else if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                // Pick a random spawn point
                Transform spawnPoint = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
                spawnPosition = spawnPoint.position;
                spawnRotation = spawnPoint.rotation;

                Debug.Log($"[PunPlayerSpawner] Using spawn point: {spawnPoint.name}");
            }
            // Option 3: Default position
            else
            {
                spawnPosition = _defaultSpawnPosition;
                spawnRotation = Quaternion.Euler(_defaultSpawnRotation);

                Debug.LogWarning("[PunPlayerSpawner] No spawn points set, using default position");
            }

            Debug.Log($"[PunPlayerSpawner] Spawning player at {spawnPosition}");
            Debug.Log($"[PunPlayerSpawner] Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}");
            Debug.Log($"[PunPlayerSpawner] My ActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber}");

            // Spawn the player using PhotonNetwork.Instantiate
            // This gives proper player ownership
            _localPlayer = PhotonNetwork.Instantiate(
                _playerPrefabPath,
                spawnPosition,
                spawnRotation,
                0  // group 0 (default)
            );

            if (_localPlayer != null)
            {
                Debug.Log($"[PunPlayerSpawner] ✓ Player spawned successfully: {_localPlayer.name}");
                Debug.Log($"[PunPlayerSpawner] PhotonView ID: {_localPlayer.GetComponent<PhotonView>().ViewID}");
                Debug.Log($"[PunPlayerSpawner] IsMine: {_localPlayer.GetComponent<PhotonView>().IsMine}");
                Debug.Log($"[PunPlayerSpawner] Owner: {_localPlayer.GetComponent<PhotonView>().Owner?.NickName}");

                // IMPORTANT: Make sure player has "Player" tag for Opsive camera
                if (!_localPlayer.CompareTag("Player"))
                {
                    Debug.LogWarning($"[PunPlayerSpawner] Player doesn't have 'Player' tag! Current tag: {_localPlayer.tag}");
                    _localPlayer.tag = "Player";
                    Debug.Log("[PunPlayerSpawner] Set player tag to 'Player'");
                }

                // Parent player to ship if spawned inside ship interior
                if (_spawnInShipInterior)
                {
                    GameObject ship = FindShipInScene();
                    if (ship != null)
                    {
                        _localPlayer.transform.SetParent(ship.transform);
                        Debug.Log($"[PunPlayerSpawner] Parented player to ship: {ship.name}");
                    }
                    else
                    {
                        Debug.LogWarning("[PunPlayerSpawner] Could not find ship to parent player to!");
                    }
                }

                // Debug: Count all PhotonViews after spawning
                PhotonView[] allViews = FindObjectsByType<PhotonView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Debug.Log($"[PunPlayerSpawner] Total PhotonViews in scene after spawn: {allViews.Length}");
            }
            else
            {
                Debug.LogError("[PunPlayerSpawner] Failed to spawn player!");
            }
        }

        /// <summary>
        /// Finds spawn points inside the ship interior
        /// Looks for objects named "SpawnPoint_1", "SpawnPoint_2", etc.
        /// </summary>
        private Transform[] FindShipInteriorSpawnPoints()
        {
            // Find all GameObjects in scene (including inactive)
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            // Look for objects with "SpawnPoint" in the name that are inside a ship
            System.Collections.Generic.List<Transform> spawnPoints = new System.Collections.Generic.List<Transform>();

            foreach (GameObject obj in allObjects)
            {
                // Check if this is a spawn point (name contains "SpawnPoint")
                if (obj.name.Contains("SpawnPoint_"))
                {
                    // Make sure it's inside a ship (parent hierarchy contains "Ship" or "Transport")
                    Transform current = obj.transform;
                    bool isInShip = false;

                    while (current != null)
                    {
                        if (current.name.Contains("Ship") || current.name.Contains("Transport"))
                        {
                            isInShip = true;
                            break;
                        }
                        current = current.parent;
                    }

                    if (isInShip)
                    {
                        spawnPoints.Add(obj.transform);
                        Debug.Log($"[PunPlayerSpawner] Found ship spawn point: {obj.name}");
                    }
                }
            }

            Debug.Log($"[PunPlayerSpawner] Found {spawnPoints.Count} spawn points in ship interior");
            return spawnPoints.ToArray();
        }

        /// <summary>
        /// Find the ship GameObject in the scene
        /// </summary>
        private GameObject FindShipInScene()
        {
            // Find all GameObjects in scene
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (GameObject obj in allObjects)
            {
                // Look for ship by name or by ShipController component
                if (obj.name.Contains("Ship") || obj.name.Contains("Transport"))
                {
                    // Verify it has a ShipController component
                    if (obj.GetComponent<ShipController>() != null)
                    {
                        Debug.Log($"[PunPlayerSpawner] Found ship: {obj.name}");
                        return obj;
                    }
                }
            }

            Debug.LogWarning("[PunPlayerSpawner] Could not find ship in scene!");
            return null;
        }

        /// <summary>
        /// Called when a new player enters the room
        /// </summary>
        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            Debug.Log($"[PunPlayerSpawner] Player entered room: {newPlayer.NickName} (ActorNumber: {newPlayer.ActorNumber})");

            // Debug: Log all PhotonViews after new player joins
            PhotonView[] allViews = FindObjectsByType<PhotonView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"[PunPlayerSpawner] After player joined - Found {allViews.Length} PhotonViews");
            foreach (PhotonView pv in allViews)
            {
                Debug.Log($"[PunPlayerSpawner] - PhotonView: {pv.gameObject.name}, ViewID: {pv.ViewID}, Owner: {pv.Owner?.NickName}");
            }
        }

        /// <summary>
        /// Called when player leaves the room
        /// </summary>
        public override void OnLeftRoom()
        {
            Debug.Log("[PunPlayerSpawner] Left room - cleaning up player");

            // Player will be automatically destroyed by Photon
            _localPlayer = null;
            _hasSpawned = false;
        }

        /// <summary>
        /// Manually spawn player (for testing)
        /// </summary>
        [ContextMenu("Spawn Player Now")]
        public void ManualSpawn()
        {
            if (_localPlayer != null)
            {
                Debug.LogWarning("[PunPlayerSpawner] Destroying existing player first");
                PhotonNetwork.Destroy(_localPlayer);
                _localPlayer = null;
            }

            SpawnPlayer();
        }

        /// <summary>
        /// Get reference to local player (useful for other scripts)
        /// </summary>
        public static GameObject GetLocalPlayer()
        {
            return _localPlayer;
        }
    }
}
