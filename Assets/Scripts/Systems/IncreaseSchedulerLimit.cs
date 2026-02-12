/*
 * Increase Scheduler Limit for Klyra's Reach
 *
 * PURPOSE:
 * Increases Opsive's Scheduler MaxEventCount to handle large hordes
 * Prevents audio scheduler errors with many enemies
 *
 * HOW TO USE:
 * 1. Create an empty GameObject in your scene called "SchedulerManager"
 * 2. Add this script to it
 * 3. That's it - it will automatically increase the limit on scene load
 */

using UnityEngine;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Increases the Opsive Scheduler's max event count to handle hordes
    /// </summary>
    [DefaultExecutionOrder(-1000)] // Run before everything else
    public class IncreaseSchedulerLimit : MonoBehaviour
    {
        [Tooltip("Maximum number of scheduled events (increase for large hordes)")]
        [SerializeField] private int _maxEventCount = 10000;

        /// <summary>
        /// Find or create the Scheduler and set the limit
        /// </summary>
        private void Awake()
        {
            // Find the Opsive Scheduler in the scene
            var scheduler = FindObjectOfType<Opsive.Shared.Game.SchedulerBase>();

            if (scheduler == null)
            {
                // Create a new Scheduler GameObject if it doesn't exist
                var schedulerGO = new GameObject("Scheduler");
                scheduler = schedulerGO.AddComponent<Opsive.Shared.Game.Scheduler>();
                Debug.Log("[IncreaseSchedulerLimit] Created new Scheduler GameObject");
            }

            // Use reflection to set the MaxEventCount field BEFORE it's used
            var field = typeof(Opsive.Shared.Game.SchedulerBase).GetField("m_MaxEventCount",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                int currentValue = (int)field.GetValue(scheduler);
                field.SetValue(scheduler, _maxEventCount);
                Debug.Log($"[IncreaseSchedulerLimit] Set Scheduler MaxEventCount from {currentValue} to {_maxEventCount}");
            }
            else
            {
                Debug.LogError("[IncreaseSchedulerLimit] Could not find m_MaxEventCount field on Scheduler!");
            }
        }
    }
}
