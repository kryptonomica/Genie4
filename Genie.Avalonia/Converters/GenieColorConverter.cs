using Avalonia.Media;

namespace GenieClient.Avalonia.Converters
{
    public static class GenieColorConverter
    {
        public static Color ToAvaloniaColor(this GenieColor c)
            => Color.FromArgb(c.A, c.R, c.G, c.B);

        public static ISolidColorBrush ToAvaloniaBrush(this GenieColor c)
            => new SolidColorBrush(c.ToAvaloniaColor());

        public static GenieColor ToGenieColor(this Color c)
            => GenieColor.FromArgb(c.A, c.R, c.G, c.B);
    }
}
