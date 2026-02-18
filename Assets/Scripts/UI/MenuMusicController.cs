/*
 * Menu Music Controller for Klyra's Reach
 *
 * PURPOSE:
 * Plays background music across the title screen and main menu scenes.
 * Uses DontDestroyOnLoad to persist between scenes. When the loading scene
 * starts, the music fades out smoothly and the object destroys itself.
 *
 * HOW TO USE:
 * 1. Open the Title scene (07_Demo_SciFiMenus_Screen_Title_03)
 * 2. Create an empty GameObject named "MenuMusic"
 * 3. Add this script to it
 * 4. Add an AudioSource component (or let the script create one)
 * 5. Assign your music clip in the Inspector
 * 6. That's it — it persists into the main menu scene and fades out
 *    when the loading scene loads
 */

using UnityEngine;
using UnityEngine.SceneManagement;

namespace KlyrasReach.UI
{
    /// <summary>
    /// Plays menu music across title and main menu scenes.
    /// Persists between scenes using DontDestroyOnLoad.
    /// Fades out and destroys itself when the loading scene starts.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MenuMusicController : MonoBehaviour
    {
        // =====================================================================
        // INSPECTOR FIELDS
        // =====================================================================

        [Header("Music")]
        [Tooltip("The music clip to play on the title and main menu screens")]
        [SerializeField] private AudioClip _menuMusic;

        [Tooltip("Volume of the music (0 to 1)")]
        [SerializeField] [Range(0f, 1f)] private float _volume = 0.5f;

        [Header("Fade Settings")]
        [Tooltip("How long (in seconds) the music takes to fade out when loading starts")]
        [SerializeField] private float _fadeDuration = 2f;

        [Header("Scene Detection")]
        [Tooltip("Name of the loading scene — music fades out when this scene loads")]
        [SerializeField] private string _loadingSceneName = "14_Demo_SciFiMenus_Screen_Loading_01";

        // =====================================================================
        // PRIVATE STATE
        // =====================================================================

        // Reference to the AudioSource component on this GameObject
        private AudioSource _audioSource;

        // Whether the music is currently fading out
        private bool _isFading = false;

        // Singleton — only one MenuMusicController should exist at a time
        private static MenuMusicController _instance;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        /// <summary>
        /// Called before Start. Sets up the singleton pattern so only one
        /// music player exists, even if the title scene is loaded again.
        /// </summary>
        private void Awake()
        {
            // If another MenuMusicController already exists, destroy this one
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // This is the one and only instance
            _instance = this;

            // Don't destroy this object when loading a new scene
            // (so music keeps playing from title -> main menu)
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Called once when the script starts. Sets up the AudioSource and starts playing.
        /// Uses the player's saved volume preference from PlayerPrefs.
        /// </summary>
        private void Start()
        {
            // Get or set up the AudioSource
            _audioSource = GetComponent<AudioSource>();
            _audioSource.clip = _menuMusic;

            // Use the player's saved volume preference instead of the Inspector value.
            // This way, if they changed it in Settings, it carries over.
            _volume = PlayerPrefs.GetFloat("MusicVolume", _volume);
            _audioSource.volume = _volume;

            _audioSource.loop = true;
            _audioSource.playOnAwake = false;

            // Start playing the music
            if (_menuMusic != null)
            {
                _audioSource.Play();
            }
            else
            {
                Debug.LogWarning("[MenuMusicController] No music clip assigned!");
            }

            // Listen for scene changes so we know when to fade out
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// Called every frame. Handles the fade-out effect when transitioning
        /// to the loading scene.
        /// </summary>
        private void Update()
        {
            // If we're fading out, gradually reduce the volume
            if (_isFading && _audioSource != null)
            {
                // Reduce volume over time based on fade duration
                _audioSource.volume -= (_volume / _fadeDuration) * Time.deltaTime;

                // Once volume hits zero, stop and clean up
                if (_audioSource.volume <= 0f)
                {
                    _audioSource.Stop();
                    _audioSource.volume = 0f;
                    _isFading = false;

                    // Clean up — no longer needed
                    Destroy(gameObject);
                }
            }
        }

        /// <summary>
        /// Cleans up when destroyed. Unsubscribes from scene events
        /// and clears the singleton reference.
        /// </summary>
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // Clear the singleton if this is the active instance
            if (_instance == this)
            {
                _instance = null;
            }
        }

        // =====================================================================
        // PUBLIC STATIC METHODS
        // =====================================================================

        /// <summary>
        /// Sets the music volume on the active MenuMusicController instance.
        /// Called by MusicVolumeSettings when the player moves the Settings slider,
        /// so the volume change is heard immediately while adjusting.
        /// Safe to call even if no MenuMusicController exists (does nothing).
        /// </summary>
        /// <param name="volume">Volume level from 0 (silent) to 1 (full)</param>
        public static void SetMusicVolume(float volume)
        {
            // If no menu music is currently playing, just return silently.
            // This happens when the player opens Settings from in-game
            // (where menu music has already been destroyed).
            if (_instance == null) return;

            // Update both the stored volume and the live audio
            _instance._volume = volume;
            if (_instance._audioSource != null)
            {
                _instance._audioSource.volume = volume;
            }
        }

        // =====================================================================
        // SCENE CHANGE HANDLER
        // =====================================================================

        /// <summary>
        /// Called whenever a new scene finishes loading.
        /// If the loading scene loaded, start fading out the music.
        /// </summary>
        /// <param name="scene">The scene that just loaded</param>
        /// <param name="mode">How the scene was loaded</param>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // If this is the loading scene, start fading the music out
            if (scene.name == _loadingSceneName)
            {
                Debug.Log("[MenuMusicController] Loading scene detected — fading out music...");
                _isFading = true;
            }
        }
    }
}
