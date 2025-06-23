using UnityEngine;

namespace CenturionCC.System.Utils
{
    public static class ColorExtension
    {
        // From UnityEngine.ColorUtility
        // ReSharper disable once InconsistentNaming
        public static string ToHtmlStringRGBA(this Color color)
        {
            var color32 = new Color32((byte)Mathf.Clamp(Mathf.RoundToInt(color.r * byte.MaxValue), 0, byte.MaxValue),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * byte.MaxValue), 0, byte.MaxValue),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * byte.MaxValue), 0, byte.MaxValue),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.a * byte.MaxValue), 0, byte.MaxValue));
            return $"{color32.r:X2}{color32.g:X2}{color32.b:X2}{color32.a:X2}";
        }
    }
}