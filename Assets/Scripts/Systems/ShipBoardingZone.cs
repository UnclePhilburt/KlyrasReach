/*
 * Ship Boarding Zone for Klyra's Reach
 *
 * PURPOSE:
 * Creates a waiting area where players gather before returning to the ship.
 * All players must enter the zone before departure - shows "Waiting for players" until everyone is ready.
 * If solo player, boards immediately.
 *
 * HOW TO USE:
 * 1. In your space station interior scene, create an empty GameObject (e.g., "Ship_Boarding_Area")
 * 2. Add this script to it
 * 3. Add a Box Collider and set "Is Trigger" = true
 * 4. Size it to cover your waiting area
 * 5. Set the scene name to load (your Space scene with the ship)
 * 6. Players enter the zone and wait for everyone before boarding
 */

using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Handles ship boarding with multiplayer synchronization - waits for all players
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ShipBoardingZone : MonoBehaviourPunCallbacks
    {
        [Header("Boarding Settings")]
        [Tooltip("Name of this boarding area (shown in UI)")]
        [SerializeField] private string _boardingAreaName = "Ship Departure Gate";

        [Tooltip("Scene to load when all players are ready (your Space scene)")]
        [SerializeField] private string _shipSceneName = "Space";

        [Tooltip("How long to wait after all players ready before departing (seconds)")]
        [SerializeField] private float _departureDelay = 3f;

        [Header("UI Settings")]
        [Tooltip("Color for UI text")]
        [SerializeField] private Color _uiColor = Color.cyan;

        [Tooltip("Font size for status text")]
        [SerializeField] private int _fontSize = 18;

        // Private state
        private HashSet<int> _playersInZone = new HashSet<int>(); // Track player ActorNumbers in zone
        private bool _isBoarding = false;
        private bool _hasBoarded = false; // Prevent multiple scene loads
        private float _departureTimer = 0f;

        /// <summary>
        /// Validate trigger setup
        /// </summary>
        private void Awake()
        {
            Collider collider = GetComponent<Collider>();
            if (collider != null && !collider.isTrigger)
            {
                Debug.LogWarning("[ShipBoardingZone] Collider should be set to 'Is Trigger'. Auto-fixing...");
                collider.isTrigger = true;
            }

            Debug.Log($"[ShipBoardingZone] '{_boardingAreaName}' initialized. Will load scene: {_shipSceneName}");
        }

        /// <summary>
        /// Update boarding timer
        /// </summary>
        private void Update()
        {
            // Don't do anything if we've already boarded
            if (_hasBoarded)
            {
                return;
            }

            // Check if all players are in zone
            if (AreAllPlayersReady() && !_isBoarding)
            {
                _isBoarding = true;
                _departureTimer = _departureDelay;
                Debug.Log("[ShipBoardingZone] All players ready - starting departure timer");
            }

            // Count down to departure
            if (_isBoarding)
            {
                _departureTimer -= Time.deltaTime;

                if (_departureTimer <= 0f && !_hasBoarded)
                {
                    _hasBoarded = true; // Prevent multiple calls
                    InitiateBoarding();
                }
            }
            // If someone leaves the zone, cancel boarding
            else if (_isBoarding && !AreAllPlayersReady())
            {
                _isBoarding = false;
                Debug.Log("[ShipBoardingZone] Player left zone - canceling departure");
            }
        }

        /// <summary>
        /// Called when a player enters the boarding zone
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[ShipBoardingZone] OnTriggerEnter - Object: {other.gameObject.name}, Tag: {other.tag}");

            // Check if this is a player character - search in parent hierarchy
            PhotonView pv = other.GetComponentInParent<PhotonView>();
            if (pv == null)
            {
                Debug.LogWarning($"[ShipBoardingZone] Object '{other.gameObject.name}' has no PhotonView in hierarchy!");
                return;
            }

            Debug.Log($"[ShipBoardingZone] PhotonView found on '{pv.gameObject.name}' - IsMine: {pv.IsMine}, Owner: {pv.Owner?.NickName}");

            if (pv.IsMine) // Only track local player
            {
                int actorNumber = pv.Owner.ActorNumber;

                Debug.Log($"[ShipBoardingZone] ✓ Player {pv.Owner.NickName} (ActorNumber {actorNumber}) entered boarding zone");

                // Check if we have a PhotonView for RPC
                if (photonView == null)
                {
                    Debug.LogError("[ShipBoardingZone] This GameObject has no PhotonView! Add a PhotonView component!");
                    return;
                }

                // Sync with other clients that this player is in the zone
                photonView.RPC("RPC_PlayerEnteredZone", RpcTarget.AllBuffered, actorNumber);
            }
        }

        /// <summary>
        /// Called when a player leaves the boarding zone
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            // Check if this is a player character - search in parent hierarchy
            PhotonView pv = other.GetComponentInParent<PhotonView>();
            if (pv != null && pv.IsMine) // Only track local player
            {
                int actorNumber = pv.Owner.ActorNumber;

                Debug.Log($"[ShipBoardingZone] Player {pv.Owner.NickName} (ActorNumber {actorNumber}) left boarding zone");

                // Sync with other clients that this player left the zone
                photonView.RPC("RPC_PlayerLeftZone", RpcTarget.AllBuffered, actorNumber);
            }
        }

        /// <summary>
        /// RPC to sync player entering zone
        /// </summary>
        [PunRPC]
        private void RPC_PlayerEnteredZone(int actorNumber)
        {
            _playersInZone.Add(actorNumber);
            Debug.Log($"[ShipBoardingZone] Player {actorNumber} added to zone. Total in zone: {_playersInZone.Count}/{PhotonNetwork.CurrentRoom.PlayerCount}");
        }

        /// <summary>
        /// RPC to sync player leaving zone
        /// </summary>
        [PunRPC]
        private void RPC_PlayerLeftZone(int actorNumber)
        {
            _playersInZone.Remove(actorNumber);
            Debug.Log($"[ShipBoardingZone] Player {actorNumber} removed from zone. Total in zone: {_playersInZone.Count}/{PhotonNetwork.CurrentRoom.PlayerCount}");
        }

        /// <summary>
        /// Checks if all players in the room are in the boarding zone
        /// </summary>
        private bool AreAllPlayersReady()
        {
            if (!PhotonNetwork.InRoom)
            {
                return false;
            }

            int totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            int playersReady = _playersInZone.Count;

            // Remove any players who left the room from our tracking
            _playersInZone.RemoveWhere(actorNumber =>
                !PhotonNetwork.CurrentRoom.Players.ContainsKey(actorNumber)
            );

            return playersReady >= totalPlayers;
        }

        /// <summary>
        /// Initiates ship boarding and scene load
        /// </summary>
        private void InitiateBoarding()
        {
            Debug.Log($"[ShipBoardingZone] Initiating ship boarding - loading scene: {_shipSceneName}");

            // Only master client loads the scene
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("[ShipBoardingZone] Master Client - Loading scene for all players");
                PhotonNetwork.LoadLevel(_shipSceneName);
            }
        }

        /// <summary>
        /// Display boarding status on screen
        /// </summary>
        private void OnGUI()
        {
            // Don't show UI if not connected
            if (!PhotonNetwork.InRoom)
            {
                return;
            }

            int totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            int playersReady = _playersInZone.Count;

            // Show status when local player is in zone OR when boarding
            bool localPlayerInZone = false;
            if (PhotonNetwork.LocalPlayer != null)
            {
                localPlayerInZone = _playersInZone.Contains(PhotonNetwork.LocalPlayer.ActorNumber);
            }

            if (!localPlayerInZone && !_isBoarding)
            {
                return;
            }

            GUI.skin.label.fontSize = _fontSize;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;

            string statusText = "";
            Color statusColor = _uiColor;

            if (_isBoarding)
            {
                // Countdown to departure (clamp to 0 to avoid negative numbers)
                float displayTimer = Mathf.Max(0f, _departureTimer);
                statusText = $"DEPARTING IN {Mathf.Ceil(displayTimer)} SECONDS...";
                statusColor = Color.green;
            }
            else if (playersReady < totalPlayers)
            {
                // Waiting for more players
                statusText = $"WAITING FOR PLAYERS TO BOARD SHIP\n{playersReady}/{totalPlayers} Players Ready";
                statusColor = Color.yellow;
            }

            // Draw background box
            Rect backgroundRect = new Rect(
                Screen.width / 2 - 250,
                Screen.height / 2 - 50,
                500,
                100
            );

            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.DrawTexture(backgroundRect, Texture2D.whiteTexture);

            // Draw status text
            GUI.color = statusColor;
            GUI.Label(backgroundRect, statusText);

            // Show player list
            GUI.skin.label.fontSize = 14;
            GUI.color = Color.white;

            string playerListText = "Players in Boarding Area:\n";
            foreach (int actorNumber in _playersInZone)
            {
                if (PhotonNetwork.CurrentRoom.Players.TryGetValue(actorNumber, out Photon.Realtime.Player player))
                {
                    playerListText += $"✓ {player.NickName}\n";
                }
            }

            Rect playerListRect = new Rect(
                Screen.width / 2 - 150,
                Screen.height / 2 + 60,
                300,
                150
            );

            GUI.Label(playerListRect, playerListText);

            // Reset GUI settings
            GUI.color = Color.white;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
        }

        /// <summary>
        /// Visualize boarding zone in editor
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f);

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
                transform.position + Vector3.up * 5f,
                $"Ship Boarding Zone\n{_boardingAreaName}\n→ {_shipSceneName}",
                new GUIStyle() {
                    normal = new GUIStyleState() { textColor = Color.cyan },
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter
                }
            );
            #endif
        }
    }
}
