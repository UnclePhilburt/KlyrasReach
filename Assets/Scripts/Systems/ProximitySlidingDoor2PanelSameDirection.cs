/*
 * 2-Panel Same-Direction Proximity-Based Sliding Door for Klyra's Reach
 *
 * PURPOSE:
 * Sliding door with 2 panels that BOTH slide in the same direction
 * (e.g., both panels slide left together)
 *
 * HOW TO USE:
 * 1. Attach to your door GameObject
 * 2. Assign both door panels in Inspector
 * 3. Press Play - it will automatically find and track the player
 */

using UnityEngine;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Sliding door with 2 panels that both slide in the same direction
    /// </summary>
    public class ProximitySlidingDoor2PanelSameDirection : MonoBehaviour
    {
        [Header("Door Panel References")]
        [Tooltip("First door panel")]
        [SerializeField] private Transform _doorPanel1;

        [Tooltip("Second door panel")]
        [SerializeField] private Transform _doorPanel2;

        [Header("Door Behavior Settings")]
        [Tooltip("How far the door panels slide when opening")]
        [SerializeField] private float _slideDistance = 5f;

        [Tooltip("Speed of the door sliding animation")]
        [SerializeField] private float _slideSpeed = 2f;

        [Tooltip("How close the player needs to be to open the door")]
        [SerializeField] private float _detectionRange = 3f;

        [Tooltip("Direction both panels slide: X=left/right, Y=up/down, Z=forward/back")]
        [SerializeField] private Vector3 _slideDirection = Vector3.left;

        [Header("Player Detection")]
        [Tooltip("Tag to identify the player GameObject")]
        [SerializeField] private string _playerTag = "Player";

        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip _openSound;
        [SerializeField] private AudioClip _closeSound;

        [Tooltip("Volume for door sounds (0-1)")]
        [SerializeField] [Range(0f, 1f)] private float _audioVolume = 0.5f;

        // Private variables
        private Vector3 _panel1ClosedPosition;
        private Vector3 _panel1OpenPosition;
        private Vector3 _panel2ClosedPosition;
        private Vector3 _panel2OpenPosition;

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
            if (_doorPanel1 == null || _doorPanel2 == null)
            {
                Debug.LogError("[ProximitySlidingDoor2PanelSameDirection] Missing door panels! Please assign both panels.");
                enabled = false;
                return;
            }

            // Store the door's starting world position for distance checks
            _detectionPoint = transform.position;

            // Store closed positions and calculate open positions
            // Both panels slide in the SAME direction
            _panel1ClosedPosition = _doorPanel1.localPosition;
            _panel1OpenPosition = _panel1ClosedPosition + (_slideDirection.normalized * _slideDistance);

            _panel2ClosedPosition = _doorPanel2.localPosition;
            _panel2OpenPosition = _panel2ClosedPosition + (_slideDirection.normalized * _slideDistance);

            // Find the player
            GameObject playerObject = GameObject.FindGameObjectWithTag(_playerTag);
            if (playerObject != null)
            {
                _playerTransform = playerObject.transform;
                Debug.Log($"[ProximitySlidingDoor2PanelSameDirection] Found player: {playerObject.name}");
            }
            else
            {
                Debug.LogError($"[ProximitySlidingDoor2PanelSameDirection] Could not find player with tag '{_playerTag}'!");
            }

            // Setup audio
            if (_openSound != null || _closeSound != null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1f;
                _audioSource.volume = _audioVolume;
            }

            Debug.Log($"[ProximitySlidingDoor2PanelSameDirection] Door '{gameObject.name}' initialized. Detection range: {_detectionRange} units");
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
                    Debug.Log($"[ProximitySlidingDoor2PanelSameDirection] Found player: {playerObject.name}");
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
            Vector3 panel1Target = playerInRange ? _panel1OpenPosition : _panel1ClosedPosition;
            Vector3 panel2Target = playerInRange ? _panel2OpenPosition : _panel2ClosedPosition;

            // Smoothly move both door panels
            _doorPanel1.localPosition = Vector3.Lerp(
                _doorPanel1.localPosition,
                panel1Target,
                Time.deltaTime * _slideSpeed
            );

            _doorPanel2.localPosition = Vector3.Lerp(
                _doorPanel2.localPosition,
                panel2Target,
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
                Debug.Log($"[ProximitySlidingDoor2PanelSameDirection] Door '{gameObject.name}' opening");
            }
            // Player just left range - start closing
            else if (!playerInRange && _isOpen)
            {
                _isOpen = false;
                PlaySound(_closeSound);
                Debug.Log($"[ProximitySlidingDoor2PanelSameDirection] Door '{gameObject.name}' closing");
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

            // Draw slide direction arrows for both panels
            if (_doorPanel1 != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 start = Application.isPlaying ? _panel1ClosedPosition : _doorPanel1.position;
                Vector3 end = start + (_slideDirection.normalized * _slideDistance);
                Gizmos.DrawLine(start, end);
                Gizmos.DrawSphere(end, 0.15f);
            }

            if (_doorPanel2 != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 start = Application.isPlaying ? _panel2ClosedPosition : _doorPanel2.position;
                Vector3 end = start + (_slideDirection.normalized * _slideDistance);
                Gizmos.DrawLine(start, end);
                Gizmos.DrawSphere(end, 0.15f);
            }
        }
    }
}
