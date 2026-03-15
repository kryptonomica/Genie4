using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace GenieClient.Avalonia.Controls
{
    public partial class VitalBar : UserControl
    {
        public static readonly StyledProperty<int> ValueProperty =
            AvaloniaProperty.Register<VitalBar, int>(nameof(Value), 100);

        public static readonly StyledProperty<string> BarTextProperty =
            AvaloniaProperty.Register<VitalBar, string>(nameof(BarText), "");

        public static readonly StyledProperty<IBrush> FillBrushProperty =
            AvaloniaProperty.Register<VitalBar, IBrush>(nameof(FillBrush));

        public static readonly StyledProperty<IBrush> TrackBrushProperty =
            AvaloniaProperty.Register<VitalBar, IBrush>(nameof(TrackBrush));

        public int Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public string BarText
        {
            get => GetValue(BarTextProperty);
            set => SetValue(BarTextProperty, value);
        }

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

        public VitalBar()
        {
            InitializeComponent();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ValueProperty)
            {
                var val = (int)change.NewValue;
                if (val < 0) val = 0;
                if (val > 100) val = 100;
                Bar.Value = val;
            }
            else if (change.Property == BarTextProperty)
            {
                Label.Text = (string)change.NewValue ?? "";
            }
            else if (change.Property == FillBrushProperty)
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
