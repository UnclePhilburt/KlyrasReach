/*
 * NPC Dialogue System for Klyra's Reach
 *
 * PURPOSE:
 * Allows NPCs to speak voice lines when player approaches and presses F.
 * Shows "Press F to Talk" prompt and displays subtitles.
 *
 * HOW TO USE:
 * 1. Add this script to your AI character
 * 2. Add voice line audio clips
 * 3. Type what they say for each voice line (subtitles)
 * 4. Walk up to NPC and press F to hear them talk
 */

using UnityEngine;
using UnityEngine.InputSystem;

namespace KlyrasReach.AI
{
    /// <summary>
    /// Single dialogue line with audio and subtitle
    /// </summary>
    [System.Serializable]
    public class DialogueLine
    {
        [Tooltip("Audio clip for this voice line")]
        public AudioClip audioClip;

        [Tooltip("Text to display as subtitle")]
        [TextArea(2, 4)]
        public string subtitleText;
    }

    /// <summary>
    /// Handles NPC dialogue with voice lines and subtitles
    /// </summary>
    public class NPCDialogue : MonoBehaviour
    {
        [Header("Dialogue Lines")]
        [Tooltip("List of voice lines this NPC can say")]
        [SerializeField] private DialogueLine[] _dialogueLines;

        [Header("Interaction Settings")]
        [Tooltip("How close player needs to be to talk")]
        [SerializeField] private float _interactionRange = 5f;

        [Tooltip("Key to press to talk")]
        [SerializeField] private Key _talkKey = Key.F;

        [Header("Audio Settings")]
        [Tooltip("Volume for voice lines (0-1)")]
        [SerializeField] [Range(0f, 1f)] private float _voiceVolume = 1f;

        [Tooltip("Is the voice 3D spatial audio?")]
        [SerializeField] private bool _spatialAudio = true;

        [Header("UI Settings")]
        [Tooltip("Color for the 'Press F' prompt")]
        [SerializeField] private Color _promptColor = Color.white;

        [Tooltip("Color for subtitle text")]
        [SerializeField] private Color _subtitleColor = Color.white;

        [Tooltip("Font size for prompt")]
        [SerializeField] private int _promptFontSize = 16;

        [Header("Debug")]
        [Tooltip("Show debug logs?")]
        [SerializeField] private bool _debugMode = true;

        [Tooltip("Font size for subtitles")]
        [SerializeField] private int _subtitleFontSize = 18;

        [Tooltip("How long to show subtitles after audio finishes (seconds)")]
        [SerializeField] private float _subtitleLingerTime = 1f;

        // Private variables
        private Camera _mainCamera;
        private AudioSource _audioSource;
        private bool _playerInRange = false;
        private bool _isPlaying = false;
        private string _currentSubtitle = "";
        private float _subtitleEndTime = 0f;
        private CharacterAIController _aiController;
        private Transform _headTransform; // Try to find the character's head bone
        private Texture2D _circleTexture; // Circle background texture

        /// <summary>
        /// Initialize components
        /// </summary>
        private void Start()
        {
            _mainCamera = Camera.main;

            // Get or add AudioSource
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _audioSource.playOnAwake = false;
            _audioSource.volume = _voiceVolume;
            _audioSource.spatialBlend = _spatialAudio ? 1f : 0f;

            // Get AI controller for character name
            _aiController = GetComponent<CharacterAIController>();

            // Try to find head bone for better positioning
            FindHeadBone();

            // Create circle texture
            CreateCircleTexture();

            if (_dialogueLines == null || _dialogueLines.Length == 0)
            {
                Debug.LogWarning($"[NPCDialogue] '{gameObject.name}' has no dialogue lines!");
            }
            else
            {
                Debug.Log($"[NPCDialogue] '{gameObject.name}' initialized with {_dialogueLines.Length} dialogue line(s)");
            }
        }

        /// <summary>
        /// Create a circular texture for the prompt icon
        /// </summary>
        private void CreateCircleTexture()
        {
            int size = 128;
            _circleTexture = new Texture2D(size, size);
            float radius = size / 2f;
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);

                    if (distance <= radius - 4)
                    {
                        // Inside circle - white
                        _circleTexture.SetPixel(x, y, Color.white);
                    }
                    else if (distance <= radius)
                    {
                        // Border area - black
                        _circleTexture.SetPixel(x, y, Color.black);
                    }
                    else
                    {
                        // Outside circle - transparent
                        _circleTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                }
            }

            _circleTexture.Apply();
        }

        /// <summary>
        /// Try to find the character's head bone for better icon positioning
        /// </summary>
        private void FindHeadBone()
        {
            // Look for common head bone names in the character's hierarchy
            Transform[] allTransforms = GetComponentsInChildren<Transform>();
            foreach (Transform t in allTransforms)
            {
                string boneName = t.name.ToLower();
                if (boneName.Contains("head") || boneName.Contains("neck"))
                {
                    _headTransform = t;
                    Debug.Log($"[NPCDialogue] Found head bone: {t.name}");
                    return;
                }
            }

            // If no head found, use root transform
            _headTransform = transform;
            Debug.Log($"[NPCDialogue] No head bone found, using root transform");
        }

        /// <summary>
        /// Check if player is looking at NPC using raycast (same as NPCNameTag)
        /// OPTIMIZED: Only raycast every 10 frames instead of every frame
        /// </summary>
        private void Update()
        {
            // PERFORMANCE: Only check every 10 frames (massive performance gain!)
            if (Time.frameCount % 10 != 0)
            {
                return;
            }

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null)
                    return;
            }

            if (_dialogueLines == null || _dialogueLines.Length == 0)
                return;

            // Cast a ray from the center of the screen forward
            Ray ray = _mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
            RaycastHit hit;

            bool wasInRange = _playerInRange;
            _playerInRange = false;

            // Raycast forward up to interaction range
            if (Physics.Raycast(ray, out hit, _interactionRange))
            {
                // Check if the hit object is this NPC or a child of this NPC
                Transform hitTransform = hit.transform;

                // Walk up the parent hierarchy to see if we hit this NPC
                while (hitTransform != null)
                {
                    if (hitTransform == transform)
                    {
                        // Player is looking directly at this NPC!
                        _playerInRange = true;
                        break;
                    }
                    hitTransform = hitTransform.parent;
                }
            }

            // Debug log when player enters/exits range
            if (_debugMode && _playerInRange != wasInRange)
            {
                if (_playerInRange)
                {
                    Debug.Log($"[NPCDialogue] Player looking at '{gameObject.name}' - raycast hit at {hit.distance:F2}m");
                }
                else
                {
                    Debug.Log($"[NPCDialogue] Player stopped looking at '{gameObject.name}'");
                }
            }

            // Check for talk key press
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && _playerInRange && !_isPlaying && keyboard[_talkKey].wasPressedThisFrame)
            {
                if (_debugMode)
                {
                    Debug.Log($"[NPCDialogue] {_talkKey} pressed! Playing dialogue...");
                }
                PlayRandomDialogue();
            }

            // Check if audio finished playing
            if (_isPlaying && !_audioSource.isPlaying)
            {
                // Audio finished, start subtitle linger timer
                if (_subtitleEndTime == 0f)
                {
                    _subtitleEndTime = Time.time + _subtitleLingerTime;
                }

                // Clear subtitle after linger time
                if (Time.time >= _subtitleEndTime)
                {
                    _isPlaying = false;
                    _currentSubtitle = "";
                    _subtitleEndTime = 0f;
                }
            }
        }

        /// <summary>
        /// Play a random dialogue line
        /// </summary>
        private void PlayRandomDialogue()
        {
            if (_dialogueLines == null || _dialogueLines.Length == 0)
                return;

            // Pick a random dialogue line
            int randomIndex = Random.Range(0, _dialogueLines.Length);
            DialogueLine line = _dialogueLines[randomIndex];

            if (line.audioClip == null)
            {
                Debug.LogWarning($"[NPCDialogue] '{gameObject.name}' dialogue line {randomIndex} has no audio clip!");
                return;
            }

            // Play the audio
            _audioSource.clip = line.audioClip;
            _audioSource.Play();

            // Show the subtitle
            _currentSubtitle = line.subtitleText;
            _isPlaying = true;
            _subtitleEndTime = 0f;

            string npcName = _aiController != null ? _aiController.GetCharacterName() : gameObject.name;
            Debug.Log($"[NPCDialogue] '{npcName}' says: \"{_currentSubtitle}\"");
        }

        /// <summary>
        /// Draw UI prompt and subtitles
        /// </summary>
        private void OnGUI()
        {
            if (_mainCamera == null)
                return;

            // Draw "Press F to Talk" prompt when in range
            if (_playerInRange && !_isPlaying)
            {
                if (_debugMode && Event.current.type == EventType.Repaint && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[NPCDialogue] OnGUI drawing prompt for '{gameObject.name}'");
                }
                DrawPrompt();
            }

            // Draw subtitles when dialogue is playing
            if (_isPlaying && !string.IsNullOrEmpty(_currentSubtitle))
            {
                DrawSubtitles();
            }
        }

        /// <summary>
        /// Draw the "Press F to Talk" prompt on NPC body
        /// </summary>
        private void DrawPrompt()
        {
            // Get position near NPC's head (like NPCNameTag does)
            Vector3 worldPosition = _headTransform.position + Vector3.up * 0.3f;
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPosition);

            // Only draw if in front of camera
            if (screenPos.z > 0)
            {
                float iconSize = 60f;

                Rect iconRect = new Rect(
                    screenPos.x - iconSize / 2f,
                    Screen.height - screenPos.y - iconSize / 2f,
                    iconSize,
                    iconSize
                );

                // Draw circular background
                if (_circleTexture != null)
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(iconRect, _circleTexture);
                }

                // Draw F letter
                GUIStyle fStyle = new GUIStyle(GUI.skin.label);
                fStyle.alignment = TextAnchor.MiddleCenter;
                fStyle.fontSize = 36;
                fStyle.fontStyle = FontStyle.Bold;

                // Draw F with slight shadow for depth
                GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                GUI.Label(new Rect(iconRect.x + 2, iconRect.y + 2, iconRect.width, iconRect.height), "F", fStyle);

                // Draw F letter (black)
                GUI.color = Color.black;
                GUI.Label(iconRect, "F", fStyle);

                GUI.color = Color.white;
            }
        }

        /// <summary>
        /// Draw subtitles at the bottom of the screen
        /// </summary>
        private void DrawSubtitles()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = _subtitleFontSize;
            style.fontStyle = FontStyle.Bold;

            // Get NPC name for subtitle
            string npcName = _aiController != null ? _aiController.GetCharacterName() : "NPC";
            string fullText = $"{npcName}: {_currentSubtitle}";

            GUIContent content = new GUIContent(fullText);
            Vector2 size = style.CalcSize(content);

            // Position at bottom center of screen
            Rect subtitleRect = new Rect(
                Screen.width / 2 - size.x / 2,
                Screen.height - size.y - 100, // 100 pixels from bottom
                size.x,
                size.y
            );

            // Draw semi-transparent black background
            Rect backgroundRect = new Rect(
                subtitleRect.x - 20,
                subtitleRect.y - 10,
                subtitleRect.width + 40,
                subtitleRect.height + 20
            );

            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.Box(backgroundRect, "");

            // Draw thick black outline
            GUI.color = Color.black;
            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    if (x != 0 || y != 0)
                    {
                        GUI.Label(new Rect(subtitleRect.x + x, subtitleRect.y + y, subtitleRect.width, subtitleRect.height), fullText, style);
                    }
                }
            }

            // Draw subtitle text
            GUI.color = _subtitleColor;
            GUI.Label(subtitleRect, fullText, style);

            GUI.color = Color.white;
        }

        /// <summary>
        /// Draw debug visualization in Scene view
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw interaction range
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
        }
    }
}
