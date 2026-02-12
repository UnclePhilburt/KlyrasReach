/*
 * Synty Collider Adder for Klyra's Reach
 *
 * PURPOSE:
 * Synty assets often don't include colliders on their prefabs/models. This causes
 * players to fall through floors, walls, etc. This utility automatically adds
 * mesh colliders to all Synty environment objects in your scene.
 *
 * HOW TO USE:
 * METHOD 1 - Entire Scene:
 *   1. Open your scene with the hangar/environment
 *   2. Go to: Tools → Klyra's Reach → Add Colliders to Scene
 *   3. All objects without colliders will get mesh colliders added
 *
 * METHOD 2 - Selected Objects Only:
 *   1. Select the objects you want to add colliders to (can multi-select)
 *   2. Right-click in Hierarchy → Klyra's Reach → Add Colliders to Selected
 *
 * NOTE: This only runs in the Unity Editor
 */

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace KlyrasReach.Utilities
{
    /// <summary>
    /// Editor utility that automatically adds colliders to Synty environment pieces
    /// </summary>
    public class SyntyColliderAdder : EditorWindow
    {
        // Tracks statistics
        private int _collidersAdded = 0;
        private int _objectsProcessed = 0;

        /// <summary>
        /// Menu item: Tools → Klyra's Reach → Add Colliders to Scene
        /// Adds colliders to ALL objects in the current scene
        /// </summary>
        [MenuItem("Tools/Klyra's Reach/Add Colliders to Scene")]
        public static void AddCollidersToScene()
        {
            SyntyColliderAdder window = GetWindow<SyntyColliderAdder>();
            window.titleContent = new GUIContent("Synty Collider Adder");
            window.minSize = new Vector2(400, 250);
        }

        /// <summary>
        /// Context menu item: Right-click in Hierarchy → Klyra's Reach → Add Colliders to Selected
        /// Adds colliders only to selected GameObjects
        /// </summary>
        [MenuItem("GameObject/Klyra's Reach/Add Colliders to Selected", false, 0)]
        public static void AddCollidersToSelected()
        {
            if (Selection.gameObjects.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Selection",
                    "Please select one or more GameObjects in the Hierarchy first.",
                    "OK"
                );
                return;
            }

            int added = 0;
            foreach (GameObject obj in Selection.gameObjects)
            {
                added += ProcessGameObjectAndChildren(obj);
            }

            Debug.Log($"[Collider Adder] Added {added} collider(s) to selected objects.");
            EditorUtility.DisplayDialog(
                "Complete",
                $"Added {added} collider(s) to selected objects.",
                "OK"
            );
        }

        /// <summary>
        /// Draws the GUI for the editor window
        /// </summary>
        void OnGUI()
        {
            GUILayout.Space(10);

            GUILayout.Label("Synty Collider Adder", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool adds mesh colliders to Synty environment objects that are missing them. " +
                "This prevents players from falling through floors and walls.",
                MessageType.Info
            );

            GUILayout.Space(10);

            // Button to add colliders to entire scene
            if (GUILayout.Button("Add Colliders to Entire Scene", GUILayout.Height(40)))
            {
                ProcessEntireScene();
            }

            GUILayout.Space(5);

            // Button to add colliders to selected objects only
            if (GUILayout.Button("Add Colliders to Selected Objects", GUILayout.Height(40)))
            {
                AddCollidersToSelected();
                Repaint(); // Refresh the window
            }

            GUILayout.Space(10);

            // Show results
            if (_collidersAdded > 0)
            {
                EditorGUILayout.HelpBox(
                    $"Processed {_objectsProcessed} object(s).\n" +
                    $"Added {_collidersAdded} collider(s)!",
                    MessageType.Info
                );
            }

            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "TIP: Select specific objects in the Hierarchy and use 'Add Colliders to Selected' " +
                "if you only want to add colliders to certain parts of your scene.",
                MessageType.None
            );
        }

        /// <summary>
        /// Processes every GameObject in the currently open scene
        /// </summary>
        private void ProcessEntireScene()
        {
            _collidersAdded = 0;
            _objectsProcessed = 0;

            // Find all GameObjects in the scene (root objects only)
            GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

            Debug.Log($"[Collider Adder] Processing scene with {rootObjects.Length} root object(s)...");

            // Process each root object and all its children
            foreach (GameObject rootObj in rootObjects)
            {
                _collidersAdded += ProcessGameObjectAndChildren(rootObj);
            }

            Debug.Log($"[Collider Adder] Complete! Processed {_objectsProcessed} object(s), added {_collidersAdded} collider(s).");

            // Repaint the window to show updated stats
            Repaint();
        }

        /// <summary>
        /// Recursively processes a GameObject and all its children, adding colliders where needed
        /// </summary>
        /// <param name="obj">The GameObject to process</param>
        /// <returns>Number of colliders added</returns>
        private static int ProcessGameObjectAndChildren(GameObject obj)
        {
            int collidersAdded = 0;

            // Process this object
            if (ShouldAddCollider(obj))
            {
                AddMeshCollider(obj);
                collidersAdded++;
            }

            // Process all children recursively
            foreach (Transform child in obj.transform)
            {
                collidersAdded += ProcessGameObjectAndChildren(child.gameObject);
            }

            return collidersAdded;
        }

        /// <summary>
        /// Determines if a GameObject needs a collider added
        /// </summary>
        /// <param name="obj">GameObject to check</param>
        /// <returns>True if collider should be added, false otherwise</returns>
        private static bool ShouldAddCollider(GameObject obj)
        {
            // Check if object has a MeshRenderer or SkinnedMeshRenderer (visual mesh)
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
            SkinnedMeshRenderer skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();

            bool hasMesh = (meshRenderer != null || skinnedMeshRenderer != null);

            // Check if object already has ANY type of collider
            Collider existingCollider = obj.GetComponent<Collider>();

            // We should add a collider if:
            // - Object has a mesh (something visible)
            // - Object doesn't already have a collider
            return hasMesh && existingCollider == null;
        }

        /// <summary>
        /// Adds a MeshCollider component to a GameObject
        /// </summary>
        /// <param name="obj">GameObject to add collider to</param>
        private static void AddMeshCollider(GameObject obj)
        {
            // Add MeshCollider component
            MeshCollider meshCollider = obj.AddComponent<MeshCollider>();

            // Get the mesh from either MeshFilter or SkinnedMeshRenderer
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            SkinnedMeshRenderer skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();

            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                // Use the mesh from MeshFilter
                meshCollider.sharedMesh = meshFilter.sharedMesh;
            }
            else if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
            {
                // Use the mesh from SkinnedMeshRenderer
                meshCollider.sharedMesh = skinnedMeshRenderer.sharedMesh;
            }

            // Set mesh collider to not be convex (allows for complex shapes)
            // Convex = false is better for environment pieces like floors/walls
            meshCollider.convex = false;

            // Mark the object as modified so Unity saves the changes
            EditorUtility.SetDirty(obj);

            Debug.Log($"[Collider Adder] Added MeshCollider to: {obj.name}");
        }
    }
}
#endif
