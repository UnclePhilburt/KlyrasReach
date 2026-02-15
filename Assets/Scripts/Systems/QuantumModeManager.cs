/*
 * Quantum Mode Manager for Klyra's Reach
 *
 * PURPOSE:
 * Manages quantum travel mode - toggles destination markers on/off with B key.
 * Similar to Star Citizen's quantum travel interface.
 *
 * HOW TO USE:
 * 1. Add this script to any GameObject in your scene (or create an empty "QuantumModeManager")
 * 2. Press B to toggle quantum mode on/off
 * 3. Destination markers will show/hide accordingly
 */

using UnityEngine;
using UnityEngine.InputSystem;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Manages quantum travel mode and destination visibility
    /// </summary>
    public class QuantumModeManager : MonoBehaviour
    {
        // Singleton instance
        private static QuantumModeManager _instance;
        public static QuantumModeManager Instance => _instance;

        // Quantum mode state
        private bool _quantumModeActive = false;
        public static bool IsQuantumModeActive => _instance != null && _instance._quantumModeActive;

        private bool _isTraveling = false;
        private float _travelProgress = 0f;

        [Header("Settings")]
        [Tooltip("Key to toggle quantum mode")]
        [SerializeField] private Key _toggleKey = Key.B;

        [Tooltip("Only allow quantum mode when in a ship")]
        [SerializeField] private bool _requireShip = true;

        [Tooltip("Ship tag to check for active ship")]
        [SerializeField] private string _shipTag = "Ship";

        [Header("Quantum Travel Settings")]
        [Tooltip("How long quantum travel takes (seconds)")]
        [SerializeField] private float _travelDuration = 3f;

        [Tooltip("Speed of visual warp effect")]
        [SerializeField] private float _warpSpeed = 5000f;

        // Private variables
        private bool _playerInShip = false;
        private bool _wasInShip = false;

        /// <summary>
        /// Set up singleton
        /// </summary>
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        /// <summary>
        /// Check for input every frame
        /// </summary>
        private void Update()
        {
            // Check if player is in a ship
            if (_requireShip)
            {
                CheckIfInShip();

                // Auto-disable quantum mode when exiting ship
                if (_wasInShip && !_playerInShip && _quantumModeActive)
                {
                    Debug.Log("[QuantumMode] Player exited ship - auto-disabling quantum mode");
                    _quantumModeActive = false;
                }

                _wasInShip = _playerInShip;
            }
            else
            {
                _playerInShip = true; // Always allow if not requiring ship
            }

            // Check for toggle input (don't allow toggle while traveling)
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[_toggleKey].wasPressedThisFrame && !_isTraveling)
            {
                Debug.Log($"[QuantumMode] B key pressed! PlayerInShip: {_playerInShip}, RequireShip: {_requireShip}");

                if (_playerInShip || !_requireShip)
                {
                    ToggleQuantumMode();
                }
                else
                {
                    Debug.LogWarning("[QuantumMode] Must be in a ship to use quantum mode");
                }
            }
        }

        /// <summary>
        /// Checks if player is currently piloting a ship
        /// OPTIMIZED: Only checks every 30 frames instead of every frame
        /// </summary>
        private void CheckIfInShip()
        {
            // PERFORMANCE: Only check every 30 frames (0.5 seconds at 60FPS)
            // FindGameObjectsWithTag is EXTREMELY expensive!
            if (Time.frameCount % 30 != 0)
            {
                return;
            }

            Debug.Log($"[QuantumMode] ========== CheckIfInShip() at frame {Time.frameCount} ==========");
            GameObject[] ships = GameObject.FindGameObjectsWithTag(_shipTag);
            Debug.Log($"[QuantumMode] Found {ships.Length} ships with tag '{_shipTag}'");

            _playerInShip = false;

            foreach (GameObject ship in ships)
            {
                var shipController = ship.GetComponent<Player.ShipController>();
                if (shipController != null)
                {
                    bool isActive = shipController.IsActive;
                    Debug.Log($"[QuantumMode] Ship '{ship.name}' has controller");
                    Debug.Log($"[QuantumMode] Checking IsActive property... IsActive = {isActive}");

                    if (isActive)
                    {
                        _playerInShip = true;
                        Debug.Log($"[QuantumMode] ✓ Player is piloting ship: {ship.name}");
                        return;
                    }
                    else
                    {
                        Debug.Log($"[QuantumMode] ✗ Ship is not active (player not piloting)");
                    }
                }
                else
                {
                    Debug.LogWarning($"[QuantumMode] Ship '{ship.name}' has no ShipController!");
                }
            }

            Debug.Log($"[QuantumMode] ========== CheckIfInShip() complete - _playerInShip = {_playerInShip} ==========");
        }

        /// <summary>
        /// Toggles quantum mode on/off
        /// </summary>
        private void ToggleQuantumMode()
        {
            _quantumModeActive = !_quantumModeActive;

            if (_quantumModeActive)
            {
                Debug.Log("[QuantumMode] Quantum mode ACTIVATED - destinations visible");
            }
            else
            {
                Debug.Log("[QuantumMode] Quantum mode DEACTIVATED - destinations hidden");
            }
        }

        /// <summary>
        /// Public method to manually set quantum mode state
        /// </summary>
        public void SetQuantumMode(bool active)
        {
            _quantumModeActive = active;
        }

        /// <summary>
        /// Starts quantum travel to a destination
        /// </summary>
        public void StartQuantumTravel(DestinationMarker destination)
        {
            Debug.Log($"[QuantumMode] StartQuantumTravel called, _isTraveling = {_isTraveling}");

            if (_isTraveling)
            {
                Debug.Log("[QuantumMode] Already traveling!");
                return;
            }

            // Find active ship
            GameObject activeShip = null;
            GameObject[] ships = GameObject.FindGameObjectsWithTag(_shipTag);
            Debug.Log($"[QuantumMode] Found {ships.Length} ships with tag '{_shipTag}'");

            foreach (GameObject ship in ships)
            {
                var controller = ship.GetComponent<Player.ShipController>();
                if (controller != null)
                {
                    Debug.Log($"[QuantumMode] Ship '{ship.name}' has controller, IsActive = {controller.IsActive}");
                    if (controller.IsActive)
                    {
                        activeShip = ship;
                        break;
                    }
                }
            }

            if (activeShip == null)
            {
                Debug.LogWarning("[QuantumMode] No active ship found!");
                return;
            }

            Debug.Log($"[QuantumMode] Starting quantum travel with ship '{activeShip.name}'");

            // Start quantum travel coroutine
            StartCoroutine(QuantumTravelSequence(activeShip, destination));
        }

        /// <summary>
        /// Quantum travel animation and teleport
        /// </summary>
        private System.Collections.IEnumerator QuantumTravelSequence(GameObject ship, DestinationMarker destination)
        {
            _isTraveling = true;
            _travelProgress = 0f;

            Vector3 startPos = ship.transform.position;
            Vector3 endPos = destination.GetQuantumArrivalPosition();

            Debug.Log($"[QuantumMode] ========== QUANTUM TRAVEL STARTING ==========");
            Debug.Log($"[QuantumMode] Quantum traveling to {destination.name}");
            Debug.Log($"[QuantumMode] Start pos: {startPos}, End pos: {endPos}, Distance: {Vector3.Distance(startPos, endPos)}");

            // Get rigidbody to properly move the ship
            Rigidbody shipRb = ship.GetComponent<Rigidbody>();
            Debug.Log($"[QuantumMode] Ship rigidbody found: {shipRb != null}, IsKinematic: {shipRb?.isKinematic}");

            // Disable quantum mode UI during travel
            bool wasActive = _quantumModeActive;
            _quantumModeActive = false;

            // Travel animation
            float elapsed = 0f;
            int frameCount = 0;
            while (elapsed < _travelDuration)
            {
                elapsed += Time.deltaTime;
                _travelProgress = elapsed / _travelDuration;

                // Smooth curve for acceleration/deceleration
                float t = Mathf.SmoothStep(0f, 1f, _travelProgress);

                // Move ship using rigidbody if available
                Vector3 newPos = Vector3.Lerp(startPos, endPos, t);

                if (shipRb != null)
                {
                    // For kinematic bodies, we need to use transform.position directly
                    ship.transform.position = newPos;
                }
                else
                {
                    ship.transform.position = newPos;
                }

                // Log every 10 frames to avoid spam
                if (frameCount % 10 == 0)
                {
                    Debug.Log($"[QuantumMode] Frame {frameCount}: Progress {_travelProgress:F2}, Position: {ship.transform.position}, Target: {newPos}");
                }
                frameCount++;

                yield return null;
            }

            // Ensure final position
            ship.transform.position = endPos;
            Debug.Log($"[QuantumMode] Set final position to: {endPos}");
            Debug.Log($"[QuantumMode] Ship actual position after set: {ship.transform.position}");

            // Stop all velocity (only for non-kinematic bodies)
            if (shipRb != null && !shipRb.isKinematic)
            {
                shipRb.linearVelocity = Vector3.zero;
                shipRb.angularVelocity = Vector3.zero;
            }

            Debug.Log($"[QuantumMode] ========== QUANTUM TRAVEL COMPLETE ==========");
            Debug.Log($"[QuantumMode] Final ship position: {ship.transform.position}");
            Debug.Log($"[QuantumMode] Distance traveled: {Vector3.Distance(startPos, ship.transform.position)}");

            // Always re-enable quantum mode after travel (assume player wants to keep using it)
            _quantumModeActive = true;
            Debug.Log($"[QuantumMode] Quantum mode re-enabled after travel");

            _isTraveling = false;
            _travelProgress = 0f;
        }

        /// <summary>
        /// Display quantum mode status on screen
        /// </summary>
        private void OnGUI()
        {
            if (_quantumModeActive)
            {
                GUI.skin.label.fontSize = 14;
                GUI.skin.label.alignment = TextAnchor.UpperRight;
                GUI.color = Color.cyan;
                GUI.Label(new Rect(Screen.width - 210, 10, 200, 30), "QUANTUM MODE ACTIVE");

                // Show instruction
                GUI.skin.label.fontSize = 12;
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                GUI.Label(new Rect(Screen.width - 280, 35, 270, 30), "Aim at destination and press V to travel");

                GUI.color = Color.white;
                GUI.skin.label.alignment = TextAnchor.UpperLeft; // Reset alignment
            }

            if (_isTraveling)
            {
                // Show quantum travel progress
                GUI.skin.label.fontSize = 16;
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.color = Color.cyan;
                GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 30, 200, 30), "QUANTUM TRAVELING...");

                // Progress bar
                float barWidth = 300f;
                float barHeight = 20f;
                Rect barBg = new Rect(Screen.width / 2 - barWidth / 2, Screen.height / 2, barWidth, barHeight);
                Rect barFill = new Rect(Screen.width / 2 - barWidth / 2, Screen.height / 2, barWidth * _travelProgress, barHeight);

                GUI.color = new Color(0, 0, 0, 0.5f);
                GUI.DrawTexture(barBg, Texture2D.whiteTexture);
                GUI.color = Color.cyan;
                GUI.DrawTexture(barFill, Texture2D.whiteTexture);

                GUI.color = Color.white;
                GUI.skin.label.alignment = TextAnchor.UpperLeft; // Reset alignment
            }
        }
    }
}
