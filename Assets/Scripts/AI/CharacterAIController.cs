/*
 * Character AI Controller for Klyra's Reach
 *
 * PURPOSE:
 * Main AI controller for NPC characters in the game.
 * Integrates with Opsive Ultimate Character Controller for movement and animations.
 * Supports multiple behaviors and can be expanded for future AI features.
 *
 * HOW TO USE:
 * 1. Add this script to your AI character GameObject (must have UltimateCharacterLocomotion)
 * 2. The AI will automatically use Opsive's animation system
 * 3. Set the character name and behavior
 * 4. Press Play
 */

using UnityEngine;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities.AI;

namespace KlyrasReach.AI
{
    /// <summary>
    /// AI behavior states
    /// </summary>
    public enum AIBehavior
    {
        Idle,           // Standing idle, no movement (default)
        Patrol,         // Walking a patrol route (future)
        Follow,         // Following the player (future)
        Wander,         // Random wandering (future)
        Guard           // Guard a position and look around (future)
    }

    /// <summary>
    /// Main AI controller for NPC characters
    /// Integrates with Opsive Ultimate Character Controller
    /// </summary>
    public class CharacterAIController : MonoBehaviour
    {
        [Header("Character Info")]
        [Tooltip("Display name for this AI character")]
        [SerializeField] private string _characterName = "NPC";

        [Tooltip("Optional description/role for this character")]
        [SerializeField] private string _characterRole = "Citizen";

        [Header("AI Behavior")]
        [Tooltip("Current behavior state of this AI")]
        [SerializeField] private AIBehavior _currentBehavior = AIBehavior.Idle;

        [Header("Patrol Settings")]
        [Tooltip("Waypoints for patrol route (empty GameObjects work great)")]
        [SerializeField] private Transform[] _patrolWaypoints;

        [Tooltip("Should the AI loop back to the first waypoint?")]
        [SerializeField] private bool _loopPatrol = true;

        [Tooltip("How close to waypoint before moving to next (meters)")]
        [SerializeField] private float _waypointReachedDistance = 1f;

        [Tooltip("How long to wait at each waypoint (seconds)")]
        [SerializeField] private float _waitTimeAtWaypoint = 2f;

        [Header("Look Settings")]
        [Tooltip("Should the AI look at the player when nearby?")]
        [SerializeField] private bool _lookAtPlayer = false;

        [Tooltip("Distance to start looking at player")]
        [SerializeField] private float _lookDistance = 5f;

        [Header("Name Tag")]
        [Tooltip("Show name tag when player looks at this NPC?")]
        [SerializeField] private bool _showNameTag = true;

        [Header("Debug")]
        [Tooltip("Show debug messages in console?")]
        [SerializeField] private bool _debugMode = false;

        // Opsive components
        private UltimateCharacterLocomotion _characterLocomotion;
        private LocalLookSource _localLookSource;
        private NPCNameTag _nameTag;

        // Movement abilities (for future behaviors)
        private PathfindingMovement _pathfindingMovement;
        private NavMeshAgentMovement _navMeshMovement;

        // Private variables
        private bool _isInitialized = false;
        private Transform _playerTransform;
        private AIBehavior _previousBehavior;

        // Patrol variables
        private int _currentWaypointIndex = 0;
        private bool _isWaitingAtWaypoint = false;
        private float _waypointWaitTimer = 0f;
        private bool _patrolReversing = false; // For ping-pong patrol

        /// <summary>
        /// Initialize the AI character
        /// </summary>
        private void Start()
        {
            InitializeAI();
        }

        /// <summary>
        /// Setup AI components and Opsive integration
        /// </summary>
        private void InitializeAI()
        {
            // Get Opsive character locomotion component
            _characterLocomotion = GetComponent<UltimateCharacterLocomotion>();
            if (_characterLocomotion == null)
            {
                Debug.LogError($"[CharacterAI] '{_characterName}' missing UltimateCharacterLocomotion component! AI will not work.");
                enabled = false;
                return;
            }

            // Get movement abilities for future use
            _pathfindingMovement = _characterLocomotion.GetAbility<PathfindingMovement>();
            _navMeshMovement = _characterLocomotion.GetAbility<NavMeshAgentMovement>();

            if (_debugMode)
            {
                Debug.Log($"[CharacterAI] '{_characterName}' PathfindingMovement: {(_pathfindingMovement != null ? "Found" : "NOT FOUND")}");
                Debug.Log($"[CharacterAI] '{_characterName}' NavMeshAgentMovement: {(_navMeshMovement != null ? "Found" : "NOT FOUND")}");
            }

            // Setup look source if needed
            if (_lookAtPlayer)
            {
                _localLookSource = GetComponent<LocalLookSource>();
                if (_localLookSource == null)
                {
                    _localLookSource = gameObject.AddComponent<LocalLookSource>();
                }

                // Find player
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    _playerTransform = playerObject.transform;
                    _localLookSource.Target = _playerTransform;
                    if (_debugMode)
                    {
                        Debug.Log($"[CharacterAI] '{_characterName}' will look at player: {playerObject.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[CharacterAI] '{_characterName}' could not find player for look target!");
                }
            }

            // Setup name tag if enabled
            if (_showNameTag)
            {
                _nameTag = GetComponent<NPCNameTag>();
                if (_nameTag == null)
                {
                    _nameTag = gameObject.AddComponent<NPCNameTag>();
                    Debug.Log($"[CharacterAI] '{_characterName}' added NPCNameTag component");
                }
                else
                {
                    Debug.Log($"[CharacterAI] '{_characterName}' found existing NPCNameTag component");
                }
            }
            else
            {
                Debug.Log($"[CharacterAI] '{_characterName}' name tag disabled");
            }

            _previousBehavior = _currentBehavior;
            _isInitialized = true;

            Debug.Log($"[CharacterAI] '{_characterName}' ({_characterRole}) initialized. Behavior: {_currentBehavior}");
        }

        /// <summary>
        /// Main AI update loop
        /// </summary>
        private void Update()
        {
            if (!_isInitialized)
                return;

            // Check if behavior changed
            if (_currentBehavior != _previousBehavior)
            {
                OnBehaviorChanged(_previousBehavior, _currentBehavior);
                _previousBehavior = _currentBehavior;
            }

            // Execute current behavior
            switch (_currentBehavior)
            {
                case AIBehavior.Idle:
                    UpdateIdleBehavior();
                    break;

                case AIBehavior.Patrol:
                    UpdatePatrolBehavior();
                    break;

                case AIBehavior.Follow:
                    UpdateFollowBehavior();
                    break;

                case AIBehavior.Wander:
                    UpdateWanderBehavior();
                    break;

                case AIBehavior.Guard:
                    UpdateGuardBehavior();
                    break;
            }

            // Update look behavior if enabled
            if (_lookAtPlayer && _localLookSource != null && _playerTransform != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
                _localLookSource.enabled = (distanceToPlayer <= _lookDistance);
            }
        }

        /// <summary>
        /// Called when behavior state changes
        /// </summary>
        private void OnBehaviorChanged(AIBehavior oldBehavior, AIBehavior newBehavior)
        {
            if (_debugMode)
            {
                Debug.Log($"[CharacterAI] '{_characterName}' behavior changed: {oldBehavior} -> {newBehavior}");
            }

            // Disable all movement abilities when changing behavior
            DisableAllMovement();

            // Initialize behavior-specific state
            if (newBehavior == AIBehavior.Patrol)
            {
                // Start patrol from first waypoint
                _currentWaypointIndex = 0;
                _isWaitingAtWaypoint = false;
                _patrolReversing = false;
                MoveToNextWaypoint();
            }
        }

        /// <summary>
        /// Disable all movement abilities
        /// </summary>
        private void DisableAllMovement()
        {
            if (_pathfindingMovement != null)
            {
                _pathfindingMovement.Enabled = false;
            }

            if (_navMeshMovement != null)
            {
                _navMeshMovement.Enabled = false;
            }
        }

        /// <summary>
        /// Idle behavior - character stands still
        /// Opsive will automatically play idle animations
        /// </summary>
        private void UpdateIdleBehavior()
        {
            // Do nothing - character will use Opsive's idle animations automatically
            // No movement abilities are active, so character just stands
        }

        /// <summary>
        /// Patrol behavior - character walks a route
        /// </summary>
        private void UpdatePatrolBehavior()
        {
            // Check if we have waypoints and PathfindingMovement ability
            if (_patrolWaypoints == null || _patrolWaypoints.Length == 0)
            {
                if (_debugMode)
                {
                    Debug.LogWarning($"[CharacterAI] '{_characterName}' has no patrol waypoints!");
                }
                return;
            }

            if (_pathfindingMovement == null && _navMeshMovement == null)
            {
                if (_debugMode)
                {
                    Debug.LogWarning($"[CharacterAI] '{_characterName}' has no PathfindingMovement or NavMeshAgentMovement ability!");
                }
                return;
            }

            // If waiting at waypoint, count down timer
            if (_isWaitingAtWaypoint)
            {
                _waypointWaitTimer -= Time.deltaTime;
                if (_waypointWaitTimer <= 0f)
                {
                    _isWaitingAtWaypoint = false;
                    MoveToNextWaypoint();
                }
                return;
            }

            // Check if we reached current waypoint
            Transform currentWaypoint = _patrolWaypoints[_currentWaypointIndex];
            float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint.position);

            // Debug: Log distance every few seconds
            if (_debugMode && Time.frameCount % 120 == 0) // Every 2 seconds at 60fps
            {
                Debug.Log($"[CharacterAI] '{_characterName}' at position {transform.position}, waypoint {_currentWaypointIndex} at {currentWaypoint.position}, distance: {distanceToWaypoint:F2}m (need <= {_waypointReachedDistance}m)");
            }

            if (distanceToWaypoint <= _waypointReachedDistance)
            {
                // Reached waypoint - start wait timer
                _isWaitingAtWaypoint = true;
                _waypointWaitTimer = _waitTimeAtWaypoint;

                if (_debugMode)
                {
                    Debug.Log($"[CharacterAI] '{_characterName}' reached waypoint {_currentWaypointIndex} ('{currentWaypoint.name}'). Waiting {_waitTimeAtWaypoint}s...");
                }

                // Stop movement while waiting
                if (_pathfindingMovement != null)
                {
                    _pathfindingMovement.Enabled = false;
                }
                if (_navMeshMovement != null)
                {
                    _navMeshMovement.Enabled = false;
                }
            }
        }

        /// <summary>
        /// Move to the next waypoint in the patrol route
        /// </summary>
        private void MoveToNextWaypoint()
        {
            if (_patrolWaypoints == null || _patrolWaypoints.Length == 0)
                return;

            // Calculate next waypoint index
            if (_loopPatrol)
            {
                // Loop mode - go 0, 1, 2, ... n, 0, 1, 2, ...
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _patrolWaypoints.Length;
            }
            else
            {
                // Ping-pong mode - go 0, 1, 2, 1, 0, 1, 2, ...
                if (_patrolReversing)
                {
                    _currentWaypointIndex--;
                    if (_currentWaypointIndex <= 0)
                    {
                        _currentWaypointIndex = 0;
                        _patrolReversing = false;
                    }
                }
                else
                {
                    _currentWaypointIndex++;
                    if (_currentWaypointIndex >= _patrolWaypoints.Length - 1)
                    {
                        _currentWaypointIndex = _patrolWaypoints.Length - 1;
                        _patrolReversing = true;
                    }
                }
            }

            Transform targetWaypoint = _patrolWaypoints[_currentWaypointIndex];

            if (_debugMode)
            {
                Debug.Log($"[CharacterAI] '{_characterName}' moving to waypoint {_currentWaypointIndex} ('{targetWaypoint.name}')");
            }

            // Use PathfindingMovement if available
            if (_pathfindingMovement != null)
            {
                _pathfindingMovement.SetDestination(targetWaypoint.position);
                _pathfindingMovement.Enabled = true;

                if (_debugMode)
                {
                    Debug.Log($"[CharacterAI] Using PathfindingMovement, enabled: {_pathfindingMovement.Enabled}");
                }
            }
            // Otherwise try NavMeshAgentMovement
            else if (_navMeshMovement != null)
            {
                _navMeshMovement.SetDestination(targetWaypoint.position);
                _navMeshMovement.Enabled = true;

                if (_debugMode)
                {
                    Debug.Log($"[CharacterAI] Using NavMeshAgentMovement, enabled: {_navMeshMovement.Enabled}");
                }
            }
            else
            {
                if (_debugMode)
                {
                    Debug.LogError($"[CharacterAI] '{_characterName}' has NO movement abilities! Add PathfindingMovement or NavMeshAgentMovement ability to the character.");
                }
            }
        }

        /// <summary>
        /// Follow behavior - character follows target (like FollowAgent.cs example)
        /// </summary>
        private void UpdateFollowBehavior()
        {
            // Future: Use PathfindingMovement.SetDestination() to follow player
            if (_debugMode)
            {
                Debug.LogWarning($"[CharacterAI] '{_characterName}' follow behavior not yet implemented!");
            }
        }

        /// <summary>
        /// Wander behavior - character randomly walks around
        /// </summary>
        private void UpdateWanderBehavior()
        {
            // Future: Pick random points and use PathfindingMovement
            if (_debugMode)
            {
                Debug.LogWarning($"[CharacterAI] '{_characterName}' wander behavior not yet implemented!");
            }
        }

        /// <summary>
        /// Guard behavior - stay in position but look around
        /// </summary>
        private void UpdateGuardBehavior()
        {
            // Future: Implement guard rotation/scanning behavior
            if (_debugMode)
            {
                Debug.LogWarning($"[CharacterAI] '{_characterName}' guard behavior not yet implemented!");
            }
        }

        /// <summary>
        /// Change the AI's current behavior
        /// </summary>
        public void SetBehavior(AIBehavior newBehavior)
        {
            _currentBehavior = newBehavior;
        }

        /// <summary>
        /// Get the current behavior state
        /// </summary>
        public AIBehavior GetCurrentBehavior()
        {
            return _currentBehavior;
        }

        /// <summary>
        /// Get the character's name
        /// </summary>
        public string GetCharacterName()
        {
            return _characterName;
        }

        /// <summary>
        /// Get the character's role
        /// </summary>
        public string GetCharacterRole()
        {
            return _characterRole;
        }

        /// <summary>
        /// Check if player is within look distance
        /// </summary>
        public bool IsPlayerNearby()
        {
            if (_playerTransform == null)
                return false;

            return Vector3.Distance(transform.position, _playerTransform.position) <= _lookDistance;
        }

        /// <summary>
        /// Draw debug visualization in Scene view
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw character identification sphere
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 0.3f);

            // Draw look distance if enabled
            if (_lookAtPlayer)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, _lookDistance);
            }

            // Draw patrol route
            if (_patrolWaypoints != null && _patrolWaypoints.Length > 0)
            {
                Gizmos.color = Color.green;

                // Draw waypoint spheres
                for (int i = 0; i < _patrolWaypoints.Length; i++)
                {
                    if (_patrolWaypoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(_patrolWaypoints[i].position, 0.5f);

                        // Draw waypoint number
                        UnityEngine.GUI.color = Color.green;
                    }
                }

                // Draw lines connecting waypoints
                Gizmos.color = Color.yellow;
                for (int i = 0; i < _patrolWaypoints.Length - 1; i++)
                {
                    if (_patrolWaypoints[i] != null && _patrolWaypoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(_patrolWaypoints[i].position, _patrolWaypoints[i + 1].position);
                    }
                }

                // If looping, draw line from last to first
                if (_loopPatrol && _patrolWaypoints.Length > 1)
                {
                    if (_patrolWaypoints[_patrolWaypoints.Length - 1] != null && _patrolWaypoints[0] != null)
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawLine(_patrolWaypoints[_patrolWaypoints.Length - 1].position, _patrolWaypoints[0].position);
                    }
                }

                // Draw line from AI to current target waypoint
                if (Application.isPlaying && _currentWaypointIndex < _patrolWaypoints.Length)
                {
                    if (_patrolWaypoints[_currentWaypointIndex] != null)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(transform.position, _patrolWaypoints[_currentWaypointIndex].position);
                    }
                }
            }
        }
    }
}
