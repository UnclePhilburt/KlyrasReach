//  Distant Lands 2025
//  COZY: Stylized Weather 3
//  All code included in this file is protected under the Unity Asset Store Eula

using UnityEngine;
using DistantLands.Cozy.Data;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DistantLands.Cozy
{
    [ExecuteAlways]
    public class CozyHorizonModule : CozyModule
    {

        private Transform layerParent;
        [CozySearchable]
        public bool hideInHierarchy = false;
        [SerializeField]
        private MeshRenderer layerPrefab;
        [SerializeField]
        private Shader cubemapShader;
        [SerializeField]
        private Shader ribbonShader;
        [SerializeField]
        private Shader spriteShader;
        [SerializeField]
        private Shader textureSheetShader;

        private int beforeClouds;
        private int afterClouds;

        [Range(0, 2)]
        public float fogMultiplier = 1;
        [Range(-1, 1)]
        public float heightOffset = 0;
        [Range(0, 360)]
        public float rotation = 0;

        [CozySearchable("horizon", "skybox", "layers")]
        public CozyHorizonProfile horizonProfile;

        public override void InitializeModule()
        {

            base.InitializeModule();

            UpdateSkyLayers();

        }

        /// <summary>
        /// LateUpdate is called every frame, if the Behaviour is enabled.
        /// It is called after all Update functions have been called.
        /// </summary>
        void LateUpdate()
        {
            //Update position and scale
            layerParent.position = weatherSphere.transform.position;
            layerParent.localScale = weatherSphere.transform.GetChild(0).localScale;
        }

        public override void CozyUpdateLoop()
        {
            if (CozyWeather.FreezeUpdateInEditMode && !Application.isPlaying)
                return;

            if (this == null)
                return;


            if (layerParent == null)
            {
                UpdateSkyLayers();
            }

            if ((layerParent.hideFlags == (HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild) && hideInHierarchy) ||
                (layerParent.hideFlags == (HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.HideInHierarchy) && !hideInHierarchy))
                UpdateSkyLayers();



            if (horizonProfile)
            {
                if (horizonProfile.layers.Count != layerParent.childCount)
                    UpdateSkyLayers();
            }
            else
            {
                UpdateSkyLayers();
            }

        }

        public void UpdateSkyLayers()
        {

            DestroyLayers();

            if (!this)
            {
                return;
            }

            layerParent = new GameObject("Horizon Layers").transform;
            layerParent.position = weatherSphere.transform.position;

            if (hideInHierarchy)
                layerParent.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.HideInHierarchy;
            else
                layerParent.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

            beforeClouds = 0;
            afterClouds = 0;

            if (horizonProfile != null)
                for (int i = horizonProfile.layers.Count - 1; i >= 0; i--)
                {
                    InitializeLayer(horizonProfile.layers[i]);
                }

        }

        public void InitializeLayer(CozyHorizonProfile.HorizonLayerReference layer)
        {
            if (layerParent == null)
            {
                UpdateSkyLayers();
                return;
            }


            MeshRenderer newLayer = Instantiate(layerPrefab, layerParent);
            newLayer.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            if (layer.texture)
                newLayer.name = layer.texture.name;
            Material layerMaterial = null;
            switch (layer.layerType)
            {
                case CozyHorizonProfile.LayerType.Cubemap:
                    layerMaterial = new Material(cubemapShader);
                    break;
                case CozyHorizonProfile.LayerType.Ribbon:
                    layerMaterial = new Material(ribbonShader);
                    layerMaterial.SetFloat("_Position", layer.placementHeight + heightOffset);
                    layerMaterial.SetFloat("_Height", layer.verticalScale);
                    layerMaterial.SetFloat("_Angle", (layer.angle / 360 + (rotation / 360)) % 1f);
                    layerMaterial.SetFloat("_Tiling", layer.tiling);
                    break;
                case CozyHorizonProfile.LayerType.TextureSheet:
                    layerMaterial = new Material(textureSheetShader);
                    layerMaterial.SetVector("_Rotation", new Vector4(layer.pitch, layer.yaw + (rotation / 360), layer.roll, 0));
                    layerMaterial.SetFloat("_Size", layer.size);
                    layerMaterial.SetFloat("_Columns", layer.columns);
                    layerMaterial.SetFloat("_Rows", layer.rows);
                    layerMaterial.SetFloat("_Framerate", layer.framerate);
                    break;
                default:
                    layerMaterial = new Material(spriteShader);
                    layerMaterial.SetVector("_Rotation", new Vector4(layer.pitch, layer.yaw + (rotation / 360), layer.roll, 0));
                    layerMaterial.SetFloat("_Size", layer.size);
                    break;
            }
            int renderQueue = 0;
            layerMaterial.hideFlags = HideFlags.DontSave;
            layerMaterial.SetColor("_Color", layer.color);
            layerMaterial.SetTexture("_Texture", layer.texture);
            layerMaterial.SetFloat("_FogLightAmount", layer.fogLightAmount);
            layerMaterial.SetFloat("_FogAmount", layer.fogAmount * fogMultiplier);

            if (layer.placementLocation == CozyHorizonProfile.PlacementLocation.behindClouds)
            {
                beforeClouds++;
                renderQueue = 2901 + layer.renderPriorityOffset;
            }
            else
            {
                afterClouds++;
                renderQueue = 2950 + layer.renderPriorityOffset;
            }

            layerMaterial.renderQueue = renderQueue;
            newLayer.material = layerMaterial;

        }

        public void DestroyLayers()
        {

            if (layerParent)
            {
                foreach (MeshRenderer layer in layerParent.GetComponentsInChildren<MeshRenderer>())
                {
                    DestroyImmediate(layer.sharedMaterial);
                }
                DestroyImmediate(layerParent.gameObject);
            }

        }

        public override void DeinitializeModule()
        {
            DestroyLayers();
        }

    }

    public class CozyHorizonLayerAttribute : PropertyAttribute
    {

    }

}