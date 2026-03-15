using System.Collections.ObjectModel;
using Dock.Model.Mvvm.Controls;
using GenieClient.Avalonia.Models;

namespace GenieClient.Avalonia.ViewModels
{
    public class OutputPanelViewModel : Tool
    {
        private const int MaxLines = 5000;

        public string WindowId { get; }
        public string IfClosed { get; set; }
        public bool IsSystemWindow { get; set; }
        public ObservableCollection<TextLine> Lines { get; } = new();

        public OutputPanelViewModel(string windowId, string title, string ifClosed = null)
        {
            WindowId = windowId;
            Title = title;
            Id = "panel-" + windowId;
            IfClosed = ifClosed;
            CanClose = true;
            CanFloat = true;
            CanPin = false;
        }

        public void AddLine(TextLine line)
        {
            Lines.Add(line);
            while (Lines.Count > MaxLines)
                Lines.RemoveAt(0);
        }

        public void Clear()
        {
            Lines.Clear();
        }
    }
}
