using UnityEngine;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Inventory;

namespace KlyrasReach
{
    /// <summary>
    /// Safe zone where weapons and magic cannot be equipped
    /// </summary>
    public class SafeZone : MonoBehaviour
    {
        [Header("Safe Zone Settings")]
        [Tooltip("Message to show when entering safe zone")]
        [SerializeField] private string _enterMessage = "Entering Safe Zone - Weapons Disabled";

        [Tooltip("Message to show when exiting safe zone")]
        [SerializeField] private string _exitMessage = "Leaving Safe Zone - Weapons Enabled";

        [Tooltip("Show debug messages")]
        [SerializeField] private bool _debugMode = true;

        private void Start()
        {
            // Wait a bit for player to fully initialize before checking
            StartCoroutine(CheckForPlayersAfterDelay());
        }

        private System.Collections.IEnumerator CheckForPlayersAfterDelay()
        {
            // Wait 1 second for player to fully spawn and initialize
            yield return new WaitForSeconds(1f);

            Debug.Log("[SafeZone] Checking for players already in safe zone...");

            // Check if any characters are already inside the safe zone
            Collider[] colliders = Physics.OverlapBox(
                transform.position + GetComponent<BoxCollider>().center,
                GetComponent<BoxCollider>().size / 2,
                transform.rotation);

            foreach (var collider in colliders)
            {
                var characterLocomotion = collider.GetComponent<UltimateCharacterLocomotion>();
                if (characterLocomotion == null)
                {
                    characterLocomotion = collider.GetComponentInParent<UltimateCharacterLocomotion>();
                }

                if (characterLocomotion != null)
                {
                    // Player is already inside - trigger the safe zone effect
                    Debug.Log($"[SafeZone] {collider.name} was already inside safe zone at start");
                    ProcessEnterSafeZone(characterLocomotion, collider.GetComponent<InventoryBase>() ?? collider.GetComponentInParent<InventoryBase>());
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Debug: Log ANY object entering
            Debug.Log($"[SafeZone] TRIGGER ENTERED by: {other.gameObject.name}");

            // Check if it's the player (might be on this object or parent)
            var characterLocomotion = other.GetComponent<UltimateCharacterLocomotion>();
            if (characterLocomotion == null)
            {
                // Try parent
                characterLocomotion = other.GetComponentInParent<UltimateCharacterLocomotion>();
            }

            if (characterLocomotion == null)
            {
                Debug.Log($"[SafeZone] {other.gameObject.name} has no UltimateCharacterLocomotion (checked parent too)");
                return;
            }

            // Get the inventory
            var inventory = other.GetComponent<InventoryBase>();
            if (inventory == null)
            {
                inventory = other.GetComponentInParent<InventoryBase>();
            }

            ProcessEnterSafeZone(characterLocomotion, inventory);
        }

        private void ProcessEnterSafeZone(UltimateCharacterLocomotion characterLocomotion, InventoryBase inventory)
        {
            if (characterLocomotion == null)
            {
                if (_debugMode)
                    Debug.LogWarning("[SafeZone] Character locomotion is null!");
                return;
            }

            if (inventory == null)
            {
                if (_debugMode)
                    Debug.LogWarning("[SafeZone] Character has no inventory component!");
                return;
            }

            // Use Opsive's EquipUnequip ability to properly holster weapons
            var equipUnequip = characterLocomotion.GetAbility<Opsive.UltimateCharacterController.Character.Abilities.Items.EquipUnequip>();
            if (equipUnequip != null)
            {
                // Trigger unequip for all items - this will play the proper animation
                equipUnequip.StartEquipUnequip(-1, true); // -1 = all items, true = unequip
                if (_debugMode)
                    Debug.Log("[SafeZone] Triggered unequip animation via ability");
            }

            // Disable item abilities AFTER triggering unequip
            DisableItemAbilities(characterLocomotion, true);

            if (_debugMode)
                Debug.Log($"[SafeZone] {characterLocomotion.gameObject.name} entered safe zone - items disabled");

            // TODO: Show UI message to player
            // You can add UI code here to display _enterMessage
        }

        private void OnTriggerExit(Collider other)
        {
            Debug.Log($"[SafeZone] TRIGGER EXITED by: {other.gameObject.name}");

            // Check if it's the player (might be on this object or parent)
            var characterLocomotion = other.GetComponent<UltimateCharacterLocomotion>();
            if (characterLocomotion == null)
            {
                characterLocomotion = other.GetComponentInParent<UltimateCharacterLocomotion>();
            }

            if (characterLocomotion == null)
                return;

            // Re-enable item abilities
            DisableItemAbilities(characterLocomotion, false);

            if (_debugMode)
                Debug.Log($"[SafeZone] {other.name} exited safe zone - items enabled");

            // TODO: Show UI message to player
            // You can add UI code here to display _exitMessage
        }

        /// <summary>
        /// Unequip all currently equipped items
        /// </summary>
        private void UnequipAllItems(InventoryBase inventory)
        {
            Debug.Log("[SafeZone] Force unequipping ALL items...");

            // Use Opsive's method to unequip all items at once
            for (int slotID = 0; slotID < inventory.SlotCount; slotID++)
            {
                var item = inventory.GetActiveCharacterItem(slotID);
                if (item != null)
                {
                    Debug.Log($"[SafeZone] Unequipping item '{item.name}' from slot {slotID}");
                }

                // Force unequip this slot (pass itemIdentifier and slotID)
                inventory.UnequipItem(slotID);
            }

            Debug.Log("[SafeZone] All items unequipped");
        }

        /// <summary>
        /// Enable or disable item equip abilities
        /// </summary>
        private void DisableItemAbilities(UltimateCharacterLocomotion characterLocomotion, bool disable)
        {
            if (characterLocomotion == null) return;

            // Disable all equip-related abilities with null checks
            var equipUnequip = characterLocomotion.GetAbilities<Opsive.UltimateCharacterController.Character.Abilities.Items.EquipUnequip>();
            if (equipUnequip != null)
            {
                foreach (var ability in equipUnequip)
                {
                    if (ability != null)
                    {
                        ability.Enabled = !disable;
                    }
                }
            }

            var toggleEquip = characterLocomotion.GetAbilities<Opsive.UltimateCharacterController.Character.Abilities.Items.ToggleEquip>();
            if (toggleEquip != null)
            {
                foreach (var ability in toggleEquip)
                {
                    if (ability != null)
                    {
                        ability.Enabled = !disable;
                    }
                }
            }

            var equipNext = characterLocomotion.GetAbilities<Opsive.UltimateCharacterController.Character.Abilities.Items.EquipNext>();
            if (equipNext != null)
            {
                foreach (var ability in equipNext)
                {
                    if (ability != null)
                    {
                        ability.Enabled = !disable;
                    }
                }
            }

            var equipPrevious = characterLocomotion.GetAbilities<Opsive.UltimateCharacterController.Character.Abilities.Items.EquipPrevious>();
            if (equipPrevious != null)
            {
                foreach (var ability in equipPrevious)
                {
                    if (ability != null)
                    {
                        ability.Enabled = !disable;
                    }
                }
            }

            var equipScroll = characterLocomotion.GetAbilities<Opsive.UltimateCharacterController.Character.Abilities.Items.EquipScroll>();
            if (equipScroll != null)
            {
                foreach (var ability in equipScroll)
                {
                    if (ability != null)
                    {
                        ability.Enabled = !disable;
                    }
                }
            }
        }

        /// <summary>
        /// Draw the safe zone in the editor
        /// </summary>
        private void OnDrawGizmos()
        {
            // Draw a green wireframe to show the safe zone
            Gizmos.color = new Color(0, 1, 0, 0.3f);

            var boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCollider.center, boxCollider.size);
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }

            var sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawSphere(sphereCollider.center, sphereCollider.radius);
                Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
            }
        }
    }
}
