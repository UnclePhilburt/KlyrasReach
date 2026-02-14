/*
 * Lower Player Footsteps for Klyra's Reach
 *
 * PURPOSE:
 * Reduces the volume of player footstep sounds
 * Makes footsteps much quieter for better audio balance
 * Works with Opsive Ultimate Character Controller's CharacterFootEffects system
 *
 * HOW TO USE:
 * Add this script to your player character prefab
 */

using UnityEngine;

namespace KlyrasReach.Player
{
    /// <summary>
    /// Lowers player footstep volume by modifying Opsive's foot AudioSource volumes
    /// </summary>
    [DefaultExecutionOrder(100)] // Execute after CharacterFootEffects creates AudioSources
    public class LowerPlayerFootsteps : MonoBehaviour
    {
        [Header("Volume Settings")]
        [Tooltip("Footstep volume (Opsive default is 0.4, this reduces it)")]
        [SerializeField] [Range(0f, 1f)] private float _footstepVolume = 0.05f;

        /// <summary>
        /// Lower footstep volume after Opsive creates the AudioSources
        /// </summary>
        private void Start()
        {
            // Opsive's CharacterFootEffects creates AudioSource components on the feet
            // Default volume is 0.4f - we reduce it here
            var footEffects = GetComponent<Opsive.UltimateCharacterController.Character.CharacterFootEffects>();
            if (footEffects != null && footEffects.Feet != null)
            {
                // Loop through each foot transform and reduce its AudioSource volume
                foreach (var foot in footEffects.Feet)
                {
                    if (foot.Object != null)
                    {
                        AudioSource audioSource = foot.Object.GetComponent<AudioSource>();
                        if (audioSource != null)
                        {
                            audioSource.volume = _footstepVolume;
                        }
                    }
                }
            }
        }
    }
}
