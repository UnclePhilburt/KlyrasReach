using UnityEngine;
using Photon.Pun;
using KlyrasReach.Player;
using UnityEngine.InputSystem;

public class PilotSeatInteraction : MonoBehaviourPun
{
    [Header("Pilot Ship Reference")]
    [Tooltip("Drag the actual flyable ship GameObject here")]
    public GameObject pilotShip;

    [Header("Interaction Settings")]
    [Tooltip("How close the player needs to be to press F")]
    public float interactionDistance = 3f;

    [Header("UI Prompt (Optional)")]
    public GameObject promptUI; // Optional: "Press F to Pilot Ship" UI

    private GameObject localPlayer;
    private bool playerInRange = false;

    void Update()
    {
        // Only process input for the local player
        if (localPlayer == null)
            return;

        // Check if keyboard is available
        if (Keyboard.current == null)
            return;

        // Check if player presses F while in range
        if (playerInRange && Keyboard.current.fKey.wasPressedThisFrame)
        {
            Debug.Log("[PilotSeat] F key pressed! Entering pilot ship...");
            EnterPilotShip();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[PilotSeat] Something entered trigger: {other.gameObject.name}, Tag: {other.tag}");

        // Try to find the player - check this object and all parents
        Transform current = other.transform;
        GameObject playerObject = null;

        while (current != null)
        {
            if (current.CompareTag("Player"))
            {
                playerObject = current.gameObject;
                Debug.Log($"[PilotSeat] Found player tag on: {current.name}");
                break;
            }
            current = current.parent;
        }

        if (playerObject != null)
        {
            // Check if it's a multiplayer game
            PhotonView pv = playerObject.GetComponent<PhotonView>();
            if (pv != null)
            {
                Debug.Log($"[PilotSeat] Multiplayer mode - IsMine: {pv.IsMine}");
                // Only activate for local player in multiplayer
                if (!pv.IsMine)
                {
                    Debug.Log("[PilotSeat] Not my player, ignoring");
                    return;
                }
            }
            else
            {
                Debug.Log("[PilotSeat] Single player mode detected");
            }

            // This is the local player (or single player)
            localPlayer = playerObject;
            playerInRange = true;
            Debug.Log("[PilotSeat] ✓ Player entered range! Press F to pilot ship.");

            // Show prompt UI if assigned
            if (promptUI != null)
                promptUI.SetActive(true);
        }
        else
        {
            Debug.Log($"[PilotSeat] Not a player (no 'Player' tag found in hierarchy)");
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Check if it's the player leaving range
        if (other.CompareTag("Player") && other.gameObject == localPlayer)
        {
            playerInRange = false;
            localPlayer = null;
            Debug.Log("[PilotSeat] Player left range");

            // Hide prompt UI if assigned
            if (promptUI != null)
                promptUI.SetActive(false);
        }
    }

    void EnterPilotShip()
    {
        Debug.Log("[PilotSeat] EnterPilotShip() called!");

        if (pilotShip == null)
        {
            Debug.LogError("[PilotSeat] Pilot Ship is not assigned in PilotSeatInteraction!");
            return;
        }
        else
        {
            Debug.Log($"[PilotSeat] Pilot ship found: {pilotShip.name}");
        }

        // Get the ShipController component (your existing ship control script)
        Debug.Log($"[PilotSeat] Looking for ShipController on: {pilotShip.name}");
        ShipController shipController = pilotShip.GetComponent<ShipController>();
        if (shipController == null)
        {
            Debug.LogError($"[PilotSeat] Pilot Ship '{pilotShip.name}' doesn't have a ShipController component!");
            return;
        }
        Debug.Log($"[PilotSeat] Found ShipController on {pilotShip.name}, IsActive: {shipController.IsActive}");

        // Get the ShipPilotController (our helper script)
        ShipPilotController pilotController = pilotShip.GetComponent<ShipPilotController>();
        if (pilotController == null)
        {
            Debug.LogError("Pilot Ship doesn't have a ShipPilotController component!");
            return;
        }

        // Get the player's camera
        Camera playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("[PilotSeat] Cannot find main camera!");
            return;
        }
        Debug.Log($"[PilotSeat] Found camera: {playerCamera.name}");

        // Disable Opsive Camera Controller so ship can control camera
        var opsiveCameraController = playerCamera.GetComponent<Opsive.UltimateCharacterController.Camera.CameraController>();
        if (opsiveCameraController != null)
        {
            opsiveCameraController.enabled = false;
            Debug.Log("[PilotSeat] Disabled Opsive Camera Controller");
        }

        // Unparent camera from player
        if (playerCamera.transform.parent != null)
        {
            Debug.Log($"[PilotSeat] Unparenting camera from: {playerCamera.transform.parent.name}");
            playerCamera.transform.SetParent(null);
        }

        // Hide the player character
        Debug.Log("[PilotSeat] Hiding player character...");
        SetPlayerVisible(localPlayer, false);

        // Disable player controls (so they can't walk around while flying)
        Debug.Log("[PilotSeat] Disabling player controls...");
        SetPlayerControlsEnabled(localPlayer, false);

        // Tell the pilot controller who is piloting
        Debug.Log("[PilotSeat] Setting pilot...");
        pilotController.SetPilot(localPlayer);

        // Activate the ship with player's camera
        Debug.Log("[PilotSeat] Activating ship controller...");
        shipController.EnterShip(playerCamera);
        Debug.Log("[PilotSeat] ✓ Ship should now be active!");

        // Hide prompt
        if (promptUI != null)
            promptUI.SetActive(false);

        playerInRange = false;
        // Don't clear localPlayer reference - we need it for when they exit
    }

    void SetPlayerVisible(GameObject player, bool visible)
    {
        // Hide/show all renderers on the player (including children)
        Renderer[] renderers = player.GetComponentsInChildren<Renderer>(true); // true = include inactive
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = visible;
        }

        // Also disable shadow casting when hidden
        if (!visible)
        {
            foreach (Renderer renderer in renderers)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
        else
        {
            foreach (Renderer renderer in renderers)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }
    }

    void SetPlayerControlsEnabled(GameObject player, bool enabled)
    {
        Debug.Log($"[PilotSeat] SetPlayerControlsEnabled - enabled: {enabled}");

        // Disable character controller
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = enabled;
            Debug.Log($"[PilotSeat] CharacterController enabled: {enabled}");
        }

        // Disable Opsive UCC
        var uccLocomotion = player.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
        if (uccLocomotion != null)
        {
            uccLocomotion.enabled = enabled;
            Debug.Log($"[PilotSeat] UCC Locomotion enabled: {enabled}");
        }

        // Also disable the UCC character handler
        var characterHandler = player.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotionHandler>();
        if (characterHandler != null)
        {
            characterHandler.enabled = enabled;
            Debug.Log($"[PilotSeat] UCC Handler enabled: {enabled}");
        }
    }
}
