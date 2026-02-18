/*
 * Settings Menu Controller for Klyra's Reach
 *
 * PURPOSE:
 * Handles the Back button on the Synty Settings screen so the player
 * can return to the main menu. Sits on top of the Synty demo scene
 * (18_Demo_SciFiMenus_Screen_Settings_01).
 *
 * HOW TO USE:
 * 1. Open the Synty Settings demo scene (18_Demo_SciFiMenus_Screen_Settings_01)
 * 2. Create an empty GameObject called "SettingsMenuController"
 * 3. Attach this script to it
 * 4. Find the Back/Close button in the Synty hierarchy and drag it into
 *    the _backButton slot in the Inspector
 * 5. Make sure the main menu scene name matches your KlyraMainMenu scene
 * 6. Add this scene to Build Settings (File > Build Settings > Add Open Scenes)
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace KlyrasReach.UI
{
    /// <summary>
    /// Handles the Back button on the Synty Settings screen.
    /// Returns to the main menu when clicked.
    /// </summary>
    public class SettingsMenuController : MonoBehaviour
    {
        // =====================================================================
        // INSPECTOR FIELDS
        // =====================================================================

        [Header("Buttons")]
        [Tooltip("The Back/Close button in the Synty Settings scene. Drag it here from the hierarchy.")]
        [SerializeField] private Button _backButton;

        [Header("Scene Settings")]
        [Tooltip("Name of the main menu scene to return to when Back is clicked")]
        [SerializeField] private string _mainMenuSceneName = "KlyraMainMenu";

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        /// <summary>
        /// Hook up the Back button on start.
        /// </summary>
        private void Start()
        {
            // Make sure cursor is visible on the settings screen
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (_backButton != null)
            {
                _backButton.onClick.AddListener(OnBackClicked);
            }
            else
            {
                Debug.LogWarning("[SettingsMenuController] No Back button assigned! " +
                    "Find the Back/Close button in the Synty hierarchy and drag it into the Inspector slot.");
            }
        }

        /// <summary>
        /// Clean up button listener when destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(OnBackClicked);
            }
        }

        // =====================================================================
        // BUTTON HANDLER
        // =====================================================================

        /// <summary>
        /// Called when the Back button is clicked.
        /// Returns to the main menu scene.
        /// </summary>
        private void OnBackClicked()
        {
            Debug.Log("[SettingsMenuController] Back clicked — returning to main menu...");
            SceneManager.LoadScene(_mainMenuSceneName);
        }
    }
}
