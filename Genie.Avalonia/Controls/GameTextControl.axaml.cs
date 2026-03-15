using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Threading;
using GenieClient.Avalonia.Models;

namespace GenieClient.Avalonia.Controls
{
    public partial class GameTextControl : UserControl
    {
        private const int MaxLines = 5000;
        private bool _autoScroll = true;

        public ObservableCollection<TextLine> Lines { get; } = new();

        public GameTextControl()
        {
            InitializeComponent();
            LinesControl.ItemsSource = Lines;
            ScrollViewer.ScrollChanged += OnScrollChanged;
        }

        public void AddLine(TextLine line)
        {
            Lines.Add(line);

            while (Lines.Count > MaxLines)
                Lines.RemoveAt(0);

            if (_autoScroll)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    ScrollViewer.ScrollToEnd();
                    _autoScroll = true;
                }, DispatcherPriority.Loaded);
            }
        }

        public void Clear()
        {
            Lines.Clear();
        }

        public void ScrollPageUp()
        {
            var offset = ScrollViewer.Offset;
            ScrollViewer.Offset = offset.WithY(Math.Max(0, offset.Y - ScrollViewer.Viewport.Height));
            _autoScroll = false;
        }

        public void ScrollPageDown()
        {
            var offset = ScrollViewer.Offset;
            var maxY = ScrollViewer.Extent.Height - ScrollViewer.Viewport.Height;
            ScrollViewer.Offset = offset.WithY(Math.Min(maxY, offset.Y + ScrollViewer.Viewport.Height));

            if (ScrollViewer.Offset.Y >= maxY - 1)
                _autoScroll = true;
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var maxY = ScrollViewer.Extent.Height - ScrollViewer.Viewport.Height;
            if (maxY <= 0)
            {
                _autoScroll = true;
                return;
            }
            _autoScroll = ScrollViewer.Offset.Y >= maxY - 20;
        }
    }
}
