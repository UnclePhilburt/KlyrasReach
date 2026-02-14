/*
 * Simple Health Bar for Klyra's Reach
 *
 * PURPOSE:
 * Shows a small health bar above character heads
 *
 * HOW TO USE:
 * 1. Add this component to your enemy prefab
 * 2. Adjust settings in Inspector if needed
 * 3. That's it!
 */

using UnityEngine;
using UnityEngine.UI;

namespace KlyrasReach.AI
{
    /// <summary>
    /// Simple health bar that displays above characters
    /// </summary>
    public class SimpleHealthBar : MonoBehaviour
    {
        // PERFORMANCE: Shared sprite for all health bars (huge memory saving!)
        private static Sprite _sharedBoxSprite;

        [Header("Settings")]
        [Tooltip("How high above the character")]
        [SerializeField] private float _heightOffset = 2f;

        [Tooltip("Width of bar in world units")]
        [SerializeField] private float _worldWidth = 1f;

        [Tooltip("Height of bar in world units")]
        [SerializeField] private float _worldHeight = 0.1f;

        [Tooltip("Health bar color")]
        [SerializeField] private Color _healthColor = Color.green;

        [Tooltip("Background color")]
        [SerializeField] private Color _backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        [Tooltip("Only show when damaged?")]
        [SerializeField] private bool _hideWhenFull = true;

        [Tooltip("Update rate based on distance (far enemies update less)")]
        [SerializeField] private bool _useDistanceLOD = true;

        [Tooltip("Max distance to show health bar")]
        [SerializeField] private float _maxVisibleDistance = 15f; // Reduced for performance

        [Tooltip("Disable health bars entirely for performance?")]
        [SerializeField] private bool _disableHealthBars = false;

        // Private variables
        private Camera _mainCamera;
        private GameObject _barObject;
        private SpriteRenderer _fillRenderer;
        private SpriteRenderer _backgroundRenderer;
        private Opsive.UltimateCharacterController.Traits.AttributeManager _attributeManager;
        private Opsive.UltimateCharacterController.Traits.Attribute _healthAttribute;

        // Performance optimization
        private float _lastHealthValue = -1f;
        private int _updateFrameOffset;
        private Transform _cameraTransform;

        private void Start()
        {
            // If disabled, don't create health bars at all
            if (_disableHealthBars)
            {
                enabled = false;
                return;
            }

            _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                _cameraTransform = _mainCamera.transform;
            }

            // Stagger updates across frames
            _updateFrameOffset = Random.Range(0, 10);

            // Get health attribute
            _attributeManager = GetComponent<Opsive.UltimateCharacterController.Traits.AttributeManager>();
            if (_attributeManager == null) return;

            _healthAttribute = _attributeManager.GetAttribute("Health");
            if (_healthAttribute == null) return;

            _lastHealthValue = _healthAttribute.Value;

            CreateHealthBar();
        }

        private void CreateHealthBar()
        {
            // Create parent object
            _barObject = new GameObject("HealthBar");
            _barObject.transform.SetParent(transform);
            _barObject.transform.localPosition = Vector3.up * _heightOffset;

            // Create background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(_barObject.transform);
            bgObj.transform.localPosition = Vector3.zero;
            _backgroundRenderer = bgObj.AddComponent<SpriteRenderer>();
            _backgroundRenderer.sprite = CreateBoxSprite();
            _backgroundRenderer.color = _backgroundColor;
            _backgroundRenderer.transform.localScale = new Vector3(_worldWidth, _worldHeight, 1f);

            // Create fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(_barObject.transform);
            fillObj.transform.localPosition = Vector3.zero;
            _fillRenderer = fillObj.AddComponent<SpriteRenderer>();
            _fillRenderer.sprite = CreateBoxSprite();
            _fillRenderer.color = _healthColor;
            _fillRenderer.transform.localScale = new Vector3(_worldWidth, _worldHeight, 1f);
            _fillRenderer.sortingOrder = 1; // Render on top of background

            // Start hidden if full
            if (_hideWhenFull)
            {
                _barObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (_barObject == null || _healthAttribute == null || _cameraTransform == null) return;

            // Distance-based LOD: Hide health bars for very distant enemies
            if (_useDistanceLOD)
            {
                float sqrDistToCamera = (_barObject.transform.position - _cameraTransform.position).sqrMagnitude;

                // Hide if too far
                if (sqrDistToCamera > _maxVisibleDistance * _maxVisibleDistance)
                {
                    if (_barObject.activeSelf)
                    {
                        _barObject.SetActive(false);
                    }
                    return;
                }

                // Stagger updates for distant health bars
                if (sqrDistToCamera > 20f * 20f) // 20 units
                {
                    // Update every 10 frames
                    if ((Time.frameCount + _updateFrameOffset) % 10 != 0)
                    {
                        return;
                    }
                }
                else if (sqrDistToCamera > 10f * 10f) // 10 units
                {
                    // Update every 5 frames
                    if ((Time.frameCount + _updateFrameOffset) % 5 != 0)
                    {
                        return;
                    }
                }
                else
                {
                    // Even close health bars only update every 3 frames (huge performance gain)
                    if ((Time.frameCount + _updateFrameOffset) % 3 != 0)
                    {
                        return;
                    }
                }
            }

            float currentHealth = _healthAttribute.Value;
            bool healthChanged = Mathf.Abs(currentHealth - _lastHealthValue) > 0.01f;

            // Face camera (only very occasionally - expensive operation!)
            // Quaternion.LookRotation is VERY expensive with many enemies
            if ((Time.frameCount + _updateFrameOffset) % 60 == 0) // Only every 60 frames (~1 second)
            {
                _barObject.transform.rotation = Quaternion.LookRotation(
                    _barObject.transform.position - _cameraTransform.position);
            }

            // Only update fill if health changed
            if (healthChanged)
            {
                _lastHealthValue = currentHealth;

                float healthPercent = currentHealth / _healthAttribute.MaxValue;

                // Update fill scale (shrink from right side)
                Vector3 fillScale = _fillRenderer.transform.localScale;
                fillScale.x = _worldWidth * healthPercent;
                _fillRenderer.transform.localScale = fillScale;

                // Reposition fill to stay left-aligned
                Vector3 fillPos = _fillRenderer.transform.localPosition;
                fillPos.x = -(_worldWidth - fillScale.x) / 2f;
                _fillRenderer.transform.localPosition = fillPos;

                // Show/hide logic
                if (_hideWhenFull)
                {
                    bool shouldShow = healthPercent < 1f && currentHealth > 0;
                    if (_barObject.activeSelf != shouldShow)
                    {
                        _barObject.SetActive(shouldShow);
                    }
                }
            }
        }

        private Sprite CreateBoxSprite()
        {
            // PERFORMANCE: Create sprite only once and share it across all health bars
            // This saves 200 texture allocations with 100 enemies!
            if (_sharedBoxSprite == null)
            {
                Texture2D texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                _sharedBoxSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            return _sharedBoxSprite;
        }

        private void OnDestroy()
        {
            if (_barObject != null)
            {
                Destroy(_barObject);
            }
        }
    }
}
