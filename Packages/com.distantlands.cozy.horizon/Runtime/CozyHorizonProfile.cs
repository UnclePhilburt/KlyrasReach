using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DistantLands.Cozy.Data
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "Distant Lands/Cozy/Horizon/Horizon Profile", order = 361)]

    public class CozyHorizonProfile : CozyProfile
    {

        public enum LayerType { Cubemap, Ribbon, Sprite, TextureSheet }
        public enum PlacementLocation { behindClouds, inFrontOfClouds }
        [System.Serializable]
        public class HorizonLayerReference
        {
            public LayerType layerType = LayerType.Ribbon;
            public PlacementLocation placementLocation = PlacementLocation.inFrontOfClouds;
            public Texture texture;
            public Color color = Color.white;
            [Range(0, 1)]
            public float fogLightAmount = 1;
            [Range(0, 1)]
            public float fogAmount = 1;
            [Range(-1, 1)]
            public float placementHeight = 0f;
            [Range(0, 1)]
            public float verticalScale = 0.5f;
            public float tiling = 2;
            [Range(0, 360)]
            public float angle;


            public float rows = 1;
            public float columns = 1;
            public float framerate = 10;

            [Range(-90, 90)]
            public float pitch = 0;
            [Range(0, 360)]
            public float yaw = 0;
            [Range(0, 360)]
            public float roll = 0;
            public float size = 1;

            public int renderPriorityOffset;
        }

        [CozyHorizonLayer]
        public List<HorizonLayerReference> layers;

    }
}