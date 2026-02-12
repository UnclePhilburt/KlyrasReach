using System.Linq;
using System.Collections;
using System.Runtime;
using UnityEngine;
using DistantLands.Cozy.Data;
using System.Collections.Generic;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DistantLands.Cozy
{
    [ExecuteAlways]
    public class BlocksModule : CozyBiomeModuleBase<BlocksModule>
    {

        #region Runtime Values
        [ColorUsage(true, true)] public Color skyZenithColor;
        [ColorUsage(true, true)] public Color skyHorizonColor;
        [ColorUsage(true, true)] public Color cloudColor;
        [ColorUsage(true, true)] public Color cloudHighlightColor;
        [ColorUsage(true, true)] public Color highAltitudeCloudColor;
        [ColorUsage(true, true)] public Color sunlightColor;
        [ColorUsage(true, true)] public Color starColor;
        [ColorUsage(true, true)] public Color ambientLightHorizonColor;
        [ColorUsage(true, true)] public Color ambientLightZenithColor;
        public float galaxyIntensity;
        [ColorUsage(true, true)] public Color fogColor1;
        [ColorUsage(true, true)] public Color fogColor2;
        [ColorUsage(true, true)] public Color fogColor3;
        [ColorUsage(true, true)] public Color fogColor4;
        [ColorUsage(true, true)] public Color fogColor5;
        [ColorUsage(true, true)] public Color fogFlareColor;
        public float gradientExponent = 0.364f;
        public float ambientLightMultiplier;
        public float sunSize = 0.7f;
        [ColorUsage(true, true)] public Color sunColor;
        public float sunFalloff = 43.7f;
        [ColorUsage(true, true)] public Color sunFlareColor;
        public float moonFalloff = 24.4f;
        [ColorUsage(true, true)] public Color moonlightColor;
        [ColorUsage(true, true)] public Color moonFlareColor;
        [ColorUsage(true, true)] public Color galaxy1Color;
        [ColorUsage(true, true)] public Color galaxy2Color;
        [ColorUsage(true, true)] public Color galaxy3Color;
        [ColorUsage(true, true)] public Color lightScatteringColor;
        public float fogStart1 = 2;
        public float fogStart2 = 5;
        public float fogStart3 = 10;
        public float fogStart4 = 30;
        public float fogVariationAmount = 0.5f;
        public float fogHeight = 0.85f;
        public float fogSmoothness = 0.5f;
        public float fogDensityMultiplier;
        public float fogLightFlareIntensity = 1;
        public float fogLightFlareFalloff = 21;
        public float fogLightFlareSquish = 1;
        [ColorUsage(true, true)] public Color cloudMoonColor;
        [ColorUsage(true, true)] public Color cloudTextureColor;
        public float cloudSunHighlightFalloff = 14.1f;
        public float cloudMoonHighlightFalloff = 22.9f;

        public float rainbowIntensity;

        #endregion

        public BlockProfile blockProfile;
        public AtmosphereProfile defaultSettings;

        public List<Block> blocks = new List<Block>();
        public List<float> keys = new List<float>();
        [Serializable]
        public class Block
        {

            [Range(0, 1)]
            public float startKey;

            [Range(0, 1)]
            public float endKey;

            public BlocksBlendable[] colorBlocks;

            [HideInInspector]
            public int seed;


            public BlocksBlendable selectedBlock;
            public void GetColorBlock(CozyWeather weather)
            {

                if (colorBlocks.Length <= 0)
                    return;

                BlocksBlendable i = null;
                List<float> floats = new List<float>();
                float totalChance = 0;

                foreach (BlocksBlendable k in colorBlocks)
                {
                    if (!k)
                        continue;

                    float c = k.chance;
                    floats.Add(c);
                    totalChance += c;
                }

                if (totalChance == 0)
                {
                    selectedBlock = colorBlocks[0];
                    return;
                }


                float selection = (float)new System.Random(seed).NextDouble() * totalChance;



                int m = 0;
                float l = 0;

                while (l <= selection)
                {

                    if (selection >= l && selection < l + floats[m])
                    {
                        i = colorBlocks[m];
                        break;
                    }
                    l += floats[m];
                    m++;

                }

                if (!i)
                {
                    i = colorBlocks[0];
                }

                selectedBlock = i;
            }

            public Block(float _startKey, float _endKey, BlocksBlendable[] _blocks)
            {

                startKey = _startKey;
                endKey = _endKey;
                colorBlocks = _blocks;

            }
            public Block(CozyTransitModule.TimeBlock block, BlocksBlendable[] _blocks)
            {

                startKey = block.start;
                endKey = block.end;
                colorBlocks = _blocks;

            }

        }

        public BlocksBlendable currentBlock;
        public BlocksBlendable testColorBlock;

        public bool useSingleBlock = false;

        public override void InitializeModule()
        {
            base.InitializeModule();
            base.SetupModule(new Type[1] { typeof(CozyTransitModule) });

            if (isBiomeModule)
            {
                AddBiome();
                return;
            }
            else
            {
                testColorBlock = null;
                GetBlocks();
            }


        }


        public override void PropogateVariables()
        {
            if (isBiomeModule)
                return;

            if (testColorBlock)
                SingleBlock(testColorBlock);
            else if (blocks.Count > 0)
                SetColorsFromBlocks();
            else if (blockProfile)
                GetBlocks();

            ComputeBiomeWeights();
            ApplyPropertiesToWeatherSphere();

        }

        public void ApplyPropertiesToWeatherSphere()
        {

            ResetWeatherSphere();

            weatherSphere.gradientExponent = Mathf.Lerp(weatherSphere.gradientExponent, gradientExponent, weight);
            weatherSphere.ambientLightHorizonColor = Color.Lerp(weatherSphere.ambientLightHorizonColor, ambientLightHorizonColor, weight);
            weatherSphere.ambientLightZenithColor = Color.Lerp(weatherSphere.ambientLightZenithColor, ambientLightZenithColor, weight);
            weatherSphere.ambientLightMultiplier = Mathf.Lerp(weatherSphere.ambientLightMultiplier, ambientLightMultiplier, weight);
            weatherSphere.cloudColor = Color.Lerp(weatherSphere.cloudColor, cloudColor, weight);
            weatherSphere.cloudHighlightColor = Color.Lerp(weatherSphere.cloudHighlightColor, cloudHighlightColor, weight);
            weatherSphere.cloudMoonColor = Color.Lerp(weatherSphere.cloudMoonColor, cloudMoonColor, weight);
            weatherSphere.cloudMoonHighlightFalloff = Mathf.Lerp(weatherSphere.cloudMoonHighlightFalloff, cloudMoonHighlightFalloff, weight);
            weatherSphere.cloudSunHighlightFalloff = Mathf.Lerp(weatherSphere.cloudSunHighlightFalloff, cloudSunHighlightFalloff, weight);
            weatherSphere.cloudTextureColor = Color.Lerp(weatherSphere.cloudTextureColor, cloudTextureColor, weight);
            weatherSphere.fogColor1 = Color.Lerp(weatherSphere.fogColor1, fogColor1, weight);
            weatherSphere.fogColor2 = Color.Lerp(weatherSphere.fogColor2, fogColor2, weight);
            weatherSphere.fogColor3 = Color.Lerp(weatherSphere.fogColor3, fogColor3, weight);
            weatherSphere.fogColor4 = Color.Lerp(weatherSphere.fogColor4, fogColor4, weight);
            weatherSphere.fogColor5 = Color.Lerp(weatherSphere.fogColor5, fogColor5, weight);
            weatherSphere.fogStart1 = Mathf.Lerp(weatherSphere.fogStart1, fogStart1, weight);
            weatherSphere.fogStart2 = Mathf.Lerp(weatherSphere.fogStart2, fogStart2, weight);
            weatherSphere.fogStart3 = Mathf.Lerp(weatherSphere.fogStart3, fogStart3, weight);
            weatherSphere.fogStart4 = Mathf.Lerp(weatherSphere.fogStart4, fogStart4, weight);
            weatherSphere.fogFlareColor = Color.Lerp(weatherSphere.fogFlareColor, fogFlareColor, weight);
            weatherSphere.fogHeight = Mathf.Lerp(weatherSphere.fogHeight, fogHeight, weight);
            weatherSphere.fogSmoothness = Mathf.Lerp(weatherSphere.fogSmoothness, fogSmoothness, weight);
            weatherSphere.fogDensityMultiplier = Mathf.Lerp(weatherSphere.fogDensityMultiplier, fogDensityMultiplier, weight);
            weatherSphere.fogLightFlareFalloff = Mathf.Lerp(weatherSphere.fogLightFlareFalloff, fogLightFlareFalloff, weight);
            weatherSphere.fogLightFlareIntensity = Mathf.Lerp(weatherSphere.fogLightFlareIntensity, fogLightFlareIntensity, weight);
            weatherSphere.fogLightFlareSquish = Mathf.Lerp(weatherSphere.fogLightFlareSquish, fogLightFlareSquish, weight);
            weatherSphere.galaxy1Color = Color.Lerp(weatherSphere.galaxy1Color, galaxy1Color, weight);
            weatherSphere.galaxy2Color = Color.Lerp(weatherSphere.galaxy2Color, galaxy2Color, weight);
            weatherSphere.galaxy3Color = Color.Lerp(weatherSphere.galaxy3Color, galaxy3Color, weight);
            weatherSphere.galaxyIntensity = Mathf.Lerp(weatherSphere.galaxyIntensity, galaxyIntensity, weight);
            weatherSphere.highAltitudeCloudColor = Color.Lerp(weatherSphere.highAltitudeCloudColor, highAltitudeCloudColor, weight);
            weatherSphere.lightScatteringColor = Color.Lerp(weatherSphere.lightScatteringColor, lightScatteringColor, weight);
            weatherSphere.moonlightColor = Color.Lerp(weatherSphere.moonlightColor, moonlightColor, weight);
            weatherSphere.moonFalloff = Mathf.Lerp(weatherSphere.moonFalloff, moonFalloff, weight);
            weatherSphere.moonFlareColor = Color.Lerp(weatherSphere.moonFlareColor, moonFlareColor, weight);
            weatherSphere.skyHorizonColor = Color.Lerp(weatherSphere.skyHorizonColor, skyHorizonColor, weight);
            weatherSphere.skyZenithColor = Color.Lerp(weatherSphere.skyZenithColor, skyZenithColor, weight);
            weatherSphere.starColor = Color.Lerp(weatherSphere.starColor, starColor, weight);
            weatherSphere.sunColor = Color.Lerp(weatherSphere.sunColor, sunColor, weight);
            weatherSphere.sunFalloff = Mathf.Lerp(weatherSphere.sunFalloff, sunFalloff, weight);
            weatherSphere.sunFlareColor = Color.Lerp(weatherSphere.sunFlareColor, sunFlareColor, weight);
            weatherSphere.sunlightColor = Color.Lerp(weatherSphere.sunlightColor, sunlightColor, weight);
            weatherSphere.sunSize = Mathf.Lerp(weatherSphere.sunSize, sunSize, weight);

            foreach (BlocksModule biome in biomes)
            {
                if (biome == null) continue;
                if (biome.system.weight == 0) continue;

                if (biome.weight > 0)
                {
                    weatherSphere.gradientExponent = Mathf.Lerp(weatherSphere.gradientExponent, biome.gradientExponent, biome.weight);
                    weatherSphere.ambientLightHorizonColor = Color.Lerp(weatherSphere.ambientLightHorizonColor, biome.ambientLightHorizonColor, biome.weight);
                    weatherSphere.ambientLightZenithColor = Color.Lerp(weatherSphere.ambientLightZenithColor, biome.skyZenithColor, biome.weight);
                    weatherSphere.ambientLightMultiplier = Mathf.Lerp(weatherSphere.ambientLightMultiplier, biome.ambientLightMultiplier, biome.weight);
                    weatherSphere.cloudColor = Color.Lerp(weatherSphere.cloudColor, biome.cloudColor, biome.weight);
                    weatherSphere.cloudHighlightColor = Color.Lerp(weatherSphere.cloudHighlightColor, biome.cloudHighlightColor, biome.weight);
                    weatherSphere.cloudMoonColor = Color.Lerp(weatherSphere.cloudMoonColor, biome.cloudMoonColor, biome.weight);
                    weatherSphere.cloudMoonHighlightFalloff = Mathf.Lerp(weatherSphere.cloudMoonHighlightFalloff, biome.cloudMoonHighlightFalloff, biome.weight);
                    weatherSphere.cloudSunHighlightFalloff = Mathf.Lerp(weatherSphere.cloudSunHighlightFalloff, biome.cloudSunHighlightFalloff, biome.weight);
                    weatherSphere.cloudTextureColor = Color.Lerp(weatherSphere.cloudTextureColor, biome.cloudTextureColor, biome.weight);
                    weatherSphere.fogColor1 = Color.Lerp(weatherSphere.fogColor1, biome.fogColor1, biome.weight);
                    weatherSphere.fogColor2 = Color.Lerp(weatherSphere.fogColor2, biome.fogColor2, biome.weight);
                    weatherSphere.fogColor3 = Color.Lerp(weatherSphere.fogColor3, biome.fogColor3, biome.weight);
                    weatherSphere.fogColor4 = Color.Lerp(weatherSphere.fogColor4, biome.fogColor4, biome.weight);
                    weatherSphere.fogColor5 = Color.Lerp(weatherSphere.fogColor5, biome.fogColor5, biome.weight);
                    weatherSphere.fogStart1 = Mathf.Lerp(weatherSphere.fogStart1, biome.fogStart1, biome.weight);
                    weatherSphere.fogStart2 = Mathf.Lerp(weatherSphere.fogStart2, biome.fogStart2, biome.weight);
                    weatherSphere.fogStart3 = Mathf.Lerp(weatherSphere.fogStart3, biome.fogStart3, biome.weight);
                    weatherSphere.fogStart4 = Mathf.Lerp(weatherSphere.fogStart4, biome.fogStart4, biome.weight);
                    weatherSphere.fogFlareColor = Color.Lerp(weatherSphere.fogFlareColor, biome.fogFlareColor, biome.weight);
                    weatherSphere.fogHeight = Mathf.Lerp(weatherSphere.fogHeight, biome.fogHeight, biome.weight);
                    weatherSphere.fogSmoothness = Mathf.Lerp(weatherSphere.fogSmoothness, biome.fogSmoothness, biome.weight);
                    weatherSphere.fogDensityMultiplier = Mathf.Lerp(weatherSphere.fogDensityMultiplier, biome.fogDensityMultiplier, biome.weight);
                    weatherSphere.fogLightFlareFalloff = Mathf.Lerp(weatherSphere.fogLightFlareFalloff, biome.fogLightFlareFalloff, biome.weight);
                    weatherSphere.fogLightFlareIntensity = Mathf.Lerp(weatherSphere.fogLightFlareIntensity, biome.fogLightFlareIntensity, biome.weight);
                    weatherSphere.fogLightFlareSquish = Mathf.Lerp(weatherSphere.fogLightFlareSquish, biome.fogLightFlareSquish, biome.weight);
                    weatherSphere.galaxy1Color = Color.Lerp(weatherSphere.galaxy1Color, biome.galaxy1Color, biome.weight);
                    weatherSphere.galaxy2Color = Color.Lerp(weatherSphere.galaxy2Color, biome.galaxy2Color, biome.weight);
                    weatherSphere.galaxy3Color = Color.Lerp(weatherSphere.galaxy3Color, biome.galaxy3Color, biome.weight);
                    weatherSphere.galaxyIntensity = Mathf.Lerp(weatherSphere.galaxyIntensity, biome.galaxyIntensity, biome.weight);
                    weatherSphere.highAltitudeCloudColor = Color.Lerp(weatherSphere.highAltitudeCloudColor, biome.highAltitudeCloudColor, biome.weight);
                    weatherSphere.lightScatteringColor = Color.Lerp(weatherSphere.lightScatteringColor, biome.lightScatteringColor, biome.weight);
                    weatherSphere.moonlightColor = Color.Lerp(weatherSphere.moonlightColor, biome.moonlightColor, biome.weight);
                    weatherSphere.moonFalloff = Mathf.Lerp(weatherSphere.moonFalloff, biome.moonFalloff, biome.weight);
                    weatherSphere.moonFlareColor = Color.Lerp(weatherSphere.moonFlareColor, biome.moonFlareColor, biome.weight);
                    weatherSphere.skyHorizonColor = Color.Lerp(weatherSphere.skyHorizonColor, biome.skyHorizonColor, biome.weight);
                    weatherSphere.skyZenithColor = Color.Lerp(weatherSphere.skyZenithColor, biome.skyZenithColor, biome.weight);
                    weatherSphere.starColor = Color.Lerp(weatherSphere.starColor, biome.starColor, biome.weight);
                    weatherSphere.sunColor = Color.Lerp(weatherSphere.sunColor, biome.sunColor, biome.weight);
                    weatherSphere.sunFalloff = Mathf.Lerp(weatherSphere.sunFalloff, biome.sunFalloff, biome.weight);
                    weatherSphere.sunFlareColor = Color.Lerp(weatherSphere.sunFlareColor, biome.sunFlareColor, biome.weight);
                    weatherSphere.sunlightColor = Color.Lerp(weatherSphere.sunlightColor, biome.sunlightColor, biome.weight);
                    weatherSphere.sunSize = Mathf.Lerp(weatherSphere.sunSize, biome.sunSize, biome.weight);

                }
            }

            weatherSphere.UpdateShaderVariables();

        }

        void ResetWeatherSphere()
        {

            weatherSphere.gradientExponent = 0;
            weatherSphere.ambientLightHorizonColor = Color.clear;
            weatherSphere.ambientLightZenithColor = Color.clear;
            weatherSphere.ambientLightMultiplier = 0;
            weatherSphere.cloudColor = Color.clear;
            weatherSphere.cloudHighlightColor = Color.clear;
            weatherSphere.cloudMoonColor = Color.clear;
            weatherSphere.cloudMoonHighlightFalloff = 0;
            weatherSphere.cloudSunHighlightFalloff = 0;
            weatherSphere.cloudTextureColor = Color.clear;
            weatherSphere.fogColor1 = Color.clear;
            weatherSphere.fogColor2 = Color.clear;
            weatherSphere.fogColor3 = Color.clear;
            weatherSphere.fogColor4 = Color.clear;
            weatherSphere.fogColor5 = Color.clear;
            weatherSphere.fogStart1 = 0;
            weatherSphere.fogStart2 = 0;
            weatherSphere.fogStart3 = 0;
            weatherSphere.fogStart4 = 0;
            weatherSphere.fogFlareColor = Color.clear;
            weatherSphere.fogHeight = 0;
            weatherSphere.fogDensityMultiplier = 0;
            weatherSphere.fogLightFlareFalloff = 0;
            weatherSphere.fogLightFlareIntensity = 0;
            weatherSphere.fogLightFlareSquish = 0;
            weatherSphere.galaxy1Color = Color.clear;
            weatherSphere.galaxy2Color = Color.clear;
            weatherSphere.galaxy3Color = Color.clear;
            weatherSphere.galaxyIntensity = 0;
            weatherSphere.highAltitudeCloudColor = Color.clear;
            weatherSphere.lightScatteringColor = Color.clear;
            weatherSphere.moonlightColor = Color.clear;
            weatherSphere.moonFalloff = 0;
            weatherSphere.moonFlareColor = Color.clear;
            weatherSphere.skyHorizonColor = Color.clear;
            weatherSphere.skyZenithColor = Color.clear;
            weatherSphere.starColor = Color.clear;
            weatherSphere.sunColor = Color.clear;
            weatherSphere.sunFalloff = 0;
            weatherSphere.sunFlareColor = Color.clear;
            weatherSphere.sunlightColor = Color.clear;
            weatherSphere.sunSize = 0;
        }

        public void GetBlocks()
        {

            if (blockProfile == null)
                return;

            List<Block> i = new List<Block>();

            List<BlocksBlendable> j = new List<BlocksBlendable>();

            if (blockProfile.timeBlocks.HasFlag(BlockProfile.TimeBlocks.dawn))
                i.Add(new Block(weatherSphere.timeModule.transit.dawnBlock, blockProfile.dawn.ToArray()));
            if (blockProfile.timeBlocks.HasFlag(BlockProfile.TimeBlocks.morning))
                i.Add(new Block(weatherSphere.timeModule.transit.morningBlock, blockProfile.morning.ToArray()));
            if (blockProfile.timeBlocks.HasFlag(BlockProfile.TimeBlocks.day))
                i.Add(new Block(weatherSphere.timeModule.transit.dayBlock, blockProfile.day.ToArray()));
            if (blockProfile.timeBlocks.HasFlag(BlockProfile.TimeBlocks.afternoon))
                i.Add(new Block(weatherSphere.timeModule.transit.afternoonBlock, blockProfile.afternoon.ToArray()));
            if (blockProfile.timeBlocks.HasFlag(BlockProfile.TimeBlocks.evening))
                i.Add(new Block(weatherSphere.timeModule.transit.eveningBlock, blockProfile.evening.ToArray()));
            if (blockProfile.timeBlocks.HasFlag(BlockProfile.TimeBlocks.twilight))
                i.Add(new Block(weatherSphere.timeModule.transit.twilightBlock, blockProfile.twilight.ToArray()));
            if (blockProfile.timeBlocks.HasFlag(BlockProfile.TimeBlocks.night))
                i.Add(new Block(weatherSphere.timeModule.transit.nightBlock, blockProfile.night.ToArray()));

            blocks = i;

            foreach (Block k in blocks)
                k.GetColorBlock(weatherSphere);

        }

        void SetColorsFromBlocks()
        {

            float time = weatherSphere.usePhysicalSunHeight ? weatherSphere.modifiedDayPercentage : weatherSphere.dayPercentage;


            BlocksBlendable currentBlock;


            if (keys.Count > 0)
                keys.Clear();

            foreach (Block j in blocks)
            {

                if (j.colorBlocks.Length == 0)
                    continue;

                keys.Add(j.startKey);
                keys.Add(j.endKey);
            }

            int k = 0;

            foreach (float i in keys)
            {

                if (time > i)
                {
                    k++;

                    if (k == keys.Count)
                    {
                        SingleBlock(blocks[^1].selectedBlock);
                        blocks[0].GetColorBlock(weatherSphere);
                    }
                    continue;
                }

                currentBlock = k > 1 ? blocks[k / 2 - 1].selectedBlock : blocks[^1].selectedBlock;

                if (k % 2 == 1)
                {

                    //In between two key frames from the same block.
                    TwoBlock(currentBlock, blocks[Mathf.FloorToInt(k / 2)].selectedBlock, (time - keys[k - 1]) / (i - keys[k - 1]));
                    break;

                }
                else
                {
                    //In between two key frames from different blocks.
                    SingleBlock(currentBlock);
                    if (k == keys.Count - 2)
                    {
                        blocks[0].seed = new System.Random().Next();
                        blocks[0].GetColorBlock(weatherSphere);
                    }
                    else
                    {
                        blocks[Mathf.FloorToInt(k / 2) + 1].seed = new System.Random().Next();
                        blocks[Mathf.FloorToInt(k / 2) + 1].GetColorBlock(weatherSphere);
                    }
                    break;

                }
            }
        }

        void SingleBlock(BlocksBlendable colorBlock)
        {

            if (colorBlock == null)
                return;

            colorBlock.SingleBlockBlend(this);

            currentBlock = colorBlock;

        }

        void TwoBlock(BlocksBlendable colorBlock1, BlocksBlendable colorBlock2, float blend)
        {
            if (colorBlock1 == null || colorBlock2 == null)
                return;

            ColorBlock currentColorBlock = colorBlock1.GetValues(this);
            ColorBlock nextColorBlock = colorBlock2.GetValues(this);

            gradientExponent = Mathf.Lerp(currentColorBlock.gradientExponent, nextColorBlock.gradientExponent, blend);
            ambientLightHorizonColor = Color.Lerp(currentColorBlock.ambientLightHorizonColor, nextColorBlock.ambientLightHorizonColor, blend);
            ambientLightZenithColor = Color.Lerp(currentColorBlock.ambientLightZenithColor, nextColorBlock.ambientLightZenithColor, blend);
            ambientLightMultiplier = Mathf.Lerp(currentColorBlock.ambientLightMultiplier, nextColorBlock.ambientLightMultiplier, blend);
            cloudColor = Color.Lerp(currentColorBlock.cloudColor, nextColorBlock.cloudColor, blend);
            cloudHighlightColor = Color.Lerp(currentColorBlock.cloudHighlightColor, nextColorBlock.cloudHighlightColor, blend);
            cloudMoonColor = Color.Lerp(currentColorBlock.cloudMoonColor, nextColorBlock.cloudMoonColor, blend);
            cloudMoonHighlightFalloff = Mathf.Lerp(currentColorBlock.cloudMoonHighlightFalloff, nextColorBlock.cloudMoonHighlightFalloff, blend);
            cloudSunHighlightFalloff = Mathf.Lerp(currentColorBlock.cloudSunHighlightFalloff, nextColorBlock.cloudSunHighlightFalloff, blend);
            cloudTextureColor = Color.Lerp(currentColorBlock.cloudTextureColor, nextColorBlock.cloudTextureColor, blend);
            fogColor1 = Color.Lerp(currentColorBlock.fogColor1, nextColorBlock.fogColor1, blend);
            fogColor2 = Color.Lerp(currentColorBlock.fogColor2, nextColorBlock.fogColor2, blend);
            fogColor3 = Color.Lerp(currentColorBlock.fogColor3, nextColorBlock.fogColor3, blend);
            fogColor4 = Color.Lerp(currentColorBlock.fogColor4, nextColorBlock.fogColor4, blend);
            fogColor5 = Color.Lerp(currentColorBlock.fogColor5, nextColorBlock.fogColor5, blend);
            fogStart1 = Mathf.Lerp(currentColorBlock.fogStart1, nextColorBlock.fogStart1, blend);
            fogStart2 = Mathf.Lerp(currentColorBlock.fogStart2, nextColorBlock.fogStart2, blend);
            fogStart3 = Mathf.Lerp(currentColorBlock.fogStart3, nextColorBlock.fogStart3, blend);
            fogStart4 = Mathf.Lerp(currentColorBlock.fogStart4, nextColorBlock.fogStart4, blend);
            fogFlareColor = Color.Lerp(currentColorBlock.fogFlareColor, nextColorBlock.fogFlareColor, blend);
            fogHeight = Mathf.Lerp(currentColorBlock.fogHeight, nextColorBlock.fogHeight, blend);
            fogSmoothness = Mathf.Lerp(currentColorBlock.fogSmoothness, nextColorBlock.fogSmoothness, blend);
            fogDensityMultiplier = Mathf.Lerp(currentColorBlock.fogDensity, nextColorBlock.fogDensity, blend);
            fogLightFlareFalloff = Mathf.Lerp(currentColorBlock.fogLightFlareFalloff, nextColorBlock.fogLightFlareFalloff, blend);
            fogLightFlareIntensity = Mathf.Lerp(currentColorBlock.fogLightFlareIntensity, nextColorBlock.fogLightFlareIntensity, blend);
            fogLightFlareSquish = Mathf.Lerp(currentColorBlock.fogLightFlareSquish, nextColorBlock.fogLightFlareSquish, blend);
            galaxy1Color = Color.Lerp(currentColorBlock.galaxy1Color, nextColorBlock.galaxy1Color, blend);
            galaxy2Color = Color.Lerp(currentColorBlock.galaxy2Color, nextColorBlock.galaxy2Color, blend);
            galaxy3Color = Color.Lerp(currentColorBlock.galaxy3Color, nextColorBlock.galaxy3Color, blend);
            galaxyIntensity = Mathf.Lerp(currentColorBlock.galaxyIntensity, nextColorBlock.galaxyIntensity, blend);
            highAltitudeCloudColor = Color.Lerp(currentColorBlock.highAltitudeCloudColor, nextColorBlock.highAltitudeCloudColor, blend);
            lightScatteringColor = Color.Lerp(currentColorBlock.lightScatteringColor, nextColorBlock.lightScatteringColor, blend);
            moonlightColor = Color.Lerp(currentColorBlock.moonlightColor, nextColorBlock.moonlightColor, blend);
            moonFalloff = Mathf.Lerp(currentColorBlock.moonFalloff, nextColorBlock.moonFalloff, blend);
            moonFlareColor = Color.Lerp(currentColorBlock.moonFlareColor, nextColorBlock.moonFlareColor, blend);
            skyHorizonColor = Color.Lerp(currentColorBlock.skyHorizonColor, nextColorBlock.skyHorizonColor, blend);
            skyZenithColor = Color.Lerp(currentColorBlock.skyZenithColor, nextColorBlock.skyZenithColor, blend);
            starColor = Color.Lerp(currentColorBlock.starColor, nextColorBlock.starColor, blend);
            sunColor = Color.Lerp(currentColorBlock.sunColor, nextColorBlock.sunColor, blend);
            sunFalloff = Mathf.Lerp(currentColorBlock.sunFalloff, nextColorBlock.sunFalloff, blend);
            sunFlareColor = Color.Lerp(currentColorBlock.sunFlareColor, nextColorBlock.sunFlareColor, blend);
            sunlightColor = Color.Lerp(currentColorBlock.sunlightColor, nextColorBlock.sunlightColor, blend);
            if (currentColorBlock.extension != null && nextColorBlock.extension != null)
            {
                currentColorBlock.extension.TwoBlock(nextColorBlock.extension, blend);
            }


            currentBlock = blend > 0.5 ? colorBlock2 : colorBlock1;

        }


    }
}