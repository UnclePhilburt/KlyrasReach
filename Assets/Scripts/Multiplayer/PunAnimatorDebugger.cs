/*
 * PUN Animator Debugger for Klyra's Reach
 *
 * PURPOSE:
 * Monitors and logs animator state changes to help debug animation sync issues
 * This script helps identify whether:
 * 1. Local player's animator parameters are being set
 * 2. PunCharacterAnimatorMonitor is sending data over network
 * 3. Remote player's animator parameters are being received and applied
 */

using UnityEngine;
using Photon.Pun;
using Opsive.UltimateCharacterController.Character;

namespace KlyrasReach.Multiplayer
{
    /// <summary>
    /// Debug script to monitor animator parameter changes
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class PunAnimatorDebugger : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _logInterval = 3f; // Log every 3 seconds

        private PhotonView _photonView;
        private Animator _animator;
        private AnimatorMonitor _animatorMonitor;
        private bool _isLocalPlayer;

        // Track previous values to detect changes
        private float _lastHorizontalMovement;
        private float _lastForwardMovement;
        private float _lastSpeed;
        private bool _lastMoving;

        private float _lastLogTime;

        private void Awake()
        {
            _photonView = GetComponent<PhotonView>();
            _animator = GetComponentInChildren<Animator>();
            _animatorMonitor = GetComponentInChildren<AnimatorMonitor>();

            _isLocalPlayer = _photonView.IsMine;
        }

        private void Start()
        {
            if (!_enableLogging) return;

            string playerType = _isLocalPlayer ? "LOCAL" : "REMOTE";
            Debug.Log($"[PunAnimatorDebugger] ========================================");
            Debug.Log($"[PunAnimatorDebugger] Initialized for {playerType} player: {gameObject.name}");
            Debug.Log($"[PunAnimatorDebugger] Has Animator: {(_animator != null)}");
            Debug.Log($"[PunAnimatorDebugger] Has AnimatorMonitor: {(_animatorMonitor != null)}");

            if (_animator != null)
            {
                Debug.Log($"[PunAnimatorDebugger] Animator enabled: {_animator.enabled}");
                Debug.Log($"[PunAnimatorDebugger] Animator update mode: {_animator.updateMode}");
                Debug.Log($"[PunAnimatorDebugger] Animator parameter count: {_animator.parameterCount}");
            }

            Debug.Log($"[PunAnimatorDebugger] ========================================");
        }

        private void Update()
        {
            if (!_enableLogging || _animator == null) return;

            // Only log at intervals
            if (Time.time - _lastLogTime < _logInterval) return;
            _lastLogTime = Time.time;

            LogAnimatorState();
        }

        private void LogAnimatorState()
        {
            // Get current animator parameter values
            int horizontalHash = Animator.StringToHash("HorizontalMovement");
            int forwardHash = Animator.StringToHash("ForwardMovement");
            int speedHash = Animator.StringToHash("Speed");
            int movingHash = Animator.StringToHash("Moving");

            float horizontal = _animator.GetFloat(horizontalHash);
            float forward = _animator.GetFloat(forwardHash);
            float speed = _animator.GetFloat(speedHash);
            bool moving = _animator.GetBool(movingHash);

            // Check if values changed
            bool hasChanged = Mathf.Abs(horizontal - _lastHorizontalMovement) > 0.01f ||
                            Mathf.Abs(forward - _lastForwardMovement) > 0.01f ||
                            Mathf.Abs(speed - _lastSpeed) > 0.01f ||
                            moving != _lastMoving;

            string playerType = _isLocalPlayer ? "LOCAL" : "REMOTE";
            string changeMarker = hasChanged ? ">>> CHANGED <<<" : "(no change)";

            Debug.Log($"[PunAnimatorDebugger] {playerType} Player '{gameObject.name}' {changeMarker}");
            Debug.Log($"[PunAnimatorDebugger]   HorizontalMovement: {horizontal:F3} (was {_lastHorizontalMovement:F3})");
            Debug.Log($"[PunAnimatorDebugger]   ForwardMovement: {forward:F3} (was {_lastForwardMovement:F3})");
            Debug.Log($"[PunAnimatorDebugger]   Speed: {speed:F3} (was {_lastSpeed:F3})");
            Debug.Log($"[PunAnimatorDebugger]   Moving: {moving} (was {_lastMoving})");
            Debug.Log($"[PunAnimatorDebugger]   Current state: {_animator.GetCurrentAnimatorStateInfo(0).shortNameHash}");

            // If using AnimatorMonitor (Opsive), also check its values
            if (_animatorMonitor != null)
            {
                Debug.Log($"[PunAnimatorDebugger]   AnimatorMonitor.HorizontalMovement: {_animatorMonitor.HorizontalMovement:F3}");
                Debug.Log($"[PunAnimatorDebugger]   AnimatorMonitor.ForwardMovement: {_animatorMonitor.ForwardMovement:F3}");
                Debug.Log($"[PunAnimatorDebugger]   AnimatorMonitor.Speed: {_animatorMonitor.Speed:F3}");
                Debug.Log($"[PunAnimatorDebugger]   AnimatorMonitor.Moving: {_animatorMonitor.Moving}");
            }

            // Track for next comparison
            _lastHorizontalMovement = horizontal;
            _lastForwardMovement = forward;
            _lastSpeed = speed;
            _lastMoving = moving;
        }
    }
}
