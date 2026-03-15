namespace GenieClient.Avalonia.Models
{
    public readonly struct TextSegment
    {
        public string Text { get; }
        public GenieColor FgColor { get; }
        public GenieColor BgColor { get; }
        public bool IsMono { get; }

        public TextSegment(string text, GenieColor fgColor, GenieColor bgColor, bool isMono = false)
        {
            Text = text;
            FgColor = fgColor;
            BgColor = bgColor;
            IsMono = isMono;
        }
    }
}
