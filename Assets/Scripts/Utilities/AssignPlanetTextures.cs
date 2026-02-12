/*
 * Assign Planet Textures Utility for Klyra's Reach
 *
 * PURPOSE:
 * Automatically assigns the planet texture to all planet materials
 * that are missing their Base Map texture.
 *
 * HOW TO USE:
 * 1. Go to: Tools → Klyra's Reach → Assign Planet Textures
 * 2. Click "Assign Textures to All Planets"
 */

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace KlyrasReach.Utilities
{
    /// <summary>
    /// Assigns textures to planet materials
    /// </summary>
    public class AssignPlanetTextures : EditorWindow
    {
        private int _materialsFixed = 0;

        [MenuItem("Tools/Klyra's Reach/Assign Planet Textures")]
        public static void ShowWindow()
        {
            AssignPlanetTextures window = GetWindow<AssignPlanetTextures>();
            window.titleContent = new GUIContent("Assign Planet Textures");
            window.minSize = new Vector2(400, 200);
        }

        void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("Assign Planet Textures", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This will assign the PolygonSciFiSpace_PlanetLines texture to all planet materials.",
                MessageType.Info
            );

            GUILayout.Space(10);

            if (GUILayout.Button("Assign Textures to All Planets", GUILayout.Height(40)))
            {
                AssignTextures();
            }

            GUILayout.Space(10);

            if (_materialsFixed > 0)
            {
                EditorGUILayout.HelpBox(
                    $"Successfully assigned textures to {_materialsFixed} planet material(s)!",
                    MessageType.Info
                );
            }
        }

        private void AssignTextures()
        {
            _materialsFixed = 0;

            // Find the planet texture
            Texture planetTexture = AssetDatabase.LoadAssetAtPath<Texture>(
                "Assets/PolygonSciFiSpace/Textures/PolygonSciFiSpace_PlanetLines_01.png"
            );

            if (planetTexture == null)
            {
                EditorUtility.DisplayDialog(
                    "Texture Not Found",
                    "Could not find PolygonSciFiSpace_PlanetLines_01.png texture!",
                    "OK"
                );
                return;
            }

            // Find all materials
            string[] materialGuids = AssetDatabase.FindAssets("t:Material");

            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                // Check if it's a planet material (name contains "Planet")
                if (material != null && material.name.Contains("Planet"))
                {
                    // Check if Base Map is empty
                    if (material.GetTexture("_BaseMap") == null)
                    {
                        material.SetTexture("_BaseMap", planetTexture);

                        // Also set a nice color tint (random planet color)
                        Color planetColor = GetRandomPlanetColor();
                        material.SetColor("_BaseColor", planetColor);

                        EditorUtility.SetDirty(material);
                        _materialsFixed++;

                        Debug.Log($"[Planet Texture Assigner] Assigned texture to: {material.name} with color {planetColor}");
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Planet Texture Assigner] Complete! Fixed {_materialsFixed} material(s).");
        }

        /// <summary>
        /// Returns a random planet-like color
        /// </summary>
        private Color GetRandomPlanetColor()
        {
            Color[] planetColors = new Color[]
            {
                new Color(0.3f, 0.5f, 0.8f),  // Blue (water world)
                new Color(0.6f, 0.4f, 0.2f),  // Brown (desert)
                new Color(0.5f, 0.7f, 0.3f),  // Green (earth-like)
                new Color(0.8f, 0.4f, 0.3f),  // Red/Orange (mars-like)
                new Color(0.7f, 0.7f, 0.5f),  // Yellow (gas giant)
                new Color(0.5f, 0.3f, 0.6f),  // Purple (alien)
                new Color(0.4f, 0.6f, 0.7f),  // Cyan (ice world)
                new Color(0.6f, 0.6f, 0.6f),  // Gray (rocky/moon)
            };

            return planetColors[Random.Range(0, planetColors.Length)];
        }
    }
}
#endif
