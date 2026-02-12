using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DistantLands.Cozy.Data
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "Distant Lands/Cozy/Color Block", order = 361)]
    public class ColorBlock : BlocksBlendable
    {

        public enum BlockStyle { advanced, simple }
        public BlockStyle blockStyle;

        #region Block Atmosphere


        [Tooltip("Sets the color of the zenith (or top) of the skybox at a certain time. Starts and ends at midnight.")]
        [ColorUsage(false, true)]
        public Color skyZenithColor;

        [Tooltip("Sets the color of the horizon (or middle) of the skybox at a certain time. Starts and ends at midnight.")]
        [ColorUsage(false, true)]
        public Color skyHorizonColor;

        [Tooltip("Sets the main color of the clouds at a certain time. Starts and ends at midnight.")]
        [ColorUsage(false, true)]
        public Color cloudColor;

        [Tooltip("Sets the highlight color of the clouds at a certain time. Starts and ends at midnight.")]
        [ColorUsage(false, true)]
        public Color cloudHighlightColor;

        [Tooltip("Sets the color of the high altitude clouds at a certain time. Starts and ends at midnight.")]
        [ColorUsage(false, true)]
        public Color highAltitudeCloudColor;

        [Tooltip("Sets the color of the sun light source at a certain time. Starts and ends at midnight.")]
        [ColorUsage(false, true)]
        public Color sunlightColor;

        [Tooltip("Sets the color of the moon light source at a certain time. Starts and ends at midnight.")]
        [ColorUsage(false, true)]
        public Color moonlightColor;

        [Tooltip("Sets the color of the star particle FX and textures at a certain time. Starts and ends at midnight.")]
        [ColorUsage(false, true)]
        public Color starColor;

        [Tooltip("Sets the color of the zenith (or top) of the ambient scene lighting at a certain time. Starts and ends at midnight.")]
        [ColorUsage(false, true)]
        public Color ambientLightHorizonColor;

        [Tooltip("Sets the color of the horizon (or middle) of the ambient scene lighting at a certain time. Starts and ends at midnight.")]
        [ColorUsage(false, true)]
        public Color ambientLightZenithColor;

        [Tooltip("Multiplies the ambient light intensity.")]
        [Range(0, 4)]
        public float ambientLightMultiplier;

        [Tooltip("Sets the intensity of the galaxy effects at a certain time. Starts and ends at midnight.")]
        [Range(0, 1)]
        public float galaxyIntensity;

        public float fogDensity = 1;
        public float fogVariationAmount = 1;



        [Tooltip("Sets the fog color from 0m away from the camera to fog start 1.")]

        [ColorUsage(true, true)]
        public Color fogColor1;

        [Tooltip("Sets the fog color from fog start 1 to fog start 2.")]

        [ColorUsage(true, true)]
        public Color fogColor2;
        [Tooltip("Sets the fog color from fog start 2 to fog start 3.")]


        [ColorUsage(true, true)]
        public Color fogColor3;
        [Tooltip("Sets the fog color from fog start 3 to fog start 4.")]


        [ColorUsage(true, true)]
        public Color fogColor4;
        [Tooltip("Sets the fog color from fog start 4 to fog start 5.")]


        [ColorUsage(true, true)]
        public Color fogColor5;

        [Tooltip("Sets the color of the fog flare.")]

        [ColorUsage(true, true)]
        public Color fogFlareColor;




        [Range(0, 1)]
        [Tooltip("Controls the exponent used to modulate from the horizon color to the zenith color of the sky.")]
        public float gradientExponent;
        [Range(0, 5)]
        [Tooltip("Sets the size of the visual sun in the sky.")]
        public float sunSize;

        [Tooltip("Sets the color of the visual sun in the sky.")]
        [ColorUsage(false, true)]
        public Color sunColor;


        [Range(0, 100)]
        [Tooltip("Sets the falloff of the halo around the visual sun.")]
        public float sunFalloff;

        [Tooltip("Sets the color of the halo around the visual sun.")]

        [ColorUsage(false, true)]
        public Color sunFlareColor;
        [Range(0, 100)]
        [Tooltip("Sets the falloff of the halo around the main moon.")]
        public float moonFalloff;

        [Tooltip("Sets the color of the halo around the main moon.")]

        [ColorUsage(false, true)]
        public Color moonFlareColor;

        [Tooltip("Sets the color of the first galaxy algorithm.")]

        [ColorUsage(false, true)]
        public Color galaxy1Color;

        [Tooltip("Sets the color of the second galaxy algorithm.")]

        [ColorUsage(false, true)]
        public Color galaxy2Color;

        [Tooltip("Sets the color of the third galaxy algorithm.")]

        [ColorUsage(false, true)]
        public Color galaxy3Color;

        [Tooltip("Sets the color of the light columns around the horizon.")]

        [ColorUsage(false, true)]
        public Color lightScatteringColor;

        [Tooltip("Sets the distance at which the first fog color fades into the second fog color.")]
        public float fogStart1;
        public float fogStart2;
        public float fogStart3;
        public float fogStart4;
        [Range(0, 2)]
        public float fogHeight;
        [Range(0, 1)]
        public float fogSmoothness = 0.5f;
        [Range(0, 2)]
        public float fogLightFlareIntensity;
        [Range(0, 40)]
        public float fogLightFlareFalloff;
        [Range(0, 10)]
        [Tooltip("Sets the height divisor for the fog flare. High values sit the flare closer to the horizon, small values extend the flare into the sky.")]
        public float fogLightFlareSquish;



        [ColorUsage(false, true)]
        public Color cloudMoonColor;
        [Range(0, 50)]
        public float cloudSunHighlightFalloff;
        [Range(0, 50)]
        public float cloudMoonHighlightFalloff;

        [ColorUsage(false, true)]
        public Color cloudTextureColor;

        public ColorBlockExtension extension;
        #endregion

        #region Simple Variables

        [ColorUsage(false, true)]
        [Tooltip("Controls the color for the skybox")]
        public Color skyColor;

        [ColorUsage(false, true)]
        [Tooltip("Controls the color for the fog")]
        public Color fogColor;

        [ColorUsage(false, true)]
        public Color simpleSunColor;

        [ColorUsage(false, true)]
        public Color simpleCloudColor;

        [ColorUsage(false, true)]
        public Color moonColor;


        [Tooltip("Controls the amount of night FX in the scene")]
        public float nightFXAmount;

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

        public override void SingleBlockBlend(BlocksModule module)
        {
            module.gradientExponent = gradientExponent;
            module.ambientLightHorizonColor = ambientLightHorizonColor;
            module.ambientLightZenithColor = ambientLightZenithColor;
            module.ambientLightMultiplier = ambientLightMultiplier;
            module.cloudColor = cloudColor;
            module.cloudHighlightColor = cloudHighlightColor;
            module.cloudMoonColor = cloudMoonColor;
            module.cloudMoonHighlightFalloff = cloudMoonHighlightFalloff;
            module.cloudSunHighlightFalloff = cloudSunHighlightFalloff;
            module.cloudTextureColor = cloudTextureColor;
            module.fogColor1 = fogColor1;
            module.fogColor2 = fogColor2;
            module.fogColor3 = fogColor3;
            module.fogColor4 = fogColor4;
            module.fogColor5 = fogColor5;
            module.fogStart1 = fogStart1;
            module.fogStart2 = fogStart2;
            module.fogStart3 = fogStart3;
            module.fogStart4 = fogStart4;
            module.fogFlareColor = fogFlareColor;
            module.fogHeight = fogHeight;
            module.fogSmoothness = fogSmoothness;
            module.fogVariationAmount = fogVariationAmount;
            module.fogDensityMultiplier = fogDensity;
            module.fogLightFlareFalloff = fogLightFlareFalloff;
            module.fogLightFlareIntensity = fogLightFlareIntensity;
            module.fogLightFlareSquish = fogLightFlareSquish;
            module.galaxy1Color = galaxy1Color;
            module.galaxy2Color = galaxy2Color;
            module.galaxy3Color = galaxy3Color;
            module.galaxyIntensity = galaxyIntensity;
            module.highAltitudeCloudColor = highAltitudeCloudColor;
            module.lightScatteringColor = lightScatteringColor;
            module.moonlightColor = moonlightColor;
            module.moonFalloff = moonFalloff;
            module.moonFlareColor = moonFlareColor;
            module.skyHorizonColor = skyHorizonColor;
            module.skyZenithColor = skyZenithColor;
            module.starColor = starColor;
            module.sunColor = sunColor;
            module.sunFalloff = sunFalloff;
            module.sunFlareColor = sunFlareColor;
            module.sunlightColor = sunlightColor;
            module.sunSize = sunSize;
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

        public override ColorBlock GetValues(BlocksModule module)
        {
            return this;
        }

        public static void ConvertToOverride(ColorBlock instance)
        {
            ColorBlockOverride colorBlockOverride = CreateInstance<ColorBlockOverride>();
            colorBlockOverride.skyZenithColor = new Overridable<Color>(instance.skyZenithColor, true);
            colorBlockOverride.skyHorizonColor = new Overridable<Color>(instance.skyHorizonColor, true);
            colorBlockOverride.cloudColor = new Overridable<Color>(instance.cloudColor, true);
            colorBlockOverride.cloudHighlightColor = new Overridable<Color>(instance.cloudHighlightColor, true);
            colorBlockOverride.highAltitudeCloudColor = new Overridable<Color>(instance.highAltitudeCloudColor, true);
            colorBlockOverride.sunlightColor = new Overridable<Color>(instance.sunlightColor, true);
            colorBlockOverride.moonlightColor = new Overridable<Color>(instance.moonlightColor, true);
            colorBlockOverride.starColor = new Overridable<Color>(instance.starColor, true);
            colorBlockOverride.ambientLightHorizonColor = new Overridable<Color>(instance.ambientLightHorizonColor, true);
            colorBlockOverride.ambientLightZenithColor = new Overridable<Color>(instance.ambientLightZenithColor, true);
            colorBlockOverride.ambientLightMultiplier = new Overridable<float>(instance.ambientLightMultiplier, true);
            colorBlockOverride.galaxyIntensity = new Overridable<float>(instance.galaxyIntensity, true);
            colorBlockOverride.fogDensity = new Overridable<float>(instance.fogDensity, true);
            colorBlockOverride.fogVariationAmount = new Overridable<float>(instance.fogVariationAmount, true);
            colorBlockOverride.fogColor1 = new Overridable<Color>(instance.fogColor1, true);
            colorBlockOverride.fogColor2 = new Overridable<Color>(instance.fogColor2, true);
            colorBlockOverride.fogColor3 = new Overridable<Color>(instance.fogColor3, true);
            colorBlockOverride.fogColor4 = new Overridable<Color>(instance.fogColor4, true);
            colorBlockOverride.fogColor5 = new Overridable<Color>(instance.fogColor5, true);
            colorBlockOverride.fogFlareColor = new Overridable<Color>(instance.fogFlareColor, true);
            colorBlockOverride.gradientExponent = new Overridable<float>(instance.gradientExponent, true);
            colorBlockOverride.sunSize = new Overridable<float>(instance.sunSize, true);
            colorBlockOverride.sunColor = new Overridable<Color>(instance.sunColor, true);
            colorBlockOverride.sunFalloff = new Overridable<float>(instance.sunFalloff, true);
            colorBlockOverride.sunFlareColor = new Overridable<Color>(instance.sunFlareColor, true);
            colorBlockOverride.moonFalloff = new Overridable<float>(instance.moonFalloff, true);
            colorBlockOverride.moonFlareColor = new Overridable<Color>(instance.moonFlareColor, true);
            colorBlockOverride.galaxy1Color = new Overridable<Color>(instance.galaxy1Color, true);
            colorBlockOverride.galaxy2Color = new Overridable<Color>(instance.galaxy2Color, true);
            colorBlockOverride.galaxy3Color = new Overridable<Color>(instance.galaxy3Color, true);
            colorBlockOverride.lightScatteringColor = new Overridable<Color>(instance.lightScatteringColor, true);
            colorBlockOverride.fogStart1 = new Overridable<float>(instance.fogStart1, true);
            colorBlockOverride.fogStart2 = new Overridable<float>(instance.fogStart2, true);
            colorBlockOverride.fogStart3 = new Overridable<float>(instance.fogStart3, true);
            colorBlockOverride.fogStart4 = new Overridable<float>(instance.fogStart4, true);
            colorBlockOverride.fogHeight = new Overridable<float>(instance.fogHeight, true);
            colorBlockOverride.fogSmoothness = new Overridable<float>(instance.fogSmoothness, true);
            colorBlockOverride.fogLightFlareIntensity = new Overridable<float>(instance.fogLightFlareIntensity, true);
            colorBlockOverride.fogLightFlareFalloff = new Overridable<float>(instance.fogLightFlareFalloff, true);
            colorBlockOverride.fogLightFlareSquish = new Overridable<float>(instance.fogLightFlareSquish, true);
            colorBlockOverride.cloudMoonColor = new Overridable<Color>(instance.cloudMoonColor, true);
            colorBlockOverride.cloudSunHighlightFalloff = new Overridable<float>(instance.cloudSunHighlightFalloff, true);
            colorBlockOverride.cloudMoonHighlightFalloff = new Overridable<float>(instance.cloudMoonHighlightFalloff, true);
            colorBlockOverride.cloudTextureColor = new Overridable<Color>(instance.cloudTextureColor, true);

#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(instance);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(colorBlockOverride, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif


        }

        public static void ConvertToColorBlock(ColorBlock instance)
        {
            ColorBlock colorBlock = CreateInstance<ColorBlock>();
            colorBlock.skyZenithColor = instance.skyZenithColor;
            colorBlock.skyHorizonColor = instance.skyHorizonColor;
            colorBlock.cloudColor = instance.cloudColor;
            colorBlock.cloudHighlightColor = instance.cloudHighlightColor;
            colorBlock.highAltitudeCloudColor = instance.highAltitudeCloudColor;
            colorBlock.sunlightColor = instance.sunlightColor;
            colorBlock.moonlightColor = instance.moonlightColor;
            colorBlock.starColor = instance.starColor;
            colorBlock.ambientLightHorizonColor = instance.ambientLightHorizonColor;
            colorBlock.ambientLightZenithColor = instance.ambientLightZenithColor;
            colorBlock.ambientLightMultiplier = instance.ambientLightMultiplier;
            colorBlock.galaxyIntensity = instance.galaxyIntensity;
            colorBlock.fogDensity = instance.fogDensity;
            colorBlock.fogVariationAmount = instance.fogVariationAmount;
            colorBlock.fogColor1 = instance.fogColor1;
            colorBlock.fogColor2 = instance.fogColor2;
            colorBlock.fogColor3 = instance.fogColor3;
            colorBlock.fogColor4 = instance.fogColor4;
            colorBlock.fogColor5 = instance.fogColor5;
            colorBlock.fogFlareColor = instance.fogFlareColor;
            colorBlock.gradientExponent = instance.gradientExponent;
            colorBlock.sunSize = instance.sunSize;
            colorBlock.sunColor = instance.sunColor;
            colorBlock.sunFalloff = instance.sunFalloff;
            colorBlock.sunFlareColor = instance.sunFlareColor;
            colorBlock.moonFalloff = instance.moonFalloff;
            colorBlock.moonFlareColor = instance.moonFlareColor;
            colorBlock.galaxy1Color = instance.galaxy1Color;
            colorBlock.galaxy2Color = instance.galaxy2Color;
            colorBlock.galaxy3Color = instance.galaxy3Color;
            colorBlock.lightScatteringColor = instance.lightScatteringColor;
            colorBlock.fogStart1 = instance.fogStart1;
            colorBlock.fogStart2 = instance.fogStart2;
            colorBlock.fogStart3 = instance.fogStart3;
            colorBlock.fogStart4 = instance.fogStart4;
            colorBlock.fogHeight = instance.fogHeight;
            colorBlock.fogSmoothness = instance.fogSmoothness;
            colorBlock.fogLightFlareIntensity = instance.fogLightFlareIntensity;
            colorBlock.fogLightFlareFalloff = instance.fogLightFlareFalloff;
            colorBlock.fogLightFlareSquish = instance.fogLightFlareSquish;
            colorBlock.cloudMoonColor = instance.cloudMoonColor;
            colorBlock.cloudSunHighlightFalloff = instance.cloudSunHighlightFalloff;
            colorBlock.cloudMoonHighlightFalloff = instance.cloudMoonHighlightFalloff;
            colorBlock.cloudTextureColor = instance.cloudTextureColor;

#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(instance);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(colorBlock, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(BlocksBlendable))]
    [CanEditMultipleObjects]
    public class E_BlocksBlendable : Editor
    {

        public static bool selectionWindowOpen;
        public static bool atmosphereWindowOpen;
        public static bool cloudsWindowOpen;
        public static bool celestialWindowOpen;
        public static bool colorAdjustmentWindowOpen;
        public static bool fogWindowOpen;

        public static bool tooltips;

        public CozyWeather defaultWeather;
        public BlocksBlendable cb;

        void OnEnable()
        {

            if (CozyWeather.instance)
                defaultWeather = CozyWeather.instance;


            cb = (BlocksBlendable)target;

        }

        public override void OnInspectorGUI()
        {

            tooltips = EditorPrefs.GetBool("CZY_Tooltips", true);

            if (defaultWeather)
                OnInspectorGUIInline(defaultWeather);
            else
                EditorGUILayout.HelpBox("To edit the Color Block make sure that your scene is properly setup with a COZY system!", MessageType.Warning);

        }

        public void DrawSimpleSettings()
        {

            GUIStyle labelStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
            labelStyle.fontStyle = FontStyle.Bold;

            EditorGUILayout.LabelField(" Skydome Settings", labelStyle);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("skyColor"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogColor"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogDensity"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("simpleSunColor"), new GUIContent("Sun Color"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("simpleCloudColor"), new GUIContent("Cloud Color"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("moonColor"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("nightFXAmount"), false);

            serializedObject.ApplyModifiedProperties();

        }

        public void OnInspectorGUIInline(CozyWeather cozyWeather)
        {


            serializedObject.Update();
            tooltips = EditorPrefs.GetBool("CZY_Tooltips", true);
            selectionWindowOpen = EditorGUILayout.BeginFoldoutHeaderGroup(selectionWindowOpen,
                new GUIContent("    Selection Settings"), EditorUtilities.FoldoutStyle);
            EditorGUILayout.EndFoldoutHeaderGroup();
            if (selectionWindowOpen)
            {

                EditorGUI.indentLevel++;

                // EditorGUILayout.PropertyField(serializedObject.FindProperty("blockStyle"));
                // EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chance"));
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("extension"));
                EditorGUILayout.Space();

                EditorGUI.indentLevel--;

            }

            colorAdjustmentWindowOpen = EditorGUILayout.BeginFoldoutHeaderGroup(colorAdjustmentWindowOpen,
                new GUIContent("    Adjust Colors"), EditorUtilities.FoldoutStyle);

            if (colorAdjustmentWindowOpen)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField("Hue Shift");
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("-15%"))
                {
                    Undo.RecordObject(cb, "Adjust Color");
                    cb.AdjustColors(BlocksBlendable.HueShift, -0.15f);
                }
                if (GUILayout.Button("-5%"))
                {
                    Undo.RecordObject(cb, "Adjust Color");
                    cb.AdjustColors(BlocksBlendable.HueShift, -0.05f);
                }
                if (GUILayout.Button("+5%"))
                {
                    Undo.RecordObject(cb, "Adjust Color");
                    cb.AdjustColors(BlocksBlendable.HueShift, 0.05f);
                }
                if (GUILayout.Button("+15%"))
                {
                    Undo.RecordObject(cb, "Adjust Color");
                    cb.AdjustColors(BlocksBlendable.HueShift, 0.15f);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField("Saturation Shift");
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("-15%"))
                {
                    Undo.RecordObject(cb, "Adjust Color");
                    cb.AdjustColors(BlocksBlendable.SaturationShift, -0.15f);
                }
                if (GUILayout.Button("-5%"))
                {
                    Undo.RecordObject(cb, "Adjust Color");
                    cb.AdjustColors(BlocksBlendable.SaturationShift, -0.05f);
                }
                if (GUILayout.Button("+5%"))
                {
                    Undo.RecordObject(cb, "Adjust Color");
                    cb.AdjustColors(BlocksBlendable.SaturationShift, 0.05f);
                }
                if (GUILayout.Button("+15%"))
                {
                    Undo.RecordObject(cb, "Adjust Color");
                    cb.AdjustColors(BlocksBlendable.SaturationShift, 0.15f);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField("Value Shift");
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("-15%"))
                {
                    Undo.RecordObject(cb, "Adjust Color");
                    cb.AdjustColors(BlocksBlendable.ValueShift, -0.15f);
                }
                if (GUILayout.Button("-5%"))
                {
                    Undo.RecordObject(cb, "Adjust Color");
                    cb.AdjustColors(BlocksBlendable.ValueShift, -0.05f);
                }
                if (GUILayout.Button("+5%"))
                {
                    Undo.RecordObject(cb, "Adjust Color");
                    cb.AdjustColors(BlocksBlendable.ValueShift, 0.05f);
                }
                if (GUILayout.Button("+15%"))
                {
                    Undo.RecordObject(cb, "Adjust Color");
                    cb.AdjustColors(BlocksBlendable.ValueShift, 0.15f);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();

                if (GUILayout.Button("Pull values from atmosphere"))
                    cb.PullFromAtmosphere();

                EditorGUI.indentLevel--;

            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            if (cb.GetType() == typeof(ColorBlock) && (ColorBlock.BlockStyle)serializedObject.FindProperty("blockStyle").enumValueIndex == ColorBlock.BlockStyle.simple)
            {

                selectionWindowOpen = EditorGUILayout.BeginFoldoutHeaderGroup(selectionWindowOpen,
                    new GUIContent("    Settings", "Controls the base versions of properties."), EditorUtilities.FoldoutStyle);

                if (selectionWindowOpen)
                    DrawSimpleSettings();

                serializedObject.ApplyModifiedProperties();
                return;

            }


            atmosphereWindowOpen = EditorGUILayout.BeginFoldoutHeaderGroup(atmosphereWindowOpen,
                new GUIContent("    Atmosphere & Lighting", "Skydome and lighting settings."), EditorUtilities.FoldoutStyle);

            if (atmosphereWindowOpen)
            {

                DrawAtmosphereTab(cozyWeather);

            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            fogWindowOpen = EditorGUILayout.BeginFoldoutHeaderGroup(fogWindowOpen,
                new GUIContent("    Fog", "Fog settings."), EditorUtilities.FoldoutStyle);

            if (fogWindowOpen)
            {

                DrawFogTab(cozyWeather);

            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            cloudsWindowOpen = EditorGUILayout.BeginFoldoutHeaderGroup(cloudsWindowOpen,
                            new GUIContent("    Clouds", "Cloud color, generation, and variation settings."), EditorUtilities.FoldoutStyle);

            if (cloudsWindowOpen)
            {

                DrawCloudsTab(cozyWeather);

            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            celestialWindowOpen = EditorGUILayout.BeginFoldoutHeaderGroup(celestialWindowOpen,
                            new GUIContent("    Celestials & VFX", "Sun, moon, and light FX settings."), EditorUtilities.FoldoutStyle);

            if (celestialWindowOpen)
            {

                DrawCelestialsTab(cozyWeather);

            }

            EditorGUILayout.EndFoldoutHeaderGroup();


            serializedObject.ApplyModifiedProperties();


        }

        void DrawAtmosphereTab(CozyWeather cozyWeather)
        {

            GUIStyle labelStyle = new GUIStyle(GUI.skin.GetStyle("Label"))
            {
                fontStyle = FontStyle.Bold
            };

            if (tooltips)
                EditorGUILayout.HelpBox("Interpolate controls change the value depending on the time of day. These range from 00:00 to 23:59, which means that morning is about 25% through the curve, midday 50%, evening 75%, etc. \n \n Constant controls set the value to a single value that remains constant regardless of the time of day.", MessageType.Info);


            EditorGUILayout.LabelField(" Skydome Settings", labelStyle);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("skyZenithColor"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("skyHorizonColor"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gradientExponent"), false);

            EditorGUILayout.Space(5);
            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField(" Lighting Settings", labelStyle);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("sunlightColor"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("moonlightColor"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ambientLightHorizonColor"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ambientLightZenithColor"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ambientLightMultiplier"), false);
            EditorGUI.indentLevel--;


        }

        void DrawFogTab(CozyWeather cozyWeather)
        {

            GUIStyle labelStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
            labelStyle.fontStyle = FontStyle.Bold;


            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogColor1"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogColor2"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogColor3"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogColor4"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogColor5"), false);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogStart1"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogStart2"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogStart3"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogStart4"), false);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogHeight"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogSmoothness"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogDensity"), false);


            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogFlareColor"), new GUIContent("Light Flare Color",
                "Sets the color of the fog for a false \"light flare\" around the main sun directional light."), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogLightFlareIntensity"), new GUIContent("Light Flare Intensity",
                "Modulates the brightness of the light flare."), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogLightFlareFalloff"), new GUIContent("Light Flare Falloff",
                "Sets the falloff speed for the light flare."), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogLightFlareSquish"), new GUIContent("Light Flare Squish",
                "Sets the height divisor for the fog flare. High values sit the flare closer to the horizon, small values extend the flare into the sky."), false);

            EditorGUI.indentLevel--;

        }

        public void RenderInWindow(Rect pos)
        {

            float space = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var propPosA = new Rect(pos.x, pos.y + space, pos.width, EditorGUIUtility.singleLineHeight);

            serializedObject.Update();

            EditorGUI.PropertyField(propPosA, serializedObject.FindProperty("chance"));

            serializedObject.ApplyModifiedProperties();
        }

        void DrawCloudsTab(CozyWeather cozyWeather)
        {

            Material cloudShader = cozyWeather.cloudMesh.sharedMaterial;

            if (tooltips)
                EditorGUILayout.HelpBox("Interpolate controls change the value depending on the time of day. These range from 00:00 to 23:59, which means that morning is about 25% through the curve, midday 50%, evening 75%, etc. \n \n Constant controls set the value to a single value that remains constant regardless of the time of day.", MessageType.Info);


            GUIStyle labelStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
            labelStyle.fontStyle = FontStyle.Bold;

            EditorGUILayout.LabelField(" Color Settings", labelStyle);
            EditorGUI.indentLevel++;


            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudColor"), new GUIContent("Cloud Color", "The main color of the unlit clouds."), false);
            if (cloudShader.HasProperty("_AltoCloudColor"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("highAltitudeCloudColor"), new GUIContent("High Altitude Color", "The main color multiplier of the high altitude clouds. The cloud types affected are the cirrostratus and the altocumulus types."), false);
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudHighlightColor"), new GUIContent("Sun Highlight Color", "The color multiplier for the clouds in a \"dot\" around the sun."), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSunHighlightFalloff"), new GUIContent("Sun Highlight Falloff", "The falloff for the \"dot\" around the sun."), false);
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudMoonColor"), new GUIContent("Moon Highlight Color", "The color multiplier for the clouds in a \"dot\" around the moon."), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudMoonHighlightFalloff"), new GUIContent("Moon Highlight Falloff", "The falloff for the \"dot\" around the moon."), false);


            EditorGUI.indentLevel--;


        }

        void DrawCelestialsTab(CozyWeather cozyWeather)
        {


            bool advancedSky = cozyWeather.skyStyle == CozyWeather.SkyStyle.desktop;


            if (tooltips)
                EditorGUILayout.HelpBox("Interpolate controls change the value depending on the time of day. These range from 00:00 to 23:59, which means that morning is about 25% through the curve, midday 50%, evening 75%, etc. \n \n Constant controls set the value to a single value that remains constant regardless of the time of day.", MessageType.Info);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
            labelStyle.fontStyle = FontStyle.Bold;

            EditorGUILayout.LabelField(" Sun Settings", labelStyle);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sunColor"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sunSize"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sunFalloff"), new GUIContent("Sun Halo Falloff"), false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sunFlareColor"), new GUIContent("Sun Halo Color"), false);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(16);

            if (advancedSky)
            {
                EditorGUILayout.LabelField(" Moon Settings", labelStyle);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("moonFalloff"), new GUIContent("Moon Halo Falloff"), false);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("moonFlareColor"), new GUIContent("Moon Halo Color"), false);
                EditorGUI.indentLevel--;
            }


            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField(" VFX", labelStyle);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("starColor"), false);

            if (advancedSky)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("galaxyIntensity"), false);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("galaxy1Color"), false);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("galaxy2Color"), false);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("galaxy3Color"), false);
                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lightScatteringColor"), false);
                EditorGUILayout.Space(5);
            }
            EditorGUI.indentLevel--;


        }
    }

    [CustomEditor(typeof(ColorBlock))]
    public class E_ColorBlock : E_BlocksBlendable
    {
        public override void OnInspectorGUI()
        {

            tooltips = EditorPrefs.GetBool("CZY_Tooltips", true);

            if (defaultWeather)
            {
                OnInspectorGUIInline(defaultWeather);
                if (GUILayout.Button("Convert to Overridable"))
                    ColorBlock.ConvertToOverride((ColorBlock)cb);
            }
            else
                EditorGUILayout.HelpBox("To edit the Color Block make sure that your scene is properly setup with a COZY system!", MessageType.Warning);

        }
    }

    [CustomEditor(typeof(ColorBlockOverride))]
    public class E_ColorBlockOverride : E_BlocksBlendable
    {

    }

#endif
}