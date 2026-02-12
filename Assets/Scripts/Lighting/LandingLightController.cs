/*
 * Landing Light Controller for Klyra's Reach
 *
 * PURPOSE:
 * Creates synchronized flashing lights on landing pads, similar to airport runway lights.
 * All lights flash in perfect sync with customizable patterns.
 *
 * HOW TO USE:
 * 1. Add this script to your landing pad GameObject
 * 2. Create child GameObjects with Light components for each light position
 * 3. Choose a flash pattern in the Inspector
 * 4. Press Play - lights will flash in sync!
 */

using UnityEngine;

namespace KlyrasReach.Lighting
{
    /// <summary>
    /// Flash pattern modes for landing lights
    /// </summary>
    public enum FlashPattern
    {
        Simultaneous,      // All lights flash together
        Sequential,        // Lights flash one after another in sequence
        AlternatingPairs,  // Pairs of lights alternate (left-right pattern)
        Wave,              // Lights flash in a wave pattern
        RandomFlicker,     // Random flickering (like broken lights)
        Pulse              // Smooth pulsing brightness
    }

    /// <summary>
    /// Controls synchronized flashing lights on landing pads
    /// </summary>
    public class LandingLightController : MonoBehaviour
    {
        [Header("Flash Pattern")]
        [Tooltip("How should the lights flash?")]
        [SerializeField] private FlashPattern _flashPattern = FlashPattern.Simultaneous;

        [Tooltip("Time between flashes (seconds)")]
        [SerializeField] private float _flashInterval = 1f;

        [Tooltip("How long each flash lasts (seconds)")]
        [SerializeField] private float _flashDuration = 0.2f;

        [Tooltip("Start flashing automatically?")]
        [SerializeField] private bool _autoStart = true;

        [Header("Light Settings")]
        [Tooltip("Light intensity when ON")]
        [SerializeField] private float _onIntensity = 2f;

        [Tooltip("Light intensity when OFF")]
        [SerializeField] private float _offIntensity = 0f;

        [Tooltip("Light color when flashing")]
        [SerializeField] private Color _flashColor = Color.white;

        [Header("Sequential/Wave Settings")]
        [Tooltip("Delay between each light in sequence (seconds)")]
        [SerializeField] private float _sequenceDelay = 0.1f;

        [Header("Pulse Settings")]
        [Tooltip("Speed of pulse animation")]
        [SerializeField] private float _pulseSpeed = 2f;

        // Private variables
        private Light[] _lights;
        private float _timer;
        private bool _isFlashing = false;
        private int _currentLightIndex = 0;

        /// <summary>
        /// Initialize all lights
        /// </summary>
        private void Start()
        {
            SetupLights();

            if (_autoStart)
            {
                StartFlashing();
            }
        }

        /// <summary>
        /// Update flash animation
        /// </summary>
        private void Update()
        {
            if (!_isFlashing || _lights == null || _lights.Length == 0)
                return;

            _timer += Time.deltaTime;

            switch (_flashPattern)
            {
                case FlashPattern.Simultaneous:
                    UpdateSimultaneousFlash();
                    break;

                case FlashPattern.Sequential:
                    UpdateSequentialFlash();
                    break;

                case FlashPattern.AlternatingPairs:
                    UpdateAlternatingPairs();
                    break;

                case FlashPattern.Wave:
                    UpdateWaveFlash();
                    break;

                case FlashPattern.RandomFlicker:
                    UpdateRandomFlicker();
                    break;

                case FlashPattern.Pulse:
                    UpdatePulse();
                    break;
            }
        }

        /// <summary>
        /// Finds or creates Light components on all child objects
        /// </summary>
        private void SetupLights()
        {
            // Get all Light components in children
            _lights = GetComponentsInChildren<Light>();

            if (_lights.Length == 0)
            {
                Debug.LogWarning($"[LandingLightController] No lights found on '{gameObject.name}'. Add child GameObjects with Light components.");
                return;
            }

            Debug.Log($"[LandingLightController] Found {_lights.Length} landing lights on '{gameObject.name}'");

            // Configure each light
            foreach (Light light in _lights)
            {
                light.intensity = _offIntensity;
                light.color = _flashColor;
            }
        }

        /// <summary>
        /// All lights flash at the same time
        /// </summary>
        private void UpdateSimultaneousFlash()
        {
            float cycleTime = _flashInterval + _flashDuration;

            if (_timer >= cycleTime)
            {
                _timer = 0f;
            }

            bool shouldBeOn = _timer < _flashDuration;

            foreach (Light light in _lights)
            {
                light.intensity = shouldBeOn ? _onIntensity : _offIntensity;
            }
        }

        /// <summary>
        /// Lights flash one after another in sequence
        /// </summary>
        private void UpdateSequentialFlash()
        {
            float totalCycleTime = (_lights.Length * _sequenceDelay) + _flashInterval;

            if (_timer >= totalCycleTime)
            {
                _timer = 0f;
            }

            // Turn all lights off first
            foreach (Light light in _lights)
            {
                light.intensity = _offIntensity;
            }

            // Figure out which light should be on
            int activeLightIndex = Mathf.FloorToInt(_timer / _sequenceDelay);
            if (activeLightIndex < _lights.Length)
            {
                float lightLocalTime = _timer - (activeLightIndex * _sequenceDelay);
                if (lightLocalTime < _flashDuration)
                {
                    _lights[activeLightIndex].intensity = _onIntensity;
                }
            }
        }

        /// <summary>
        /// Lights alternate in pairs (left-right pattern)
        /// </summary>
        private void UpdateAlternatingPairs()
        {
            float cycleTime = _flashInterval + _flashDuration;

            if (_timer >= cycleTime * 2)
            {
                _timer = 0f;
            }

            bool firstGroupActive = (_timer < cycleTime) && (_timer % cycleTime < _flashDuration);
            bool secondGroupActive = (_timer >= cycleTime) && ((_timer - cycleTime) < _flashDuration);

            // Even indices = group 1, odd indices = group 2
            for (int i = 0; i < _lights.Length; i++)
            {
                bool isEven = (i % 2 == 0);
                bool shouldBeOn = (isEven && firstGroupActive) || (!isEven && secondGroupActive);
                _lights[i].intensity = shouldBeOn ? _onIntensity : _offIntensity;
            }
        }

        /// <summary>
        /// Lights flash in a wave pattern across the pad
        /// </summary>
        private void UpdateWaveFlash()
        {
            // Similar to sequential but smoother
            float waveSpeed = 1f / _flashInterval;

            for (int i = 0; i < _lights.Length; i++)
            {
                float offset = (float)i / _lights.Length;
                float wave = Mathf.Sin((_timer * waveSpeed * Mathf.PI * 2f) - (offset * Mathf.PI * 2f));

                // Map sine wave (-1 to 1) to intensity (0 to max)
                float intensity = Mathf.Lerp(_offIntensity, _onIntensity, (wave + 1f) * 0.5f);
                _lights[i].intensity = intensity;
            }
        }

        /// <summary>
        /// Random flickering like damaged lights
        /// </summary>
        private void UpdateRandomFlicker()
        {
            foreach (Light light in _lights)
            {
                if (Random.value < 0.1f) // 10% chance each frame
                {
                    light.intensity = Random.value < 0.5f ? _onIntensity : _offIntensity;
                }
            }
        }

        /// <summary>
        /// Smooth pulsing brightness
        /// </summary>
        private void UpdatePulse()
        {
            float pulse = Mathf.Sin(_timer * _pulseSpeed) * 0.5f + 0.5f; // 0 to 1
            float intensity = Mathf.Lerp(_offIntensity, _onIntensity, pulse);

            foreach (Light light in _lights)
            {
                light.intensity = intensity;
            }
        }

        /// <summary>
        /// Start the flashing sequence
        /// </summary>
        public void StartFlashing()
        {
            if (_lights == null || _lights.Length == 0)
            {
                SetupLights();
            }

            _isFlashing = true;
            _timer = 0f;
            Debug.Log($"[LandingLightController] Started flashing with pattern: {_flashPattern}");
        }

        /// <summary>
        /// Stop the flashing sequence
        /// </summary>
        public void StopFlashing()
        {
            _isFlashing = false;

            // Turn all lights off
            if (_lights != null)
            {
                foreach (Light light in _lights)
                {
                    light.intensity = _offIntensity;
                }
            }

            Debug.Log("[LandingLightController] Stopped flashing");
        }

        /// <summary>
        /// Change the flash pattern at runtime
        /// </summary>
        public void SetFlashPattern(FlashPattern newPattern)
        {
            _flashPattern = newPattern;
            _timer = 0f;
            Debug.Log($"[LandingLightController] Changed to pattern: {newPattern}");
        }

        /// <summary>
        /// Change the flash speed
        /// </summary>
        public void SetFlashInterval(float interval)
        {
            _flashInterval = Mathf.Max(0.1f, interval);
        }

        /// <summary>
        /// Draw debug visualization in Scene view
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Light[] lights = GetComponentsInChildren<Light>();

            for (int i = 0; i < lights.Length; i++)
            {
                Gizmos.color = _flashColor;
                Gizmos.DrawWireSphere(lights[i].transform.position, 0.3f);

                // Draw line connecting lights in sequence
                if (i < lights.Length - 1)
                {
                    Gizmos.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, 0.3f);
                    Gizmos.DrawLine(lights[i].transform.position, lights[i + 1].transform.position);
                }
            }
        }
    }
}
