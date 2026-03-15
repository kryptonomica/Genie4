using System;
using System.Collections.Generic;

namespace GenieClient.Avalonia.Models
{
    public class TextLine
    {
        public IReadOnlyList<TextSegment> Segments { get; }
        public DateTime Timestamp { get; }

        public TextLine(IReadOnlyList<TextSegment> segments)
        {
            Segments = segments;
            Timestamp = DateTime.Now;
        }

        public TextLine(IReadOnlyList<TextSegment> segments, DateTime timestamp)
        {
            Segments = segments;
            Timestamp = timestamp;
        }
    }
}
