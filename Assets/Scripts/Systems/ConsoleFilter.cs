/*
 * Console Filter for Klyra's Reach
 *
 * PURPOSE:
 * Filters out annoying console warnings that you don't need to see
 *
 * HOW TO USE:
 * 1. Create an empty GameObject called "ConsoleFilter" in your scene
 * 2. Add this script to it
 * 3. Warnings will be automatically filtered
 */

using UnityEngine;

namespace KlyrasReach.Systems
{
    /// <summary>
    /// Filters out specific console warnings
    /// </summary>
    public class ConsoleFilter : MonoBehaviour
    {
        private void Awake()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            // Filter out Cozy Weather "FX Block Zone" warnings
            if (logString.Contains("Tag: FX Block Zone is not defined"))
            {
                // Suppress this warning - don't log it
                return;
            }

            // Filter out Scheduler errors (if still happening)
            if (logString.Contains("ActiveEvents array is full"))
            {
                // Suppress this error
                return;
            }
        }
    }
}
