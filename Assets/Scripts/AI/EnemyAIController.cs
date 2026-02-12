/*
 * Enemy AI Controller for Klyra's Reach
 *
 * PURPOSE:
 * AI controller for hostile enemies (zombies, aliens, etc.)
 * Detects player, chases them, and attacks when in range
 *
 * HOW TO USE:
 * 1. Add this script to your enemy character (must have UltimateCharacterLocomotion)
 * 2. Configure detection and attack ranges
 * 3. The enemy will automatically chase and attack the player
 */

using UnityEngine;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using Opsive.UltimateCharacterController.Character.Abilities.AI;

namespace KlyrasReach.AI
{
    /// <summary>
    /// Enemy AI states
    /// </summary>
    public enum EnemyAIState
    {
        Idle,           // Standing around, looking for player
        Chasing,        // Chasing the player
        Attacking,      // In attack range, attacking player
        Dead            // Enemy is dead
    }

    /// <summary>
    /// AI controller for hostile enemies
    /// </summary>
    public class EnemyAIController : MonoBehaviour
    {
        [Header("Enemy Info")]
        [Tooltip("Display name for this enemy")]
        [SerializeField] private string _enemyName = "Enemy";

        [Tooltip("Type of enemy (for logging/debugging)")]
        [SerializeField] private string _enemyType = "Thrall";

        [Header("Detection Settings")]
        [Tooltip("How far the enemy can detect the player")]
        [SerializeField] private float _detectionRange = 15f;

        [Tooltip("How close player needs to be to attack")]
        [SerializeField] private float _attackRange = 2f;

        [Tooltip("How often to check for player (seconds)")]
        [SerializeField] private float _detectionInterval = 0.5f;

        [Header("Chase Settings")]
        [Tooltip("How close to get to player before stopping")]
        [SerializeField] private float _stopDistance = 1.5f;

        [Tooltip("How often to update path while chasing (seconds)")]
        [SerializeField] private float _pathUpdateInterval = 0.5f;

        [Tooltip("Movement speed multiplier (1 = walk, 2 = run, 3 = sprint)")]
        [SerializeField] private float _chaseSpeed = 3f;

        [Header("Attack Settings")]
        [Tooltip("Time between attacks (seconds)")]
        [SerializeField] private float _attackCooldown = 1.5f;

        [Tooltip("Damage per attack")]
        [SerializeField] private float _attackDamage = 10f;

        [Header("Wander Settings")]
        [Tooltip("Should enemy wander when idle?")]
        [SerializeField] private bool _enableWander = true;

        [Tooltip("How far from spawn point to wander")]
        [SerializeField] private float _wanderRadius = 10f;

        [Tooltip("Time between picking new wander points (seconds)")]
        [SerializeField] private float _wanderInterval = 5f;

        [Header("Debug")]
        [Tooltip("Show debug messages in console?")]
        [SerializeField] private bool _debugMode = true;

        // Opsive components
        private UltimateCharacterLocomotion _characterLocomotion;
        private PathfindingMovement _pathfindingMovement;
        private NavMeshAgentMovement _navMeshMovement;
        private SpeedChange _speedChangeAbility;

        // State tracking
        private EnemyAIState _currentState = EnemyAIState.Idle;
        private Transform _playerTransform;
        private float _lastDetectionTime = 0f;
        private float _lastPathUpdateTime = 0f;
        private float _lastAttackTime = 0f;
        private bool _isInitialized = false;

        // Wander tracking
        private Vector3 _spawnPosition;
        private Vector3 _currentWanderTarget;
        private float _nextWanderTime = 0f;
        private bool _hasWanderTarget = false;

        /// <summary>
        /// Initialize the enemy AI
        /// </summary>
        private void Start()
        {
            InitializeAI();
        }

        /// <summary>
        /// Setup AI components and find player
        /// </summary>
        private void InitializeAI()
        {
            // Get Opsive character locomotion component
            _characterLocomotion = GetComponent<UltimateCharacterLocomotion>();
            if (_characterLocomotion == null)
            {
                Debug.LogError($"[EnemyAI] '{_enemyName}' missing UltimateCharacterLocomotion component! AI will not work.");
                enabled = false;
                return;
            }

            // Get movement abilities
            _pathfindingMovement = _characterLocomotion.GetAbility<PathfindingMovement>();
            _navMeshMovement = _characterLocomotion.GetAbility<NavMeshAgentMovement>();
            _speedChangeAbility = _characterLocomotion.GetAbility<SpeedChange>();

            if (_pathfindingMovement == null && _navMeshMovement == null)
            {
                Debug.LogError($"[EnemyAI] '{_enemyName}' has no PathfindingMovement or NavMeshAgentMovement ability! Add one to the character.");
                enabled = false;
                return;
            }

            // Disable CharacterFootEffects to prevent footstep sounds (performance optimization for large hordes)
            var footEffects = GetComponent<Opsive.UltimateCharacterController.Character.CharacterFootEffects>();
            if (footEffects != null)
            {
                footEffects.enabled = false;
                if (_debugMode)
                {
                    Debug.Log($"[EnemyAI] '{_enemyName}' disabled CharacterFootEffects for performance");
                }
            }

            // Disable any existing health bar canvases from Opsive prefab
            Transform existingHealthCanvas = transform.Find("HealthBar");
            if (existingHealthCanvas != null)
            {
                existingHealthCanvas.gameObject.SetActive(false);
                if (_debugMode)
                {
                    Debug.Log($"[EnemyAI] '{_enemyName}' disabled existing HealthBar canvas");
                }
            }

            if (_debugMode)
            {
                Debug.Log($"[EnemyAI] '{_enemyName}' PathfindingMovement: {(_pathfindingMovement != null ? "Found" : "NOT FOUND")}");
                Debug.Log($"[EnemyAI] '{_enemyName}' NavMeshAgentMovement: {(_navMeshMovement != null ? "Found" : "NOT FOUND")}");
                Debug.Log($"[EnemyAI] '{_enemyName}' SpeedChange: {(_speedChangeAbility != null ? "Found" : "NOT FOUND")}");
            }

            // Find player
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                _playerTransform = playerObject.transform;
                if (_debugMode)
                {
                    Debug.Log($"[EnemyAI] '{_enemyName}' found player: {playerObject.name}");
                }
            }
            else
            {
                Debug.LogWarning($"[EnemyAI] '{_enemyName}' could not find player! Make sure player has 'Player' tag.");
            }

            // Save spawn position for wandering
            _spawnPosition = transform.position;

            _isInitialized = true;
            _currentState = EnemyAIState.Idle;

            Debug.Log($"[EnemyAI] '{_enemyName}' ({_enemyType}) initialized. State: {_currentState}");
        }

        /// <summary>
        /// Main AI update loop
        /// </summary>
        private void Update()
        {
            if (!_isInitialized || _currentState == EnemyAIState.Dead)
                return;

            // Detect player periodically
            if (Time.time >= _lastDetectionTime + _detectionInterval)
            {
                DetectPlayer();
                _lastDetectionTime = Time.time;
            }

            // Execute current state behavior
            switch (_currentState)
            {
                case EnemyAIState.Idle:
                    UpdateIdleState();
                    break;

                case EnemyAIState.Chasing:
                    UpdateChasingState();
                    break;

                case EnemyAIState.Attacking:
                    UpdateAttackingState();
                    break;
            }
        }

        /// <summary>
        /// Detect if player is within range
        /// </summary>
        private void DetectPlayer()
        {
            if (_playerTransform == null)
                return;

            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

            // Check if player is within attack range
            if (distanceToPlayer <= _attackRange)
            {
                if (_currentState != EnemyAIState.Attacking)
                {
                    ChangeState(EnemyAIState.Attacking);
                }
            }
            // Check if player is within detection range
            else if (distanceToPlayer <= _detectionRange)
            {
                if (_currentState == EnemyAIState.Idle)
                {
                    ChangeState(EnemyAIState.Chasing);
                }
            }
            // Player is out of range
            else
            {
                if (_currentState != EnemyAIState.Idle)
                {
                    ChangeState(EnemyAIState.Idle);
                }
            }
        }

        /// <summary>
        /// Idle state - wandering around spawn area
        /// </summary>
        private void UpdateIdleState()
        {
            if (!_enableWander)
                return;

            // Check if we need a new wander target
            if (!_hasWanderTarget || Time.time >= _nextWanderTime)
            {
                PickNewWanderTarget();
                _nextWanderTime = Time.time + _wanderInterval;
            }

            // Check if we reached the wander target
            if (_hasWanderTarget)
            {
                float distanceToTarget = Vector3.Distance(transform.position, _currentWanderTarget);

                if (distanceToTarget <= 2f)
                {
                    // Reached target, stop moving
                    _hasWanderTarget = false;
                    StopMovement();
                }
                else
                {
                    // Keep moving to target
                    UpdateWanderPath();
                }
            }
        }

        /// <summary>
        /// Pick a random point within wander radius
        /// </summary>
        private void PickNewWanderTarget()
        {
            // Pick random point in circle around spawn position
            Vector2 randomCircle = Random.insideUnitCircle * _wanderRadius;
            _currentWanderTarget = _spawnPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
            _hasWanderTarget = true;

            if (_debugMode && Time.frameCount % 120 == 0)
            {
                Debug.Log($"[EnemyAI] '{_enemyName}' picked new wander target: {_currentWanderTarget}");
            }
        }

        /// <summary>
        /// Update path to current wander target
        /// </summary>
        private void UpdateWanderPath()
        {
            if (_pathfindingMovement != null)
            {
                _pathfindingMovement.SetDestination(_currentWanderTarget);
                if (!_pathfindingMovement.Enabled)
                {
                    _pathfindingMovement.Enabled = true;
                }
            }
            else if (_navMeshMovement != null)
            {
                _navMeshMovement.SetDestination(_currentWanderTarget);
                if (!_navMeshMovement.Enabled)
                {
                    _navMeshMovement.Enabled = true;
                }
            }
        }

        /// <summary>
        /// Chasing state - pursuing the player
        /// </summary>
        private void UpdateChasingState()
        {
            if (_playerTransform == null)
                return;

            // Update path periodically
            if (Time.time >= _lastPathUpdateTime + _pathUpdateInterval)
            {
                UpdatePath();
                _lastPathUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// Attacking state - attacking the player
        /// </summary>
        private void UpdateAttackingState()
        {
            if (_playerTransform == null)
                return;

            // Stop moving when attacking
            StopMovement();

            // Face the player
            Vector3 directionToPlayer = (_playerTransform.position - transform.position);
            directionToPlayer.y = 0; // Keep on horizontal plane
            if (directionToPlayer.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }

            // Attack if cooldown is ready
            if (Time.time >= _lastAttackTime + _attackCooldown)
            {
                PerformAttack();
                _lastAttackTime = Time.time;
            }
        }

        /// <summary>
        /// Update path to chase player
        /// </summary>
        private void UpdatePath()
        {
            if (_playerTransform == null)
                return;

            Vector3 targetPosition = _playerTransform.position;

            // Use PathfindingMovement if available
            if (_pathfindingMovement != null)
            {
                _pathfindingMovement.SetDestination(targetPosition);
                if (!_pathfindingMovement.Enabled)
                {
                    _pathfindingMovement.Enabled = true;
                }
            }
            // Otherwise try NavMeshAgentMovement
            else if (_navMeshMovement != null)
            {
                _navMeshMovement.SetDestination(targetPosition);
                if (!_navMeshMovement.Enabled)
                {
                    _navMeshMovement.Enabled = true;
                }
            }

            // Enable sprint using SpeedChange ability
            if (_speedChangeAbility != null && !_speedChangeAbility.IsActive)
            {
                _characterLocomotion.TryStartAbility(_speedChangeAbility);

                if (_debugMode)
                {
                    Debug.Log($"[EnemyAI] '{_enemyName}' started sprinting!");
                }
            }

            if (_debugMode && Time.frameCount % 120 == 0)
            {
                Debug.Log($"[EnemyAI] '{_enemyName}' updating path to player at {targetPosition}");
            }
        }

        /// <summary>
        /// Stop all movement
        /// </summary>
        private void StopMovement()
        {
            if (_pathfindingMovement != null)
            {
                _pathfindingMovement.Enabled = false;
            }

            if (_navMeshMovement != null)
            {
                _navMeshMovement.Enabled = false;
            }

            // Stop sprinting
            if (_speedChangeAbility != null && _speedChangeAbility.IsActive)
            {
                _characterLocomotion.TryStopAbility(_speedChangeAbility);
            }
        }

        /// <summary>
        /// Perform attack on player
        /// </summary>
        private void PerformAttack()
        {
            if (_debugMode)
            {
                Debug.Log($"[EnemyAI] '{_enemyName}' attacks player for {_attackDamage} damage!");
            }

            // TODO: Apply damage to player using Opsive's health system
            // For now, just log the attack
            // You can integrate with Opsive's Health component here:
            // var playerHealth = _playerTransform.GetComponent<Opsive.UltimateCharacterController.Traits.Health>();
            // if (playerHealth != null) playerHealth.Damage(_attackDamage);
        }

        /// <summary>
        /// Change AI state
        /// </summary>
        private void ChangeState(EnemyAIState newState)
        {
            if (_currentState == newState)
                return;

            EnemyAIState oldState = _currentState;
            _currentState = newState;

            if (_debugMode)
            {
                Debug.Log($"[EnemyAI] '{_enemyName}' state changed: {oldState} -> {newState}");
            }

            // Handle state transitions
            switch (newState)
            {
                case EnemyAIState.Idle:
                    StopMovement();
                    break;

                case EnemyAIState.Chasing:
                    _hasWanderTarget = false; // Clear wander target when chasing
                    UpdatePath();
                    break;

                case EnemyAIState.Attacking:
                    StopMovement();
                    break;

                case EnemyAIState.Dead:
                    StopMovement();
                    break;
            }
        }

        /// <summary>
        /// Called when enemy dies
        /// </summary>
        public void OnDeath()
        {
            ChangeState(EnemyAIState.Dead);
            Debug.Log($"[EnemyAI] '{_enemyName}' died!");

            // TODO: Play death animation, drop loot, etc.
        }

        /// <summary>
        /// Get current state
        /// </summary>
        public EnemyAIState GetCurrentState()
        {
            return _currentState;
        }

        /// <summary>
        /// Draw debug visualization in Scene view
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw detection range
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _detectionRange);

            // Draw attack range
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _attackRange);

            // Draw wander radius around spawn point
            if (_enableWander && Application.isPlaying)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
                Gizmos.DrawWireSphere(_spawnPosition, _wanderRadius);

                // Draw current wander target
                if (_hasWanderTarget)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(_currentWanderTarget, 0.5f);
                    Gizmos.DrawLine(transform.position, _currentWanderTarget);
                }
            }

            // Draw line to player if chasing
            if (Application.isPlaying && _playerTransform != null && _currentState == EnemyAIState.Chasing)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _playerTransform.position);
            }
        }
    }
}
