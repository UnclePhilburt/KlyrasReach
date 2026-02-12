/*
 * Ravager Spawner for Klyra's Reach
 *
 * PURPOSE:
 * Spawns Ravagers at an outpost with initial spawns + reinforcement waves
 * Creates the feeling that the outpost has been taken over by aliens
 *
 * HOW TO USE:
 * 1. Create empty GameObjects as spawn points around your outpost
 * 2. Create an empty GameObject called "RavagerSpawner" at the outpost
 * 3. Attach this script to it
 * 4. Assign your Ravager prefab
 * 5. Drag all spawn point GameObjects into the Spawn Points array
 * 6. Adjust spawn settings (initial count, max alive, horde chance, etc.)
 */

using UnityEngine;
using System.Collections.Generic;

namespace KlyrasReach.AI
{
    /// <summary>
    /// Spawns Ravagers at outpost with initial population + reinforcement waves
    /// </summary>
    public class RavagerSpawner : MonoBehaviour
    {
        [Header("Ravager Prefab")]
        [Tooltip("The Ravager enemy prefab to spawn")]
        [SerializeField] private GameObject _ravagerPrefab;

        [Header("Spawn Points")]
        [Tooltip("Empty GameObjects marking where Ravagers can spawn")]
        [SerializeField] private Transform[] _spawnPoints;

        [Header("Initial Spawn Settings")]
        [Tooltip("How many Ravagers to spawn when outpost loads")]
        [SerializeField] private int _initialSpawnCount = 30;

        [Tooltip("Should initial Ravagers spawn immediately on scene load?")]
        [SerializeField] private bool _spawnOnStart = true;

        [Header("Reinforcement Settings")]
        [Tooltip("Maximum Ravagers that can be alive at once")]
        [SerializeField] private int _maxAliveAtOnce = 80;

        [Tooltip("Minimum time between spawning reinforcements (seconds)")]
        [SerializeField] private float _minSpawnInterval = 3f;

        [Tooltip("Maximum time between spawning reinforcements (seconds)")]
        [SerializeField] private float _maxSpawnInterval = 8f;

        [Tooltip("How many Ravagers to spawn per reinforcement")]
        [SerializeField] private int _reinforcementCount = 8;

        [Header("Horde Settings")]
        [Tooltip("Chance (0-1) of spawning a horde instead of normal reinforcement")]
        [SerializeField] [Range(0f, 1f)] private float _hordeChance = 0.4f;

        [Tooltip("How many Ravagers in a horde")]
        [SerializeField] private int _hordeSize = 20;

        [Header("Player Detection (Optional)")]
        [Tooltip("Only spawn initial Ravagers when player gets close?")]
        [SerializeField] private bool _waitForPlayerProximity = false;

        [Tooltip("Distance player needs to be to trigger initial spawn")]
        [SerializeField] private float _activationRange = 50f;

        [Header("Object Pooling")]
        [Tooltip("Pre-spawn enemies during loading for better performance?")]
        [SerializeField] private bool _useObjectPooling = true;

        [Tooltip("Size of object pool (should be >= maxAliveAtOnce)")]
        [SerializeField] private int _poolSize = 100;

        [Header("Debug")]
        [Tooltip("Show debug messages?")]
        [SerializeField] private bool _debugMode = true;

        // Private variables
        private List<GameObject> _spawnedRavagers = new List<GameObject>();
        private List<GameObject> _objectPool = new List<GameObject>();
        private float _nextSpawnTime = 0f;
        private bool _hasSpawnedInitial = false;
        private Transform _playerTransform;
        private Transform _poolContainer;

        /// <summary>
        /// Initialize spawner
        /// </summary>
        private void Start()
        {
            // Validate setup
            if (_ravagerPrefab == null)
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

            // Find player
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                _playerTransform = playerObject.transform;
            }

            // Create object pool if enabled
            if (_useObjectPooling)
            {
                CreateObjectPool();
            }

            // Spawn initial Ravagers immediately if configured
            if (_spawnOnStart && !_waitForPlayerProximity)
            {
                SpawnInitialRavagers();
            }

            // Set first reinforcement spawn time
            _nextSpawnTime = Time.time + Random.Range(_minSpawnInterval, _maxSpawnInterval);

            if (_debugMode)
            {
                Debug.Log($"[RavagerSpawner] '{gameObject.name}' initialized. Initial count: {_initialSpawnCount}, Max alive: {_maxAliveAtOnce}, Pooling: {_useObjectPooling}");
            }
        }

        /// <summary>
        /// Create object pool - pre-spawn all enemies during loading
        /// </summary>
        private void CreateObjectPool()
        {
            // Create container for pooled objects
            GameObject poolContainerGO = new GameObject($"{gameObject.name}_Pool");
            poolContainerGO.transform.SetParent(transform);
            poolContainerGO.transform.position = new Vector3(0, -1000, 0); // Move pool container far below map
            _poolContainer = poolContainerGO.transform;

            // Pre-spawn all enemies
            for (int i = 0; i < _poolSize; i++)
            {
                GameObject ravager = Instantiate(_ravagerPrefab, _poolContainer);
                ravager.name = $"Ravager_Pooled_{i}";

                // Enable briefly to force initialization (Start/Awake), then disable
                ravager.SetActive(true);
                _objectPool.Add(ravager);
            }

            // Wait one frame, then disable all pooled enemies
            StartCoroutine(DisablePoolAfterInitialization());

            if (_debugMode)
            {
                Debug.Log($"[RavagerSpawner] Created object pool with {_poolSize} Ravagers (pre-initialized during loading)");
            }
        }

        /// <summary>
        /// Disable all pooled enemies after they've initialized
        /// </summary>
        private System.Collections.IEnumerator DisablePoolAfterInitialization()
        {
            // Wait for all Start() methods to complete
            yield return new WaitForEndOfFrame();

            // Now disable all pooled enemies
            foreach (GameObject ravager in _objectPool)
            {
                ravager.SetActive(false);
            }

            if (_debugMode)
            {
                Debug.Log($"[RavagerSpawner] Pool initialized and disabled. Ready for gameplay!");
            }
        }

        /// <summary>
        /// Check for spawning reinforcements
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

            // Check if it's time to spawn reinforcements
            if (Time.time >= _nextSpawnTime && _spawnedRavagers.Count < _maxAliveAtOnce)
            {
                SpawnReinforcements();
                _nextSpawnTime = Time.time + Random.Range(_minSpawnInterval, _maxSpawnInterval);
            }
        }

        /// <summary>
        /// Spawn initial Ravagers at the outpost
        /// </summary>
        private void SpawnInitialRavagers()
        {
            if (_hasSpawnedInitial)
                return;

            int spawnCount = Mathf.Min(_initialSpawnCount, _spawnPoints.Length);

            for (int i = 0; i < spawnCount; i++)
            {
                SpawnRavagerAtPoint(_spawnPoints[i]);
            }

            _hasSpawnedInitial = true;

            if (_debugMode)
            {
                Debug.Log($"[RavagerSpawner] Spawned {spawnCount} initial Ravagers at '{gameObject.name}'");
            }
        }

        /// <summary>
        /// Spawn reinforcement wave or horde
        /// </summary>
        private void SpawnReinforcements()
        {
            // Random chance to spawn a horde instead of normal reinforcement
            bool isHorde = Random.value <= _hordeChance;
            int spawnCount = isHorde ? _hordeSize : _reinforcementCount;

            // Don't exceed max alive limit
            int availableSlots = _maxAliveAtOnce - _spawnedRavagers.Count;
            spawnCount = Mathf.Min(spawnCount, availableSlots);

            if (spawnCount <= 0)
                return;

            // Spawn the reinforcements
            for (int i = 0; i < spawnCount; i++)
            {
                // Pick a random spawn point
                Transform spawnPoint = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
                SpawnRavagerAtPoint(spawnPoint);
            }

            if (_debugMode)
            {
                string type = isHorde ? "HORDE" : "reinforcement";
                Debug.Log($"[RavagerSpawner] Spawned {spawnCount} Ravager {type} at '{gameObject.name}'. Total alive: {_spawnedRavagers.Count}");
            }
        }

        /// <summary>
        /// Spawn a single Ravager at a specific spawn point
        /// </summary>
        private void SpawnRavagerAtPoint(Transform spawnPoint)
        {
            if (spawnPoint == null)
                return;

            GameObject ravager = null;

            // Use object pool if enabled
            if (_useObjectPooling)
            {
                ravager = GetFromPool();
                if (ravager == null)
                {
                    if (_debugMode)
                    {
                        Debug.LogWarning($"[RavagerSpawner] Object pool exhausted! Increase pool size.");
                    }
                    return;
                }
            }
            else
            {
                // Fallback to Instantiate if pooling disabled
                ravager = Instantiate(_ravagerPrefab);
            }

            // Add random offset in a radius so they don't spawn on top of each other
            Vector3 randomOffset = new Vector3(
                Random.Range(-3f, 3f),
                0f,
                Random.Range(-3f, 3f)
            );
            Vector3 spawnPosition = spawnPoint.position + randomOffset;

            // Position and activate the Ravager
            ravager.transform.position = spawnPosition;
            ravager.transform.rotation = spawnPoint.rotation;
            ravager.SetActive(true);

            _spawnedRavagers.Add(ravager);
        }

        /// <summary>
        /// Get an inactive Ravager from the pool
        /// </summary>
        private GameObject GetFromPool()
        {
            foreach (GameObject ravager in _objectPool)
            {
                if (!ravager.activeInHierarchy)
                {
                    return ravager;
                }
            }
            return null; // Pool exhausted
        }

        /// <summary>
        /// Get current alive count
        /// </summary>
        public int GetAliveCount()
        {
            _spawnedRavagers.RemoveAll(ravager => ravager == null);
            return _spawnedRavagers.Count;
        }

        /// <summary>
        /// Manually trigger a horde spawn
        /// </summary>
        public void SpawnHorde()
        {
            int availableSlots = _maxAliveAtOnce - _spawnedRavagers.Count;
            int spawnCount = Mathf.Min(_hordeSize, availableSlots);

            for (int i = 0; i < spawnCount; i++)
            {
                Transform spawnPoint = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
                SpawnRavagerAtPoint(spawnPoint);
            }

            if (_debugMode)
            {
                Debug.Log($"[RavagerSpawner] Manually spawned horde of {spawnCount} Ravagers!");
            }
        }

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
        }
    }
}
