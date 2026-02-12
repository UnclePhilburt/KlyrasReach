using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DistantLands.Cozy.Data
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "Distant Lands/Cozy/Color Block Override", order = 361)]
    public class ColorBlockOverride : BlocksBlendable
    {

        #region  Block Atmosphere


        [Tooltip("Sets the color of the zenith (or top) of the skybox at a certain time. Starts and ends at midnight.")]
        public Overridable<Color> skyZenithColor;

        [Tooltip("Sets the color of the horizon (or middle) of the skybox at a certain time. Starts and ends at midnight.")]
        public Overridable<Color> skyHorizonColor;

        [Tooltip("Sets the main color of the clouds at a certain time. Starts and ends at midnight.")]
        public Overridable<Color> cloudColor;

        [Tooltip("Sets the highlight color of the clouds at a certain time. Starts and ends at midnight.")]
        public Overridable<Color> cloudHighlightColor;

        [Tooltip("Sets the color of the high altitude clouds at a certain time. Starts and ends at midnight.")]
        public Overridable<Color> highAltitudeCloudColor;

        [Tooltip("Sets the color of the sun light source at a certain time. Starts and ends at midnight.")]
        public Overridable<Color> sunlightColor;

        [Tooltip("Sets the color of the moon light source at a certain time. Starts and ends at midnight.")]
        public Overridable<Color> moonlightColor;

        [Tooltip("Sets the color of the star particle FX and textures at a certain time. Starts and ends at midnight.")]
        public Overridable<Color> starColor;

        [Tooltip("Sets the color of the zenith (or top) of the ambient scene lighting at a certain time. Starts and ends at midnight.")]
        public Overridable<Color> ambientLightHorizonColor;

        [Tooltip("Sets the color of the horizon (or middle) of the ambient scene lighting at a certain time. Starts and ends at midnight.")]
        public Overridable<Color> ambientLightZenithColor;

        [Tooltip("Multiplies the ambient light intensity.")]
        [OverrideRange(0, 4)]
        public Overridable<float> ambientLightMultiplier;

        [Tooltip("Sets the intensity of the galaxy effects at a certain time. Starts and ends at midnight.")]
        [OverrideRange(0, 1)]
        public Overridable<float> galaxyIntensity;

        public Overridable<float> fogDensity = 1f;
        public Overridable<float> fogVariationAmount = 1f;



        [Tooltip("Sets the fog color from 0m away from the camera to fog start 1.")]

        public Overridable<Color> fogColor1;

        [Tooltip("Sets the fog color from fog start 1 to fog start 2.")]

        public Overridable<Color> fogColor2;
        [Tooltip("Sets the fog color from fog start 2 to fog start 3.")]


        public Overridable<Color> fogColor3;
        [Tooltip("Sets the fog color from fog start 3 to fog start 4.")]


        public Overridable<Color> fogColor4;
        [Tooltip("Sets the fog color from fog start 4 to fog start 5.")]


        public Overridable<Color> fogColor5;

        [Tooltip("Sets the color of the fog flare.")]

        public Overridable<Color> fogFlareColor;




        [OverrideRange(0, 1)]
        [Tooltip("Controls the exponent used to modulate from the horizon color to the zenith color of the sky.")]
        public Overridable<float> gradientExponent;

        [OverrideRange(0, 5)]
        [Tooltip("Sets the size of the visual sun in the sky.")]
        public Overridable<float> sunSize;

        [Tooltip("Sets the color of the visual sun in the sky.")]
        public Overridable<Color> sunColor;


        [OverrideRange(0, 100)]
        [Tooltip("Sets the falloff of the halo around the visual sun.")]
        public Overridable<float> sunFalloff;

        [Tooltip("Sets the color of the halo around the visual sun.")]
        public Overridable<Color> sunFlareColor;

        [OverrideRange(0, 100)]
        [Tooltip("Sets the falloff of the halo around the main moon.")]
        public Overridable<float> moonFalloff;

        [Tooltip("Sets the color of the halo around the main moon.")]
        public Overridable<Color> moonFlareColor;

        [Tooltip("Sets the color of the first galaxy algorithm.")]
        public Overridable<Color> galaxy1Color;

        [Tooltip("Sets the color of the second galaxy algorithm.")]
        public Overridable<Color> galaxy2Color;

        [Tooltip("Sets the color of the third galaxy algorithm.")]
        public Overridable<Color> galaxy3Color;

        [Tooltip("Sets the color of the light columns around the horizon.")]
        public Overridable<Color> lightScatteringColor;

        [Tooltip("Sets the distance at which the first fog color fades into the second fog color.")]
        [OverrideRange(0, 50)]
        public Overridable<float> fogStart1;
        [OverrideRange(0, 50)]
        public Overridable<float> fogStart2;
        [OverrideRange(0, 50)]
        public Overridable<float> fogStart3;
        [OverrideRange(0, 50)]
        public Overridable<float> fogStart4;
        [OverrideRange(0, 2)]
        public Overridable<float> fogHeight;
        [OverrideRange(0, 1)]
        public Overridable<float> fogSmoothness = 0.5f;
        [OverrideRange(0, 2)]
        public Overridable<float> fogLightFlareIntensity;
        [OverrideRange(0, 40)]
        public Overridable<float> fogLightFlareFalloff;
        [OverrideRange(0, 10)]
        [Tooltip("Sets the height divisor for the fog flare. High values sit the flare closer to the horizon, small values extend the flare into the sky.")]
        public Overridable<float> fogLightFlareSquish;



        public Overridable<Color> cloudMoonColor;
        [OverrideRange(0, 50)]
        public Overridable<float> cloudSunHighlightFalloff;
        [OverrideRange(0, 50)]
        public Overridable<float> cloudMoonHighlightFalloff;

        public Overridable<Color> cloudTextureColor;

        public ColorBlockExtension extension;
        #endregion

        public override void PullFromAtmosphere()
        {


            CozyWeather weatherSphere = CozyWeather.instance;
            float i = CozyWeather.instance.timeModule.currentTime;
#if UNITY_EDITOR
            Undo.RecordObject(this, "Pull From Atmosphere");
#endif
            gradientExponent = weatherSphere.gradientExponent;
            ambientLightHorizonColor = weatherSphere.ambientLightHorizonColor;
            ambientLightZenithColor = weatherSphere.ambientLightZenithColor;
            ambientLightMultiplier = weatherSphere.ambientLightMultiplier;
            cloudColor = weatherSphere.cloudColor;
            cloudHighlightColor = weatherSphere.cloudHighlightColor;
            cloudMoonColor = weatherSphere.cloudMoonColor;
            cloudMoonHighlightFalloff = weatherSphere.cloudMoonHighlightFalloff;
            cloudSunHighlightFalloff = weatherSphere.cloudSunHighlightFalloff;
            cloudTextureColor = weatherSphere.cloudTextureColor;
            fogColor1 = weatherSphere.fogColor1;
            fogColor2 = weatherSphere.fogColor2;
            fogColor3 = weatherSphere.fogColor3;
            fogColor4 = weatherSphere.fogColor4;
            fogColor5 = weatherSphere.fogColor5;
            fogStart1 = weatherSphere.fogStart1;
            fogStart2 = weatherSphere.fogStart2;
            fogStart3 = weatherSphere.fogStart3;
            fogStart4 = weatherSphere.fogStart4;
            fogFlareColor = weatherSphere.fogFlareColor;
            fogHeight = weatherSphere.fogHeight;
            fogSmoothness = weatherSphere.fogSmoothness;
            fogVariationAmount = weatherSphere.fogVariationAmount;
            fogLightFlareFalloff = weatherSphere.fogLightFlareFalloff;
            fogLightFlareIntensity = weatherSphere.fogLightFlareIntensity;
            fogLightFlareSquish = weatherSphere.fogLightFlareSquish;
            galaxy1Color = weatherSphere.galaxy1Color;
            galaxy2Color = weatherSphere.galaxy2Color;
            galaxy3Color = weatherSphere.galaxy3Color;
            galaxyIntensity = weatherSphere.galaxyIntensity;
            highAltitudeCloudColor = weatherSphere.highAltitudeCloudColor;
            lightScatteringColor = weatherSphere.lightScatteringColor;
            moonlightColor = weatherSphere.moonlightColor;
            moonFalloff = weatherSphere.moonFalloff;
            moonFlareColor = weatherSphere.moonFlareColor;
            skyHorizonColor = weatherSphere.skyHorizonColor;
            skyZenithColor = weatherSphere.skyZenithColor;
            starColor = weatherSphere.starColor;
            sunColor = weatherSphere.sunColor;
            sunFalloff = weatherSphere.sunFalloff;
            sunFlareColor = weatherSphere.sunFlareColor;
            sunlightColor = weatherSphere.sunlightColor;
            sunSize = weatherSphere.sunSize;
            if (extension != null) extension.PullFromWorld();
        }

        public override ColorBlock GetValues(BlocksModule module)
        {
            float time = module.weatherSphere.modifiedDayPercentage;
            ColorBlock currentColors = CreateInstance<ColorBlock>();
            currentColors.ambientLightHorizonColor = ambientLightHorizonColor ? ambientLightHorizonColor : module.defaultSettings.ambientLightHorizonColor.GetColorValue(time);
            currentColors.ambientLightZenithColor = ambientLightZenithColor ? ambientLightZenithColor : module.defaultSettings.ambientLightZenithColor.GetColorValue(time);
            currentColors.ambientLightMultiplier = ambientLightMultiplier ? ambientLightMultiplier : module.defaultSettings.ambientLightMultiplier.GetFloatValue(time);
            currentColors.cloudColor = cloudColor ? cloudColor : module.defaultSettings.cloudColor.GetColorValue(time);
            currentColors.cloudHighlightColor = cloudHighlightColor ? cloudHighlightColor : module.defaultSettings.cloudHighlightColor.GetColorValue(time);
            currentColors.cloudMoonColor = cloudMoonColor ? cloudMoonColor : module.defaultSettings.cloudMoonColor.GetColorValue(time);
            currentColors.cloudMoonHighlightFalloff = cloudMoonHighlightFalloff ? cloudMoonHighlightFalloff : module.defaultSettings.cloudMoonHighlightFalloff.GetFloatValue(time);
            currentColors.cloudSunHighlightFalloff = cloudSunHighlightFalloff ? cloudSunHighlightFalloff : module.defaultSettings.cloudSunHighlightFalloff.GetFloatValue(time);
            currentColors.cloudTextureColor = cloudTextureColor ? cloudTextureColor : module.defaultSettings.cloudTextureColor.GetColorValue(time);
            currentColors.fogColor1 = fogColor1 ? fogColor1 : module.defaultSettings.fogColor1.GetColorValue(time);
            currentColors.fogColor2 = fogColor2 ? fogColor2 : module.defaultSettings.fogColor2.GetColorValue(time);
            currentColors.fogColor3 = fogColor3 ? fogColor3 : module.defaultSettings.fogColor3.GetColorValue(time);
            currentColors.fogColor4 = fogColor4 ? fogColor4 : module.defaultSettings.fogColor4.GetColorValue(time);
            currentColors.fogColor5 = fogColor5 ? fogColor5 : module.defaultSettings.fogColor5.GetColorValue(time);
            currentColors.fogStart1 = fogStart1 ? fogStart1 : module.defaultSettings.fogStart1.GetFloatValue(time);
            currentColors.fogStart2 = fogStart2 ? fogStart2 : module.defaultSettings.fogStart2.GetFloatValue(time);
            currentColors.fogStart3 = fogStart3 ? fogStart3 : module.defaultSettings.fogStart3.GetFloatValue(time);
            currentColors.fogStart4 = fogStart4 ? fogStart4 : module.defaultSettings.fogStart4.GetFloatValue(time);
            currentColors.fogFlareColor = fogFlareColor ? fogFlareColor : module.defaultSettings.fogFlareColor.GetColorValue(time);
            currentColors.fogHeight = fogHeight ? fogHeight : module.defaultSettings.fogHeight.GetFloatValue(time);
            currentColors.fogSmoothness = fogSmoothness ? fogSmoothness : module.defaultSettings.fogSmoothness.GetFloatValue(time);
            currentColors.fogDensity = fogDensity ? fogDensity : module.defaultSettings.fogDensityMultiplier.GetFloatValue(time);
            currentColors.fogVariationAmount = fogVariationAmount ? fogVariationAmount : module.defaultSettings.fogVariationAmount.GetFloatValue(time);
            currentColors.fogLightFlareFalloff = fogLightFlareFalloff ? fogLightFlareFalloff : module.defaultSettings.fogLightFlareFalloff.GetFloatValue(time);
            currentColors.fogLightFlareIntensity = fogLightFlareIntensity ? fogLightFlareIntensity : module.defaultSettings.fogLightFlareIntensity.GetFloatValue(time);
            currentColors.fogLightFlareSquish = fogLightFlareSquish ? fogLightFlareSquish : module.defaultSettings.fogLightFlareSquish.GetFloatValue(time);
            currentColors.galaxy1Color = galaxy1Color ? galaxy1Color : module.defaultSettings.galaxy1Color.GetColorValue(time);
            currentColors.galaxy2Color = galaxy2Color ? galaxy2Color : module.defaultSettings.galaxy2Color.GetColorValue(time);
            currentColors.galaxy3Color = galaxy3Color ? galaxy3Color : module.defaultSettings.galaxy3Color.GetColorValue(time);
            currentColors.galaxyIntensity = galaxyIntensity ? galaxyIntensity : module.defaultSettings.galaxyIntensity.GetFloatValue(time);
            currentColors.highAltitudeCloudColor = highAltitudeCloudColor ? highAltitudeCloudColor : module.defaultSettings.highAltitudeCloudColor.GetColorValue(time);
            currentColors.lightScatteringColor = lightScatteringColor ? lightScatteringColor : module.defaultSettings.lightScatteringColor.GetColorValue(time);
            currentColors.moonlightColor = moonlightColor ? moonlightColor : module.defaultSettings.moonlightColor.GetColorValue(time);
            currentColors.moonFalloff = moonFalloff ? moonFalloff : module.defaultSettings.moonFalloff.GetFloatValue(time);
            currentColors.moonFlareColor = moonFlareColor ? moonFlareColor : module.defaultSettings.moonFlareColor.GetColorValue(time);
            currentColors.skyHorizonColor = skyHorizonColor ? skyHorizonColor : module.defaultSettings.skyHorizonColor.GetColorValue(time);
            currentColors.skyZenithColor = skyZenithColor ? skyZenithColor : module.defaultSettings.skyZenithColor.GetColorValue(time);
            currentColors.starColor = starColor ? starColor : module.defaultSettings.starColor.GetColorValue(time);
            currentColors.sunColor = sunColor ? sunColor : module.defaultSettings.sunColor.GetColorValue(time);
            currentColors.sunFalloff = sunFalloff ? sunFalloff : module.defaultSettings.sunFalloff.GetFloatValue(time);
            currentColors.sunFlareColor = sunFlareColor ? sunFlareColor : module.defaultSettings.sunFlareColor.GetColorValue(time);
            currentColors.sunlightColor = sunlightColor ? sunlightColor : module.defaultSettings.sunlightColor.GetColorValue(time);
            currentColors.sunSize = sunSize ? sunSize : module.defaultSettings.sunSize.GetFloatValue(time);

            return currentColors;

        }

        public override void SingleBlockBlend(BlocksModule module)
        {

            float time = module.weatherSphere.modifiedDayPercentage;

            module.ambientLightHorizonColor = ambientLightHorizonColor ? ambientLightHorizonColor : module.defaultSettings.ambientLightHorizonColor.GetColorValue(time);
            module.ambientLightZenithColor = ambientLightZenithColor ? ambientLightZenithColor : module.defaultSettings.ambientLightZenithColor.GetColorValue(time);
            module.ambientLightMultiplier = ambientLightMultiplier ? ambientLightMultiplier : module.defaultSettings.ambientLightMultiplier.GetFloatValue(time);
            module.cloudColor = cloudColor ? cloudColor : module.defaultSettings.cloudColor.GetColorValue(time);
            module.cloudHighlightColor = cloudHighlightColor ? cloudHighlightColor : module.defaultSettings.cloudHighlightColor.GetColorValue(time);
            module.cloudMoonColor = cloudMoonColor ? cloudMoonColor : module.defaultSettings.cloudMoonColor.GetColorValue(time);
            module.cloudMoonHighlightFalloff = cloudMoonHighlightFalloff ? cloudMoonHighlightFalloff : module.defaultSettings.cloudMoonHighlightFalloff.GetFloatValue(time);
            module.cloudSunHighlightFalloff = cloudSunHighlightFalloff ? cloudSunHighlightFalloff : module.defaultSettings.cloudSunHighlightFalloff.GetFloatValue(time);
            module.cloudTextureColor = cloudTextureColor ? cloudTextureColor : module.defaultSettings.cloudTextureColor.GetColorValue(time);
            module.fogColor1 = fogColor1 ? fogColor1 : module.defaultSettings.fogColor1.GetColorValue(time);
            module.fogColor2 = fogColor2 ? fogColor2 : module.defaultSettings.fogColor2.GetColorValue(time);
            module.fogColor3 = fogColor3 ? fogColor3 : module.defaultSettings.fogColor3.GetColorValue(time);
            module.fogColor4 = fogColor4 ? fogColor4 : module.defaultSettings.fogColor4.GetColorValue(time);
            module.fogColor5 = fogColor5 ? fogColor5 : module.defaultSettings.fogColor5.GetColorValue(time);
            module.fogStart1 = fogStart1 ? fogStart1 : module.defaultSettings.fogStart1.GetFloatValue(time);
            module.fogStart2 = fogStart2 ? fogStart2 : module.defaultSettings.fogStart2.GetFloatValue(time);
            module.fogStart3 = fogStart3 ? fogStart3 : module.defaultSettings.fogStart3.GetFloatValue(time);
            module.fogStart4 = fogStart4 ? fogStart4 : module.defaultSettings.fogStart4.GetFloatValue(time);
            module.fogFlareColor = fogFlareColor ? fogFlareColor : module.defaultSettings.fogFlareColor.GetColorValue(time);
            module.fogHeight = fogHeight ? fogHeight : module.defaultSettings.fogHeight.GetFloatValue(time);
            module.fogSmoothness = fogSmoothness ? fogSmoothness : module.defaultSettings.fogSmoothness.GetFloatValue(time);
            module.fogVariationAmount = fogVariationAmount ? fogVariationAmount : module.defaultSettings.fogVariationAmount.GetFloatValue(time);
            module.fogDensityMultiplier = fogDensity ? fogDensity : module.defaultSettings.fogDensityMultiplier.GetFloatValue(time);
            module.fogLightFlareFalloff = fogLightFlareFalloff ? fogLightFlareFalloff : module.defaultSettings.fogLightFlareFalloff.GetFloatValue(time);
            module.fogLightFlareIntensity = fogLightFlareIntensity ? fogLightFlareIntensity : module.defaultSettings.fogLightFlareIntensity.GetFloatValue(time);
            module.fogLightFlareSquish = fogLightFlareSquish ? fogLightFlareSquish : module.defaultSettings.fogLightFlareSquish.GetFloatValue(time);
            module.galaxy1Color = galaxy1Color ? galaxy1Color : module.defaultSettings.galaxy1Color.GetColorValue(time);
            module.galaxy2Color = galaxy2Color ? galaxy2Color : module.defaultSettings.galaxy2Color.GetColorValue(time);
            module.galaxy3Color = galaxy3Color ? galaxy3Color : module.defaultSettings.galaxy3Color.GetColorValue(time);
            module.galaxyIntensity = galaxyIntensity ? galaxyIntensity : module.defaultSettings.galaxyIntensity.GetFloatValue(time);
            module.highAltitudeCloudColor = highAltitudeCloudColor ? highAltitudeCloudColor : module.defaultSettings.highAltitudeCloudColor.GetColorValue(time);
            module.lightScatteringColor = lightScatteringColor ? lightScatteringColor : module.defaultSettings.lightScatteringColor.GetColorValue(time);
            module.moonlightColor = moonlightColor ? moonlightColor : module.defaultSettings.moonlightColor.GetColorValue(time);
            module.moonFalloff = moonFalloff ? moonFalloff : module.defaultSettings.moonFalloff.GetFloatValue(time);
            module.moonFlareColor = moonFlareColor ? moonFlareColor : module.defaultSettings.moonFlareColor.GetColorValue(time);
            module.skyHorizonColor = skyHorizonColor ? skyHorizonColor : module.defaultSettings.skyHorizonColor.GetColorValue(time);
            module.skyZenithColor = skyZenithColor ? skyZenithColor : module.defaultSettings.skyZenithColor.GetColorValue(time);
            module.starColor = starColor ? starColor : module.defaultSettings.starColor.GetColorValue(time);
            module.sunColor = sunColor ? sunColor : module.defaultSettings.sunColor.GetColorValue(time);
            module.sunFalloff = sunFalloff ? sunFalloff : module.defaultSettings.sunFalloff.GetFloatValue(time);
            module.sunFlareColor = sunFlareColor ? sunFlareColor : module.defaultSettings.sunFlareColor.GetColorValue(time);
            module.sunlightColor = sunlightColor ? sunlightColor : module.defaultSettings.sunlightColor.GetColorValue(time);
            module.sunSize = sunSize ? sunSize : module.defaultSettings.sunSize.GetFloatValue(time);
            if (extension != null) extension.SingleBlock();

        }

        public override void AdjustColors(ColorAdjustment colorMethod, float adjustment)
        {
            skyZenithColor = colorMethod(skyZenithColor, adjustment);
            skyHorizonColor = colorMethod(skyHorizonColor, adjustment);
            cloudColor = colorMethod(cloudColor, adjustment);
            cloudHighlightColor = colorMethod(cloudHighlightColor, adjustment);
            highAltitudeCloudColor = colorMethod(highAltitudeCloudColor, adjustment);
            sunlightColor = colorMethod(sunlightColor, adjustment);
            moonlightColor = colorMethod(moonlightColor, adjustment);
            starColor = colorMethod(starColor, adjustment);
            ambientLightHorizonColor = colorMethod(ambientLightHorizonColor, adjustment);
            ambientLightZenithColor = colorMethod(ambientLightZenithColor, adjustment);
            fogColor1 = colorMethod(fogColor1, adjustment);
            fogColor2 = colorMethod(fogColor2, adjustment);
            fogColor3 = colorMethod(fogColor3, adjustment);
            fogColor4 = colorMethod(fogColor4, adjustment);
            fogColor5 = colorMethod(fogColor5, adjustment);
            fogFlareColor = colorMethod(fogFlareColor, adjustment);
            sunColor = colorMethod(sunColor, adjustment);
            sunFlareColor = colorMethod(sunFlareColor, adjustment);
            moonFlareColor = colorMethod(moonFlareColor, adjustment);
            galaxy1Color = colorMethod(galaxy1Color, adjustment);
            galaxy2Color = colorMethod(galaxy2Color, adjustment);
            galaxy3Color = colorMethod(galaxy3Color, adjustment);
            lightScatteringColor = colorMethod(lightScatteringColor, adjustment);
            cloudMoonColor = colorMethod(cloudMoonColor, adjustment);

        }

    }
}