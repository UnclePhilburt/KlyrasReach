/*
 * Outpost 47 Landing Pad - Completely separate from Space Port
 *
 * PURPOSE:
 * Dedicated landing pad script for Outpost 47 only.
 * No shared code or variables with other landing pads.
 */

using UnityEngine;
using UnityEngine.InputSystem;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Landing pad specifically for Outpost 47
    /// </summary>
    public class Outpost47LandingPad : MonoBehaviour
    {
        [Header("Outpost 47 Landing Settings")]
        [Tooltip("How close ship needs to be to land (in units)")]
        [SerializeField] private float _landingRange = 50f;

        [Tooltip("Transform for player exit position at Outpost 47")]
        [SerializeField] private Transform _outpost47ExitPoint;

        [Tooltip("Where the ship parks at Outpost 47")]
        [SerializeField] private Vector3 _shipParkPosition = new Vector3(0, 2, 0);

        [Tooltip("How long the landing animation takes")]
        [SerializeField] private float _landingDuration = 2f;

        [Header("Detection")]
        [Tooltip("Tag to identify ships")]
        [SerializeField] private string _shipTag = "Ship";

        // Private variables
        private bool _shipInRange = false;
        private GameObject _nearbyShip = null;
        private Player.ShipController _shipController = null;
        private bool _isLanding = false;
        private GameObject _parkedShip = null;
        private float _shipActivatedTime = 0f;

        private void Update()
        {
            if (_isLanding || StationLandingPad._anyLandingInProgress) return;

            FindNearbyShip();

            if (_shipInRange && _nearbyShip != null && _shipController != null && _shipController.IsActive)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
                {
                    if (Time.time - _shipActivatedTime > 0.5f)
                    {
                        Debug.Log($"[Outpost47] F PRESSED - Landing initiated at Outpost 47");

                        // Track player position at time of pressing F
                        GameObject player = GameObject.FindGameObjectWithTag("Player");
                        if (player != null)
                        {
                            Debug.Log($"[Outpost47] PLAYER POSITION AT F PRESS: {player.transform.position}");
                        }
                        else
                        {
                            Debug.Log($"[Outpost47] NO PLAYER EXISTS AT F PRESS");
                        }

                        StartCoroutine(LandingSequence());
                        StartCoroutine(TrackPlayerPositionAfterDelay(5f));
                    }
                }
            }
        }

        private System.Collections.IEnumerator TrackPlayerPositionAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Debug.Log($"[Outpost47] PLAYER POSITION 5 SECONDS LATER: {player.transform.position}");
            }
            else
            {
                Debug.Log($"[Outpost47] NO PLAYER EXISTS 5 SECONDS LATER");
            }
        }

        private void FindNearbyShip()
        {
            GameObject[] ships = GameObject.FindGameObjectsWithTag(_shipTag);

            bool wasInRange = _shipInRange;
            _shipInRange = false;
            _nearbyShip = null;
            _shipController = null;

            foreach (GameObject ship in ships)
            {
                float distance = Vector3.Distance(transform.position, ship.transform.position);

                if (distance <= _landingRange)
                {
                    var controller = ship.GetComponent<Player.ShipController>();
                    if (controller != null && controller.IsActive)
                    {
                        _shipInRange = true;
                        _nearbyShip = ship;
                        _shipController = controller;

                        if (!wasInRange)
                        {
                            _shipActivatedTime = Time.time;
                        }

                        return;
                    }
                }
            }
        }

        private System.Collections.IEnumerator LandingSequence()
        {
            _isLanding = true;
            StationLandingPad._anyLandingInProgress = true;

            // CRITICAL: Set this flag to prevent PlayerSpawnPoint from spawning a player at Space Port
            PlayerPrefs.SetInt("IsBoardingShip", 1);
            PlayerPrefs.Save();

            Debug.Log("[Outpost47] Starting landing sequence at Outpost 47");

            // Exit the ship
            _shipController.ExitShip();

            // Disable ship physics
            Rigidbody shipRb = _nearbyShip.GetComponent<Rigidbody>();
            if (shipRb != null)
            {
                shipRb.isKinematic = true;
                shipRb.linearVelocity = Vector3.zero;
                shipRb.angularVelocity = Vector3.zero;
            }

            // Reset ship entry point
            var shipEntryPoint = _nearbyShip.GetComponent<Player.ShipEntryPoint>();
            if (shipEntryPoint != null)
            {
                shipEntryPoint.ResetPilotingState();
                shipEntryPoint.enabled = true;
            }

            // Calculate exit position for Outpost 47
            Vector3 exitPos;
            Quaternion exitRot;

            if (_outpost47ExitPoint != null)
            {
                exitPos = _outpost47ExitPoint.position;
                exitRot = _outpost47ExitPoint.rotation;
                Debug.Log($"[Outpost47] Using Outpost 47 exit point at {exitPos}");
            }
            else
            {
                exitPos = transform.position + new Vector3(0, 1, 5);
                exitRot = transform.rotation;
                Debug.LogWarning("[Outpost47] No exit point assigned! Using default offset");
            }

            // Get or spawn player
            GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
            Debug.Log($"[Outpost47] Found {allPlayers.Length} players with 'Player' tag");

            foreach (GameObject p in allPlayers)
            {
                Debug.Log($"[Outpost47] Player '{p.name}' at position {p.transform.position}");
            }

            // DESTROY ALL EXISTING PLAYERS to prevent duplicates
            if (allPlayers.Length > 0)
            {
                Debug.Log($"[Outpost47] Destroying {allPlayers.Length} existing players");
                foreach (GameObject p in allPlayers)
                {
                    Destroy(p);
                }
            }

            GameObject player = null;

            // Always spawn a fresh player at the exit point
            Debug.Log("[Outpost47] Spawning fresh player at Outpost 47 exit");

            PlayerSpawnPoint spawner = FindObjectOfType<PlayerSpawnPoint>();
            if (spawner != null && spawner.PlayerPrefab != null)
            {
                player = Instantiate(spawner.PlayerPrefab, exitPos, exitRot);
                player.name = "Player";
                player.tag = "Player";

                Debug.Log($"[Outpost47] Spawned player at Outpost 47: {exitPos}");

                Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.enabled = true;
                }

                var locomotion = player.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
                if (locomotion != null)
                {
                    locomotion.enabled = true;
                }

                var charController = player.GetComponent<CharacterController>();
                if (charController != null)
                {
                    charController.enabled = true;
                }

                yield return null;
            }
            else
            {
                Debug.LogError("[Outpost47] No PlayerSpawnPoint found!");
            }

            // Final position check - player should already be at exit point from spawn
            if (player != null)
            {
                var charController = player.GetComponent<CharacterController>();
                if (charController != null)
                {
                    charController.enabled = false;
                }

                var locomotion = player.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
                if (locomotion != null)
                {
                    locomotion.enabled = false;
                }

                player.transform.position = exitPos;
                player.transform.rotation = exitRot;
                Debug.Log($"[Outpost47] Moved player to Outpost 47 exit: {exitPos}");

                Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.enabled = true;
                }

                if (charController != null)
                {
                    charController.enabled = true;
                }

                if (locomotion != null)
                {
                    locomotion.enabled = true;
                }
            }

            yield return null;

            // Re-enable camera
            Camera mainCam = Camera.main;
            if (mainCam != null && player != null)
            {
                var cameraController = mainCam.GetComponent<Opsive.UltimateCharacterController.Camera.CameraController>();
                if (cameraController != null)
                {
                    cameraController.enabled = false;
                    cameraController.Character = player;
                    mainCam.transform.position = player.transform.position + new Vector3(0, 2, -5);
                    cameraController.enabled = true;

                    Debug.Log($"[Outpost47] Camera linked to player at Outpost 47");
                }
            }

            // Park the ship at Outpost 47
            Vector3 parkPos = transform.position + transform.TransformDirection(_shipParkPosition);

            float elapsed = 0f;
            Vector3 startPos = _nearbyShip.transform.position;

            while (elapsed < _landingDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _landingDuration;
                _nearbyShip.transform.position = Vector3.Lerp(startPos, parkPos, t);
                yield return null;
            }

            _nearbyShip.transform.position = parkPos;
            _parkedShip = _nearbyShip;

            Debug.Log("[Outpost47] Ship parked at Outpost 47");

            // Clear the boarding flag now that landing is complete
            PlayerPrefs.SetInt("IsBoardingShip", 0);
            PlayerPrefs.Save();

            _isLanding = false;
            StationLandingPad._anyLandingInProgress = false;
        }

        private void OnGUI()
        {
            if (_shipInRange && !_isLanding && _shipController != null && _shipController.IsActive)
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

                GUI.Label(promptRect, "Press F to Land at Outpost 47");
                GUI.color = Color.white;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw landing range (cyan for Outpost 47)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _landingRange);

            // Draw ship park position
            Gizmos.color = Color.blue;
            Vector3 parkPos = transform.position + transform.TransformDirection(_shipParkPosition);
            Gizmos.DrawWireCube(parkPos, new Vector3(10f, 5f, 15f));

            // Draw player exit position
            if (_outpost47ExitPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_outpost47ExitPoint.position, 1f);
                Gizmos.DrawLine(transform.position, _outpost47ExitPoint.position);
            }
        }
    }
}
