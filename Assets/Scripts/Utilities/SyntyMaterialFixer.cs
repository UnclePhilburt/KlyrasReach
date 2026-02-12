/*
 * Synty Material Fixer for Unity 6 URP
 *
 * PURPOSE:
 * This editor utility automatically converts Synty materials that use custom shaders
 * (like SyntyStudios/Triplanar) to URP-compatible shaders. This fixes the "pink material"
 * issue that occurs when Synty assets are imported into Unity 6 with URP.
 *
 * HOW TO USE:
 * 1. In Unity, go to the top menu: Tools → Klyra's Reach → Fix Synty Materials
 * 2. The script will find all materials using incompatible shaders
 * 3. It will automatically convert them to Universal Render Pipeline/Lit shader
 * 4. Textures will be remapped to the correct URP material slots
 *
 * NOTE: This script only runs in the Unity Editor, not in builds.
 */

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace KlyrasReach.Utilities
{
    /// <summary>
    /// Editor utility class that fixes Synty materials to be compatible with URP in Unity 6
    /// </summary>
    public class SyntyMaterialFixer : EditorWindow
    {
        // Tracks how many materials were successfully fixed
        private int _materialsFixed = 0;

        // List of Synty shader names that need to be converted
        private static readonly string[] SYNTY_SHADER_NAMES = new string[]
        {
            "SyntyStudios/Triplanar",
            "SyntyStudios/EnvTriplanar",
            "SyntyStudios/Standard",
            "SyntyStudios/PolyArt",
            "SyntyStudios/SpaceShip_Rim",
            "SyntyStudios/SpaceShip",
            "SyntyStudios/Planet",
            "SyntyStudios/SkyboxUnlit",
            "Synty/Standard",
            "Synty/PolyArt"
        };

        /// <summary>
        /// Creates the menu item in Unity's top menu bar
        /// This allows users to access the tool via: Tools → Klyra's Reach → Fix Synty Materials
        /// </summary>
        [MenuItem("Tools/Klyra's Reach/Fix Synty Materials")]
        public static void ShowWindow()
        {
            // Opens the utility window
            EditorWindow window = GetWindow(typeof(SyntyMaterialFixer));
            window.titleContent = new GUIContent("Synty Material Fixer");
            window.minSize = new Vector2(400, 200);
        }

        /// <summary>
        /// Draws the GUI for the editor window
        /// This is called automatically by Unity when the window is visible
        /// </summary>
        void OnGUI()
        {
            // Add some padding around the content
            GUILayout.Space(10);

            // Title
            GUILayout.Label("Synty Material Fixer for URP", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Explanation text
            EditorGUILayout.HelpBox(
                "This tool will automatically convert Synty materials that use custom shaders " +
                "(like Triplanar) to URP-compatible shaders. This fixes pink/magenta materials.",
                MessageType.Info
            );

            GUILayout.Space(10);

            // The main button that starts the fixing process
            if (GUILayout.Button("Fix All Synty Materials", GUILayout.Height(40)))
            {
                FixAllMaterials();
            }

            GUILayout.Space(10);

            // Show results if any materials were fixed
            if (_materialsFixed > 0)
            {
                EditorGUILayout.HelpBox(
                    $"Successfully fixed {_materialsFixed} material(s)!",
                    MessageType.Info
                );
            }
        }

        /// <summary>
        /// Main function that finds and fixes all Synty materials in the project
        /// </summary>
        private void FixAllMaterials()
        {
            // Reset counter
            _materialsFixed = 0;

            // Find all material assets in the project
            // "t:Material" tells Unity to search for Material assets only
            string[] materialGuids = AssetDatabase.FindAssets("t:Material");

            Debug.Log($"[Synty Fixer] Found {materialGuids.Length} total materials in project. Checking for Synty materials...");

            // Loop through each material we found
            foreach (string guid in materialGuids)
            {
                // Convert the GUID to an actual file path
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // Load the material from the path
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                // Check if this material needs fixing
                if (material != null && NeedsFix(material))
                {
                    // Fix the material
                    FixMaterial(material);
                    _materialsFixed++;
                }
            }

            // Save all changes to disk
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Show results in the console
            Debug.Log($"[Synty Fixer] Complete! Fixed {_materialsFixed} material(s).");
        }

        /// <summary>
        /// Checks if a material uses a Synty shader that needs to be converted
        /// </summary>
        /// <param name="material">The material to check</param>
        /// <returns>True if the material needs fixing, false otherwise</returns>
        private bool NeedsFix(Material material)
        {
            // Get the shader name from the material
            string shaderName = material.shader.name;

            // Check if the shader name matches any of the Synty shaders we know about
            foreach (string syntyShaderName in SYNTY_SHADER_NAMES)
            {
                if (shaderName.Contains(syntyShaderName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Converts a Synty material to use URP/Lit shader and remaps textures
        /// </summary>
        /// <param name="material">The material to fix</param>
        private void FixMaterial(Material material)
        {
            // Store the old shader name for logging
            string oldShaderName = material.shader.name;

            // Save existing textures before changing shader
            // (Changing shaders can sometimes clear texture assignments)
            Texture mainTexture = material.GetTexture("_MainTex");
            Texture normalMap = material.GetTexture("_BumpMap");
            Texture metallicMap = material.GetTexture("_MetallicGlossMap");

            // Try to get color, but default to white if property doesn't exist
            Color mainColor = Color.white;
            if (material.HasProperty("_Color"))
            {
                mainColor = material.GetColor("_Color");
            }

            // Find the URP/Lit shader
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");

            if (urpLitShader == null)
            {
                Debug.LogError("[Synty Fixer] Could not find URP/Lit shader! Make sure you're using URP.");
                return;
            }

            // Assign the new shader to the material
            material.shader = urpLitShader;

            // Remap textures to URP shader slots
            if (mainTexture != null)
            {
                material.SetTexture("_BaseMap", mainTexture); // URP uses _BaseMap instead of _MainTex
            }

            if (normalMap != null)
            {
                material.SetTexture("_BumpMap", normalMap); // Normal map stays the same
            }

            if (metallicMap != null)
            {
                material.SetTexture("_MetallicGlossMap", metallicMap);
            }

            // Set the base color
            material.SetColor("_BaseColor", mainColor); // URP uses _BaseColor instead of _Color

            // Mark the material as modified so Unity saves the changes
            EditorUtility.SetDirty(material);

            Debug.Log($"[Synty Fixer] Fixed material: {material.name} ({oldShaderName} → {urpLitShader.name})");
        }
    }
}
#endif
