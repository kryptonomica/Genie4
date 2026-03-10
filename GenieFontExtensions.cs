using System.Drawing;

namespace GenieClient
{
    public static class GenieFontExtensions
    {
        public static Font ToDrawingFont(this GenieFont f)
        {
            return new Font(f.FamilyName, f.Size, (FontStyle)f.Style);
        }

        public static GenieFont ToGenieFont(this Font f)
        {
            return new GenieFont(f.Name, f.Size, (GenieFontStyle)f.Style);
        }
    }
}
