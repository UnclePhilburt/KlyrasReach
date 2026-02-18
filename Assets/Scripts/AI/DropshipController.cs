/*
 * Dropship Controller for Klyra's Reach
 *
 * PURPOSE:
 * Controls the cinematic dropship that flies in, hovers over a drop zone,
 * deploys enemy reinforcements one at a time, then flies away. This replaces
 * the immersion-breaking "pop into existence" reinforcement spawns.
 *
 * FLIGHT SEQUENCE (runs as coroutine on master client):
 * 1. APPROACH (~3-4s) — Fly from 200m away, descend to hover height (25m).
 *    Uses SmoothStep easing so the ship decelerates smoothly on arrival.
 * 2. HOVER + DROP — Gentle vertical bob while spawning enemies one at a time
 *    (0.4s interval). Each enemy is spawned via RavagerSpawner callback.
 * 3. DEPART (~3s) — Accelerate away in the opposite direction, climbing out.
 *    Banks slightly for visual polish.
 * 4. CLEANUP — PhotonNetwork.Destroy() once out of range.
 *
 * The dropship is invincible — purely cinematic, no colliders, no health.
 *
 * NETWORK SYNC:
 * Master client runs the flight coroutine and sends position/rotation to all
 * clients via IPunObservable. Non-master clients interpolate smoothly with lerp.
 *
 * HOW TO USE:
 * 1. Attach this script to the Dropship prefab (SM_Ship_Transport_01)
 * 2. Add a PhotonView component to the prefab root
 * 3. Add this script to the PhotonView's Observed Components list
 * 4. Remove all colliders from the ship (purely visual)
 * 5. RavagerSpawner spawns the dropship and calls BeginDropMission()
 */

using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

namespace KlyrasReach.AI
{
    /// <summary>
    /// Network-synced flight controller for the enemy dropship.
    /// Flies in from a random direction, hovers, deploys enemies, then flies away.
    /// Attached to the dropship prefab with a PhotonView.
    /// </summary>
    public class DropshipController : MonoBehaviourPun, IPunObservable
    {
        // =====================================================
        //  FLIGHT SETTINGS
        // =====================================================

        [Header("Approach")]
        [Tooltip("How far away the ship starts its approach (meters)")]
        [SerializeField] private float _approachDistance = 400f;

        [Tooltip("Altitude above the drop zone during hover (meters)")]
        [SerializeField] private float _hoverHeight = 80f;

        [Tooltip("Speed during approach phase (meters/second)")]
        [SerializeField] private float _approachSpeed = 50f;

        [Header("Departure")]
        [Tooltip("Speed during departure phase — faster than approach for a dramatic exit")]
        [SerializeField] private float _departSpeed = 60f;

        [Tooltip("How much the ship tilts (banks) when turning/departing (degrees)")]
        [SerializeField] private float _bankAngle = 15f;

        [Header("Hover")]
        [Tooltip("Vertical bob amount while hovering (meters)")]
        [SerializeField] private float _hoverBobAmount = 0.3f;

        [Tooltip("How fast the hover bob oscillates")]
        [SerializeField] private float _hoverBobSpeed = 1.5f;

        [Header("Enemy Drop")]
        [Tooltip("Seconds between each enemy drop during hover phase")]
        [SerializeField] private float _dropInterval = 0.4f;

        [Tooltip("Where enemies appear relative to ship center (usually below)")]
        [SerializeField] private Vector3 _dropOffset = new Vector3(0f, -5f, 0f);

        [Header("Network Interpolation")]
        [Tooltip("How fast non-master clients interpolate to the network position")]
        [SerializeField] private float _positionLerpSpeed = 10f;

        [Tooltip("How fast non-master clients interpolate to the network rotation")]
        [SerializeField] private float _rotationLerpSpeed = 10f;

        [Header("Debug")]
        [SerializeField] private bool _debugMode = true;

        // =====================================================
        //  PRIVATE STATE
        // =====================================================

        // Network sync targets (received from master, used by non-master for interpolation)
        private Vector3 _networkPosition;
        private Quaternion _networkRotation;

        // Tracks whether the mission coroutine is running
        private bool _missionActive = false;

        // =====================================================
        //  PUBLIC API — Called by RavagerSpawner
        // =====================================================

        /// <summary>
        /// Start the full drop mission sequence. Called by RavagerSpawner after
        /// spawning the dropship via PhotonNetwork.InstantiateRoomObject.
        ///
        /// Only runs on the Master Client — non-master clients see the ship
        /// move via network position sync.
        /// </summary>
        /// <param name="dropZone">World position of the center of the drop zone</param>
        /// <param name="enemyCount">How many enemies to drop during hover phase</param>
        /// <param name="spawner">Reference to the RavagerSpawner for spawning enemies</param>
        /// <param name="enemyPrefabPath">Default Resources path to the enemy prefab</param>
        /// <param name="dropZoneRadius">Scatter radius for enemy landing positions</param>
        /// <param name="perEnemyPrefabPaths">Optional per-enemy prefab paths for mixed composition (Ravagers + Sentinels). If null, all enemies use enemyPrefabPath.</param>
        public void BeginDropMission(Vector3 dropZone, int enemyCount,
            RavagerSpawner spawner, string enemyPrefabPath, float dropZoneRadius,
            List<string> perEnemyPrefabPaths = null)
        {
            // Only the master client runs the flight logic
            if (!PhotonNetwork.IsMasterClient) return;

            if (_missionActive)
            {
                Debug.LogWarning("[DropshipController] Mission already in progress!");
                return;
            }

            _missionActive = true;

            if (_debugMode)
            {
                Debug.Log($"[DropshipController] Starting drop mission: {enemyCount} enemies " +
                    $"at drop zone {dropZone}, radius {dropZoneRadius}m");
            }

            // Start the full flight sequence as a coroutine
            StartCoroutine(DropMissionCoroutine(dropZone, enemyCount, spawner, enemyPrefabPath, dropZoneRadius, perEnemyPrefabPaths));
        }

        // =====================================================
        //  FLIGHT SEQUENCE COROUTINE
        // =====================================================

        /// <summary>
        /// Master-client-only coroutine that executes the full flight sequence:
        /// approach → hover + drop → depart → cleanup.
        /// </summary>
        private System.Collections.IEnumerator DropMissionCoroutine(
            Vector3 dropZone, int enemyCount, RavagerSpawner spawner,
            string enemyPrefabPath, float dropZoneRadius,
            List<string> perEnemyPrefabPaths = null)
        {
            // ==========================================
            // CALCULATE APPROACH AND DEPARTURE VECTORS
            // ==========================================

            // Pick a random horizontal direction to approach from
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 approachDirection = new Vector3(Mathf.Cos(randomAngle), 0f, Mathf.Sin(randomAngle));

            // Start position: far away at SAME altitude as hover (flat approach, no nosedive)
            // The ship flies in level, which looks correct for a massive transport ship
            Vector3 hoverPosition = dropZone + (Vector3.up * _hoverHeight);
            Vector3 startPosition = hoverPosition + (approachDirection * _approachDistance);

            // Depart position: opposite direction, far away and climbing out
            Vector3 departDirection = -approachDirection;
            Vector3 departPosition = hoverPosition + (departDirection * _approachDistance) + (Vector3.up * 120f);

            // Place the ship at the start position immediately
            transform.position = startPosition;

            // Face toward the hover point — flat approach so the nose stays level
            Vector3 toDropZone = (hoverPosition - startPosition).normalized;
            transform.rotation = Quaternion.LookRotation(toDropZone);

            if (_debugMode)
            {
                Debug.Log($"[DropshipController] Approaching from {startPosition} to {hoverPosition}");
            }

            // ==========================================
            // PHASE 1: APPROACH (fly in and decelerate)
            // ==========================================

            // Calculate approach time based on distance and speed
            float approachDist = Vector3.Distance(startPosition, hoverPosition);
            float approachDuration = approachDist / _approachSpeed;

            float elapsed = 0f;
            while (elapsed < approachDuration)
            {
                elapsed += Time.deltaTime;

                // SmoothStep easing: accelerates at start, decelerates at end
                // This makes the arrival look natural — the ship slows down as it reaches the hover point
                float t = Mathf.Clamp01(elapsed / approachDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // Interpolate position along the approach path
                transform.position = Vector3.Lerp(startPosition, hoverPosition, smoothT);

                // Face the HORIZONTAL direction of travel — keeps the nose level.
                // Using the full travel vector would angle the nose down on any descent,
                // which looks like a nosedive on a massive ship.
                Vector3 horizontalTravelDir = (hoverPosition - startPosition);
                horizontalTravelDir.y = 0f; // Strip vertical component to keep nose level
                horizontalTravelDir.Normalize();
                Quaternion targetRotation = Quaternion.LookRotation(horizontalTravelDir);

                // Add a subtle bank during approach (tilts into the turn)
                float bankAmount = _bankAngle * (1f - smoothT) * 0.5f; // Less bank as we slow down
                targetRotation *= Quaternion.Euler(0f, 0f, bankAmount);

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);

                yield return null;
            }

            // Snap to exact hover position
            transform.position = hoverPosition;

            if (_debugMode)
            {
                Debug.Log($"[DropshipController] Arrived at hover position. Deploying {enemyCount} enemies...");
            }

            // ==========================================
            // PHASE 2: HOVER + DROP ENEMIES
            // ==========================================

            // Face a consistent direction while hovering (toward where we came from looks dramatic)
            Quaternion hoverRotation = Quaternion.LookRotation(-approachDirection);

            for (int i = 0; i < enemyCount; i++)
            {
                // Gentle vertical bob while hovering using sine wave
                float bobOffset = Mathf.Sin(Time.time * _hoverBobSpeed) * _hoverBobAmount;
                transform.position = hoverPosition + new Vector3(0f, bobOffset, 0f);

                // Keep rotation steady with gentle settling
                transform.rotation = Quaternion.Slerp(transform.rotation, hoverRotation, Time.deltaTime * 3f);

                // Calculate world-space drop position (ship center + offset)
                Vector3 dropPosition = transform.position + transform.TransformDirection(_dropOffset);

                // Determine which prefab path to use for this enemy.
                // If per-enemy paths are provided (mixed composition), use the specific one.
                // Otherwise fall back to the default enemyPrefabPath.
                string thisEnemyPath = enemyPrefabPath;
                if (perEnemyPrefabPaths != null && i < perEnemyPrefabPaths.Count)
                {
                    thisEnemyPath = perEnemyPrefabPaths[i];
                }

                // Tell the spawner to spawn an enemy at the drop position
                // The spawner handles the falling logic and NavMesh placement
                if (spawner != null)
                {
                    spawner.SpawnEnemyFromDropship(dropPosition, dropZone, dropZoneRadius, thisEnemyPath);
                }

                if (_debugMode)
                {
                    Debug.Log($"[DropshipController] Dropped enemy {i + 1}/{enemyCount} at {dropPosition}");
                }

                // Wait between drops
                if (i < enemyCount - 1)
                {
                    yield return new WaitForSeconds(_dropInterval);
                }
            }

            // Brief pause after last drop before departing
            yield return new WaitForSeconds(0.5f);

            if (_debugMode)
            {
                Debug.Log($"[DropshipController] All enemies deployed. Departing...");
            }

            // ==========================================
            // PHASE 3: DEPART (accelerate away, climbing)
            // ==========================================

            Vector3 departStartPos = transform.position;
            float departDist = Vector3.Distance(departStartPos, departPosition);
            float departDuration = departDist / _departSpeed;

            elapsed = 0f;
            while (elapsed < departDuration)
            {
                elapsed += Time.deltaTime;

                // Ease-in: starts slow, accelerates (opposite of approach)
                float t = Mathf.Clamp01(elapsed / departDuration);
                float easeInT = t * t; // Quadratic ease-in — accelerates dramatically

                // Interpolate along departure path
                transform.position = Vector3.Lerp(departStartPos, departPosition, easeInT);

                // Face the HORIZONTAL departure direction — nose stays mostly level.
                // The ship climbs out but doesn't pitch up steeply like a rocket.
                // A slight nose-up is added gradually for a natural climb-out feel.
                Vector3 horizontalDepartDir = (departPosition - departStartPos);
                horizontalDepartDir.y = 0f;
                horizontalDepartDir.Normalize();
                Quaternion targetRot = Quaternion.LookRotation(horizontalDepartDir);

                // Gradual nose-up pitch as the ship climbs out (max ~10 degrees)
                float noseUpPitch = -10f * t; // Negative X = nose up in Unity
                targetRot *= Quaternion.Euler(noseUpPitch, 0f, 0f);

                // Bank into the departure turn for visual polish
                float bankT = Mathf.Clamp01(t * 3f); // Bank builds up quickly at the start
                float departBank = _bankAngle * (1f - bankT); // Then levels out
                targetRot *= Quaternion.Euler(0f, 0f, departBank);

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 4f);

                yield return null;
            }

            if (_debugMode)
            {
                Debug.Log($"[DropshipController] Departed. Destroying dropship.");
            }

            // ==========================================
            // PHASE 4: CLEANUP
            // ==========================================

            // Master client destroys the dropship on all clients
            _missionActive = false;
            PhotonNetwork.Destroy(gameObject);
        }

        // =====================================================
        //  NETWORK SYNC (non-master interpolation)
        // =====================================================

        /// <summary>
        /// Non-master clients interpolate position and rotation smoothly
        /// to match the master client's flight path.
        /// </summary>
        private void Update()
        {
            // Only non-master clients need to interpolate
            if (PhotonNetwork.IsMasterClient) return;
            if (!PhotonNetwork.IsConnected) return;

            // Smooth position interpolation
            transform.position = Vector3.Lerp(transform.position, _networkPosition, Time.deltaTime * _positionLerpSpeed);

            // Smooth rotation interpolation
            transform.rotation = Quaternion.Lerp(transform.rotation, _networkRotation, Time.deltaTime * _rotationLerpSpeed);
        }

        // =====================================================
        //  PHOTON SERIALIZATION (IPunObservable)
        // =====================================================

        /// <summary>
        /// Syncs position and rotation from master to all clients.
        /// Master sends current transform data, non-master clients store
        /// it as interpolation targets used in Update().
        /// </summary>
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // MASTER CLIENT: Send position and rotation
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
            }
            else
            {
                // NON-MASTER CLIENT: Receive and store as interpolation targets
                _networkPosition = (Vector3)stream.ReceiveNext();
                _networkRotation = (Quaternion)stream.ReceiveNext();
            }
        }
    }
}
