/*
 * Fix Dark Materials Utility for Klyra's Reach
 *
 * PURPOSE:
 * After converting Synty materials to URP, some materials may appear black or dark purple.
 * This is usually because the Base Color is set to black instead of white.
 * This utility fixes all materials with dark base colors.
 *
 * HOW TO USE:
 * 1. Go to: Tools → Klyra's Reach → Fix Dark Materials
 * 2. Click "Fix All Dark Materials"
 * 3. The script will find and fix materials with black/dark base colors
 */

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace KlyrasReach.Utilities
{
    /// <summary>
    /// Editor utility that fixes materials with black or very dark base colors
    /// </summary>
    public class FixDarkMaterials : EditorWindow
    {
        private int _materialsFixed = 0;
        private float _brightnessThreshold = 0.3f; // Colors darker than this will be fixed

        /// <summary>
        /// Creates menu item: Tools → Klyra's Reach → Fix Dark Materials
        /// </summary>
        [MenuItem("Tools/Klyra's Reach/Fix Dark Materials")]
        public static void ShowWindow()
        {
            FixDarkMaterials window = GetWindow<FixDarkMaterials>();
            window.titleContent = new GUIContent("Fix Dark Materials");
            window.minSize = new Vector2(400, 250);
        }

        /// <summary>
        /// Draws the editor window GUI
        /// </summary>
        void OnGUI()
        {
            GUILayout.Space(10);

            GUILayout.Label("Fix Dark Materials", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool finds materials with black or very dark base colors and sets them to white. " +
                "This fixes materials that appear too dark after URP conversion.",
                MessageType.Info
            );

            GUILayout.Space(10);

            // Slider to adjust brightness threshold
            GUILayout.Label("Brightness Threshold (materials darker than this will be fixed):");
            _brightnessThreshold = EditorGUILayout.Slider(_brightnessThreshold, 0f, 1f);

            GUILayout.Space(10);

            // Main button
            if (GUILayout.Button("Fix All Dark Materials", GUILayout.Height(40)))
            {
                FixAllDarkMaterials();
            }

            GUILayout.Space(10);

            // Show results
            if (_materialsFixed > 0)
            {
                EditorGUILayout.HelpBox(
                    $"Successfully fixed {_materialsFixed} material(s)!",
                    MessageType.Info
                );
            }
        }

        /// <summary>
        /// Finds all materials in the project and fixes dark ones
        /// </summary>
        private void FixAllDarkMaterials()
        {
            _materialsFixed = 0;

            // Find all materials in project
            string[] materialGuids = AssetDatabase.FindAssets("t:Material");

            Debug.Log($"[Dark Material Fixer] Checking {materialGuids.Length} materials...");

            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material != null && IsMaterialTooDark(material))
                {
                    FixMaterial(material);
                    _materialsFixed++;
                }
            }

            // Save changes
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Dark Material Fixer] Complete! Fixed {_materialsFixed} material(s).");
        }

        /// <summary>
        /// Checks if a material's base color is too dark
        /// </summary>
        /// <param name="material">Material to check</param>
        /// <returns>True if material is too dark</returns>
        private bool IsMaterialTooDark(Material material)
        {
            // Check if material has a _BaseColor property (URP Lit shader)
            if (material.HasProperty("_BaseColor"))
            {
                Color baseColor = material.GetColor("_BaseColor");

                // Calculate perceived brightness (weighted RGB formula)
                // Human eyes perceive green as brighter than red, and red brighter than blue
                float brightness = (baseColor.r * 0.299f + baseColor.g * 0.587f + baseColor.b * 0.114f);

                return brightness < _brightnessThreshold;
            }

            // Also check old _Color property (for materials not fully converted)
            if (material.HasProperty("_Color"))
            {
                Color color = material.GetColor("_Color");
                float brightness = (color.r * 0.299f + color.g * 0.587f + color.b * 0.114f);
                return brightness < _brightnessThreshold;
            }

            return false;
        }

        /// <summary>
        /// Fixes a dark material by setting its base color to white
        /// </summary>
        /// <param name="material">Material to fix</param>
        private void FixMaterial(Material material)
        {
            Color oldColor = Color.black;

            // Fix _BaseColor if it exists (URP)
            if (material.HasProperty("_BaseColor"))
            {
                oldColor = material.GetColor("_BaseColor");
                material.SetColor("_BaseColor", Color.white);
            }

            // Fix _Color if it exists (legacy)
            if (material.HasProperty("_Color"))
            {
                oldColor = material.GetColor("_Color");
                material.SetColor("_Color", Color.white);
            }

            // Also check for metallic being too high (can make things look dark/black)
            if (material.HasProperty("_Metallic"))
            {
                float metallic = material.GetFloat("_Metallic");
                if (metallic > 0.8f)
                {
                    // High metallic with dark color = black material
                    // Reduce it to a reasonable value
                    material.SetFloat("_Metallic", 0.2f);
                    Debug.Log($"[Dark Material Fixer] Reduced metallic on: {material.name}");
                }
            }

            EditorUtility.SetDirty(material);

            Debug.Log($"[Dark Material Fixer] Fixed material: {material.name} (was {oldColor}, now white)");
        }
    }
}
#endif
