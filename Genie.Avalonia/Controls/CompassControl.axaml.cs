using System.Collections.Generic;
using Avalonia.Controls;

namespace GenieClient.Avalonia.Controls
{
    public partial class CompassControl : UserControl
    {
        private Dictionary<string, TextBlock> _directionMap;

        public CompassControl()
        {
            InitializeComponent();

            _directionMap = new Dictionary<string, TextBlock>
            {
                { "north", DirN },
                { "northeast", DirNE },
                { "east", DirE },
                { "southeast", DirSE },
                { "south", DirS },
                { "southwest", DirSW },
                { "west", DirW },
                { "northwest", DirNW },
                { "up", DirUp },
                { "down", DirDn },
                { "out", DirOut }
            };
        }

        public void SetDirection(string direction, bool active)
        {
            if (_directionMap.TryGetValue(direction, out var textBlock))
            {
                textBlock.Opacity = active ? 1.0 : 0.15;
            }
        }

        public void ClearAll()
        {
            foreach (var tb in _directionMap.Values)
                tb.Opacity = 0.15;
        }
    }
}
