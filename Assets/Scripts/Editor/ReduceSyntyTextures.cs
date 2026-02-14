/*
 * Reduce Synty Texture Quality
 *
 * PURPOSE:
 * Batch-processes all Synty textures to reduce memory usage for browser performance
 *
 * HOW TO USE:
 * 1. In Unity menu: Tools > Reduce Synty Texture Quality
 * 2. Wait for processing to complete
 * 3. Rebuild WebGL
 */

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace KlyrasReach.Editor
{
    public class ReduceSyntyTextures : EditorWindow
    {
        private int _maxTextureSize = 512;
        private int _smallTextureSize = 256;
        private bool _applyCompression = true;
        private bool _processingComplete = false;
        private int _processedCount = 0;

        [MenuItem("Tools/Reduce Synty Texture Quality")]
        public static void ShowWindow()
        {
            GetWindow<ReduceSyntyTextures>("Reduce Synty Textures");
        }

        private void OnGUI()
        {
            GUILayout.Label("Reduce Synty Texture Quality", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This will reduce texture quality on all Synty assets to improve browser performance.\n\n" +
                "WARNING: This will modify your texture import settings. Make a backup first!",
                MessageType.Warning);

            GUILayout.Space(10);

            _maxTextureSize = EditorGUILayout.IntPopup(
                "Max Texture Size (Large)",
                _maxTextureSize,
                new string[] { "256", "512", "1024" },
                new int[] { 256, 512, 1024 });

            _smallTextureSize = EditorGUILayout.IntPopup(
                "Max Texture Size (Small)",
                _smallTextureSize,
                new string[] { "128", "256", "512" },
                new int[] { 128, 256, 512 });

            _applyCompression = EditorGUILayout.Toggle("Apply Compression", _applyCompression);

            GUILayout.Space(10);

            if (_processingComplete)
            {
                EditorGUILayout.HelpBox($"Processing complete! Reduced quality on {_processedCount} textures.", MessageType.Info);
            }

            if (GUILayout.Button("Process All Synty Textures", GUILayout.Height(40)))
            {
                ProcessTextures();
            }
        }

        private void ProcessTextures()
        {
            _processingComplete = false;
            _processedCount = 0;

            // Find all texture files in Synty folders
            string[] folderPaths = new string[]
            {
                "Assets/PolygonSciFiSpace/Textures",
                "Assets/PolygonSciFiWorlds/Textures",
                "Assets/Synty"
            };

            List<string> allTexturePaths = new List<string>();

            foreach (string folder in folderPaths)
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    allTexturePaths.Add(path);
                }
            }

            Debug.Log($"[ReduceSyntyTextures] Found {allTexturePaths.Count} textures to process");

            int progress = 0;
            foreach (string texturePath in allTexturePaths)
            {
                progress++;
                EditorUtility.DisplayProgressBar(
                    "Reducing Texture Quality",
                    $"Processing {progress}/{allTexturePaths.Count}: {System.IO.Path.GetFileName(texturePath)}",
                    (float)progress / allTexturePaths.Count);

                if (ProcessSingleTexture(texturePath))
                {
                    _processedCount++;
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _processingComplete = true;
            Debug.Log($"[ReduceSyntyTextures] Complete! Processed {_processedCount} textures.");
        }

        private bool ProcessSingleTexture(string texturePath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null) return false;

            bool modified = false;

            // Get current texture to check its size
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null) return false;

            int originalWidth = texture.width;
            int originalHeight = texture.height;
            int maxDimension = Mathf.Max(originalWidth, originalHeight);

            // Determine appropriate max size based on original dimensions
            int targetMaxSize;
            if (maxDimension <= 512)
            {
                targetMaxSize = _smallTextureSize; // Small textures
            }
            else
            {
                targetMaxSize = _maxTextureSize; // Large textures
            }

            // Check if we need to modify this texture
            if (importer.maxTextureSize > targetMaxSize || !importer.isReadable)
            {
                // Set max texture size
                importer.maxTextureSize = targetMaxSize;

                // Apply compression
                if (_applyCompression)
                {
                    // Use automatic compression based on platform
                    TextureImporterPlatformSettings platformSettings = importer.GetDefaultPlatformTextureSettings();
                    platformSettings.format = TextureImporterFormat.Automatic;
                    platformSettings.textureCompression = TextureImporterCompression.Compressed;
                    importer.SetPlatformTextureSettings(platformSettings);
                }

                // Enable mipmaps for performance
                if (importer.textureType == TextureImporterType.Default)
                {
                    importer.mipmapEnabled = true;
                }

                // Mark as modified and reimport
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                modified = true;

                Debug.Log($"[ReduceSyntyTextures] Reduced: {texturePath} ({originalWidth}x{originalHeight} -> max {targetMaxSize})");
            }

            return modified;
        }
    }
}
#endif
