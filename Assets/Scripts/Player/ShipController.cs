/*
 * Basic Ship Flight Controller for Klyra's Reach
 *
 * PURPOSE:
 * Arcade-style ship flight controls that can be attached to any ship prefab.
 * Each ship can have different speed, handling, and acceleration values.
 *
 * CONTROLS:
 * W/S - Forward/Backward
 * A/D - Strafe Left/Right
 * Space/Ctrl - Up/Down
 * Mouse - Look/Aim direction
 *
 * HOW TO USE:
 * 1. Attach this script to a ship GameObject
 * 2. Adjust flight characteristics in Inspector (speed, turn rate, etc.)
 * 3. Assign a camera to follow the ship
 * 4. Player can enter with "F" key when nearby
 */

using UnityEngine;
using UnityEngine.InputSystem;

namespace KlyrasReach.Player
{
    /// <summary>
    /// Handles arcade-style ship flight controls
    /// Can be customized per ship type for different flight characteristics
    /// </summary>
    public class ShipController : MonoBehaviour
    {
        [Header("Flight Characteristics")]
        [Tooltip("Maximum forward speed")]
        [SerializeField] private float _maxSpeed = 50f;

        [Tooltip("Maximum reverse speed")]
        [SerializeField] private float _maxReverseSpeed = 20f;

        [Tooltip("How quickly the ship accelerates")]
        [SerializeField] private float _acceleration = 10f;

        [Tooltip("How quickly the ship slows down")]
        [SerializeField] private float _deceleration = 5f;

        [Tooltip("Speed when strafing left/right")]
        [SerializeField] private float _strafeSpeed = 25f;

        [Tooltip("Speed when moving up/down")]
        [SerializeField] private float _verticalSpeed = 20f;

        [Header("Turn Rate")]
        [Tooltip("How fast the ship turns with mouse (pitch/yaw)")]
        [SerializeField] private float _turnSpeed = 2f;

        [Tooltip("Mouse sensitivity for looking")]
        [SerializeField] private float _mouseSensitivity = 3f;

        [Header("Camera")]
        [Tooltip("Camera that follows this ship (assign your main camera here)")]
        [SerializeField] private Camera _shipCamera;

        [Tooltip("Camera offset from ship (third-person view)")]
        [SerializeField] private Vector3 _cameraOffset = new Vector3(0, 2, -10);

        [Tooltip("How smoothly the camera follows the ship")]
        [SerializeField] private float _cameraSmoothing = 5f;

        [Header("Ship State")]
        [Tooltip("Is this ship currently being piloted?")]
        [SerializeField] private bool _isActive = false;

        [Header("Audio")]
        [Tooltip("Continuous engine hum sound (looping)")]
        [SerializeField] private AudioClip _engineHumSound;

        [Tooltip("Engine thrust sound that changes with speed (looping)")]
        [SerializeField] private AudioClip _engineThrustSound;

        [Tooltip("Ambient space sound (looping)")]
        [SerializeField] private AudioClip _spaceAmbienceSound;

        [Tooltip("Volume for engine hum (0-1)")]
        [SerializeField] private float _engineHumVolume = 0.3f;

        [Tooltip("Volume for engine thrust (0-1)")]
        [SerializeField] private float _engineThrustVolume = 0.5f;

        [Tooltip("Volume for space ambience (0-1)")]
        [SerializeField] private float _spaceAmbienceVolume = 0.2f;

        // Private movement variables
        private Rigidbody _rigidbody;
        private float _currentSpeed = 0f;
        private float _pitch = 0f; // Up/down rotation
        private float _yaw = 0f;   // Left/right rotation

        // Audio sources
        private AudioSource _engineHumSource;
        private AudioSource _engineThrustSource;
        private AudioSource _spaceAmbienceSource;

        /// <summary>
        /// Public property to check if ship is being piloted
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// Initialize ship components
        /// </summary>
        private void Awake()
        {
            // Get or add Rigidbody for physics-based movement
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
            }

            // Configure rigidbody for ship flight
            _rigidbody.useGravity = false; // No gravity in space!
            _rigidbody.isKinematic = false; // Non-kinematic for physical collisions
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate; // Smooth movement
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous; // Better collision detection
            _rigidbody.linearDamping = 0f; // No drag for tight control
            _rigidbody.angularDamping = 5f; // High rotation drag for stable flight
            _rigidbody.mass = 1000f; // Heavy ship = less bouncy

            // Freeze rotation to prevent tumbling on collision
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            // Start inactive until player enters
            _isActive = false;

            // Initialize audio sources
            InitializeAudio();

            Debug.Log($"[ShipController] Ship '{gameObject.name}' initialized. Max Speed: {_maxSpeed}");
        }

        /// <summary>
        /// Called every frame - handles input
        /// </summary>
        private void Update()
        {
            // Only process input if ship is active (player is piloting)
            if (!_isActive)
            {
                return;
            }

            // Handle mouse look
            HandleMouseLook();
        }

        /// <summary>
        /// Called after all Updates - best for camera following to avoid jitter
        /// </summary>
        private void LateUpdate()
        {
            if (!_isActive)
            {
                return;
            }

            // Update camera position
            UpdateCamera();
        }

        /// <summary>
        /// Called at fixed intervals - handles physics-based movement
        /// </summary>
        private void FixedUpdate()
        {
            if (!_isActive)
            {
                return;
            }

            // Handle all flight movement
            HandleMovement();

            // Apply rotation based on mouse look
            ApplyRotation();
        }

        /// <summary>
        /// Handles mouse input for pitch and yaw
        /// </summary>
        private void HandleMouseLook()
        {
            // Get mouse delta from new Input System
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            float mouseX = mouseDelta.x * _mouseSensitivity * 0.1f; // Scale down for smoother control
            float mouseY = mouseDelta.y * _mouseSensitivity * 0.1f;

            // Apply to pitch and yaw
            _yaw += mouseX;
            _pitch -= mouseY; // Negative because mouse up = pitch up

            // Clamp pitch to prevent flipping upside down too much
            _pitch = Mathf.Clamp(_pitch, -80f, 80f);
        }

        /// <summary>
        /// Applies rotation to the ship based on mouse input
        /// </summary>
        private void ApplyRotation()
        {
            // Create rotation from pitch and yaw
            Quaternion targetRotation = Quaternion.Euler(_pitch, _yaw, 0f);

            // Smoothly rotate ship toward target
            _rigidbody.MoveRotation(Quaternion.Slerp(
                _rigidbody.rotation,
                targetRotation,
                Time.fixedDeltaTime * _turnSpeed
            ));
        }

        /// <summary>
        /// Handles all ship movement based on keyboard input
        /// </summary>
        private void HandleMovement()
        {
            // Get input from keyboard using new Input System
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return; // No keyboard connected

            float forwardInput = 0f;   // W/S
            float strafeInput = 0f;    // A/D
            float verticalInput = 0f;  // Space/Ctrl

            // Forward/Backward (W/S)
            if (keyboard.wKey.isPressed) forwardInput += 1f;
            if (keyboard.sKey.isPressed) forwardInput -= 1f;

            // Strafe (A/D)
            if (keyboard.dKey.isPressed) strafeInput += 1f;
            if (keyboard.aKey.isPressed) strafeInput -= 1f;

            // Vertical (Space/Ctrl)
            if (keyboard.spaceKey.isPressed) verticalInput = 1f;
            if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed) verticalInput = -1f;

            // Calculate forward/backward movement
            if (forwardInput > 0)
            {
                // Accelerate forward
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, _maxSpeed, _acceleration * Time.fixedDeltaTime);
            }
            else if (forwardInput < 0)
            {
                // Accelerate backward (reverse)
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, -_maxReverseSpeed, _acceleration * Time.fixedDeltaTime);
            }
            else
            {
                // Decelerate when no input
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, _deceleration * Time.fixedDeltaTime);
            }

            // Calculate movement vector in ship's local space
            Vector3 movement = Vector3.zero;

            // Forward/backward
            movement += transform.forward * _currentSpeed;

            // Strafe left/right
            movement += transform.right * strafeInput * _strafeSpeed;

            // Up/down
            movement += transform.up * verticalInput * _verticalSpeed;

            // Apply movement as force to rigidbody (allows physics collisions)
            // Use AddForce with VelocityChange mode for direct control
            _rigidbody.linearVelocity = movement;

            // Update engine audio based on speed
            UpdateEngineAudio();
        }

        /// <summary>
        /// Updates camera to follow the ship
        /// </summary>
        private void UpdateCamera()
        {
            if (_shipCamera == null)
            {
                Debug.LogWarning("[ShipController] Ship camera is null! Cannot update camera position.");
                return;
            }

            // Calculate target camera position (behind and above ship)
            // No lerp needed - rigidbody interpolation already smooths movement
            Vector3 targetPosition = transform.position + transform.TransformDirection(_cameraOffset);
            _shipCamera.transform.position = targetPosition;

            // Make camera look at the ship
            _shipCamera.transform.LookAt(transform.position + transform.forward * 5f);
        }

        /// <summary>
        /// Public method to activate ship (called when player enters)
        /// </summary>
        public void EnterShip(Camera playerCamera)
        {
            _isActive = true;
            _shipCamera = playerCamera;

            // Re-enable physics (in case it was disabled by landing pad)
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
            }

            // Lock cursor for flight controls
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Initialize rotation to current ship rotation
            Vector3 currentRotation = transform.eulerAngles;
            _pitch = currentRotation.x;
            _yaw = currentRotation.y;

            // Start ship audio
            StartShipAudio();

            Debug.Log($"[ShipController] Player entered ship '{gameObject.name}'");
        }

        /// <summary>
        /// Public method to deactivate ship (called when player exits)
        /// </summary>
        public void ExitShip()
        {
            _isActive = false;

            // Stop all movement
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _currentSpeed = 0f;

            // Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Stop ship audio
            StopShipAudio();

            Debug.Log($"[ShipController] Player exited ship '{gameObject.name}'");
        }

        /// <summary>
        /// Initializes audio sources for ship sounds
        /// </summary>
        private void InitializeAudio()
        {
            // Create engine hum audio source
            if (_engineHumSound != null)
            {
                _engineHumSource = gameObject.AddComponent<AudioSource>();
                _engineHumSource.clip = _engineHumSound;
                _engineHumSource.loop = true;
                _engineHumSource.playOnAwake = false;
                _engineHumSource.volume = 0f; // Start at 0, will fade in when ship activates
                _engineHumSource.spatialBlend = 0f; // 2D sound (always hear it in cockpit)
                Debug.Log("[ShipController] Engine hum audio source created");
            }

            // Create engine thrust audio source
            if (_engineThrustSound != null)
            {
                _engineThrustSource = gameObject.AddComponent<AudioSource>();
                _engineThrustSource.clip = _engineThrustSound;
                _engineThrustSource.loop = true;
                _engineThrustSource.playOnAwake = false;
                _engineThrustSource.volume = 0f; // Start at 0, will change with speed
                _engineThrustSource.pitch = 0.8f; // Lower pitch at rest
                _engineThrustSource.spatialBlend = 0f; // 2D sound
                Debug.Log("[ShipController] Engine thrust audio source created");
            }

            // Create space ambience audio source
            if (_spaceAmbienceSound != null)
            {
                _spaceAmbienceSource = gameObject.AddComponent<AudioSource>();
                _spaceAmbienceSource.clip = _spaceAmbienceSound;
                _spaceAmbienceSource.loop = true;
                _spaceAmbienceSource.playOnAwake = false;
                _spaceAmbienceSource.volume = 0f; // Start at 0, will fade in when ship activates
                _spaceAmbienceSource.spatialBlend = 0f; // 2D sound
                Debug.Log("[ShipController] Space ambience audio source created");
            }
        }

        /// <summary>
        /// Updates engine audio based on current speed
        /// </summary>
        private void UpdateEngineAudio()
        {
            if (!_isActive) return;

            // Calculate speed percentage (0 to 1)
            float speedPercent = Mathf.Abs(_currentSpeed) / _maxSpeed;

            // Update engine thrust volume and pitch based on speed
            if (_engineThrustSource != null)
            {
                // Volume increases with speed
                _engineThrustSource.volume = Mathf.Lerp(
                    _engineThrustVolume * 0.3f,  // Minimum volume (idle)
                    _engineThrustVolume,          // Maximum volume (full speed)
                    speedPercent
                );

                // Pitch increases with speed (0.8 to 1.4)
                _engineThrustSource.pitch = Mathf.Lerp(0.8f, 1.4f, speedPercent);
            }
        }

        /// <summary>
        /// Starts all ship audio when entering ship
        /// </summary>
        private void StartShipAudio()
        {
            if (_engineHumSource != null && !_engineHumSource.isPlaying)
            {
                _engineHumSource.volume = _engineHumVolume;
                _engineHumSource.Play();
                Debug.Log("[ShipController] Engine hum started");
            }

            if (_engineThrustSource != null && !_engineThrustSource.isPlaying)
            {
                _engineThrustSource.volume = _engineThrustVolume * 0.3f; // Start low
                _engineThrustSource.Play();
                Debug.Log("[ShipController] Engine thrust started");
            }

            if (_spaceAmbienceSource != null && !_spaceAmbienceSource.isPlaying)
            {
                _spaceAmbienceSource.volume = _spaceAmbienceVolume;
                _spaceAmbienceSource.Play();
                Debug.Log("[ShipController] Space ambience started");
            }
        }

        /// <summary>
        /// Stops all ship audio when exiting ship
        /// </summary>
        private void StopShipAudio()
        {
            if (_engineHumSource != null && _engineHumSource.isPlaying)
            {
                _engineHumSource.Stop();
                Debug.Log("[ShipController] Engine hum stopped");
            }

            if (_engineThrustSource != null && _engineThrustSource.isPlaying)
            {
                _engineThrustSource.Stop();
                Debug.Log("[ShipController] Engine thrust stopped");
            }

            if (_spaceAmbienceSource != null && _spaceAmbienceSource.isPlaying)
            {
                _spaceAmbienceSource.Stop();
                Debug.Log("[ShipController] Space ambience stopped");
            }
        }

        /// <summary>
        /// Draw debug information in Scene view
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw forward direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 5f);

            // Draw camera position preview
            if (_shipCamera != null || !Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Vector3 camPos = transform.position + transform.TransformDirection(_cameraOffset);
                Gizmos.DrawWireSphere(camPos, 0.5f);
                Gizmos.DrawLine(transform.position, camPos);
            }
        }
    }
}
