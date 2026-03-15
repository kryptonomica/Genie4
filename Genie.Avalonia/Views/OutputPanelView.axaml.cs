using System.Collections.Specialized;
using Avalonia.Controls;
using GenieClient.Avalonia.ViewModels;

namespace GenieClient.Avalonia.Views
{
    public partial class OutputPanelView : UserControl
    {
        private INotifyCollectionChanged _subscribedCollection;

        public OutputPanelView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, System.EventArgs e)
        {
            // Unsubscribe from previous
            if (_subscribedCollection != null)
            {
                _subscribedCollection.CollectionChanged -= OnLinesChanged;
                _subscribedCollection = null;
            }

            TextControl.Clear();

            if (DataContext is OutputPanelViewModel panel)
            {
                // Load existing lines
                foreach (var line in panel.Lines)
                    TextControl.AddLine(line);

                panel.Lines.CollectionChanged += OnLinesChanged;
                _subscribedCollection = panel.Lines;
            }
            else if (DataContext is GameDocumentViewModel doc)
            {
                foreach (var line in doc.Lines)
                    TextControl.AddLine(line);

                doc.Lines.CollectionChanged += OnLinesChanged;
                _subscribedCollection = doc.Lines;
            }
        }

        private void OnLinesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Models.TextLine line in e.NewItems)
                        TextControl.AddLine(line);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    TextControl.Clear();
                    break;
                case NotifyCollectionChangedAction.Remove:
                    // Lines removed from beginning (buffer trim) — rebuild is expensive,
                    // let the GameTextControl's own Lines collection handle it via AddLine cap
                    break;
            }
        }
    }
}
