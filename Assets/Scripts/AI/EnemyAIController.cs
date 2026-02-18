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
using UnityEngine.AI;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using Opsive.UltimateCharacterController.Character.Abilities.AI;
using Opsive.UltimateCharacterController.Character.Abilities.Items;
using Photon.Pun;

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
        // No longer static - each enemy tracks their own target

        [Header("Enemy Info")]
        [Tooltip("Display name for this enemy")]
        [SerializeField] private string _enemyName = "Enemy";

        [Tooltip("Type of enemy (for logging/debugging)")]
        [SerializeField] private string _enemyType = "Thrall";

        [Header("Detection Settings")]
        [Tooltip("How far the enemy can detect the player")]
        [SerializeField] private float _detectionRange = 25f;

        [Tooltip("How close player needs to be to attack")]
        [SerializeField] private float _attackRange = 2f;

        [Tooltip("How often to check for player (seconds)")]
        [SerializeField] private float _detectionInterval = 1f; // Checks for players more often

        [Tooltip("When hit, alert nearby enemies within this radius to also attack")]
        [SerializeField] private float _alertRadius = 30f;

        [Header("Chase Settings")]
        [Tooltip("How close to get to player before stopping")]
        [SerializeField] private float _stopDistance = 1.5f;

        [Tooltip("How often to update path while chasing (seconds)")]
        [SerializeField] private float _pathUpdateInterval = 1.5f; // Updates path more often for tighter chasing

        [Tooltip("Should enemies sprint when chasing? (Check for fast enemies, uncheck for slow zombies)")]
        [SerializeField] private bool _sprintWhenChasing = true; // Enable sprinting for faster movement

        [Tooltip("Speed multiplier when sprinting/chasing (2 = double speed). Applied directly to NavMeshAgent.")]
        [SerializeField] private float _chaseSpeedMultiplier = 2.5f;

        [Header("Attack Settings")]
        [Tooltip("Time between attacks (seconds)")]
        [SerializeField] private float _attackCooldown = 1.5f;

        [Tooltip("Damage per attack")]
        [SerializeField] private float _attackDamage = 20f;

        [Tooltip("Which Item Set index to equip on spawn (0 = first item set, 1 = second, etc.). " +
                 "Set to -1 to not auto-equip anything. Check your Item Set Manager to find the right index.")]
        [SerializeField] private int _autoEquipItemSetIndex = 0;

        [Tooltip("Minimum time between combo swings during a lunge (seconds). " +
                 "The Opsive RepeatCombo module auto-advances substates 2→3→4 when Use is triggered while a previous attack is playing.")]
        [SerializeField] private float _comboAttackInterval = 0.45f;

        [Tooltip("Maximum number of attacks per lunge. Matches the 3-hit sword combo (substates 2, 3, 4).")]
        [SerializeField] private int _maxComboHits = 3;

        // =====================================================
        //  FLANKING — Enemies approach from different angles
        //  instead of clumping into a single blob.
        // =====================================================
        [Header("Flanking")]
        [Tooltip("Enable flanking? Each enemy picks a random approach angle so they spread out around the player.")]
        [SerializeField] private bool _enableFlanking = true;

        [Tooltip("How far from the player the flanking offset point is (meters). Enemies navigate to this offset point instead of directly at the player.")]
        [SerializeField] private float _flankingRadius = 4f;

        [Tooltip("When closer than this distance, drop the flanking offset and go straight for the player.")]
        [SerializeField] private float _flankingCutoffDistance = 3f;

        // =====================================================
        //  CIRCLING / LUNGING — Enemies orbit the player and
        //  take turns lunging in to attack, creating a
        //  read-and-react combat rhythm.
        // =====================================================
        [Header("Circling Attacks")]
        [Tooltip("Enable circling behavior? Enemies orbit the player and take turns lunging in to attack.")]
        [SerializeField] private bool _enableCircling = true;

        [Tooltip("Radius at which enemies orbit the player while circling (meters).")]
        [SerializeField] private float _circlingRadius = 3f;

        [Tooltip("How fast the enemy orbits the player (degrees per second).")]
        [SerializeField] private float _circlingSpeed = 30f;

        [Tooltip("Minimum time spent circling before lunging in to attack (seconds).")]
        [SerializeField] private float _minAttackWaitTime = 2f;

        [Tooltip("Maximum time spent circling before lunging in to attack (seconds).")]
        [SerializeField] private float _maxAttackWaitTime = 4f;

        [Tooltip("How long the lunge attack lasts before retreating back to circling (seconds). " +
                 "Set to 1.8s to allow time for 2-3 combo hits during each lunge.")]
        [SerializeField] private float _lungeDuration = 1.8f;

        // =====================================================
        //  LEAP CHARGE — During chase, enemies occasionally
        //  burst forward at high speed to close the gap.
        //  Scary, unpredictable gap-closer.
        // =====================================================
        [Header("Leap Charge")]
        [Tooltip("Enable leap charge? Enemies occasionally burst forward at high speed during chase.")]
        [SerializeField] private bool _enableLeapCharge = true;

        [Tooltip("Maximum distance to player for a charge to trigger (meters).")]
        [SerializeField] private float _chargeRange = 12f;

        [Tooltip("Minimum distance to player for a charge to trigger (meters). Prevents charging when already close.")]
        [SerializeField] private float _chargeMinRange = 4f;

        [Tooltip("Chance per second of triggering a charge (0-1). 0.3 = 30% chance each second.")]
        [SerializeField] private float _chargeChance = 0.3f;

        [Tooltip("Speed multiplier during charge (4 = 4x normal speed).")]
        [SerializeField] private float _chargeSpeedMultiplier = 4f;

        [Tooltip("How long the charge burst lasts (seconds).")]
        [SerializeField] private float _chargeDuration = 1f;

        [Tooltip("Cooldown between charges (seconds). Prevents spam-charging.")]
        [SerializeField] private float _chargeCooldown = 8f;

        [Header("Wander Settings")]
        [Tooltip("Should enemy wander when idle?")]
        [SerializeField] private bool _enableWander = false; // Disabled for performance

        [Tooltip("How far from spawn point to wander")]
        [SerializeField] private float _wanderRadius = 10f;

        [Tooltip("Time between picking new wander points (seconds)")]
        [SerializeField] private float _wanderInterval = 10f; // Increased from 5

        [Header("Patrol Settings")]
        [Tooltip("Patrol route assigned by the spawner (overrides random wander when set)")]
        [SerializeField] private PatrolRoute _patrolRoute;

        [Tooltip("Should the enemy start patrolling immediately, or wait until first idle?")]
        [SerializeField] private bool _patrolOnStart = true;

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
        private Use _useAbility; // Opsive's Use ability for triggering melee attacks
        private EquipUnequip _equipUnequipAbility; // Opsive's ability for equipping items (sword, etc.)
        private Opsive.UltimateCharacterController.Traits.AttributeManager _attributeManager;
        private Opsive.UltimateCharacterController.Traits.Attribute _healthAttribute;
        private Animator _animator;
        private Animator[] _allAnimators; // Cache to avoid duplicate GetComponentsInChildren calls
        private Rigidbody _rigidbody;
        private Collider[] _allColliders;

        // NavMeshAgent direct speed control (more reliable than SpeedChange ability)
        private UnityEngine.AI.NavMeshAgent _navMeshAgent;
        private float _originalNavMeshSpeed;  // Saved on startup so we can restore it
        private bool _isSprintSpeedApplied = false;

        // Photon components
        private PhotonView _photonView;

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

        // Patrol tracking
        private int _currentPatrolWaypointIndex = 0;     // Which waypoint we're heading to
        private float _patrolWaitTimer = 0f;              // Countdown timer for pausing at waypoints
        private bool _isWaitingAtWaypoint = false;        // Are we currently paused at a waypoint?
        private int _patrolDirection = 1;                 // +1 = forward, -1 = reverse (for ping-pong mode)
        private float _lastPatrolPathUpdate = 0f;         // Throttle patrol path updates (same as wander)

        // Performance optimization
        private int _updateFrameOffset;
        private float _cachedDistanceToPlayer;
        private float _lastWanderPathUpdate = 0f;

        // Retarget timer - how often to re-evaluate closest player
        private float _lastRetargetTime = 0f;
        private const float RETARGET_INTERVAL = 3f; // Re-check for closest player every 3 seconds

        // Flanking state - random approach angle assigned when entering chase
        private float _flankAngle = 0f;

        // Circling / lunging state - sub-phases within the Attacking state
        // NOTE: Timers use absolute Time.time comparisons, NOT deltaTime subtraction.
        // This is critical because the LOD frame-skipping system means Update() only runs
        // every 3-20 frames. Using Time.deltaTime would make timers run at 1/3 to 1/20 speed.
        private bool _isLunging = false;           // True = sprinting at player to attack; False = orbiting
        private float _nextLungeTime = 0f;          // Absolute time when the next lunge should start
        private float _lungeEndTime = 0f;           // Absolute time when the current lunge ends
        private float _circleAngle = 0f;            // Current orbit position around the player (degrees)
        private float _lastCircleUpdateTime = 0f;   // For frame-rate-independent orbit advancement
        private int _comboHitsThisLunge = 0;           // Tracks how many combo hits fired this lunge (max _maxComboHits)

        // Leap charge state - burst of speed during chase
        private bool _isCharging = false;           // Currently in a charge burst
        private float _chargeEndTime = 0f;          // Absolute time when current charge ends
        private float _lastChargeTime = -999f;      // Last time a charge ended (for cooldown)

        /// <summary>
        /// Initialize the enemy AI
        /// </summary>
        private void Start()
        {
            InitializeAI();
        }

        /// <summary>
        /// Called every time the GameObject is activated (including when pulled from
        /// the object pool). Triggers weapon auto-equip here instead of Start()
        /// because Start() only fires once per lifetime — when the pool deactivates
        /// and reactivates the enemy, Start() doesn't run again, so the 0.5s
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
        /// Setup AI components and find player
        /// </summary>
        private void InitializeAI()
        {
            // MULTIPLAYER: Check if this is a networked enemy
            _photonView = GetComponent<PhotonView>();

            // Log multiplayer status (AI logic in Update is guarded by IsMasterClient check,
            // but we still initialize fully on all clients so health, death effects, etc. work)
            if (_photonView != null && PhotonNetwork.IsConnected)
            {
                if (!PhotonNetwork.IsMasterClient)
                {
                    Debug.Log($"[EnemyAI] '{_enemyName}' is on non-master client - AI won't run but components initialized for sync");
                }
                else
                {
                    Debug.Log($"[EnemyAI] '{_enemyName}' is on MASTER CLIENT - AI will run here");
                }
            }
            else
            {
                Debug.Log($"[EnemyAI] '{_enemyName}' is in SINGLE PLAYER mode - AI will run normally");
            }

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

            // Get Use ability for melee attacks (triggers the equipped weapon's attack)
            // Try GetItemAbility first (Item Abilities list), fallback to GetAbility (regular Abilities list)
            _useAbility = _characterLocomotion.GetItemAbility<Use>();
            if (_useAbility == null)
                _useAbility = _characterLocomotion.GetAbility<Use>();
            if (_useAbility != null)
            {
                Debug.Log($"[EnemyAI] '{_enemyName}' has Use ability - will use Opsive melee attacks");
            }

            // Get EquipUnequip ability so we can auto-equip the sword on spawn
            // Must use GetItemAbility because EquipUnequip is in the Item Abilities list, not regular Abilities
            _equipUnequipAbility = _characterLocomotion.GetItemAbility<EquipUnequip>();
            if (_equipUnequipAbility == null)
                _equipUnequipAbility = _characterLocomotion.GetAbility<EquipUnequip>();
            if (_equipUnequipAbility != null)
            {
                Debug.Log($"[EnemyAI] '{_enemyName}' has EquipUnequip ability - will auto-equip item set {_autoEquipItemSetIndex}");
            }
            else
            {
                Debug.LogWarning($"[EnemyAI] '{_enemyName}' could NOT find EquipUnequip ability! Sword/shield won't be equipped.");
            }

            if (_pathfindingMovement == null && _navMeshMovement == null)
            {
                // On non-master clients, movement abilities aren't needed because
                // NetworkEnemySync handles position/rotation. Don't disable the script
                // since OnDeath() still needs to be callable for death sync.
                if (_photonView != null && PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
                {
                    Debug.Log($"[EnemyAI] '{_enemyName}' no movement abilities on non-master client - OK, NetworkEnemySync handles position");
                }
                else
                {
                    Debug.LogError($"[EnemyAI] '{_enemyName}' has no PathfindingMovement or NavMeshAgentMovement ability! Add one to the character.");
                    enabled = false;
                    return;
                }
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

            // SPRINT SPEED: Get the NavMeshAgent directly so we can change its speed
            // when chasing. This is more reliable than the SpeedChange ability which
            // might not be configured on the prefab.
            // NOTE: We DON'T cache the speed here because Opsive may not have set it yet.
            // Instead, _originalNavMeshSpeed is captured right before the first sprint.
            _navMeshAgent = GetComponentInChildren<NavMeshAgent>();

            // PHYSICS: Get Rigidbody and set collision detection mode.
            // ContinuousSpeculative prevents fast-moving projectiles (fireballs) from
            // tunneling through the enemy's collider. This matters because enemies now
            // move during combat (circling, lunging, charging), increasing relative velocity
            // between projectile and enemy. Slightly more expensive than Discrete but
            // ensures reliable hit detection.
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody != null)
            {
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
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

            // Disable PhotonAnimatorView on enemies — it expects the Animator on the
            // root GameObject, but Opsive characters have it on a child (the model).
            // This causes MissingComponentException every frame, which disrupts
            // initialization and weapon equipping. In multiplayer, NetworkEnemySync
            // already destroys it, but in single player we need to handle it here.
            var photonAnimView = GetComponent<Photon.Pun.PhotonAnimatorView>();
            if (photonAnimView != null)
            {
                Destroy(photonAnimView);
            }

            // Disable any existing health bar canvases from Opsive prefab
            Transform existingHealthCanvas = transform.Find("HealthBar");
            if (existingHealthCanvas != null)
            {
                existingHealthCanvas.gameObject.SetActive(false);
            }

            // Find closest player (multiplayer support)
            FindClosestPlayer();

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

            // Stagger retarget timer so all enemies don't retarget on the same frame
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
        /// Waits a short moment for Opsive to finish setting up inventory, then equips the weapon.
        /// Without this delay, the equip can fail because inventory isn't ready yet on frame 1.
        /// </summary>
        private System.Collections.IEnumerator AutoEquipAfterDelay()
        {
            // Wait a frame for Opsive to initialize
            yield return null;

            // STEP 1: Force-load the default loadout into inventory.
            // "Load Default Loadout On Respawn" only triggers on respawn, not initial spawn.
            // So for freshly spawned enemies, we manually load it here.
            var inventory = GetComponent<Opsive.UltimateCharacterController.Inventory.InventoryBase>();
            if (inventory != null)
            {
                inventory.LoadDefaultLoadout();
                Debug.Log($"[EnemyAI] '{_enemyName}' manually loaded default loadout into inventory");
            }
            else
            {
                Debug.LogWarning($"[EnemyAI] '{_enemyName}' has no Inventory component! Can't load items.");
            }

            // STEP 2: Wait for item sets to generate from the loadout we just loaded.
            // The Item Set Manager needs a moment to process the new items.
            yield return new WaitForSeconds(0.5f);

            // STEP 3: Equip the weapon set
            if (_equipUnequipAbility != null && _autoEquipItemSetIndex >= 0)
            {
                _equipUnequipAbility.StartEquipUnequip(_autoEquipItemSetIndex, true, true);
                Debug.Log($"[EnemyAI] '{_enemyName}' auto-equipping item set index {_autoEquipItemSetIndex} (sword/shield) - FORCED instant");
            }

        }

        /// <summary>
        /// Main AI update loop with distance-based LOD
        /// </summary>
        private void Update()
        {
            // SAFETY NET: Catch enemies that somehow fell through the world.
            // If an enemy falls below Y = -50, it's definitely off the map.
            // Master client destroys the networked enemy; single player just deactivates it.
            if (transform.position.y < -50f)
            {
                Debug.LogWarning($"[EnemyAI] '{_enemyName}' fell through world at Y={transform.position.y:F1}! Destroying.");

                if (_photonView != null && PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient)
                {
                    // Master client destroys the networked enemy via Photon
                    PhotonNetwork.Destroy(gameObject);
                }
                else if (_photonView == null || !PhotonNetwork.IsConnected)
                {
                    // Single player: deactivate for pool reuse
                    gameObject.SetActive(false);
                }
                // Non-master clients do nothing - master will handle destruction
                return;
            }

            // MULTIPLAYER: Only run AI on master client
            if (_photonView != null && PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
            {
                return; // Other clients don't run AI - they receive synced data from master
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
        /// Find the closest player (multiplayer support)
        /// Called periodically for performance
        /// </summary>
        private void FindClosestPlayer()
        {
            // Find all players in the scene
            GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");

            Debug.Log($"[EnemyAI] '{_enemyName}' FindClosestPlayer: Found {allPlayers.Length} players with 'Player' tag");

            if (allPlayers.Length == 0)
            {
                _playerTransform = null;
                return;
            }

            // Find closest player
            Transform closestPlayer = null;
            float closestDistance = float.MaxValue;

            foreach (GameObject player in allPlayers)
            {
                // Skip if player is dead or invalid
                if (player == null || !player.activeInHierarchy)
                {
                    Debug.Log($"[EnemyAI] Skipping null or inactive player");
                    continue;
                }

                float sqrDist = (player.transform.position - transform.position).sqrMagnitude;
                float actualDist = Mathf.Sqrt(sqrDist);

                Debug.Log($"[EnemyAI] Player '{player.name}' at position {player.transform.position}, distance: {actualDist:F2}m from enemy at {transform.position}");

                if (sqrDist < closestDistance)
                {
                    closestDistance = sqrDist;
                    closestPlayer = player.transform;
                }
            }

            if (closestPlayer != null)
            {
                Debug.Log($"[EnemyAI] '{_enemyName}' selected closest player: '{closestPlayer.name}' at distance {Mathf.Sqrt(closestDistance):F2}m");
            }

            _playerTransform = closestPlayer;
        }

        /// <summary>
        /// Detect if player is within range (optimized with sqrMagnitude)
        /// </summary>
        private void DetectPlayer()
        {
            // Update closest player target periodically using a proper timer
            // This allows enemies to switch targets if a closer player appears
            if (Time.time >= _lastRetargetTime + RETARGET_INTERVAL)
            {
                FindClosestPlayer();
                _lastRetargetTime = Time.time;
            }

            if (_playerTransform == null)
                return;

            // Use cached distance if available, otherwise calculate
            float distanceToPlayer = _useDistanceLOD ? _cachedDistanceToPlayer :
                Vector3.Distance(transform.position, _playerTransform.position);

            Debug.Log($"[EnemyAI] '{_enemyName}' DetectPlayer: Distance to '{_playerTransform.name}' is {distanceToPlayer:F2}m (Attack range: {_attackRange}m, Detection range: {_detectionRange}m)");

            // CIRCLING: Use a wider effective attack range so the enemy enters attack state
            // at circling distance instead of melee distance. This prevents flickering between
            // Chase and Attack when the enemy is orbiting at _circlingRadius.
            float effectiveAttackRange = _attackRange;
            if (_enableCircling)
            {
                effectiveAttackRange = _circlingRadius + 1f;
            }

            // Check if player is within attack range
            if (distanceToPlayer <= effectiveAttackRange)
            {
                if (_currentState != EnemyAIState.Attacking)
                {
                    Debug.Log($"[EnemyAI] '{_enemyName}' ENTERING ATTACK STATE - player within {effectiveAttackRange}m");
                    ChangeState(EnemyAIState.Attacking);
                }
            }
            // Check if player is within detection range but beyond attack range
            else if (distanceToPlayer <= _detectionRange)
            {
                if (_currentState == EnemyAIState.Idle)
                {
                    Debug.Log($"[EnemyAI] '{_enemyName}' ENTERING CHASE STATE - player within {_detectionRange}m");
                    ChangeState(EnemyAIState.Chasing);
                }
                // If the player moved well beyond attack range while we were attacking, chase them.
                // Uses a wider threshold than the entry range (hysteresis) to prevent flickering
                // between Attack and Chase when the enemy orbits near the edge of attack range.
                else if (_currentState == EnemyAIState.Attacking && distanceToPlayer > effectiveAttackRange + 3f)
                {
                    Debug.Log($"[EnemyAI] '{_enemyName}' player left attack range - switching to CHASE");
                    ChangeState(EnemyAIState.Chasing);
                }
            }
            // Player is out of range
            else
            {
                if (_currentState != EnemyAIState.Idle)
                {
                    Debug.Log($"[EnemyAI] '{_enemyName}' RETURNING TO IDLE - player out of range");
                    ChangeState(EnemyAIState.Idle);
                }
            }
        }

        /// <summary>
        /// Idle state - patrol route takes priority, falls back to random wander.
        /// Patrol routes make enemies feel alive (walking guard duty) instead of standing still.
        /// </summary>
        private void UpdateIdleState()
        {
            // PRIORITY 1: Follow patrol route if one is assigned and valid
            if (_patrolRoute != null && _patrolRoute.IsValid())
            {
                UpdatePatrolRouteFollowing();
                return; // Patrol takes over - skip random wander
            }

            // PRIORITY 2: Random wander (original behavior, used as fallback)
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

        // =====================================================
        //  PATROL ROUTE SYSTEM
        //  Enemies walk between waypoints when idle, making
        //  the outpost feel guarded and alive.
        // =====================================================

        /// <summary>
        /// Assign a patrol route to this enemy. Called by RavagerSpawner after spawning.
        /// The enemy will follow this route when idle instead of random wandering.
        /// </summary>
        /// <param name="route">The PatrolRoute to follow (can be null to clear)</param>
        public void AssignPatrolRoute(PatrolRoute route)
        {
            _patrolRoute = route;

            // Reset patrol state so we start fresh from the beginning of the route
            _currentPatrolWaypointIndex = 0;
            _patrolWaitTimer = 0f;
            _isWaitingAtWaypoint = false;
            _patrolDirection = 1;

            if (route != null && route.IsValid())
            {
                Debug.Log($"[EnemyAI] '{_enemyName}' assigned patrol route '{route.name}' with {route.WaypointCount} waypoints");
            }
        }

        /// <summary>
        /// Main patrol update - moves enemy between waypoints with wait pauses.
        /// Uses the same 0.5s throttle + distance LOD as wandering for performance.
        /// Called from UpdateIdleState when a patrol route is assigned.
        /// </summary>
        private void UpdatePatrolRouteFollowing()
        {
            // Skip patrol for very distant enemies (same performance optimization as wander)
            if (_useDistanceLOD && _cachedDistanceToPlayer > _farDistance * 1.5f)
                return;

            // WAITING AT WAYPOINT: Count down the pause timer
            if (_isWaitingAtWaypoint)
            {
                _patrolWaitTimer -= Time.deltaTime;

                // Still waiting - don't move yet
                if (_patrolWaitTimer > 0f)
                    return;

                // Done waiting - advance to the next waypoint
                _isWaitingAtWaypoint = false;
                AdvanceToNextPatrolWaypoint();
                return;
            }

            // MOVING TO WAYPOINT: Check if we've arrived (throttled to every 0.5s)
            if (Time.time < _lastPatrolPathUpdate + 0.5f)
                return;

            _lastPatrolPathUpdate = Time.time;

            // Get current waypoint position
            Vector3 waypointPos = _patrolRoute.GetWaypointPosition(_currentPatrolWaypointIndex);
            float reachedDist = _patrolRoute.WaypointReachedDistance;

            // Check if we're close enough to the waypoint (sqrMagnitude avoids expensive sqrt)
            float sqrDistToWaypoint = (transform.position - waypointPos).sqrMagnitude;
            float sqrReachedDist = reachedDist * reachedDist;

            if (sqrDistToWaypoint <= sqrReachedDist)
            {
                // ARRIVED at waypoint - stop and start waiting
                StopMovement();
                _isWaitingAtWaypoint = true;
                _patrolWaitTimer = _patrolRoute.WaitTimeAtWaypoint;
            }
            else
            {
                // NOT there yet - update path to the waypoint
                SetDestinationToWaypoint(waypointPos);
            }
        }

        /// <summary>
        /// Move to the next waypoint in the patrol route.
        /// Handles both Loop (circular) and PingPong (back-and-forth) modes.
        /// </summary>
        private void AdvanceToNextPatrolWaypoint()
        {
            int waypointCount = _patrolRoute.WaypointCount;

            if (_patrolRoute.LoopMode == PatrolLoopMode.Loop)
            {
                // LOOP MODE: 0 → 1 → 2 → 3 → 0 → 1 → 2 → 3 → ...
                _currentPatrolWaypointIndex = (_currentPatrolWaypointIndex + 1) % waypointCount;
            }
            else // PingPong
            {
                // PING-PONG MODE: 0 → 1 → 2 → 3 → 2 → 1 → 0 → 1 → 2 → ...
                _currentPatrolWaypointIndex += _patrolDirection;

                // Reverse direction when we hit either end
                if (_currentPatrolWaypointIndex >= waypointCount)
                {
                    _currentPatrolWaypointIndex = waypointCount - 2; // Step back from the end
                    _patrolDirection = -1; // Start going backwards
                }
                else if (_currentPatrolWaypointIndex < 0)
                {
                    _currentPatrolWaypointIndex = 1; // Step forward from the start
                    _patrolDirection = 1; // Start going forwards
                }
            }

            // Immediately start moving to the new waypoint
            Vector3 nextPos = _patrolRoute.GetWaypointPosition(_currentPatrolWaypointIndex);
            SetDestinationToWaypoint(nextPos);
        }

        /// <summary>
        /// Tell the movement system to navigate to a specific waypoint position.
        /// Uses whichever movement ability is available (PathfindingMovement or NavMeshAgentMovement).
        /// </summary>
        /// <param name="waypointPosition">World position of the waypoint to walk to</param>
        private void SetDestinationToWaypoint(Vector3 waypointPosition)
        {
            // Use PathfindingMovement if available (preferred)
            if (_pathfindingMovement != null)
            {
                _pathfindingMovement.SetDestination(waypointPosition);
                if (!_pathfindingMovement.Enabled)
                {
                    _pathfindingMovement.Enabled = true;
                }
            }
            // Fall back to NavMeshAgentMovement
            else if (_navMeshMovement != null)
            {
                _navMeshMovement.SetDestination(waypointPosition);
                if (!_navMeshMovement.Enabled)
                {
                    _navMeshMovement.Enabled = true;
                }
            }

            // Make sure we're NOT sprinting during patrol (enemies casually walk their route)
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
        /// Chasing state - pursuing the player at sprint speed.
        /// LEAP CHARGE: Has a random chance each second to burst forward at high speed
        /// for a short duration, creating an unpredictable gap-closer effect.
        /// </summary>
        private void UpdateChasingState()
        {
            if (_playerTransform == null)
                return;

            // ----- LEAP CHARGE LOGIC -----
            if (_enableLeapCharge)
            {
                if (_isCharging)
                {
                    // Check if charge duration has elapsed (absolute time, LOD-safe)
                    if (Time.time >= _chargeEndTime)
                    {
                        EndCharge();
                    }
                    else
                    {
                        // Force charge speed every frame through Opsive's SpeedChange
                        if (_speedChangeAbility != null)
                        {
                            _speedChangeAbility.SpeedChangeMultiplier = _chargeSpeedMultiplier;
                            _speedChangeAbility.MaxSpeedChangeValue = _chargeSpeedMultiplier + 1f;
                        }
                    }
                }
                else
                {
                    // Not charging — check if we should start one
                    float distToPlayer = _useDistanceLOD ? _cachedDistanceToPlayer :
                        Vector3.Distance(transform.position, _playerTransform.position);

                    // Only charge if within the right distance band and cooldown has elapsed
                    if (distToPlayer >= _chargeMinRange && distToPlayer <= _chargeRange &&
                        Time.time >= _lastChargeTime + _chargeCooldown)
                    {
                        // Random chance check — use a fixed per-check probability that works
                        // regardless of LOD frame skipping. At ~20 checks/sec (every 3 frames
                        // at 60fps), 0.015 per check ≈ 30% per second.
                        if (Random.value < 0.015f)
                        {
                            StartCharge();
                        }
                    }
                }
            }

            // Apply sprint speed through Opsive's SpeedChange ability
            if (!_isCharging)
            {
                ApplySprintSpeed();
            }

            // Update path periodically
            if (Time.time >= _lastPathUpdateTime + _pathUpdateInterval)
            {
                UpdatePath();
                _lastPathUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// Begin a leap charge — burst forward at extreme speed.
        /// Sets Opsive SpeedChange to the charge multiplier (higher than sprint).
        /// </summary>
        private void StartCharge()
        {
            _isCharging = true;
            _chargeEndTime = Time.time + _chargeDuration;

            // Boost SpeedChange to charge multiplier (e.g. 4x instead of normal 2.5x)
            if (_speedChangeAbility != null)
            {
                _speedChangeAbility.SpeedChangeMultiplier = _chargeSpeedMultiplier;
                _speedChangeAbility.MaxSpeedChangeValue = _chargeSpeedMultiplier + 1f;

                if (!_speedChangeAbility.IsActive)
                {
                    _characterLocomotion.TryStartAbility(_speedChangeAbility);
                }
            }

            // Also boost NavMeshAgent for path planning
            if (_navMeshAgent != null)
            {
                if (!_isSprintSpeedApplied && _navMeshAgent.speed > 0.1f)
                {
                    _originalNavMeshSpeed = _navMeshAgent.speed;
                }
                _navMeshAgent.speed = _originalNavMeshSpeed * _chargeSpeedMultiplier;
                _isSprintSpeedApplied = true;
            }

            if (_debugMode)
            {
                Debug.Log($"[EnemyAI] '{_enemyName}' LEAP CHARGE! Multiplier: {_chargeSpeedMultiplier}x");
            }
        }

        /// <summary>
        /// End a leap charge — restore to normal sprint speed (not walk speed).
        /// Records the time so the cooldown can be enforced.
        /// </summary>
        private void EndCharge()
        {
            _isCharging = false;
            _lastChargeTime = Time.time;

            // Restore SpeedChange back to chase multiplier (still sprinting, just not charging)
            if (_speedChangeAbility != null)
            {
                _speedChangeAbility.SpeedChangeMultiplier = _chaseSpeedMultiplier;
                _speedChangeAbility.MaxSpeedChangeValue = _chaseSpeedMultiplier + 1f;
            }

            // Restore NavMeshAgent to sprint speed for path planning
            if (_navMeshAgent != null)
            {
                _navMeshAgent.speed = _originalNavMeshSpeed * _chaseSpeedMultiplier;
            }

            if (_debugMode)
            {
                Debug.Log($"[EnemyAI] '{_enemyName}' charge ended, back to {_chaseSpeedMultiplier}x sprint");
            }
        }

        /// <summary>
        /// Attacking state - two modes depending on _enableCircling:
        ///
        /// CIRCLING MODE (new): Enemies orbit the player at _circlingRadius, slowly rotating
        /// their angle. After a random wait (2-4s), they lunge in, attack once, then retreat
        /// back to circling. This creates a read-and-react combat rhythm where enemies take
        /// turns attacking instead of all swinging at once.
        ///
        /// ORIGINAL MODE (fallback): Stand still and swing on cooldown (old behavior).
        /// Used when _enableCircling is false.
        /// </summary>
        private void UpdateAttackingState()
        {
            if (_playerTransform == null)
                return;

            // ===== CIRCLING MODE =====
            // All timers use absolute Time.time comparisons instead of deltaTime subtraction.
            // This is critical because the LOD frame-skipping system means this method only
            // runs every 3-20 frames. Time.deltaTime would only give the last frame's delta
            // (~16ms) when the REAL elapsed time is 3-20x that. Time.time always works correctly.
            if (_enableCircling)
            {
                if (_isLunging)
                {
                    // ----- LUNGING SUB-STATE -----
                    // Sprint directly at the player, attack once, then retreat to circling

                    FacePlayer();

                    // Move toward the player
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

                    // Apply sprint speed during lunge for a burst of aggression
                    ApplySprintSpeed();

                    // COMBO ATTACKS: Fire up to _maxComboHits rapid attacks during the lunge.
                    // Each call to PerformAttack() triggers TryStartAbility(Use), and the Opsive
                    // RepeatCombo module auto-advances through substates 2→3→4 when triggered
                    // while a previous attack animation is still playing.
                    if (_comboHitsThisLunge < _maxComboHits)
                    {
                        float distToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
                        if (distToPlayer <= _attackRange && Time.time >= _lastAttackTime + _comboAttackInterval)
                        {
                            PerformAttack();
                            _lastAttackTime = Time.time;
                            _comboHitsThisLunge++;
                        }
                    }

                    // Check if lunge duration has elapsed (absolute time, LOD-safe)
                    if (Time.time >= _lungeEndTime)
                    {
                        // Lunge finished — retreat back to circling
                        _isLunging = false;
                        _comboHitsThisLunge = 0;
                        _nextLungeTime = Time.time + Random.Range(_minAttackWaitTime, _maxAttackWaitTime);
                        _lastCircleUpdateTime = Time.time;
                        RestoreNormalSpeed();

                        if (_debugMode)
                        {
                            float waitTime = _nextLungeTime - Time.time;
                            Debug.Log($"[EnemyAI] '{_enemyName}' lunge complete, returning to circle (next lunge in {waitTime:F1}s)");
                        }
                    }
                }
                else
                {
                    // ----- CIRCLING SUB-STATE -----
                    // Orbit the player at _circlingRadius, slowly rotating angle.
                    // When the wait timer expires, transition to lunging.

                    FacePlayer();

                    // Advance the orbit angle using real elapsed time (LOD-safe)
                    float circleElapsed = Time.time - _lastCircleUpdateTime;
                    _lastCircleUpdateTime = Time.time;
                    _circleAngle += _circlingSpeed * circleElapsed;
                    if (_circleAngle >= 360f) _circleAngle -= 360f;

                    // Calculate orbit position around the player
                    float angleRad = _circleAngle * Mathf.Deg2Rad;
                    Vector3 orbitOffset = new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad)) * _circlingRadius;
                    Vector3 orbitTarget = _playerTransform.position + orbitOffset;

                    // Snap orbit target to NavMesh for safety
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(orbitTarget, out hit, _circlingRadius, NavMesh.AllAreas))
                    {
                        orbitTarget = hit.position;
                    }

                    // Move to the orbit point
                    if (_pathfindingMovement != null)
                    {
                        _pathfindingMovement.SetDestination(orbitTarget);
                        if (!_pathfindingMovement.Enabled)
                            _pathfindingMovement.Enabled = true;
                    }
                    else if (_navMeshMovement != null)
                    {
                        _navMeshMovement.SetDestination(orbitTarget);
                        if (!_navMeshMovement.Enabled)
                            _navMeshMovement.Enabled = true;
                    }

                    // Check if wait time has elapsed — time to lunge! (absolute time, LOD-safe)
                    if (Time.time >= _nextLungeTime)
                    {
                        // Start lunging at the player
                        _isLunging = true;
                        _comboHitsThisLunge = 0;
                        _lungeEndTime = Time.time + _lungeDuration;

                        if (_debugMode)
                        {
                            Debug.Log($"[EnemyAI] '{_enemyName}' LUNGING at player!");
                        }
                    }
                }

                return; // Circling mode handled everything, skip original code below
            }

            // ===== ORIGINAL MODE (fallback when circling disabled) =====
            // Stand still and swing on cooldown — the old behavior

            // Stop moving when attacking (only once)
            if (_pathfindingMovement != null && _pathfindingMovement.Enabled ||
                _navMeshMovement != null && _navMeshMovement.Enabled)
            {
                StopMovement();
            }

            // Face the player (infrequently for performance)
            if ((Time.frameCount + _updateFrameOffset) % 5 == 0)
            {
                FacePlayer();
            }

            // Attack if cooldown is ready
            if (Time.time >= _lastAttackTime + _attackCooldown)
            {
                PerformAttack();
                _lastAttackTime = Time.time;
            }
        }

        /// <summary>
        /// Smoothly rotates the enemy to face the current player target.
        /// Keeps rotation on the horizontal plane (no tilting up/down).
        /// Used by both circling and original attack modes.
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
        /// Update path to chase player.
        /// FLANKING: If enabled, navigates to an offset point around the player so enemies
        /// approach from different angles. Falls back to direct path when very close or off-mesh.
        /// </summary>
        private void UpdatePath()
        {
            if (_playerTransform == null)
                return;

            Vector3 targetPosition = _playerTransform.position;

            // FLANKING: Calculate an offset approach point instead of going directly at the player.
            // Each enemy has a unique _flankAngle, so they spread out around the player.
            if (_enableFlanking)
            {
                float distToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

                // Only use flanking offset when far enough away. When close, go direct.
                if (distToPlayer > _flankingCutoffDistance)
                {
                    // Convert flank angle to a direction on the XZ plane
                    float angleRad = _flankAngle * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad)) * _flankingRadius;
                    Vector3 flankedTarget = _playerTransform.position + offset;

                    // Make sure the flanked position is actually on the NavMesh
                    // If not, fall back to going directly at the player
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(flankedTarget, out hit, _flankingRadius, NavMesh.AllAreas))
                    {
                        targetPosition = hit.position;
                    }
                    // else: targetPosition stays as the direct player position (safe fallback)
                }
            }

            if (_debugMode)
            {
                Debug.Log($"[EnemyAI] '{_enemyName}' UpdatePath - target: {targetPosition}");
            }

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
            else
            {
                Debug.LogError($"[EnemyAI] '{_enemyName}' has NO pathfinding component! Cannot chase player!");
            }

            // Sprint speed is now handled by ApplySprintSpeed() in UpdateChasingState
            // which directly sets the NavMeshAgent speed for reliable results
        }

        /// <summary>
        /// Stop all movement and restore normal speed
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

            // Restore normal speed (stops SpeedChange ability and resets NavMeshAgent)
            RestoreNormalSpeed();
        }

        /// <summary>
        /// Apply sprint speed through Opsive's SpeedChange ability.
        ///
        /// WHY NOT NavMeshAgent.speed?
        /// Opsive's NavMeshAgentMovement sets updatePosition = false, which means
        /// the NavMeshAgent does NOT move the character — Opsive's motor does.
        /// NavMeshAgent.speed only affects path planning, not actual movement.
        /// The ONLY way to change actual movement speed is through Opsive's
        /// SpeedChange ability, which multiplies the motor's InputVector.
        ///
        /// We programmatically set SpeedChangeMultiplier AND MaxSpeedChangeValue
        /// (the clamp) so the multiplier actually takes effect.
        /// </summary>
        private void ApplySprintSpeed()
        {
            if (!_sprintWhenChasing)
                return;

            // PRIMARY SPEED CONTROL: Set Opsive's SpeedChange multiplier and clamp
            // The SpeedChange ability multiplies the character's InputVector, which
            // is what actually controls how fast the motor moves the character.
            if (_speedChangeAbility != null)
            {
                // Set the multiplier to our chase speed value
                _speedChangeAbility.SpeedChangeMultiplier = _chaseSpeedMultiplier;

                // CRITICAL: Also raise the clamp! By default MaxSpeedChangeValue = 2,
                // which caps the InputVector at 2.0 regardless of multiplier.
                // We set it higher so our multiplier can actually take full effect.
                _speedChangeAbility.MaxSpeedChangeValue = _chaseSpeedMultiplier + 1f;

                // Start the ability if not already running
                if (!_speedChangeAbility.IsActive)
                {
                    _characterLocomotion.TryStartAbility(_speedChangeAbility);
                }
            }

            // Also set NavMeshAgent.speed for path planning fidelity (turn radius, corners).
            // This doesn't affect actual movement but helps the NavMesh plan better paths.
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
        /// Stops the SpeedChange ability and resets NavMeshAgent speed.
        /// Called when exiting chase (idle, attacking, dead, patrolling).
        /// </summary>
        private void RestoreNormalSpeed()
        {
            // Stop Opsive's SpeedChange ability (restores normal motor speed)
            if (_speedChangeAbility != null && _speedChangeAbility.IsActive)
            {
                _characterLocomotion.TryStopAbility(_speedChangeAbility);
            }

            // Also restore NavMeshAgent speed for path planning
            if (_navMeshAgent != null && _isSprintSpeedApplied)
            {
                _navMeshAgent.speed = _originalNavMeshSpeed;
                _isSprintSpeedApplied = false;
            }
        }

        /// <summary>
        /// Perform attack on player.
        /// If the Opsive Use ability is set up (with an unarmed melee item), triggers a proper
        /// melee attack with animation and Opsive damage. Falls back to direct health subtraction
        /// if the Use ability isn't configured.
        /// </summary>
        private void PerformAttack()
        {
            if (_playerTransform == null)
                return;

            if (_debugMode)
            {
                Debug.Log($"[EnemyAI] '{_enemyName}' attacking player '{_playerTransform.name}'!");
            }

            // OPTION 1: Use Opsive's melee attack system (proper animation + hitbox damage)
            // Requires an unarmed item with MeleeAction to be set up on the enemy in the Inspector
            if (_useAbility != null)
            {
                _characterLocomotion.TryStartAbility(_useAbility);
                // Opsive handles the animation, hitbox detection, and damage application
                return;
            }

            // OPTION 2: Fallback - directly subtract health if Use ability isn't set up
            if (_debugMode)
            {
                Debug.Log($"[EnemyAI] No Use ability found, using direct damage fallback ({_attackDamage} damage)");
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
                        Debug.Log($"[EnemyAI] Player health is now: {healthAttribute.Value}/{healthAttribute.MaxValue}");
                    }
                }
            }
        }

        /// <summary>
        /// Change AI state
        /// </summary>
        private void ChangeState(EnemyAIState newState)
        {
            if (_currentState == newState)
                return;

            Debug.Log($"[EnemyAI] '{_enemyName}' STATE CHANGE: {_currentState} → {newState}");

            _currentState = newState;

            // Handle state transitions
            switch (newState)
            {
                case EnemyAIState.Idle:
                    Debug.Log($"[EnemyAI] Stopping movement (entering Idle)");
                    StopMovement();
                    break;

                case EnemyAIState.Chasing:
                    Debug.Log($"[EnemyAI] Starting chase behavior");
                    _hasWanderTarget = false; // Clear wander target when chasing
                    _isWaitingAtWaypoint = false; // Stop any patrol wait when chasing

                    // FLANKING: Pick a random approach angle so each enemy comes from a different direction
                    if (_enableFlanking)
                    {
                        _flankAngle = Random.Range(0f, 360f);
                    }

                    UpdatePath(); // This will handle sprint enabling if needed
                    break;

                case EnemyAIState.Attacking:
                    Debug.Log($"[EnemyAI] Entering attack mode");

                    // End any active leap charge on arrival
                    if (_isCharging)
                    {
                        EndCharge();
                    }

                    // CIRCLING: Initialize circling sub-state instead of just stopping
                    if (_enableCircling)
                    {
                        _isLunging = false;
                        _comboHitsThisLunge = 0;
                        _nextLungeTime = Time.time + Random.Range(_minAttackWaitTime, _maxAttackWaitTime);
                        _circleAngle = Random.Range(0f, 360f); // Start orbiting from a random angle
                        _lastCircleUpdateTime = Time.time;

                        // Restore normal speed for circling (we were sprinting during chase)
                        RestoreNormalSpeed();
                    }
                    else
                    {
                        // Original behavior: just stop moving
                        StopMovement();
                    }
                    break;

                case EnemyAIState.Dead:
                    Debug.Log($"[EnemyAI] Enemy died");
                    StopMovement();
                    break;
            }
        }

        /// <summary>
        /// Called by Opsive when character takes damage.
        /// Spawns blood effect, aggros onto the attacker, AND alerts nearby allies.
        /// </summary>
        private void OnHealthDamage(float amount, Vector3 position, Vector3 force, GameObject attacker, Collider hitCollider)
        {
            // AGGRO: If we're idle or patrolling and something hits us, chase the attacker!
            // This makes enemies react to being shot from any distance.
            if (attacker != null && _currentState == EnemyAIState.Idle)
            {
                _playerTransform = attacker.transform;
                Debug.Log($"[EnemyAI] '{_enemyName}' HIT by '{attacker.name}'! Aggroing and chasing attacker!");
                ChangeState(EnemyAIState.Chasing);

                // ALERT NEARBY ALLIES: "I'm under attack, get him!"
                AlertNearbyEnemies(attacker.transform);
            }

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
        /// Find all idle enemies within _alertRadius and tell them to chase the attacker.
        /// Uses OverlapSphere (runs once per hit, not per frame, so very cheap).
        /// </summary>
        /// <param name="attacker">The player/thing that hit us</param>
        private void AlertNearbyEnemies(Transform attacker)
        {
            // Find all colliders in the alert radius
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, _alertRadius);

            int alertedCount = 0;

            foreach (Collider col in nearbyColliders)
            {
                // Skip ourselves
                if (col.gameObject == gameObject)
                    continue;

                // Check if this collider belongs to an enemy with AI
                EnemyAIController nearbyEnemy = col.GetComponentInParent<EnemyAIController>();

                // Skip if no AI, or it's us, or already chasing/attacking/dead
                if (nearbyEnemy == null || nearbyEnemy == this || nearbyEnemy._currentState != EnemyAIState.Idle)
                    continue;

                // Alert this enemy!
                nearbyEnemy.AlertedByAlly(attacker);
                alertedCount++;
            }

            if (alertedCount > 0)
            {
                Debug.Log($"[EnemyAI] '{_enemyName}' alerted {alertedCount} nearby enemies!");
            }
        }

        /// <summary>
        /// Called by a nearby ally that got hit. Makes this enemy aggro on the attacker.
        /// Does NOT re-alert other enemies (prevents chain reaction / infinite loop).
        /// </summary>
        /// <param name="attacker">The player that attacked our ally</param>
        public void AlertedByAlly(Transform attacker)
        {
            // Only react if we're idle (don't interrupt an enemy already fighting)
            if (_currentState != EnemyAIState.Idle || attacker == null)
                return;

            _playerTransform = attacker;
            Debug.Log($"[EnemyAI] '{_enemyName}' ALERTED by ally! Chasing '{attacker.name}'!");
            ChangeState(EnemyAIState.Chasing);
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

            // Reset all combat behavior state so pooled enemies start clean
            _isLunging = false;
            _comboHitsThisLunge = 0;
            _nextLungeTime = 0f;
            _lungeEndTime = 0f;
            _circleAngle = 0f;
            _lastCircleUpdateTime = 0f;
            _flankAngle = 0f;
            _isCharging = false;
            _chargeEndTime = 0f;
            _lastChargeTime = -999f;

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
        /// Return enemy to pool after death animation.
        /// In multiplayer, master client destroys the networked enemy.
        /// In single player, deactivates for object pool reuse.
        /// </summary>
        private System.Collections.IEnumerator ReturnToPoolAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            // MULTIPLAYER: Master client destroys networked enemies via Photon
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
                // Non-master clients don't destroy - Photon handles removal
                yield break;
            }

            // SINGLE PLAYER: Reset and return to pool for reuse
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

            // Reset all combat behavior state for clean re-use from pool
            _isLunging = false;
            _comboHitsThisLunge = 0;
            _nextLungeTime = 0f;
            _lungeEndTime = 0f;
            _circleAngle = 0f;
            _lastCircleUpdateTime = 0f;
            _flankAngle = 0f;
            _isCharging = false;
            _chargeEndTime = 0f;
            _lastChargeTime = -999f;

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

                // FLANKING: Show the offset approach point as a blue sphere
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

            // CIRCLING/LUNGING: Show orbit target during attack state
            if (Application.isPlaying && _playerTransform != null && _currentState == EnemyAIState.Attacking && _enableCircling)
            {
                float angleRad = _circleAngle * Mathf.Deg2Rad;
                Vector3 orbitOffset = new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad)) * _circlingRadius;
                Vector3 orbitTarget = _playerTransform.position + orbitOffset;

                // Yellow = circling, Red = lunging
                Gizmos.color = _isLunging ? Color.red : Color.yellow;
                Gizmos.DrawSphere(orbitTarget, 0.4f);
                Gizmos.DrawLine(transform.position, _isLunging ? _playerTransform.position : orbitTarget);
            }

            // LEAP CHARGE: Show charge range as orange wireframe
            if (_enableLeapCharge)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Orange, semi-transparent
                Gizmos.DrawWireSphere(transform.position, _chargeRange);
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
                Gizmos.DrawWireSphere(transform.position, _chargeMinRange);
            }

            // Draw line to current patrol waypoint if patrolling
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
