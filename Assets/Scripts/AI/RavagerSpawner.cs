/*
 * Ravager Spawner for Klyra's Reach
 *
 * PURPOSE:
 * Spawns mixed groups of Ravagers (melee) and Sentinels (ranged) at an outpost
 * with initial spawns + reinforcement waves.
 * Creates the feeling that the outpost has been taken over by aliens.
 *
 * FEATURES:
 * - Mixed enemy composition: configurable Ravager/Sentinel ratio per group
 * - Initial spawn + reinforcement waves + horde events
 * - Staggered spawning (enemies trickle in, not all at once)
 * - Out-of-sight spawning (no pop-in - enemies appear where camera can't see)
 * - Patrol route assignment (enemies walk guard duty when idle)
 * - Time pressure escalation (the longer you stay, the harder it gets)
 * - Object pooling for single player performance (both enemy types)
 * - Photon multiplayer support (master client spawns for all)
 *
 * HOW TO USE:
 * 1. Create empty GameObjects as spawn points around your outpost
 * 2. Create an empty GameObject called "RavagerSpawner" at the outpost
 * 3. Attach this script to it
 * 4. Assign your Ravager prefab (required) and Sentinel prefab (optional)
 * 5. Drag all spawn point GameObjects into the Spawn Points array
 * 6. Adjust spawn settings (initial count, max alive, horde chance, etc.)
 * 7. Set min/max Sentinels per group for mixed composition
 * 8. (Optional) Add PatrolRoute objects and assign them for guard behavior
 * 9. (Optional) Add SpawnVisibilityChecker for out-of-sight spawning
 * 10. (Optional) Enable Time Pressure for escalating difficulty
 */

using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

namespace KlyrasReach.AI
{
    /// <summary>
    /// Spawns mixed groups of Ravagers (melee) and Sentinels (ranged) at outpost.
    /// Supports configurable composition, staggered spawning, out-of-sight placement,
    /// patrol routes, dropship delivery, and time pressure escalation.
    /// </summary>
    public class RavagerSpawner : MonoBehaviour
    {
        [Header("Ravager Prefab")]
        [Tooltip("The Ravager enemy prefab to spawn (used in single player / object pool)")]
        [SerializeField] private GameObject _ravagerPrefab;

        [Tooltip("Path to Ravager prefab in Resources folder (used in multiplayer for Photon spawning)")]
        [SerializeField] private string _ravagerPrefabNetworkPath = "Enemies/Ravager";

        [Header("Sentinel Prefab (Mixed Spawning)")]
        [Tooltip("Optional Sentinel enemy prefab to mix into spawn groups (used in single player / object pool). Leave empty to spawn only Ravagers.")]
        [SerializeField] private GameObject _sentinelPrefab;

        [Tooltip("Path to Sentinel prefab in Resources folder (used in multiplayer for Photon spawning)")]
        [SerializeField] private string _sentinelPrefabNetworkPath = "Enemies/Hollow Sentinel Sniper";

        [Tooltip("Minimum Sentinels to include in each spawn group (initial, reinforcement, horde)")]
        [SerializeField] private int _minSentinelsPerGroup = 1;

        [Tooltip("Maximum Sentinels to include in each spawn group")]
        [SerializeField] private int _maxSentinelsPerGroup = 2;

        [Header("Spawn Points")]
        [Tooltip("Empty GameObjects marking where Ravagers can spawn")]
        [SerializeField] private Transform[] _spawnPoints;

        [Header("Initial Spawn Settings")]
        [Tooltip("How many Ravagers to spawn when outpost loads")]
        [SerializeField] private int _initialSpawnCount = 8;

        [Tooltip("Should initial Ravagers spawn immediately on scene load?")]
        [SerializeField] private bool _spawnOnStart = true;

        [Header("Reinforcement Settings")]
        [Tooltip("Maximum Ravagers that can be alive at once")]
        [SerializeField] private int _maxAliveAtOnce = 15;

        [Tooltip("Minimum time between spawning reinforcements (seconds)")]
        [SerializeField] private float _minSpawnInterval = 15f;

        [Tooltip("Maximum time between spawning reinforcements (seconds)")]
        [SerializeField] private float _maxSpawnInterval = 30f;

        [Tooltip("How many Ravagers to spawn per reinforcement")]
        [SerializeField] private int _reinforcementCount = 3;

        [Header("Horde Settings")]
        [Tooltip("Chance (0-1) of spawning a horde instead of normal reinforcement")]
        [SerializeField] [Range(0f, 1f)] private float _hordeChance = 0.2f;

        [Tooltip("How many Ravagers in a horde")]
        [SerializeField] private int _hordeSize = 8;

        // =====================================================
        //  STAGGERED SPAWNING - Enemies trickle in instead of popping in all at once
        // =====================================================

        [Header("Staggered Spawning")]
        [Tooltip("Spawn enemies one at a time with delays instead of all at once?")]
        [SerializeField] private bool _useStaggeredSpawning = true;

        [Tooltip("Delay between each individual enemy spawn (seconds)")]
        [SerializeField] private float _staggerDelay = 0.3f;

        [Tooltip("Only spawn enemies at points the player can't currently see?")]
        [SerializeField] private bool _useOutOfSightSpawning = true;

        [Tooltip("If no hidden spawn point is found, how long to wait before giving up and spawning anyway (seconds)")]
        [SerializeField] private float _outOfSightMaxWaitTime = 5f;

        // =====================================================
        //  PATROL ROUTES - Enemies walk guard routes when idle
        // =====================================================

        [Header("Patrol Routes")]
        [Tooltip("Should patrol squads be spawned along routes?")]
        [SerializeField] private bool _spawnPatrolGroups = true;

        [Tooltip("How many enemies walk each patrol route together as a squad")]
        [SerializeField] private int _enemiesPerPatrolRoute = 2;

        [Tooltip("Available patrol routes near this outpost. Each route gets its own squad of enemies.")]
        [SerializeField] private PatrolRoute[] _patrolRoutes;

        // =====================================================
        //  TIME PRESSURE ESCALATION - Difficulty ramps up over time
        // =====================================================

        [Header("Time Pressure")]
        [Tooltip("Should difficulty increase the longer the player stays at the outpost?")]
        [SerializeField] private bool _enableTimePressure = true;

        [Tooltip("How the difficulty ramps up over time. X axis = 0 (start) to 1 (full escalation). Y axis = escalation amount (0 = base values, 1 = max values).")]
        [SerializeField] private AnimationCurve _escalationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("How long (seconds) until difficulty reaches maximum (600 = 10 minutes)")]
        [SerializeField] private float _escalationDuration = 600f;

        [Header("Time Pressure - Escalated Values")]
        [Tooltip("Spawn interval at maximum escalation (faster spawning = more pressure)")]
        [SerializeField] private float _escalatedMinSpawnInterval = 8f;

        [Tooltip("Spawn interval at maximum escalation")]
        [SerializeField] private float _escalatedMaxSpawnInterval = 15f;

        [Tooltip("Reinforcement count at maximum escalation")]
        [SerializeField] private int _escalatedReinforcementCount = 5;

        [Tooltip("Horde chance at maximum escalation")]
        [SerializeField] [Range(0f, 1f)] private float _escalatedHordeChance = 0.4f;

        [Tooltip("Horde size at maximum escalation")]
        [SerializeField] private int _escalatedHordeSize = 10;

        [Tooltip("Max alive at maximum escalation")]
        [SerializeField] private int _escalatedMaxAliveAtOnce = 25;

        [Header("Player Detection (Optional)")]
        [Tooltip("Only spawn initial Ravagers when player gets close?")]
        [SerializeField] private bool _waitForPlayerProximity = false;

        [Tooltip("Distance player needs to be to trigger initial spawn")]
        [SerializeField] private float _activationRange = 50f;

        [Header("Object Pooling")]
        [Tooltip("Pre-spawn enemies during loading for better performance?")]
        [SerializeField] private bool _useObjectPooling = true;

        [Tooltip("Size of object pool (should be >= maxAliveAtOnce)")]
        [SerializeField] private int _poolSize = 30;

        [Tooltip("How many enemies to initialize per frame? (Higher = faster loading but more lag)")]
        [SerializeField] private int _poolInitBatchSize = 5;

        [Tooltip("How many frames to wait between batches? (0 = every frame, 1 = every other frame)")]
        [SerializeField] private int _poolInitFrameDelay = 0;

        // =====================================================
        //  DROPSHIP REINFORCEMENTS - Enemies arrive via cinematic dropship
        // =====================================================

        [Header("Dropship Reinforcements")]
        [Tooltip("Use a dropship to deliver reinforcement waves instead of spawning on the ground?")]
        [SerializeField] private bool _useDropships = true;

        [Tooltip("Resources path to the Dropship prefab (must have PhotonView + DropshipController)")]
        [SerializeField] private string _dropshipPrefabPath = "Ships/Dropship";

        [Tooltip("Center of the drop zone where enemies will land. If empty, uses a random spawn point.")]
        [SerializeField] private Transform _dropZoneCenter;

        [Tooltip("Scatter radius for enemy landing positions around the drop zone center (meters)")]
        [SerializeField] private float _dropZoneRadius = 10f;

        [Header("Debug")]
        [Tooltip("Show debug messages?")]
        [SerializeField] private bool _debugMode = true;

        // =====================================================
        //  PRIVATE STATE
        // =====================================================

        // Tracking spawned enemies
        private List<GameObject> _spawnedRavagers = new List<GameObject>();
        private List<GameObject> _objectPool = new List<GameObject>();
        private List<GameObject> _sentinelObjectPool = new List<GameObject>();
        private float _nextSpawnTime = 0f;
        private bool _hasSpawnedInitial = false;
        private Transform _playerTransform;
        private Transform _poolContainer;
        private bool _isMultiplayer = false; // Tracks if we're in a Photon room

        // Visibility checker reference (found automatically on same or parent GameObject)
        private SpawnVisibilityChecker _visibilityChecker;

        // Escalation timer - tracks how long the outpost has been active
        private float _escalationStartTime = 0f;

        // Staggered spawn coroutine tracking (so we don't stack multiple coroutines)
        private bool _isStaggeredSpawning = false;

        // =====================================================
        //  INITIALIZATION
        // =====================================================

        /// <summary>
        /// Initialize spawner
        /// </summary>
        private void Start()
        {
            // Check if we're in multiplayer
            _isMultiplayer = PhotonNetwork.IsConnected && PhotonNetwork.InRoom;

            // MULTIPLAYER: Only the Master Client spawns enemies.
            // Photon automatically creates them on all clients via InstantiateRoomObject.
            if (_isMultiplayer && !PhotonNetwork.IsMasterClient)
            {
                Debug.Log("[RavagerSpawner] Non-master client - spawner disabled (master will spawn enemies via Photon)");
                enabled = false;
                return;
            }

            // Validate setup
            if (!_isMultiplayer && _ravagerPrefab == null)
            {
                Debug.LogError("[RavagerSpawner] No Ravager prefab assigned!");
                enabled = false;
                return;
            }

            if (_spawnPoints == null || _spawnPoints.Length == 0)
            {
                Debug.LogError("[RavagerSpawner] No spawn points assigned!");
                enabled = false;
                return;
            }

            // Log mixed spawning status
            if (!_isMultiplayer && _sentinelPrefab == null)
            {
                Debug.Log("[RavagerSpawner] No Sentinel prefab assigned - mixed spawning disabled (Ravager-only mode)");
            }
            else if (_isMultiplayer && string.IsNullOrEmpty(_sentinelPrefabNetworkPath))
            {
                Debug.Log("[RavagerSpawner] No Sentinel network path set - mixed spawning disabled (Ravager-only mode)");
            }

            // Find player
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                _playerTransform = playerObject.transform;
            }

            // Find visibility checker (look on this object, then children, then parent)
            _visibilityChecker = GetComponentInChildren<SpawnVisibilityChecker>();
            if (_visibilityChecker == null)
            {
                _visibilityChecker = GetComponentInParent<SpawnVisibilityChecker>();
            }

            if (_useOutOfSightSpawning && _visibilityChecker == null)
            {
                Debug.LogWarning("[RavagerSpawner] Out-of-sight spawning enabled but no SpawnVisibilityChecker found! " +
                    "Add SpawnVisibilityChecker component to this GameObject. Falling back to normal spawning.");
            }

            // Create object pool ONLY in single player (Photon spawning doesn't use pools)
            if (_useObjectPooling && !_isMultiplayer)
            {
                CreateObjectPool();
            }

            // Spawn initial Ravagers immediately if configured
            if (_spawnOnStart && !_waitForPlayerProximity)
            {
                SpawnInitialRavagers();
            }

            // Spawn patrol squads at each route's starting waypoint
            if (_spawnPatrolGroups)
            {
                SpawnPatrolGroups();
            }

            // Set first reinforcement spawn time (uses escalated values if enabled)
            _nextSpawnTime = Time.time + GetEscalatedSpawnInterval();

            // Start the escalation timer
            _escalationStartTime = Time.time;

            if (_isMultiplayer)
            {
                Debug.Log($"[RavagerSpawner] Master Client spawner initialized - will use PhotonNetwork.InstantiateRoomObject");
            }

            if (_enableTimePressure)
            {
                Debug.Log($"[RavagerSpawner] Time pressure enabled - difficulty will escalate over {_escalationDuration}s");
            }
        }

        // =====================================================
        //  OBJECT POOLING (unchanged from original)
        // =====================================================

        /// <summary>
        /// Create object pool - pre-spawn all enemies during loading.
        /// Pools both Ravagers and Sentinels (if Sentinel prefab is assigned).
        /// Sentinel pool is sized at roughly 1/3 of the main pool to match typical group ratios.
        /// </summary>
        private void CreateObjectPool()
        {
            // Create container for pooled objects
            GameObject poolContainerGO = new GameObject($"{gameObject.name}_Pool");
            poolContainerGO.transform.SetParent(transform);
            poolContainerGO.transform.position = new Vector3(0, -1000, 0); // Move pool container far below map
            _poolContainer = poolContainerGO.transform;

            // Pre-spawn Ravager pool
            for (int i = 0; i < _poolSize; i++)
            {
                GameObject ravager = Instantiate(_ravagerPrefab, _poolContainer);
                ravager.name = $"Ravager_Pooled_{i}";

                // Start inactive - will be initialized gradually over time
                ravager.SetActive(false);
                _objectPool.Add(ravager);
            }

            // Pre-spawn Sentinel pool (proportional to main pool, ~1/3 size)
            if (_sentinelPrefab != null)
            {
                int sentinelPoolSize = Mathf.Max(1, _poolSize / 3);
                for (int i = 0; i < sentinelPoolSize; i++)
                {
                    GameObject sentinel = Instantiate(_sentinelPrefab, _poolContainer);
                    sentinel.name = $"Sentinel_Pooled_{i}";

                    sentinel.SetActive(false);
                    _sentinelObjectPool.Add(sentinel);
                }
            }

            // Initialize pool gradually over multiple frames to avoid lag spike
            StartCoroutine(InitializePoolGradually());
        }

        /// <summary>
        /// Initialize pool gradually - activate enemies in small batches over time.
        /// This spreads the initialization cost across multiple frames.
        /// Handles both Ravager and Sentinel pools.
        /// Batch size and delay are configurable in Inspector.
        /// </summary>
        private IEnumerator InitializePoolGradually()
        {
            int batchSize = Mathf.Max(1, _poolInitBatchSize); // Ensure at least 1 per batch

            // Initialize Ravager pool
            for (int i = 0; i < _objectPool.Count; i += batchSize)
            {
                // Activate a batch of enemies
                for (int j = i; j < Mathf.Min(i + batchSize, _objectPool.Count); j++)
                {
                    GameObject ravager = _objectPool[j];
                    ravager.SetActive(true);
                }

                // Wait one frame for their Start() methods to run
                yield return null;

                // Deactivate the batch
                for (int j = i; j < Mathf.Min(i + batchSize, _objectPool.Count); j++)
                {
                    GameObject ravager = _objectPool[j];
                    ravager.SetActive(false);
                }

                // Wait additional frames if configured
                for (int k = 0; k < _poolInitFrameDelay; k++)
                {
                    yield return null;
                }
            }

            // Initialize Sentinel pool (same gradual init process)
            for (int i = 0; i < _sentinelObjectPool.Count; i += batchSize)
            {
                for (int j = i; j < Mathf.Min(i + batchSize, _sentinelObjectPool.Count); j++)
                {
                    _sentinelObjectPool[j].SetActive(true);
                }

                yield return null;

                for (int j = i; j < Mathf.Min(i + batchSize, _sentinelObjectPool.Count); j++)
                {
                    _sentinelObjectPool[j].SetActive(false);
                }

                for (int k = 0; k < _poolInitFrameDelay; k++)
                {
                    yield return null;
                }
            }

            if (_debugMode)
            {
                UnityEngine.Debug.Log($"[RavagerSpawner] Pool initialized - {_objectPool.Count} Ravagers + {_sentinelObjectPool.Count} Sentinels (batch size: {batchSize}, frame delay: {_poolInitFrameDelay})");
            }
        }

        // =====================================================
        //  MIXED COMPOSITION HELPERS
        //  Determine how many Sentinels to include in each group
        // =====================================================

        /// <summary>
        /// Determine how many Sentinels should be in a group of the given total size.
        /// Returns 0 if no Sentinel prefab is configured (Ravager-only mode).
        /// Result is clamped so Sentinels never exceed total group size.
        /// </summary>
        /// <param name="totalGroupSize">Total number of enemies in this group</param>
        /// <returns>Number of Sentinels for the group</returns>
        private int GetSentinelCountForGroup(int totalGroupSize)
        {
            // No Sentinel prefab = Ravager-only mode
            if (!_isMultiplayer && _sentinelPrefab == null) return 0;
            if (_isMultiplayer && string.IsNullOrEmpty(_sentinelPrefabNetworkPath)) return 0;

            // Pick random count between min and max, clamped to not exceed total group size
            int sentinelCount = Random.Range(_minSentinelsPerGroup, _maxSentinelsPerGroup + 1);
            return Mathf.Min(sentinelCount, totalGroupSize);
        }

        /// <summary>
        /// Build a list of isSentinel flags for a spawn group, with Sentinels randomly
        /// shuffled into the list so they aren't always at the end.
        /// </summary>
        /// <param name="totalCount">Total enemies in the group</param>
        /// <param name="sentinelCount">How many should be Sentinels</param>
        /// <returns>Shuffled list of bools (true = Sentinel, false = Ravager)</returns>
        private List<bool> BuildSentinelFlags(int totalCount, int sentinelCount)
        {
            List<bool> flags = new List<bool>();

            // Fill with Ravagers first, then Sentinels
            int ravagerCount = totalCount - sentinelCount;
            for (int i = 0; i < ravagerCount; i++) flags.Add(false);
            for (int i = 0; i < sentinelCount; i++) flags.Add(true);

            // Fisher-Yates shuffle so Sentinels are mixed in randomly
            for (int i = flags.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                bool temp = flags[i];
                flags[i] = flags[j];
                flags[j] = temp;
            }

            return flags;
        }

        // =====================================================
        //  MAIN UPDATE LOOP
        // =====================================================

        /// <summary>
        /// Check for spawning reinforcements. Uses escalated values when time pressure is enabled.
        /// </summary>
        private void Update()
        {
            // Check if we should spawn initial Ravagers (player proximity mode)
            if (!_hasSpawnedInitial && _waitForPlayerProximity && _playerTransform != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
                if (distanceToPlayer <= _activationRange)
                {
                    SpawnInitialRavagers();
                }
            }

            // Clean up dead/inactive Ravagers from tracking list
            if (_useObjectPooling)
            {
                // With pooling, remove inactive ones (they're back in the pool)
                _spawnedRavagers.RemoveAll(ravager => ravager == null || !ravager.activeInHierarchy);
            }
            else
            {
                // Without pooling, remove destroyed ones
                _spawnedRavagers.RemoveAll(ravager => ravager == null);
            }

            // Get escalated max alive count (increases over time if time pressure is on)
            int currentMaxAlive = GetEscalatedInt(_maxAliveAtOnce, _escalatedMaxAliveAtOnce);

            // Check if it's time to spawn reinforcements
            if (Time.time >= _nextSpawnTime && _spawnedRavagers.Count < currentMaxAlive)
            {
                SpawnReinforcements();

                // Schedule next spawn using escalated interval (gets faster over time)
                _nextSpawnTime = Time.time + GetEscalatedSpawnInterval();
            }
        }

        // =====================================================
        //  SPAWNING - Initial + Reinforcements
        // =====================================================

        /// <summary>
        /// Spawn initial enemies at the outpost with mixed Ravager/Sentinel composition.
        /// Uses staggered spawning if enabled (enemies trickle in over a few seconds).
        /// </summary>
        private void SpawnInitialRavagers()
        {
            if (_hasSpawnedInitial)
                return;

            _hasSpawnedInitial = true;

            int spawnCount = Mathf.Min(_initialSpawnCount, _spawnPoints.Length);

            // Determine mixed composition (how many Sentinels in this group)
            int sentinelCount = GetSentinelCountForGroup(spawnCount);
            List<bool> sentinelFlags = BuildSentinelFlags(spawnCount, sentinelCount);

            if (_debugMode && sentinelCount > 0)
            {
                Debug.Log($"[RavagerSpawner] Initial spawn: {spawnCount - sentinelCount} Ravagers + {sentinelCount} Sentinels");
            }

            if (_useStaggeredSpawning)
            {
                // Build list of spawn points for the staggered coroutine
                List<Transform> spawnPointList = new List<Transform>();
                for (int i = 0; i < spawnCount; i++)
                {
                    spawnPointList.Add(_spawnPoints[i]);
                }
                StartCoroutine(StaggeredSpawnCoroutine(spawnPointList, sentinelFlags));
            }
            else
            {
                // Spawn all at once with mixed composition
                for (int i = 0; i < spawnCount; i++)
                {
                    SpawnRavagerAtPoint(_spawnPoints[i], sentinelFlags[i]);
                }
            }
        }

        /// <summary>
        /// Spawn reinforcement wave or horde with mixed Ravager/Sentinel composition.
        /// Uses escalated values when time pressure is enabled (bigger waves, more hordes).
        /// If dropships are enabled in multiplayer, reinforcements arrive via cinematic dropship.
        /// </summary>
        private void SpawnReinforcements()
        {
            // Get escalated values (these increase over time if time pressure is on)
            float currentHordeChance = GetEscalatedFloat(_hordeChance, _escalatedHordeChance);
            int currentHordeSize = GetEscalatedInt(_hordeSize, _escalatedHordeSize);
            int currentReinforcementCount = GetEscalatedInt(_reinforcementCount, _escalatedReinforcementCount);
            int currentMaxAlive = GetEscalatedInt(_maxAliveAtOnce, _escalatedMaxAliveAtOnce);

            // Random chance to spawn a horde instead of normal reinforcement
            bool isHorde = Random.value <= currentHordeChance;
            int spawnCount = isHorde ? currentHordeSize : currentReinforcementCount;

            // Don't exceed max alive limit
            int availableSlots = currentMaxAlive - _spawnedRavagers.Count;
            spawnCount = Mathf.Min(spawnCount, availableSlots);

            if (spawnCount <= 0)
                return;

            // Determine mixed composition (how many Sentinels in this wave)
            int sentinelCount = GetSentinelCountForGroup(spawnCount);
            List<bool> sentinelFlags = BuildSentinelFlags(spawnCount, sentinelCount);

            if (_debugMode)
            {
                string waveType = isHorde ? "HORDE" : "Reinforcement";
                float escalation = GetEscalationMultiplier();
                int ravagerCount = spawnCount - sentinelCount;
                Debug.Log($"[RavagerSpawner] {waveType}: {ravagerCount} Ravagers + {sentinelCount} Sentinels (escalation: {escalation:P0})");
            }

            // DROPSHIP DELIVERY: In multiplayer with dropships enabled, reinforcements
            // arrive via cinematic dropship instead of popping into existence on the ground.
            // Falls back to normal ground spawning in single player or if dropships are off.
            if (_useDropships && _isMultiplayer)
            {
                SpawnDropshipReinforcements(spawnCount, sentinelFlags);
                return;
            }

            if (_useStaggeredSpawning)
            {
                // Build list of spawn points for the staggered coroutine
                List<Transform> spawnPointList = new List<Transform>();
                for (int i = 0; i < spawnCount; i++)
                {
                    // Pick a random spawn point for each enemy
                    spawnPointList.Add(_spawnPoints[Random.Range(0, _spawnPoints.Length)]);
                }
                StartCoroutine(StaggeredSpawnCoroutine(spawnPointList, sentinelFlags));
            }
            else
            {
                // Spawn all at once with mixed composition
                for (int i = 0; i < spawnCount; i++)
                {
                    Transform spawnPoint = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
                    SpawnRavagerAtPoint(spawnPoint, sentinelFlags[i]);
                }
            }
        }

        // =====================================================
        //  STAGGERED SPAWNING - Enemies appear one at a time
        // =====================================================

        /// <summary>
        /// Spawns enemies one at a time with delays between each.
        /// If out-of-sight spawning is enabled, tries to find hidden spawn points first.
        /// Uses coroutine yields (not per-frame checks) for efficiency.
        /// </summary>
        /// <param name="spawnPointList">List of spawn points to use (one per enemy)</param>
        /// <param name="isSentinelList">Parallel list indicating which entries are Sentinels (true) vs Ravagers (false). If null, all are Ravagers.</param>
        private IEnumerator StaggeredSpawnCoroutine(List<Transform> spawnPointList, List<bool> isSentinelList = null)
        {
            for (int i = 0; i < spawnPointList.Count; i++)
            {
                Transform chosenPoint = spawnPointList[i];

                // Determine if this enemy is a Sentinel
                bool isSentinel = (isSentinelList != null && i < isSentinelList.Count) ? isSentinelList[i] : false;

                // TRY OUT-OF-SIGHT SPAWNING: Find a spawn point the camera can't see
                if (_useOutOfSightSpawning && _visibilityChecker != null && _visibilityChecker.IsReady())
                {
                    Transform hiddenPoint = FindHiddenSpawnPoint();
                    if (hiddenPoint != null)
                    {
                        // Found a hidden point - use it instead of the random one
                        chosenPoint = hiddenPoint;
                    }
                    else
                    {
                        // All spawn points are visible. Wait up to _outOfSightMaxWaitTime
                        // for the player to look away, then spawn anyway.
                        float waitStartTime = Time.time;
                        bool foundHidden = false;

                        while (Time.time - waitStartTime < _outOfSightMaxWaitTime)
                        {
                            yield return new WaitForSeconds(0.5f); // Check every 0.5s

                            hiddenPoint = FindHiddenSpawnPoint();
                            if (hiddenPoint != null)
                            {
                                chosenPoint = hiddenPoint;
                                foundHidden = true;
                                break;
                            }
                        }

                        if (!foundHidden && _debugMode)
                        {
                            Debug.Log($"[RavagerSpawner] Gave up waiting for hidden spawn point after {_outOfSightMaxWaitTime}s - spawning at random point");
                        }
                    }
                }

                // Spawn the enemy at the chosen point (Ravager or Sentinel)
                SpawnRavagerAtPoint(chosenPoint, isSentinel);

                // Wait between spawns (stagger delay)
                if (i < spawnPointList.Count - 1)
                {
                    yield return new WaitForSeconds(_staggerDelay);
                }
            }
        }

        /// <summary>
        /// Find a spawn point that is NOT visible to the player's camera.
        /// Shuffles the spawn points array to avoid always picking the same hidden spot.
        /// Returns null if ALL spawn points are currently visible.
        /// </summary>
        /// <returns>A hidden spawn point Transform, or null if none found</returns>
        private Transform FindHiddenSpawnPoint()
        {
            // Shuffle spawn points so we don't always pick the same hidden one
            // (Fisher-Yates shuffle on a temporary index array to avoid modifying the original)
            int[] indices = new int[_spawnPoints.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = i;
            }

            // Shuffle the indices
            for (int i = indices.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int temp = indices[i];
                indices[i] = indices[j];
                indices[j] = temp;
            }

            // Check each spawn point (in shuffled order) until we find a hidden one
            for (int i = 0; i < indices.Length; i++)
            {
                Transform point = _spawnPoints[indices[i]];
                if (point == null) continue;

                // Check if this point is visible to the camera
                if (!_visibilityChecker.IsPositionVisible(point.position))
                {
                    // This point is NOT visible (hidden) - perfect for spawning!
                    return point;
                }
            }

            // All spawn points are visible - return null
            return null;
        }

        // =====================================================
        //  SPAWN A SINGLE RAVAGER (core spawn logic)
        // =====================================================

        /// <summary>
        /// Spawn a single enemy at a specific spawn point Transform.
        /// Wrapper for SpawnRavagerAtPosition using the spawn point's position + random offset.
        /// </summary>
        /// <param name="spawnPoint">Transform marking the spawn location</param>
        /// <param name="isSentinel">If true, spawns a Sentinel instead of a Ravager</param>
        private void SpawnRavagerAtPoint(Transform spawnPoint, bool isSentinel = false)
        {
            if (spawnPoint == null)
                return;

            // Add random offset in a radius so they don't spawn on top of each other
            Vector3 randomOffset = new Vector3(
                Random.Range(-3f, 3f),
                0f,
                Random.Range(-3f, 3f)
            );

            SpawnRavagerAtPosition(spawnPoint.position + randomOffset, spawnPoint.position, null, isSentinel);
        }

        /// <summary>
        /// Core spawn method. Handles NavMesh snapping, multiplayer vs single player,
        /// object pooling, and optional patrol route assignment.
        /// Spawns either a Ravager or Sentinel based on the isSentinel flag.
        /// </summary>
        /// <param name="rawPosition">Desired spawn position (will be snapped to NavMesh)</param>
        /// <param name="fallbackPosition">Backup position if rawPosition is off NavMesh</param>
        /// <param name="patrolRoute">Patrol route to assign (null = no patrol)</param>
        /// <param name="isSentinel">If true, spawns a Sentinel instead of a Ravager</param>
        private void SpawnRavagerAtPosition(Vector3 rawPosition, Vector3 fallbackPosition, PatrolRoute patrolRoute, bool isSentinel = false)
        {
            // Pick the correct prefab/path based on enemy type
            string networkPath = isSentinel ? _sentinelPrefabNetworkPath : _ravagerPrefabNetworkPath;
            GameObject prefab = isSentinel ? _sentinelPrefab : _ravagerPrefab;
            string enemyTypeName = isSentinel ? "Sentinel" : "Ravager";

            // SNAP TO NAVMESH: Find the nearest valid NavMesh position so enemies
            // don't spawn off the edge and fall through the world.
            // Search radius of 10m should be plenty to find nearby NavMesh.
            Vector3 spawnPosition;
            NavMeshHit navHit;

            if (NavMesh.SamplePosition(rawPosition, out navHit, 10f, NavMesh.AllAreas))
            {
                // Found valid NavMesh position - use it
                spawnPosition = navHit.position;
            }
            else
            {
                // Raw position was off NavMesh. Try the fallback position.
                if (NavMesh.SamplePosition(fallbackPosition, out navHit, 10f, NavMesh.AllAreas))
                {
                    spawnPosition = navHit.position;

                    if (_debugMode)
                    {
                        Debug.LogWarning($"[RavagerSpawner] Offset position was off NavMesh, using fallback at {spawnPosition}");
                    }
                }
                else
                {
                    // Fallback position itself is not near any NavMesh - skip this spawn entirely
                    Debug.LogError($"[RavagerSpawner] Position {fallbackPosition} is NOT near any NavMesh! Skipping spawn.");
                    return;
                }
            }

            // MULTIPLAYER: Spawn via Photon so enemy exists on all clients
            if (_isMultiplayer)
            {
                GameObject enemy = PhotonNetwork.InstantiateRoomObject(
                    networkPath,
                    spawnPosition,
                    Quaternion.identity
                );

                if (enemy != null)
                {
                    _spawnedRavagers.Add(enemy);

                    // Assign patrol route if provided (supports both Ravager and Sentinel AI)
                    if (patrolRoute != null)
                    {
                        EnemyAIController ai = enemy.GetComponent<EnemyAIController>();
                        if (ai != null) { ai.AssignPatrolRoute(patrolRoute); }
                        else
                        {
                            SentinelAIController sentinelAI = enemy.GetComponent<SentinelAIController>();
                            if (sentinelAI != null) sentinelAI.AssignPatrolRoute(patrolRoute);
                        }
                    }
                }
                else
                {
                    Debug.LogError($"[RavagerSpawner] Failed to network spawn {enemyTypeName}! Check that '{networkPath}' exists in Resources.");
                }
                return;
            }

            // SINGLE PLAYER: Use object pool or local Instantiate
            GameObject localEnemy = null;

            if (_useObjectPooling)
            {
                localEnemy = GetFromPool(isSentinel);
                if (localEnemy == null)
                {
                    return;
                }
            }
            else
            {
                if (prefab == null)
                {
                    Debug.LogError($"[RavagerSpawner] No {enemyTypeName} prefab assigned! Cannot spawn.");
                    return;
                }
                localEnemy = Instantiate(prefab);
            }

            // Position and activate the enemy
            localEnemy.transform.position = spawnPosition;
            localEnemy.transform.rotation = Quaternion.identity;
            localEnemy.SetActive(true);

            _spawnedRavagers.Add(localEnemy);

            // Assign patrol route if provided (supports both Ravager and Sentinel AI)
            if (patrolRoute != null)
            {
                EnemyAIController ai = localEnemy.GetComponent<EnemyAIController>();
                if (ai != null) { ai.AssignPatrolRoute(patrolRoute); }
                else
                {
                    SentinelAIController sentinelAI = localEnemy.GetComponent<SentinelAIController>();
                    if (sentinelAI != null) sentinelAI.AssignPatrolRoute(patrolRoute);
                }
            }
        }

        // =====================================================
        //  PATROL GROUP SPAWNING
        //  Each route gets a dedicated squad of enemies that
        //  spawn at the first waypoint and travel together.
        // =====================================================

        /// <summary>
        /// Spawn a squad of enemies for each patrol route.
        /// They all start at the route's first waypoint so they travel as a group.
        /// Optionally includes one Sentinel per squad if the Sentinel prefab is configured
        /// and the squad has at least 2 members.
        /// A small random offset keeps them from stacking on the exact same spot.
        /// </summary>
        private void SpawnPatrolGroups()
        {
            if (_patrolRoutes == null || _patrolRoutes.Length == 0)
                return;

            foreach (PatrolRoute route in _patrolRoutes)
            {
                // Skip null or invalid routes (less than 2 waypoints)
                if (route == null || !route.IsValid())
                    continue;

                // Get the first waypoint as the squad's spawn position
                Vector3 routeStart = route.GetWaypointPosition(0);

                // Determine if this patrol squad should include a Sentinel.
                // Only include one if: Sentinel is configured AND squad has 2+ members.
                int squadSize = _enemiesPerPatrolRoute;
                int sentinelCount = 0;
                if (squadSize >= 2)
                {
                    sentinelCount = GetSentinelCountForGroup(squadSize);
                    // Cap patrol sentinels at 1 to keep squads mostly melee
                    sentinelCount = Mathf.Min(sentinelCount, 1);
                }

                List<bool> sentinelFlags = BuildSentinelFlags(squadSize, sentinelCount);

                if (_debugMode)
                {
                    int ravagerCount = squadSize - sentinelCount;
                    Debug.Log($"[RavagerSpawner] Spawning patrol squad at route '{route.name}': {ravagerCount} Ravagers + {sentinelCount} Sentinels");
                }

                // Spawn the squad - small offset so they don't overlap
                for (int i = 0; i < squadSize; i++)
                {
                    Vector3 offset = new Vector3(
                        Random.Range(-1.5f, 1.5f),
                        0f,
                        Random.Range(-1.5f, 1.5f)
                    );

                    SpawnRavagerAtPosition(routeStart + offset, routeStart, route, sentinelFlags[i]);
                }
            }
        }

        // =====================================================
        //  TIME PRESSURE ESCALATION
        // =====================================================

        /// <summary>
        /// Get the current escalation multiplier (0 to 1).
        /// 0 = just started (base difficulty), 1 = maximum escalation.
        /// Uses the AnimationCurve for custom pacing (e.g., slow start, fast ramp, etc.)
        /// Only evaluated when spawning (every 3-8s), so essentially free.
        /// </summary>
        /// <returns>Escalation value from 0 (no escalation) to 1 (maximum)</returns>
        private float GetEscalationMultiplier()
        {
            // If time pressure is disabled, always return 0 (no escalation)
            if (!_enableTimePressure)
                return 0f;

            // Calculate how far through the escalation period we are (0 to 1)
            float elapsed = Time.time - _escalationStartTime;
            float normalizedTime = Mathf.Clamp01(elapsed / _escalationDuration);

            // Apply the animation curve for custom pacing
            // (e.g., the curve can make it ramp slowly at first, then spike)
            return _escalationCurve.Evaluate(normalizedTime);
        }

        /// <summary>
        /// Lerp a float value between base and escalated based on current escalation.
        /// Example: GetEscalatedFloat(8f, 3f) at 50% escalation = 5.5f
        /// </summary>
        /// <param name="baseValue">Value at 0 escalation (start of encounter)</param>
        /// <param name="escalatedValue">Value at 1 escalation (maximum difficulty)</param>
        /// <returns>Interpolated value based on current escalation</returns>
        private float GetEscalatedFloat(float baseValue, float escalatedValue)
        {
            return Mathf.Lerp(baseValue, escalatedValue, GetEscalationMultiplier());
        }

        /// <summary>
        /// Lerp an int value between base and escalated based on current escalation.
        /// Rounds to nearest integer.
        /// </summary>
        /// <param name="baseValue">Value at 0 escalation</param>
        /// <param name="escalatedValue">Value at 1 escalation</param>
        /// <returns>Interpolated integer value</returns>
        private int GetEscalatedInt(int baseValue, int escalatedValue)
        {
            return Mathf.RoundToInt(Mathf.Lerp(baseValue, escalatedValue, GetEscalationMultiplier()));
        }

        /// <summary>
        /// Get the current spawn interval (time between reinforcement waves).
        /// Uses escalated values if time pressure is enabled.
        /// </summary>
        /// <returns>Random interval between current min and max spawn intervals</returns>
        private float GetEscalatedSpawnInterval()
        {
            float currentMin = GetEscalatedFloat(_minSpawnInterval, _escalatedMinSpawnInterval);
            float currentMax = GetEscalatedFloat(_maxSpawnInterval, _escalatedMaxSpawnInterval);
            return Random.Range(currentMin, currentMax);
        }

        // =====================================================
        //  DROPSHIP REINFORCEMENT SYSTEM
        //  Spawns a cinematic dropship that flies in, hovers,
        //  and drops enemies one at a time. Each enemy falls
        //  from the ship to the ground with physics-like motion.
        // =====================================================

        /// <summary>
        /// Spawn a dropship that will deliver the given number of enemies with mixed composition.
        /// The dropship flies in from a random direction, hovers over the drop zone,
        /// and calls SpawnEnemyFromDropship() for each enemy. Master client only.
        /// </summary>
        /// <param name="enemyCount">How many enemies the dropship should deliver</param>
        /// <param name="sentinelFlags">Which enemies are Sentinels (true) vs Ravagers (false)</param>
        private void SpawnDropshipReinforcements(int enemyCount, List<bool> sentinelFlags)
        {
            // Determine the drop zone center:
            // Use the assigned drop zone if set, otherwise pick a random spawn point
            Vector3 dropZonePosition;
            if (_dropZoneCenter != null)
            {
                dropZonePosition = _dropZoneCenter.position;
            }
            else
            {
                // Fallback: use a random spawn point as the drop zone center
                Transform randomPoint = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
                dropZonePosition = randomPoint.position;
            }

            if (_debugMode)
            {
                Debug.Log($"[RavagerSpawner] Calling in dropship with {enemyCount} enemies at drop zone {dropZonePosition}");
            }

            // Spawn the dropship via Photon so it appears on all clients
            GameObject dropship = PhotonNetwork.InstantiateRoomObject(
                _dropshipPrefabPath,
                dropZonePosition + Vector3.up * 200f, // Start high up (DropshipController will reposition)
                Quaternion.identity
            );

            if (dropship == null)
            {
                Debug.LogError($"[RavagerSpawner] Failed to spawn dropship! Check that '{_dropshipPrefabPath}' exists in Resources folder.");
                return;
            }

            // Build a list of prefab paths for the dropship to use, one per enemy.
            // This lets the dropship spawn the correct enemy type for each drop.
            List<string> prefabPaths = new List<string>();
            for (int i = 0; i < enemyCount; i++)
            {
                bool isSentinel = (sentinelFlags != null && i < sentinelFlags.Count) ? sentinelFlags[i] : false;
                prefabPaths.Add(isSentinel ? _sentinelPrefabNetworkPath : _ravagerPrefabNetworkPath);
            }

            // Tell the dropship to begin its flight sequence
            DropshipController controller = dropship.GetComponent<DropshipController>();
            if (controller != null)
            {
                controller.BeginDropMission(
                    dropZonePosition,
                    enemyCount,
                    this, // Pass ourselves so the dropship can call SpawnEnemyFromDropship
                    _ravagerPrefabNetworkPath,
                    _dropZoneRadius,
                    prefabPaths // Pass per-enemy prefab paths for mixed composition
                );
            }
            else
            {
                Debug.LogError("[RavagerSpawner] Dropship prefab is missing DropshipController component!");
                PhotonNetwork.Destroy(dropship);
            }
        }

        /// <summary>
        /// Spawns a single enemy at the given air position (below the dropship) and starts
        /// the falling sequence. Called by DropshipController for each enemy during hover phase.
        ///
        /// The enemy is spawned mid-air with NavMeshAgent and AI disabled, then a coroutine
        /// moves it down to the ground. Once landed, NavMeshAgent warps to ground position
        /// and AI is re-enabled so the enemy starts chasing players normally.
        ///
        /// WHY THIS WORKS WITH NetworkEnemySync:
        /// On non-master clients, NetworkEnemySync.Awake() already disables NavMeshAgent
        /// and interpolates position from the master. As the master moves the enemy downward
        /// in the fall coroutine, non-master clients see it fall smoothly via position sync.
        /// No changes to NetworkEnemySync needed.
        /// </summary>
        /// <param name="airPosition">World position where the enemy spawns (below the dropship)</param>
        /// <param name="dropZoneCenter">Center of the landing area on the ground</param>
        /// <param name="dropZoneRadius">Scatter radius for the landing position</param>
        /// <param name="prefabPath">Resources path for the enemy prefab to spawn (defaults to Ravager path)</param>
        public void SpawnEnemyFromDropship(Vector3 airPosition, Vector3 dropZoneCenter, float dropZoneRadius, string prefabPath = null)
        {
            // Default to Ravager path if no specific path provided (backward compatibility)
            if (string.IsNullOrEmpty(prefabPath))
            {
                prefabPath = _ravagerPrefabNetworkPath;
            }

            // Pick a random XZ position within the drop zone radius for landing
            Vector2 randomCircle = Random.insideUnitCircle * dropZoneRadius;
            Vector3 targetGround = dropZoneCenter + new Vector3(randomCircle.x, 0f, randomCircle.y);

            // Snap to NavMesh to find a valid landing position
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(targetGround, out navHit, 15f, NavMesh.AllAreas))
            {
                targetGround = navHit.position;
            }
            else
            {
                // If no NavMesh found near the random point, try the drop zone center directly
                if (NavMesh.SamplePosition(dropZoneCenter, out navHit, 15f, NavMesh.AllAreas))
                {
                    targetGround = navHit.position;
                    if (_debugMode)
                    {
                        Debug.LogWarning($"[RavagerSpawner] Random drop point off NavMesh, using drop zone center instead");
                    }
                }
                else
                {
                    Debug.LogError($"[RavagerSpawner] Drop zone center {dropZoneCenter} is not near NavMesh! Skipping enemy drop.");
                    return;
                }
            }

            // Spawn the enemy at the air position (mid-air, below the dropship)
            GameObject enemy = PhotonNetwork.InstantiateRoomObject(
                prefabPath,
                airPosition,
                Quaternion.identity
            );

            if (enemy == null)
            {
                Debug.LogError($"[RavagerSpawner] Failed to spawn enemy from dropship! Check that '{prefabPath}' exists in Resources.");
                return;
            }

            // Track this enemy in our spawned list
            _spawnedRavagers.Add(enemy);

            // IMMEDIATELY disable NavMeshAgent on master client to prevent snap-to-ground.
            // Without this, NavMeshAgent would instantly teleport the enemy to the nearest
            // NavMesh surface, ruining the falling animation.
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = false;
            }

            // IMMEDIATELY disable the AI controller to prevent chasing during the fall.
            // This is safe because Start() → InitializeAI() has already run and cached all
            // component references. Disabling stops Update() but keeps the cached state.
            // When we re-enable it after landing, Update() resumes and the AI picks up
            // exactly where initialization left off — Start() does NOT re-run.
            // Supports both Ravager (EnemyAIController) and Sentinel (SentinelAIController).
            EnemyAIController ai = enemy.GetComponent<EnemyAIController>();
            SentinelAIController sentinelAI = enemy.GetComponent<SentinelAIController>();
            if (ai != null) ai.enabled = false;
            else if (sentinelAI != null) sentinelAI.enabled = false;

            // Start the falling coroutine to move the enemy from air to ground
            StartCoroutine(DropEnemyToGround(enemy, targetGround));
        }

        /// <summary>
        /// Coroutine that moves an enemy from its air spawn position down to the ground.
        /// Simulates a gravity-like accelerating fall, with XZ drift toward the landing spot.
        /// Once on the ground, re-enables NavMeshAgent (warped to position) and AI controller.
        ///
        /// Fall physics:
        /// - Starts at 0 m/s, accelerates up to 12 m/s (gravity-like feel)
        /// - XZ position drifts from air position toward landing spot during fall
        /// - On landing: snaps to ground, warps NavMeshAgent, re-enables everything
        /// </summary>
        /// <param name="enemy">The enemy GameObject that was just spawned mid-air</param>
        /// <param name="groundTarget">The NavMesh-snapped ground position to land at</param>
        private System.Collections.IEnumerator DropEnemyToGround(GameObject enemy, Vector3 groundTarget)
        {
            // Safety: if the enemy was destroyed before we start, bail out
            if (enemy == null) yield break;

            // Fall parameters
            float fallSpeed = 0f;           // Current vertical speed (starts at 0, accelerates)
            float gravity = 18f;            // Acceleration — feels like heavy drop, not floaty
            float maxFallSpeed = 30f;       // Terminal velocity cap (fast, since it's a long fall from 80m)

            // Starting position (mid-air below the dropship)
            Vector3 startPos = enemy.transform.position;

            // Track fall progress for XZ drift calculation
            float totalVerticalDistance = startPos.y - groundTarget.y;
            if (totalVerticalDistance <= 0f)
            {
                // Already at or below ground — just snap and enable
                totalVerticalDistance = 1f;
            }

            while (enemy != null)
            {
                // Accelerate the fall (gravity)
                fallSpeed += gravity * Time.deltaTime;
                fallSpeed = Mathf.Min(fallSpeed, maxFallSpeed);

                // Move down
                Vector3 pos = enemy.transform.position;
                pos.y -= fallSpeed * Time.deltaTime;

                // Calculate how far through the fall we are (0 = just started, 1 = landed)
                float fallProgress = Mathf.Clamp01(1f - (pos.y - groundTarget.y) / totalVerticalDistance);

                // Drift XZ toward landing spot during fall (smooth interpolation)
                pos.x = Mathf.Lerp(startPos.x, groundTarget.x, fallProgress);
                pos.z = Mathf.Lerp(startPos.z, groundTarget.z, fallProgress);

                // Check if we've reached the ground
                if (pos.y <= groundTarget.y)
                {
                    // LANDED — snap to exact ground position
                    pos = groundTarget;
                    enemy.transform.position = pos;

                    if (_debugMode)
                    {
                        Debug.Log($"[RavagerSpawner] Enemy landed at {pos}");
                    }

                    // Re-enable NavMeshAgent and warp it to the landing position.
                    // Warp() is used instead of setting position because it properly
                    // places the agent on the NavMesh without a path calculation.
                    NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
                    if (agent != null)
                    {
                        agent.enabled = true;
                        agent.Warp(pos);
                    }

                    // Re-enable AI controller so the enemy starts chasing players
                    // Supports both Ravager (EnemyAIController) and Sentinel (SentinelAIController)
                    EnemyAIController ai = enemy.GetComponent<EnemyAIController>();
                    if (ai != null) { ai.enabled = true; }
                    else
                    {
                        SentinelAIController sentinelAI = enemy.GetComponent<SentinelAIController>();
                        if (sentinelAI != null) sentinelAI.enabled = true;
                    }

                    yield break; // Done with this enemy's fall
                }

                // Still falling — update position
                enemy.transform.position = pos;
                yield return null;
            }

            // If we get here, the enemy was destroyed during the fall (e.g., killed mid-air)
            if (_debugMode)
            {
                Debug.Log("[RavagerSpawner] Enemy destroyed during dropship fall sequence");
            }
        }

        // =====================================================
        //  OBJECT POOL HELPERS
        // =====================================================

        /// <summary>
        /// Get an inactive enemy from the appropriate pool (Ravager or Sentinel).
        /// </summary>
        /// <param name="isSentinel">If true, pulls from the Sentinel pool; otherwise from Ravager pool</param>
        private GameObject GetFromPool(bool isSentinel = false)
        {
            List<GameObject> pool = isSentinel ? _sentinelObjectPool : _objectPool;

            foreach (GameObject enemy in pool)
            {
                if (!enemy.activeInHierarchy)
                {
                    return enemy;
                }
            }
            return null; // Pool exhausted
        }

        // =====================================================
        //  PUBLIC API
        // =====================================================

        /// <summary>
        /// Get current alive count
        /// </summary>
        public int GetAliveCount()
        {
            _spawnedRavagers.RemoveAll(ravager => ravager == null);
            return _spawnedRavagers.Count;
        }

        /// <summary>
        /// Manually trigger a horde spawn with mixed Ravager/Sentinel composition.
        /// </summary>
        public void SpawnHorde()
        {
            int currentMaxAlive = GetEscalatedInt(_maxAliveAtOnce, _escalatedMaxAliveAtOnce);
            int currentHordeSize = GetEscalatedInt(_hordeSize, _escalatedHordeSize);
            int availableSlots = currentMaxAlive - _spawnedRavagers.Count;
            int spawnCount = Mathf.Min(currentHordeSize, availableSlots);

            if (spawnCount <= 0)
                return;

            // Determine mixed composition for the horde
            int sentinelCount = GetSentinelCountForGroup(spawnCount);
            List<bool> sentinelFlags = BuildSentinelFlags(spawnCount, sentinelCount);

            if (_debugMode && sentinelCount > 0)
            {
                Debug.Log($"[RavagerSpawner] Manual horde: {spawnCount - sentinelCount} Ravagers + {sentinelCount} Sentinels");
            }

            if (_useStaggeredSpawning)
            {
                List<Transform> spawnPointList = new List<Transform>();
                for (int i = 0; i < spawnCount; i++)
                {
                    spawnPointList.Add(_spawnPoints[Random.Range(0, _spawnPoints.Length)]);
                }
                StartCoroutine(StaggeredSpawnCoroutine(spawnPointList, sentinelFlags));
            }
            else
            {
                for (int i = 0; i < spawnCount; i++)
                {
                    Transform spawnPoint = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
                    SpawnRavagerAtPoint(spawnPoint, sentinelFlags[i]);
                }
            }
        }

        /// <summary>
        /// Get current escalation level as a percentage (0-100%).
        /// Useful for UI or debugging.
        /// </summary>
        public float GetEscalationPercent()
        {
            return GetEscalationMultiplier() * 100f;
        }

        // =====================================================
        //  GIZMO VISUALIZATION
        // =====================================================

        /// <summary>
        /// Draw debug visualization in Scene view
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw spawn points
            if (_spawnPoints != null)
            {
                Gizmos.color = Color.red;
                foreach (Transform spawnPoint in _spawnPoints)
                {
                    if (spawnPoint != null)
                    {
                        Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                        Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + Vector3.up * 2f);
                    }
                }
            }

            // Draw activation range if using player proximity
            if (_waitForPlayerProximity)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, _activationRange);
            }

            // Draw connection lines to all spawn points from spawner center
            if (_spawnPoints != null)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                foreach (Transform spawnPoint in _spawnPoints)
                {
                    if (spawnPoint != null)
                    {
                        Gizmos.DrawLine(transform.position, spawnPoint.position);
                    }
                }
            }

            // Draw lines from spawner to assigned patrol routes (cyan)
            if (_spawnPatrolGroups && _patrolRoutes != null)
            {
                Gizmos.color = Color.cyan;
                foreach (PatrolRoute route in _patrolRoutes)
                {
                    if (route != null)
                    {
                        Gizmos.DrawLine(transform.position, route.transform.position);
                    }
                }
            }

            // Draw dropship drop zone (yellow circle at hover height + ground circle)
            if (_useDropships && _dropZoneCenter != null)
            {
                // Ground drop zone circle (where enemies will land)
                Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
                Gizmos.DrawWireSphere(_dropZoneCenter.position, _dropZoneRadius);

                // Line from spawner to drop zone
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawLine(transform.position, _dropZoneCenter.position);

                // Hover position indicator (80m above drop zone)
                Vector3 hoverPos = _dropZoneCenter.position + Vector3.up * 80f;
                Gizmos.color = new Color(1f, 0.8f, 0f, 0.4f);
                Gizmos.DrawWireSphere(hoverPos, 3f);
                Gizmos.DrawLine(_dropZoneCenter.position, hoverPos);
            }
        }
    }
}
