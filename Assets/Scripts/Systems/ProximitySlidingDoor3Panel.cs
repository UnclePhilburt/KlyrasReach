/*
 * 3-Panel Proximity-Based Sliding Door for Klyra's Reach
 *
 * PURPOSE:
 * Sliding door with 3 panels: left, right, and top-middle
 * Left/right panels slide horizontally, top panel slides down vertically
 *
 * HOW TO USE:
 * 1. Attach to your door GameObject
 * 2. Assign all 3 door panels in Inspector (left, right, top)
 * 3. Press Play - it will automatically find and track the player
 */

using UnityEngine;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Sliding door with 3 panels that uses distance checking
    /// Left and right panels slide horizontally, top panel slides vertically
    /// </summary>
    public class ProximitySlidingDoor3Panel : MonoBehaviour
    {
        [Header("Door Panel References")]
        [Tooltip("Left door panel that slides left")]
        [SerializeField] private Transform _leftDoorPanel;

        [Tooltip("Right door panel that slides right")]
        [SerializeField] private Transform _rightDoorPanel;

        [Tooltip("Top/middle door panel that slides down")]
        [SerializeField] private Transform _topDoorPanel;

        [Header("Door Behavior Settings")]
        [Tooltip("How far the side panels slide when opening")]
        [SerializeField] private float _sideSlideDistance = 5f;

        [Tooltip("How far the top panel slides down when opening")]
        [SerializeField] private float _topSlideDistance = 3f;

        [Tooltip("Speed of the door sliding animation")]
        [SerializeField] private float _slideSpeed = 2f;

        [Tooltip("How close the player needs to be to open the door")]
        [SerializeField] private float _detectionRange = 3f;

        [Header("Slide Directions")]
        [Tooltip("Direction the side panels slide (usually X for left/right)")]
        [SerializeField] private Vector3 _sideSlideDirection = Vector3.right;

        [Tooltip("Direction the top panel slides (usually -Y for down)")]
        [SerializeField] private Vector3 _topSlideDirection = Vector3.down;

        [Header("Player Detection")]
        [Tooltip("Tag to identify the player GameObject")]
        [SerializeField] private string _playerTag = "Player";

        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip _openSound;
        [SerializeField] private AudioClip _closeSound;

        [Tooltip("Volume for door sounds (0-1)")]
        [SerializeField] [Range(0f, 1f)] private float _audioVolume = 0.5f;

        // Private variables
        private Vector3 _leftDoorClosedPosition;
        private Vector3 _leftDoorOpenPosition;
        private Vector3 _rightDoorClosedPosition;
        private Vector3 _rightDoorOpenPosition;
        private Vector3 _topDoorClosedPosition;
        private Vector3 _topDoorOpenPosition;

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
            if (_leftDoorPanel == null || _rightDoorPanel == null || _topDoorPanel == null)
            {
                Debug.LogError("[ProximitySlidingDoor3Panel] Missing door panels! Please assign all 3 panels (left, right, top).");
                enabled = false;
                return;
            }

            // Store the door's starting world position for distance checks
            _detectionPoint = transform.position;

            // Store closed positions and calculate open positions
            _leftDoorClosedPosition = _leftDoorPanel.localPosition;
            _leftDoorOpenPosition = _leftDoorClosedPosition + (_sideSlideDirection.normalized * _sideSlideDistance);

            _rightDoorClosedPosition = _rightDoorPanel.localPosition;
            _rightDoorOpenPosition = _rightDoorClosedPosition + (-_sideSlideDirection.normalized * _sideSlideDistance);

            _topDoorClosedPosition = _topDoorPanel.localPosition;
            _topDoorOpenPosition = _topDoorClosedPosition + (-_topSlideDirection.normalized * _topSlideDistance);

            // Find the player
            GameObject playerObject = GameObject.FindGameObjectWithTag(_playerTag);
            if (playerObject != null)
            {
                _playerTransform = playerObject.transform;
                Debug.Log($"[ProximitySlidingDoor3Panel] Found player: {playerObject.name}");
            }
            else
            {
                Debug.LogError($"[ProximitySlidingDoor3Panel] Could not find player with tag '{_playerTag}'!");
            }

            // Setup audio
            if (_openSound != null || _closeSound != null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1f;
                _audioSource.volume = _audioVolume;
            }

            Debug.Log($"[ProximitySlidingDoor3Panel] Door '{gameObject.name}' initialized. Detection range: {_detectionRange} units");
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
                    Debug.Log($"[ProximitySlidingDoor3Panel] Found player: {playerObject.name}");
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

            // Determine target positions based on whether door should be open
            Vector3 leftTarget = playerInRange ? _leftDoorOpenPosition : _leftDoorClosedPosition;
            Vector3 rightTarget = playerInRange ? _rightDoorOpenPosition : _rightDoorClosedPosition;
            Vector3 topTarget = playerInRange ? _topDoorOpenPosition : _topDoorClosedPosition;

            // Smoothly move all door panels
            _leftDoorPanel.localPosition = Vector3.Lerp(
                _leftDoorPanel.localPosition,
                leftTarget,
                Time.deltaTime * _slideSpeed
            );

            _rightDoorPanel.localPosition = Vector3.Lerp(
                _rightDoorPanel.localPosition,
                rightTarget,
                Time.deltaTime * _slideSpeed
            );

            _topDoorPanel.localPosition = Vector3.Lerp(
                _topDoorPanel.localPosition,
                topTarget,
                Time.deltaTime * _slideSpeed
            );

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
                Debug.Log($"[ProximitySlidingDoor3Panel] Door '{gameObject.name}' opening");
            }
            // Player just left range - start closing
            else if (!playerInRange && _isOpen)
            {
                _isOpen = false;
                PlaySound(_closeSound);
                Debug.Log($"[ProximitySlidingDoor3Panel] Door '{gameObject.name}' closing");
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

            // Draw slide direction arrows for side panels
            if (_leftDoorPanel != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 start = Application.isPlaying ? _leftDoorClosedPosition : _leftDoorPanel.position;
                Vector3 end = start + (-_sideSlideDirection.normalized * _sideSlideDistance);
                Gizmos.DrawLine(start, end);
                Gizmos.DrawSphere(end, 0.15f);
            }

            if (_rightDoorPanel != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 start = Application.isPlaying ? _rightDoorClosedPosition : _rightDoorPanel.position;
                Vector3 end = start + (_sideSlideDirection.normalized * _sideSlideDistance);
                Gizmos.DrawLine(start, end);
                Gizmos.DrawSphere(end, 0.15f);
            }

            // Draw slide direction arrow for top panel
            if (_topDoorPanel != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 start = Application.isPlaying ? _topDoorClosedPosition : _topDoorPanel.position;
                Vector3 end = start + (_topSlideDirection.normalized * _topSlideDistance);
                Gizmos.DrawLine(start, end);
                Gizmos.DrawSphere(end, 0.15f);
            }
        }
    }
}
