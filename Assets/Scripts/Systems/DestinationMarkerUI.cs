/*
 * Destination Marker UI for Klyra's Reach
 *
 * PURPOSE:
 * Shows a HUD marker for destinations (planets, stations, outposts) when player looks at them.
 * Uses Unity UI Canvas and TextMeshPro for better visuals and rounded corners.
 *
 * HOW TO USE:
 * 1. Add this script to a planet, station, or outpost GameObject
 * 2. Assign the UI prefab (will be created automatically if not assigned)
 * 3. Set the destination name
 * 4. When player looks at it while in a ship, marker appears on HUD
 */

using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Displays a HUD marker for destinations using Canvas UI
    /// </summary>
    public class DestinationMarkerUI : MonoBehaviour
    {
        [Header("Marker Settings")]
        [Tooltip("Name of this destination (shown on HUD)")]
        [SerializeField] private string _destinationName = "Destination";

        [Tooltip("How far away the marker can be seen")]
        [SerializeField] private float _maxDisplayDistance = 50000f;

        [Tooltip("Color of the marker")]
        [SerializeField] private Color _markerColor = Color.cyan;

        [Tooltip("Size of the marker box")]
        [SerializeField] private float _markerSize = 30f;

        [Tooltip("Font size for the label")]
        [SerializeField] private int _fontSize = 18;

        [Header("Detection")]
        [Tooltip("Tag to identify ships")]
        [SerializeField] private string _shipTag = "Ship";

        [Tooltip("Only show marker when looking directly at it (within this angle)")]
        [SerializeField] private float _viewAngle = 45f;

        [Header("UI References")]
        [Tooltip("Canvas to spawn UI elements on (leave empty to find automatically)")]
        [SerializeField] private Canvas _canvas;

        // Private variables
        private Camera _mainCamera;
        private bool _playerInShip = false;
        private Transform _playerShipTransform;

        // UI elements
        private GameObject _markerUI;
        private RectTransform _markerRect;
        private Image _backgroundImage;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _distanceText;
        private Image _boxImage;

        /// <summary>
        /// Initialize
        /// </summary>
        private void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogWarning("[DestinationMarkerUI] No main camera found!");
            }

            // Find or create canvas
            if (_canvas == null)
            {
                _canvas = FindObjectOfType<Canvas>();

                if (_canvas == null)
                {
                    // Create canvas
                    GameObject canvasObj = new GameObject("DestinationMarkersCanvas");
                    _canvas = canvasObj.AddComponent<Canvas>();
                    _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasObj.AddComponent<CanvasScaler>();
                    canvasObj.AddComponent<GraphicRaycaster>();
                }
            }

            // Create UI elements
            CreateMarkerUI();
        }

        /// <summary>
        /// Creates the UI elements for the marker
        /// </summary>
        private void CreateMarkerUI()
        {
            // Main container
            _markerUI = new GameObject($"Marker_{_destinationName}");
            _markerUI.transform.SetParent(_canvas.transform, false);
            _markerRect = _markerUI.AddComponent<RectTransform>();
            _markerRect.sizeDelta = new Vector2(200, 80);

            // Box outline
            GameObject boxObj = new GameObject("Box");
            boxObj.transform.SetParent(_markerUI.transform, false);
            RectTransform boxRect = boxObj.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 1f);
            boxRect.anchorMax = new Vector2(0.5f, 1f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(_markerSize, _markerSize);
            boxRect.anchoredPosition = new Vector2(0, 0);

            _boxImage = boxObj.AddComponent<Image>();
            _boxImage.color = _markerColor;
            _boxImage.sprite = null; // Will draw as box
            _boxImage.type = Image.Type.Sliced;

            // Add outline component for box effect
            Outline boxOutline = boxObj.AddComponent<Outline>();
            boxOutline.effectColor = _markerColor;
            boxOutline.effectDistance = new Vector2(2, 2);

            // Background panel with rounded corners
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(_markerUI.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0f);
            bgRect.anchorMax = new Vector2(0.5f, 0f);
            bgRect.pivot = new Vector2(0.5f, 1f);
            bgRect.sizeDelta = new Vector2(180, 50);
            bgRect.anchoredPosition = new Vector2(0, -_markerSize - 5);

            _backgroundImage = bgObj.AddComponent<Image>();
            _backgroundImage.color = new Color(0, 0, 0, 0.7f);

            // Name text
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(bgObj.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = Vector2.one;
            nameRect.offsetMin = new Vector2(5, 15);
            nameRect.offsetMax = new Vector2(-5, -5);

            _nameText = nameObj.AddComponent<TextMeshProUGUI>();
            _nameText.text = _destinationName;
            _nameText.fontSize = _fontSize;
            _nameText.alignment = TextAlignmentOptions.Center;
            _nameText.color = Color.white;
            _nameText.fontStyle = FontStyles.Bold;

            // Distance text
            GameObject distObj = new GameObject("DistanceText");
            distObj.transform.SetParent(bgObj.transform, false);
            RectTransform distRect = distObj.AddComponent<RectTransform>();
            distRect.anchorMin = Vector2.zero;
            distRect.anchorMax = Vector2.one;
            distRect.offsetMin = new Vector2(5, 5);
            distRect.offsetMax = new Vector2(-5, -15);

            _distanceText = distObj.AddComponent<TextMeshProUGUI>();
            _distanceText.text = "0m";
            _distanceText.fontSize = _fontSize - 2;
            _distanceText.alignment = TextAlignmentOptions.Center;
            _distanceText.color = new Color(0.8f, 0.8f, 0.8f);

            // Start hidden
            _markerUI.SetActive(false);
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

            // Update marker visibility and position
            UpdateMarker();
        }

        /// <summary>
        /// Tries to find the active player ship
        /// </summary>
        private void FindActiveShip()
        {
            GameObject[] ships = GameObject.FindGameObjectsWithTag(_shipTag);

            foreach (GameObject ship in ships)
            {
                var shipController = ship.GetComponent<Player.ShipController>();
                if (shipController != null && shipController.IsActive)
                {
                    _playerShipTransform = ship.transform;
                    return;
                }
            }
        }

        /// <summary>
        /// Updates marker visibility and position
        /// </summary>
        private void UpdateMarker()
        {
            if (_markerUI == null || _mainCamera == null)
            {
                return;
            }

            // Only show markers when quantum mode is active
            if (!QuantumModeManager.IsQuantumModeActive)
            {
                _markerUI.SetActive(false);
                return;
            }

            // Check if player is in ship and in range
            if (_playerShipTransform != null)
            {
                float distance = Vector3.Distance(transform.position, _playerShipTransform.position);
                _playerInShip = distance <= _maxDisplayDistance;

                if (_playerInShip)
                {
                    // Check if destination is in front of camera
                    Vector3 screenPos = _mainCamera.WorldToScreenPoint(transform.position);

                    if (screenPos.z > 0)
                    {
                        // Check if destination is within view angle
                        Vector3 directionToDestination = (transform.position - _mainCamera.transform.position).normalized;
                        float angle = Vector3.Angle(_mainCamera.transform.forward, directionToDestination);

                        if (angle <= _viewAngle)
                        {
                            // Show marker
                            _markerUI.SetActive(true);

                            // Update position
                            _markerRect.position = screenPos;

                            // Update distance text
                            string distanceText;
                            if (distance >= 1000f)
                            {
                                distanceText = $"{distance / 1000f:F1}km";
                            }
                            else
                            {
                                distanceText = $"{distance:F0}m";
                            }
                            _distanceText.text = distanceText;

                            return;
                        }
                    }
                }
            }

            // Hide marker
            _markerUI.SetActive(false);
        }

        /// <summary>
        /// Clean up UI elements
        /// </summary>
        private void OnDestroy()
        {
            if (_markerUI != null)
            {
                Destroy(_markerUI);
            }
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
