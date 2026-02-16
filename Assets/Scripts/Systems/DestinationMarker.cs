/*
 * Destination Marker for Klyra's Reach
 *
 * PURPOSE:
 * Shows a HUD marker for destinations (planets, stations, outposts) when player looks at them.
 * Helps identify locations in space for navigation.
 *
 * HOW TO USE:
 * 1. Add this script to a planet, station, or outpost GameObject
 * 2. Set the destination name
 * 3. When player looks at it while in a ship, marker appears on HUD
 */

using UnityEngine;
using UnityEngine.InputSystem;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Displays a HUD marker for destinations when player looks at them
    /// </summary>
    public class DestinationMarker : MonoBehaviour
    {
        [Header("Marker Settings")]
        [Tooltip("Name of this destination (shown on HUD)")]
        [SerializeField] private string _destinationName = "Destination";

        [Tooltip("How far away the marker can be seen")]
        [SerializeField] private float _maxDisplayDistance = 50000f;

        [Tooltip("Color of the marker")]
        [SerializeField] private Color _markerColor = Color.cyan;

        [Tooltip("Size of the marker on screen")]
        [SerializeField] private float _markerSize = 30f;

        [Tooltip("Font size for the label")]
        [SerializeField] private int _fontSize = 16;

        [Header("Detection")]
        [Tooltip("Tag to identify ships")]
        [SerializeField] private string _shipTag = "Ship";

        [Tooltip("Only show marker when looking directly at it (within this angle)")]
        [SerializeField] private float _viewAngle = 45f;

        [Header("Quantum Travel")]
        [Tooltip("Minimum distance to allow quantum travel (prevents clutter when close)")]
        [SerializeField] private float _minQuantumDistance = 5000f;

        [Tooltip("Use a specific arrival point instead of auto-calculating")]
        [SerializeField] private bool _useSpecificArrivalPoint = true;

        [Tooltip("Specific world position to arrive at (if using specific point)")]
        [SerializeField] private Transform _arrivalPoint;

        [Tooltip("Distance from destination to spawn ship when quantum traveling (if no specific point)")]
        [SerializeField] private float _quantumArrivalDistance = 1000f;

        [Tooltip("Direction offset for arrival (relative to destination, if no specific point)")]
        [SerializeField] private Vector3 _quantumArrivalOffset = new Vector3(0, 2000f, -5000f);

        // Private variables
        private Camera _mainCamera;
        private bool _playerInShip = false;
        private Transform _playerShipTransform;
        private Texture2D _roundedBoxTexture;
        private GUIStyle _backgroundStyle;
        private bool _isMouseOver = false;
        private Rect _markerScreenRect;

        /// <summary>
        /// Initialize camera reference and create rounded box texture
        /// </summary>
        private void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogWarning("[DestinationMarker] No main camera found!");
            }

            // Create rounded box texture
            CreateRoundedBoxTexture();
        }

        /// <summary>
        /// Creates a texture with rounded corners for the background
        /// </summary>
        private void CreateRoundedBoxTexture()
        {
            int width = 64;
            int height = 64;
            int cornerRadius = 8;

            _roundedBoxTexture = new Texture2D(width, height);
            Color backgroundColor = new Color(0, 0, 0, 0.7f);
            Color transparent = new Color(0, 0, 0, 0);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Check if pixel is in corner region
                    bool inTopLeft = x < cornerRadius && y < cornerRadius;
                    bool inTopRight = x >= width - cornerRadius && y < cornerRadius;
                    bool inBottomLeft = x < cornerRadius && y >= height - cornerRadius;
                    bool inBottomRight = x >= width - cornerRadius && y >= height - cornerRadius;

                    if (inTopLeft || inTopRight || inBottomLeft || inBottomRight)
                    {
                        // Calculate distance from corner
                        float cornerX = inTopLeft || inBottomLeft ? cornerRadius : width - cornerRadius;
                        float cornerY = inTopLeft || inTopRight ? cornerRadius : height - cornerRadius;
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(cornerX, cornerY));

                        // If outside corner radius, make transparent
                        if (distance > cornerRadius)
                        {
                            _roundedBoxTexture.SetPixel(x, y, transparent);
                        }
                        else
                        {
                            _roundedBoxTexture.SetPixel(x, y, backgroundColor);
                        }
                    }
                    else
                    {
                        _roundedBoxTexture.SetPixel(x, y, backgroundColor);
                    }
                }
            }

            _roundedBoxTexture.Apply();

            // Create GUI style with the rounded texture
            _backgroundStyle = new GUIStyle();
            _backgroundStyle.normal.background = _roundedBoxTexture;
        }

        /// <summary>
        /// Check for nearby ships every frame
        /// </summary>
        private void Update()
        {
            // Only track ships when quantum mode is active
            if (!QuantumModeManager.IsQuantumModeActive)
            {
                _playerInShip = false;
                _playerShipTransform = null;
                _isMouseOver = false;
                return;
            }

            // Try to find active ship if we don't have one
            if (_playerShipTransform == null)
            {
                FindActiveShip();
            }

            // Update if player is in ship based on distance
            if (_playerShipTransform != null)
            {
                float distance = Vector3.Distance(transform.position, _playerShipTransform.position);
                _playerInShip = distance <= _maxDisplayDistance;
            }
            else
            {
                _playerInShip = false;
            }

            // Check for quantum travel activation with V key
            // Only allow quantum travel if player is ALREADY in ship and flying
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.vKey.wasPressedThisFrame)
            {
                Debug.Log($"[DestinationMarker '{_destinationName}'] ========== V KEY PRESSED ==========");
                Debug.Log($"[DestinationMarker '{_destinationName}'] _isMouseOver: {_isMouseOver}");
                Debug.Log($"[DestinationMarker '{_destinationName}'] QuantumMode: {QuantumModeManager.IsQuantumModeActive}");
                Debug.Log($"[DestinationMarker '{_destinationName}'] HasShip: {_playerShipTransform != null}");
                Debug.Log($"[DestinationMarker '{_destinationName}'] All conditions met: {_isMouseOver && QuantumModeManager.IsQuantumModeActive && _playerShipTransform != null}");
            }

            if (_isMouseOver && keyboard != null && keyboard.vKey.wasPressedThisFrame && QuantumModeManager.IsQuantumModeActive && _playerShipTransform != null)
            {
                Debug.Log($"[DestinationMarker '{_destinationName}'] Checking ship controller...");

                // Double check that ship is actually being piloted
                var controller = _playerShipTransform.GetComponent<Player.ShipController>();
                Debug.Log($"[DestinationMarker '{_destinationName}'] Ship controller found: {controller != null}, IsActive: {controller?.IsActive}");

                if (controller != null && controller.IsActive)
                {
                    Debug.Log($"[DestinationMarker '{_destinationName}'] ✓ All checks passed - calling InitiateQuantumTravel()");
                    InitiateQuantumTravel();
                }
                else
                {
                    Debug.LogWarning($"[DestinationMarker '{_destinationName}'] Ship controller not active - cannot quantum travel");
                }
            }
        }

        /// <summary>
        /// Public method to get quantum arrival position
        /// </summary>
        public Vector3 GetQuantumArrivalPosition()
        {
            // If using a specific arrival point, use that
            if (_useSpecificArrivalPoint && _arrivalPoint != null)
            {
                Debug.Log($"[DestinationMarker] Using specific arrival point at {_arrivalPoint.position}");
                return _arrivalPoint.position;
            }

            // Calculate a safe arrival distance based on the object's size
            float safeDistance = CalculateSafeArrivalDistance();

            // Direction from destination towards camera (so you arrive facing it)
            Vector3 directionFromDestination = (_mainCamera.transform.position - transform.position).normalized;

            // Arrive at a safe distance in the direction you were looking from
            Vector3 arrivalPos = transform.position + directionFromDestination * safeDistance;
            Debug.Log($"[DestinationMarker] Calculated arrival position: {arrivalPos}, distance from destination: {Vector3.Distance(arrivalPos, transform.position)}");
            return arrivalPos;
        }

        /// <summary>
        /// Calculates a safe arrival distance based on the destination's size
        /// </summary>
        private float CalculateSafeArrivalDistance()
        {
            // Try to get the bounds of the destination
            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                // Use the bounds size to determine safe distance
                float maxSize = Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.y, renderer.bounds.size.z);
                float safeDistance = maxSize * 5f + 3000f; // Much larger buffer to stay well outside
                Debug.Log($"[DestinationMarker] Calculated safe distance: {safeDistance} (maxSize: {maxSize})");
                return safeDistance;
            }

            // Fallback to manual offset if no renderer found
            Debug.Log($"[DestinationMarker] No renderer found, using fallback distance: {_quantumArrivalOffset.magnitude}");
            return _quantumArrivalOffset.magnitude;
        }

        /// <summary>
        /// Initiates quantum travel to this destination
        /// </summary>
        private void InitiateQuantumTravel()
        {
            if (_playerShipTransform == null)
            {
                Debug.LogWarning("[DestinationMarker] Cannot initiate quantum travel - no player ship transform");
                return;
            }

            Debug.Log($"[DestinationMarker] Initiating quantum travel to {_destinationName}");

            // Get QuantumModeManager to handle the travel
            QuantumModeManager manager = QuantumModeManager.Instance;
            if (manager != null)
            {
                Debug.Log($"[DestinationMarker] Found QuantumModeManager, calling StartQuantumTravel");
                manager.StartQuantumTravel(this);
            }
            else
            {
                Debug.LogError("[DestinationMarker] QuantumModeManager.Instance is null!");
            }
        }

        /// <summary>
        /// Tries to find the active player ship
        /// </summary>
        private void FindActiveShip()
        {
            Debug.Log($"[DestinationMarker '{_destinationName}'] FindActiveShip() - Searching for ships with tag '{_shipTag}'");
            GameObject[] ships = GameObject.FindGameObjectsWithTag(_shipTag);
            Debug.Log($"[DestinationMarker '{_destinationName}'] Found {ships.Length} ships");

            foreach (GameObject ship in ships)
            {
                Debug.Log($"[DestinationMarker '{_destinationName}'] Checking ship: {ship.name}");
                var shipController = ship.GetComponent<Player.ShipController>();
                if (shipController != null)
                {
                    Debug.Log($"[DestinationMarker '{_destinationName}'] Ship has controller, IsActive: {shipController.IsActive}");
                    if (shipController.IsActive)
                    {
                        _playerShipTransform = ship.transform;
                        Debug.Log($"[DestinationMarker '{_destinationName}'] ✓ Found active ship: {ship.name}");
                        return;
                    }
                }
                else
                {
                    Debug.Log($"[DestinationMarker '{_destinationName}'] Ship has NO ShipController component");
                }
            }

            Debug.Log($"[DestinationMarker '{_destinationName}'] No active ship found");
        }

        /// <summary>
        /// Draw the UI marker on screen
        /// </summary>
        private void OnGUI()
        {
            // Only show when quantum mode is active
            if (!QuantumModeManager.IsQuantumModeActive)
            {
                return;
            }

            // Only show if player is in ship and in range
            if (!_playerInShip || _mainCamera == null || _playerShipTransform == null)
            {
                Debug.Log($"[DestinationMarker '{_destinationName}'] Not showing - PlayerInShip: {_playerInShip}, HasCamera: {_mainCamera != null}, HasShip: {_playerShipTransform != null}");
                return;
            }

            // Calculate distance to destination
            float distance = Vector3.Distance(transform.position, _playerShipTransform.position);

            // Don't show quantum travel option if too close (prevents UI clutter)
            if (distance < _minQuantumDistance)
            {
                Debug.Log($"[DestinationMarker '{_destinationName}'] Too close - Distance: {distance}, Min: {_minQuantumDistance}");
                return;
            }

            // Convert destination position to screen position
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(transform.position);

            // Only draw if in front of camera
            if (screenPos.z > 0)
            {
                // Check if destination is within view angle
                Vector3 directionToDestination = (transform.position - _mainCamera.transform.position).normalized;
                float angle = Vector3.Angle(_mainCamera.transform.forward, directionToDestination);

                Debug.Log($"[DestinationMarker '{_destinationName}'] Angle from camera: {angle:F1}° (max: {_viewAngle}°), Distance: {distance:F0}m");

                if (angle > _viewAngle)
                {
                    Debug.Log($"[DestinationMarker '{_destinationName}'] Outside view angle - not showing");
                    return; // Not looking at it
                }

                Debug.Log($"[DestinationMarker '{_destinationName}'] VISIBLE - Distance: {distance:F0}m, Angle: {angle:F1}°");

                // Draw marker box
                float halfSize = _markerSize / 2f;
                Rect markerRect = new Rect(
                    screenPos.x - halfSize,
                    Screen.height - screenPos.y - halfSize,
                    _markerSize,
                    _markerSize
                );

                // Check if marker is near center of screen (what player is looking at)
                Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
                Vector2 markerCenter = new Vector2(screenPos.x, Screen.height - screenPos.y);
                float distanceFromCenter = Vector2.Distance(screenCenter, markerCenter);
                _isMouseOver = distanceFromCenter < 100f; // Within 100 pixels of center

                Debug.Log($"[DestinationMarker '{_destinationName}'] DistanceFromCenter: {distanceFromCenter:F0}px, IsMouseOver: {_isMouseOver}");

                // Set marker color (highlight if looking at it)
                GUI.color = _isMouseOver ? Color.white : _markerColor;

                // Draw box outline
                DrawBox(markerRect);

                // Store rect for click detection
                _markerScreenRect = markerRect;

                // Draw label with name and distance
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.skin.label.fontSize = _fontSize;

                // Format distance (km if > 1000m)
                string distanceText;
                if (distance >= 1000f)
                {
                    distanceText = $"{distance / 1000f:F1}km";
                }
                else
                {
                    distanceText = $"{distance:F0}m";
                }

                string labelText = $"{_destinationName}\n{distanceText}";

                // Add "Press V" prompt when hovering
                if (_isMouseOver)
                {
                    labelText += "\nPress V to Travel";
                }

                Rect labelRect = new Rect(
                    screenPos.x - 100,
                    Screen.height - screenPos.y + _markerSize,
                    200,
                    70
                );

                // Draw dark background box with rounded corners
                if (_backgroundStyle != null)
                {
                    Rect backgroundRect = new Rect(
                        labelRect.x + 10,
                        labelRect.y,
                        labelRect.width - 20,
                        labelRect.height
                    );
                    GUI.Box(backgroundRect, "", _backgroundStyle);
                }

                // Draw black text shadow/outline for better visibility
                GUI.color = Color.black;
                GUI.Label(new Rect(labelRect.x - 1, labelRect.y - 1, labelRect.width, labelRect.height), labelText);
                GUI.Label(new Rect(labelRect.x + 1, labelRect.y - 1, labelRect.width, labelRect.height), labelText);
                GUI.Label(new Rect(labelRect.x - 1, labelRect.y + 1, labelRect.width, labelRect.height), labelText);
                GUI.Label(new Rect(labelRect.x + 1, labelRect.y + 1, labelRect.width, labelRect.height), labelText);

                // Draw white text on top
                GUI.color = Color.white;
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
            Gizmos.color = new Color(0, 1, 1, 0.1f);
            Gizmos.DrawWireSphere(transform.position, _maxDisplayDistance);

            // Draw destination marker
            Gizmos.color = _markerColor;
            Gizmos.DrawWireSphere(transform.position, 50f);
        }
    }
}
