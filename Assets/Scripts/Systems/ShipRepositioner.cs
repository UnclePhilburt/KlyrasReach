using UnityEngine;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Simply repositions the ship when returning from docking
    /// Does NOT spawn anything - just moves existing ship
    /// </summary>
    public class ShipRepositioner : MonoBehaviour
    {
        // Static variables to persist across scene loads
        private static Vector3 _savedShipPosition;
        private static Quaternion _savedShipRotation;
        private static bool _hasSavedPosition = false;

        /// <summary>
        /// Saves ship position before docking
        /// Called by DockingStation
        /// </summary>
        public static void SaveShipPosition(Vector3 position, Quaternion rotation)
        {
            _savedShipPosition = position;
            _savedShipRotation = rotation;
            _hasSavedPosition = true;

            Debug.Log($"[ShipRepositioner] ========== SAVE CALLED ==========");
            Debug.Log($"[ShipRepositioner] Saved ship position: {position}");
            Debug.Log($"[ShipRepositioner] Saved ship rotation: {rotation.eulerAngles}");
            Debug.Log($"[ShipRepositioner] _hasSavedPosition set to TRUE");
        }

        void Start()
        {
            Debug.Log($"[ShipRepositioner] Start() called. _hasSavedPosition: {_hasSavedPosition}");

            // Only reposition if we have a saved position
            if (!_hasSavedPosition)
            {
                Debug.Log("[ShipRepositioner] No saved position - ship stays where it is");
                return;
            }

            // Wait a moment for other systems to initialize, then reposition
            StartCoroutine(RepositionAfterDelay());
        }

        System.Collections.IEnumerator RepositionAfterDelay()
        {
            Debug.Log("[ShipRepositioner] Waiting 0.1 seconds for other systems to initialize...");
            yield return new WaitForSeconds(0.1f);

            Debug.Log($"[ShipRepositioner] Saved position exists: {_savedShipPosition}");

            // Find the PILOTABLE ship (has ShipController component)
            Player.ShipController[] controllers = FindObjectsByType<Player.ShipController>(FindObjectsSortMode.None);
            GameObject ship = null;

            foreach (var controller in controllers)
            {
                ship = controller.gameObject;
                Debug.Log($"[ShipRepositioner] Found ship with ShipController: {ship.name}");
                break;
            }

            if (ship == null)
            {
                Debug.LogWarning("[ShipRepositioner] No ship found with ShipController!");
                yield break;
            }

            Debug.Log($"[ShipRepositioner] Using pilotable ship: {ship.name} at position {ship.transform.position}");
            Debug.Log($"[ShipRepositioner] Moving ship from {ship.transform.position} to {_savedShipPosition}");

            // Reposition it
            ship.transform.position = _savedShipPosition;
            ship.transform.rotation = _savedShipRotation;

            Debug.Log($"[ShipRepositioner] ✓ Repositioned ship! New position: {ship.transform.position}");

            // Clear saved position for next time
            _hasSavedPosition = false;
        }

    }
}
