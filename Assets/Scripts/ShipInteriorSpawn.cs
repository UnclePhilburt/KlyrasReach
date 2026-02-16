using UnityEngine;
using Photon.Pun;

public class ShipInteriorSpawn : MonoBehaviourPun
{
    [Header("Spawn Settings")]
    [Tooltip("Your player character prefab from Resources/Characters")]
    public GameObject playerPrefab;

    [Tooltip("Spawn player when scene starts?")]
    public bool spawnOnStart = true;

    void Start()
    {
        if (!spawnOnStart)
            return;

        // Check if player already exists
        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            Debug.Log("[ShipInteriorSpawn] Player already exists - skipping spawn");
            return;
        }

        SpawnPlayer();
    }

    public void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[ShipInteriorSpawn] No player prefab assigned!");
            return;
        }

        GameObject player = null;

        // Check if we're connected to Photon
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            // Multiplayer: Use Photon to spawn
            Debug.Log("[ShipInteriorSpawn] Spawning player with Photon (multiplayer)");
            player = PhotonNetwork.Instantiate(
                playerPrefab.name,
                transform.position,
                transform.rotation
            );
        }
        else
        {
            // Single player: Use normal Instantiate
            Debug.Log("[ShipInteriorSpawn] Spawning player locally (single player)");
            player = Instantiate(playerPrefab, transform.position, transform.rotation);
        }

        if (player != null)
        {
            player.name = "Player";
            Debug.Log($"[ShipInteriorSpawn] Successfully spawned player at {transform.position}");
        }
        else
        {
            Debug.LogError("[ShipInteriorSpawn] Failed to spawn player!");
        }
    }

    void OnDrawGizmos()
    {
        // Draw spawn point marker
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Draw direction arrow
        Gizmos.color = Color.blue;
        Vector3 forward = transform.forward * 2f;
        Gizmos.DrawRay(transform.position, forward);
    }
}
