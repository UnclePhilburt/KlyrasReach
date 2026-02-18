/*
 * PUN Input Controller for Klyra's Reach
 *
 * PURPOSE:
 * Disables input components for remote players and enables custom remote animator
 * This is needed because Unity 6.3 WebGL has issues with Opsive's input system
 *
 * IMPORTANT: This only disables INPUT, not the character controller or animator!
 * Remote players still need their UltimateCharacterLocomotion enabled to receive
 * networked position/rotation updates from PunCharacterTransformMonitor.
 */

using UnityEngine;
using Photon.Pun;
using Opsive.Shared.Events;
using Opsive.Shared.Input; // For PlayerInputProxy
using Opsive.UltimateCharacterController.Character;
using KlyrasReach.UI;

namespace KlyrasReach.Multiplayer
{
    /// <summary>
    /// Disables input for remote players using Opsive's event system
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class PunInputController : MonoBehaviour
    {
        private PhotonView _photonView;
        private UltimateCharacterLocomotion _locomotion;
        private AnimatorMonitor _animatorMonitor;

        private void Awake()
        {
            _photonView = GetComponent<PhotonView>();
            _locomotion = GetComponent<UltimateCharacterLocomotion>();
            _animatorMonitor = GetComponentInChildren<AnimatorMonitor>();
        }

        private void Start()
        {
            if (!_photonView.IsMine)
            {
                SetupRemotePlayer();
            }
            else
            {
                SetupLocalPlayer();
            }
        }

        private void SetupLocalPlayer()
        {
            Debug.Log($"[PunInputController] ========================================");
            Debug.Log($"[PunInputController] Setting up LOCAL player: {gameObject.name}");

            // Ensure player has correct tag
            if (gameObject.tag != "Player")
            {
                gameObject.tag = "Player";
                Debug.Log($"[PunInputController] Set tag to 'Player' for local player");
            }

            Debug.Log($"[PunInputController] UltimateCharacterLocomotion enabled: {(_locomotion != null ? _locomotion.enabled.ToString() : "NOT FOUND")}");
            Debug.Log($"[PunInputController] AnimatorMonitor found: {(_animatorMonitor != null ? "YES" : "NO")}");

            if (_locomotion != null)
            {
                Debug.Log($"[PunInputController] Locomotion TimeScale: {_locomotion.TimeScale}");
            }

            // Apply saved look sensitivity to Opsive's PlayerInput via the proxy.
            // The actual PlayerInput gets reparented to a different GameObject by
            // Opsive on Awake, so we go through the PlayerInputProxy which stays
            // on the player character and delegates to the real PlayerInput.
            var inputProxy = GetComponent<PlayerInputProxy>();
            if (inputProxy != null)
            {
                float savedSensitivity = PlayerPrefs.GetFloat("LookSensitivity", 0.5f);
                float multiplier = LookSensitivitySettings.NormalizedToMultiplier(savedSensitivity);
                inputProxy.LookSensitivityMultiplier = multiplier;
                Debug.Log($"[PunInputController] Applied saved look sensitivity: {savedSensitivity:F2} → {multiplier:F2}x");
            }

            Debug.Log($"[PunInputController] LOCAL player setup complete - Opsive system will handle movement and animation");
            Debug.Log($"[PunInputController] ========================================");
        }

        private void SetupRemotePlayer()
        {
            Debug.Log($"[PunInputController] ========================================");
            Debug.Log($"[PunInputController] Setting up REMOTE player: {gameObject.name}");

            // Ensure player has correct tag
            if (gameObject.tag != "Player")
            {
                gameObject.tag = "Player";
                Debug.Log($"[PunInputController] Set tag to 'Player' for remote player");
            }

            // DISABLE INPUT: Use Opsive's event system to disable gameplay input for remote players
            // This prevents the Handler from trying to read from PlayerInput
            // while keeping all components enabled for proper animation sync
            EventHandler.ExecuteEvent(gameObject, "OnEnableGameplayInput", false);

            Debug.Log($"[PunInputController] Disabled gameplay input via event system");

            // DISABLE LOCOMOTION AND INPUT COMPONENTS
            // Find and disable components by iterating through all MonoBehaviours
            var allComponents = GetComponents<MonoBehaviour>();
            foreach (var component in allComponents)
            {
                var typeName = component.GetType().Name;

                // Disable PlayerInputProxy to prevent remote players from reading keyboard/mouse
                if (typeName == "PlayerInputProxy")
                {
                    component.enabled = false;
                    Debug.Log($"[PunInputController] Disabled PlayerInputProxy component");
                }

                // Disable UltimateCharacterLocomotionHandler to prevent WebGL errors
                if (typeName == "UltimateCharacterLocomotionHandler")
                {
                    component.enabled = false;
                    Debug.Log($"[PunInputController] Disabled UltimateCharacterLocomotionHandler to prevent WebGL errors");
                }

            }

            // IMPORTANT: Do NOT disable UltimateCharacterLocomotion!
            // It needs to stay enabled to receive networked updates
            if (_locomotion != null)
            {
                Debug.Log($"[PunInputController] UltimateCharacterLocomotion is: {(_locomotion.enabled ? "ENABLED (correct)" : "DISABLED (ERROR!)")}");
                if (!_locomotion.enabled)
                {
                    Debug.LogError($"[PunInputController] ERROR: UltimateCharacterLocomotion is disabled! Remote player won't move!");
                }
            }
            else
            {
                Debug.LogWarning($"[PunInputController] WARNING: No UltimateCharacterLocomotion found!");
            }

            // Check AnimatorMonitor
            if (_animatorMonitor != null)
            {
                Debug.Log($"[PunInputController] AnimatorMonitor enabled: {_animatorMonitor.enabled}");
                Debug.Log($"[PunInputController] AnimatorMonitor has animator: {(_animatorMonitor.GetComponent<Animator>() != null)}");
            }
            else
            {
                Debug.LogWarning($"[PunInputController] WARNING: No AnimatorMonitor found in children!");
            }

            Debug.Log($"[PunInputController] Remote player setup complete!");
            Debug.Log($"[PunInputController] ========================================");
        }
    }
}
