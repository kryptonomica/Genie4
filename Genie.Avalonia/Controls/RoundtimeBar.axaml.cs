using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace GenieClient.Avalonia.Controls
{
    public partial class RoundtimeBar : UserControl
    {
        public static readonly StyledProperty<IBrush> FillBrushProperty =
            AvaloniaProperty.Register<RoundtimeBar, IBrush>(nameof(FillBrush));

        public static readonly StyledProperty<IBrush> TrackBrushProperty =
            AvaloniaProperty.Register<RoundtimeBar, IBrush>(nameof(TrackBrush));

        private DispatcherTimer _timer;
        private DateTime _endTime;
        private double _totalSeconds;

        public int CurrentRT => Math.Max(0, (int)Math.Ceiling((_endTime - DateTime.Now).TotalSeconds));

        public IBrush FillBrush
        {
            get => GetValue(FillBrushProperty);
            set => SetValue(FillBrushProperty, value);
        }

        public IBrush TrackBrush
        {
            get => GetValue(TrackBrushProperty);
            set => SetValue(TrackBrushProperty, value);
        }

        public RoundtimeBar()
        {
            InitializeComponent();

            // Clear any theme transitions so the bar updates instantly
            Bar.Transitions = new Transitions();
            Bar.Maximum = 1000;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _timer.Tick += (_, _) => UpdateBar();
        }

        public void SetRT(int seconds)
        {
            if (seconds <= 0)
            {
                _timer.Stop();
                _totalSeconds = 0;
                Bar.Value = 0;
                Label.Text = "";
                return;
            }

            _totalSeconds = seconds;
            _endTime = DateTime.Now.AddSeconds(seconds);
            Bar.Value = 1000;
            Label.Text = seconds.ToString();
            _timer.Start();
        }

        private void UpdateBar()
        {
            double remaining = (_endTime - DateTime.Now).TotalSeconds;

            if (remaining <= 0)
            {
                _timer.Stop();
                Bar.Value = 0;
                Label.Text = "";
                return;
            }

            int displaySeconds = (int)Math.Ceiling(remaining);
            Label.Text = displaySeconds.ToString();

            // Bar drains smoothly within each displayed second
            // When showing "3", bar goes from 3/total to 2/total over that second
            double secondFraction = remaining - (displaySeconds - 1); // 0.0 to 1.0 within current second
            double barValue = (displaySeconds - 1 + secondFraction) / _totalSeconds;
            Bar.Value = barValue * 1000;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == FillBrushProperty)
            {
                if (change.NewValue is IBrush brush)
                    Bar.Foreground = brush;
            }
            else if (change.Property == TrackBrushProperty)
            {
                if (change.NewValue is IBrush brush)
                    Bar.Background = brush;
            }
        }
    }
}
