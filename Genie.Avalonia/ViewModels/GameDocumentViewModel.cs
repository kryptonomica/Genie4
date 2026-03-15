using System.Collections.ObjectModel;
using Dock.Model.Mvvm.Controls;
using GenieClient.Avalonia.Models;

namespace GenieClient.Avalonia.ViewModels
{
    public class GameDocumentViewModel : Document
    {
        private const int MaxLines = 5000;

        public ObservableCollection<TextLine> Lines { get; } = new();

        public GameDocumentViewModel()
        {
            Title = "Game";
            Id = "main-game";
            CanClose = false;
            CanFloat = false;
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
