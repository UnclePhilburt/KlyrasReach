/*
 * Audio Zone Manager for Klyra's Reach
 *
 * PURPOSE:
 * Manages synchronized audio playback across multiple speakers in a zone.
 * All speakers play the same music at the same time, creating ambient audio.
 *
 * HOW TO USE:
 * 1. Create an empty GameObject in your space station scene
 * 2. Name it "Audio Zone - [Zone Name]" (e.g., "Audio Zone - Main Deck")
 * 3. Add this AudioZoneManager script to it
 * 4. Assign your music AudioClip in the Inspector
 * 5. The script will automatically find and sync all child speakers
 */

using UnityEngine;

namespace KlyrasReach.Audio
{
    /// <summary>
    /// Playlist playback modes
    /// </summary>
    public enum PlaylistMode
    {
        Sequential,  // Play songs in order
        Random,      // Play random song each time
        Shuffle,     // Shuffle once, then play in that order
        Loop         // Loop single song (uses first song in playlist)
    }

    /// <summary>
    /// Manages synchronized audio playback across multiple speakers in a zone
    /// </summary>
    public class AudioZoneManager : MonoBehaviour
    {
        [Header("Playlist Settings")]
        [Tooltip("List of music tracks to play")]
        [SerializeField] private AudioClip[] _playlist;

        [Tooltip("How should the playlist be played?")]
        [SerializeField] private PlaylistMode _playlistMode = PlaylistMode.Sequential;

        [Tooltip("Volume for all speakers in this zone (0-1)")]
        [SerializeField] [Range(0f, 1f)] private float _volume = 0.5f;

        [Tooltip("Play on start?")]
        [SerializeField] private bool _playOnStart = true;

        [Header("3D Audio Settings")]
        [Tooltip("Min distance for 3D audio (full volume within this range)")]
        [SerializeField] private float _minDistance = 5f;

        [Tooltip("Max distance for 3D audio (silent beyond this range)")]
        [SerializeField] private float _maxDistance = 50f;

        [Tooltip("How 2D/3D is the audio? (0 = 2D, 1 = 3D)")]
        [SerializeField] [Range(0f, 1f)] private float _spatialBlend = 1f;

        // Private variables
        private AudioSource[] _speakers;
        private bool _isPlaying = false;
        private int _currentTrackIndex = 0;
        private int[] _shuffledIndices;
        private int _shufflePlayIndex = 0;

        /// <summary>
        /// Initialize all speakers in this zone
        /// </summary>
        private void Start()
        {
            SetupSpeakers();

            // Initialize shuffle if needed
            if (_playlistMode == PlaylistMode.Shuffle && _playlist != null && _playlist.Length > 0)
            {
                ShufflePlaylist();
            }

            if (_playOnStart && _playlist != null && _playlist.Length > 0)
            {
                PlayMusic();
            }
        }

        /// <summary>
        /// Check if current track has finished and play next
        /// </summary>
        private void Update()
        {
            // Only auto-advance if not in Loop mode
            if (_isPlaying && _playlistMode != PlaylistMode.Loop && _speakers != null && _speakers.Length > 0)
            {
                // Check if the first speaker finished (all speakers are synced)
                if (!_speakers[0].isPlaying)
                {
                    PlayNextTrack();
                }
            }
        }

        /// <summary>
        /// Finds or creates AudioSource components on all child objects
        /// </summary>
        private void SetupSpeakers()
        {
            // Get all AudioSource components in children (including this object)
            _speakers = GetComponentsInChildren<AudioSource>();

            if (_speakers.Length == 0)
            {
                Debug.LogWarning($"[AudioZoneManager] No speakers found in zone '{gameObject.name}'. Add child GameObjects with AudioSource components.");
                return;
            }

            Debug.Log($"[AudioZoneManager] Found {_speakers.Length} speakers in zone '{gameObject.name}'");

            // Configure each speaker
            foreach (AudioSource speaker in _speakers)
            {
                ConfigureSpeaker(speaker);
            }
        }

        /// <summary>
        /// Configures an individual speaker with zone settings
        /// </summary>
        private void ConfigureSpeaker(AudioSource speaker)
        {
            speaker.volume = _volume;
            speaker.playOnAwake = false;
            speaker.loop = (_playlistMode == PlaylistMode.Loop); // Only loop in Loop mode

            // 3D audio settings
            speaker.spatialBlend = _spatialBlend;
            speaker.minDistance = _minDistance;
            speaker.maxDistance = _maxDistance;
            speaker.rolloffMode = AudioRolloffMode.Linear;

            // Stop any currently playing audio
            if (speaker.isPlaying)
            {
                speaker.Stop();
            }
        }

        /// <summary>
        /// Starts playing music on all speakers simultaneously
        /// </summary>
        public void PlayMusic()
        {
            if (_speakers == null || _speakers.Length == 0)
            {
                Debug.LogWarning("[AudioZoneManager] No speakers to play music on");
                return;
            }

            if (_playlist == null || _playlist.Length == 0)
            {
                Debug.LogWarning("[AudioZoneManager] No music in playlist");
                return;
            }

            // Get the current track to play
            AudioClip currentClip = GetCurrentTrack();
            if (currentClip == null)
            {
                Debug.LogWarning("[AudioZoneManager] Current track is null");
                return;
            }

            Debug.Log($"[AudioZoneManager] Playing track {_currentTrackIndex + 1}/{_playlist.Length}: '{currentClip.name}' on {_speakers.Length} speakers");

            // Set clip on all speakers and play synchronized
            foreach (AudioSource speaker in _speakers)
            {
                speaker.clip = currentClip;
                speaker.loop = (_playlistMode == PlaylistMode.Loop);
                speaker.volume = _volume; // Apply current volume setting
                speaker.Play();
            }

            _isPlaying = true;
        }

        /// <summary>
        /// Gets the current track based on playlist mode
        /// </summary>
        private AudioClip GetCurrentTrack()
        {
            if (_playlist == null || _playlist.Length == 0) return null;

            switch (_playlistMode)
            {
                case PlaylistMode.Sequential:
                case PlaylistMode.Loop:
                    return _playlist[_currentTrackIndex];

                case PlaylistMode.Random:
                    _currentTrackIndex = Random.Range(0, _playlist.Length);
                    return _playlist[_currentTrackIndex];

                case PlaylistMode.Shuffle:
                    if (_shuffledIndices == null || _shuffledIndices.Length != _playlist.Length)
                    {
                        ShufflePlaylist();
                    }
                    return _playlist[_shuffledIndices[_shufflePlayIndex]];

                default:
                    return _playlist[_currentTrackIndex];
            }
        }

        /// <summary>
        /// Shuffles the playlist indices
        /// </summary>
        private void ShufflePlaylist()
        {
            _shuffledIndices = new int[_playlist.Length];
            for (int i = 0; i < _playlist.Length; i++)
            {
                _shuffledIndices[i] = i;
            }

            // Fisher-Yates shuffle
            for (int i = _shuffledIndices.Length - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                int temp = _shuffledIndices[i];
                _shuffledIndices[i] = _shuffledIndices[randomIndex];
                _shuffledIndices[randomIndex] = temp;
            }

            _shufflePlayIndex = 0;
            Debug.Log("[AudioZoneManager] Playlist shuffled");
        }

        /// <summary>
        /// Plays the next track in the playlist
        /// </summary>
        public void PlayNextTrack()
        {
            if (_playlist == null || _playlist.Length == 0) return;

            switch (_playlistMode)
            {
                case PlaylistMode.Sequential:
                    _currentTrackIndex = (_currentTrackIndex + 1) % _playlist.Length;
                    break;

                case PlaylistMode.Random:
                    // Random mode picks random in GetCurrentTrack()
                    break;

                case PlaylistMode.Shuffle:
                    _shufflePlayIndex = (_shufflePlayIndex + 1) % _shuffledIndices.Length;
                    if (_shufflePlayIndex == 0)
                    {
                        // Reshuffle when we complete the list
                        ShufflePlaylist();
                    }
                    break;

                case PlaylistMode.Loop:
                    // Loop mode doesn't advance
                    return;
            }

            StopMusic();
            PlayMusic();
        }

        /// <summary>
        /// Plays the previous track in the playlist
        /// </summary>
        public void PlayPreviousTrack()
        {
            if (_playlist == null || _playlist.Length == 0) return;

            switch (_playlistMode)
            {
                case PlaylistMode.Sequential:
                    _currentTrackIndex--;
                    if (_currentTrackIndex < 0) _currentTrackIndex = _playlist.Length - 1;
                    break;

                case PlaylistMode.Random:
                    // Random mode just picks another random
                    break;

                case PlaylistMode.Shuffle:
                    _shufflePlayIndex--;
                    if (_shufflePlayIndex < 0)
                    {
                        ShufflePlaylist();
                        _shufflePlayIndex = _shuffledIndices.Length - 1;
                    }
                    break;

                case PlaylistMode.Loop:
                    // Loop mode doesn't advance
                    return;
            }

            StopMusic();
            PlayMusic();
        }

        /// <summary>
        /// Plays a specific track by index
        /// </summary>
        public void PlayTrack(int trackIndex)
        {
            if (_playlist == null || trackIndex < 0 || trackIndex >= _playlist.Length)
            {
                Debug.LogWarning($"[AudioZoneManager] Invalid track index: {trackIndex}");
                return;
            }

            _currentTrackIndex = trackIndex;
            StopMusic();
            PlayMusic();
        }

        /// <summary>
        /// Stops music on all speakers
        /// </summary>
        public void StopMusic()
        {
            if (_speakers == null) return;

            foreach (AudioSource speaker in _speakers)
            {
                speaker.Stop();
            }

            _isPlaying = false;
        }

        /// <summary>
        /// Pauses music on all speakers
        /// </summary>
        public void PauseMusic()
        {
            if (_speakers == null) return;

            foreach (AudioSource speaker in _speakers)
            {
                speaker.Pause();
            }

            _isPlaying = false;
        }

        /// <summary>
        /// Resumes music on all speakers
        /// </summary>
        public void ResumeMusic()
        {
            if (_speakers == null) return;

            foreach (AudioSource speaker in _speakers)
            {
                speaker.UnPause();
            }

            _isPlaying = true;
        }

        /// <summary>
        /// Changes the volume of all speakers
        /// </summary>
        public void SetVolume(float volume)
        {
            _volume = Mathf.Clamp01(volume);

            if (_speakers == null) return;

            foreach (AudioSource speaker in _speakers)
            {
                speaker.volume = _volume;
            }
        }

        /// <summary>
        /// Changes the entire playlist (stops current playback)
        /// </summary>
        public void ChangePlaylist(AudioClip[] newPlaylist)
        {
            if (newPlaylist == null || newPlaylist.Length == 0)
            {
                Debug.LogWarning("[AudioZoneManager] Trying to change to null/empty playlist");
                return;
            }

            bool wasPlaying = _isPlaying;

            StopMusic();
            _playlist = newPlaylist;
            _currentTrackIndex = 0;

            // Re-shuffle if in shuffle mode
            if (_playlistMode == PlaylistMode.Shuffle)
            {
                ShufflePlaylist();
            }

            if (wasPlaying)
            {
                PlayMusic();
            }
        }

        /// <summary>
        /// Adds a track to the playlist
        /// </summary>
        public void AddTrackToPlaylist(AudioClip newTrack)
        {
            if (newTrack == null)
            {
                Debug.LogWarning("[AudioZoneManager] Trying to add null track");
                return;
            }

            if (_playlist == null)
            {
                _playlist = new AudioClip[] { newTrack };
            }
            else
            {
                AudioClip[] newPlaylist = new AudioClip[_playlist.Length + 1];
                _playlist.CopyTo(newPlaylist, 0);
                newPlaylist[_playlist.Length] = newTrack;
                _playlist = newPlaylist;
            }

            Debug.Log($"[AudioZoneManager] Added '{newTrack.name}' to playlist. Playlist now has {_playlist.Length} tracks");
        }

        /// <summary>
        /// Gets the name of the currently playing track
        /// </summary>
        public string GetCurrentTrackName()
        {
            AudioClip current = GetCurrentTrack();
            return current != null ? current.name : "None";
        }

        /// <summary>
        /// Gets the current track index
        /// </summary>
        public int GetCurrentTrackIndex()
        {
            return _currentTrackIndex;
        }

        /// <summary>
        /// Gets the total number of tracks
        /// </summary>
        public int GetPlaylistLength()
        {
            return _playlist != null ? _playlist.Length : 0;
        }

        /// <summary>
        /// Refresh speaker list (call if you add/remove speakers at runtime)
        /// </summary>
        public void RefreshSpeakers()
        {
            bool wasPlaying = _isPlaying;

            if (wasPlaying)
            {
                StopMusic();
            }

            SetupSpeakers();

            if (wasPlaying)
            {
                PlayMusic();
            }
        }

        /// <summary>
        /// Draw debug information in Scene view
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw max audio range for each speaker
            AudioSource[] speakers = GetComponentsInChildren<AudioSource>();
            foreach (AudioSource speaker in speakers)
            {
                Gizmos.color = new Color(0, 1, 0, 0.1f);
                Gizmos.DrawWireSphere(speaker.transform.position, _maxDistance);

                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawWireSphere(speaker.transform.position, _minDistance);
            }
        }
    }
}
