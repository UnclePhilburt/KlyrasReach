/*
 * Network Ship Spawner for Klyra's Reach
 *
 * PURPOSE:
 * Spawns a personal ship for each player when they join the room.
 * Ships are assigned to spawn points sequentially (round-robin).
 * Ships persist across scenes so players can travel between locations.
 *
 * HOW TO USE:
 * 1. Add this script to a GameObject in your main scene (space station, etc.)
 * 2. Assign your 3 ship spawn points to the array
 * 3. Set the ship prefab path (should be in Resources folder)
 * 4. When player joins room, their ship spawns automatically
 */

using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace KlyrasReach.Multiplayer
{
    /// <summary>
    /// Manages spawning personal ships for each player over the network
    /// </summary>
    public class NetworkShipSpawner : MonoBehaviourPunCallbacks
    {
        [Header("Ship Settings")]
        [Tooltip("Path to ship prefab in Resources folder (e.g., 'Ships/SM_Ship_Massive_Transport_01')")]
        [SerializeField] private string _shipPrefabPath = "Ships/SM_Ship_Massive_Transport_01";

        [Header("Spawn Points")]
        [Tooltip("Array of spawn point transforms - ships spawn sequentially at these")]
        [SerializeField] private Transform[] _spawnPoints;

        [Header("Spawn Timing")]
        [Tooltip("Delay before spawning ship after joining room (gives time for player to spawn)")]
        [SerializeField] private float _spawnDelay = 1f;

        // Track next spawn point to use (static so it persists across scene loads)
        private static int _nextSpawnIndex = 0;

        private GameObject _myShip; // Reference to this player's ship

        /// <summary>
        /// Check if already in a room when scene loads
        /// </summary>
        private void Start()
        {
            Debug.Log($"[NetworkShipSpawner] Start - PhotonNetwork.InRoom: {PhotonNetwork.InRoom}");

            // If we're already in a room (joined before this scene loaded), spawn ship now
            if (PhotonNetwork.InRoom)
            {
                Debug.Log("[NetworkShipSpawner] Already in room - will spawn ship after delay");
                Invoke(nameof(SpawnMyShip), _spawnDelay);
            }
        }

        /// <summary>
        /// Called when player joins a room
        /// </summary>
        public override void OnJoinedRoom()
        {
            Debug.Log("[NetworkShipSpawner] Joined room - will spawn ship after delay");

            // Wait a bit for player character to spawn first
            Invoke(nameof(SpawnMyShip), _spawnDelay);
        }

        /// <summary>
        /// Spawns this player's personal ship
        /// </summary>
        private void SpawnMyShip()
        {
            // Don't spawn if we already have a ship
            if (_myShip != null)
            {
                Debug.Log("[NetworkShipSpawner] Already have a ship, skipping spawn");
                return;
            }

            // Validate spawn points
            if (_spawnPoints == null || _spawnPoints.Length == 0)
            {
                Debug.LogError("[NetworkShipSpawner] No spawn points assigned! Please assign spawn points in Inspector.");
                return;
            }

            // Get next spawn point (round-robin)
            Transform spawnPoint = _spawnPoints[_nextSpawnIndex];
            _nextSpawnIndex = (_nextSpawnIndex + 1) % _spawnPoints.Length; // Cycle through spawn points

            Debug.Log($"[NetworkShipSpawner] Spawning ship at spawn point {_nextSpawnIndex} of {_spawnPoints.Length}");

            // Spawn the ship over the network
            _myShip = PhotonNetwork.Instantiate(
                _shipPrefabPath,
                spawnPoint.position,
                spawnPoint.rotation
            );

            if (_myShip != null)
            {
                // Make ship persist across scene loads
                DontDestroyOnLoad(_myShip);

                Debug.Log($"[NetworkShipSpawner] ✓ Ship spawned successfully: {_myShip.name}");
                Debug.Log($"[NetworkShipSpawner] Ship will persist across scenes");

                // Store ship reference in PhotonView custom properties so we can find it later
                PhotonView pv = _myShip.GetComponent<PhotonView>();
                if (pv != null)
                {
                    Debug.Log($"[NetworkShipSpawner] Ship PhotonView ID: {pv.ViewID}, IsMine: {pv.IsMine}");
                }
            }
            else
            {
                Debug.LogError($"[NetworkShipSpawner] Failed to spawn ship! Check that '{_shipPrefabPath}' exists in Resources folder.");
            }
        }

        /// <summary>
        /// Called when player leaves the room
        /// </summary>
        public override void OnLeftRoom()
        {
            Debug.Log("[NetworkShipSpawner] Left room");

            // Ship will be automatically destroyed by Photon when we leave the room
            // No need to manually destroy it
            _myShip = null;
        }

        /// <summary>
        /// Public method to get reference to this player's ship
        /// </summary>
        public GameObject GetMyShip()
        {
            return _myShip;
        }

        /// <summary>
        /// Draw spawn points in Scene view for debugging
        /// </summary>
        private void OnDrawGizmos()
        {
            if (_spawnPoints == null) return;

            for (int i = 0; i < _spawnPoints.Length; i++)
            {
                if (_spawnPoints[i] == null) continue;

                // Draw spawn point indicator
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(_spawnPoints[i].position, 2f);

                // Draw spawn index number
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    _spawnPoints[i].position + Vector3.up * 3f,
                    $"Ship Spawn {i + 1}",
                    new GUIStyle() { normal = new GUIStyleState() { textColor = Color.cyan } }
                );
                #endif

                // Draw direction arrow
                Gizmos.color = Color.yellow;
                Vector3 forward = _spawnPoints[i].forward * 5f;
                Gizmos.DrawRay(_spawnPoints[i].position, forward);
            }
        }
    }
}
