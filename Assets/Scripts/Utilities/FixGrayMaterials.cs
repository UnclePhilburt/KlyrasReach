/*
 * Fix Gray Materials Utility for Klyra's Reach
 *
 * PURPOSE:
 * Some materials appear gray after URP conversion because their textures
 * aren't assigned properly or the shader settings are wrong.
 * This utility fixes gray materials by ensuring textures are mapped correctly.
 *
 * HOW TO USE:
 * 1. Go to: Tools → Klyra's Reach → Fix Gray Materials
 * 2. Click "Fix All Gray Materials"
 */

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace KlyrasReach.Utilities
{
    /// <summary>
    /// Editor utility that fixes materials appearing as solid gray
    /// </summary>
    public class FixGrayMaterials : EditorWindow
    {
        private int _materialsFixed = 0;

        /// <summary>
        /// Creates menu item: Tools → Klyra's Reach → Fix Gray Materials
        /// </summary>
        [MenuItem("Tools/Klyra's Reach/Fix Gray Materials")]
        public static void ShowWindow()
        {
            FixGrayMaterials window = GetWindow<FixGrayMaterials>();
            window.titleContent = new GUIContent("Fix Gray Materials");
            window.minSize = new Vector2(400, 200);
        }

        /// <summary>
        /// Draws the editor window GUI
        /// </summary>
        void OnGUI()
        {
            GUILayout.Space(10);

            GUILayout.Label("Fix Gray Materials", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool finds materials that appear gray and fixes their texture assignments and shader settings.",
                MessageType.Info
            );

            GUILayout.Space(10);

            if (GUILayout.Button("Fix All Gray Materials", GUILayout.Height(40)))
            {
                FixAllGrayMaterials();
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

        /// <summary>
        /// Finds and fixes all gray materials
        /// </summary>
        private void FixAllGrayMaterials()
        {
            _materialsFixed = 0;

            string[] materialGuids = AssetDatabase.FindAssets("t:Material");

            Debug.Log($"[Gray Material Fixer] Checking {materialGuids.Length} materials...");

            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material != null && NeedsFix(material))
                {
                    FixMaterial(material);
                    _materialsFixed++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Gray Material Fixer] Complete! Fixed {_materialsFixed} material(s).");
        }

        /// <summary>
        /// Checks if a material needs fixing
        /// </summary>
        private bool NeedsFix(Material material)
        {
            // Check if using URP Lit shader
            if (material.shader.name != "Universal Render Pipeline/Lit")
            {
                return false;
            }

            // Check if base map is missing or base color is gray
            bool hasNoTexture = material.GetTexture("_BaseMap") == null;
            Color baseColor = material.GetColor("_BaseColor");
            bool isGray = Mathf.Approximately(baseColor.r, baseColor.g) &&
                          Mathf.Approximately(baseColor.g, baseColor.b) &&
                          baseColor.r < 0.9f && baseColor.r > 0.4f; // Gray range

            return hasNoTexture || isGray;
        }

        /// <summary>
        /// Fixes a gray material
        /// </summary>
        private void FixMaterial(Material material)
        {
            // Try to find texture in old slots
            Texture mainTex = material.GetTexture("_MainTex");

            if (mainTex != null && material.GetTexture("_BaseMap") == null)
            {
                // Remap old _MainTex to new _BaseMap
                material.SetTexture("_BaseMap", mainTex);
                Debug.Log($"[Gray Material Fixer] Remapped texture for: {material.name}");
            }

            // Set base color to white if it's gray
            Color baseColor = material.GetColor("_BaseColor");
            if (Mathf.Approximately(baseColor.r, baseColor.g) &&
                Mathf.Approximately(baseColor.g, baseColor.b) &&
                baseColor.r < 0.9f)
            {
                material.SetColor("_BaseColor", Color.white);
                Debug.Log($"[Gray Material Fixer] Reset base color to white: {material.name}");
            }

            // Reset smoothness if too high (can make things look gray)
            if (material.HasProperty("_Smoothness"))
            {
                float smoothness = material.GetFloat("_Smoothness");
                if (smoothness > 0.8f)
                {
                    material.SetFloat("_Smoothness", 0.5f);
                    Debug.Log($"[Gray Material Fixer] Adjusted smoothness: {material.name}");
                }
            }

            EditorUtility.SetDirty(material);
        }
    }
}
#endif
