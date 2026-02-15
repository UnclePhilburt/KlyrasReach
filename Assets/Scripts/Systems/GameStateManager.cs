/*
 * Game State Manager for Klyra's Reach
 *
 * PURPOSE:
 * Centralized state management that works in both single-player and multiplayer.
 * Replaces PlayerPrefs for network-synced state.
 *
 * NETWORK READY:
 * - Single-player: Uses local state only
 * - Multiplayer (future): Will sync state via Photon Custom Properties
 */

using UnityEngine;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Manages game state in a network-ready way
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        private static GameStateManager _instance;
        public static GameStateManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to find existing instance
                    _instance = FindObjectOfType<GameStateManager>();

                    // Create new if none exists
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameStateManager");
                        _instance = go.AddComponent<GameStateManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // Player state flags (network-ready)
        private bool _isBoardingShip = false;

        private void Awake()
        {
            // Singleton pattern
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Gets whether local player is boarding a ship
        /// NETWORK READY: In multiplayer, this will check PhotonView owner's state
        /// </summary>
        public bool IsBoardingShip
        {
            get
            {
                // TODO: When PUN is added, check PhotonNetwork.IsConnected
                // If multiplayer, get from Photon Custom Properties
                // For now, use local state
                return _isBoardingShip;
            }
            set
            {
                _isBoardingShip = value;

                // TODO: When PUN is added, sync via Photon Custom Properties:
                // if (PhotonNetwork.IsConnected)
                // {
                //     ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
                //     props["IsBoardingShip"] = value;
                //     PhotonNetwork.LocalPlayer.SetCustomProperties(props);
                // }

                Debug.Log($"[GameStateManager] IsBoardingShip set to: {value}");
            }
        }

        /// <summary>
        /// Clears all state (useful for new games or disconnects)
        /// </summary>
        public void ClearState()
        {
            _isBoardingShip = false;
            Debug.Log("[GameStateManager] State cleared");
        }
    }
}
