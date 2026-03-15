using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GenieClient.Avalonia.Views
{
    public partial class ConnectDialog : Window
    {
        public string AccountName => AccountBox.Text ?? string.Empty;
        public string Password => PasswordBox.Text ?? string.Empty;
        public string Character => CharacterBox.Text ?? string.Empty;
        public string Game => (GameBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "DR";
        public bool Confirmed { get; private set; }

        public string InitialAccount { set => AccountBox.Text = value; }
        public string InitialCharacter { set => CharacterBox.Text = value; }
        public string InitialGame
        {
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                for (int i = 0; i < GameBox.Items.Count; i++)
                {
                    if (GameBox.Items[i] is ComboBoxItem item &&
                        string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                    {
                        GameBox.SelectedIndex = i;
                        return;
                    }
                }
            }
        }

        public ConnectDialog()
        {
            InitializeComponent();
            ConnectButton.Click += OnConnect;
            CancelButton.Click += OnCancel;
        }

        private void OnConnect(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            Close();
        }
    }
}
