using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;

namespace GenieClient.Avalonia.Controls
{
    public partial class CommandInputBox : UserControl
    {
        public event Action<string> SendText;
        public event Action ScrollPageUp;
        public event Action ScrollPageDown;

        private readonly List<string> _history = new();
        private int _historyPos = -1;
        private const int HistorySize = 20;
        private const int HistoryMinLength = 3;
        private bool _keepInput;

        public bool KeepInput
        {
            get => _keepInput;
            set => _keepInput = value;
        }

        public CommandInputBox()
        {
            InitializeComponent();
            InputBox.KeyDown += OnKeyDown;
        }

        public void FocusInput()
        {
            InputBox.Focus();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    HandleEnter();
                    e.Handled = true;
                    break;

                case Key.Up:
                    HistoryBack();
                    e.Handled = true;
                    break;

                case Key.Down:
                    HistoryForward();
                    e.Handled = true;
                    break;

                case Key.PageUp:
                    ScrollPageUp?.Invoke();
                    e.Handled = true;
                    break;

                case Key.PageDown:
                    ScrollPageDown?.Invoke();
                    e.Handled = true;
                    break;
            }
        }

        private void HandleEnter()
        {
            var text = InputBox.Text ?? string.Empty;
            SendText?.Invoke(text);
            AddToHistory(text);

            if (_keepInput)
                InputBox.SelectAll();
            else
                InputBox.Text = string.Empty;

            _historyPos = -1;
        }

        private void AddToHistory(string text)
        {
            if (text.Length < HistoryMinLength)
                return;

            // Remove duplicate if exists at top
            if (_history.Count > 0 && _history[0] == text)
                return;

            _history.Insert(0, text);
            while (_history.Count > HistorySize)
                _history.RemoveAt(_history.Count - 1);
        }

        private void HistoryBack()
        {
            if (_history.Count == 0) return;
            if (_historyPos < _history.Count - 1)
            {
                _historyPos++;
                InputBox.Text = _history[_historyPos];
                InputBox.CaretIndex = InputBox.Text.Length;
            }
        }

        private void HistoryForward()
        {
            if (_historyPos > 0)
            {
                _historyPos--;
                InputBox.Text = _history[_historyPos];
                InputBox.CaretIndex = InputBox.Text.Length;
            }
            else if (_historyPos == 0)
            {
                _historyPos = -1;
                InputBox.Text = string.Empty;
            }
        }
    }
}
