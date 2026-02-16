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

        [Header("Physics")]
        [Tooltip("Center of mass offset (adjusts rotation pivot point) - Move backward on Z to rotate around center")]
        [SerializeField] private Vector3 _centerOfMassOffset = new Vector3(0, 0, -25);

        [Header("Camera")]
        [Tooltip("Camera that follows this ship (assign your main camera here)")]
        [SerializeField] private Camera _shipCamera;

        [Tooltip("Use first-person cockpit view (true) or third-person view (false)")]
        [SerializeField] private bool _useFirstPersonView = true;

        [Tooltip("Cockpit camera position (first-person view) - position relative to ship")]
        [SerializeField] private Vector3 _cockpitCameraPosition = new Vector3(0, 1.5f, 2f);

        [Tooltip("Cockpit camera rotation offset")]
        [SerializeField] private Vector3 _cockpitCameraRotation = Vector3.zero;

        [Tooltip("Camera offset from ship (third-person view)")]
        [SerializeField] private Vector3 _cameraOffset = new Vector3(0, 10, -100);

        [Tooltip("How smoothly the camera follows the ship (third-person only)")]
        [SerializeField] private float _cameraSmoothing = 5f;

        [Header("Ship State")]
        [Tooltip("Is this ship currently being piloted?")]
        [SerializeField] private bool _isActive = false;

        [Header("Thruster Effects")]
        [Tooltip("Thruster particle systems (assign FX_Flame_Booster_Round prefabs)")]
        [SerializeField] private ParticleSystem[] _thrusterEffects;

        [Tooltip("Minimum speed before thrusters activate (0-1, percentage of max speed)")]
        [SerializeField] private float _thrusterActivationThreshold = 0.1f;

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
        private float _currentSpeed = 0f;         // Current forward/backward speed
        private float _currentStrafeSpeed = 0f;   // Current left/right speed (with momentum)
        private float _currentVerticalSpeed = 0f; // Current up/down speed (with momentum)
        private float _pitch = 0f;                // Up/down rotation
        private float _yaw = 0f;                  // Left/right rotation

        // Free look variables
        private float _freeLookPitch = 0f;        // Camera free look up/down
        private float _freeLookYaw = 0f;          // Camera free look left/right
        private bool _isFreeLooking = false;      // Is Alt held?

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
            _rigidbody.isKinematic = true; // Start kinematic until someone pilots it
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate; // Smooth movement
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous; // Better collision detection
            _rigidbody.linearDamping = 0f; // No drag for tight control
            _rigidbody.angularDamping = 5f; // High rotation drag for stable flight
            _rigidbody.mass = 1000f; // Heavy ship = less bouncy

            // Set center of mass so ship rotates around its center, not the nose
            _rigidbody.centerOfMass = _centerOfMassOffset;
            Debug.Log($"[ShipController] Center of mass set to: {_centerOfMassOffset}");

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
            // Debug every 50 frames to see if Update is running
            if (Time.frameCount % 50 == 0)
            {
                Debug.Log($"[ShipController] Update() on '{gameObject.name}' (Instance ID: {GetInstanceID()}) - _isActive: {_isActive}");
            }

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
            if (Time.frameCount % 50 == 0)
            {
                Debug.Log($"[ShipController] LateUpdate() - _isActive: {_isActive}");
            }

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

            // DEBUG: Check rigidbody constraints every frame
            if (_rigidbody != null && Time.frameCount % 50 == 0) // Log every 50 frames to avoid spam
            {
                Debug.Log($"[ShipController] Rigidbody constraints: {_rigidbody.constraints}, isKinematic: {_rigidbody.isKinematic}");
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
            // Check if Alt is held for free look
            Keyboard keyboard = Keyboard.current;
            _isFreeLooking = keyboard != null && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed);

            // Get mouse delta from new Input System
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            float mouseX = mouseDelta.x * _mouseSensitivity * 0.1f; // Scale down for smoother control
            float mouseY = mouseDelta.y * _mouseSensitivity * 0.1f;

            if (_isFreeLooking)
            {
                // Free look mode - apply mouse to camera offset only
                _freeLookYaw += mouseX;
                _freeLookPitch -= mouseY;

                // Clamp free look to prevent extreme angles
                _freeLookPitch = Mathf.Clamp(_freeLookPitch, -80f, 80f);
                _freeLookYaw = Mathf.Clamp(_freeLookYaw, -120f, 120f);
            }
            else
            {
                // Normal mode - apply mouse to ship rotation
                _yaw += mouseX;
                _pitch -= mouseY; // Negative because mouse up = pitch up

                // Clamp pitch to prevent flipping upside down too much
                _pitch = Mathf.Clamp(_pitch, -80f, 80f);

                // Smoothly reset free look when Alt released
                _freeLookPitch = Mathf.Lerp(_freeLookPitch, 0f, Time.deltaTime * 5f);
                _freeLookYaw = Mathf.Lerp(_freeLookYaw, 0f, Time.deltaTime * 5f);
            }
        }

        /// <summary>
        /// Applies rotation to the ship based on mouse input
        /// </summary>
        private void ApplyRotation()
        {
            // Create rotation from pitch and yaw
            Quaternion targetRotation = Quaternion.Euler(_pitch, _yaw, 0f);

            // Smoothly rotate TRANSFORM (ManualMovingPlatform syncs to rigidbody)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.fixedDeltaTime * _turnSpeed
            );
        }

        /// <summary>
        /// Handles all ship movement based on keyboard input
        /// </summary>
        private void HandleMovement()
        {
            // Get input from keyboard using new Input System
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                Debug.LogWarning("[ShipController] Keyboard.current is NULL!");
                return;
            }

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

            // DEBUG: Log input detection
            if (forwardInput != 0 || strafeInput != 0 || verticalInput != 0)
            {
                Debug.Log($"[ShipController] INPUT DETECTED! Forward: {forwardInput}, Strafe: {strafeInput}, Vertical: {verticalInput}");
            }
            if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed) verticalInput = -1f;

            // Calculate forward/backward movement with acceleration
            if (forwardInput > 0)
            {
                // Accelerate forward
                float oldSpeed = _currentSpeed;
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, _maxSpeed, _acceleration * Time.fixedDeltaTime);
                Debug.Log($"[ShipController] ACCELERATING: oldSpeed={oldSpeed}, newSpeed={_currentSpeed}, maxSpeed={_maxSpeed}, accel={_acceleration}, dt={Time.fixedDeltaTime}");
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

            // Calculate strafe movement with acceleration (same feel as forward/backward)
            if (strafeInput > 0)
            {
                // Accelerate right
                _currentStrafeSpeed = Mathf.MoveTowards(_currentStrafeSpeed, _strafeSpeed, _acceleration * Time.fixedDeltaTime);
            }
            else if (strafeInput < 0)
            {
                // Accelerate left
                _currentStrafeSpeed = Mathf.MoveTowards(_currentStrafeSpeed, -_strafeSpeed, _acceleration * Time.fixedDeltaTime);
            }
            else
            {
                // Decelerate when no input
                _currentStrafeSpeed = Mathf.MoveTowards(_currentStrafeSpeed, 0f, _deceleration * Time.fixedDeltaTime);
            }

            // Calculate vertical movement with acceleration (same feel as forward/backward)
            if (verticalInput > 0)
            {
                // Accelerate up
                _currentVerticalSpeed = Mathf.MoveTowards(_currentVerticalSpeed, _verticalSpeed, _acceleration * Time.fixedDeltaTime);
            }
            else if (verticalInput < 0)
            {
                // Accelerate down
                _currentVerticalSpeed = Mathf.MoveTowards(_currentVerticalSpeed, -_verticalSpeed, _acceleration * Time.fixedDeltaTime);
            }
            else
            {
                // Decelerate when no input
                _currentVerticalSpeed = Mathf.MoveTowards(_currentVerticalSpeed, 0f, _deceleration * Time.fixedDeltaTime);
            }

            // Calculate movement vector in ship's local space
            Vector3 movement = Vector3.zero;

            // Forward/backward (with momentum)
            movement += transform.forward * _currentSpeed;

            // Strafe left/right (now with momentum!)
            movement += transform.right * _currentStrafeSpeed;

            // Up/down (now with momentum!)
            movement += transform.up * _currentVerticalSpeed;

            // Apply movement
            Debug.Log($"[ShipController] Movement vector: {movement}, magnitude: {movement.magnitude}");

            // Move the TRANSFORM directly
            // ManualMovingPlatform will sync Transform -> Rigidbody
            // This way Opsive's character controller sees it as a moving platform
            Vector3 oldPosition = transform.position;
            Vector3 newPosition = transform.position + movement * Time.fixedDeltaTime;
            transform.position = newPosition;

            float actualMovement = Vector3.Distance(oldPosition, transform.position);
            Debug.Log($"[ShipController] Transform moved: oldPos={oldPosition}, newPos={newPosition}, actualMovement={actualMovement}");

            // Update engine audio based on speed
            UpdateEngineAudio();

            // Update thruster particle effects based on movement
            UpdateThrusterEffects();
        }

        /// <summary>
        /// Updates camera to follow the ship
        /// </summary>
        private void UpdateCamera()
        {
            Debug.Log($"[ShipController] UpdateCamera() called! _shipCamera: {(_shipCamera != null ? _shipCamera.name : "NULL")}, _useFirstPersonView: {_useFirstPersonView}");

            if (_shipCamera == null)
            {
                Debug.LogWarning("[ShipController] Ship camera is null! Cannot update camera position.");
                return;
            }

            if (_useFirstPersonView)
            {
                // First-person cockpit view
                // Position camera at cockpit position (relative to ship)
                Vector3 cockpitPosition = transform.position + transform.TransformDirection(_cockpitCameraPosition);
                _shipCamera.transform.position = cockpitPosition;

                // Camera rotates with ship + cockpit rotation offset + free look offset
                Quaternion freeLookRotation = Quaternion.Euler(_freeLookPitch, _freeLookYaw, 0f);
                Quaternion cockpitRotation = transform.rotation * Quaternion.Euler(_cockpitCameraRotation) * freeLookRotation;
                _shipCamera.transform.rotation = cockpitRotation;
            }
            else
            {
                // Third-person view (behind ship)

                if (_isFreeLooking)
                {
                    // Free look - orbit camera around ship
                    // Create rotation from free look angles (relative to world space)
                    Quaternion orbitRotation = Quaternion.Euler(_freeLookPitch, transform.eulerAngles.y + _freeLookYaw, 0f);

                    // Calculate camera position - orbit around ship at same distance
                    float distance = _cameraOffset.magnitude;
                    Vector3 orbitOffset = orbitRotation * Vector3.back * distance;
                    _shipCamera.transform.position = transform.position + orbitOffset;

                    // Camera looks at ship
                    _shipCamera.transform.LookAt(transform.position);
                }
                else
                {
                    // Normal third-person - camera follows ship rotation
                    Vector3 targetPosition = transform.position + transform.TransformDirection(_cameraOffset);
                    _shipCamera.transform.position = targetPosition;

                    // Camera rotates with the ship
                    _shipCamera.transform.rotation = transform.rotation;
                }
            }
        }

        /// <summary>
        /// Public method to activate ship (called when player enters)
        /// </summary>
        public void EnterShip(Camera playerCamera)
        {
            Debug.Log($"[ShipController] EnterShip() called on '{gameObject.name}' (Instance ID: {GetInstanceID()})");
            Debug.Log($"[ShipController] BEFORE: _isActive = {_isActive}");

            _isActive = true;
            _shipCamera = playerCamera;

            Debug.Log($"[ShipController] AFTER: _isActive = {_isActive} (Instance ID: {GetInstanceID()})");

            // Keep rigidbody kinematic (required due to concave mesh colliders in ship interior)
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
                _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                Debug.Log("[ShipController] Rigidbody configured for flight");
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

            Debug.Log($"[ShipController] Player entered ship '{gameObject.name}' - IsActive is now: {_isActive}");
        }

        /// <summary>
        /// Public method to deactivate ship (called when player exits)
        /// </summary>
        public void ExitShip()
        {
            Debug.Log($"[ShipController] ExitShip() called on '{gameObject.name}'");
            Debug.Log($"[ShipController] BEFORE: _isActive = {_isActive}");

            _isActive = false;

            Debug.Log($"[ShipController] AFTER: _isActive = {_isActive}");

            // Stop all movement
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _currentSpeed = 0f;
                _currentStrafeSpeed = 0f;
                _currentVerticalSpeed = 0f;

                // Make ship kinematic so it doesn't drift when parked
                _rigidbody.isKinematic = true;
                Debug.Log("[ShipController] Set rigidbody to kinematic (ship parked)");
            }

            // Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Stop ship audio
            StopShipAudio();

            // Stop thruster effects
            StopThrusterEffects();

            Debug.Log($"[ShipController] Player exited ship '{gameObject.name}' - IsActive is now: {_isActive}");
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
        /// Updates thruster particle effects based on ship movement
        /// </summary>
        private void UpdateThrusterEffects()
        {
            if (!_isActive || _thrusterEffects == null || _thrusterEffects.Length == 0)
            {
                return;
            }

            // Calculate total movement intensity (0 to 1)
            float forwardIntensity = Mathf.Abs(_currentSpeed) / _maxSpeed;
            float strafeIntensity = Mathf.Abs(_currentStrafeSpeed) / _strafeSpeed;
            float verticalIntensity = Mathf.Abs(_currentVerticalSpeed) / _verticalSpeed;

            // Use the maximum intensity of all movement axes
            float totalIntensity = Mathf.Max(forwardIntensity, strafeIntensity, verticalIntensity);

            // Activate/deactivate thrusters based on movement
            bool shouldBeActive = totalIntensity > _thrusterActivationThreshold;

            foreach (ParticleSystem thruster in _thrusterEffects)
            {
                if (thruster == null) continue;

                if (shouldBeActive)
                {
                    // Start particle system if not playing
                    if (!thruster.isPlaying)
                    {
                        thruster.Play();
                    }

                    // Adjust emission rate based on intensity
                    var emission = thruster.emission;
                    emission.rateOverTimeMultiplier = Mathf.Lerp(10f, 100f, totalIntensity);

                    // Adjust size based on intensity
                    var main = thruster.main;
                    main.startSizeMultiplier = Mathf.Lerp(0.5f, 1.5f, totalIntensity);
                }
                else
                {
                    // Stop particle system if playing
                    if (thruster.isPlaying)
                    {
                        thruster.Stop();
                    }
                }
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
        /// Stops all thruster particle effects when exiting ship
        /// </summary>
        private void StopThrusterEffects()
        {
            if (_thrusterEffects == null) return;

            foreach (ParticleSystem thruster in _thrusterEffects)
            {
                if (thruster != null && thruster.isPlaying)
                {
                    thruster.Stop();
                }
            }

            Debug.Log("[ShipController] Thruster effects stopped");
        }

        /// <summary>
        /// Draw debug information in Scene view
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw forward direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 5f);

            // Draw center of mass (rotation pivot point)
            Gizmos.color = Color.red;
            Vector3 centerOfMass = transform.position + transform.TransformDirection(_centerOfMassOffset);
            Gizmos.DrawWireSphere(centerOfMass, 2f); // Red sphere shows rotation center
            Gizmos.DrawLine(transform.position, centerOfMass);

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
