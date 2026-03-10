using System;
using System.Collections.Generic;

namespace GenieClient
{
    public readonly struct GenieColor : IEquatable<GenieColor>
    {
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;
        public readonly byte A;
        public readonly string Name;
        private readonly bool _isNotEmpty;

        public bool IsEmpty => !_isNotEmpty;
        public bool IsNamedColor => Name != null && !IsEmpty;

        private GenieColor(byte a, byte r, byte g, byte b, string name = null)
        {
            A = a;
            R = r;
            G = g;
            B = b;
            Name = name;
            _isNotEmpty = true;
        }

        // Factory methods
        public static GenieColor FromArgb(int r, int g, int b)
        {
            return new GenieColor(255, (byte)r, (byte)g, (byte)b);
        }

        public static GenieColor FromArgb(int a, int r, int g, int b)
        {
            return new GenieColor((byte)a, (byte)r, (byte)g, (byte)b);
        }

        public static GenieColor FromName(string name)
        {
            if (name != null && s_namedColors.TryGetValue(name, out var color))
                return color;
            return default;
        }

        public static bool TryFromName(string name, out GenieColor color)
        {
            if (name != null && s_namedColors.TryGetValue(name, out color))
                return true;
            color = default;
            return false;
        }

        // Equality
        public bool Equals(GenieColor other)
        {
            return A == other.A && R == other.R && G == other.G && B == other.B && _isNotEmpty == other._isNotEmpty;
        }

        public override bool Equals(object obj)
        {
            return obj is GenieColor c && Equals(c);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(A, R, G, B, _isNotEmpty);
        }

        public static bool operator ==(GenieColor left, GenieColor right) => left.Equals(right);
        public static bool operator !=(GenieColor left, GenieColor right) => !left.Equals(right);

        public override string ToString()
        {
            if (IsEmpty) return "GenieColor [Empty]";
            if (IsNamedColor) return $"GenieColor [{Name}]";
            return $"GenieColor [A={A}, R={R}, G={G}, B={B}]";
        }

        // Named color constants (matching System.Drawing.KnownColor values)
        public static readonly GenieColor Transparent = new GenieColor(0, 0, 0, 0, "Transparent");
        public static readonly GenieColor AliceBlue = new GenieColor(255, 240, 248, 255, "AliceBlue");
        public static readonly GenieColor AntiqueWhite = new GenieColor(255, 250, 235, 215, "AntiqueWhite");
        public static readonly GenieColor Aqua = new GenieColor(255, 0, 255, 255, "Aqua");
        public static readonly GenieColor Aquamarine = new GenieColor(255, 127, 255, 212, "Aquamarine");
        public static readonly GenieColor Azure = new GenieColor(255, 240, 255, 255, "Azure");
        public static readonly GenieColor Beige = new GenieColor(255, 245, 245, 220, "Beige");
        public static readonly GenieColor Bisque = new GenieColor(255, 255, 228, 196, "Bisque");
        public static readonly GenieColor Black = new GenieColor(255, 0, 0, 0, "Black");
        public static readonly GenieColor BlanchedAlmond = new GenieColor(255, 255, 235, 205, "BlanchedAlmond");
        public static readonly GenieColor Blue = new GenieColor(255, 0, 0, 255, "Blue");
        public static readonly GenieColor BlueViolet = new GenieColor(255, 138, 43, 226, "BlueViolet");
        public static readonly GenieColor Brown = new GenieColor(255, 165, 42, 42, "Brown");
        public static readonly GenieColor BurlyWood = new GenieColor(255, 222, 184, 135, "BurlyWood");
        public static readonly GenieColor CadetBlue = new GenieColor(255, 95, 158, 160, "CadetBlue");
        public static readonly GenieColor Chartreuse = new GenieColor(255, 127, 255, 0, "Chartreuse");
        public static readonly GenieColor Chocolate = new GenieColor(255, 210, 105, 30, "Chocolate");
        public static readonly GenieColor Coral = new GenieColor(255, 255, 127, 80, "Coral");
        public static readonly GenieColor CornflowerBlue = new GenieColor(255, 100, 149, 237, "CornflowerBlue");
        public static readonly GenieColor Cornsilk = new GenieColor(255, 255, 248, 220, "Cornsilk");
        public static readonly GenieColor Crimson = new GenieColor(255, 220, 20, 60, "Crimson");
        public static readonly GenieColor Cyan = new GenieColor(255, 0, 255, 255, "Cyan");
        public static readonly GenieColor DarkBlue = new GenieColor(255, 0, 0, 139, "DarkBlue");
        public static readonly GenieColor DarkCyan = new GenieColor(255, 0, 139, 139, "DarkCyan");
        public static readonly GenieColor DarkGoldenrod = new GenieColor(255, 184, 134, 11, "DarkGoldenrod");
        public static readonly GenieColor DarkGray = new GenieColor(255, 169, 169, 169, "DarkGray");
        public static readonly GenieColor DarkGreen = new GenieColor(255, 0, 100, 0, "DarkGreen");
        public static readonly GenieColor DarkKhaki = new GenieColor(255, 189, 183, 107, "DarkKhaki");
        public static readonly GenieColor DarkMagenta = new GenieColor(255, 139, 0, 139, "DarkMagenta");
        public static readonly GenieColor DarkOliveGreen = new GenieColor(255, 85, 107, 47, "DarkOliveGreen");
        public static readonly GenieColor DarkOrange = new GenieColor(255, 255, 140, 0, "DarkOrange");
        public static readonly GenieColor DarkOrchid = new GenieColor(255, 153, 50, 204, "DarkOrchid");
        public static readonly GenieColor DarkRed = new GenieColor(255, 139, 0, 0, "DarkRed");
        public static readonly GenieColor DarkSalmon = new GenieColor(255, 233, 150, 122, "DarkSalmon");
        public static readonly GenieColor DarkSeaGreen = new GenieColor(255, 143, 188, 143, "DarkSeaGreen");
        public static readonly GenieColor DarkSlateBlue = new GenieColor(255, 72, 61, 139, "DarkSlateBlue");
        public static readonly GenieColor DarkSlateGray = new GenieColor(255, 47, 79, 79, "DarkSlateGray");
        public static readonly GenieColor DarkTurquoise = new GenieColor(255, 0, 206, 209, "DarkTurquoise");
        public static readonly GenieColor DarkViolet = new GenieColor(255, 148, 0, 211, "DarkViolet");
        public static readonly GenieColor DeepPink = new GenieColor(255, 255, 20, 147, "DeepPink");
        public static readonly GenieColor DeepSkyBlue = new GenieColor(255, 0, 191, 255, "DeepSkyBlue");
        public static readonly GenieColor DimGray = new GenieColor(255, 105, 105, 105, "DimGray");
        public static readonly GenieColor DodgerBlue = new GenieColor(255, 30, 144, 255, "DodgerBlue");
        public static readonly GenieColor Firebrick = new GenieColor(255, 178, 34, 34, "Firebrick");
        public static readonly GenieColor FloralWhite = new GenieColor(255, 255, 250, 240, "FloralWhite");
        public static readonly GenieColor ForestGreen = new GenieColor(255, 34, 139, 34, "ForestGreen");
        public static readonly GenieColor Fuchsia = new GenieColor(255, 255, 0, 255, "Fuchsia");
        public static readonly GenieColor Gainsboro = new GenieColor(255, 220, 220, 220, "Gainsboro");
        public static readonly GenieColor GhostWhite = new GenieColor(255, 248, 248, 255, "GhostWhite");
        public static readonly GenieColor Gold = new GenieColor(255, 255, 215, 0, "Gold");
        public static readonly GenieColor Goldenrod = new GenieColor(255, 218, 165, 32, "Goldenrod");
        public static readonly GenieColor Gray = new GenieColor(255, 128, 128, 128, "Gray");
        public static readonly GenieColor Green = new GenieColor(255, 0, 128, 0, "Green");
        public static readonly GenieColor GreenYellow = new GenieColor(255, 173, 255, 47, "GreenYellow");
        public static readonly GenieColor Honeydew = new GenieColor(255, 240, 255, 240, "Honeydew");
        public static readonly GenieColor HotPink = new GenieColor(255, 255, 105, 180, "HotPink");
        public static readonly GenieColor IndianRed = new GenieColor(255, 205, 92, 92, "IndianRed");
        public static readonly GenieColor Indigo = new GenieColor(255, 75, 0, 130, "Indigo");
        public static readonly GenieColor Ivory = new GenieColor(255, 255, 255, 240, "Ivory");
        public static readonly GenieColor Khaki = new GenieColor(255, 240, 230, 140, "Khaki");
        public static readonly GenieColor Lavender = new GenieColor(255, 230, 230, 250, "Lavender");
        public static readonly GenieColor LavenderBlush = new GenieColor(255, 255, 240, 245, "LavenderBlush");
        public static readonly GenieColor LawnGreen = new GenieColor(255, 124, 252, 0, "LawnGreen");
        public static readonly GenieColor LemonChiffon = new GenieColor(255, 255, 250, 205, "LemonChiffon");
        public static readonly GenieColor LightBlue = new GenieColor(255, 173, 216, 230, "LightBlue");
        public static readonly GenieColor LightCoral = new GenieColor(255, 240, 128, 128, "LightCoral");
        public static readonly GenieColor LightCyan = new GenieColor(255, 224, 255, 255, "LightCyan");
        public static readonly GenieColor LightGoldenrodYellow = new GenieColor(255, 250, 250, 210, "LightGoldenrodYellow");
        public static readonly GenieColor LightGray = new GenieColor(255, 211, 211, 211, "LightGray");
        public static readonly GenieColor LightGreen = new GenieColor(255, 144, 238, 144, "LightGreen");
        public static readonly GenieColor LightPink = new GenieColor(255, 255, 182, 193, "LightPink");
        public static readonly GenieColor LightSalmon = new GenieColor(255, 255, 160, 122, "LightSalmon");
        public static readonly GenieColor LightSeaGreen = new GenieColor(255, 32, 178, 170, "LightSeaGreen");
        public static readonly GenieColor LightSkyBlue = new GenieColor(255, 135, 206, 250, "LightSkyBlue");
        public static readonly GenieColor LightSlateGray = new GenieColor(255, 119, 136, 153, "LightSlateGray");
        public static readonly GenieColor LightSteelBlue = new GenieColor(255, 176, 196, 222, "LightSteelBlue");
        public static readonly GenieColor LightYellow = new GenieColor(255, 255, 255, 224, "LightYellow");
        public static readonly GenieColor Lime = new GenieColor(255, 0, 255, 0, "Lime");
        public static readonly GenieColor LimeGreen = new GenieColor(255, 50, 205, 50, "LimeGreen");
        public static readonly GenieColor Linen = new GenieColor(255, 250, 240, 230, "Linen");
        public static readonly GenieColor Magenta = new GenieColor(255, 255, 0, 255, "Magenta");
        public static readonly GenieColor Maroon = new GenieColor(255, 128, 0, 0, "Maroon");
        public static readonly GenieColor MediumAquamarine = new GenieColor(255, 102, 205, 170, "MediumAquamarine");
        public static readonly GenieColor MediumBlue = new GenieColor(255, 0, 0, 205, "MediumBlue");
        public static readonly GenieColor MediumOrchid = new GenieColor(255, 186, 85, 211, "MediumOrchid");
        public static readonly GenieColor MediumPurple = new GenieColor(255, 147, 112, 219, "MediumPurple");
        public static readonly GenieColor MediumSeaGreen = new GenieColor(255, 60, 179, 113, "MediumSeaGreen");
        public static readonly GenieColor MediumSlateBlue = new GenieColor(255, 123, 104, 238, "MediumSlateBlue");
        public static readonly GenieColor MediumSpringGreen = new GenieColor(255, 0, 250, 154, "MediumSpringGreen");
        public static readonly GenieColor MediumTurquoise = new GenieColor(255, 72, 209, 204, "MediumTurquoise");
        public static readonly GenieColor MediumVioletRed = new GenieColor(255, 199, 21, 133, "MediumVioletRed");
        public static readonly GenieColor MidnightBlue = new GenieColor(255, 25, 25, 112, "MidnightBlue");
        public static readonly GenieColor MintCream = new GenieColor(255, 245, 255, 250, "MintCream");
        public static readonly GenieColor MistyRose = new GenieColor(255, 255, 228, 225, "MistyRose");
        public static readonly GenieColor Moccasin = new GenieColor(255, 255, 228, 181, "Moccasin");
        public static readonly GenieColor NavajoWhite = new GenieColor(255, 255, 222, 173, "NavajoWhite");
        public static readonly GenieColor Navy = new GenieColor(255, 0, 0, 128, "Navy");
        public static readonly GenieColor OldLace = new GenieColor(255, 253, 245, 230, "OldLace");
        public static readonly GenieColor Olive = new GenieColor(255, 128, 128, 0, "Olive");
        public static readonly GenieColor OliveDrab = new GenieColor(255, 107, 142, 35, "OliveDrab");
        public static readonly GenieColor Orange = new GenieColor(255, 255, 165, 0, "Orange");
        public static readonly GenieColor OrangeRed = new GenieColor(255, 255, 69, 0, "OrangeRed");
        public static readonly GenieColor Orchid = new GenieColor(255, 218, 112, 214, "Orchid");
        public static readonly GenieColor PaleGoldenrod = new GenieColor(255, 238, 232, 170, "PaleGoldenrod");
        public static readonly GenieColor PaleGreen = new GenieColor(255, 152, 251, 152, "PaleGreen");
        public static readonly GenieColor PaleTurquoise = new GenieColor(255, 175, 238, 238, "PaleTurquoise");
        public static readonly GenieColor PaleVioletRed = new GenieColor(255, 219, 112, 147, "PaleVioletRed");
        public static readonly GenieColor PapayaWhip = new GenieColor(255, 255, 239, 213, "PapayaWhip");
        public static readonly GenieColor PeachPuff = new GenieColor(255, 255, 218, 185, "PeachPuff");
        public static readonly GenieColor Peru = new GenieColor(255, 205, 133, 63, "Peru");
        public static readonly GenieColor Pink = new GenieColor(255, 255, 192, 203, "Pink");
        public static readonly GenieColor Plum = new GenieColor(255, 221, 160, 221, "Plum");
        public static readonly GenieColor PowderBlue = new GenieColor(255, 176, 224, 230, "PowderBlue");
        public static readonly GenieColor Purple = new GenieColor(255, 128, 0, 128, "Purple");
        public static readonly GenieColor Red = new GenieColor(255, 255, 0, 0, "Red");
        public static readonly GenieColor RosyBrown = new GenieColor(255, 188, 143, 143, "RosyBrown");
        public static readonly GenieColor RoyalBlue = new GenieColor(255, 65, 105, 225, "RoyalBlue");
        public static readonly GenieColor SaddleBrown = new GenieColor(255, 139, 69, 19, "SaddleBrown");
        public static readonly GenieColor Salmon = new GenieColor(255, 250, 128, 114, "Salmon");
        public static readonly GenieColor SandyBrown = new GenieColor(255, 244, 164, 96, "SandyBrown");
        public static readonly GenieColor SeaGreen = new GenieColor(255, 46, 139, 87, "SeaGreen");
        public static readonly GenieColor SeaShell = new GenieColor(255, 255, 245, 238, "SeaShell");
        public static readonly GenieColor Sienna = new GenieColor(255, 160, 82, 45, "Sienna");
        public static readonly GenieColor Silver = new GenieColor(255, 192, 192, 192, "Silver");
        public static readonly GenieColor SkyBlue = new GenieColor(255, 135, 206, 235, "SkyBlue");
        public static readonly GenieColor SlateBlue = new GenieColor(255, 106, 90, 205, "SlateBlue");
        public static readonly GenieColor SlateGray = new GenieColor(255, 112, 128, 144, "SlateGray");
        public static readonly GenieColor Snow = new GenieColor(255, 255, 250, 250, "Snow");
        public static readonly GenieColor SpringGreen = new GenieColor(255, 0, 255, 127, "SpringGreen");
        public static readonly GenieColor SteelBlue = new GenieColor(255, 70, 130, 180, "SteelBlue");
        public static readonly GenieColor Tan = new GenieColor(255, 210, 180, 140, "Tan");
        public static readonly GenieColor Teal = new GenieColor(255, 0, 128, 128, "Teal");
        public static readonly GenieColor Thistle = new GenieColor(255, 216, 191, 216, "Thistle");
        public static readonly GenieColor Tomato = new GenieColor(255, 255, 99, 71, "Tomato");
        public static readonly GenieColor Turquoise = new GenieColor(255, 64, 224, 208, "Turquoise");
        public static readonly GenieColor Violet = new GenieColor(255, 238, 130, 238, "Violet");
        public static readonly GenieColor Wheat = new GenieColor(255, 245, 222, 179, "Wheat");
        public static readonly GenieColor White = new GenieColor(255, 255, 255, 255, "White");
        public static readonly GenieColor WhiteSmoke = new GenieColor(255, 245, 245, 245, "WhiteSmoke");
        public static readonly GenieColor Yellow = new GenieColor(255, 255, 255, 0, "Yellow");
        public static readonly GenieColor YellowGreen = new GenieColor(255, 154, 205, 50, "YellowGreen");

        private static readonly Dictionary<string, GenieColor> s_namedColors = BuildNamedColorDictionary();

        private static Dictionary<string, GenieColor> BuildNamedColorDictionary()
        {
            return new Dictionary<string, GenieColor>(StringComparer.OrdinalIgnoreCase)
            {
                ["Transparent"] = Transparent,
                ["AliceBlue"] = AliceBlue,
                ["AntiqueWhite"] = AntiqueWhite,
                ["Aqua"] = Aqua,
                ["Aquamarine"] = Aquamarine,
                ["Azure"] = Azure,
                ["Beige"] = Beige,
                ["Bisque"] = Bisque,
                ["Black"] = Black,
                ["BlanchedAlmond"] = BlanchedAlmond,
                ["Blue"] = Blue,
                ["BlueViolet"] = BlueViolet,
                ["Brown"] = Brown,
                ["BurlyWood"] = BurlyWood,
                ["CadetBlue"] = CadetBlue,
                ["Chartreuse"] = Chartreuse,
                ["Chocolate"] = Chocolate,
                ["Coral"] = Coral,
                ["CornflowerBlue"] = CornflowerBlue,
                ["Cornsilk"] = Cornsilk,
                ["Crimson"] = Crimson,
                ["Cyan"] = Cyan,
                ["DarkBlue"] = DarkBlue,
                ["DarkCyan"] = DarkCyan,
                ["DarkGoldenrod"] = DarkGoldenrod,
                ["DarkGray"] = DarkGray,
                ["DarkGreen"] = DarkGreen,
                ["DarkKhaki"] = DarkKhaki,
                ["DarkMagenta"] = DarkMagenta,
                ["DarkOliveGreen"] = DarkOliveGreen,
                ["DarkOrange"] = DarkOrange,
                ["DarkOrchid"] = DarkOrchid,
                ["DarkRed"] = DarkRed,
                ["DarkSalmon"] = DarkSalmon,
                ["DarkSeaGreen"] = DarkSeaGreen,
                ["DarkSlateBlue"] = DarkSlateBlue,
                ["DarkSlateGray"] = DarkSlateGray,
                ["DarkTurquoise"] = DarkTurquoise,
                ["DarkViolet"] = DarkViolet,
                ["DeepPink"] = DeepPink,
                ["DeepSkyBlue"] = DeepSkyBlue,
                ["DimGray"] = DimGray,
                ["DodgerBlue"] = DodgerBlue,
                ["Firebrick"] = Firebrick,
                ["FloralWhite"] = FloralWhite,
                ["ForestGreen"] = ForestGreen,
                ["Fuchsia"] = Fuchsia,
                ["Gainsboro"] = Gainsboro,
                ["GhostWhite"] = GhostWhite,
                ["Gold"] = Gold,
                ["Goldenrod"] = Goldenrod,
                ["Gray"] = Gray,
                ["Green"] = Green,
                ["GreenYellow"] = GreenYellow,
                ["Honeydew"] = Honeydew,
                ["HotPink"] = HotPink,
                ["IndianRed"] = IndianRed,
                ["Indigo"] = Indigo,
                ["Ivory"] = Ivory,
                ["Khaki"] = Khaki,
                ["Lavender"] = Lavender,
                ["LavenderBlush"] = LavenderBlush,
                ["LawnGreen"] = LawnGreen,
                ["LemonChiffon"] = LemonChiffon,
                ["LightBlue"] = LightBlue,
                ["LightCoral"] = LightCoral,
                ["LightCyan"] = LightCyan,
                ["LightGoldenrodYellow"] = LightGoldenrodYellow,
                ["LightGray"] = LightGray,
                ["LightGreen"] = LightGreen,
                ["LightPink"] = LightPink,
                ["LightSalmon"] = LightSalmon,
                ["LightSeaGreen"] = LightSeaGreen,
                ["LightSkyBlue"] = LightSkyBlue,
                ["LightSlateGray"] = LightSlateGray,
                ["LightSteelBlue"] = LightSteelBlue,
                ["LightYellow"] = LightYellow,
                ["Lime"] = Lime,
                ["LimeGreen"] = LimeGreen,
                ["Linen"] = Linen,
                ["Magenta"] = Magenta,
                ["Maroon"] = Maroon,
                ["MediumAquamarine"] = MediumAquamarine,
                ["MediumBlue"] = MediumBlue,
                ["MediumOrchid"] = MediumOrchid,
                ["MediumPurple"] = MediumPurple,
                ["MediumSeaGreen"] = MediumSeaGreen,
                ["MediumSlateBlue"] = MediumSlateBlue,
                ["MediumSpringGreen"] = MediumSpringGreen,
                ["MediumTurquoise"] = MediumTurquoise,
                ["MediumVioletRed"] = MediumVioletRed,
                ["MidnightBlue"] = MidnightBlue,
                ["MintCream"] = MintCream,
                ["MistyRose"] = MistyRose,
                ["Moccasin"] = Moccasin,
                ["NavajoWhite"] = NavajoWhite,
                ["Navy"] = Navy,
                ["OldLace"] = OldLace,
                ["Olive"] = Olive,
                ["OliveDrab"] = OliveDrab,
                ["Orange"] = Orange,
                ["OrangeRed"] = OrangeRed,
                ["Orchid"] = Orchid,
                ["PaleGoldenrod"] = PaleGoldenrod,
                ["PaleGreen"] = PaleGreen,
                ["PaleTurquoise"] = PaleTurquoise,
                ["PaleVioletRed"] = PaleVioletRed,
                ["PapayaWhip"] = PapayaWhip,
                ["PeachPuff"] = PeachPuff,
                ["Peru"] = Peru,
                ["Pink"] = Pink,
                ["Plum"] = Plum,
                ["PowderBlue"] = PowderBlue,
                ["Purple"] = Purple,
                ["Red"] = Red,
                ["RosyBrown"] = RosyBrown,
                ["RoyalBlue"] = RoyalBlue,
                ["SaddleBrown"] = SaddleBrown,
                ["Salmon"] = Salmon,
                ["SandyBrown"] = SandyBrown,
                ["SeaGreen"] = SeaGreen,
                ["SeaShell"] = SeaShell,
                ["Sienna"] = Sienna,
                ["Silver"] = Silver,
                ["SkyBlue"] = SkyBlue,
                ["SlateBlue"] = SlateBlue,
                ["SlateGray"] = SlateGray,
                ["Snow"] = Snow,
                ["SpringGreen"] = SpringGreen,
                ["SteelBlue"] = SteelBlue,
                ["Tan"] = Tan,
                ["Teal"] = Teal,
                ["Thistle"] = Thistle,
                ["Tomato"] = Tomato,
                ["Turquoise"] = Turquoise,
                ["Violet"] = Violet,
                ["Wheat"] = Wheat,
                ["White"] = White,
                ["WhiteSmoke"] = WhiteSmoke,
                ["Yellow"] = Yellow,
                ["YellowGreen"] = YellowGreen,
            };
        }

        /// <summary>
        /// Returns all named colors for enumeration (used by #color command).
        /// </summary>
        public static IReadOnlyDictionary<string, GenieColor> NamedColors => s_namedColors;
    }
}
