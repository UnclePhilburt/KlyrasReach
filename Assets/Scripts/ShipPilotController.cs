using UnityEngine;
using Photon.Pun;
using KlyrasReach.Player;
using UnityEngine.InputSystem;

public class ShipPilotController : MonoBehaviourPun
{
    [Header("Interior Ship Reference")]
    [Tooltip("Drag the stationary ship interior GameObject here")]
    public GameObject shipInterior;

    [Header("Exit Settings")]
    [Tooltip("Where the player returns to when exiting (usually somewhere in the interior)")]
    public Transform exitSpawnPoint;

    [Header("Exit Key")]
    [Tooltip("Key to press to exit the pilot ship")]
    public KeyCode exitKey = KeyCode.E;

    private GameObject currentPilot;
    private ShipController shipController;

    void Awake()
    {
        // Get the ShipController component
        shipController = GetComponent<ShipController>();
        if (shipController == null)
        {
            Debug.LogError("ShipPilotController requires a ShipController component on the same GameObject!");
        }
    }

    void Update()
    {
        // Check if keyboard is available
        if (Keyboard.current == null)
            return;

        // If someone is piloting, check for exit input (E key)
        if (currentPilot != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            ExitPilotShip();
        }
    }

    public void SetPilot(GameObject player)
    {
        if (currentPilot != null)
        {
            // If it's the same player, just log a warning
            if (currentPilot == player)
            {
                Debug.LogWarning("[ShipPilot] You're already piloting this ship!");
                return;
            }
            else
            {
                Debug.LogWarning("[ShipPilot] Another player is already piloting this ship!");
                return;
            }
        }

        currentPilot = player;
        Debug.Log($"[ShipPilot] {player.name} is now piloting the ship. Press E to exit.");
    }

    void ExitPilotShip()
    {
        if (currentPilot == null)
            return;

        if (exitSpawnPoint == null)
        {
            Debug.LogError("Exit Spawn Point is not assigned in ShipPilotController!");
            return;
        }

        // Exit the ship (calls ExitShip() on ShipController)
        if (shipController != null)
        {
            shipController.ExitShip();
        }

        // Re-enable Opsive Camera Controller
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            var opsiveCameraController = mainCamera.GetComponent<Opsive.UltimateCharacterController.Camera.CameraController>();
            if (opsiveCameraController != null)
            {
                opsiveCameraController.Character = currentPilot;
                opsiveCameraController.enabled = true;
                Debug.Log("[ShipPilot] Re-enabled Opsive Camera Controller");
            }
        }

        // Teleport player back to interior
        currentPilot.transform.position = exitSpawnPoint.position;
        currentPilot.transform.rotation = exitSpawnPoint.rotation;

        // Re-enable player visibility
        SetPlayerVisible(currentPilot, true);

        // Re-enable player controls
        SetPlayerControlsEnabled(currentPilot, true);

        Debug.Log($"[ShipPilot] {currentPilot.name} exited the pilot ship and returned to interior.");
        currentPilot = null;
    }

    void SetPlayerVisible(GameObject player, bool visible)
    {
        // Show/hide all renderers on the player (including children)
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
        Debug.Log($"[ShipPilot] SetPlayerControlsEnabled - enabled: {enabled}");

        // Re-enable character controller
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = enabled;
            Debug.Log($"[ShipPilot] CharacterController enabled: {enabled}");
        }

        // Re-enable Opsive UCC
        var uccLocomotion = player.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
        if (uccLocomotion != null)
        {
            uccLocomotion.enabled = enabled;
            Debug.Log($"[ShipPilot] UCC Locomotion enabled: {enabled}");
        }

        // Also re-enable the UCC character handler
        var characterHandler = player.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
        if (characterHandler != null)
        {
            characterHandler.enabled = enabled;
            Debug.Log($"[ShipPilot] UCC Handler enabled: {enabled}");
        }
    }

    // Called when player disconnects
    void OnPhotonPlayerDisconnected()
    {
        // If the current pilot disconnects, return them to interior
        if (currentPilot != null)
        {
            ExitPilotShip();
        }
    }

    // Public method to check if ship is being piloted
    public bool IsPiloted()
    {
        return currentPilot != null;
    }

    // Public method to get current pilot
    public GameObject GetCurrentPilot()
    {
        return currentPilot;
    }
}
