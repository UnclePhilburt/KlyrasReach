/*
 * Landing Zone UI Marker for Klyra's Reach
 *
 * PURPOSE:
 * Shows a UI marker on screen when player (in ship) is close to a landing zone.
 * Helps players find where to land on planets/stations.
 *
 * HOW TO USE:
 * 1. Create a landing pad GameObject
 * 2. Add this script to it
 * 3. Configure the marker settings in Inspector
 * 4. When player flies close in a ship, marker appears on HUD
 */

using UnityEngine;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Shows a UI marker for landing zones when player is nearby in a ship
    /// </summary>
    public class LandingZoneMarker : MonoBehaviour
    {
        [Header("Marker Settings")]
        [Tooltip("Name of this landing zone (shown on HUD)")]
        [SerializeField] private string _landingZoneName = "Landing Pad";

        [Tooltip("How close player needs to be to see the marker (in units)")]
        [SerializeField] private float _displayRange = 2000f;

        [Tooltip("Color of the marker")]
        [SerializeField] private Color _markerColor = Color.cyan;

        [Tooltip("Size of the marker on screen")]
        [SerializeField] private float _markerSize = 20f;

        [Header("Detection")]
        [Tooltip("Tag to identify ships")]
        [SerializeField] private string _shipTag = "Ship";

        // Private variables
        private Camera _mainCamera;
        private bool _playerInShip = false;
        private Transform _playerShipTransform;

        /// <summary>
        /// Initialize camera reference
        /// </summary>
        private void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogWarning("[LandingZoneMarker] No main camera found!");
            }
        }

        /// <summary>
        /// Check for nearby ships every frame
        /// </summary>
        private void Update()
        {
            // Try to find active ship if we don't have one
            if (_playerShipTransform == null)
            {
                FindActiveShip();
            }

            // Update if player is in ship based on distance
            if (_playerShipTransform != null)
            {
                float distance = Vector3.Distance(transform.position, _playerShipTransform.position);
                _playerInShip = distance <= _displayRange;
            }
            else
            {
                _playerInShip = false;
            }
        }

        /// <summary>
        /// Tries to find the active player ship
        /// </summary>
        private void FindActiveShip()
        {
            // Find all ships in scene
            GameObject[] ships = GameObject.FindGameObjectsWithTag(_shipTag);

            foreach (GameObject ship in ships)
            {
                // Check if this ship is active (being piloted)
                var shipController = ship.GetComponent<Player.ShipController>();
                if (shipController != null && shipController.IsActive)
                {
                    _playerShipTransform = ship.transform;
                    return;
                }
            }
        }

        /// <summary>
        /// Draw the UI marker on screen
        /// </summary>
        private void OnGUI()
        {
            // Only show if player is in ship and in range
            if (!_playerInShip || _mainCamera == null)
            {
                return;
            }

            // Convert landing zone world position to screen position
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(transform.position);

            // Only draw if in front of camera
            if (screenPos.z > 0)
            {
                // Calculate distance to landing zone
                float distance = Vector3.Distance(transform.position, _playerShipTransform.position);

                // Draw marker box
                float halfSize = _markerSize / 2f;
                Rect markerRect = new Rect(
                    screenPos.x - halfSize,
                    Screen.height - screenPos.y - halfSize,
                    _markerSize,
                    _markerSize
                );

                // Set marker color
                GUI.color = _markerColor;

                // Draw box outline
                DrawBox(markerRect);

                // Draw label with name and distance
                GUI.color = Color.white;
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.skin.label.fontSize = 14;

                string labelText = $"{_landingZoneName}\n{distance:F0}m";
                Rect labelRect = new Rect(
                    screenPos.x - 100,
                    Screen.height - screenPos.y + _markerSize,
                    200,
                    40
                );

                GUI.Label(labelRect, labelText);

                // Reset GUI color
                GUI.color = Color.white;
            }
        }

        /// <summary>
        /// Draws a box outline on the GUI
        /// </summary>
        private void DrawBox(Rect rect)
        {
            // Draw four lines to make a box
            float thickness = 2f;

            // Top
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            // Bottom
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), Texture2D.whiteTexture);
            // Left
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            // Right
            GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        }

        /// <summary>
        /// Draw debug visualization in Scene view
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw detection range
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _displayRange);

            // Draw landing zone marker
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 5f);
        }
    }
}
