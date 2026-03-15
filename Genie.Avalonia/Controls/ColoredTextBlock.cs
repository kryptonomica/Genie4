using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Controls;
using Avalonia.Media;
using GenieClient.Avalonia.Converters;
using GenieClient.Avalonia.Models;

namespace GenieClient.Avalonia.Controls
{
    public class ColoredTextBlock : TextBlock
    {
        public static readonly StyledProperty<IReadOnlyList<TextSegment>> SegmentsProperty =
            AvaloniaProperty.Register<ColoredTextBlock, IReadOnlyList<TextSegment>>(nameof(Segments));

        public IReadOnlyList<TextSegment> Segments
        {
            get => GetValue(SegmentsProperty);
            set => SetValue(SegmentsProperty, value);
        }

        private static readonly FontFamily MonoFont = new FontFamily("Courier New, Consolas, monospace");

        static ColoredTextBlock()
        {
            SegmentsProperty.Changed.AddClassHandler<ColoredTextBlock>((tb, _) => tb.RebuildInlines());
        }

        private void RebuildInlines()
        {
            Inlines.Clear();
            var segments = Segments;
            if (segments == null) return;

            foreach (var seg in segments)
            {
                var run = new Run(seg.Text);

                if (!seg.FgColor.IsEmpty)
                    run.Foreground = seg.FgColor.ToAvaloniaBrush();

                if (!seg.BgColor.IsEmpty && seg.BgColor != GenieColor.Transparent)
                    run.Background = seg.BgColor.ToAvaloniaBrush();

                if (seg.IsMono)
                    run.FontFamily = MonoFont;

                Inlines.Add(run);
            }
        }
    }
}
