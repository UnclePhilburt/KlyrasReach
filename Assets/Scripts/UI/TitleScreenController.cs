/*
 * Title Screen Controller for Klyra's Reach
 *
 * PURPOSE:
 * Waits for the player to press any key, then loads the main menu scene.
 * Used on the Synty Title_03 demo scene (07_Demo_SciFiMenus_Screen_Title_03).
 *
 * HOW TO USE:
 * 1. Open the Synty Title_03 demo scene
 * 2. Create an empty GameObject named "TitleScreenController"
 * 3. Add this script to it
 * 4. Set the Main Menu Scene Name to match your main menu scene
 *    (default: "01_Demo_SciFiMenus_Screen_MainMenu_01")
 * 5. Make sure both scenes are in Build Settings
 *    - Title scene at index 0 (loads first)
 *    - Main menu scene at index 1
 */

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace KlyrasReach.UI
{
    /// <summary>
    /// Simple controller for the title screen. Waits for any key press,
    /// then loads the main menu scene. Uses the new Input System.
    /// </summary>
    public class TitleScreenController : MonoBehaviour
    {
        [Header("Scene Settings")]
        [Tooltip("The name of the main menu scene to load when the player presses any key")]
        [SerializeField] private string _mainMenuSceneName = "01_Demo_SciFiMenus_Screen_MainMenu_01";

        // Prevents loading the scene multiple times if the player mashes keys
        private bool _isTransitioning = false;

        /// <summary>
        /// Called once on start. Shows the cursor in case it was hidden from gameplay.
        /// </summary>
        private void Start()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        /// <summary>
        /// Called every frame. Checks if the player pressed any key/button/click,
        /// then loads the main menu scene.
        /// </summary>
        private void Update()
        {
            // Don't check if we're already loading the next scene
            if (_isTransitioning) return;

            // Check if any key, mouse button, or gamepad button was pressed this frame
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                LoadMainMenu();
                return;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                LoadMainMenu();
                return;
            }

            if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            {
                LoadMainMenu();
                return;
            }
        }

        /// <summary>
        /// Loads the main menu scene. Only runs once thanks to the _isTransitioning flag.
        /// </summary>
        private void LoadMainMenu()
        {
            _isTransitioning = true;
            Debug.Log("[TitleScreenController] Input detected — loading main menu...");
            SceneManager.LoadScene(_mainMenuSceneName);
        }
    }
}
