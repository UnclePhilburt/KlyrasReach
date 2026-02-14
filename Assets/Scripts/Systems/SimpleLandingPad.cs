/*
 * Simple Landing Pad - Clean, minimal implementation
 *
 * All this does:
 * 1. Detect ship in range
 * 2. When F is pressed, exit ship
 * 3. Move player to exit point
 * 4. Park the ship
 *
 * That's it. No spawning, no complex logic.
 */

using UnityEngine;
using UnityEngine.InputSystem;

namespace KlyrasReach.Systems
{
    public class SimpleLandingPad : MonoBehaviour
    {
        [Header("Landing Settings")]
        [SerializeField] private float _landingRange = 50f;
        [SerializeField] private Transform _playerExitPoint;
        [SerializeField] private Vector3 _shipParkPosition = new Vector3(0, 2, 0);
        [SerializeField] private float _landingDuration = 2f;
        [SerializeField] private string _shipTag = "Ship";

        private bool _isLanding = false;
        private static bool _anyLandingInProgress = false;

        private void Update()
        {
            // PERFORMANCE: Only check every 30 frames (0.5 seconds at 60FPS)
            // FindGameObjectsWithTag is EXTREMELY expensive!
            if (Time.frameCount % 30 != 0)
            {
                return;
            }

            if (_isLanding || _anyLandingInProgress) return;

            // Find nearby ship
            GameObject[] ships = GameObject.FindGameObjectsWithTag(_shipTag);
            foreach (GameObject ship in ships)
            {
                float distance = Vector3.Distance(transform.position, ship.transform.position);
                if (distance > _landingRange) continue;

                var controller = ship.GetComponent<Player.ShipController>();
                if (controller == null || !controller.IsActive) continue;

                // Ship in range and active - check for F key
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
                {
                    // Set flag BEFORE starting coroutine to prevent PlayerSpawnPoint from spawning
                    PlayerPrefs.SetInt("IsBoardingShip", 1);
                    PlayerPrefs.Save();

                    StartCoroutine(LandShip(ship, controller));
                    return;
                }
            }
        }

        private System.Collections.IEnumerator LandShip(GameObject ship, Player.ShipController controller)
        {
            _isLanding = true;
            _anyLandingInProgress = true;

            Debug.Log($"[SimpleLandingPad] ===== LANDING AT: {gameObject.name} =====");
            Debug.Log($"[SimpleLandingPad] This landing pad is at position: {transform.position}");

            // 1. Exit the ship
            controller.ExitShip();

            // 2. Stop ship physics
            Rigidbody shipRb = ship.GetComponent<Rigidbody>();
            if (shipRb != null)
            {
                shipRb.isKinematic = true;
                shipRb.linearVelocity = Vector3.zero;
                shipRb.angularVelocity = Vector3.zero;
            }

            // 3. Reset ship entry point
            var shipEntry = ship.GetComponent<Player.ShipEntryPoint>();
            if (shipEntry != null)
            {
                shipEntry.ResetPilotingState();
                shipEntry.enabled = true;
            }

            // 4. Find the player (should exist)
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("[SimpleLandingPad] No player found!");
                _isLanding = false;
                _anyLandingInProgress = false;
                yield break;
            }

            // 5. Calculate exit position
            Vector3 exitPos;
            Quaternion exitRot;

            if (_playerExitPoint != null)
            {
                exitPos = _playerExitPoint.position;
                exitRot = _playerExitPoint.rotation;
                Debug.Log($"[SimpleLandingPad] Using assigned exit point: {_playerExitPoint.name} at position {exitPos}");
            }
            else
            {
                exitPos = transform.position + new Vector3(0, 1, 5);
                exitRot = transform.rotation;
                Debug.LogWarning($"[SimpleLandingPad] No exit point assigned! Using default offset: {exitPos}");
            }

            // 6. Move player to exit point
            var charController = player.GetComponent<CharacterController>();
            var locomotion = player.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();

            if (charController != null) charController.enabled = false;
            if (locomotion != null) locomotion.enabled = false;

            player.transform.position = exitPos;
            player.transform.rotation = exitRot;

            Debug.Log($"[SimpleLandingPad] SET player position to {exitPos}");

            // Make sure player is visible
            Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.enabled = true;
            }

            // KEEP COMPONENTS DISABLED until after camera setup
            Debug.Log($"[SimpleLandingPad] Before camera setup, player at: {player.transform.position}");

            // 7. Setup camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var camController = mainCam.GetComponent<Opsive.UltimateCharacterController.Camera.CameraController>();
                if (camController != null)
                {
                    camController.enabled = false;
                    Debug.Log($"[SimpleLandingPad] After disabling camera, player at: {player.transform.position}");

                    camController.Character = player;
                    Debug.Log($"[SimpleLandingPad] After setting Character, player at: {player.transform.position}");

                    mainCam.transform.position = player.transform.position + new Vector3(0, 2, -5);
                    Debug.Log($"[SimpleLandingPad] After moving camera, player at: {player.transform.position}");

                    camController.enabled = true;
                    Debug.Log($"[SimpleLandingPad] After enabling camera, player at: {player.transform.position}");
                }
            }

            // KEEP CHARACTER COMPONENTS DISABLED during ship parking

            // 8. Park the ship
            Debug.Log($"[SimpleLandingPad] Before parking ship, player at: {player.transform.position}");

            Vector3 parkPos = transform.position + transform.TransformDirection(_shipParkPosition);
            float elapsed = 0f;
            Vector3 startPos = ship.transform.position;

            while (elapsed < _landingDuration)
            {
                elapsed += Time.deltaTime;
                ship.transform.position = Vector3.Lerp(startPos, parkPos, elapsed / _landingDuration);
                yield return null;
            }

            ship.transform.position = parkPos;
            Debug.Log($"[SimpleLandingPad] After parking ship, player at: {player.transform.position}");

            // NOW re-enable character components at the VERY END
            if (charController != null) charController.enabled = true;
            if (locomotion != null) locomotion.enabled = true;
            Debug.Log($"[SimpleLandingPad] After re-enabling character components, player at: {player.transform.position}");

            // Final verification - where is the player actually?
            Debug.Log($"[SimpleLandingPad] Landing complete - Player final position: {player.transform.position}");
            Debug.Log($"[SimpleLandingPad] Expected position was: {exitPos}");

            // Clear the flag so player can spawn normally next time
            PlayerPrefs.SetInt("IsBoardingShip", 0);
            PlayerPrefs.Save();

            _isLanding = false;
            _anyLandingInProgress = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _landingRange);

            if (_playerExitPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_playerExitPoint.position, 1f);
                Gizmos.DrawLine(transform.position, _playerExitPoint.position);
            }
        }
    }
}
