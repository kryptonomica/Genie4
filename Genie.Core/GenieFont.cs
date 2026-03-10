using System;

namespace GenieClient
{
    public enum GenieFontStyle
    {
        Regular = 0,
        Bold = 1,
        Italic = 2,
        BoldItalic = 3
    }

    public readonly struct GenieFont : IEquatable<GenieFont>
    {
        public readonly string FamilyName;
        public readonly float Size;
        public readonly GenieFontStyle Style;

        public GenieFont(string familyName, float size, GenieFontStyle style = GenieFontStyle.Regular)
        {
            FamilyName = familyName ?? "Courier New";
            Size = size;
            Style = style;
        }

        public string Name => FamilyName;

        public bool Equals(GenieFont other)
        {
            return string.Equals(FamilyName, other.FamilyName, StringComparison.Ordinal)
                && Size == other.Size
                && Style == other.Style;
        }

        public override bool Equals(object obj)
        {
            return obj is GenieFont f && Equals(f);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FamilyName, Size, Style);
        }

        public static bool operator ==(GenieFont left, GenieFont right) => left.Equals(right);
        public static bool operator !=(GenieFont left, GenieFont right) => !left.Equals(right);

        public override string ToString()
        {
            return $"GenieFont [{FamilyName}, {Size}pt, {Style}]";
        }
    }
}
