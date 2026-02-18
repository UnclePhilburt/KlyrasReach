/*
 * Look Sensitivity Settings for Klyra's Reach
 *
 * PURPOSE:
 * Connects the look sensitivity slider in the Settings menu to the actual
 * mouse sensitivity used for both on-foot (Opsive PlayerInput) and ship
 * (ShipController) controls. Saves the player's preference using PlayerPrefs
 * so it persists across scenes and game restarts.
 *
 * The slider value (0-1) is mapped to a multiplier range:
 *   0.0 → 0.25x  (very slow)
 *   0.5 → 1.0x   (default / normal)
 *   1.0 → 3.0x   (very fast)
 *
 * HOW TO USE:
 * 1. Open the Settings scene (18_Demo_SciFiMenus_Screen_Settings_01)
 * 2. Find the "vibration intensity" slider in the Gameplay tab
 * 3. Add this script to that slider GameObject
 * 4. That's it — the script auto-finds the Slider in children
 */

using UnityEngine;
using UnityEngine.UI;
using KlyrasReach.Player;

namespace KlyrasReach.UI
{
    /// <summary>
    /// Bridges the Settings menu look sensitivity slider to PlayerPrefs and
    /// live sensitivity on both Opsive PlayerInput and ShipController.
    /// Reads the saved value on start and updates it whenever the slider moves.
    /// </summary>
    public class LookSensitivitySettings : MonoBehaviour
    {
        // =====================================================================
        // INSPECTOR FIELDS
        // =====================================================================

        [Header("UI Reference (optional — auto-finds if left empty)")]
        [Tooltip("The sensitivity slider. Leave empty to auto-find in children.")]
        [SerializeField] private Slider _sensitivitySlider;

        // =====================================================================
        // CONSTANTS
        // =====================================================================

        // The PlayerPrefs key where we store the sensitivity (0 to 1)
        private const string SENSITIVITY_PREF_KEY = "LookSensitivity";

        // Default sensitivity if no saved preference exists (0.5 = 1x multiplier)
        private const float DEFAULT_SENSITIVITY = 0.5f;

        // Multiplier range — slider 0 maps to MIN, slider 1 maps to MAX
        // At the default of 0.5, the multiplier works out to ~1.0x
        private const float MIN_MULTIPLIER = 0.25f;
        private const float MAX_MULTIPLIER = 3.0f;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        /// <summary>
        /// Called once when the script starts.
        /// Loads the saved sensitivity, sets the slider to match, and
        /// starts listening for slider changes.
        /// </summary>
        private void Start()
        {
            // Auto-find the Slider if none was assigned in the Inspector.
            // The Synty settings prefab has the Slider on a child object,
            // so GetComponentInChildren finds it automatically.
            if (_sensitivitySlider == null)
            {
                _sensitivitySlider = GetComponentInChildren<Slider>();
            }

            // If we still don't have one, something is wrong
            if (_sensitivitySlider == null)
            {
                Debug.LogError("[LookSensitivitySettings] No Slider found! Make sure this script is on the settings slider GameObject.");
                return;
            }

            // Load the saved sensitivity from PlayerPrefs (or use default if first time)
            float savedSensitivity = PlayerPrefs.GetFloat(SENSITIVITY_PREF_KEY, DEFAULT_SENSITIVITY);

            // Set the slider to show the saved sensitivity
            // Works whether the slider range is 0-1 or 0-100
            if (_sensitivitySlider.maxValue > 1f)
            {
                // Slider uses 0-100 range — scale up from our 0-1 saved value
                _sensitivitySlider.value = savedSensitivity * _sensitivitySlider.maxValue;
            }
            else
            {
                // Slider uses 0-1 range — use the saved value directly
                _sensitivitySlider.value = savedSensitivity;
            }

            // Apply the saved sensitivity right away so any active player/ship
            // picks it up when the settings scene loads
            ApplySensitivity(savedSensitivity);

            // Listen for slider changes — whenever the player moves the slider,
            // OnSensitivitySliderChanged will be called
            _sensitivitySlider.onValueChanged.AddListener(OnSensitivitySliderChanged);
        }

        /// <summary>
        /// Clean up the listener when this object is destroyed
        /// </summary>
        private void OnDestroy()
        {
            if (_sensitivitySlider != null)
            {
                _sensitivitySlider.onValueChanged.RemoveListener(OnSensitivitySliderChanged);
            }
        }

        // =====================================================================
        // SLIDER CALLBACK
        // =====================================================================

        /// <summary>
        /// Called every time the player moves the sensitivity slider.
        /// Converts the slider value to a 0-1 range, saves it, and
        /// updates any active player/ship sensitivity in real time.
        /// </summary>
        /// <param name="sliderValue">The raw value from the slider</param>
        private void OnSensitivitySliderChanged(float sliderValue)
        {
            // Convert to 0-1 range (handles both 0-100 and 0-1 sliders)
            float normalizedValue;
            if (_sensitivitySlider.maxValue > 1f)
            {
                // Slider is 0-100 — normalize to 0-1
                normalizedValue = sliderValue / _sensitivitySlider.maxValue;
            }
            else
            {
                // Slider is already 0-1
                normalizedValue = sliderValue;
            }

            // Save the sensitivity so it persists across scenes and game restarts
            PlayerPrefs.SetFloat(SENSITIVITY_PREF_KEY, normalizedValue);
            PlayerPrefs.Save();

            // Apply to any active player/ship right now so the change is felt immediately
            ApplySensitivity(normalizedValue);
        }

        // =====================================================================
        // SENSITIVITY APPLICATION
        // =====================================================================

        /// <summary>
        /// Converts a normalized 0-1 value into a multiplier and applies it
        /// to both the Opsive PlayerInput (on-foot) and ShipController (ship).
        /// </summary>
        /// <param name="normalizedValue">Slider value in 0-1 range</param>
        private void ApplySensitivity(float normalizedValue)
        {
            // Convert 0-1 slider value to a usable multiplier
            // Lerp from MIN_MULTIPLIER (0.25) to MAX_MULTIPLIER (3.0)
            // At 0.5 (default), this gives ~1.625 — close to 1x
            // Using a curve so 0.5 maps more closely to 1.0:
            //   Below 0.5: lerp 0.25 → 1.0
            //   Above 0.5: lerp 1.0 → 3.0
            float multiplier;
            if (normalizedValue <= 0.5f)
            {
                // Map 0-0.5 to MIN_MULTIPLIER-1.0
                float t = normalizedValue / 0.5f; // remap to 0-1
                multiplier = Mathf.Lerp(MIN_MULTIPLIER, 1.0f, t);
            }
            else
            {
                // Map 0.5-1.0 to 1.0-MAX_MULTIPLIER
                float t = (normalizedValue - 0.5f) / 0.5f; // remap to 0-1
                multiplier = Mathf.Lerp(1.0f, MAX_MULTIPLIER, t);
            }

            Debug.Log($"[LookSensitivitySettings] Slider: {normalizedValue:F2} → Multiplier: {multiplier:F2}x");

            // --- Apply to Opsive PlayerInput (on-foot character) ---
            // The actual PlayerInput gets reparented away from the player by Opsive,
            // so we go through the PlayerInputProxy which stays on the character.
            var inputProxy = FindAnyObjectByType<Opsive.Shared.Input.PlayerInputProxy>();
            if (inputProxy != null)
            {
                inputProxy.LookSensitivityMultiplier = multiplier;
                Debug.Log($"[LookSensitivitySettings] Set Opsive PlayerInput sensitivity to {multiplier:F2}x");
            }

            // --- Apply to ShipController (ship flight) ---
            ShipController.SetLookSensitivity(normalizedValue);
        }

        // =====================================================================
        // STATIC HELPER
        // =====================================================================

        /// <summary>
        /// Converts a normalized 0-1 sensitivity value to a multiplier.
        /// Can be called from other scripts that need to read the saved sensitivity.
        /// Uses a two-segment curve so 0.5 maps to exactly 1.0x.
        /// </summary>
        /// <param name="normalizedValue">Value from 0 to 1</param>
        /// <returns>Multiplier from 0.25 to 3.0</returns>
        public static float NormalizedToMultiplier(float normalizedValue)
        {
            if (normalizedValue <= 0.5f)
            {
                float t = normalizedValue / 0.5f;
                return Mathf.Lerp(MIN_MULTIPLIER, 1.0f, t);
            }
            else
            {
                float t = (normalizedValue - 0.5f) / 0.5f;
                return Mathf.Lerp(1.0f, MAX_MULTIPLIER, t);
            }
        }
    }
}
