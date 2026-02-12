/*
 * Disable Enemy Footsteps for Klyra's Reach
 *
 * PURPOSE:
 * Immediately disables CharacterFootEffects on enemy characters
 * Prevents footstep audio overload with large hordes
 *
 * HOW TO USE:
 * Add this script to your enemy prefab - it will auto-disable footsteps
 */

using UnityEngine;

namespace KlyrasReach.AI
{
    /// <summary>
    /// Disables footsteps immediately when enemy spawns
    /// Runs in Awake() before anything else can trigger
    /// </summary>
    [DefaultExecutionOrder(-100)] // Execute before other scripts
    public class DisableEnemyFootsteps : MonoBehaviour
    {
        /// <summary>
        /// Disable footsteps immediately on spawn
        /// </summary>
        private void Awake()
        {
            // Find and disable CharacterFootEffects
            var footEffects = GetComponent<Opsive.UltimateCharacterController.Character.CharacterFootEffects>();
            if (footEffects != null)
            {
                footEffects.enabled = false;
                Debug.Log($"[DisableEnemyFootsteps] Disabled footsteps on {gameObject.name}");
            }
        }
    }
}
