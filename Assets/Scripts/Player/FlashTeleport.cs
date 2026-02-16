/*
 * Flash Teleport Ability for Klyra's Reach
 *
 * PURPOSE:
 * Sci-fi teleport - character disappears and reappears with cool effects
 *
 * HOW TO USE:
 * 1. Add this ability to your player's UltimateCharacterLocomotion component
 * 2. Set a keybind (e.g., Left Shift or Space)
 * 3. Assign teleport effects (FX_Portal_Sphere_01 or FX_Portal_Round_01)
 * 4. Player will vanish and teleport forward!
 */

using UnityEngine;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using System.Collections;
using System;
using Photon.Pun;

namespace KlyrasReach.Player
{
    /// <summary>
    /// True teleport ability - character disappears and reappears
    /// </summary>
    [Serializable]
    public class FlashTeleport : Ability
    {
        [Header("Teleport Settings")]
        [Tooltip("How far to teleport forward")]
        [SerializeField] private float _teleportDistance = 8f;

        [Tooltip("How long teleport animation takes (seconds)")]
        [SerializeField] private float _teleportDuration = 0.3f;

        [Tooltip("Cooldown between teleports (seconds)")]
        [SerializeField] private float _cooldown = 2f;

        [Tooltip("Make player invulnerable during teleport?")]
        [SerializeField] private bool _invulnerableDuringTeleport = true;

        [Header("Visual Effects")]
        [Tooltip("Teleport out effect (FX_Portal_Sphere_01)")]
        [SerializeField] private GameObject _teleportOutEffectPrefab;

        [Tooltip("Teleport in effect (FX_Portal_Sphere_01)")]
        [SerializeField] private GameObject _teleportInEffectPrefab;

        [Tooltip("Extra sparkle effect (optional - FX_Sparkles_Small_01)")]
        [SerializeField] private GameObject _extraSparkleEffect;

        // Private variables
        private Vector3 _teleportTargetPosition;
        private Quaternion _teleportTargetRotation;
        private bool _isTeleporting;
        private float _lastTeleportTime = -999f;
        private int _originalLayer;
        private Renderer[] _characterRenderers;
        private PhotonView _photonView;

        /// <summary>
        /// Initialize - get PhotonView reference
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            _photonView = m_GameObject.GetComponent<PhotonView>();
        }

        /// <summary>
        /// Can the ability start?
        /// </summary>
        public override bool CanStartAbility()
        {
            // IMPORTANT: Only allow local player to teleport
            if (_photonView != null && !_photonView.IsMine)
            {
                Debug.Log("[FlashTeleport] Not local player - cannot teleport");
                return false;
            }

            // Check cooldown
            if (Time.time < _lastTeleportTime + _cooldown)
            {
                Debug.Log($"[FlashTeleport] On cooldown: {(_lastTeleportTime + _cooldown - Time.time):F2}s remaining");
                return false;
            }

            // Can't teleport while already teleporting
            if (_isTeleporting)
            {
                Debug.Log("[FlashTeleport] Already teleporting");
                return false;
            }

            // Must be grounded
            if (!m_CharacterLocomotion.Grounded)
            {
                Debug.Log("[FlashTeleport] Not grounded");
                return false;
            }

            // Can't teleport while piloting a ship (character is invisible when in ship)
            Renderer[] renderers = m_GameObject.GetComponentsInChildren<Renderer>();
            bool isVisible = false;
            foreach (Renderer renderer in renderers)
            {
                if (renderer.enabled)
                {
                    isVisible = true;
                    break;
                }
            }

            if (!isVisible)
            {
                Debug.Log("[FlashTeleport] Cannot teleport while in ship (character invisible)");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Start the teleport
        /// </summary>
        protected override void AbilityStarted()
        {
            base.AbilityStarted();

            _isTeleporting = true;
            _lastTeleportTime = Time.time;

            // Get camera forward direction (flattened to horizontal plane)
            Transform cameraTransform = m_CharacterLocomotion.LookSource.GameObject.transform;
            Vector3 cameraForward = cameraTransform.forward;

            // Flatten to horizontal plane (remove Y component)
            Vector3 teleportDirection = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;

            // Calculate target position and rotation
            Vector3 startPosition = m_Transform.position;
            _teleportTargetPosition = startPosition + teleportDirection * _teleportDistance;
            _teleportTargetRotation = Quaternion.LookRotation(teleportDirection);

            Debug.Log($"[FlashTeleport] START POS: {startPosition}");
            Debug.Log($"[FlashTeleport] Camera facing: {cameraForward}, Teleport direction: {teleportDirection}");
            Debug.Log($"[FlashTeleport] TARGET POS (before wall check): {_teleportTargetPosition}, Distance: {_teleportDistance}m");

            // No wall collision - teleport through everything!
            Debug.Log($"[FlashTeleport] Full teleport distance: {_teleportDistance}m, ignoring obstacles");

            // Make invulnerable
            if (_invulnerableDuringTeleport)
            {
                _originalLayer = m_GameObject.layer;
                m_GameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            }

            // Start the teleport coroutine
            m_CharacterLocomotion.StartCoroutine(TeleportSequence(startPosition));

            Debug.Log($"[FlashTeleport] Teleporting from {startPosition} to {_teleportTargetPosition}");

            // Sync teleport effects to other players via RPC
            if (_photonView != null)
            {
                _photonView.RPC("RPC_TeleportEffects", RpcTarget.Others, startPosition, _teleportTargetPosition);
            }
        }

        /// <summary>
        /// Execute the teleport sequence
        /// </summary>
        private IEnumerator TeleportSequence(Vector3 startPosition)
        {
            // 1. Spawn teleport OUT effect (raised up 1 meter)
            if (_teleportOutEffectPrefab != null)
            {
                Vector3 raisedStartPos = startPosition + Vector3.up * 1f;
                GameObject outEffect = UnityEngine.Object.Instantiate(_teleportOutEffectPrefab, raisedStartPos, Quaternion.identity);
                UnityEngine.Object.Destroy(outEffect, 3f);
            }

            // 2. Make character invisible
            _characterRenderers = m_GameObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in _characterRenderers)
            {
                renderer.enabled = false;
            }

            // 3. Wait half the duration (teleport happens in middle)
            yield return new WaitForSeconds(_teleportDuration * 0.5f);

            // 4. Actually teleport the character and rotate to face teleport direction
            Debug.Log($"[FlashTeleport] Setting position to {_teleportTargetPosition} with rotation {_teleportTargetRotation.eulerAngles}");
            m_CharacterLocomotion.SetPositionAndRotation(_teleportTargetPosition, _teleportTargetRotation, true);

            // 5. Spawn teleport IN effect at new position (raised up 1 meter)
            if (_teleportInEffectPrefab != null)
            {
                Vector3 raisedTargetPos = _teleportTargetPosition + Vector3.up * 1f;
                GameObject inEffect = UnityEngine.Object.Instantiate(_teleportInEffectPrefab, raisedTargetPos, Quaternion.identity);
                UnityEngine.Object.Destroy(inEffect, 3f);
            }

            // 6. Extra sparkles for coolness (raised up 1 meter)
            if (_extraSparkleEffect != null)
            {
                Vector3 raisedTargetPos = _teleportTargetPosition + Vector3.up * 1f;
                GameObject sparkles = UnityEngine.Object.Instantiate(_extraSparkleEffect, raisedTargetPos, Quaternion.identity);
                UnityEngine.Object.Destroy(sparkles, 2f);
            }

            // 7. Wait other half of duration
            yield return new WaitForSeconds(_teleportDuration * 0.5f);

            // 8. Make character visible again
            foreach (Renderer renderer in _characterRenderers)
            {
                renderer.enabled = true;
            }

            // 9. Stop the ability
            Debug.Log("[FlashTeleport] Coroutine complete, calling StopAbility");
            StopAbility();
        }

        /// <summary>
        /// No update needed - coroutine handles everything
        /// </summary>
        public override void Update()
        {
            // Teleport handled entirely by coroutine
        }

        /// <summary>
        /// Stop the teleport
        /// </summary>
        protected override void AbilityStopped(bool force)
        {
            base.AbilityStopped(force);

            Debug.Log($"[FlashTeleport] AbilityStopped called, force={force}, setting _isTeleporting=false");
            _isTeleporting = false;

            // Restore vulnerability (in case teleport was interrupted)
            if (_invulnerableDuringTeleport)
            {
                m_GameObject.layer = _originalLayer;
            }

            // Make sure character is visible (in case interrupted mid-teleport)
            if (_characterRenderers != null)
            {
                foreach (Renderer renderer in _characterRenderers)
                {
                    renderer.enabled = true;
                }
            }

            Debug.Log($"[FlashTeleport] Teleport complete at {m_Transform.position}, _isTeleporting={_isTeleporting}");
        }

        /// <summary>
        /// Get cooldown percentage (for UI)
        /// </summary>
        public float GetCooldownPercent()
        {
            float timeSinceLastTeleport = Time.time - _lastTeleportTime;
            return Mathf.Clamp01(timeSinceLastTeleport / _cooldown);
        }

        /// <summary>
        /// Is ability on cooldown?
        /// </summary>
        public bool IsOnCooldown()
        {
            return Time.time < _lastTeleportTime + _cooldown;
        }

        /// <summary>
        /// RPC to show teleport effects on remote clients
        /// </summary>
        [PunRPC]
        private void RPC_TeleportEffects(Vector3 startPos, Vector3 endPos)
        {
            Debug.Log($"[FlashTeleport] RPC_TeleportEffects received - spawning effects for remote player");

            // Spawn teleport OUT effect at start position (raised up 1 meter)
            if (_teleportOutEffectPrefab != null)
            {
                Vector3 raisedStartPos = startPos + Vector3.up * 1f;
                GameObject outEffect = UnityEngine.Object.Instantiate(_teleportOutEffectPrefab, raisedStartPos, Quaternion.identity);
                UnityEngine.Object.Destroy(outEffect, 3f);
            }

            // Spawn teleport IN effect at end position after half duration (raised up 1 meter)
            m_CharacterLocomotion.StartCoroutine(SpawnTeleportInEffectDelayed(endPos, _teleportDuration * 0.5f));
        }

        /// <summary>
        /// Spawn teleport IN effect after a delay
        /// </summary>
        private IEnumerator SpawnTeleportInEffectDelayed(Vector3 position, float delay)
        {
            yield return new WaitForSeconds(delay);

            // Spawn teleport IN effect (raised up 1 meter)
            if (_teleportInEffectPrefab != null)
            {
                Vector3 raisedPos = position + Vector3.up * 1f;
                GameObject inEffect = UnityEngine.Object.Instantiate(_teleportInEffectPrefab, raisedPos, Quaternion.identity);
                UnityEngine.Object.Destroy(inEffect, 3f);
            }

            // Extra sparkles (raised up 1 meter)
            if (_extraSparkleEffect != null)
            {
                Vector3 raisedPos = position + Vector3.up * 1f;
                GameObject sparkles = UnityEngine.Object.Instantiate(_extraSparkleEffect, raisedPos, Quaternion.identity);
                UnityEngine.Object.Destroy(sparkles, 2f);
            }
        }
    }
}
