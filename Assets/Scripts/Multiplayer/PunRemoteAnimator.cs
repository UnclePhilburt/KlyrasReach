/*
 * PUN Remote Animator for Klyra's Reach
 *
 * PURPOSE:
 * Manually updates animator parameters for remote players based on networked velocity
 * This bypasses the need for UltimateCharacterLocomotionHandler on remote players
 *
 * CRITICAL: This script is DISABLED by default and only activates for remote players.
 * It does NOT interfere with local player animations which are handled by Opsive's system.
 */

using UnityEngine;
using Photon.Pun;
using Opsive.UltimateCharacterController.Character;

namespace KlyrasReach.Multiplayer
{
    /// <summary>
    /// Updates animator for remote players based on velocity
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class PunRemoteAnimator : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool _enableDebugLogging = true;
        [SerializeField] private float _debugLogInterval = 2f; // Log every 2 seconds

        private PhotonView _photonView;
        private Animator _animator;
        private AnimatorMonitor _animatorMonitor;

        // Animator parameter hashes (for performance)
        private int _horizontalMovementHash;
        private int _forwardMovementHash;
        private int _movingHash;
        private int _speedHash;

        // Track position for velocity calculation
        private Vector3 _lastPosition;
        private Vector3 _currentVelocity;

        // Debug tracking
        private float _lastDebugLogTime;
        private bool _isRemotePlayer;
        private bool _isInitialized;

        private void Awake()
        {
            _photonView = GetComponent<PhotonView>();

            Debug.Log($"[PunRemoteAnimator] Awake called on: {gameObject.name}, PhotonView IsMine: {_photonView.IsMine}");

            // Find animator on child object
            _animator = GetComponentInChildren<Animator>();

            if (_animator == null)
            {
                Debug.LogError($"[PunRemoteAnimator] No Animator found on children of {gameObject.name}!");
                enabled = false;
                return;
            }

            Debug.Log($"[PunRemoteAnimator] Found animator on: {_animator.gameObject.name}");

            // Try to find AnimatorMonitor (Opsive component)
            _animatorMonitor = GetComponentInChildren<AnimatorMonitor>();
            if (_animatorMonitor != null)
            {
                Debug.Log($"[PunRemoteAnimator] Found AnimatorMonitor on: {_animatorMonitor.gameObject.name}");
            }

            // Cache animator parameter hashes
            _horizontalMovementHash = Animator.StringToHash("HorizontalMovement");
            _forwardMovementHash = Animator.StringToHash("ForwardMovement");
            _movingHash = Animator.StringToHash("Moving");
            _speedHash = Animator.StringToHash("Speed");

            _lastPosition = transform.position;
        }

        private void Start()
        {
            _isRemotePlayer = !_photonView.IsMine;

            if (_isRemotePlayer)
            {
                Debug.Log($"[PunRemoteAnimator] ========================================");
                Debug.Log($"[PunRemoteAnimator] ACTIVATED for REMOTE player: {gameObject.name}");
                Debug.Log($"[PunRemoteAnimator] Animator enabled: {_animator.enabled}");
                Debug.Log($"[PunRemoteAnimator] Animator GameObject active: {_animator.gameObject.activeInHierarchy}");
                Debug.Log($"[PunRemoteAnimator] Current animator state: {_animator.GetCurrentAnimatorStateInfo(0).fullPathHash}");

                // Log all animator parameters
                LogAnimatorParameters();

                Debug.Log($"[PunRemoteAnimator] ========================================");

                _isInitialized = true;
            }
            else
            {
                Debug.Log($"[PunRemoteAnimator] This is LOCAL player: {gameObject.name} - Script will NOT update animator");
                // Disable this script for local player (Opsive's system handles it)
                enabled = false;
            }
        }

        private void Update()
        {
            // Only run for remote players
            if (!_isRemotePlayer || !_isInitialized || _animator == null) return;

            UpdateAnimatorFromMovement();

            // Periodic debug logging
            if (_enableDebugLogging && Time.time - _lastDebugLogTime > _debugLogInterval)
            {
                LogCurrentState();
                _lastDebugLogTime = Time.time;
            }
        }

        private void UpdateAnimatorFromMovement()
        {
            // Calculate velocity from position change
            Vector3 currentPosition = transform.position;
            _currentVelocity = (currentPosition - _lastPosition) / Time.deltaTime;
            _lastPosition = currentPosition;

            // Convert world velocity to local space
            Vector3 localVelocity = transform.InverseTransformDirection(_currentVelocity);

            // Calculate movement values
            float horizontalMovement = localVelocity.x;
            float forwardMovement = localVelocity.z;
            float speed = _currentVelocity.magnitude;
            bool isMoving = speed > 0.1f;

            // Update animator parameters
            _animator.SetFloat(_horizontalMovementHash, horizontalMovement);
            _animator.SetFloat(_forwardMovementHash, forwardMovement);
            _animator.SetFloat(_speedHash, speed);
            _animator.SetBool(_movingHash, isMoving);
        }

        private void LogAnimatorParameters()
        {
            if (_animator == null) return;

            Debug.Log($"[PunRemoteAnimator] Checking animator parameters:");

            // Check each parameter
            foreach (AnimatorControllerParameter param in _animator.parameters)
            {
                string value = "";
                switch (param.type)
                {
                    case AnimatorControllerParameterType.Float:
                        value = _animator.GetFloat(param.nameHash).ToString("F3");
                        break;
                    case AnimatorControllerParameterType.Bool:
                        value = _animator.GetBool(param.nameHash).ToString();
                        break;
                    case AnimatorControllerParameterType.Int:
                        value = _animator.GetInteger(param.nameHash).ToString();
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        value = "(trigger)";
                        break;
                }
                Debug.Log($"[PunRemoteAnimator]   - {param.name} ({param.type}): {value}");
            }
        }

        private void LogCurrentState()
        {
            if (_animator == null) return;

            float horizontalMovement = _animator.GetFloat(_horizontalMovementHash);
            float forwardMovement = _animator.GetFloat(_forwardMovementHash);
            float speed = _animator.GetFloat(_speedHash);
            bool moving = _animator.GetBool(_movingHash);

            Debug.Log($"[PunRemoteAnimator] Current State for {gameObject.name}:");
            Debug.Log($"[PunRemoteAnimator]   Position: {transform.position}");
            Debug.Log($"[PunRemoteAnimator]   Velocity: {_currentVelocity.magnitude:F3} m/s");
            Debug.Log($"[PunRemoteAnimator]   HorizontalMovement: {horizontalMovement:F3}");
            Debug.Log($"[PunRemoteAnimator]   ForwardMovement: {forwardMovement:F3}");
            Debug.Log($"[PunRemoteAnimator]   Speed: {speed:F3}");
            Debug.Log($"[PunRemoteAnimator]   Moving: {moving}");
            Debug.Log($"[PunRemoteAnimator]   Current Animation: {_animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")} (Idle?)");
        }
    }
}
