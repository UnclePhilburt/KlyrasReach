/*
 * Fix Sci-Fi Worlds Materials for Klyra's Reach
 *
 * PURPOSE:
 * After converting Sci-Fi Worlds materials to URP, they often appear white
 * because textures aren't remapped. This tool finds and assigns the correct textures.
 *
 * HOW TO USE:
 * 1. Go to: Tools → Klyra's Reach → Fix Sci-Fi Worlds Materials
 * 2. Click "Fix All Sci-Fi Worlds Materials"
 */

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace KlyrasReach.Utilities
{
    public class FixSciFiWorldsMaterials : EditorWindow
    {
        private int _materialsFixed = 0;

        [MenuItem("Tools/Klyra's Reach/Fix Sci-Fi Worlds Materials")]
        public static void ShowWindow()
        {
            FixSciFiWorldsMaterials window = GetWindow<FixSciFiWorldsMaterials>();
            window.titleContent = new GUIContent("Fix Sci-Fi Worlds Materials");
            window.minSize = new Vector2(400, 200);
        }

        void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("Fix Sci-Fi Worlds Materials", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool assigns textures to Sci-Fi Worlds materials that appear white after URP conversion.",
                MessageType.Info
            );

            GUILayout.Space(10);

            if (GUILayout.Button("Fix All Sci-Fi Worlds Materials", GUILayout.Height(40)))
            {
                FixMaterials();
            }

            GUILayout.Space(10);

            if (_materialsFixed > 0)
            {
                EditorGUILayout.HelpBox(
                    $"Successfully fixed {_materialsFixed} material(s)!",
                    MessageType.Info
                );
            }
        }

        private void FixMaterials()
        {
            _materialsFixed = 0;

            // Find all materials with "SciFiWorlds" or "PolygonSciFiWorlds" in the name
            string[] materialGuids = AssetDatabase.FindAssets("t:Material PolygonSciFiWorlds");

            Debug.Log($"[Sci-Fi Worlds Fixer] Found {materialGuids.Length} Sci-Fi Worlds materials...");

            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material != null)
                {
                    if (FixMaterial(material))
                    {
                        _materialsFixed++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Sci-Fi Worlds Fixer] Complete! Fixed {_materialsFixed} material(s).");
        }

        private bool FixMaterial(Material material)
        {
            bool wasFixed = false;

            // Skip materials that aren't using URP shaders (like Unlit/Texture for skyboxes)
            if (!material.shader.name.Contains("Universal Render Pipeline"))
            {
                return false;
            }

            // Check if material has no base map assigned
            if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") == null)
            {
                // Try to find texture with similar name
                string materialName = material.name;

                // Common texture name patterns in Sci-Fi Worlds
                string[] textureSearchPatterns = new string[]
                {
                    materialName.Replace("_Mat", "_Texture"),
                    materialName.Replace("Mat", "Texture"),
                    materialName,
                    "PolygonSciFiWorlds_Texture_01_A" // Default texture
                };

                Texture2D foundTexture = null;

                foreach (string pattern in textureSearchPatterns)
                {
                    foundTexture = FindTexture(pattern);
                    if (foundTexture != null)
                    {
                        break;
                    }
                }

                // If we found a texture, assign it
                if (foundTexture != null)
                {
                    material.SetTexture("_BaseMap", foundTexture);
                    material.SetColor("_BaseColor", Color.white);
                    EditorUtility.SetDirty(material);
                    Debug.Log($"[Sci-Fi Worlds Fixer] Assigned texture to: {material.name}");
                    wasFixed = true;
                }
                else
                {
                    // No matching texture found, just set a reasonable color
                    material.SetColor("_BaseColor", new Color(0.8f, 0.8f, 0.8f));
                    EditorUtility.SetDirty(material);
                    Debug.LogWarning($"[Sci-Fi Worlds Fixer] No texture found for: {material.name}, set to gray");
                    wasFixed = true;
                }
            }

            return wasFixed;
        }

        private Texture2D FindTexture(string textureName)
        {
            // Search for texture in project
            string[] textureGuids = AssetDatabase.FindAssets($"{textureName} t:Texture2D");

            if (textureGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(textureGuids[0]);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }

            return null;
        }
    }
}
#endif
