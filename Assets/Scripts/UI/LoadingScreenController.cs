/*
 * Loading Screen Controller for Klyra's Reach
 *
 * PURPOSE:
 * Handles the Photon connection process on the loading screen scene.
 * When this scene loads, it automatically starts connecting to Photon
 * and shows status updates. If connection fails, it goes back to the main menu.
 *
 * HOW TO USE:
 * 1. Open the Synty Loading demo scene (14_Demo_SciFiMenus_Screen_Loading_01)
 * 2. Create an empty GameObject named "LoadingScreenController"
 * 3. Add this script to it
 * 4. Optionally assign a TMP_Text to show connection status
 * 5. Make sure the scene is in Build Settings
 *
 * FLOW:
 * Scene loads -> auto-connects to Photon -> shows status -> loads game scene
 * If error -> goes back to main menu scene
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

namespace KlyrasReach.UI
{
    /// <summary>
    /// Automatically connects to Photon when the loading scene starts.
    /// Shows connection status and loads the game scene when ready.
    /// Goes back to the main menu if something goes wrong.
    /// </summary>
    public class LoadingScreenController : MonoBehaviourPunCallbacks
    {
        // =====================================================================
        // INSPECTOR FIELDS
        // =====================================================================

        [Header("Scene Settings")]
        [Tooltip("The game scene to load once connected and in a room")]
        [SerializeField] private string _gameSceneName = "SampleScene";

        [Tooltip("The main menu scene to go back to if connection fails")]
        [SerializeField] private string _mainMenuSceneName = "01_Demo_SciFiMenus_Screen_MainMenu_01";

        [Header("Connection Settings")]
        [Tooltip("Game version for matchmaking (must match other players)")]
        [SerializeField] private string _gameVersion = "1.0";

        [Tooltip("Maximum players allowed in a room")]
        [SerializeField] private byte _maxPlayersPerRoom = 20;

        [Header("Status Display (Optional)")]
        [Tooltip("A TMP_Text element to show connection status messages")]
        [SerializeField] private TMP_Text _statusText;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        /// <summary>
        /// Called when the scene starts. Immediately begins connecting to Photon.
        /// </summary>
        private void Start()
        {
            // Show cursor in case it was hidden
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Set up Photon and start connecting
            PhotonNetwork.GameVersion = _gameVersion;
            PhotonNetwork.AutomaticallySyncScene = true;

            UpdateStatus("Connecting to server...");
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("[LoadingScreenController] Connecting to Photon...");
        }

        // =====================================================================
        // PHOTON CALLBACKS
        // =====================================================================

        /// <summary>
        /// Called when successfully connected to the Photon master server.
        /// Tries to join an existing room or create a new one.
        /// </summary>
        public override void OnConnectedToMaster()
        {
            Debug.Log("[LoadingScreenController] Connected to Master Server");
            UpdateStatus("Connected! Finding room...");

            // Try to join any available room
            PhotonNetwork.JoinRandomRoom();
        }

        /// <summary>
        /// Called when no random room is available to join.
        /// Creates a new room instead.
        /// </summary>
        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log($"[LoadingScreenController] No rooms available: {message}");
            UpdateStatus("No rooms found, creating new room...");

            // Create a new room with a random name
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = _maxPlayersPerRoom;
            roomOptions.IsVisible = true;
            roomOptions.IsOpen = true;

            string roomName = "Room_" + UnityEngine.Random.Range(1000, 9999);
            PhotonNetwork.CreateRoom(roomName, roomOptions);
        }

        /// <summary>
        /// Called when successfully joined a room. Loads the game scene.
        /// </summary>
        public override void OnJoinedRoom()
        {
            Debug.Log($"[LoadingScreenController] Joined room: {PhotonNetwork.CurrentRoom.Name}");
            UpdateStatus($"Joined room! Loading game...");

            // Load the game scene
            SceneManager.LoadScene(_gameSceneName);
        }

        /// <summary>
        /// Called when joining a specific room fails. Tries to create a new one.
        /// </summary>
        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"[LoadingScreenController] Join room failed: {message}");
            UpdateStatus("Join failed, creating new room...");

            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = _maxPlayersPerRoom;
            PhotonNetwork.CreateRoom("Room_" + UnityEngine.Random.Range(1000, 9999), roomOptions);
        }

        /// <summary>
        /// Called when room creation fails. Goes back to the main menu.
        /// </summary>
        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[LoadingScreenController] Create room failed: {message}");
            UpdateStatus($"Failed: {message}");

            // Go back to main menu after a short delay so the player can read the error
            Invoke(nameof(ReturnToMainMenu), 2f);
        }

        /// <summary>
        /// Called when disconnected from Photon. Goes back to the main menu.
        /// </summary>
        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarning($"[LoadingScreenController] Disconnected: {cause}");
            UpdateStatus($"Disconnected: {cause}");

            // Go back to main menu after a short delay so the player can read the error
            Invoke(nameof(ReturnToMainMenu), 2f);
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        /// <summary>
        /// Updates the status text on screen (if assigned).
        /// </summary>
        /// <param name="status">The status message to display</param>
        private void UpdateStatus(string status)
        {
            Debug.Log($"[LoadingScreenController] Status: {status}");

            if (_statusText != null)
            {
                _statusText.text = status;
            }
        }

        /// <summary>
        /// Loads the main menu scene so the player can try again.
        /// </summary>
        private void ReturnToMainMenu()
        {
            Debug.Log("[LoadingScreenController] Returning to main menu...");
            SceneManager.LoadScene(_mainMenuSceneName);
        }
    }
}
