/*
 * Proximity-Based Hinged Door for Klyra's Reach
 *
 * PURPOSE:
 * Hinged/swinging door that rotates open when player approaches
 * Uses distance checking instead of trigger collisions
 *
 * HOW TO USE:
 * 1. Attach to your door GameObject (or parent if you have multiple door panels)
 * 2. Assign the door panel(s) in Inspector
 * 3. Set the open angle (90 degrees = standard door)
 * 4. Press Play - it will automatically find and track the player
 *
 * IMPORTANT: The door will rotate around its pivot point. Make sure the pivot
 * is positioned at the hinge edge of the door in your 3D model!
 */

using UnityEngine;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Hinged door that rotates open using distance checking
    /// Supports single or double doors
    /// </summary>
    public class ProximityHingedDoor : MonoBehaviour
    {
        [Header("Door Panel References")]
        [Tooltip("The door panel that will swing (or left panel for double doors)")]
        [SerializeField] private Transform _leftDoorPanel;

        [Tooltip("Optional second door panel for double doors")]
        [SerializeField] private Transform _rightDoorPanel;

        [Header("Door Behavior Settings")]
        [Tooltip("How many degrees the door rotates when opening (90 = standard door)")]
        [SerializeField] private float _openAngle = 90f;

        [Tooltip("Speed of the door rotation animation")]
        [SerializeField] private float _rotationSpeed = 2f;

        [Tooltip("How close the player needs to be to open the door")]
        [SerializeField] private float _detectionRange = 3f;

        [Header("Rotation Settings")]
        [Tooltip("Axis to rotate around (Y = vertical axis for normal doors)")]
        [SerializeField] private Vector3 _rotationAxis = Vector3.up;

        [Tooltip("Reverse the rotation direction")]
        [SerializeField] private bool _reverseRotation = false;

        [Header("Player Detection")]
        [Tooltip("Tag to identify the player GameObject")]
        [SerializeField] private string _playerTag = "Player";

        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip _openSound;
        [SerializeField] private AudioClip _closeSound;

        [Tooltip("Volume for door sounds (0-1)")]
        [SerializeField] [Range(0f, 1f)] private float _audioVolume = 0.5f;

        // Private variables
        private Quaternion _leftDoorClosedRotation;
        private Quaternion _leftDoorOpenRotation;
        private Quaternion _rightDoorClosedRotation;
        private Quaternion _rightDoorOpenRotation;

        private bool _isOpen = false;
        private Transform _playerTransform;
        private AudioSource _audioSource;
        private Vector3 _detectionPoint; // Fixed position for distance checks

        /// <summary>
        /// Initialize door rotations and find the player
        /// </summary>
        private void Start()
        {
            // Validate setup
            if (_leftDoorPanel == null)
            {
                Debug.LogError("[ProximityHingedDoor] No door panel assigned! Please assign at least the Left Door Panel.");
                enabled = false;
                return;
            }

            // Store the door's starting world position for distance checks
            _detectionPoint = transform.position;

            // Store closed rotations
            _leftDoorClosedRotation = _leftDoorPanel.localRotation;

            // Calculate open rotation for left door
            float leftAngle = _reverseRotation ? _openAngle : -_openAngle;
            Quaternion leftRotation = Quaternion.AngleAxis(leftAngle, _rotationAxis);
            _leftDoorOpenRotation = _leftDoorClosedRotation * leftRotation;

            // If there's a right door, calculate its rotation (opposite direction)
            if (_rightDoorPanel != null)
            {
                _rightDoorClosedRotation = _rightDoorPanel.localRotation;
                float rightAngle = _reverseRotation ? -_openAngle : _openAngle;
                Quaternion rightRotation = Quaternion.AngleAxis(rightAngle, _rotationAxis);
                _rightDoorOpenRotation = _rightDoorClosedRotation * rightRotation;
            }

            // Find the player
            GameObject playerObject = GameObject.FindGameObjectWithTag(_playerTag);
            if (playerObject != null)
            {
                _playerTransform = playerObject.transform;
                Debug.Log($"[ProximityHingedDoor] Found player: {playerObject.name}");
            }
            else
            {
                Debug.LogError($"[ProximityHingedDoor] Could not find player with tag '{_playerTag}'!");
            }

            // Setup audio
            if (_openSound != null || _closeSound != null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1f;
                _audioSource.volume = _audioVolume;
            }

            Debug.Log($"[ProximityHingedDoor] Door '{gameObject.name}' initialized. Detection range: {_detectionRange} units, Open angle: {_openAngle} degrees");
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
                    Debug.Log($"[ProximityHingedDoor] Found player: {playerObject.name}");
                }
                else
                {
                    return; // Still no player, wait for next frame
                }
            }

            // Calculate distance between door's ORIGINAL position and player
            float distanceToPlayer = Vector3.Distance(_detectionPoint, _playerTransform.position);

            // Determine if player is in range
            bool playerInRange = distanceToPlayer <= _detectionRange;

            // Determine target rotations based on whether door should be open
            Quaternion leftTarget = playerInRange ? _leftDoorOpenRotation : _leftDoorClosedRotation;
            Quaternion rightTarget = playerInRange ? _rightDoorOpenRotation : _rightDoorClosedRotation;

            // Smoothly rotate door panels
            _leftDoorPanel.localRotation = Quaternion.Slerp(
                _leftDoorPanel.localRotation,
                leftTarget,
                Time.deltaTime * _rotationSpeed
            );

            if (_rightDoorPanel != null)
            {
                _rightDoorPanel.localRotation = Quaternion.Slerp(
                    _rightDoorPanel.localRotation,
                    rightTarget,
                    Time.deltaTime * _rotationSpeed
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
                Debug.Log($"[ProximityHingedDoor] Door '{gameObject.name}' opening");
            }
            // Player just left range - start closing
            else if (!playerInRange && _isOpen)
            {
                _isOpen = false;
                PlaySound(_closeSound);
                Debug.Log($"[ProximityHingedDoor] Door '{gameObject.name}' closing");
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

            // Draw rotation arc for left door
            if (_leftDoorPanel != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 doorPosition = _leftDoorPanel.position;
                Vector3 forward = _leftDoorPanel.forward;

                // Draw current facing direction
                Gizmos.DrawLine(doorPosition, doorPosition + forward * 2f);

                // Draw open position preview
                float previewAngle = _reverseRotation ? _openAngle : -_openAngle;
                Vector3 openForward = Quaternion.AngleAxis(previewAngle, _rotationAxis) * forward;
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(doorPosition, doorPosition + openForward * 2f);
            }

            // Draw rotation arc for right door (if exists)
            if (_rightDoorPanel != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 doorPosition = _rightDoorPanel.position;
                Vector3 forward = _rightDoorPanel.forward;

                // Draw current facing direction
                Gizmos.DrawLine(doorPosition, doorPosition + forward * 2f);

                // Draw open position preview
                float previewAngle = _reverseRotation ? -_openAngle : _openAngle;
                Vector3 openForward = Quaternion.AngleAxis(previewAngle, _rotationAxis) * forward;
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(doorPosition, doorPosition + openForward * 2f);
            }
        }
    }
}
