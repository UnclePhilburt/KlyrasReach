/*
 * PUN Look Source Setup for Klyra's Reach
 *
 * PURPOSE:
 * Automatically adds the correct look source to spawned players:
 * - Local player: Uses camera (handled by PunCameraLinker)
 * - Remote players: Uses LocalLookSource
 *
 * HOW TO USE:
 * Add this to your player prefab (SM_Chr_Psionic_01)
 */

using UnityEngine;
using Photon.Pun;
using Opsive.UltimateCharacterController.Character;

namespace KlyrasReach.Multiplayer
{
    /// <summary>
    /// Sets up the correct look source for PUN players
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(UltimateCharacterLocomotion))]
    public class PunLookSourceSetup : MonoBehaviour
    {
        private PhotonView _photonView;
        private UltimateCharacterLocomotion _locomotion;

        private bool _setupComplete = false;

        private void Start()
        {
            _photonView = GetComponent<PhotonView>();
            _locomotion = GetComponent<UltimateCharacterLocomotion>();
        }

        private void Update()
        {
            // Wait until ownership is properly set before running setup
            if (_setupComplete) return;
            if (_photonView.Owner == null) return;

            _setupComplete = true;

            if (!_photonView.IsMine)
            {
                SetupRemotePlayer();
            }
            else
            {
                Debug.Log("[PunLookSourceSetup] This is the LOCAL player - waiting for camera link");

                // Remove LocalLookSource if it exists on local player (shouldn't be there)
                var localLookSource = GetComponent<Opsive.UltimateCharacterController.Character.LocalLookSource>();
                if (localLookSource != null)
                {
                    Destroy(localLookSource);
                    Debug.Log("[PunLookSourceSetup] Removed LocalLookSource from local player");
                }
            }
        }

        /// <summary>
        /// Sets up remote player with LocalLookSource
        /// </summary>
        private void SetupRemotePlayer()
        {
            Debug.Log($"[PunLookSourceSetup] Setting up remote player: {gameObject.name}");

            // Add LocalLookSource for remote players
            var localLookSource = gameObject.GetComponent<Opsive.UltimateCharacterController.Character.LocalLookSource>();

            if (localLookSource == null)
            {
                localLookSource = gameObject.AddComponent<Opsive.UltimateCharacterController.Character.LocalLookSource>();
                Debug.Log("[PunLookSourceSetup] Added LocalLookSource to remote player");
            }

            // Disable input for remote players (they're controlled by other clients)
            // The input will be disabled by PunCharacter component automatically

            // Disable camera controller attachment for remote players
            var locomotionHandler = gameObject.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotionHandler>();
            if (locomotionHandler != null)
            {
                Debug.Log("[PunLookSourceSetup] Remote player setup complete");
            }
        }
    }
}
