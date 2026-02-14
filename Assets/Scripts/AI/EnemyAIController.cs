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
        // Static cached player reference (shared by all enemies for performance)
        private static Transform _cachedPlayerTransform = null;

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
        [SerializeField] private float _detectionInterval = 2f; // Very dumb AI

        [Header("Chase Settings")]
        [Tooltip("How close to get to player before stopping")]
        [SerializeField] private float _stopDistance = 1.5f;

        [Tooltip("How often to update path while chasing (seconds)")]
        [SerializeField] private float _pathUpdateInterval = 3f; // Very dumb pathfinding

        [Tooltip("Should enemies sprint when chasing? (Check for fast enemies, uncheck for slow zombies)")]
        [SerializeField] private bool _sprintWhenChasing = true; // Enable sprinting for faster movement

        [Header("Attack Settings")]
        [Tooltip("Time between attacks (seconds)")]
        [SerializeField] private float _attackCooldown = 1.5f;

        [Tooltip("Damage per attack")]
        [SerializeField] private float _attackDamage = 10f;

        [Header("Wander Settings")]
        [Tooltip("Should enemy wander when idle?")]
        [SerializeField] private bool _enableWander = false; // Disabled for performance

        [Tooltip("How far from spawn point to wander")]
        [SerializeField] private float _wanderRadius = 10f;

        [Tooltip("Time between picking new wander points (seconds)")]
        [SerializeField] private float _wanderInterval = 10f; // Increased from 5

        [Header("Performance")]
        [Tooltip("Update rate based on distance (far enemies update less)")]
        [SerializeField] private bool _useDistanceLOD = true;

        [Tooltip("Completely disable all animations? (HUGE performance gain)")]
        [SerializeField] private bool _disableAllAnimations = false;

        [Tooltip("Distance for full update rate")]
        [SerializeField] private float _closeDistance = 10f; // Very close only

        [Tooltip("Distance for reduced update rate")]
        [SerializeField] private float _farDistance = 30f; // Medium distance

        [Tooltip("Distance for minimum update rate")]
        [SerializeField] private float _veryFarDistance = 80f;

        [Header("Death Effects")]
        [Tooltip("Blood splatter effect on death")]
        [SerializeField] private GameObject _bloodEffectPrefab;

        [Tooltip("Play blood effect on death?")]
        [SerializeField] private bool _useBloodEffect = true;

        [Header("Hit Effects")]
        [Tooltip("Blood splatter when taking damage")]
        [SerializeField] private GameObject _hitBloodEffectPrefab;

        [Tooltip("Play blood on hit?")]
        [SerializeField] private bool _useHitBloodEffect = true;

        [Tooltip("Minimum damage to show blood (prevents spam)")]
        [SerializeField] private float _minDamageForBlood = 1f;

        [Header("Debug")]
        [Tooltip("Show debug messages in console?")]
        [SerializeField] private bool _debugMode = false;

        // Opsive components
        private UltimateCharacterLocomotion _characterLocomotion;
        private PathfindingMovement _pathfindingMovement;
        private NavMeshAgentMovement _navMeshMovement;
        private SpeedChange _speedChangeAbility;
        private Opsive.UltimateCharacterController.Traits.AttributeManager _attributeManager;
        private Opsive.UltimateCharacterController.Traits.Attribute _healthAttribute;
        private Animator _animator;
        private Animator[] _allAnimators; // Cache to avoid duplicate GetComponentsInChildren calls
        private Rigidbody _rigidbody;
        private Collider[] _allColliders;

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

        // Performance optimization
        private int _updateFrameOffset;
        private float _cachedDistanceToPlayer;
        private float _lastWanderPathUpdate = 0f;

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

            // Get ALL Animators on this enemy and children (MAJOR performance optimization)
            _animator = GetComponent<Animator>();
            _allAnimators = GetComponentsInChildren<Animator>(); // CACHE to avoid calling this again later

            // Disable animations entirely if requested (HUGE performance gain)
            if (_disableAllAnimations)
            {
                foreach (var anim in _allAnimators)
                {
                    if (anim != null)
                    {
                        anim.enabled = false;
                    }
                }
            }
            else
            {
                // Set Animator to update less frequently and cull when offscreen
                foreach (var anim in _allAnimators)
                {
                    if (anim != null)
                    {
                        anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                    }
                }
            }

            // PHYSICS OPTIMIZATION: Get Rigidbody and optimize collision detection
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody != null)
            {
                // Discrete collision detection is much faster than Continuous
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                // Reduce solver iterations for faster physics
                _rigidbody.solverIterations = 1;
                _rigidbody.solverVelocityIterations = 1;
            }

            // Get all colliders (we might disable some for distant enemies)
            _allColliders = GetComponentsInChildren<Collider>();

            // Disable CharacterFootEffects to prevent footstep sounds
            var footEffects = GetComponent<Opsive.UltimateCharacterController.Character.CharacterFootEffects>();
            if (footEffects != null)
            {
                footEffects.enabled = false;
            }

            // Disable CharacterIK if present (VERY expensive)
            var characterIK = GetComponent<Opsive.UltimateCharacterController.Character.CharacterIK>();
            if (characterIK != null)
            {
                characterIK.enabled = false;
            }

            // Disable any existing health bar canvases from Opsive prefab
            Transform existingHealthCanvas = transform.Find("HealthBar");
            if (existingHealthCanvas != null)
            {
                existingHealthCanvas.gameObject.SetActive(false);
            }

            // Find player using cached reference (performance optimization)
            if (_cachedPlayerTransform == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    _cachedPlayerTransform = playerObject.transform;
                }
            }
            _playerTransform = _cachedPlayerTransform;

            // Get health attribute for death detection
            _attributeManager = GetComponent<Opsive.UltimateCharacterController.Traits.AttributeManager>();
            if (_attributeManager != null)
            {
                _healthAttribute = _attributeManager.GetAttribute("Health");
                if (_healthAttribute != null)
                {
                    // Subscribe to death event
                    Opsive.Shared.Events.EventHandler.RegisterEvent<Vector3, Vector3, GameObject>(gameObject, "OnDeath", OnOpsiveDeath);

                    // Subscribe to damage event
                    Opsive.Shared.Events.EventHandler.RegisterEvent<float, Vector3, Vector3, GameObject, Collider>(gameObject, "OnHealthDamage", OnHealthDamage);
                }
            }

            // Save spawn position for wandering
            _spawnPosition = transform.position;

            // Stagger updates across frames to spread CPU load
            _updateFrameOffset = Random.Range(0, 10);

            _isInitialized = true;
            _currentState = EnemyAIState.Idle;
        }

        /// <summary>
        /// Main AI update loop with distance-based LOD
        /// </summary>
        private void Update()
        {
            if (!_isInitialized)
                return;

            // Check for death
            if (_currentState != EnemyAIState.Dead && _healthAttribute != null)
            {
                if (_healthAttribute.Value <= 0f)
                {
                    OnDeath();
                    return;
                }
            }

            if (_currentState == EnemyAIState.Dead)
                return;

            // Distance-based LOD: Skip frames for distant enemies
            if (_useDistanceLOD && _playerTransform != null)
            {
                // Calculate distance once per frame
                float sqrDistToPlayer = (transform.position - _playerTransform.position).sqrMagnitude;
                _cachedDistanceToPlayer = Mathf.Sqrt(sqrDistToPlayer);

                // CRITICAL OPTIMIZATION: Disable Animator for distant enemies
                float animatorCutoffDistance = 15f; // Only animate enemies within 15m

                // Disable animations for distant enemies
                if (!_disableAllAnimations && _animator != null)
                {
                    bool shouldAnimate = sqrDistToPlayer < (animatorCutoffDistance * animatorCutoffDistance);
                    if (_animator.enabled != shouldAnimate)
                    {
                        _animator.enabled = shouldAnimate;

                        // Also toggle all child animators (use cached array!)
                        if (_allAnimators != null)
                        {
                            foreach (var anim in _allAnimators)
                            {
                                if (anim != null)
                                {
                                    anim.enabled = shouldAnimate;
                                }
                            }
                        }
                    }
                }

                // Very far enemies: Update every 20 frames (barely think)
                if (sqrDistToPlayer > _veryFarDistance * _veryFarDistance)
                {
                    if ((Time.frameCount + _updateFrameOffset) % 20 != 0)
                        return;
                }
                // Far enemies: Update every 10 frames
                else if (sqrDistToPlayer > _farDistance * _farDistance)
                {
                    if ((Time.frameCount + _updateFrameOffset) % 10 != 0)
                        return;
                }
                // Medium distance: Update every 5 frames
                else if (sqrDistToPlayer > _closeDistance * _closeDistance)
                {
                    if ((Time.frameCount + _updateFrameOffset) % 5 != 0)
                        return;
                }
                // Close enemies: Update every 3 frames (VERY DUMB AI)
                // Critical for performance with large hordes
                else
                {
                    if ((Time.frameCount + _updateFrameOffset) % 3 != 0)
                        return;
                }
            }

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
        /// Detect if player is within range (optimized with sqrMagnitude)
        /// </summary>
        private void DetectPlayer()
        {
            if (_playerTransform == null)
                return;

            // Use cached distance if available, otherwise calculate
            float distanceToPlayer = _useDistanceLOD ? _cachedDistanceToPlayer :
                Vector3.Distance(transform.position, _playerTransform.position);

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
        /// Idle state - wandering around spawn area (optimized)
        /// </summary>
        private void UpdateIdleState()
        {
            if (!_enableWander)
                return;

            // Disable wandering for very distant enemies to save performance
            if (_useDistanceLOD && _cachedDistanceToPlayer > _farDistance * 1.5f)
                return;

            // Check if we need a new wander target
            if (!_hasWanderTarget || Time.time >= _nextWanderTime)
            {
                PickNewWanderTarget();
                _nextWanderTime = Time.time + _wanderInterval;
            }

            // Check if we reached the wander target (only every 0.5 seconds)
            if (_hasWanderTarget && Time.time >= _lastWanderPathUpdate + 0.5f) // Increased from 0.2
            {
                // Use sqrMagnitude instead of Distance (avoids sqrt)
                float sqrDistToTarget = (transform.position - _currentWanderTarget).sqrMagnitude;

                if (sqrDistToTarget <= 4f) // 2f * 2f = 4f
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

                _lastWanderPathUpdate = Time.time;
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
        /// Attacking state - attacking the player (optimized)
        /// </summary>
        private void UpdateAttackingState()
        {
            if (_playerTransform == null)
                return;

            // Stop moving when attacking (only once)
            if (_pathfindingMovement != null && _pathfindingMovement.Enabled ||
                _navMeshMovement != null && _navMeshMovement.Enabled)
            {
                StopMovement();
            }

            // Face the player (very infrequently for performance)
            // Quaternion.Slerp is expensive with many enemies
            if ((Time.frameCount + _updateFrameOffset) % 5 == 0)
            {
                Vector3 directionToPlayer = (_playerTransform.position - transform.position);
                directionToPlayer.y = 0; // Keep on horizontal plane
                if (directionToPlayer.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
                }
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

            // Enable sprint using SpeedChange ability (only if enabled)
            if (_sprintWhenChasing && _speedChangeAbility != null && !_speedChangeAbility.IsActive)
            {
                _characterLocomotion.TryStartAbility(_speedChangeAbility);
            }
            else if (!_sprintWhenChasing && _speedChangeAbility != null && _speedChangeAbility.IsActive)
            {
                // Stop sprinting if we disabled sprint mode
                _characterLocomotion.TryStopAbility(_speedChangeAbility);
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
            // TODO: Apply damage to player using Opsive's health system
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

            _currentState = newState;

            // Handle state transitions
            switch (newState)
            {
                case EnemyAIState.Idle:
                    StopMovement();
                    break;

                case EnemyAIState.Chasing:
                    _hasWanderTarget = false; // Clear wander target when chasing
                    UpdatePath(); // This will handle sprint enabling if needed
                    break;

                case EnemyAIState.Attacking:
                    StopMovement(); // This will stop sprinting
                    break;

                case EnemyAIState.Dead:
                    StopMovement();
                    break;
            }
        }

        /// <summary>
        /// Called by Opsive when character takes damage
        /// </summary>
        private void OnHealthDamage(float amount, Vector3 position, Vector3 force, GameObject attacker, Collider hitCollider)
        {
            // Only spawn blood if damage is significant
            if (!_useHitBloodEffect || amount < _minDamageForBlood)
                return;

            if (_hitBloodEffectPrefab != null)
            {
                // Spawn blood at hit position
                GameObject hitBlood = Instantiate(_hitBloodEffectPrefab, position, Quaternion.identity);

                // Rotate blood to face away from attacker (spray direction)
                if (attacker != null)
                {
                    Vector3 direction = (position - attacker.transform.position).normalized;
                    hitBlood.transform.rotation = Quaternion.LookRotation(direction);
                }

                // Clean up after 2 seconds
                Destroy(hitBlood, 2f);
            }
        }

        /// <summary>
        /// Called by Opsive when character dies
        /// </summary>
        private void OnOpsiveDeath(Vector3 position, Vector3 force, GameObject attacker)
        {
            OnDeath();
        }

        /// <summary>
        /// Called when enemy dies
        /// </summary>
        public void OnDeath()
        {
            if (_currentState == EnemyAIState.Dead)
            {
                return; // Already dead
            }

            ChangeState(EnemyAIState.Dead);

            // Stop all movement
            StopMovement();

            // Spawn blood effect
            if (_useBloodEffect && _bloodEffectPrefab != null)
            {
                GameObject bloodFX = Instantiate(_bloodEffectPrefab, transform.position, Quaternion.identity);
                Destroy(bloodFX, 5f); // Clean up after 5 seconds
            }

            // Hide the enemy body immediately (blood effect replaces it visually)
            StartCoroutine(HideEnemyBody());

            // Disable AI script to save performance
            enabled = false;

            // Return to pool after delay (if using object pooling)
            StartCoroutine(ReturnToPoolAfterDelay(3f));
        }

        /// <summary>
        /// Hide enemy body after death
        /// </summary>
        private System.Collections.IEnumerator HideEnemyBody()
        {
            // Wait one frame so blood spawns first
            yield return null;

            // Find all renderers and disable them
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = false;
            }

            // Disable health bar component
            var healthBar = GetComponent<SimpleHealthBar>();
            if (healthBar != null)
            {
                healthBar.enabled = false;
            }
        }

        /// <summary>
        /// Return enemy to pool after death animation
        /// </summary>
        private System.Collections.IEnumerator ReturnToPoolAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            // Re-enable renderers for next spawn
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = true;
            }

            // Re-enable health bar
            var healthBar = GetComponent<SimpleHealthBar>();
            if (healthBar != null)
            {
                healthBar.enabled = true;
            }

            // Reset health for next spawn
            if (_healthAttribute != null)
            {
                _healthAttribute.Value = _healthAttribute.MaxValue;
            }

            // Reset state
            _currentState = EnemyAIState.Idle;
            enabled = true;

            // Deactivate (spawner will reactivate when needed)
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Get current state
        /// </summary>
        public EnemyAIState GetCurrentState()
        {
            return _currentState;
        }

        /// <summary>
        /// Cleanup on destroy
        /// </summary>
        private void OnDestroy()
        {
            // Unsubscribe from Opsive events
            if (_attributeManager != null && _healthAttribute != null)
            {
                Opsive.Shared.Events.EventHandler.UnregisterEvent<Vector3, Vector3, GameObject>(gameObject, "OnDeath", OnOpsiveDeath);
                Opsive.Shared.Events.EventHandler.UnregisterEvent<float, Vector3, Vector3, GameObject, Collider>(gameObject, "OnHealthDamage", OnHealthDamage);
            }
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
