/*
 * Adaptive Crosshair for Klyra's Reach
 *
 * PURPOSE:
 * Shows a small crosshair that contrasts with the background
 *
 * HOW TO USE:
 * 1. Create an empty GameObject called "Crosshair" in your scene
 * 2. Add this script to it
 * 3. That's it! Crosshair will appear and adapt colors automatically
 */

using UnityEngine;
using UnityEngine.UI;

namespace KlyrasReach.UI
{
    /// <summary>
    /// Crosshair that automatically contrasts with background
    /// </summary>
    public class AdaptiveCrosshair : MonoBehaviour
    {
        [Header("Crosshair Settings")]
        [Tooltip("Size of dot")]
        [SerializeField] private float _dotSize = 4f;

        [Tooltip("How often to sample background color (seconds)")]
        [SerializeField] private float _sampleInterval = 0.1f;

        [Tooltip("Use outline for extra visibility?")]
        [SerializeField] private bool _useOutline = true;

        // Private variables
        private Canvas _canvas;
        private Image _dot;
        private Camera _mainCamera;
        private float _nextSampleTime;
        private RenderTexture _sampleTexture;
        private Texture2D _readTexture;

        private void Start()
        {
            _mainCamera = Camera.main;
            CreateCrosshair();

            // Create small texture for sampling
            _sampleTexture = new RenderTexture(1, 1, 0);
            _readTexture = new Texture2D(1, 1);
        }

        private void CreateCrosshair()
        {
            // Create canvas
            GameObject canvasGO = new GameObject("CrosshairCanvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 1000; // Render on top

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Create dot
            GameObject dotGO = new GameObject("CrosshairDot");
            dotGO.transform.SetParent(canvasGO.transform);
            RectTransform rect = dotGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(_dotSize, _dotSize);

            _dot = dotGO.AddComponent<Image>();
            _dot.color = Color.white;

            // Make it a circle
            Texture2D circleTexture = CreateCircleTexture((int)_dotSize * 4);
            _dot.sprite = Sprite.Create(circleTexture,
                new Rect(0, 0, circleTexture.width, circleTexture.height),
                new Vector2(0.5f, 0.5f));

            // Add outline if enabled
            if (_useOutline)
            {
                AddOutline(_dot);
            }
        }

        private Texture2D CreateCircleTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size);
            float radius = size / 2f;
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    Color pixelColor = distance <= radius ? Color.white : Color.clear;
                    texture.SetPixel(x, y, pixelColor);
                }
            }

            texture.Apply();
            return texture;
        }

        private void AddOutline(Image image)
        {
            Outline outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, 1);
        }

        private void Update()
        {
            if (Time.time >= _nextSampleTime)
            {
                UpdateCrosshairColor();
                _nextSampleTime = Time.time + _sampleInterval;
            }
        }

        private void UpdateCrosshairColor()
        {
            if (_mainCamera == null || _dot == null) return;

            // Sample center pixel
            Color backgroundColor = SampleCenterPixel();

            // Calculate contrasting color
            float brightness = backgroundColor.r * 0.299f + backgroundColor.g * 0.587f + backgroundColor.b * 0.114f;
            Color crosshairColor = brightness > 0.5f ? Color.black : Color.white;

            // Apply color
            _dot.color = crosshairColor;
        }

        private Color SampleCenterPixel()
        {
            // Render camera to small texture at screen center
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = _sampleTexture;

            // Capture screen center
            _mainCamera.targetTexture = _sampleTexture;
            _mainCamera.Render();
            _mainCamera.targetTexture = null;

            // Read pixel
            _readTexture.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
            _readTexture.Apply();

            RenderTexture.active = currentRT;

            return _readTexture.GetPixel(0, 0);
        }

        private void OnDestroy()
        {
            if (_sampleTexture != null)
            {
                _sampleTexture.Release();
                Destroy(_sampleTexture);
            }

            if (_readTexture != null)
            {
                Destroy(_readTexture);
            }
        }
    }
}
