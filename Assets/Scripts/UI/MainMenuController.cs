/*
 * Main Menu Controller for Klyra's Reach
 *
 * PURPOSE:
 * Hooks into the existing Synty SciFi Main Menu buttons.
 * Sits on top of the Synty demo scene (01_Demo_SciFiMenus_Screen_MainMenu_01).
 *
 * HOW TO USE:
 * 1. Open the Synty MainMenu_01 demo scene
 * 2. Create an empty GameObject, add this script
 * 3. Drag the existing buttons (Button_NewGame, Button_Settings, Button_Quit)
 *    into the Inspector slots
 * 4. Set the Loading Scene Name to your loading scene
 *
 * FLOW:
 * New Game -> loads the Loading scene (which handles Photon connection)
 * Settings -> loads the Synty Settings scene (with Back button to return)
 * Quit     -> exits the application
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace KlyrasReach.UI
{
    /// <summary>
    /// Hooks into the Synty MainMenu_01 buttons.
    /// New Game loads the loading scene, Settings is a placeholder, Quit exits.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        // =====================================================================
        // INSPECTOR FIELDS - Drag the existing Synty buttons into these slots
        // =====================================================================

        [Header("Existing Synty Buttons (from Button_Group)")]
        [Tooltip("Button_NewGame — loads the loading scene to connect to Photon")]
        [SerializeField] private Button _newGameButton;

        [Tooltip("Button_Settings — opens the Synty Settings screen")]
        [SerializeField] private Button _settingsButton;

        [Tooltip("Button_Quit — exits the application")]
        [SerializeField] private Button _quitButton;

        [Header("Scene Settings")]
        [Tooltip("Name of the loading scene to load when New Game is clicked")]
        [SerializeField] private string _loadingSceneName = "14_Demo_SciFiMenus_Screen_Loading_01";

        [Tooltip("Name of the Synty Settings scene to load when Settings is clicked")]
        [SerializeField] private string _settingsSceneName = "18_Demo_SciFiMenus_Screen_Settings_01";

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        /// <summary>
        /// Called once when the script starts.
        /// Shows the cursor and hooks up button click listeners.
        /// </summary>
        private void Start()
        {
            // --- Show the cursor (it may be hidden/locked from the game scene) ---
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // --- Hook up button click listeners ---
            if (_newGameButton != null)
            {
                _newGameButton.onClick.AddListener(OnNewGameClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.AddListener(OnQuitClicked);
            }
        }

        /// <summary>
        /// Cleans up button listeners when destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (_newGameButton != null)
            {
                _newGameButton.onClick.RemoveListener(OnNewGameClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveListener(OnQuitClicked);
            }
        }

        // =====================================================================
        // BUTTON HANDLERS
        // =====================================================================

        /// <summary>
        /// Called when Button_NewGame is clicked.
        /// Loads the loading scene, which handles connecting to Photon.
        /// </summary>
        private void OnNewGameClicked()
        {
            Debug.Log("[MainMenuController] New Game clicked — loading connecting screen...");
            SceneManager.LoadScene(_loadingSceneName);
        }

        /// <summary>
        /// Called when Button_Settings is clicked.
        /// Loads the Synty Settings scene. SettingsMenuController handles the Back button.
        /// </summary>
        private void OnSettingsClicked()
        {
            Debug.Log("[MainMenuController] Settings clicked — loading settings screen...");
            SceneManager.LoadScene(_settingsSceneName);
        }

        /// <summary>
        /// Called when Button_Quit is clicked.
        /// Exits the application. In the Unity Editor, stops play mode instead.
        /// </summary>
        private void OnQuitClicked()
        {
            Debug.Log("[MainMenuController] Quit clicked — exiting application");

            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
