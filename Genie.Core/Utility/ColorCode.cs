using System;

namespace GenieClient.Genie
{
    internal class ColorCode
    {
        // === Primary methods (GenieColor-based) ===

        public static string ColorToString(GenieColor oColor)
        {
            if (oColor.IsNamedColor)
            {
                return oColor.Name;
            }
            else
            {
                return ColorToHex(oColor);
            }
        }

        public static string ColorToString(GenieColor oFgColor, GenieColor oBgColor)
        {
            return ColorToString(oFgColor) + ", " + ColorToString(oBgColor);
        }

        public static GenieColor StringToColor(string sColor)
        {
            try
            {
                if (sColor.StartsWith("@"))
                    return default;
                if (IsHexString(sColor))
                {
                    return HexToColor(sColor);
                }
                else if (IsColorString(sColor))
                {
                    return GenieColor.FromName(sColor);
                }
            }
            catch (Exception)
            {
                return default;
            }

            return default;
        }

        public static int ColorToColorref(GenieColor clr)
        {
            return clr.R | (clr.G << 8) | (clr.B << 16);
        }

        public static string ColorToHex(GenieColor oColor)
        {
            return "#" + oColor.R.ToString("X2") + oColor.G.ToString("X2") + oColor.B.ToString("X2");
        }

        public static GenieColor ColorToLighter(GenieColor oColor)
        {
            int R = (int)(oColor.R / 1.299);
            int G = (int)(oColor.G / 1.587);
            int B = (int)(oColor.B / 1.114);
            return GenieColor.FromArgb(R, G, B);
        }

        public static GenieColor ColorToDarker(GenieColor oColor)
        {
            int R = (int)(oColor.R * 0.299);
            int G = (int)(oColor.G * 0.587);
            int B = (int)(oColor.B * 0.114);
            return GenieColor.FromArgb(R, G, B);
        }

        public static GenieColor ColorToGrayscale(GenieColor oColor)
        {
            int iColor = (int)(oColor.R * 0.299 + oColor.G * 0.587 + oColor.B * 0.114);
            return GenieColor.FromArgb(iColor, iColor, iColor);
        }

        public static GenieColor HexToColor(string sColor)
        {
            try
            {
                if (sColor.Length == 7 && sColor[0] == '#')
                {
                    int r = Convert.ToInt32(sColor.Substring(1, 2), 16);
                    int g = Convert.ToInt32(sColor.Substring(3, 2), 16);
                    int b = Convert.ToInt32(sColor.Substring(5, 2), 16);
                    return GenieColor.FromArgb(r, g, b);
                }
            }
            catch (Exception)
            {
                return default;
            }

            return default;
        }

        public const string ValidHexChars = "1234567890aAbBcCdDeEfF";

        public static bool IsHexString(string sText)
        {
            if (!sText.StartsWith("#"))
                return false;
            if (sText.Trim().Length != 7)
                return false;
            sText = sText.Substring(1);
            foreach (char c in sText.ToCharArray())
            {
                if (ValidHexChars.IndexOf(c) == -1)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsColorString(string sText)
        {
            return GenieColor.TryFromName(sText, out _);
        }
    }
}
