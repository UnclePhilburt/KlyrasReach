/*
 * Landing Zone Trigger for Klyra's Reach
 *
 * PURPOSE:
 * Detects when player's ship enters landing zone and allows them to land.
 * Landing triggers scene transition to ground-level version.
 *
 * HOW TO USE:
 * 1. Add this script to your landing pad GameObject (same one with LandingZoneMarker)
 * 2. Configure the trigger range and target scene
 * 3. When player flies close and presses F, scene transitions
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Handles landing zone interaction and scene transitions
    /// </summary>
    public class LandingZoneTrigger : MonoBehaviour
    {
        [Header("Landing Settings")]
        [Tooltip("Unique ID for this landing pad (must match ShipSpawnPoint ID)")]
        [SerializeField] private string _landingPadID = "LandingPad1";

        [Tooltip("How close ship needs to be to land (in units)")]
        [SerializeField] private float _landingRange = 50f;

        [Tooltip("Name of scene to load when landing (leave empty to reload current scene)")]
        [SerializeField] private string _targetSceneName = "";

        [Tooltip("How long the fade to black takes (seconds)")]
        [SerializeField] private float _fadeDuration = 1f;

        [Header("Detection")]
        [Tooltip("Tag to identify ships")]
        [SerializeField] private string _shipTag = "Ship";

        // Private variables
        private bool _shipInRange = false;
        private GameObject _nearbyShip = null;
        private Player.ShipController _shipController = null;
        private bool _isLanding = false;

        // Singleton reference so ShipEntryPoint can check if at landing zone
        private static LandingZoneTrigger _currentLandingZone = null;
        public static bool IsPlayerAtLandingZone => _currentLandingZone != null;
        public static bool IsPlayerInLandingRange => _currentLandingZone != null && _currentLandingZone._shipInRange;

        /// <summary>
        /// Check for nearby ships every frame
        /// </summary>
        private void Update()
        {
            if (_isLanding) return; // Don't check while landing is in progress

            // Find active ship
            FindNearbyShip();

            // Check for landing input - ONLY if ship is in range AND being piloted
            if (_shipInRange && _nearbyShip != null && _shipController != null && _shipController.IsActive)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
                {
                    Debug.Log($"[LandingZone] F pressed at distance: {Vector3.Distance(transform.position, _nearbyShip.transform.position)}");
                    InitiateLanding();
                }
            }
        }

        /// <summary>
        /// Finds ships within landing range
        /// </summary>
        private void FindNearbyShip()
        {
            GameObject[] ships = GameObject.FindGameObjectsWithTag(_shipTag);

            _shipInRange = false;
            _nearbyShip = null;
            _shipController = null;
            _currentLandingZone = null;

            foreach (GameObject ship in ships)
            {
                float distance = Vector3.Distance(transform.position, ship.transform.position);

                if (distance <= _landingRange)
                {
                    // Check if ship is being piloted
                    var controller = ship.GetComponent<Player.ShipController>();
                    if (controller != null && controller.IsActive)
                    {
                        _shipInRange = true;
                        _nearbyShip = ship;
                        _shipController = controller;
                        _currentLandingZone = this;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Starts the landing sequence
        /// </summary>
        private void InitiateLanding()
        {
            _isLanding = true;
            Debug.Log($"[LandingZone] Initiating landing sequence at pad: {_landingPadID}");

            // Save which landing pad we're landing at
            PlayerPrefs.SetString("LastLandingPadID", _landingPadID);
            PlayerPrefs.Save();

            // Start fade and scene transition
            StartCoroutine(LandingSequence());
        }

        /// <summary>
        /// Handles the landing animation and scene transition
        /// </summary>
        private System.Collections.IEnumerator LandingSequence()
        {
            // TODO: Stop ship movement here if needed
            // _shipController.DisableControls();

            // Fade to black
            yield return StartCoroutine(FadeToBlack());

            // Reload scene (or load target scene)
            if (string.IsNullOrEmpty(_targetSceneName))
            {
                // Reload current scene
                Scene currentScene = SceneManager.GetActiveScene();
                SceneManager.LoadScene(currentScene.name);
            }
            else
            {
                // Load specified scene
                SceneManager.LoadScene(_targetSceneName);
            }
        }

        /// <summary>
        /// Fades screen to black
        /// </summary>
        private System.Collections.IEnumerator FadeToBlack()
        {
            float elapsed = 0f;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                // Fade effect happens in OnGUI
                yield return null;
            }
        }

        /// <summary>
        /// Draws UI for landing prompt and fade effect
        /// </summary>
        private void OnGUI()
        {
            // Show landing prompt ONLY if in range, not landing, AND actively piloting the ship
            if (_shipInRange && !_isLanding && _shipController != null && _shipController.IsActive)
            {
                GUI.skin.label.fontSize = 20;
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.color = Color.green;

                Rect promptRect = new Rect(
                    Screen.width / 2 - 150,
                    Screen.height - 100,
                    300,
                    40
                );

                GUI.Label(promptRect, "Press F to Land");
                GUI.color = Color.white;
            }

            // Draw fade to black overlay if landing
            if (_isLanding)
            {
                GUI.color = new Color(0, 0, 0, 1f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }

        /// <summary>
        /// Draw debug visualization
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw landing range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _landingRange);

            // Draw landing pad
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(10f, 0.5f, 10f));
        }
    }
}
