/*
 * Sentinel AI Controller for Klyra's Reach
 *
 * PURPOSE:
 * AI controller for the Hollow Sentinel — a ranged sniper enemy that stays at
 * distance and shoots the player. Contrasts with the Ravager's melee combat.
 *
 * BEHAVIOR:
 * - Detects player within detection range
 * - Chases to preferred engagement distance (not melee range)
 * - Holds position at preferred range and shoots on cooldown
 * - Retreats if the player closes to within minimum range
 * - Closes the gap if the player runs too far away
 * - Flanks to approach from different angles (optional)
 * - Patrols/wanders when idle (same as Ravager)
 *
 * ARCHITECTURE:
 * This is a standalone controller (not derived from EnemyAIController) so it can
 * be customized independently. It shares the same public interface (OnDeath(),
 * AssignPatrolRoute(), GetCurrentState()) so NetworkEnemySync and RavagerSpawner
 * work with it seamlessly.
 *
 * HOW TO USE:
 * 1. Add this script to the Hollow Sentinel prefab
 * 2. Also add NetworkEnemySync and SimpleHealthBar
 * 3. Configure detection range, preferred range, min range, shoot cooldown
 * 4. Use a RavagerSpawner in the scene pointed at the Sentinel prefab
 */

using UnityEngine;
using UnityEngine.AI;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using Opsive.UltimateCharacterController.Character.Abilities.AI;
using Opsive.UltimateCharacterController.Character.Abilities.Items;
using Photon.Pun;

namespace KlyrasReach.AI
{
    /// <summary>
    /// Ranged AI controller for the Hollow Sentinel sniper enemy.
    /// Stays at distance, shoots, and retreats if the player gets too close.
    /// </summary>
    public class SentinelAIController : MonoBehaviour
    {
        // =====================================================
        //  INSPECTOR SETTINGS — Configure these in Unity
        // =====================================================

        [Header("Enemy Info")]
        [Tooltip("Display name for this enemy (used in debug logs)")]
        [SerializeField] private string _enemyName = "Hollow Sentinel";

        [Tooltip("Type of enemy (for logging/debugging)")]
        [SerializeField] private string _enemyType = "Sentinel";

        [Header("Detection Settings")]
        [Tooltip("How far the enemy can detect the player (meters). Snipers should have long detection range.")]
        [SerializeField] private float _detectionRange = 40f;

        [Tooltip("How often to check for the player (seconds). Lower = more responsive but costs more CPU.")]
        [SerializeField] private float _detectionInterval = 1f;

        [Tooltip("When hit, alert nearby idle enemies within this radius to also attack (meters).")]
        [SerializeField] private float _alertRadius = 30f;

        // =====================================================
        //  RANGED COMBAT — The Sentinel's core behavior.
        //  These control the distance-keeping and shooting.
        // =====================================================
        [Header("Ranged Combat")]
        [Tooltip("Ideal engagement distance (meters). The Sentinel tries to stay at this distance from the player and shoot.")]
        [SerializeField] private float _preferredRange = 25f;

        [Tooltip("If the player gets closer than this, the Sentinel retreats to preferred range (meters).")]
        [SerializeField] private float _minRange = 8f;

        [Tooltip("Maximum shooting distance (meters). Beyond this, the Sentinel chases to close the gap.")]
        [SerializeField] private float _maxRange = 35f;

        [Tooltip("Time between shots (seconds). Lower = faster fire rate.")]
        [SerializeField] private float _shootCooldown = 2.5f;

        [Tooltip("Fallback damage per attack if Opsive Use ability isn't set up.")]
        [SerializeField] private float _attackDamage = 25f;

        [Tooltip("Which Item Set index to equip on spawn (0 = first item set). " +
                 "Set to -1 to not auto-equip. Check your Item Set Manager for the right index.")]
        [SerializeField] private int _autoEquipItemSetIndex = 0;

        // =====================================================
        //  MOVEMENT — How fast the Sentinel moves.
        // =====================================================
        [Header("Movement")]
        [Tooltip("Should the Sentinel sprint when chasing or retreating?")]
        [SerializeField] private bool _sprintWhenChasing = true;

        [Tooltip("Speed multiplier when sprinting/chasing. Lower than Ravagers since snipers are less aggressive.")]
        [SerializeField] private float _chaseSpeedMultiplier = 1.5f;

        [Tooltip("How often to update the navigation path while chasing (seconds).")]
        [SerializeField] private float _pathUpdateInterval = 1.5f;

        [Tooltip("How close to the destination before stopping (meters).")]
        [SerializeField] private float _stopDistance = 1.5f;

        // =====================================================
        //  FLANKING — Approach from different angles instead
        //  of running straight at the player.
        // =====================================================
        [Header("Flanking")]
        [Tooltip("Enable flanking? Each Sentinel picks a random approach angle so they spread out around the player.")]
        [SerializeField] private bool _enableFlanking = true;

        [Tooltip("How far from the player the flanking offset point is (meters).")]
        [SerializeField] private float _flankingRadius = 4f;

        [Tooltip("When closer than this, drop the flanking offset and go straight for the player.")]
        [SerializeField] private float _flankingCutoffDistance = 10f;

        // =====================================================
        //  IDLE BEHAVIOR — What the Sentinel does when no
        //  player is detected (patrol or wander).
        // =====================================================
        [Header("Wander Settings")]
        [Tooltip("Should the Sentinel wander randomly when idle?")]
        [SerializeField] private bool _enableWander = false;

        [Tooltip("How far from spawn point to wander (meters).")]
        [SerializeField] private float _wanderRadius = 10f;

        [Tooltip("Time between picking new wander points (seconds).")]
        [SerializeField] private float _wanderInterval = 10f;

        [Header("Patrol Settings")]
        [Tooltip("Patrol route assigned by the spawner (overrides random wander when set).")]
        [SerializeField] private PatrolRoute _patrolRoute;

        [Tooltip("Should the enemy start patrolling immediately, or wait until first idle?")]
        [SerializeField] private bool _patrolOnStart = true;

        // =====================================================
        //  PERFORMANCE — LOD and frame-skipping to handle
        //  large numbers of enemies efficiently.
        // =====================================================
        [Header("Performance")]
        [Tooltip("Update rate based on distance (far enemies update less often).")]
        [SerializeField] private bool _useDistanceLOD = true;

        [Tooltip("Completely disable all animations? (HUGE performance gain for distant enemies)")]
        [SerializeField] private bool _disableAllAnimations = false;

        [Tooltip("Distance for full update rate (meters).")]
        [SerializeField] private float _closeDistance = 10f;

        [Tooltip("Distance for reduced update rate (meters).")]
        [SerializeField] private float _farDistance = 30f;

        [Tooltip("Distance for minimum update rate (meters).")]
        [SerializeField] private float _veryFarDistance = 80f;

        // =====================================================
        //  DEATH & HIT EFFECTS — Blood, body hiding, etc.
        // =====================================================
        [Header("Death Effects")]
        [Tooltip("Blood splatter effect on death.")]
        [SerializeField] private GameObject _bloodEffectPrefab;

        [Tooltip("Play blood effect on death?")]
        [SerializeField] private bool _useBloodEffect = true;

        [Header("Hit Effects")]
        [Tooltip("Blood splatter when taking damage.")]
        [SerializeField] private GameObject _hitBloodEffectPrefab;

        [Tooltip("Play blood on hit?")]
        [SerializeField] private bool _useHitBloodEffect = true;

        [Tooltip("Minimum damage to show blood (prevents spam from tiny hits).")]
        [SerializeField] private float _minDamageForBlood = 1f;

        [Header("Debug")]
        [Tooltip("Show debug messages in console?")]
        [SerializeField] private bool _debugMode = false;

        // =====================================================
        //  PRIVATE COMPONENT REFERENCES — Cached on Start()
        // =====================================================

        // Opsive components
        private UltimateCharacterLocomotion _characterLocomotion;
        private PathfindingMovement _pathfindingMovement;
        private NavMeshAgentMovement _navMeshMovement;
        private SpeedChange _speedChangeAbility;
        private Use _useAbility;                    // Opsive's Use ability — triggers the equipped weapon's attack
        private EquipUnequip _equipUnequipAbility;  // Opsive's ability for equipping items (sniper rifle)
        private Opsive.UltimateCharacterController.Traits.AttributeManager _attributeManager;
        private Opsive.UltimateCharacterController.Traits.Attribute _healthAttribute;
        private Animator _animator;
        private Animator[] _allAnimators;            // Cached to avoid repeated GetComponentsInChildren calls
        private Rigidbody _rigidbody;
        private Collider[] _allColliders;

        // NavMeshAgent direct speed control
        private NavMeshAgent _navMeshAgent;
        private float _originalNavMeshSpeed;         // Saved on startup so we can restore after sprinting
        private bool _isSprintSpeedApplied = false;

        // Photon components
        private PhotonView _photonView;

        // =====================================================
        //  PRIVATE STATE — Tracked at runtime
        // =====================================================

        // Core state machine
        private EnemyAIState _currentState = EnemyAIState.Idle;
        private Transform _playerTransform;          // Current target player
        private float _lastDetectionTime = 0f;
        private float _lastPathUpdateTime = 0f;
        private float _lastAttackTime = 0f;
        private bool _isInitialized = false;

        // Wander tracking
        private Vector3 _spawnPosition;
        private Vector3 _currentWanderTarget;
        private float _nextWanderTime = 0f;
        private bool _hasWanderTarget = false;

        // Patrol tracking
        private int _currentPatrolWaypointIndex = 0;
        private float _patrolWaitTimer = 0f;
        private bool _isWaitingAtWaypoint = false;
        private int _patrolDirection = 1;            // +1 = forward, -1 = reverse (ping-pong)
        private float _lastPatrolPathUpdate = 0f;

        // Performance optimization
        private int _updateFrameOffset;
        private float _cachedDistanceToPlayer;
        private float _lastWanderPathUpdate = 0f;

        // Retarget timer — how often to re-evaluate closest player
        private float _lastRetargetTime = 0f;
        private const float RETARGET_INTERVAL = 3f;

        // Flanking state
        private float _flankAngle = 0f;

        // =====================================================
        //  INITIALIZATION
        // =====================================================

        /// <summary>
        /// Initialize the Sentinel AI on spawn
        /// </summary>
        private void Start()
        {
            InitializeAI();
        }

        /// <summary>
        /// Called every time the GameObject is activated (including when pulled from
        /// the object pool). Triggers weapon auto-equip here instead of Start()
        /// because Start() only fires once per lifetime — when the pool deactivates
        /// and reactivates the sentinel, Start() doesn't run again, so the 0.5s
        /// equip coroutine that was started during pool init gets killed and never
        /// retries. OnEnable() fires on every activation, fixing this.
        /// </summary>
        private void OnEnable()
        {
            // Only equip on master client or single player — non-master clients get
            // weapon visuals from NetworkEnemySync forcing renderers active.
            bool isMasterOrSinglePlayer = _photonView == null || !PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient;
            if (_autoEquipItemSetIndex >= 0 && _equipUnequipAbility != null && isMasterOrSinglePlayer)
            {
                StartCoroutine(AutoEquipAfterDelay());
            }
        }

        /// <summary>
        /// Setup AI components, find player, equip weapon.
        /// Same initialization flow as EnemyAIController so all systems are ready.
        /// </summary>
        private void InitializeAI()
        {
            // MULTIPLAYER: Check if this is a networked enemy
            _photonView = GetComponent<PhotonView>();

            // Log multiplayer status
            if (_photonView != null && PhotonNetwork.IsConnected)
            {
                if (!PhotonNetwork.IsMasterClient)
                {
                    Debug.Log($"[SentinelAI] '{_enemyName}' is on non-master client - AI won't run but components initialized for sync");
                }
                else
                {
                    Debug.Log($"[SentinelAI] '{_enemyName}' is on MASTER CLIENT - AI will run here");
                }
            }
            else
            {
                Debug.Log($"[SentinelAI] '{_enemyName}' is in SINGLE PLAYER mode - AI will run normally");
            }

            // Get Opsive character locomotion component (required)
            _characterLocomotion = GetComponent<UltimateCharacterLocomotion>();
            if (_characterLocomotion == null)
            {
                Debug.LogError($"[SentinelAI] '{_enemyName}' missing UltimateCharacterLocomotion component! AI will not work.");
                enabled = false;
                return;
            }

            // Get movement abilities (need at least one for pathfinding)
            _pathfindingMovement = _characterLocomotion.GetAbility<PathfindingMovement>();
            _navMeshMovement = _characterLocomotion.GetAbility<NavMeshAgentMovement>();
            _speedChangeAbility = _characterLocomotion.GetAbility<SpeedChange>();

            // Get Use ability for shooting (triggers the equipped weapon's attack)
            _useAbility = _characterLocomotion.GetItemAbility<Use>();
            if (_useAbility == null)
                _useAbility = _characterLocomotion.GetAbility<Use>();
            if (_useAbility != null)
            {
                Debug.Log($"[SentinelAI] '{_enemyName}' has Use ability - will use Opsive ranged attacks");
            }

            // Get EquipUnequip ability for auto-equipping the sniper rifle on spawn
            _equipUnequipAbility = _characterLocomotion.GetItemAbility<EquipUnequip>();
            if (_equipUnequipAbility == null)
                _equipUnequipAbility = _characterLocomotion.GetAbility<EquipUnequip>();
            if (_equipUnequipAbility != null)
            {
                Debug.Log($"[SentinelAI] '{_enemyName}' has EquipUnequip ability - will auto-equip item set {_autoEquipItemSetIndex}");
            }
            else
            {
                Debug.LogWarning($"[SentinelAI] '{_enemyName}' could NOT find EquipUnequip ability! Sniper rifle won't be equipped.");
            }

            // Check movement abilities
            if (_pathfindingMovement == null && _navMeshMovement == null)
            {
                // On non-master clients, NetworkEnemySync handles position — no movement needed
                if (_photonView != null && PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
                {
                    Debug.Log($"[SentinelAI] '{_enemyName}' no movement abilities on non-master client - OK, NetworkEnemySync handles position");
                }
                else
                {
                    Debug.LogError($"[SentinelAI] '{_enemyName}' has no PathfindingMovement or NavMeshAgentMovement ability! Add one to the character.");
                    enabled = false;
                    return;
                }
            }

            // Cache animators for LOD optimization
            _animator = GetComponent<Animator>();
            _allAnimators = GetComponentsInChildren<Animator>();

            // Disable animations entirely if requested
            if (_disableAllAnimations)
            {
                foreach (var anim in _allAnimators)
                {
                    if (anim != null) anim.enabled = false;
                }
            }
            else
            {
                // Cull animations when offscreen for performance
                foreach (var anim in _allAnimators)
                {
                    if (anim != null) anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                }
            }

            // Get NavMeshAgent for direct speed control
            _navMeshAgent = GetComponentInChildren<NavMeshAgent>();

            // Set collision detection for reliable projectile hits
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody != null)
            {
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }

            // Cache all colliders
            _allColliders = GetComponentsInChildren<Collider>();

            // Disable footstep sounds (performance)
            var footEffects = GetComponent<Opsive.UltimateCharacterController.Character.CharacterFootEffects>();
            if (footEffects != null) footEffects.enabled = false;

            // Disable IK (very expensive)
            var characterIK = GetComponent<Opsive.UltimateCharacterController.Character.CharacterIK>();
            if (characterIK != null) characterIK.enabled = false;

            // Disable PhotonAnimatorView on enemies — it expects the Animator on the
            // root GameObject, but Opsive characters have it on a child (the model).
            // This causes MissingComponentException every frame, which disrupts
            // initialization and weapon equipping. In multiplayer, NetworkEnemySync
            // already destroys it, but in single player we need to handle it here.
            var photonAnimView = GetComponent<Photon.Pun.PhotonAnimatorView>();
            if (photonAnimView != null)
            {
                Destroy(photonAnimView);
                Debug.Log($"[SentinelAI] '{_enemyName}' destroyed PhotonAnimatorView (Animator is on child, not root)");
            }

            // Disable any existing health bar canvases from Opsive prefab
            Transform existingHealthCanvas = transform.Find("HealthBar");
            if (existingHealthCanvas != null) existingHealthCanvas.gameObject.SetActive(false);

            // Find closest player
            FindClosestPlayer();

            // Get health attribute for death detection
            _attributeManager = GetComponent<Opsive.UltimateCharacterController.Traits.AttributeManager>();
            if (_attributeManager != null)
            {
                _healthAttribute = _attributeManager.GetAttribute("Health");
                if (_healthAttribute != null)
                {
                    // Subscribe to death and damage events
                    Opsive.Shared.Events.EventHandler.RegisterEvent<Vector3, Vector3, GameObject>(gameObject, "OnDeath", OnOpsiveDeath);
                    Opsive.Shared.Events.EventHandler.RegisterEvent<float, Vector3, Vector3, GameObject, Collider>(gameObject, "OnHealthDamage", OnHealthDamage);
                }
            }

            // Save spawn position for wandering
            _spawnPosition = transform.position;

            // Stagger updates across frames to spread CPU load
            _updateFrameOffset = Random.Range(0, 10);
            _lastRetargetTime = Time.time + Random.Range(0f, RETARGET_INTERVAL);

            _isInitialized = true;
            _currentState = EnemyAIState.Idle;

            // AUTO-EQUIP: Also trigger here as a fallback for non-pooled spawning.
            // OnEnable() fires BEFORE Start(), so on the first activation (e.g.
            // PhotonNetwork.Instantiate or direct Instantiate), _equipUnequipAbility
            // is still null in OnEnable(). This catches that case.
            // For pooled enemies, OnEnable() handles re-equip on subsequent activations.
            bool isMasterOrSinglePlayer = _photonView == null || !PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient;
            if (_autoEquipItemSetIndex >= 0 && _equipUnequipAbility != null && isMasterOrSinglePlayer)
            {
                StartCoroutine(AutoEquipAfterDelay());
            }
        }

        /// <summary>
        /// Waits for Opsive to finish initializing inventory, then equips the weapon.
        /// Without the delay, equip can fail because inventory isn't ready on frame 1.
        /// </summary>
        private System.Collections.IEnumerator AutoEquipAfterDelay()
        {
            // Wait a frame for Opsive to initialize
            yield return null;

            // Load the default loadout into inventory
            var inventory = GetComponent<Opsive.UltimateCharacterController.Inventory.InventoryBase>();
            if (inventory != null)
            {
                inventory.LoadDefaultLoadout();
                Debug.Log($"[SentinelAI] '{_enemyName}' loaded default loadout into inventory");
            }
            else
            {
                Debug.LogWarning($"[SentinelAI] '{_enemyName}' has no Inventory component! Can't load items.");
            }

            // Wait for item sets to generate from the loadout
            yield return new WaitForSeconds(0.5f);

            // Equip the weapon set (sniper rifle)
            if (_equipUnequipAbility != null && _autoEquipItemSetIndex >= 0)
            {
                _equipUnequipAbility.StartEquipUnequip(_autoEquipItemSetIndex, true, true);
                Debug.Log($"[SentinelAI] '{_enemyName}' auto-equipping item set index {_autoEquipItemSetIndex} (sniper rifle)");
            }
        }

        // =====================================================
        //  MAIN UPDATE LOOP — Distance-based LOD
        // =====================================================

        /// <summary>
        /// Main AI update loop. Only runs on master client in multiplayer.
        /// Uses distance-based frame skipping for performance (same as EnemyAIController).
        /// </summary>
        private void Update()
        {
            // SAFETY NET: Destroy enemies that fell through the world
            if (transform.position.y < -50f)
            {
                Debug.LogWarning($"[SentinelAI] '{_enemyName}' fell through world at Y={transform.position.y:F1}! Destroying.");

                if (_photonView != null && PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(gameObject);
                }
                else if (_photonView == null || !PhotonNetwork.IsConnected)
                {
                    gameObject.SetActive(false);
                }
                return;
            }

            // MULTIPLAYER: Only run AI on master client
            if (_photonView != null && PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
            {
                return;
            }

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
                float sqrDistToPlayer = (transform.position - _playerTransform.position).sqrMagnitude;
                _cachedDistanceToPlayer = Mathf.Sqrt(sqrDistToPlayer);

                // Disable Animator for distant enemies (big performance gain)
                float animatorCutoffDistance = 15f;
                if (!_disableAllAnimations && _animator != null)
                {
                    bool shouldAnimate = sqrDistToPlayer < (animatorCutoffDistance * animatorCutoffDistance);
                    if (_animator.enabled != shouldAnimate)
                    {
                        _animator.enabled = shouldAnimate;
                        if (_allAnimators != null)
                        {
                            foreach (var anim in _allAnimators)
                            {
                                if (anim != null) anim.enabled = shouldAnimate;
                            }
                        }
                    }
                }

                // Frame skipping based on distance
                if (sqrDistToPlayer > _veryFarDistance * _veryFarDistance)
                {
                    if ((Time.frameCount + _updateFrameOffset) % 20 != 0) return;
                }
                else if (sqrDistToPlayer > _farDistance * _farDistance)
                {
                    if ((Time.frameCount + _updateFrameOffset) % 10 != 0) return;
                }
                else if (sqrDistToPlayer > _closeDistance * _closeDistance)
                {
                    if ((Time.frameCount + _updateFrameOffset) % 5 != 0) return;
                }
                else
                {
                    if ((Time.frameCount + _updateFrameOffset) % 3 != 0) return;
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

        // =====================================================
        //  PLAYER DETECTION
        // =====================================================

        /// <summary>
        /// Find the closest player (multiplayer support).
        /// Searches all GameObjects tagged "Player" and picks the nearest one.
        /// </summary>
        private void FindClosestPlayer()
        {
            GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");

            Debug.Log($"[SentinelAI] '{_enemyName}' FindClosestPlayer: Found {allPlayers.Length} players with 'Player' tag");

            if (allPlayers.Length == 0)
            {
                _playerTransform = null;
                return;
            }

            Transform closestPlayer = null;
            float closestDistance = float.MaxValue;

            foreach (GameObject player in allPlayers)
            {
                if (player == null || !player.activeInHierarchy) continue;

                float sqrDist = (player.transform.position - transform.position).sqrMagnitude;

                if (sqrDist < closestDistance)
                {
                    closestDistance = sqrDist;
                    closestPlayer = player.transform;
                }
            }

            if (closestPlayer != null)
            {
                Debug.Log($"[SentinelAI] '{_enemyName}' selected closest player: '{closestPlayer.name}' at distance {Mathf.Sqrt(closestDistance):F2}m");
            }

            _playerTransform = closestPlayer;
        }

        /// <summary>
        /// Check if player is within detection/attack range and transition states.
        /// Ranged enemies use _maxRange as their effective attack entry range.
        /// </summary>
        private void DetectPlayer()
        {
            // Re-check for closest player periodically
            if (Time.time >= _lastRetargetTime + RETARGET_INTERVAL)
            {
                FindClosestPlayer();
                _lastRetargetTime = Time.time;
            }

            if (_playerTransform == null)
                return;

            float distanceToPlayer = _useDistanceLOD ? _cachedDistanceToPlayer :
                Vector3.Distance(transform.position, _playerTransform.position);

            if (_debugMode)
            {
                Debug.Log($"[SentinelAI] '{_enemyName}' DetectPlayer: Distance={distanceToPlayer:F2}m (maxRange={_maxRange}m, detectionRange={_detectionRange}m)");
            }

            // Ranged enemies enter attack state at their max shooting range.
            // This is much further than a melee enemy would start attacking.
            float effectiveAttackRange = _maxRange;

            // Check if player is within attack range
            if (distanceToPlayer <= effectiveAttackRange)
            {
                if (_currentState != EnemyAIState.Attacking)
                {
                    Debug.Log($"[SentinelAI] '{_enemyName}' ENTERING ATTACK STATE - player within {effectiveAttackRange}m");
                    ChangeState(EnemyAIState.Attacking);
                }
            }
            // Check if player is within detection range but beyond attack range
            else if (distanceToPlayer <= _detectionRange)
            {
                if (_currentState == EnemyAIState.Idle)
                {
                    Debug.Log($"[SentinelAI] '{_enemyName}' ENTERING CHASE STATE - player within {_detectionRange}m");
                    ChangeState(EnemyAIState.Chasing);
                }
                // If the player moved well beyond max range while we were attacking, chase them
                else if (_currentState == EnemyAIState.Attacking && distanceToPlayer > effectiveAttackRange + 5f)
                {
                    Debug.Log($"[SentinelAI] '{_enemyName}' player left attack range - switching to CHASE");
                    ChangeState(EnemyAIState.Chasing);
                }
            }
            // Player is out of range
            else
            {
                if (_currentState != EnemyAIState.Idle)
                {
                    Debug.Log($"[SentinelAI] '{_enemyName}' RETURNING TO IDLE - player out of range");
                    ChangeState(EnemyAIState.Idle);
                }
            }
        }

        // =====================================================
        //  STATE: IDLE — Patrol or wander
        // =====================================================

        /// <summary>
        /// Idle state — patrol route takes priority, falls back to random wander.
        /// Identical to EnemyAIController's idle behavior.
        /// </summary>
        private void UpdateIdleState()
        {
            // PRIORITY 1: Follow patrol route if assigned
            if (_patrolRoute != null && _patrolRoute.IsValid())
            {
                UpdatePatrolRouteFollowing();
                return;
            }

            // PRIORITY 2: Random wander (fallback)
            if (!_enableWander)
                return;

            // Disable wandering for very distant enemies
            if (_useDistanceLOD && _cachedDistanceToPlayer > _farDistance * 1.5f)
                return;

            // Check if we need a new wander target
            if (!_hasWanderTarget || Time.time >= _nextWanderTime)
            {
                PickNewWanderTarget();
                _nextWanderTime = Time.time + _wanderInterval;
            }

            // Check if we reached the wander target (throttled to every 0.5s)
            if (_hasWanderTarget && Time.time >= _lastWanderPathUpdate + 0.5f)
            {
                float sqrDistToTarget = (transform.position - _currentWanderTarget).sqrMagnitude;

                if (sqrDistToTarget <= 4f) // 2m * 2m = 4
                {
                    _hasWanderTarget = false;
                    StopMovement();
                }
                else
                {
                    UpdateWanderPath();
                }

                _lastWanderPathUpdate = Time.time;
            }
        }

        /// <summary>
        /// Pick a random point within wander radius of spawn position
        /// </summary>
        private void PickNewWanderTarget()
        {
            Vector2 randomCircle = Random.insideUnitCircle * _wanderRadius;
            _currentWanderTarget = _spawnPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
            _hasWanderTarget = true;
        }

        // =====================================================
        //  PATROL ROUTE SYSTEM — Walk between waypoints
        // =====================================================

        /// <summary>
        /// Assign a patrol route to this enemy. Called by RavagerSpawner after spawning.
        /// </summary>
        /// <param name="route">The PatrolRoute to follow (can be null to clear)</param>
        public void AssignPatrolRoute(PatrolRoute route)
        {
            _patrolRoute = route;

            // Reset patrol state for a fresh start
            _currentPatrolWaypointIndex = 0;
            _patrolWaitTimer = 0f;
            _isWaitingAtWaypoint = false;
            _patrolDirection = 1;

            if (route != null && route.IsValid())
            {
                Debug.Log($"[SentinelAI] '{_enemyName}' assigned patrol route '{route.name}' with {route.WaypointCount} waypoints");
            }
        }

        /// <summary>
        /// Main patrol update — moves enemy between waypoints with wait pauses.
        /// </summary>
        private void UpdatePatrolRouteFollowing()
        {
            if (_useDistanceLOD && _cachedDistanceToPlayer > _farDistance * 1.5f)
                return;

            // WAITING AT WAYPOINT: Count down pause timer
            if (_isWaitingAtWaypoint)
            {
                _patrolWaitTimer -= Time.deltaTime;
                if (_patrolWaitTimer > 0f) return;

                _isWaitingAtWaypoint = false;
                AdvanceToNextPatrolWaypoint();
                return;
            }

            // MOVING TO WAYPOINT: Check if arrived (throttled to every 0.5s)
            if (Time.time < _lastPatrolPathUpdate + 0.5f)
                return;

            _lastPatrolPathUpdate = Time.time;

            Vector3 waypointPos = _patrolRoute.GetWaypointPosition(_currentPatrolWaypointIndex);
            float reachedDist = _patrolRoute.WaypointReachedDistance;
            float sqrDistToWaypoint = (transform.position - waypointPos).sqrMagnitude;

            if (sqrDistToWaypoint <= reachedDist * reachedDist)
            {
                // ARRIVED — stop and wait
                StopMovement();
                _isWaitingAtWaypoint = true;
                _patrolWaitTimer = _patrolRoute.WaitTimeAtWaypoint;
            }
            else
            {
                // NOT there yet — keep walking
                SetDestinationToWaypoint(waypointPos);
            }
        }

        /// <summary>
        /// Advance to the next waypoint in the patrol route (loop or ping-pong).
        /// </summary>
        private void AdvanceToNextPatrolWaypoint()
        {
            int waypointCount = _patrolRoute.WaypointCount;

            if (_patrolRoute.LoopMode == PatrolLoopMode.Loop)
            {
                _currentPatrolWaypointIndex = (_currentPatrolWaypointIndex + 1) % waypointCount;
            }
            else // PingPong
            {
                _currentPatrolWaypointIndex += _patrolDirection;

                if (_currentPatrolWaypointIndex >= waypointCount)
                {
                    _currentPatrolWaypointIndex = waypointCount - 2;
                    _patrolDirection = -1;
                }
                else if (_currentPatrolWaypointIndex < 0)
                {
                    _currentPatrolWaypointIndex = 1;
                    _patrolDirection = 1;
                }
            }

            Vector3 nextPos = _patrolRoute.GetWaypointPosition(_currentPatrolWaypointIndex);
            SetDestinationToWaypoint(nextPos);
        }

        /// <summary>
        /// Navigate to a waypoint position at walking speed (no sprint during patrol).
        /// </summary>
        private void SetDestinationToWaypoint(Vector3 waypointPosition)
        {
            if (_pathfindingMovement != null)
            {
                _pathfindingMovement.SetDestination(waypointPosition);
                if (!_pathfindingMovement.Enabled) _pathfindingMovement.Enabled = true;
            }
            else if (_navMeshMovement != null)
            {
                _navMeshMovement.SetDestination(waypointPosition);
                if (!_navMeshMovement.Enabled) _navMeshMovement.Enabled = true;
            }

            // Walk, don't sprint, during patrol
            RestoreNormalSpeed();
            if (_speedChangeAbility != null && _speedChangeAbility.IsActive)
            {
                _characterLocomotion.TryStopAbility(_speedChangeAbility);
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
                if (!_pathfindingMovement.Enabled) _pathfindingMovement.Enabled = true;
            }
            else if (_navMeshMovement != null)
            {
                _navMeshMovement.SetDestination(_currentWanderTarget);
                if (!_navMeshMovement.Enabled) _navMeshMovement.Enabled = true;
            }
        }

        // =====================================================
        //  STATE: CHASING — Pursuing the player
        // =====================================================

        /// <summary>
        /// Chasing state — move toward the player to get within shooting range.
        /// No leap charge for snipers — they just sprint to position.
        /// </summary>
        private void UpdateChasingState()
        {
            if (_playerTransform == null)
                return;

            // Apply sprint speed while chasing
            ApplySprintSpeed();

            // Update navigation path periodically
            if (Time.time >= _lastPathUpdateTime + _pathUpdateInterval)
            {
                UpdateChasePath();
                _lastPathUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// Update navigation path to chase the player.
        /// Uses flanking offset if enabled so Sentinels spread out.
        /// </summary>
        private void UpdateChasePath()
        {
            if (_playerTransform == null)
                return;

            Vector3 targetPosition = _playerTransform.position;

            // FLANKING: Navigate to an offset point so enemies approach from different angles
            if (_enableFlanking)
            {
                float distToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

                if (distToPlayer > _flankingCutoffDistance)
                {
                    float angleRad = _flankAngle * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad)) * _flankingRadius;
                    Vector3 flankedTarget = _playerTransform.position + offset;

                    // Snap to NavMesh, fall back to direct path if off-mesh
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(flankedTarget, out hit, _flankingRadius, NavMesh.AllAreas))
                    {
                        targetPosition = hit.position;
                    }
                }
            }

            if (_debugMode)
            {
                Debug.Log($"[SentinelAI] '{_enemyName}' UpdateChasePath - target: {targetPosition}");
            }

            if (_pathfindingMovement != null)
            {
                _pathfindingMovement.SetDestination(targetPosition);
                if (!_pathfindingMovement.Enabled) _pathfindingMovement.Enabled = true;
            }
            else if (_navMeshMovement != null)
            {
                _navMeshMovement.SetDestination(targetPosition);
                if (!_navMeshMovement.Enabled) _navMeshMovement.Enabled = true;
            }
        }

        // =====================================================
        //  STATE: ATTACKING — The Sentinel's ranged combat
        //  This is where the Sentinel differs from the Ravager.
        //  Instead of circling/lunging, it holds at range and
        //  shoots, retreating if the player gets too close.
        // =====================================================

        /// <summary>
        /// Attacking state — ranged combat behavior.
        ///
        /// Three distance zones:
        /// 1. TOO CLOSE (below _minRange): Sprint away to preferred range
        /// 2. SWEET SPOT (_minRange to _preferredRange+5): Stop and shoot on cooldown
        /// 3. TOO FAR (beyond _preferredRange+5): Walk closer to get back in range
        ///
        /// The Sentinel always faces the player while in attack state.
        /// </summary>
        private void UpdateAttackingState()
        {
            if (_playerTransform == null)
                return;

            // Always face the player while in combat
            FacePlayer();

            float distToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

            if (distToPlayer < _minRange)
            {
                // ===== TOO CLOSE — RETREAT =====
                // Player is dangerously close. Sprint away from the player to get back
                // to our preferred engagement distance.

                // Calculate a retreat point directly away from the player
                Vector3 awayDir = (transform.position - _playerTransform.position).normalized;
                Vector3 retreatTarget = _playerTransform.position + awayDir * _preferredRange;

                // Snap retreat target to NavMesh so we don't path off the map
                NavMeshHit hit;
                if (NavMesh.SamplePosition(retreatTarget, out hit, 10f, NavMesh.AllAreas))
                {
                    retreatTarget = hit.position;
                }

                // Move to retreat position
                if (_pathfindingMovement != null)
                {
                    _pathfindingMovement.SetDestination(retreatTarget);
                    if (!_pathfindingMovement.Enabled)
                        _pathfindingMovement.Enabled = true;
                }
                else if (_navMeshMovement != null)
                {
                    _navMeshMovement.SetDestination(retreatTarget);
                    if (!_navMeshMovement.Enabled)
                        _navMeshMovement.Enabled = true;
                }

                // Sprint when retreating for urgency
                ApplySprintSpeed();

                if (_debugMode)
                {
                    Debug.Log($"[SentinelAI] '{_enemyName}' RETREATING - player too close ({distToPlayer:F1}m < {_minRange}m)");
                }
            }
            else if (distToPlayer > _preferredRange + 5f)
            {
                // ===== TOO FAR — CLOSE THE GAP =====
                // Player has moved beyond our effective range. Walk toward them
                // to get back within shooting distance.

                if (_pathfindingMovement != null)
                {
                    _pathfindingMovement.SetDestination(_playerTransform.position);
                    if (!_pathfindingMovement.Enabled)
                        _pathfindingMovement.Enabled = true;
                }
                else if (_navMeshMovement != null)
                {
                    _navMeshMovement.SetDestination(_playerTransform.position);
                    if (!_navMeshMovement.Enabled)
                        _navMeshMovement.Enabled = true;
                }

                // Walk, don't sprint — no rush when repositioning forward
                RestoreNormalSpeed();

                if (_debugMode)
                {
                    Debug.Log($"[SentinelAI] '{_enemyName}' CLOSING GAP - player too far ({distToPlayer:F1}m > {_preferredRange + 5f:F1}m)");
                }
            }
            else
            {
                // ===== SWEET SPOT — HOLD POSITION AND SHOOT =====
                // We're at the ideal engagement distance. Stop moving and
                // fire the weapon on cooldown.

                StopMovement();

                // Shoot on cooldown
                if (Time.time >= _lastAttackTime + _shootCooldown)
                {
                    PerformAttack();
                    _lastAttackTime = Time.time;
                }
            }
        }

        // =====================================================
        //  COMBAT HELPERS
        // =====================================================

        /// <summary>
        /// Smoothly rotates the Sentinel to face the current player target.
        /// Keeps rotation on the horizontal plane (no tilting up/down).
        /// </summary>
        private void FacePlayer()
        {
            if (_playerTransform == null)
                return;

            Vector3 directionToPlayer = (_playerTransform.position - transform.position);
            directionToPlayer.y = 0; // Keep on horizontal plane
            if (directionToPlayer.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }

        /// <summary>
        /// Perform attack on the player.
        /// Uses Opsive's Use ability to fire the equipped weapon (sniper rifle).
        /// Falls back to direct health subtraction if Use ability isn't configured.
        /// </summary>
        private void PerformAttack()
        {
            if (_playerTransform == null)
                return;

            if (_debugMode)
            {
                Debug.Log($"[SentinelAI] '{_enemyName}' FIRING at player '{_playerTransform.name}'!");
            }

            // OPTION 1: Use Opsive's weapon system (proper animation + projectile)
            if (_useAbility != null)
            {
                _characterLocomotion.TryStartAbility(_useAbility);
                return;
            }

            // OPTION 2: Fallback — directly subtract health
            if (_debugMode)
            {
                Debug.Log($"[SentinelAI] No Use ability found, using direct damage fallback ({_attackDamage} damage)");
            }

            var playerAttributeManager = _playerTransform.GetComponent<Opsive.UltimateCharacterController.Traits.AttributeManager>();
            if (playerAttributeManager != null)
            {
                var healthAttribute = playerAttributeManager.GetAttribute("Health");
                if (healthAttribute != null)
                {
                    healthAttribute.Value -= _attackDamage;

                    if (_debugMode)
                    {
                        Debug.Log($"[SentinelAI] Player health: {healthAttribute.Value}/{healthAttribute.MaxValue}");
                    }
                }
            }
        }

        // =====================================================
        //  MOVEMENT HELPERS
        // =====================================================

        /// <summary>
        /// Stop all movement and restore normal speed
        /// </summary>
        private void StopMovement()
        {
            if (_pathfindingMovement != null)
                _pathfindingMovement.Enabled = false;

            if (_navMeshMovement != null)
                _navMeshMovement.Enabled = false;

            RestoreNormalSpeed();
        }

        /// <summary>
        /// Apply sprint speed through Opsive's SpeedChange ability.
        /// Also sets NavMeshAgent speed for path planning fidelity.
        /// </summary>
        private void ApplySprintSpeed()
        {
            if (!_sprintWhenChasing)
                return;

            // Set Opsive SpeedChange multiplier and clamp
            if (_speedChangeAbility != null)
            {
                _speedChangeAbility.SpeedChangeMultiplier = _chaseSpeedMultiplier;
                _speedChangeAbility.MaxSpeedChangeValue = _chaseSpeedMultiplier + 1f;

                if (!_speedChangeAbility.IsActive)
                {
                    _characterLocomotion.TryStartAbility(_speedChangeAbility);
                }
            }

            // Also set NavMeshAgent speed for path planning
            if (_navMeshAgent != null)
            {
                if (!_isSprintSpeedApplied && _navMeshAgent.speed > 0.1f)
                {
                    _originalNavMeshSpeed = _navMeshAgent.speed;
                }
                _navMeshAgent.speed = _originalNavMeshSpeed * _chaseSpeedMultiplier;
                _isSprintSpeedApplied = true;
            }
        }

        /// <summary>
        /// Restore normal movement speed.
        /// Stops SpeedChange ability and resets NavMeshAgent speed.
        /// </summary>
        private void RestoreNormalSpeed()
        {
            if (_speedChangeAbility != null && _speedChangeAbility.IsActive)
            {
                _characterLocomotion.TryStopAbility(_speedChangeAbility);
            }

            if (_navMeshAgent != null && _isSprintSpeedApplied)
            {
                _navMeshAgent.speed = _originalNavMeshSpeed;
                _isSprintSpeedApplied = false;
            }
        }

        // =====================================================
        //  STATE MACHINE
        // =====================================================

        /// <summary>
        /// Change AI state with proper transition handling
        /// </summary>
        private void ChangeState(EnemyAIState newState)
        {
            if (_currentState == newState)
                return;

            Debug.Log($"[SentinelAI] '{_enemyName}' STATE CHANGE: {_currentState} -> {newState}");

            _currentState = newState;

            switch (newState)
            {
                case EnemyAIState.Idle:
                    StopMovement();
                    break;

                case EnemyAIState.Chasing:
                    _hasWanderTarget = false;
                    _isWaitingAtWaypoint = false;

                    // Pick a random flanking angle for this chase
                    if (_enableFlanking)
                    {
                        _flankAngle = Random.Range(0f, 360f);
                    }

                    UpdateChasePath();
                    break;

                case EnemyAIState.Attacking:
                    // Sentinel entering attack mode — restore normal speed (was sprinting during chase)
                    RestoreNormalSpeed();
                    break;

                case EnemyAIState.Dead:
                    StopMovement();
                    break;
            }
        }

        // =====================================================
        //  DAMAGE & AGGRO
        // =====================================================

        /// <summary>
        /// Called by Opsive when this Sentinel takes damage.
        /// Aggros onto the attacker, spawns blood, and alerts nearby allies.
        /// </summary>
        private void OnHealthDamage(float amount, Vector3 position, Vector3 force, GameObject attacker, Collider hitCollider)
        {
            // AGGRO: If idle and hit, chase the attacker regardless of distance
            if (attacker != null && _currentState == EnemyAIState.Idle)
            {
                _playerTransform = attacker.transform;
                Debug.Log($"[SentinelAI] '{_enemyName}' HIT by '{attacker.name}'! Aggroing and chasing attacker!");
                ChangeState(EnemyAIState.Chasing);

                // Alert nearby allies
                AlertNearbyEnemies(attacker.transform);
            }

            // Spawn hit blood effect
            if (!_useHitBloodEffect || amount < _minDamageForBlood)
                return;

            if (_hitBloodEffectPrefab != null)
            {
                GameObject hitBlood = Instantiate(_hitBloodEffectPrefab, position, Quaternion.identity);

                if (attacker != null)
                {
                    Vector3 direction = (position - attacker.transform.position).normalized;
                    hitBlood.transform.rotation = Quaternion.LookRotation(direction);
                }

                Destroy(hitBlood, 2f);
            }
        }

        /// <summary>
        /// Alert nearby idle enemies within _alertRadius to chase the attacker.
        /// Also alerts EnemyAIController enemies (Ravagers), not just other Sentinels.
        /// </summary>
        private void AlertNearbyEnemies(Transform attacker)
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, _alertRadius);
            int alertedCount = 0;

            foreach (Collider col in nearbyColliders)
            {
                if (col.gameObject == gameObject) continue;

                // Check for other Sentinels
                SentinelAIController nearbySentinel = col.GetComponentInParent<SentinelAIController>();
                if (nearbySentinel != null && nearbySentinel != this && nearbySentinel._currentState == EnemyAIState.Idle)
                {
                    nearbySentinel.AlertedByAlly(attacker);
                    alertedCount++;
                    continue; // Don't double-count if this collider also has EnemyAIController
                }

                // Also alert Ravagers (EnemyAIController enemies)
                EnemyAIController nearbyRavager = col.GetComponentInParent<EnemyAIController>();
                if (nearbyRavager != null && nearbyRavager.GetCurrentState() == EnemyAIState.Idle)
                {
                    nearbyRavager.AlertedByAlly(attacker);
                    alertedCount++;
                }
            }

            if (alertedCount > 0)
            {
                Debug.Log($"[SentinelAI] '{_enemyName}' alerted {alertedCount} nearby enemies!");
            }
        }

        /// <summary>
        /// Called by a nearby ally that got hit. Makes this Sentinel aggro on the attacker.
        /// </summary>
        public void AlertedByAlly(Transform attacker)
        {
            if (_currentState != EnemyAIState.Idle || attacker == null)
                return;

            _playerTransform = attacker;
            Debug.Log($"[SentinelAI] '{_enemyName}' ALERTED by ally! Chasing '{attacker.name}'!");
            ChangeState(EnemyAIState.Chasing);
        }

        // =====================================================
        //  DEATH & CLEANUP
        // =====================================================

        /// <summary>
        /// Called by Opsive when character dies (event callback)
        /// </summary>
        private void OnOpsiveDeath(Vector3 position, Vector3 force, GameObject attacker)
        {
            OnDeath();
        }

        /// <summary>
        /// Handle enemy death — stop movement, play effects, schedule cleanup.
        /// Public so NetworkEnemySync can call it for death sync on non-master clients.
        /// </summary>
        public void OnDeath()
        {
            if (_currentState == EnemyAIState.Dead)
                return;

            ChangeState(EnemyAIState.Dead);

            // Stop all movement
            StopMovement();

            // Reset combat state for clean pool reuse
            _flankAngle = 0f;

            // Spawn blood effect
            if (_useBloodEffect && _bloodEffectPrefab != null)
            {
                GameObject bloodFX = Instantiate(_bloodEffectPrefab, transform.position, Quaternion.identity);
                Destroy(bloodFX, 5f);
            }

            // Hide enemy body (blood replaces it visually)
            StartCoroutine(HideEnemyBody());

            // Disable AI to save performance
            enabled = false;

            // Return to pool after delay
            StartCoroutine(ReturnToPoolAfterDelay(3f));
        }

        /// <summary>
        /// Hide enemy body after death
        /// </summary>
        private System.Collections.IEnumerator HideEnemyBody()
        {
            yield return null; // Wait one frame so blood spawns first

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = false;
            }

            var healthBar = GetComponent<SimpleHealthBar>();
            if (healthBar != null) healthBar.enabled = false;
        }

        /// <summary>
        /// Return enemy to pool after death.
        /// Multiplayer: master client destroys via Photon.
        /// Single player: deactivates for pool reuse.
        /// </summary>
        private System.Collections.IEnumerator ReturnToPoolAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            // MULTIPLAYER: Master client destroys networked enemies
            if (_photonView != null && PhotonNetwork.IsConnected)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    var networkSync = GetComponent<NetworkEnemySync>();
                    if (networkSync != null)
                    {
                        networkSync.NetworkDestroy();
                    }
                    else
                    {
                        PhotonNetwork.Destroy(gameObject);
                    }
                }
                yield break;
            }

            // SINGLE PLAYER: Reset and return to pool
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = true;
            }

            var healthBar = GetComponent<SimpleHealthBar>();
            if (healthBar != null) healthBar.enabled = true;

            if (_healthAttribute != null)
            {
                _healthAttribute.Value = _healthAttribute.MaxValue;
            }

            _currentState = EnemyAIState.Idle;
            enabled = true;

            // Reset combat state for clean pool reuse
            _flankAngle = 0f;

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Get current AI state (used by other systems for status checks)
        /// </summary>
        public EnemyAIState GetCurrentState()
        {
            return _currentState;
        }

        /// <summary>
        /// Cleanup event subscriptions on destroy
        /// </summary>
        private void OnDestroy()
        {
            if (_attributeManager != null && _healthAttribute != null)
            {
                Opsive.Shared.Events.EventHandler.UnregisterEvent<Vector3, Vector3, GameObject>(gameObject, "OnDeath", OnOpsiveDeath);
                Opsive.Shared.Events.EventHandler.UnregisterEvent<float, Vector3, Vector3, GameObject, Collider>(gameObject, "OnHealthDamage", OnHealthDamage);
            }
        }

        // =====================================================
        //  DEBUG GIZMOS — Visual helpers in the Scene view
        // =====================================================

        /// <summary>
        /// Draw debug visualization in Scene view when the object is selected.
        /// Shows detection range, preferred range, min range, and current target.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Detection range (yellow)
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _detectionRange);

            // Max shooting range (red)
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _maxRange);

            // Preferred engagement range (green)
            Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, _preferredRange);

            // Minimum range / retreat threshold (orange-red)
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _minRange);

            // Wander radius
            if (_enableWander && Application.isPlaying)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
                Gizmos.DrawWireSphere(_spawnPosition, _wanderRadius);

                if (_hasWanderTarget)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(_currentWanderTarget, 0.5f);
                    Gizmos.DrawLine(transform.position, _currentWanderTarget);
                }
            }

            // Line to player while chasing
            if (Application.isPlaying && _playerTransform != null && _currentState == EnemyAIState.Chasing)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _playerTransform.position);

                // Show flanking offset point
                if (_enableFlanking)
                {
                    float distToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
                    if (distToPlayer > _flankingCutoffDistance)
                    {
                        float angleRad = _flankAngle * Mathf.Deg2Rad;
                        Vector3 offset = new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad)) * _flankingRadius;
                        Vector3 flankTarget = _playerTransform.position + offset;
                        Gizmos.color = Color.blue;
                        Gizmos.DrawSphere(flankTarget, 0.4f);
                        Gizmos.DrawLine(transform.position, flankTarget);
                    }
                }
            }

            // Line to player while attacking (cyan = shooting, orange = retreating)
            if (Application.isPlaying && _playerTransform != null && _currentState == EnemyAIState.Attacking)
            {
                float dist = Vector3.Distance(transform.position, _playerTransform.position);
                Gizmos.color = dist < _minRange ? new Color(1f, 0.5f, 0f) : Color.cyan;
                Gizmos.DrawLine(transform.position, _playerTransform.position);
            }

            // Patrol waypoint
            if (Application.isPlaying && _patrolRoute != null && _patrolRoute.IsValid() && _currentState == EnemyAIState.Idle)
            {
                Gizmos.color = Color.cyan;
                Vector3 waypointPos = _patrolRoute.GetWaypointPosition(_currentPatrolWaypointIndex);
                Gizmos.DrawLine(transform.position, waypointPos);
                Gizmos.DrawSphere(waypointPos, 0.3f);
            }
        }
    }
}
