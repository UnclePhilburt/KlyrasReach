/*
 * Network Enemy Sync for Klyra's Reach
 *
 * PURPOSE:
 * Synchronizes enemy state (position, rotation, health, death) across all
 * clients in multiplayer. The Master Client is authoritative - it runs the AI
 * and sends state to other clients. Non-master clients interpolate to the
 * received state.
 *
 * Also handles networked damage: when any player hits an enemy, the damage
 * is routed to the Master Client via RPC, applied there, and the resulting
 * health is synced to all clients via OnPhotonSerializeView.
 *
 * NON-MASTER CLIENT SETUP:
 * On non-master clients, this script disables ALL Opsive components (except
 * AttributeManager for health tracking) to prevent them from:
 * - Fighting with the networked position/animator sync
 * - Causing NullReferenceExceptions (compiled Opsive components reference
 *   UltimateCharacterLocomotion's internal state which becomes invalid when disabled)
 * - Overriding animator parameters via OnPhotonSerializeView callbacks
 * It then drives the Animator directly based on movement speed computed from
 * position deltas, and uses LateUpdate to force Height=0 (grounded) after any
 * OnPhotonSerializeView overrides. This fixes the "falling animation" bug and
 * ensures colliders stay aligned with the visual mesh for proper hit detection.
 *
 * HOW TO USE:
 * 1. Add this component to your enemy prefab (must also have a PhotonView)
 * 2. Make sure the PhotonView's Observed Components list includes this script
 * 3. Enemies must be spawned with PhotonNetwork.InstantiateRoomObject in multiplayer
 * 4. Damage scripts should call RequestDamage() instead of health.Damage() directly
 *
 * REQUIRES:
 * - PhotonView component on the same GameObject
 * - Opsive AttributeManager with "Health" attribute (optional but recommended)
 */

using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController;

namespace KlyrasReach.AI
{
    /// <summary>
    /// Syncs enemy position, health, and damage across the Photon network.
    /// Master Client is authoritative on all enemy state.
    /// On non-master clients, disables Opsive locomotion and drives animator directly.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class NetworkEnemySync : MonoBehaviourPun, IPunObservable
    {
        [Header("Position Sync")]
        [Tooltip("How fast non-master clients interpolate to the network position")]
        [SerializeField] private float _positionLerpSpeed = 10f;

        [Tooltip("How fast non-master clients interpolate to the network rotation")]
        [SerializeField] private float _rotationLerpSpeed = 10f;

        [Tooltip("If position is off by more than this, snap instead of lerp (meters)")]
        [SerializeField] private float _snapDistance = 5f;

        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;

        // Opsive health references
        private Opsive.UltimateCharacterController.Traits.AttributeManager _attributeManager;
        private Opsive.UltimateCharacterController.Traits.Attribute _healthAttribute;

        // Reference to the AI controller for triggering death
        // Supports both Ravagers (EnemyAIController) and Sentinels (SentinelAIController)
        private EnemyAIController _aiController;
        private SentinelAIController _sentinelAIController;

        // Network state received from master client
        private Vector3 _networkPosition;
        private Quaternion _networkRotation;
        private bool _isDead = false;

        // Animator control for non-master clients
        // On non-master clients, Opsive is disabled so we drive the Animator directly
        private Animator _animator;
        private Vector3 _previousPosition;        // For computing movement speed from position delta
        private float _currentSpeed = 0f;          // Smoothed speed for animator blending
        private bool _isNonMasterClient = false;    // Cached flag to avoid checking every frame
        private Renderer[] _cachedRenderers;         // Cached for forcing visibility on alive enemies
        private Vector3 _lastSetPosition;            // Tracks position WE set, to detect external changes
        private float _spawnTime;                     // Time.time when this enemy was created

        // Position authority flag - set true after first interpolation in Update().
        // When true, FixedUpdate and Update start will always reset position to
        // _lastSetPosition, undoing any external modifications unconditionally.
        private bool _positionAuthority = false;
        private bool _hasReceivedFirstSync = false;  // Tracks if OnPhotonSerializeView ever fires

        // Opsive animator parameter hash IDs (cached for performance)
        // These match the parameter names Opsive uses in its Animator Controllers
        private static readonly int HASH_SPEED = Animator.StringToHash("Speed");
        private static readonly int HASH_HEIGHT = Animator.StringToHash("Height");
        private static readonly int HASH_MOVING = Animator.StringToHash("Moving");
        private static readonly int HASH_FORWARD_MOVEMENT = Animator.StringToHash("ForwardMovement");
        private static readonly int HASH_HORIZONTAL_MOVEMENT = Animator.StringToHash("HorizontalMovement");

        // Opsive ability animator parameters - these control which ability state the animator uses.
        // AbilityIndex=2 is the "Fall" ability, which causes the floating/falling animation.
        // We MUST reset AbilityIndex to 0 on non-master clients to prevent enemies from being
        // stuck in the Fall state.
        private static readonly int HASH_ABILITY_INDEX = Animator.StringToHash("AbilityIndex");
        private static readonly int HASH_ABILITY_CHANGE = Animator.StringToHash("AbilityChange");
        private static readonly int HASH_ABILITY_INT_DATA = Animator.StringToHash("AbilityIntData");
        private static readonly int HASH_ABILITY_FLOAT_DATA = Animator.StringToHash("AbilityFloatData");
        private static readonly int HASH_MOVEMENT_SET_ID = Animator.StringToHash("MovementSetID");

        /// <summary>
        /// Runs BEFORE Start() on ALL clients (master AND non-master).
        ///
        /// CRITICAL: Removes ALL Opsive IPunObservable components from the PhotonView's
        /// observed components list. This MUST happen on both sides (master + non-master)
        /// to keep the Photon serialization stream aligned.
        ///
        /// WHY THIS IS NEEDED:
        /// Each observed component writes/reads data from the same PhotonStream in order.
        /// Opsive's PunCharacterAnimatorMonitor has a conditional "HasItemParameters" check
        /// (line 240/298) that writes/reads EXTRA item data. When we disable AnimatorMonitor
        /// on non-master clients, HasItemParameters can return a DIFFERENT value than on
        /// the master. This causes the non-master to read a different amount of data,
        /// misaligning the stream. Our NetworkEnemySync then reads garbage data for position,
        /// health, and death - causing some enemies to float, jitter invisible, or be immune
        /// to damage.
        ///
        /// By removing all Opsive observed components on BOTH sides, only NetworkEnemySync
        /// writes/reads the stream, and it always matches.
        /// </summary>
        private void Awake()
        {
            CleanupPhotonViewObservedComponents();

            // CRITICAL: Determine master/non-master status IMMEDIATELY in Awake().
            // PhotonNetwork.IsMasterClient is a static check that works even in Awake.
            // We MUST disable Opsive components HERE (before any Start() methods run)
            // because otherwise Opsive's Start() initializes the Respawner, Inventory,
            // Health, etc. on non-master clients. This causes:
            //   - Respawner teleporting enemies to respawn positions
            //   - Inventory trying to spawn CharacterItem prefabs (null prefab errors)
            //   - Health potentially triggering death before sync starts
            //   - Renderers being hidden by Opsive's item equip system
            // By disabling in Awake(), none of Opsive's Start() methods will execute.
            _isNonMasterClient = PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient;

            if (_isNonMasterClient)
            {
                DisableOpsiveComponentsEarly();
            }
        }

        /// <summary>
        /// EARLY disabling of Opsive components in Awake() - runs BEFORE any Start() methods.
        /// This prevents Opsive's Respawner, Inventory, Health, and item systems from
        /// initializing on non-master clients where they'd fight with our sync system.
        /// The full non-master setup (animator, rigidbody, renderers) happens in Start().
        /// </summary>
        private void DisableOpsiveComponentsEarly()
        {
            // Disable ALL Opsive MonoBehaviours on this GameObject
            int disabledCount = 0;
            var allComponents = GetComponents<MonoBehaviour>();
            foreach (var component in allComponents)
            {
                if (component == null) continue;
                if (component == this) continue;
                if (component is EnemyAIController || component is SentinelAIController) continue;

                string ns = component.GetType().Namespace ?? "";

                // Disable ALL Opsive components EXCEPT AttributeManager (needed for health sync)
                if (ns.Contains("Opsive"))
                {
                    if (component.GetType().Name == "AttributeManager") continue;
                    component.enabled = false;
                    disabledCount++;
                }
            }

            // Also disable Opsive components on CHILD objects (AnimatorMonitor lives on model child)
            var allChildComponents = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var component in allChildComponents)
            {
                if (component == null) continue;
                if (component.gameObject == gameObject) continue;

                string ns = component.GetType().Namespace ?? "";
                if (ns.Contains("Opsive"))
                {
                    component.enabled = false;
                    disabledCount++;
                }
            }

            // Disable NavMeshAgent immediately too - prevents "not close enough to NavMesh" errors
            var navAgent = GetComponent<NavMeshAgent>();
            if (navAgent != null)
            {
                navAgent.enabled = false;
            }

            // Disable PhotonAnimatorView immediately - it syncs Animator parameters and
            // modifies transform.position, fighting with our position sync.
            // DestroyConflictingNetworkComponents in Start() will Destroy it, but we must
            // disable it NOW to prevent its Update() from running before our Start().
            var photonAnimView = GetComponent<Photon.Pun.PhotonAnimatorView>();
            if (photonAnimView != null)
            {
                photonAnimView.enabled = false;
            }

            // CRITICAL: Destroy the Respawner component to prevent a teleport race condition.
            // When we disable Respawner above (before its own Awake runs), OnDisable() fires
            // with m_NetworkInfo still null. The multiplayer guard:
            //     if (m_NetworkInfo != null && !HasAuthority()) return;
            // FAILS because m_NetworkInfo is null (first condition false → whole check false).
            // So it schedules a respawn via ScheduleRespawnOnDisable. Seconds later, the
            // scheduled respawn fires and teleports the enemy back to its spawn position,
            // causing a sudden 3-5m lateral jump on non-master clients.
            // Destroying the component makes the scheduled callback find a null target.
            var respawner = GetComponent<Opsive.UltimateCharacterController.Traits.Respawner>();
            if (respawner != null)
            {
                Destroy(respawner);

                if (_debugMode)
                {
                    Debug.Log($"[NetworkEnemySync] Destroyed Respawner on '{gameObject.name}' to prevent scheduled teleport");
                }
            }

            // Set Rigidbody to kinematic IMMEDIATELY in Awake, not Start.
            // FixedUpdate can run between Awake and Start. If the Rigidbody is
            // non-kinematic during that gap, physics resolves collider overlaps
            // and pushes the enemy ~0.5m upward, fighting with position sync.
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            // Unregister from SimulationManager as early as possible.
            // Opsive's SimulationManager calls Move() on all registered characters every
            // FixedUpdate, which applies gravity (moves transform directly, not via Rigidbody).
            // CharacterLocomotion registers in OnEnable, which fires during Instantiate.
            // If we wait until Start() to unregister, FixedUpdate can sneak in between
            // Awake and Start, applying ~0.9m of gravity downward.
            var locomotion = GetComponent<CharacterLocomotion>();
            if (locomotion != null && locomotion.SimulationIndex >= 0)
            {
                SimulationManager.UnregisterCharacter(locomotion.SimulationIndex);

                if (_debugMode)
                {
                    Debug.Log($"[NetworkEnemySync] EARLY Awake unregister of '{gameObject.name}' from SimulationManager (index {locomotion.SimulationIndex})");
                }
            }

            // Record spawn time so Update() can use a grace period for position overrides
            _spawnTime = Time.time;

            if (_debugMode)
            {
                Debug.Log($"[NetworkEnemySync] EARLY Awake() disabled {disabledCount} Opsive components on '{gameObject.name}' (before Start)");
            }
        }

        /// <summary>
        /// Removes ALL other components from the PhotonView's observed list.
        /// Only NetworkEnemySync should remain - it handles ALL enemy serialization.
        /// Must run on ALL clients to keep serialization streams aligned.
        /// </summary>
        private void CleanupPhotonViewObservedComponents()
        {
            if (photonView == null || photonView.ObservedComponents == null) return;

            // Remove ALL components except ourselves from the observed list.
            // Previously we only removed Opsive-namespace components, but ANY extra
            // observer causes stream misalignment (master writes X bytes, non-master
            // reads different amount → garbage data for position/health/death).
            for (int i = photonView.ObservedComponents.Count - 1; i >= 0; i--)
            {
                var component = photonView.ObservedComponents[i];

                // Keep ourselves
                if (component == this) continue;

                // Remove everything else (null entries, Opsive components, any stray observers)
                photonView.ObservedComponents.RemoveAt(i);

                if (_debugMode && component != null)
                {
                    Debug.Log($"[NetworkEnemySync] Removed '{component.GetType().Name}' from PhotonView observed list on '{gameObject.name}'");
                }
            }

            // Make sure WE are in the list (in case prefab wasn't set up correctly)
            if (!photonView.ObservedComponents.Contains(this))
            {
                photonView.ObservedComponents.Add(this);
            }

            // ALWAYS log the observed list state so we can verify serialization works
            string observerList = "";
            for (int i = 0; i < photonView.ObservedComponents.Count; i++)
            {
                var obs = photonView.ObservedComponents[i];
                observerList += obs != null ? obs.GetType().Name : "NULL";
                if (i < photonView.ObservedComponents.Count - 1) observerList += ", ";
            }
            Debug.Log($"[NetworkEnemySync] PhotonView observed list for '{gameObject.name}': [{observerList}] " +
                $"(viewID={photonView.ViewID}, isMine={photonView.IsMine}, observeOption={photonView.Synchronization})");
        }

        /// <summary>
        /// Destroys Opsive PUN network monitor components that conflict with our sync system.
        /// Must run on ALL clients (master AND non-master) because:
        ///   - PunHealthMonitor sends OnDamageRPC to All and DieRPC to Others
        ///   - DieRPC calls Health.Die() which hides renderers → invisibility jitter
        ///   - PunRespawnerMonitor sends respawn RPCs that conflict with our death handling
        /// We Destroy() instead of disable because Photon delivers RPCs to disabled components.
        /// </summary>
        private void DestroyConflictingNetworkComponents()
        {
            int destroyedCount = 0;
            var allComponents = GetComponents<MonoBehaviour>();
            foreach (var component in allComponents)
            {
                if (component == null) continue;
                if (component == this) continue;

                string typeName = component.GetType().Name;
                string ns = component.GetType().Namespace ?? "";

                // Destroy ALL Opsive "Pun" network components (PunHealthMonitor,
                // PunRespawnerMonitor, PunCharacterAnimatorMonitor, PunItemMonitor, etc.)
                // Photon delivers RPCs to disabled MonoBehaviours, so just disabling
                // them isn't enough. When the master enemy attacks/moves/equips, these
                // components send RPCs that the non-master receives and acts on - even
                // though the component is disabled. This can hide renderers, trigger
                // death animations, or change animator state, causing enemies to
                // "disappear" on non-master clients.
                // NetworkEnemySync handles ALL enemy sync, so these are not needed.
                //
                // Also destroy PhotonAnimatorView - it syncs Animator parameters over the
                // network independently of our system. On non-master, it can override the
                // Animator state we set, and its Update() modifies transform.position by
                // ~0.1-0.5m per frame, causing position fighting and walk-in-place.
                // We drive the Animator directly in UpdateAnimatorOnNonMaster().
                bool isConflicting = (ns.Contains("Opsive") && typeName.Contains("Pun")) ||
                                     typeName == "PhotonAnimatorView" ||
                                     typeName == "PhotonTransformView" ||
                                     typeName == "PhotonTransformViewClassic";

                if (isConflicting)
                {
                    component.enabled = false;
                    Destroy(component);
                    destroyedCount++;

                    if (_debugMode)
                    {
                        Debug.Log($"[NetworkEnemySync] Destroyed '{typeName}' on '{gameObject.name}' to prevent conflicting sync");
                    }
                }
            }

            // Also check child objects (PunCharacterAnimatorMonitor lives on the model child)
            var allChildComponents = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var component in allChildComponents)
            {
                if (component == null) continue;
                if (component.gameObject == gameObject) continue; // Already handled above

                string typeName = component.GetType().Name;
                string ns = component.GetType().Namespace ?? "";

                bool isConflictingChild = (ns.Contains("Opsive") && typeName.Contains("Pun")) ||
                                          typeName == "PhotonAnimatorView" ||
                                          typeName == "PhotonTransformView" ||
                                          typeName == "PhotonTransformViewClassic";

                if (isConflictingChild)
                {
                    component.enabled = false;
                    Destroy(component);
                    destroyedCount++;

                    if (_debugMode)
                    {
                        Debug.Log($"[NetworkEnemySync] Destroyed child '{typeName}' on '{component.gameObject.name}' to prevent conflicting sync");
                    }
                }
            }

            if (_debugMode && destroyedCount > 0)
            {
                Debug.Log($"[NetworkEnemySync] Destroyed {destroyedCount} conflicting Opsive PUN component(s) on '{gameObject.name}'");
            }
        }

        private void Start()
        {
            // Run observed components cleanup AGAIN in Start() to catch any components
            // that might have been added during other scripts' Awake() calls.
            // (Awake order between components is non-deterministic in Unity)
            CleanupPhotonViewObservedComponents();

            // CRITICAL: Destroy PunHealthMonitor on ALL clients (master AND non-master).
            // PunHealthMonitor implements INetworkHealthMonitor and is called directly by
            // Opsive's Health component. It sends OnDamageRPC (to All) and DieRPC (to Others).
            // On non-master clients, DieRPC calls Health.Die() which hides renderers and
            // triggers ragdoll - this causes the "jittering invisibility" bug.
            // We handle ALL health/damage/death sync through NetworkEnemySync, so
            // PunHealthMonitor's RPCs conflict with our system.
            // NOTE: We Destroy() rather than just disable, because Photon delivers RPCs
            // even to disabled MonoBehaviours.
            DestroyConflictingNetworkComponents();

            // Cache component references
            _attributeManager = GetComponent<Opsive.UltimateCharacterController.Traits.AttributeManager>();
            if (_attributeManager != null)
            {
                _healthAttribute = _attributeManager.GetAttribute("Health");

                // CRITICAL: Reset health to max on spawn.
                // Disabling Opsive components in Awake() can interrupt Health initialization,
                // leaving the health attribute at a partial value. This ensures every
                // Ravager starts at full health regardless of component initialization order.
                if (_healthAttribute != null)
                {
                    _healthAttribute.Value = _healthAttribute.MaxValue;
                }
            }

            // Clear death flag — this is a fresh spawn
            _isDead = false;

            _aiController = GetComponent<EnemyAIController>();
            _sentinelAIController = GetComponent<SentinelAIController>();

            // MASTER CLIENT ONLY: Listen for Opsive's damage event.
            // When Opsive weapons (like the Vector) hit this enemy, Opsive calls
            // Health.Damage() which fires "OnHealthDamage". We intercept this and
            // route the damage through our own system so it syncs to all clients.
            // Without this, Opsive weapon damage only applies locally on the master
            // and never reaches non-master clients.
            if (!_isNonMasterClient)
            {
                Opsive.Shared.Events.EventHandler.RegisterEvent<float, Vector3, Vector3, GameObject, Collider>(
                    gameObject, "OnHealthDamage", OnOpsiveDamageIntercepted);
            }

            // Initialize network state to current transform
            _networkPosition = transform.position;
            _networkRotation = transform.rotation;
            _previousPosition = transform.position;

            // NON-MASTER CLIENT SETUP:
            // Opsive components were already disabled in Awake() (DisableOpsiveComponentsEarly).
            // Now finish the setup: animator, rigidbody, renderers, colliders.
            // _isNonMasterClient was set in Awake() already.
            if (_isNonMasterClient)
            {
                SetupNonMasterClient();
            }

            if (_debugMode)
            {
                Debug.Log($"[NetworkEnemySync] Initialized on '{gameObject.name}', IsMasterClient: {!_isNonMasterClient}");
            }
        }

        /// <summary>
        /// Finishes non-master client setup in Start().
        ///
        /// NOTE: Opsive components were already disabled in Awake() by DisableOpsiveComponentsEarly().
        /// This method handles the remaining setup that needs Start() timing:
        /// animator control, rigidbody, and forcing renderers/colliders visible.
        ///
        /// On non-master clients, NetworkEnemySync handles EVERYTHING for enemies:
        /// position sync, rotation sync, health sync, animator control, and death sync.
        /// </summary>
        private void SetupNonMasterClient()
        {
            // ==========================================
            // STEP 1: Cache the Animator for direct control
            // ==========================================
            // We drive the Animator directly with movement speed computed from position deltas.
            // The Animator itself stays ENABLED - only Opsive's AnimatorMonitor is disabled.
            _animator = GetComponentInChildren<Animator>();

            if (_animator != null)
            {
                // CRITICAL: Disable root motion on non-master clients!
                // Without this, the walking animation's root motion moves the character,
                // fighting with our position sync. This causes "walk in place" where the
                // feet animate but the character doesn't actually move to the network position.
                _animator.applyRootMotion = false;

                // Reset ALL ability parameters to force the animator out of the
                // Fall state (AbilityIndex=2). Without this, the enemy will be stuck in the
                // falling/floating animation because Opsive's Fall ability set AbilityIndex=2
                // before we disabled the locomotion system.
                _animator.SetInteger(HASH_ABILITY_INDEX, 0);      // 0 = no ability (idle/movement)
                _animator.SetInteger(HASH_ABILITY_INT_DATA, 0);    // Clear ability data
                _animator.SetFloat(HASH_ABILITY_FLOAT_DATA, 0f);   // Clear ability float data
                _animator.SetFloat(HASH_HEIGHT, 0f);               // Grounded
                _animator.SetInteger(HASH_MOVEMENT_SET_ID, 0);     // Default movement set
                _animator.SetTrigger(HASH_ABILITY_CHANGE);         // Trigger state transition
            }

            // ==========================================
            // STEP 2: Set Rigidbody to kinematic
            // ==========================================
            // Prevents physics from interfering with our networked position sync.
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            // ==========================================
            // STEP 3: Unregister from Opsive's SimulationManager
            // ==========================================
            // CRITICAL FIX FOR JITTER: Opsive's SimulationManager keeps a list of all
            // registered characters and calls Move() on them every FixedUpdate/Update.
            // It does NOT check if the component is enabled - it calls Move() regardless.
            // CharacterLocomotion.OnEnable() registers with SimulationManager, and if it
            // runs before our Awake() disables it, the character stays registered.
            // SimulationManager.Move() then fights with our position sync = jitter.
            // We unregister here in Start() so ALL Awake/OnEnable calls have finished.
            var locomotion = GetComponent<CharacterLocomotion>();
            if (locomotion != null && locomotion.SimulationIndex >= 0)
            {
                SimulationManager.UnregisterCharacter(locomotion.SimulationIndex);

                if (_debugMode)
                {
                    Debug.Log($"[NetworkEnemySync] Unregistered '{gameObject.name}' from SimulationManager (was index {locomotion.SimulationIndex})");
                }
            }

            // Also handle deferred registration: CharacterLocomotion.OnEnable() sometimes
            // defers registration via CharacterInitializer callback. If that fires after
            // our unregister, the character would re-register. Run a delayed check to catch this.
            StartCoroutine(DelayedSimulationManagerUnregister());

            // CRITICAL: Opsive's initialization chain (Start, coroutines, initializer callbacks)
            // can RE-ENABLE components we disabled in Awake. For example, CharacterIK, item
            // components (ThirdPersonObject, ShieldCollider), and CapsuleColliderPositioner
            // get re-enabled by Opsive's own setup after our Awake sweep.
            // Run a delayed sweep to catch anything that got re-enabled during initialization.
            StartCoroutine(DelayedOpsiveComponentSweep());

            // ==========================================
            // STEP 4: Force all renderers and colliders enabled
            // ==========================================
            // Disabling Opsive components in Awake() can trigger their OnDisable()
            // callbacks, which may hide mesh parts, reset skeleton positions, or disable
            // colliders. This causes "half invisible" enemies and damage immunity.
            // We explicitly re-enable everything AFTER all components are disabled.
            _cachedRenderers = GetComponentsInChildren<Renderer>(true);
            foreach (var rend in _cachedRenderers)
            {
                if (rend != null)
                {
                    rend.enabled = true;
                }
            }

            // Force all colliders enabled so fireballs can hit the enemy
            var allColliders = GetComponentsInChildren<Collider>(true);
            foreach (var col in allColliders)
            {
                if (col != null)
                {
                    col.enabled = true;
                }
            }

            // NOTE: We do NOT force-activate all child GameObjects here.
            // The nested character model may have intentionally hidden alternate meshes
            // (e.g., different armor/skin variants). Force-activating them shows the wrong model.
            // Weapon visuals (Void's Edge, Void's Ward) are already active by default in the
            // prefab, and their Renderers are force-enabled in Step 3 above.

            if (_debugMode)
            {
                Debug.Log($"[NetworkEnemySync] Non-master setup on '{gameObject.name}': " +
                    $"rootMotion=OFF, forced {_cachedRenderers.Length} renderers + {allColliders.Length} colliders enabled");
            }
        }

        /// <summary>
        /// Unconditional position guard during the physics phase.
        /// On non-master clients, our Update() is the ONLY authority on position.
        /// FixedUpdate resets any external change (gravity, physics, SimulationManager)
        /// back to the last position our interpolation set.
        /// </summary>
        private void FixedUpdate()
        {
            if (!_isNonMasterClient || !_positionAuthority) return;
            transform.position = _lastSetPosition;
        }

        private void Update()
        {
            // Only non-master clients need to interpolate to network position
            // Master client moves enemies directly via AI
            if (!_isNonMasterClient) return;

            // POSITION GUARD: Reset position to what our interpolation set last frame.
            // Undoes ANY external modification (Animator, physics, Opsive, Photon, etc.)
            // unconditionally. Simple and bulletproof.
            if (_positionAuthority)
            {
                transform.position = _lastSetPosition;
            }

            // Interpolate position smoothly
            float distToTarget = Vector3.Distance(transform.position, _networkPosition);
            if (distToTarget > _snapDistance)
            {
                // Too far off - snap immediately (teleport, respawn, etc.)
                transform.position = _networkPosition;
            }
            else
            {
                // Interpolate XZ smoothly for visual polish, but snap Y directly
                // to the network value. The master's Opsive locomotion keeps enemies
                // grounded, so the network Y is always correct. If we lerp Y too,
                // the straight-line interpolation cuts through terrain on slopes,
                // causing enemies to appear underground.
                float lerpFactor = Time.deltaTime * _positionLerpSpeed;
                float newX = Mathf.Lerp(transform.position.x, _networkPosition.x, lerpFactor);
                float newZ = Mathf.Lerp(transform.position.z, _networkPosition.z, lerpFactor);
                transform.position = new Vector3(newX, _networkPosition.y, newZ);
            }

            // Remember what WE set so FixedUpdate/Update guards can undo external changes
            _lastSetPosition = transform.position;
            _positionAuthority = true; // Enable guards after first interpolation

            // Interpolate rotation smoothly
            transform.rotation = Quaternion.Lerp(transform.rotation, _networkRotation, Time.deltaTime * _rotationLerpSpeed);

            // ANIMATOR CONTROL: Drive animator based on movement speed
            // We compute speed from position delta since we don't have Opsive doing it for us
            UpdateAnimatorOnNonMaster();

            // TEMP DEBUG: Every 3 seconds, log position state to verify interpolation works.
            // Remove this once enemies are confirmed working.
            if (Time.frameCount % 180 == 0)
            {
                float distToNetwork = Vector3.Distance(transform.position, _networkPosition);
                Debug.Log($"[NetworkEnemySync] POSITION CHECK '{gameObject.name}': " +
                    $"pos={transform.position}, networkTarget={_networkPosition}, " +
                    $"dist={distToNetwork:F3}, speed={_currentSpeed:F2}");
            }
        }

        /// <summary>
        /// Catches deferred SimulationManager registration.
        /// Opsive's CharacterLocomotion.OnEnable() can defer registration via
        /// CharacterInitializer callback. If that fires after our Start() unregister,
        /// the character would be re-registered. This coroutine waits a frame and
        /// checks again, ensuring the character is fully unregistered.
        /// </summary>
        private System.Collections.IEnumerator DelayedSimulationManagerUnregister()
        {
            // Wait one frame for all deferred callbacks to fire
            yield return null;

            var locomotion = GetComponent<CharacterLocomotion>();
            if (locomotion != null && locomotion.SimulationIndex >= 0)
            {
                SimulationManager.UnregisterCharacter(locomotion.SimulationIndex);

                if (_debugMode)
                {
                    Debug.Log($"[NetworkEnemySync] DELAYED unregister of '{gameObject.name}' from SimulationManager (caught deferred registration)");
                }
            }
        }

        /// <summary>
        /// Delayed sweep to re-disable any Opsive components that got re-enabled.
        /// Opsive's initialization runs across multiple frames (Start, coroutines,
        /// CharacterInitializer callbacks, item equip delays). We sweep at 0.5s, 1.5s,
        /// and 3s to catch everything regardless of how long Opsive takes to initialize.
        /// </summary>
        private System.Collections.IEnumerator DelayedOpsiveComponentSweep()
        {
            yield return new WaitForSeconds(0.5f);
            int count1 = SweepDisableOpsiveComponents();
            if (count1 > 0) Debug.Log($"[NetworkEnemySync] Sweep at 0.5s: re-disabled {count1} Opsive component(s) on '{gameObject.name}'");

            yield return new WaitForSeconds(1.0f);
            int count2 = SweepDisableOpsiveComponents();
            if (count2 > 0) Debug.Log($"[NetworkEnemySync] Sweep at 1.5s: re-disabled {count2} Opsive component(s) on '{gameObject.name}'");

            yield return new WaitForSeconds(1.5f);
            int count3 = SweepDisableOpsiveComponents();
            if (count3 > 0) Debug.Log($"[NetworkEnemySync] Sweep at 3.0s: re-disabled {count3} Opsive component(s) on '{gameObject.name}'");
        }

        /// <summary>
        /// Finds and disables any Opsive components that are currently enabled.
        /// Returns how many were disabled. Skips AttributeManager (needed for health sync)
        /// and NetworkEnemySync/EnemyAIController (our scripts).
        /// </summary>
        private int SweepDisableOpsiveComponents()
        {
            int disabledCount = 0;

            // Sweep root object
            var allComponents = GetComponents<MonoBehaviour>();
            foreach (var component in allComponents)
            {
                if (component == null || component == this) continue;
                if (component is EnemyAIController || component is SentinelAIController) continue;
                if (!component.enabled) continue;

                string ns = component.GetType().Namespace ?? "";
                if (ns.Contains("Opsive"))
                {
                    if (component.GetType().Name == "AttributeManager") continue;
                    component.enabled = false;
                    disabledCount++;
                }
            }

            // Sweep all children
            var allChildComponents = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var component in allChildComponents)
            {
                if (component == null || component.gameObject == gameObject) continue;
                if (!component.enabled) continue;

                string ns = component.GetType().Namespace ?? "";
                if (ns.Contains("Opsive"))
                {
                    component.enabled = false;
                    disabledCount++;
                }
            }

            // Also re-disable NavMeshAgent if it got re-enabled
            var navAgent = GetComponent<NavMeshAgent>();
            if (navAgent != null && navAgent.enabled)
            {
                navAgent.enabled = false;
                disabledCount++;
            }

            return disabledCount;
        }

        /// <summary>
        /// DIAGNOSTIC: Logs detailed information about what could have moved the enemy.
        /// Checks the state of every known system that can modify transform.position:
        /// CharacterLocomotion, SimulationManager, NavMeshAgent, Rigidbody, Respawner.
        /// The "phase" parameter tells us WHEN in the frame the move happened, which
        /// narrows down the culprit (physics vs script vs callback).
        /// </summary>
        private void LogMoverDiagnostic(string phase, Vector3 previousPos, Vector3 currentPos)
        {
            Vector3 delta = currentPos - previousPos;
            float distance = delta.magnitude;

            // Check state of every known mover
            var locomotion = GetComponent<CharacterLocomotion>();
            var navAgent = GetComponent<NavMeshAgent>();
            var rb = GetComponent<Rigidbody>();
            var respawner = GetComponent<Opsive.UltimateCharacterController.Traits.Respawner>();

            // Check for any ENABLED Opsive components (should all be disabled)
            string enabledOpsive = "";
            var allComps = GetComponents<MonoBehaviour>();
            foreach (var comp in allComps)
            {
                if (comp == null || comp == this) continue;
                if (!comp.enabled) continue;
                string ns = comp.GetType().Namespace ?? "";
                if (ns.Contains("Opsive"))
                {
                    enabledOpsive += $"\n    - {comp.GetType().Name} (ENABLED!)";
                }
            }
            // Also check children (must check comp.enabled - GetComponentsInChildren returns
            // all components regardless of enabled state on active GameObjects)
            var childComps = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var comp in childComps)
            {
                if (comp == null || comp.gameObject == gameObject) continue;
                if (!comp.enabled) continue; // Only report actually ENABLED components
                string ns = comp.GetType().Namespace ?? "";
                if (ns.Contains("Opsive"))
                {
                    enabledOpsive += $"\n    - {comp.GetType().Name} on '{comp.gameObject.name}' (ENABLED!)";
                }
            }
            if (string.IsNullOrEmpty(enabledOpsive)) enabledOpsive = "\n    (none - good)";

            // AUTO-FIX: If we found enabled Opsive components, disable them immediately.
            // This prevents them from moving the enemy on subsequent frames.
            if (!string.IsNullOrEmpty(enabledOpsive) && enabledOpsive != "\n    (none - good)")
            {
                int swept = SweepDisableOpsiveComponents();
                if (swept > 0)
                {
                    enabledOpsive += $"\n    >>> AUTO-DISABLED {swept} component(s)";
                }
            }

            // Also check CharacterController (Unity built-in, not a MonoBehaviour, inherits Collider)
            var charController = GetComponent<CharacterController>();

            // Check Animator state (root motion is the most likely remaining mover)
            var animator = GetComponentInChildren<Animator>();
            string animInfo = "null";
            if (animator != null)
            {
                animInfo = $"enabled={animator.enabled}, applyRootMotion={animator.applyRootMotion}, " +
                    $"updateMode={animator.updateMode}, hasRootMotion={animator.hasRootMotion}";
            }

            // Check for non-Opsive enabled MonoBehaviours that could move the enemy
            // (Photon components, custom scripts, etc.)
            string otherEnabled = "";
            foreach (var comp in allComps)
            {
                if (comp == null || comp == this) continue;
                if (comp is EnemyAIController || comp is SentinelAIController) continue;
                if (!comp.enabled) continue;
                string ns = comp.GetType().Namespace ?? "";
                if (!ns.Contains("Opsive")) // Already reported Opsive ones above
                {
                    otherEnabled += $"\n    - {comp.GetType().FullName}";
                }
            }
            if (string.IsNullOrEmpty(otherEnabled)) otherEnabled = "\n    (none)";

            string log = $"[NetworkEnemySync] ===== MOVER DIAGNOSTIC on '{gameObject.name}' =====\n" +
                $"  WHEN: {phase}\n" +
                $"  MOVED: {distance:F3}m | Delta: ({delta.x:F3}, {delta.y:F3}, {delta.z:F3})\n" +
                $"  FROM: {previousPos} → TO: {currentPos}\n" +
                $"  NETWORK TARGET: {_networkPosition}\n" +
                $"  TIME: {Time.time:F2}s (spawn was {Time.time - _spawnTime:F1}s ago)\n" +
                $"  --- Component States ---\n" +
                $"  CharacterLocomotion: {(locomotion != null ? $"exists, enabled={locomotion.enabled}, SimIndex={locomotion.SimulationIndex}" : "NULL (destroyed)")}\n" +
                $"  NavMeshAgent: {(navAgent != null ? $"exists, enabled={navAgent.enabled}" + (navAgent.enabled ? $", hasPath={navAgent.hasPath}, velocity={navAgent.velocity}" : "") : "NULL (destroyed)")}\n" +
                $"  Rigidbody: {(rb != null ? $"exists, kinematic={rb.isKinematic}" + (!rb.isKinematic ? $", velocity={rb.linearVelocity}" : "") : "NULL (destroyed)")}\n" +
                $"  CharacterController: {(charController != null ? $"EXISTS enabled={charController.enabled}" : "null")}\n" +
                $"  Respawner: {(respawner != null ? "STILL EXISTS (should be destroyed!)" : "destroyed (good)")}\n" +
                $"  Animator: {animInfo}\n" +
                $"  Enabled Opsive components: {enabledOpsive}\n" +
                $"  Enabled non-Opsive scripts: {otherEnabled}\n" +
                $"  ===========================================";

            Debug.LogError(log); // Use LogError so it's hard to miss in the console
        }

        /// <summary>
        /// CRITICAL: Forces grounded state AFTER all other updates in the frame.
        ///
        /// WHY THIS IS NEEDED:
        /// Even though we disabled PunCharacterAnimatorMonitor, Photon still calls
        /// OnPhotonSerializeView() on disabled components. Inside that method,
        /// PunCharacterAnimatorMonitor directly calls m_AnimatorMonitor.SetHeightParameter()
        /// which sets the Height on the Animator. If the master client sends a non-zero
        /// Height value (e.g. during enemy movement), it overrides our Height=0 setting
        /// from Update(), causing the falling animation to play.
        ///
        /// LateUpdate runs AFTER all Update calls and after Photon's serialization
        /// callbacks, so our Height=0 gets the final word every frame.
        /// </summary>
        private void LateUpdate()
        {
            if (!_isNonMasterClient) return;

            // Force grounded state AND idle ability - prevents falling/floating animation
            // no matter what other components might have set during this frame.
            if (_animator != null)
            {
                _animator.SetFloat(HASH_HEIGHT, 0f);
                _animator.SetInteger(HASH_ABILITY_INDEX, 0);
            }

            // SAFETY NET: Force all renderers to stay visible on non-master clients.
            // Various Opsive systems (item equip, ability callbacks, PUN RPCs delivered
            // to disabled components) can hide renderers as a side effect. Rather than
            // chasing every possible source, we brute-force override visibility here
            // in LateUpdate, which runs after everything else has had its say.
            if (!_isDead && _cachedRenderers != null)
            {
                for (int i = 0; i < _cachedRenderers.Length; i++)
                {
                    if (_cachedRenderers[i] != null && !_cachedRenderers[i].enabled)
                    {
                        _cachedRenderers[i].enabled = true;
                    }
                }
            }

        }

        /// <summary>
        /// Drives the enemy Animator directly on non-master clients.
        /// Computes movement speed from position delta and sets the Opsive animator
        /// parameters so the correct walk/run/idle animations play.
        /// Also forces Height=0 (grounded) to prevent the falling animation.
        /// </summary>
        private void UpdateAnimatorOnNonMaster()
        {
            if (_animator == null) return;

            // Compute movement speed from position delta
            // Use only XZ plane (ignore vertical movement)
            Vector3 currentPos = transform.position;
            Vector3 delta = currentPos - _previousPosition;
            delta.y = 0f;
            float rawSpeed = delta.magnitude / Time.deltaTime;
            _previousPosition = currentPos;

            // Smooth the speed to avoid jitter (lerp towards target speed)
            _currentSpeed = Mathf.Lerp(_currentSpeed, rawSpeed, Time.deltaTime * 8f);

            // Clamp tiny values to zero to prevent micro-twitching animations
            if (_currentSpeed < 0.05f)
            {
                _currentSpeed = 0f;
            }

            // Set Opsive's animator parameters directly:
            // Speed - overall movement speed (used for walk/run blend)
            _animator.SetFloat(HASH_SPEED, _currentSpeed);

            // Height - 0 means grounded (prevents falling animation)
            _animator.SetFloat(HASH_HEIGHT, 0f);

            // AbilityIndex - 0 means no ability active (idle/movement).
            // THIS IS THE KEY FIX for the floating/falling animation.
            // AbilityIndex=2 is Opsive's "Fall" ability which plays the floating anim.
            // On non-master clients, Opsive's locomotion is disabled so AbilityIndex can
            // get stuck at 2 (Fall). We force it to 0 every frame.
            _animator.SetInteger(HASH_ABILITY_INDEX, 0);

            // Moving - bool flag for whether the character is moving
            bool isMoving = _currentSpeed > 0.1f;
            _animator.SetBool(HASH_MOVING, isMoving);

            // ForwardMovement - normalized forward input (0 to 1 for forward movement)
            // Use 1.0 when moving (enemies always move forward toward their target)
            _animator.SetFloat(HASH_FORWARD_MOVEMENT, isMoving ? 1f : 0f);

            // HorizontalMovement - keep at 0 (enemies don't strafe)
            _animator.SetFloat(HASH_HORIZONTAL_MOVEMENT, 0f);
        }

        // Tracks whether we're currently inside ApplyDamageOnMaster to avoid
        // infinite loops (our damage triggers Opsive event, Opsive event triggers our damage)
        private bool _isApplyingDamage = false;

        /// <summary>
        /// Intercepts damage dealt by Opsive's built-in weapon system (e.g., the Vector).
        /// When Opsive's Health.Damage() fires, it modifies health directly and fires this event.
        /// We UNDO Opsive's health change and re-apply the damage through our own system
        /// so it syncs properly to all clients via OnPhotonSerializeView.
        ///
        /// Without this, Opsive weapon damage only applies on the master client and
        /// non-master clients never see the health change — making enemies appear invincible.
        /// </summary>
        private void OnOpsiveDamageIntercepted(float amount, Vector3 position, Vector3 force, GameObject attacker, Collider hitCollider)
        {
            // If WE triggered this event (from ApplyDamageOnMaster), ignore it to avoid infinite loop
            if (_isApplyingDamage) return;
            if (_isDead) return;

            if (_debugMode)
            {
                Debug.Log($"[NetworkEnemySync] Intercepted Opsive damage on '{gameObject.name}': {amount} from {(attacker != null ? attacker.name : "null")}");
            }

            // Opsive already subtracted 'amount' from health. Undo that so we don't double-apply.
            if (_healthAttribute != null)
            {
                _healthAttribute.Value += amount;
            }

            // Now apply the damage through our network-synced system
            Vector3 hitDirection = force.normalized;
            ApplyDamageOnMaster(amount, position, hitDirection, attacker);
        }

        /// <summary>
        /// Called by damage scripts when this enemy is hit by any player.
        /// Routes damage to the Master Client for authoritative application.
        /// </summary>
        /// <param name="amount">Damage amount</param>
        /// <param name="hitPosition">World position of the hit</param>
        /// <param name="hitDirection">Direction of the hit (for blood effects)</param>
        /// <param name="attacker">The GameObject that dealt the damage (can be null)</param>
        public void RequestDamage(float amount, Vector3 hitPosition, Vector3 hitDirection, GameObject attacker)
        {
            if (_isDead) return;

            if (PhotonNetwork.IsMasterClient)
            {
                // We ARE the master client - apply damage directly
                ApplyDamageOnMaster(amount, hitPosition, hitDirection, attacker);
            }
            else
            {
                // Send damage request to master client via RPC
                // Note: GameObject can't be sent via RPC, so we only send the data we can
                photonView.RPC(nameof(RPC_RequestDamage), RpcTarget.MasterClient, amount, hitPosition, hitDirection);

                if (_debugMode)
                {
                    Debug.Log($"[NetworkEnemySync] Sent damage RPC to master: {amount} damage on '{gameObject.name}'");
                }
            }
        }

        /// <summary>
        /// RPC received by Master Client when a non-master player damages this enemy
        /// </summary>
        [PunRPC]
        private void RPC_RequestDamage(float amount, Vector3 hitPosition, Vector3 hitDirection, PhotonMessageInfo info)
        {
            // Only master client should process this
            if (!PhotonNetwork.IsMasterClient) return;

            if (_debugMode)
            {
                Debug.Log($"[NetworkEnemySync] Master received damage RPC from {info.Sender.NickName}: {amount} on '{gameObject.name}'");
            }

            ApplyDamageOnMaster(amount, hitPosition, hitDirection, null);
        }

        /// <summary>
        /// Applies damage on the Master Client (authoritative).
        /// Health changes are synced to all clients via OnPhotonSerializeView.
        ///
        /// IMPORTANT: We modify the health attribute DIRECTLY instead of going through
        /// Opsive's Health.Damage() method. This is because Health.Damage() triggers
        /// PunHealthMonitor which sends its own damage/death RPCs to all clients.
        /// Those RPCs conflict with our NetworkEnemySync health sync, causing:
        ///   - Double damage application on non-master clients
        ///   - Opsive's death system triggering in parallel with ours (renderer hiding,
        ///     ragdoll, etc.) which causes the "jittering invisibility" bug
        ///   - Some enemies becoming un-damageable due to inconsistent death state
        ///
        /// By modifying the attribute directly, PunHealthMonitor never fires, and our
        /// OnPhotonSerializeView is the ONLY path for syncing health to other clients.
        /// </summary>
        private void ApplyDamageOnMaster(float amount, Vector3 hitPosition, Vector3 hitDirection, GameObject attacker)
        {
            if (_isDead) return;

            // Safety: if _healthAttribute wasn't cached yet (damage arrived before Start),
            // try to grab it now so the damage isn't silently dropped
            if (_healthAttribute == null)
            {
                _attributeManager = GetComponent<Opsive.UltimateCharacterController.Traits.AttributeManager>();
                if (_attributeManager != null)
                {
                    _healthAttribute = _attributeManager.GetAttribute("Health");
                }

                if (_healthAttribute == null)
                {
                    Debug.LogWarning($"[NetworkEnemySync] Cannot apply damage to '{gameObject.name}' — health attribute not found!");
                    return;
                }
            }

            // Set flag so OnOpsiveDamageIntercepted knows to ignore the event
            // (modifying the attribute fires Opsive's "OnHealthDamage" which we're listening to)
            _isApplyingDamage = true;

            // Modify health attribute directly (bypasses Opsive Health + PunHealthMonitor)
            _healthAttribute.Value -= amount;

            // Clear the flag now that the attribute change is done
            _isApplyingDamage = false;

            // NOTE: We do NOT fire "OnHealthDamage" Opsive event here!
            // PunHealthMonitor (before it's fully destroyed) listens for this event and
            // would send OnDamageRPC to all clients, causing invisibility jitter.
            // EnemyAIController.OnHealthDamage (blood effects) is still registered for
            // this event, so we call it directly on our cached _aiController instead.

            if (_debugMode)
            {
                float currentHealth = _healthAttribute != null ? _healthAttribute.Value : 0f;
                Debug.Log($"[NetworkEnemySync] Damage applied on master: {amount} to '{gameObject.name}', health now: {currentHealth}");
            }

            // Check for death
            if (_healthAttribute != null && _healthAttribute.Value <= 0f && !_isDead)
            {
                _isDead = true;

                if (_debugMode)
                {
                    Debug.Log($"[NetworkEnemySync] Enemy '{gameObject.name}' died on master - syncing death to all clients");
                }
            }
        }

        /// <summary>
        /// Photon serialization - syncs state from Master Client to all other clients.
        /// Called automatically by Photon at the configured send rate.
        /// </summary>
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // MASTER CLIENT: Send current state to all clients
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
                stream.SendNext(_healthAttribute != null ? _healthAttribute.Value : 0f);
                stream.SendNext(_isDead);
            }
            else
            {
                // NON-MASTER CLIENT: Receive state from master
                _networkPosition = (Vector3)stream.ReceiveNext();
                _networkRotation = (Quaternion)stream.ReceiveNext();
                float networkHealth = (float)stream.ReceiveNext();
                bool networkIsDead = (bool)stream.ReceiveNext();

                // Log ONCE when first sync is received - if this never fires,
                // Photon serialization is broken (observed list empty or misconfigured)
                if (!_hasReceivedFirstSync)
                {
                    _hasReceivedFirstSync = true;
                    Debug.Log($"[NetworkEnemySync] FIRST SYNC received for '{gameObject.name}': " +
                        $"pos={_networkPosition}, health={networkHealth}, dead={networkIsDead}");
                }

                // Apply health value from master
                if (_healthAttribute != null)
                {
                    _healthAttribute.Value = networkHealth;
                }

                // Handle death sync - if master says we're dead, trigger death locally
                if (networkIsDead && !_isDead)
                {
                    _isDead = true;

                    if (_debugMode)
                    {
                        Debug.Log($"[NetworkEnemySync] Death synced from master for '{gameObject.name}'");
                    }

                    // Trigger death on the AI controller (works even if component is "disabled"
                    // since we're calling the method directly, not relying on Update).
                    // Check both controller types — enemy will have one or the other.
                    if (_aiController != null)
                    {
                        _aiController.OnDeath();
                    }
                    else if (_sentinelAIController != null)
                    {
                        _sentinelAIController.OnDeath();
                    }
                }
            }
        }

        /// <summary>
        /// Cleanup when this enemy is destroyed.
        /// Unregisters the Opsive damage event listener to prevent stale callbacks.
        /// If we don't do this, a destroyed enemy could still receive "OnHealthDamage"
        /// events from Opsive's event system, causing NullReferenceExceptions.
        /// </summary>
        private void OnDestroy()
        {
            // Unregister the Opsive damage interceptor (only master client registered it)
            if (!_isNonMasterClient)
            {
                Opsive.Shared.Events.EventHandler.UnregisterEvent<float, Vector3, Vector3, GameObject, Collider>(
                    gameObject, "OnHealthDamage", OnOpsiveDamageIntercepted);
            }
        }

        /// <summary>
        /// Called when master client wants to destroy this enemy (after death + delay).
        /// Uses PhotonNetwork.Destroy so it's removed on all clients.
        /// </summary>
        public void NetworkDestroy()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}
