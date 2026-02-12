/*
 * Simple Sliding Door for Klyra's Reach
 *
 * PURPOSE:
 * Super simple automatic door - just attach this script directly to any door object
 * and it will automatically slide when the player gets close.
 *
 * HOW TO USE:
 * 1. Select a door object in your scene (the actual Synty door model)
 * 2. Click "Add Component" in the Inspector
 * 3. Search for "Simple Sliding Door" and add it
 * 4. Adjust the settings (which way it slides, how far, how fast)
 * 5. Done! Press Play and walk near it
 *
 * THAT'S IT! No parents, no children, just put it on the door itself.
 */

using UnityEngine;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Simple automatic sliding door - attach directly to any door GameObject
    /// </summary>
    public class SimpleSlidingDoor : MonoBehaviour
    {
        [Header("Slide Settings")]
        [Tooltip("Which direction the door slides: Left(-1) or Right(+1)")]
        [SerializeField] private float _slideDirectionX = 1f;

        [Tooltip("How far the door slides when opening (in Unity units)")]
        [SerializeField] private float _slideDistance = 2f;

        [Tooltip("How fast the door slides")]
        [SerializeField] private float _slideSpeed = 2f;

        [Header("Detection Settings")]
        [Tooltip("How close the player needs to be for the door to open")]
        [SerializeField] private float _detectionRange = 3f;

        [Tooltip("Tag used to identify the player")]
        [SerializeField] private string _playerTag = "Player";

        [Header("Audio (Optional)")]
        [Tooltip("Sound when door opens")]
        [SerializeField] private AudioClip _openSound;

        [Tooltip("Sound when door closes")]
        [SerializeField] private AudioClip _closeSound;

        // Private variables
        private Vector3 _closedPosition;      // Where the door starts
        private Vector3 _openPosition;        // Where the door slides to
        private bool _isOpen = false;         // Is door currently open?
        private Transform _playerTransform;   // Reference to player
        private AudioSource _audioSource;     // For playing sounds

        /// <summary>
        /// Called when script starts
        /// Sets up the door's positions and audio
        /// </summary>
        private void Start()
        {
            // Remember where the door starts (closed position)
            _closedPosition = transform.position;

            // Calculate where the door should be when fully open
            // We slide in the X direction (left/right) by default
            Vector3 slideVector = new Vector3(_slideDirectionX, 0, 0).normalized * _slideDistance;
            _openPosition = _closedPosition + slideVector;

            // Set up audio if sounds are assigned
            if (_openSound != null || _closeSound != null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1f; // 3D sound
            }

            // Try to find the player in the scene
            GameObject player = GameObject.FindGameObjectWithTag(_playerTag);
            if (player != null)
            {
                _playerTransform = player.transform;
            }
            else
            {
                Debug.LogWarning($"[SimpleSlidingDoor] Could not find player with tag '{_playerTag}'. Make sure your player has the correct tag!");
            }

            Debug.Log($"[SimpleSlidingDoor] Door '{gameObject.name}' initialized. Closed pos: {_closedPosition}, Open pos: {_openPosition}");
        }

        /// <summary>
        /// Called every frame
        /// Checks player distance and slides the door accordingly
        /// </summary>
        private void Update()
        {
            // If we don't have a player reference, can't do anything
            if (_playerTransform == null)
            {
                return;
            }

            // Calculate how far the player is from this door
            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

            // Determine if door should be open or closed based on player distance
            bool shouldBeOpen = distanceToPlayer <= _detectionRange;

            // Pick the target position based on whether door should be open
            Vector3 targetPosition = shouldBeOpen ? _openPosition : _closedPosition;

            // Smoothly slide the door toward the target position
            transform.position = Vector3.Lerp(
                transform.position,           // Current position
                targetPosition,                // Target position
                Time.deltaTime * _slideSpeed   // Speed (frame-rate independent)
            );

            // Check if door state changed (for sound effects)
            CheckDoorStateChange(shouldBeOpen);
        }

        /// <summary>
        /// Checks if the door just finished opening or closing and plays sounds
        /// </summary>
        /// <param name="shouldBeOpen">Whether the door should currently be open</param>
        private void CheckDoorStateChange(bool shouldBeOpen)
        {
            // Calculate how close we are to the target position
            float distanceToTarget = Vector3.Distance(
                transform.position,
                shouldBeOpen ? _openPosition : _closedPosition
            );

            // If door just finished opening
            if (shouldBeOpen && !_isOpen && distanceToTarget < 0.1f)
            {
                _isOpen = true;
                PlaySound(_openSound);
                Debug.Log($"[SimpleSlidingDoor] '{gameObject.name}' opened");
            }
            // If door just finished closing
            else if (!shouldBeOpen && _isOpen && distanceToTarget < 0.1f)
            {
                _isOpen = false;
                PlaySound(_closeSound);
                Debug.Log($"[SimpleSlidingDoor] '{gameObject.name}' closed");
            }
        }

        /// <summary>
        /// Plays a sound effect if one is assigned
        /// </summary>
        /// <param name="clip">The audio clip to play</param>
        private void PlaySound(AudioClip clip)
        {
            if (_audioSource != null && clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Draws visual guides in the Scene view (Editor only)
        /// Shows the detection range and slide path
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw detection range as a sphere
            Gizmos.color = new Color(0, 1, 0, 0.3f); // Green, transparent
            Gizmos.DrawWireSphere(transform.position, _detectionRange);

            // Draw the slide path
            if (Application.isPlaying)
            {
                // In Play mode, show actual positions
                Gizmos.color = Color.red;
                Gizmos.DrawLine(_closedPosition, _openPosition);
                Gizmos.DrawSphere(_openPosition, 0.1f);
            }
            else
            {
                // In Edit mode, show preview
                Vector3 slideVector = new Vector3(_slideDirectionX, 0, 0).normalized * _slideDistance;
                Vector3 previewOpenPos = transform.position + slideVector;
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, previewOpenPos);
                Gizmos.DrawSphere(previewOpenPos, 0.1f);
            }
        }
    }
}
