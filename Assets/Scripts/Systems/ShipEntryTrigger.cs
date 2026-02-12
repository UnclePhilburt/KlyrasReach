/*
 * Ship Entry Trigger for Klyra's Reach
 *
 * PURPOSE:
 * Allows player to board their ship and return to space/flight scene.
 * This is the reverse of landing - takes player from ground back to ship.
 *
 * HOW TO USE:
 * 1. Add this script to your landed ship GameObject in the ground scene
 * 2. Set the "Flight Scene Name" to the name of your space/flight scene
 * 3. Set the boarding range (how close player needs to be)
 * 4. When player walks up and presses F, they'll be loaded into the ship in flight scene
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Handles boarding ship and transitioning back to flight scene
    /// </summary>
    public class ShipEntryTrigger : MonoBehaviour
    {
        [Header("Boarding Settings")]
        [Tooltip("How close player needs to be to board ship (in units)")]
        [SerializeField] private float _boardingRange = 5f;

        [Tooltip("Name of the flight/space scene to load when boarding")]
        [SerializeField] private string _flightSceneName = "SpaceScene";

        [Tooltip("How long the fade transition takes (seconds)")]
        [SerializeField] private float _fadeDuration = 1f;

        [Header("Detection")]
        [Tooltip("Tag to identify the player")]
        [SerializeField] private string _playerTag = "Player";

        // Private variables
        private bool _playerInRange = false;
        private GameObject _nearbyPlayer = null;
        private bool _isBoarding = false;
        private float _fadeAlpha = 0f;

        /// <summary>
        /// Check for nearby player every frame
        /// </summary>
        private void Update()
        {
            if (_isBoarding) return; // Don't check while boarding is in progress

            // Find nearby player
            FindNearbyPlayer();

            // Check for boarding input
            if (_playerInRange && _nearbyPlayer != null)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
                {
                    Debug.Log($"[ShipEntry] F pressed - initiating boarding at distance: {Vector3.Distance(transform.position, _nearbyPlayer.transform.position)}");
                    InitiateBoarding();
                }
            }
            else if (_playerInRange)
            {
                Debug.Log("[ShipEntry] Player in range but nearby player is null!");
            }
        }

        /// <summary>
        /// Finds player within boarding range
        /// </summary>
        private void FindNearbyPlayer()
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag(_playerTag);

            _playerInRange = false;
            _nearbyPlayer = null;

            foreach (GameObject player in players)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);

                if (distance <= _boardingRange)
                {
                    _playerInRange = true;
                    _nearbyPlayer = player;
                    return;
                }
            }
        }

        /// <summary>
        /// Starts the boarding sequence
        /// </summary>
        private void InitiateBoarding()
        {
            _isBoarding = true;
            Debug.Log("[ShipEntry] Initiating boarding sequence...");

            // Save that we're boarding so the flight scene knows to put player in ship
            PlayerPrefs.SetInt("IsBoardingShip", 1);
            PlayerPrefs.Save();
            Debug.Log($"[ShipEntry] Set IsBoardingShip flag to 1, will load scene: {_flightSceneName}");

            // Start fade and scene transition
            StartCoroutine(BoardingSequence());
        }

        /// <summary>
        /// Handles the boarding animation and scene transition
        /// </summary>
        private System.Collections.IEnumerator BoardingSequence()
        {
            // Disable player movement if needed
            // TODO: Add player controller disable here if you have one

            // Fade to black
            yield return StartCoroutine(FadeToBlack());

            // Load flight scene
            SceneManager.LoadScene(_flightSceneName);
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
                _fadeAlpha = Mathf.Clamp01(elapsed / _fadeDuration);
                yield return null;
            }

            _fadeAlpha = 1f;
        }

        /// <summary>
        /// Draws UI for boarding prompt and fade effect
        /// </summary>
        private void OnGUI()
        {
            // Show boarding prompt if player is in range and not boarding yet
            if (_playerInRange && !_isBoarding)
            {
                GUI.skin.label.fontSize = 20;
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.color = Color.cyan;

                Rect promptRect = new Rect(
                    Screen.width / 2 - 150,
                    Screen.height - 100,
                    300,
                    40
                );

                GUI.Label(promptRect, "Press F to Board Ship");
                GUI.color = Color.white;
            }

            // Draw fade to black overlay
            if (_isBoarding || _fadeAlpha > 0)
            {
                GUI.color = new Color(0, 0, 0, _fadeAlpha);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }

        /// <summary>
        /// Draw debug visualization in Scene view
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw boarding range
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _boardingRange);

            // Draw ship marker
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(transform.position, new Vector3(10f, 5f, 15f));
        }
    }
}
