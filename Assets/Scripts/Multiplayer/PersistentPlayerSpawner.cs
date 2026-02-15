/*
 * Persistent Player Spawner for Klyra's Reach
 *
 * PURPOSE:
 * Spawns player characters that persist across scene changes using DontDestroyOnLoad.
 * This ensures all players can see each other even when they're in different scenes.
 *
 * HOW TO USE:
 * 1. Add this to a GameObject in your game scene (replaces SingleCharacterSpawnManager)
 * 2. Assign your player prefab (must be in Resources folder)
 * 3. Set spawn mode and spawn location
 * 4. Players will spawn once and persist across all scene changes
 */

using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Opsive.UltimateCharacterController.Game;

namespace KlyrasReach.Multiplayer
{
    /// <summary>
    /// Spawns player characters with DontDestroyOnLoad for persistence across scenes
    /// </summary>
    public class PersistentPlayerSpawner : MonoBehaviourPunCallbacks
    {
        public enum SpawnMode
        {
            FixedLocation,  // Always spawns at a fixed location
            SpawnPoint      // Uses Opsive's Spawn Point system
        }

        [Header("Player Settings")]
        [Tooltip("Path to player prefab in Resources folder (e.g., 'Characters/SM_Chr_Psionic_01')")]
        [SerializeField] private string _playerPrefabPath = "Characters/SM_Chr_Psionic_01";

        [Header("Spawn Settings")]
        [Tooltip("How players should spawn")]
        [SerializeField] private SpawnMode _spawnMode = SpawnMode.FixedLocation;

        [Tooltip("Fixed spawn location (used if SpawnMode is FixedLocation)")]
        [SerializeField] private Transform _spawnLocation;

        [Tooltip("Offset multiplied by player count (prevents players spawning inside each other)")]
        [SerializeField] private Vector3 _spawnLocationOffset = new Vector3(2f, 0f, 0f);

        [Tooltip("Spawn point grouping (used if SpawnMode is SpawnPoint, -1 = any)")]
        [SerializeField] private int _spawnPointGrouping = -1;

        [Header("Spawn Timing")]
        [Tooltip("Delay before spawning player")]
        [SerializeField] private float _spawnDelay = 0.5f;

        private static GameObject _myPlayer; // Reference to this player's character
        private static bool _hasSpawned = false; // Track if we've already spawned

        /// <summary>
        /// Check if already in a room when scene loads
        /// </summary>
        private void Start()
        {
            Debug.Log($"[PersistentPlayerSpawner] Start - PhotonNetwork.InRoom: {PhotonNetwork.InRoom}, HasSpawned: {_hasSpawned}");

            // If we're already in a room and haven't spawned yet, spawn now
            if (PhotonNetwork.InRoom && !_hasSpawned)
            {
                Debug.Log("[PersistentPlayerSpawner] Already in room - will spawn player after delay");
                Invoke(nameof(SpawnMyPlayer), _spawnDelay);
            }
        }

        /// <summary>
        /// Called when player joins a room
        /// </summary>
        public override void OnJoinedRoom()
        {
            Debug.Log("[PersistentPlayerSpawner] Joined room - will spawn player after delay");

            // Only spawn if we haven't already
            if (!_hasSpawned)
            {
                Invoke(nameof(SpawnMyPlayer), _spawnDelay);
            }
        }

        /// <summary>
        /// Spawns this player's character
        /// </summary>
        private void SpawnMyPlayer()
        {
            // Don't spawn if we already have a player
            if (_myPlayer != null)
            {
                Debug.Log("[PersistentPlayerSpawner] Already have a player, skipping spawn");
                return;
            }

            // Determine spawn position and rotation
            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;

            if (_spawnMode == SpawnMode.SpawnPoint)
            {
                // Use Opsive's Spawn Point system
                if (!SpawnPointManager.GetPlacement(null, _spawnPointGrouping, ref spawnPosition, ref spawnRotation))
                {
                    Debug.LogWarning($"[PersistentPlayerSpawner] No spawn points found for grouping {_spawnPointGrouping}. Using fallback position.");
                    spawnPosition = Vector3.zero;
                    spawnRotation = Quaternion.identity;
                }
            }
            else
            {
                // Use fixed location
                if (_spawnLocation != null)
                {
                    spawnPosition = _spawnLocation.position;
                    spawnRotation = _spawnLocation.rotation;
                }

                // Offset based on player count to prevent overlap
                int playerCount = PhotonNetwork.CurrentRoom.PlayerCount - 1; // -1 because we're not spawned yet
                spawnPosition += _spawnLocationOffset * playerCount;
            }

            Debug.Log($"[PersistentPlayerSpawner] Spawning player at {spawnPosition}");

            // Spawn the player over the network
            _myPlayer = PhotonNetwork.Instantiate(
                _playerPrefabPath,
                spawnPosition,
                spawnRotation
            );

            if (_myPlayer != null)
            {
                // Make player persist across scene loads
                DontDestroyOnLoad(_myPlayer);
                _hasSpawned = true;

                Debug.Log($"[PersistentPlayerSpawner] ✓ Player spawned successfully: {_myPlayer.name}");
                Debug.Log($"[PersistentPlayerSpawner] Player will persist across scenes");

                PhotonView pv = _myPlayer.GetComponent<PhotonView>();
                if (pv != null)
                {
                    Debug.Log($"[PersistentPlayerSpawner] Player PhotonView ID: {pv.ViewID}, IsMine: {pv.IsMine}");
                }
            }
            else
            {
                Debug.LogError($"[PersistentPlayerSpawner] Failed to spawn player! Check that '{_playerPrefabPath}' exists in Resources folder.");
            }
        }

        /// <summary>
        /// Called when player leaves the room
        /// </summary>
        public override void OnLeftRoom()
        {
            Debug.Log("[PersistentPlayerSpawner] Left room");

            // Player will be automatically destroyed by Photon
            _myPlayer = null;
            _hasSpawned = false;
        }

        /// <summary>
        /// Public method to get reference to this player's character
        /// </summary>
        public static GameObject GetMyPlayer()
        {
            return _myPlayer;
        }

        /// <summary>
        /// Reset spawn state (useful for testing)
        /// </summary>
        public static void ResetSpawnState()
        {
            _hasSpawned = false;
            _myPlayer = null;
        }
    }
}
