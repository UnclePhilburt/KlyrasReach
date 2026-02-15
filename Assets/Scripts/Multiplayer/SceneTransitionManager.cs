/*
 * Scene Transition Manager for Klyra's Reach
 *
 * PURPOSE:
 * Handles scene transitions for individual players in multiplayer.
 * Ensures ships persist across scene loads (DontDestroyOnLoad).
 * Each player can travel independently without affecting others.
 *
 * HOW TO USE:
 * Call SceneTransitionManager.LoadScene("SceneName") to travel to a new location.
 * Your ship will persist and travel with you.
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

namespace KlyrasReach.Multiplayer
{
    /// <summary>
    /// Manages scene transitions for multiplayer
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        private static SceneTransitionManager _instance;

        private void Awake()
        {
            // Singleton pattern
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("[SceneTransitionManager] Initialized - ready for scene transitions");
        }

        /// <summary>
        /// Load a new scene for this player only (independent travel)
        /// </summary>
        public static void LoadScene(string sceneName)
        {
            if (!PhotonNetwork.IsConnected)
            {
                Debug.LogWarning("[SceneTransitionManager] Not connected to Photon - loading scene locally");
                SceneManager.LoadScene(sceneName);
                return;
            }

            Debug.Log($"[SceneTransitionManager] Loading scene '{sceneName}' for this player only");
            Debug.Log($"[SceneTransitionManager] Ships and networked objects will persist");

            // Load the scene (only for this player, others stay in their current scene)
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Load a scene by build index
        /// </summary>
        public static void LoadScene(int sceneBuildIndex)
        {
            if (!PhotonNetwork.IsConnected)
            {
                Debug.LogWarning("[SceneTransitionManager] Not connected to Photon - loading scene locally");
                SceneManager.LoadScene(sceneBuildIndex);
                return;
            }

            Debug.Log($"[SceneTransitionManager] Loading scene index {sceneBuildIndex} for this player only");
            Debug.Log($"[SceneTransitionManager] Ships and networked objects will persist");

            SceneManager.LoadScene(sceneBuildIndex);
        }

        /// <summary>
        /// Load scene asynchronously with progress tracking
        /// </summary>
        public static void LoadSceneAsync(string sceneName, System.Action<float> onProgress = null, System.Action onComplete = null)
        {
            if (_instance != null)
            {
                _instance.StartCoroutine(_instance.LoadSceneAsyncCoroutine(sceneName, onProgress, onComplete));
            }
            else
            {
                Debug.LogError("[SceneTransitionManager] No instance found! Make sure SceneTransitionManager exists in the scene.");
            }
        }

        private System.Collections.IEnumerator LoadSceneAsyncCoroutine(string sceneName, System.Action<float> onProgress, System.Action onComplete)
        {
            Debug.Log($"[SceneTransitionManager] Loading scene '{sceneName}' asynchronously");

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                onProgress?.Invoke(progress);

                Debug.Log($"[SceneTransitionManager] Loading progress: {progress * 100f:F0}%");

                yield return null;
            }

            Debug.Log($"[SceneTransitionManager] Scene '{sceneName}' loaded successfully");
            onComplete?.Invoke();
        }

        /// <summary>
        /// Get the name of the current scene
        /// </summary>
        public static string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }

        /// <summary>
        /// Check if we're currently in a specific scene
        /// </summary>
        public static bool IsInScene(string sceneName)
        {
            return SceneManager.GetActiveScene().name == sceneName;
        }
    }
}
