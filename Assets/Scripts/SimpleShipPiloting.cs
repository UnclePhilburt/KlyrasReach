using UnityEngine;
using UnityEngine.InputSystem;
using KlyrasReach.Player;

public class SimpleShipPiloting : MonoBehaviour
{
    [Header("Setup")]
    public GameObject pilotShip;
    public Transform exitPoint;

    [Header("Camera")]
    public Vector3 cameraOffset = new Vector3(0, 10, -30);

    private GameObject currentPlayer;
    private bool isPiloting = false;
    private Camera mainCamera;
    private ShipController shipController;
    private Transform originalParent;

    void Start()
    {
        Debug.Log($"[SimpleShip] Script started on {gameObject.name}");

        mainCamera = Camera.main;
        Debug.Log($"[SimpleShip] Main camera: {(mainCamera != null ? mainCamera.name : "NULL")}");

        if (pilotShip != null)
        {
            shipController = pilotShip.GetComponent<ShipController>();
            Debug.Log($"[SimpleShip] Pilot ship assigned: {pilotShip.name}, ShipController: {(shipController != null ? "Found" : "NOT FOUND")}");
        }
        else
        {
            Debug.LogError("[SimpleShip] Pilot Ship is NOT assigned!");
        }

        if (exitPoint == null)
        {
            Debug.LogError("[SimpleShip] Exit Point is NOT assigned!");
        }

        // Check if this object has a trigger collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("[SimpleShip] NO COLLIDER on this object!");
        }
        else if (!col.isTrigger)
        {
            Debug.LogError("[SimpleShip] Collider exists but IS TRIGGER is NOT checked!");
        }
        else
        {
            Debug.Log("[SimpleShip] ✓ Trigger collider found and configured correctly");
        }
    }

    void Update()
    {
        // F to enter
        if (!isPiloting && currentPlayer != null && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            EnterShip();
        }

        // E to exit
        if (isPiloting && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            ExitShip();
        }

        // Move camera to follow ship
        if (isPiloting && pilotShip != null && mainCamera != null)
        {
            Vector3 targetPos = pilotShip.transform.position + pilotShip.transform.TransformDirection(cameraOffset);
            mainCamera.transform.position = targetPos;
            mainCamera.transform.rotation = pilotShip.transform.rotation;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[SimpleShip] Something entered trigger: {other.gameObject.name}, Tag: {other.tag}");

        // Try to find the player by looking up the hierarchy for "Player" tag
        Transform current = other.transform;
        while (current != null)
        {
            if (current.CompareTag("Player"))
            {
                currentPlayer = current.gameObject;
                Debug.Log($"[SimpleShip] ✓ Player in range - press F to pilot (found on: {current.name})");
                return;
            }
            current = current.parent;
        }

        Debug.Log($"[SimpleShip] Not a player - no 'Player' tag found in hierarchy");
    }

    void OnTriggerExit(Collider other)
    {
        if (currentPlayer != null && (other.gameObject == currentPlayer || other.transform.root == currentPlayer.transform))
        {
            currentPlayer = null;
        }
    }

    void EnterShip()
    {
        if (pilotShip == null || currentPlayer == null) return;

        Debug.Log("[SimpleShip] Entering ship (remote control mode)!");

        // Save original parent and unparent player from ship so they don't move with it
        originalParent = currentPlayer.transform.parent;
        currentPlayer.transform.SetParent(null);
        Debug.Log($"[SimpleShip] Unparented player from {(originalParent != null ? originalParent.name : "null")}");

        // Hide player (they stay in cockpit but invisible)
        Renderer[] renderers = currentPlayer.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            r.enabled = false;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Disable player controls
        var ucc = currentPlayer.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
        if (ucc != null) ucc.enabled = false;

        var cc = currentPlayer.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Disable Opsive camera
        var opsiveCam = mainCamera.GetComponent<Opsive.UltimateCharacterController.Camera.CameraController>();
        if (opsiveCam != null) opsiveCam.enabled = false;

        // Activate ship (camera switches to ship)
        if (shipController != null)
        {
            shipController.EnterShip(mainCamera);
        }

        isPiloting = true;

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void ExitShip()
    {
        if (currentPlayer == null) return;

        Debug.Log("[SimpleShip] Exiting ship (switching camera back)!");

        // Deactivate ship
        if (shipController != null)
        {
            shipController.ExitShip();
        }

        // Re-parent player back to ship interior
        if (originalParent != null)
        {
            currentPlayer.transform.SetParent(originalParent);
            Debug.Log($"[SimpleShip] Re-parented player to {originalParent.name}");
        }

        // Show player (they're still in the same spot in the cockpit)
        Renderer[] renderers = currentPlayer.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            r.enabled = true;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        // Re-enable player controls
        var ucc = currentPlayer.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
        if (ucc != null) ucc.enabled = true;

        var cc = currentPlayer.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = true;

        // Re-enable Opsive camera (camera switches back to player)
        var opsiveCam = mainCamera.GetComponent<Opsive.UltimateCharacterController.Camera.CameraController>();
        if (opsiveCam != null)
        {
            opsiveCam.Character = currentPlayer;
            opsiveCam.enabled = true;
        }

        isPiloting = false;

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
