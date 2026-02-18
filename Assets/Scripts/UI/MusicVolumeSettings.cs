/*
 * Music Volume Settings for Klyra's Reach
 *
 * PURPOSE:
 * Connects the music slider in the Settings menu to the actual game audio.
 * Saves the player's volume preference using PlayerPrefs so it persists
 * across scenes and game restarts.
 *
 * HOW TO USE:
 * 1. Open the Settings scene (18_Demo_SciFiMenus_Screen_Settings_01)
 * 2. Find the music slider GameObject in the Sound section
 * 3. Add this script to that GameObject (the one with SampleSettingsSlider)
 * 4. That's it — the script auto-finds the Slider in children
 */

using UnityEngine;
using UnityEngine.UI;

namespace KlyrasReach.UI
{
    /// <summary>
    /// Bridges the Settings menu music slider to PlayerPrefs and live audio.
    /// Reads the saved volume on start and updates it whenever the slider moves.
    /// </summary>
    public class MusicVolumeSettings : MonoBehaviour
    {
        // =====================================================================
        // INSPECTOR FIELDS
        // =====================================================================

        [Header("UI Reference (optional — auto-finds if left empty)")]
        [Tooltip("The music volume slider. Leave empty to auto-find in children.")]
        [SerializeField] private Slider _musicSlider;

        // =====================================================================
        // CONSTANTS
        // =====================================================================

        // The PlayerPrefs key where we store the music volume (0 to 1)
        private const string VOLUME_PREF_KEY = "MusicVolume";

        // Default volume if no saved preference exists (50%)
        private const float DEFAULT_VOLUME = 0.5f;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        /// <summary>
        /// Called once when the script starts.
        /// Loads the saved volume, sets the slider to match, and
        /// starts listening for slider changes.
        /// </summary>
        private void Start()
        {
            // Auto-find the Slider if none was assigned in the Inspector.
            // The Synty settings prefab has the Slider on a child object,
            // so GetComponentInChildren finds it automatically.
            if (_musicSlider == null)
            {
                _musicSlider = GetComponentInChildren<Slider>();
            }

            // If we still don't have one, something is wrong
            if (_musicSlider == null)
            {
                Debug.LogError("[MusicVolumeSettings] No Slider found! Make sure this script is on the settings slider GameObject.");
                return;
            }

            // Load the saved volume from PlayerPrefs (or use default if first time)
            float savedVolume = PlayerPrefs.GetFloat(VOLUME_PREF_KEY, DEFAULT_VOLUME);

            // Set the slider to show the saved volume
            // Works whether the slider range is 0-1 or 0-100
            if (_musicSlider.maxValue > 1f)
            {
                // Slider uses 0-100 range — scale up from our 0-1 saved value
                _musicSlider.value = savedVolume * _musicSlider.maxValue;
            }
            else
            {
                // Slider uses 0-1 range — use the saved value directly
                _musicSlider.value = savedVolume;
            }

            // Listen for slider changes — whenever the player moves the slider,
            // OnMusicSliderChanged will be called
            _musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        }

        /// <summary>
        /// Clean up the listener when this object is destroyed
        /// </summary>
        private void OnDestroy()
        {
            if (_musicSlider != null)
            {
                _musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
            }
        }

        // =====================================================================
        // SLIDER CALLBACK
        // =====================================================================

        /// <summary>
        /// Called every time the player moves the music slider.
        /// Converts the slider value to a 0-1 range, saves it, and
        /// updates any currently playing music in real time.
        /// </summary>
        /// <param name="sliderValue">The raw value from the slider</param>
        private void OnMusicSliderChanged(float sliderValue)
        {
            // Convert to 0-1 range (handles both 0-100 and 0-1 sliders)
            float normalizedVolume;
            if (_musicSlider.maxValue > 1f)
            {
                // Slider is 0-100 — normalize to 0-1
                normalizedVolume = sliderValue / _musicSlider.maxValue;
            }
            else
            {
                // Slider is already 0-1
                normalizedVolume = sliderValue;
            }

            // Save the volume so it persists across scenes and game restarts
            PlayerPrefs.SetFloat(VOLUME_PREF_KEY, normalizedVolume);
            PlayerPrefs.Save();

            // Update the menu music volume in real time (if menu music is playing)
            // This lets the player hear the change immediately while adjusting
            MenuMusicController.SetMusicVolume(normalizedVolume);
        }
    }
}
