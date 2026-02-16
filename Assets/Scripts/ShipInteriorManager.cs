using UnityEngine;
using Photon.Pun;

public class ShipInteriorManager : MonoBehaviourPun
{
    [Header("Interior Settings")]
    [Tooltip("Should the interior be hidden when game starts?")]
    public bool hideInteriorOnStart = true;

    [Header("Hidden Location (Optional)")]
    [Tooltip("Move interior to this location to hide it (leave empty to just disable renderers)")]
    public Transform hiddenLocation;

    [Header("Pilot Ship Reference")]
    [Tooltip("Reference to the pilot ship (optional, for tracking)")]
    public GameObject pilotShip;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Renderer[] interiorRenderers;

    void Start()
    {
        // Store original position
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // Get all renderers in the interior (for hiding/showing)
        interiorRenderers = GetComponentsInChildren<Renderer>();

        if (hideInteriorOnStart)
        {
            HideInterior();
        }
    }

    public void HideInterior()
    {
        // Option 1: Move to hidden location
        if (hiddenLocation != null)
        {
            transform.position = hiddenLocation.position;
            transform.rotation = hiddenLocation.rotation;
        }

        // Option 2: Disable all renderers (makes it invisible)
        foreach (Renderer renderer in interiorRenderers)
        {
            renderer.enabled = false;
        }

        Debug.Log("Ship interior hidden.");
    }

    public void ShowInterior()
    {
        // Restore original position if it was moved
        if (hiddenLocation != null)
        {
            transform.position = originalPosition;
            transform.rotation = originalRotation;
        }

        // Enable all renderers
        foreach (Renderer renderer in interiorRenderers)
        {
            renderer.enabled = true;
        }

        Debug.Log("Ship interior shown.");
    }

    // Call this if you ever want the interior to follow the pilot ship
    // (Not needed based on your requirements, but included for flexibility)
    void Update()
    {
        // Uncomment this if you want interior to follow pilot ship
        // if (pilotShip != null)
        // {
        //     transform.position = pilotShip.transform.position;
        //     transform.rotation = pilotShip.transform.rotation;
        // }
    }

    // Public methods for other scripts to use
    public bool IsHidden()
    {
        return interiorRenderers.Length > 0 && !interiorRenderers[0].enabled;
    }

    public Vector3 GetOriginalPosition()
    {
        return originalPosition;
    }
}
