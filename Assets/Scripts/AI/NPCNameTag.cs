/*
 * NPC Name Tag for Klyra's Reach
 *
 * PURPOSE:
 * Shows the NPC's name when the player looks at them.
 * Similar to destination markers but for AI characters.
 *
 * HOW TO USE:
 * 1. Add this script to your AI character GameObject (or CharacterAIController adds it automatically)
 * 2. The name will automatically pull from CharacterAIController if present
 * 3. Look at an NPC - their name appears above their head
 */

using UnityEngine;

namespace KlyrasReach.AI
{
    /// <summary>
    /// Displays an NPC's name when the player looks at them
    /// </summary>
    public class NPCNameTag : MonoBehaviour
    {
        [Header("Display Settings")]
        [Tooltip("Name to display (auto-filled from CharacterAIController if present)")]
        [SerializeField] private string _displayName = "NPC";

        [Tooltip("Role/title to display under name (auto-filled from CharacterAIController if present)")]
        [SerializeField] private string _displayRole = "";

        [Tooltip("Maximum distance to show name tag")]
        [SerializeField] private float _maxDisplayDistance = 10f;

        [Tooltip("Angle within which player must be looking to see name (degrees)")]
        [SerializeField] private float _viewAngle = 60f;

        [Header("Debug")]
        [Tooltip("Show debug logs?")]
        [SerializeField] private bool _debugMode = false;

        [Tooltip("How far above character's head to show name")]
        [SerializeField] private float _heightOffset = 0.5f;

        [Tooltip("Font size for the name")]
        [SerializeField] private int _nameFontSize = 18;

        [Tooltip("Font size for the role")]
        [SerializeField] private int _roleFontSize = 14;

        [Tooltip("Color of the name text")]
        [SerializeField] private Color _nameColor = new Color(0.4f, 1f, 0.4f); // Green like CoD MW2

        [Tooltip("Color of the role text")]
        [SerializeField] private Color _roleColor = new Color(0.6f, 0.9f, 0.6f); // Lighter green

        [Tooltip("Outline thickness (higher = thicker outline)")]
        [SerializeField] private int _outlineThickness = 2;

        // Private variables
        private Camera _mainCamera;
        private CharacterAIController _aiController;
        private bool _isPlayerLookingAt = false;
        private Transform _headTransform; // Try to find the character's head bone

        /// <summary>
        /// Initialize references and create UI textures
        /// </summary>
        private void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogWarning("[NPCNameTag] No main camera found!");
            }

            // Try to get CharacterAIController and pull name/role from it
            _aiController = GetComponent<CharacterAIController>();
            if (_aiController != null)
            {
                _displayName = _aiController.GetCharacterName();
                _displayRole = _aiController.GetCharacterRole();
            }

            // Try to find head bone for better positioning
            FindHeadBone();
        }

        /// <summary>
        /// Try to find the character's head bone for better name tag positioning
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
                    return;
                }
            }

            // If no head found, use root transform
            _headTransform = transform;
        }

        /// <summary>
        /// Check if player is looking at this NPC using raycast
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
                // Try to find camera again
                _mainCamera = Camera.main;
                if (_mainCamera == null)
                {
                    _isPlayerLookingAt = false;
                    return;
                }
            }

            // Cast a ray from the center of the screen forward
            Ray ray = _mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
            RaycastHit hit;

            // Raycast forward up to max distance
            if (Physics.Raycast(ray, out hit, _maxDisplayDistance))
            {
                // Check if the hit object is this NPC or a child of this NPC
                Transform hitTransform = hit.transform;
                bool hitThisNPC = false;

                // Walk up the parent hierarchy to see if we hit this NPC
                while (hitTransform != null)
                {
                    if (hitTransform == transform)
                    {
                        hitThisNPC = true;
                        break;
                    }
                    hitTransform = hitTransform.parent;
                }

                if (hitThisNPC)
                {
                    // Player is looking directly at this NPC!
                    _isPlayerLookingAt = true;
                }
                else
                {
                    // Raycast hit something else
                    _isPlayerLookingAt = false;
                }
            }
            else
            {
                // Raycast didn't hit anything
                _isPlayerLookingAt = false;
            }
        }

        /// <summary>
        /// Draw the name tag UI
        /// </summary>
        private void OnGUI()
        {
            // Only show when player is looking at this NPC
            if (!_isPlayerLookingAt || _mainCamera == null)
            {
                return;
            }

            // Get position above character's head
            Vector3 worldPosition = _headTransform.position + Vector3.up * _heightOffset;

            // Convert to screen position
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPosition);

            // Only draw if in front of camera
            if (screenPos.z > 0)
            {
                // Set up styles - CoD MW2 style
                GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
                nameStyle.alignment = TextAnchor.MiddleCenter;
                nameStyle.fontSize = _nameFontSize;
                nameStyle.fontStyle = FontStyle.Bold;

                // Calculate sizes
                GUIContent nameContent = new GUIContent(_displayName);
                Vector2 nameSize = nameStyle.CalcSize(nameContent);

                // Calculate name position (centered above head)
                Rect nameRect = new Rect(
                    screenPos.x - nameSize.x / 2,
                    Screen.height - screenPos.y - nameSize.y / 2,
                    nameSize.x,
                    nameSize.y
                );

                // Draw thick black outline for name (CoD MW2 style - multiple passes)
                GUI.color = Color.black;
                for (int x = -_outlineThickness; x <= _outlineThickness; x++)
                {
                    for (int y = -_outlineThickness; y <= _outlineThickness; y++)
                    {
                        if (x != 0 || y != 0) // Skip center
                        {
                            GUI.Label(new Rect(nameRect.x + x, nameRect.y + y, nameRect.width, nameRect.height), _displayName, nameStyle);
                        }
                    }
                }

                // Draw green name text
                GUI.color = _nameColor;
                GUI.Label(nameRect, _displayName, nameStyle);

                // Reset color
                GUI.color = Color.white;
            }
        }

        /// <summary>
        /// Public method to check if player is currently looking at this NPC
        /// </summary>
        public bool IsPlayerLookingAt()
        {
            return _isPlayerLookingAt;
        }

        /// <summary>
        /// Update display name at runtime
        /// </summary>
        public void SetDisplayName(string name)
        {
            _displayName = name;
        }

        /// <summary>
        /// Update display role at runtime
        /// </summary>
        public void SetDisplayRole(string role)
        {
            _displayRole = role;
        }

        /// <summary>
        /// Draw debug gizmos in Scene view
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw detection range sphere
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _maxDisplayDistance);

            // Draw name tag position
            if (_headTransform != null)
            {
                Vector3 tagPos = _headTransform.position + Vector3.up * _heightOffset;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(tagPos, 0.2f);
                Gizmos.DrawLine(transform.position, tagPos);
            }

            // Show if currently being looked at
            if (_isPlayerLookingAt)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);
            }
        }
    }
}
