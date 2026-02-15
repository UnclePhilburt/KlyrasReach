/*
 * PUN Camera Linker for Klyra's Reach
 *
 * PURPOSE:
 * Automatically links the camera to the local player when they spawn via PUN.
 *
 * HOW TO USE:
 * 1. Add this to the Main Camera in your game scene
 * 2. It will automatically find and link to your local player
 */

using UnityEngine;
using Photon.Pun;

namespace KlyrasReach.Multiplayer
{
    /// <summary>
    /// Links camera to the local PUN player when they spawn
    /// </summary>
    public class PunCameraLinker : MonoBehaviour
    {
        private Opsive.UltimateCharacterController.Camera.CameraController _cameraController;
        private bool _linkedToPlayer = false;

        private void Awake()
        {
            _cameraController = GetComponent<Opsive.UltimateCharacterController.Camera.CameraController>();

            if (_cameraController == null)
            {
                Debug.LogError("[PunCameraLinker] No CameraController found! Add this script to the Main Camera with CameraController.");
            }
        }

        private void Update()
        {
            // If already linked, nothing to do
            if (_linkedToPlayer) return;

            // Try to find the local player
            GameObject localPlayer = FindLocalPlayer();

            if (localPlayer != null)
            {
                LinkToPlayer(localPlayer);
            }
        }

        /// <summary>
        /// Finds the local player (the one controlled by this client)
        /// </summary>
        private GameObject FindLocalPlayer()
        {
            // Find all PhotonView components in the scene
            PhotonView[] photonViews = FindObjectsByType<PhotonView>(FindObjectsSortMode.None);

            foreach (PhotonView pv in photonViews)
            {
                // Check if this is OUR player (local player)
                if (pv.IsMine)
                {
                    // Check if it has UltimateCharacterLocomotion (it's a character)
                    var locomotion = pv.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
                    if (locomotion != null)
                    {
                        Debug.Log($"[PunCameraLinker] Found local player: {pv.gameObject.name}");
                        return pv.gameObject;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Links the camera to the player
        /// </summary>
        private void LinkToPlayer(GameObject player)
        {
            if (_cameraController == null) return;

            Debug.Log($"[PunCameraLinker] Linking camera to player: {player.name}");

            // Assign the character to the camera
            _cameraController.Character = player;

            // Also link crosshairs if present
            LinkCrosshairsToPlayer(player);

            _linkedToPlayer = true;
        }

        /// <summary>
        /// Links the CrosshairsMonitor to the player (if it exists)
        /// </summary>
        private void LinkCrosshairsToPlayer(GameObject player)
        {
            var crosshairsMonitor = FindAnyObjectByType<Opsive.UltimateCharacterController.UI.CrosshairsMonitor>();

            if (crosshairsMonitor != null)
            {
                crosshairsMonitor.Character = player;
                Debug.Log("[PunCameraLinker] Linked CrosshairsMonitor to player");
            }
        }
    }
}
