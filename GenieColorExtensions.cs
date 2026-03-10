using System.Drawing;

namespace GenieClient
{
    public static class GenieColorExtensions
    {
        public static Color ToDrawingColor(this GenieColor c)
        {
            if (c.IsEmpty) return Color.Empty;
            if (c.IsNamedColor)
            {
                var dc = Color.FromName(c.Name);
                if (dc.IsKnownColor) return dc;
            }
            return Color.FromArgb(c.A, c.R, c.G, c.B);
        }

        public static GenieColor ToGenieColor(this Color c)
        {
            if (c.IsEmpty) return default;
            if (c.IsNamedColor) return GenieColor.FromName(c.Name);
            return GenieColor.FromArgb(c.A, c.R, c.G, c.B);
        }
    }
}
