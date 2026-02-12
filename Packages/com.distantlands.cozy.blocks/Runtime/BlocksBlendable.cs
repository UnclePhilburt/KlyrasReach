using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DistantLands.Cozy.Data
{
    [System.Serializable]
    public abstract class BlocksBlendable : CozyProfile
    {

        public WeightedRandomChance chance;
        public delegate Color ColorAdjustment(Color color, float adjustment);
        public abstract void AdjustColors(ColorAdjustment colorMethod, float adjustment);

        public abstract void PullFromAtmosphere();
        public abstract void SingleBlockBlend(BlocksModule module);
        public abstract ColorBlock GetValues(BlocksModule module);
        public static Color HueShift(Color color, float shift)
        {
            float h, s, v, a;
            a = color.a;
            Color.RGBToHSV(color, out h, out s, out v);
            h += shift;
            Color colorPlusAlpha = Color.HSVToRGB(h, s, v, true);
            colorPlusAlpha.a = a;
            return colorPlusAlpha;
        }
        public static Color ValueShift(Color color, float shift)
        {
            float h, s, v, a;
            a = color.a;
            Color.RGBToHSV(color, out h, out s, out v);
            v += shift;
            Color colorPlusAlpha = Color.HSVToRGB(h, s, v, true);
            colorPlusAlpha.a = a;
            return colorPlusAlpha;
        }
        public static Color SaturationShift(Color color, float shift)
        {
            float h, s, v, a;
            a = color.a;
            Color.RGBToHSV(color, out h, out s, out v);
            s += shift;
            Color colorPlusAlpha = Color.HSVToRGB(h, s, v, true);
            colorPlusAlpha.a = a;
            return colorPlusAlpha;
        }

    }
}
