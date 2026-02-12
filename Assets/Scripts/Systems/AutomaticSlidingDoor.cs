/*
 * Automatic Sliding Door System for Klyra's Reach
 *
 * PURPOSE:
 * Creates sci-fi style automatic sliding doors that open when the player approaches
 * and close when they move away. Perfect for hangar entrances and interior doorways.
 *
 * HOW TO SET UP:
 * 1. Create a parent GameObject called "SlidingDoor"
 * 2. Add this script to the parent
 * 3. Create child objects for the door panels (left/right or single)
 * 4. Assign the door panel GameObjects to the script in the Inspector
 * 5. Configure settings (open distance, slide amount, speed, etc.)
 * 6. The script will automatically create a trigger zone for detection
 *
 * FEATURES:
 * - Smooth sliding animation
 * - Automatic player detection
 * - Configurable slide direction (left/right, up/down)
 * - Optional sound effects (door open/close sounds)
 * - Adjustable detection range
 * - Works with any player tag
 */

using UnityEngine;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Handles automatic sliding door behavior with player proximity detection
    /// </summary>
    public class AutomaticSlidingDoor : MonoBehaviour
    {
        [Header("Door Panel References")]
        [Tooltip("The left door panel (or single door panel if only one)")]
        [SerializeField] private Transform _leftDoorPanel;

        [Tooltip("The right door panel (leave empty for single-panel doors)")]
        [SerializeField] private Transform _rightDoorPanel;

        [Header("Door Behavior Settings")]
        [Tooltip("How far each door panel slides when opening (in Unity units)")]
        [SerializeField] private float _slideDistance = 2f;

        [Tooltip("How fast the doors slide open/closed")]
        [SerializeField] private float _slideSpeed = 2f;

        [Tooltip("How close the player needs to be for doors to open (in Unity units)")]
        [SerializeField] private float _detectionRange = 3f;

        [Tooltip("Direction the door(s) slide: X = left/right, Y = up/down, Z = forward/back")]
        [SerializeField] private Vector3 _slideDirection = Vector3.right;

        [Header("Player Detection")]
        [Tooltip("Tag to identify the player GameObject (default: 'Player')")]
        [SerializeField] private string _playerTag = "Player";

        [Header("Audio (Optional)")]
        [Tooltip("Sound to play when door opens (optional)")]
        [SerializeField] private AudioClip _openSound;

        [Tooltip("Sound to play when door closes (optional)")]
        [SerializeField] private AudioClip _closeSound;

        // Private variables to track door state
        private Vector3 _leftDoorClosedPosition;  // Starting position of left door
        private Vector3 _leftDoorOpenPosition;    // Target position when left door is open
        private Vector3 _rightDoorClosedPosition; // Starting position of right door
        private Vector3 _rightDoorOpenPosition;   // Target position when right door is open

        private bool _isOpen = false;             // Current state of the door
        private bool _playerInRange = false;      // Is player close enough to trigger?
        private AudioSource _audioSource;         // Component for playing sounds
        private BoxCollider _triggerZone;         // Invisible trigger zone for detection

        /// <summary>
        /// Called when the script is first loaded
        /// Stores the starting positions of door panels and sets up trigger zone
        /// </summary>
        private void Awake()
        {
            // Make sure we have at least one door panel assigned
            if (_leftDoorPanel == null)
            {
                Debug.LogError("[AutomaticSlidingDoor] No door panel assigned! Please assign at least the Left Door Panel in the Inspector.");
                enabled = false; // Disable this script if not set up properly
                return;
            }

            // Store the original (closed) positions of the door panels
            _leftDoorClosedPosition = _leftDoorPanel.localPosition;

            // Calculate where the left door should be when fully open
            // We normalize the slide direction to ensure consistent sliding regardless of vector length
            _leftDoorOpenPosition = _leftDoorClosedPosition + (_slideDirection.normalized * _slideDistance);

            // If there's a right door panel, set up its positions too
            if (_rightDoorPanel != null)
            {
                _rightDoorClosedPosition = _rightDoorPanel.localPosition;
                // Right door slides in the opposite direction
                _rightDoorOpenPosition = _rightDoorClosedPosition + (-_slideDirection.normalized * _slideDistance);
            }

            // Set up audio source for door sounds (if sounds are assigned)
            if (_openSound != null || _closeSound != null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false; // Don't play automatically
                _audioSource.spatialBlend = 1f;   // Make it 3D sound (volume based on distance)
            }

            // Create an invisible trigger zone for player detection
            CreateTriggerZone();
        }

        /// <summary>
        /// Creates an invisible box collider that detects when the player is near
        /// </summary>
        private void CreateTriggerZone()
        {
            // Add a box collider to this GameObject
            _triggerZone = gameObject.AddComponent<BoxCollider>();

            // Make it a trigger (doesn't physically block objects, just detects them)
            _triggerZone.isTrigger = true;

            // Size the trigger zone based on detection range
            // We make it slightly larger than the detection range for smooth triggering
            _triggerZone.size = new Vector3(_detectionRange * 2, 3f, _detectionRange * 2);

            Debug.Log($"[AutomaticSlidingDoor] Trigger zone created with range: {_detectionRange} units");
        }

        /// <summary>
        /// Called every frame
        /// Handles the smooth sliding animation of the doors
        /// </summary>
        private void Update()
        {
            // Determine target positions based on whether door should be open or closed
            Vector3 leftTargetPosition = _playerInRange ? _leftDoorOpenPosition : _leftDoorClosedPosition;
            Vector3 rightTargetPosition = _playerInRange ? _rightDoorOpenPosition : _rightDoorClosedPosition;

            // Smoothly move left door panel toward its target position
            _leftDoorPanel.localPosition = Vector3.Lerp(
                _leftDoorPanel.localPosition,  // Current position
                leftTargetPosition,             // Target position
                Time.deltaTime * _slideSpeed    // Speed (frame-rate independent)
            );

            // If there's a right door, move it too
            if (_rightDoorPanel != null)
            {
                _rightDoorPanel.localPosition = Vector3.Lerp(
                    _rightDoorPanel.localPosition,
                    rightTargetPosition,
                    Time.deltaTime * _slideSpeed
                );
            }

            // Check if door state has changed (for sound effects)
            UpdateDoorState();
        }

        /// <summary>
        /// Checks if the door has finished opening or closing and plays appropriate sounds
        /// </summary>
        private void UpdateDoorState()
        {
            // Calculate how close the door is to being fully open
            float distanceToOpenPosition = Vector3.Distance(_leftDoorPanel.localPosition, _leftDoorOpenPosition);

            // If player is in range and door just finished opening
            if (_playerInRange && !_isOpen && distanceToOpenPosition < 0.1f)
            {
                _isOpen = true;
                PlaySound(_openSound);
                Debug.Log("[AutomaticSlidingDoor] Door opened");
            }
            // If player left range and door just finished closing
            else if (!_playerInRange && _isOpen && distanceToOpenPosition > _slideDistance - 0.1f)
            {
                _isOpen = false;
                PlaySound(_closeSound);
                Debug.Log("[AutomaticSlidingDoor] Door closed");
            }
        }

        /// <summary>
        /// Called when something enters the trigger zone
        /// </summary>
        /// <param name="other">The collider that entered the trigger</param>
        private void OnTriggerEnter(Collider other)
        {
            // Check if the object that entered has the player tag
            if (other.CompareTag(_playerTag))
            {
                _playerInRange = true;
                Debug.Log("[AutomaticSlidingDoor] Player detected - opening door");
            }
        }

        /// <summary>
        /// Called when something exits the trigger zone
        /// </summary>
        /// <param name="other">The collider that exited the trigger</param>
        private void OnTriggerExit(Collider other)
        {
            // Check if the player left the trigger zone
            if (other.CompareTag(_playerTag))
            {
                _playerInRange = false;
                Debug.Log("[AutomaticSlidingDoor] Player left range - closing door");
            }
        }

        /// <summary>
        /// Plays a sound effect if one is assigned
        /// </summary>
        /// <param name="clip">The audio clip to play</param>
        private void PlaySound(AudioClip clip)
        {
            // Only play if we have both an audio source and a clip
            if (_audioSource != null && clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Draws visual guides in the Scene view (only visible in Unity Editor)
        /// Shows the trigger zone and slide direction
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw the detection range as a semi-transparent box
            Gizmos.color = new Color(0, 1, 0, 0.3f); // Green, semi-transparent
            Gizmos.DrawWireCube(transform.position, new Vector3(_detectionRange * 2, 3f, _detectionRange * 2));

            // Draw an arrow showing the slide direction (if we have a door panel assigned)
            if (_leftDoorPanel != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 arrowStart = _leftDoorPanel.position;
                Vector3 arrowEnd = arrowStart + (_slideDirection.normalized * _slideDistance);
                Gizmos.DrawLine(arrowStart, arrowEnd);

                // Draw arrow head
                Gizmos.DrawSphere(arrowEnd, 0.1f);
            }
        }
    }
}
