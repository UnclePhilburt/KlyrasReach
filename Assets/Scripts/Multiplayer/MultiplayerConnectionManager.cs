/*
 * Multiplayer Connection Manager for Klyra's Reach
 *
 * PURPOSE:
 * Handles connecting to Photon servers, creating/joining rooms, and loading game scenes.
 *
 * HOW TO USE:
 * 1. Add this to a GameObject in your Lobby scene
 * 2. Assign UI buttons to call Connect(), CreateRoom(), JoinRoom()
 * 3. Set the Game Scene Name in Inspector
 * 4. When connected and in a room, it auto-loads the game scene
 */

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using PhotonPlayer = Photon.Realtime.Player;

namespace KlyrasReach.Multiplayer
{
    /// <summary>
    /// Manages connection to Photon servers and room creation/joining
    /// </summary>
    public class MultiplayerConnectionManager : MonoBehaviourPunCallbacks
    {
        [Header("Connection Settings")]
        [Tooltip("Game version for matchmaking (only same versions can play together)")]
        [SerializeField] private string _gameVersion = "1.0";

        [Tooltip("Maximum players per room")]
        [SerializeField] private byte _maxPlayersPerRoom = 20;

        [Header("Scene Settings")]
        [Tooltip("Scene to load when room is joined (e.g., 'SpaceStation' or 'PlanetSurface')")]
        [SerializeField] private string _gameSceneName = "YourGameSceneHere";

        [Header("Status Display (Optional)")]
        [Tooltip("UI Text to show connection status (optional)")]
        [SerializeField] private TMPro.TextMeshProUGUI _statusText;

        // --- Events for UI controllers (e.g., MainMenuController) to subscribe to ---

        /// <summary>
        /// Fired whenever the connection status message changes.
        /// Listeners receive the new status string.
        /// </summary>
        public event Action<string> OnStatusChanged;

        /// <summary>
        /// Fired when a connection error occurs (disconnect, room creation failure, etc.).
        /// Listeners receive an error description string.
        /// </summary>
        public event Action<string> OnConnectionError;

        /// <summary>
        /// Whether the manager is currently in the process of connecting.
        /// </summary>
        public bool IsConnecting => _isConnecting;

        // Connection state
        private bool _isConnecting = false;

        private void Start()
        {
            // Set game version for matchmaking
            PhotonNetwork.GameVersion = _gameVersion;

            // Enable auto-sync - all players stay in the same scene
            // This ensures players can see each other (required for player ownership to work)
            PhotonNetwork.AutomaticallySyncScene = true;

            UpdateStatus("Ready to connect");
        }

        /// <summary>
        /// Connect to Photon servers
        /// Call this from a UI button
        /// </summary>
        public void Connect()
        {
            if (_isConnecting)
            {
                Debug.LogWarning("[MultiplayerConnectionManager] Already connecting...");
                return;
            }

            if (PhotonNetwork.IsConnected)
            {
                Debug.Log("[MultiplayerConnectionManager] Already connected");
                // If already connected, just try to join a room
                JoinOrCreateRoom();
                return;
            }

            _isConnecting = true;
            UpdateStatus("Connecting to Photon...");

            // Connect to Photon Cloud
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("[MultiplayerConnectionManager] Connecting to Photon...");
        }

        /// <summary>
        /// Disconnect from Photon
        /// </summary>
        public void Disconnect()
        {
            PhotonNetwork.Disconnect();
            UpdateStatus("Disconnected");
        }

        /// <summary>
        /// Join or create a random room
        /// </summary>
        private void JoinOrCreateRoom()
        {
            UpdateStatus("Finding room...");

            // Try to join a random existing room
            PhotonNetwork.JoinRandomRoom();
        }

        /// <summary>
        /// Create a new room with specific name
        /// </summary>
        public void CreateRoom(string roomName = null)
        {
            if (!PhotonNetwork.IsConnected)
            {
                Debug.LogError("[MultiplayerConnectionManager] Not connected to Photon!");
                Connect();
                return;
            }

            if (string.IsNullOrEmpty(roomName))
            {
                roomName = "Room_" + UnityEngine.Random.Range(1000, 9999);
            }

            UpdateStatus($"Creating room: {roomName}");

            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = _maxPlayersPerRoom;
            roomOptions.IsVisible = true;
            roomOptions.IsOpen = true;

            PhotonNetwork.CreateRoom(roomName, roomOptions);
        }

        /// <summary>
        /// Join a specific room by name
        /// </summary>
        public void JoinRoom(string roomName)
        {
            if (!PhotonNetwork.IsConnected)
            {
                Debug.LogError("[MultiplayerConnectionManager] Not connected to Photon!");
                Connect();
                return;
            }

            UpdateStatus($"Joining room: {roomName}");
            PhotonNetwork.JoinRoom(roomName);
        }

        /// <summary>
        /// Called when connected to Photon master server
        /// </summary>
        public override void OnConnectedToMaster()
        {
            Debug.Log("[MultiplayerConnectionManager] Connected to Master Server");
            _isConnecting = false;
            UpdateStatus("Connected! Finding room...");

            // Automatically try to join a room
            JoinOrCreateRoom();
        }

        /// <summary>
        /// Called when disconnected from Photon
        /// </summary>
        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarning($"[MultiplayerConnectionManager] Disconnected: {cause}");
            _isConnecting = false;
            UpdateStatus($"Disconnected: {cause}");

            // Notify listeners about the connection error
            OnConnectionError?.Invoke($"Disconnected: {cause}");
        }

        /// <summary>
        /// Called when successfully joined a room
        /// </summary>
        public override void OnJoinedRoom()
        {
            Debug.Log($"[MultiplayerConnectionManager] Joined room: {PhotonNetwork.CurrentRoom.Name}");
            Debug.Log($"[MultiplayerConnectionManager] Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");

            UpdateStatus($"Joined room: {PhotonNetwork.CurrentRoom.Name} ({PhotonNetwork.CurrentRoom.PlayerCount} players)");

            // Each player loads the game scene independently (no auto-sync)
            // This allows players to travel between different scenes while staying in the same room
            Debug.Log($"[MultiplayerConnectionManager] Loading scene: {_gameSceneName}");
            SceneManager.LoadScene(_gameSceneName);
        }

        /// <summary>
        /// Called when joining a room fails
        /// </summary>
        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"[MultiplayerConnectionManager] Join room failed: {message}");
            UpdateStatus("Join failed, creating new room...");

            // If join fails, create a new room
            CreateRoom();
        }

        /// <summary>
        /// Called when joining a random room fails (no rooms available)
        /// </summary>
        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log($"[MultiplayerConnectionManager] No rooms available: {message}");
            UpdateStatus("No rooms found, creating new room...");

            // No rooms available, create one
            CreateRoom();
        }

        /// <summary>
        /// Called when room creation fails
        /// </summary>
        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[MultiplayerConnectionManager] Create room failed: {message}");
            UpdateStatus($"Failed to create room: {message}");
            _isConnecting = false;

            // Notify listeners about the room creation error
            OnConnectionError?.Invoke($"Failed to create room: {message}");
        }

        /// <summary>
        /// Called when a player joins the room
        /// </summary>
        public override void OnPlayerEnteredRoom(PhotonPlayer newPlayer)
        {
            Debug.Log($"[MultiplayerConnectionManager] Player joined: {newPlayer.NickName}");
            UpdateStatus($"{newPlayer.NickName} joined ({PhotonNetwork.CurrentRoom.PlayerCount} players)");
        }

        /// <summary>
        /// Called when a player leaves the room
        /// </summary>
        public override void OnPlayerLeftRoom(PhotonPlayer otherPlayer)
        {
            Debug.Log($"[MultiplayerConnectionManager] Player left: {otherPlayer.NickName}");
            UpdateStatus($"{otherPlayer.NickName} left ({PhotonNetwork.CurrentRoom.PlayerCount} players)");
        }

        /// <summary>
        /// Updates status text if assigned
        /// </summary>
        private void UpdateStatus(string status)
        {
            Debug.Log($"[MultiplayerConnectionManager] Status: {status}");

            if (_statusText != null)
            {
                _statusText.text = status;
            }

            // Notify any listeners (MainMenuController, etc.) about the status change
            OnStatusChanged?.Invoke(status);
        }

        /// <summary>
        /// Display connection info for debugging (Editor only, so it doesn't overlay the Synty menu)
        /// </summary>
#if UNITY_EDITOR
        private void OnGUI()
        {
            if (PhotonNetwork.IsConnected)
            {
                GUILayout.Label($"Connected: {PhotonNetwork.IsConnected}");
                GUILayout.Label($"Room: {(PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : "None")}");
                GUILayout.Label($"Players: {(PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.PlayerCount.ToString() : "0")}");
            }
            else
            {
                GUILayout.Label("Not connected");
            }
        }
#endif
    }
}
