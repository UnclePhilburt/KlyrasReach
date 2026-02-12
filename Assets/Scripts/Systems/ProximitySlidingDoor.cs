/*
 * Proximity-Based Sliding Door for Klyra's Reach
 *
 * PURPOSE:
 * This version doesn't rely on trigger collisions - it simply checks distance
 * to the player every frame. More reliable with Opsive character controllers.
 *
 * HOW TO USE:
 * 1. Attach to your door GameObject
 * 2. Assign the door panel(s) in Inspector
 * 3. Press Play - it will automatically find and track the player
 */

using UnityEngine;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Sliding door that uses distance checking instead of trigger collisions
    /// More reliable with character controllers on custom layers
    /// </summary>
    public class ProximitySlidingDoor : MonoBehaviour
    {
        [Header("Door Panel References")]
        [Tooltip("The door panel that will slide (or left panel for double doors)")]
        [SerializeField] private Transform _leftDoorPanel;

        [Tooltip("Optional second door panel for double doors")]
        [SerializeField] private Transform _rightDoorPanel;

        [Header("Door Behavior Settings")]
        [Tooltip("How far the door slides when opening")]
        [SerializeField] private float _slideDistance = 5f;

        [Tooltip("Speed of the door sliding animation")]
        [SerializeField] private float _slideSpeed = 2f;

        [Tooltip("How close the player needs to be to open the door")]
        [SerializeField] private float _detectionRange = 3f;

        [Tooltip("Direction the door slides: X=left/right, Y=up/down, Z=forward/back")]
        [SerializeField] private Vector3 _slideDirection = Vector3.right;

        [Header("Player Detection")]
        [Tooltip("Tag to identify the player GameObject")]
        [SerializeField] private string _playerTag = "Player";

        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip _openSound;
        [SerializeField] private AudioClip _closeSound;

        // Private variables
        private Vector3 _leftDoorClosedPosition;
        private Vector3 _leftDoorOpenPosition;
        private Vector3 _rightDoorClosedPosition;
        private Vector3 _rightDoorOpenPosition;

        private bool _isOpen = false;
        private Transform _playerTransform;
        private AudioSource _audioSource;
        private Vector3 _detectionPoint; // Fixed position for distance checks

        /// <summary>
        /// Initialize door positions and find the player
        /// </summary>
        private void Start()
        {
            // Validate setup
            if (_leftDoorPanel == null)
            {
                Debug.LogError("[ProximitySlidingDoor] No door panel assigned! Please assign at least the Left Door Panel.");
                enabled = false;
                return;
            }

            // Store the door's starting world position for distance checks
            // This prevents the door from leaving its own detection range when it moves
            _detectionPoint = transform.position;

            // Store closed positions
            _leftDoorClosedPosition = _leftDoorPanel.localPosition;
            _leftDoorOpenPosition = _leftDoorClosedPosition + (_slideDirection.normalized * _slideDistance);

            if (_rightDoorPanel != null)
            {
                _rightDoorClosedPosition = _rightDoorPanel.localPosition;
                _rightDoorOpenPosition = _rightDoorClosedPosition + (-_slideDirection.normalized * _slideDistance);
            }

            // Find the player
            GameObject playerObject = GameObject.FindGameObjectWithTag(_playerTag);
            if (playerObject != null)
            {
                _playerTransform = playerObject.transform;
                Debug.Log($"[ProximitySlidingDoor] Found player: {playerObject.name}");
            }
            else
            {
                Debug.LogError($"[ProximitySlidingDoor] Could not find player with tag '{_playerTag}'!");
            }

            // Setup audio
            if (_openSound != null || _closeSound != null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1f;
            }

            Debug.Log($"[ProximitySlidingDoor] Door '{gameObject.name}' initialized. Detection range: {_detectionRange} units");
        }

        /// <summary>
        /// Check player distance and animate door every frame
        /// </summary>
        private void Update()
        {
            // If we don't have a player reference, try to find one
            if (_playerTransform == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag(_playerTag);
                if (playerObject != null)
                {
                    _playerTransform = playerObject.transform;
                    Debug.Log($"[ProximitySlidingDoor] Found player: {playerObject.name}");
                }
                else
                {
                    return; // Still no player, wait for next frame
                }
            }

            // Calculate distance between door's ORIGINAL position and player
            // This prevents doors from leaving their own detection range when they slide
            float distanceToPlayer = Vector3.Distance(_detectionPoint, _playerTransform.position);

            // Determine if player is in range
            bool playerInRange = distanceToPlayer <= _detectionRange;

            // Determine target positions based on whether door should be open
            Vector3 leftTarget = playerInRange ? _leftDoorOpenPosition : _leftDoorClosedPosition;
            Vector3 rightTarget = playerInRange ? _rightDoorOpenPosition : _rightDoorClosedPosition;

            // Smoothly move door panels
            _leftDoorPanel.localPosition = Vector3.Lerp(
                _leftDoorPanel.localPosition,
                leftTarget,
                Time.deltaTime * _slideSpeed
            );

            if (_rightDoorPanel != null)
            {
                _rightDoorPanel.localPosition = Vector3.Lerp(
                    _rightDoorPanel.localPosition,
                    rightTarget,
                    Time.deltaTime * _slideSpeed
                );
            }

            // Check if door state changed (for sounds and logging)
            UpdateDoorState(playerInRange);
        }

        /// <summary>
        /// Detect when door state changes and play sounds immediately
        /// </summary>
        private void UpdateDoorState(bool playerInRange)
        {
            // Player just entered range - start opening
            if (playerInRange && !_isOpen)
            {
                _isOpen = true;
                PlaySound(_openSound);
                Debug.Log($"[ProximitySlidingDoor] Door '{gameObject.name}' opening");
            }
            // Player just left range - start closing
            else if (!playerInRange && _isOpen)
            {
                _isOpen = false;
                PlaySound(_closeSound);
                Debug.Log($"[ProximitySlidingDoor] Door '{gameObject.name}' closing");
            }
        }

        /// <summary>
        /// Play a sound effect
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (_audioSource != null && clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Draw visual debug helpers in Scene view
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw detection range sphere at the original position
            Vector3 debugPosition = Application.isPlaying ? _detectionPoint : transform.position;
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(debugPosition, _detectionRange);

            // Draw slide direction arrow
            if (_leftDoorPanel != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 start = Application.isPlaying ? _leftDoorClosedPosition : _leftDoorPanel.position;
                Vector3 end = start + (_slideDirection.normalized * _slideDistance);
                Gizmos.DrawLine(start, end);
                Gizmos.DrawSphere(end, 0.15f);
            }
        }
    }
}
