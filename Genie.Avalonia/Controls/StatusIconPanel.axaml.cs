using Avalonia.Controls;
using Avalonia.Media;

namespace GenieClient.Avalonia.Controls
{
    public partial class StatusIconPanel : UserControl
    {
        public StatusIconPanel()
        {
            InitializeComponent();
        }

        public void SetPosture(string posture)
        {
            switch (posture)
            {
                case "standing":
                    PostureText.Text = "ST";
                    PostureText.Foreground = new SolidColorBrush(Color.Parse("LimeGreen"));
                    break;
                case "kneeling":
                    PostureText.Text = "KN";
                    PostureText.Foreground = new SolidColorBrush(Color.Parse("Yellow"));
                    break;
                case "sitting":
                    PostureText.Text = "SI";
                    PostureText.Foreground = new SolidColorBrush(Color.Parse("Orange"));
                    break;
                case "prone":
                    PostureText.Text = "PR";
                    PostureText.Foreground = new SolidColorBrush(Color.Parse("OrangeRed"));
                    break;
                case "dead":
                    PostureText.Text = "X";
                    PostureText.Foreground = new SolidColorBrush(Color.Parse("Red"));
                    break;
            }
        }

        public void UpdateStunned(bool active) => StunnedBorder.IsVisible = active;
        public void UpdateBleeding(bool active) => BleedingBorder.IsVisible = active;
        public void UpdateInvisible(bool active) => InvisibleBorder.IsVisible = active;
        public void UpdateHidden(bool active) => HiddenBorder.IsVisible = active;
        public void UpdateJoined(bool active) => JoinedBorder.IsVisible = active;
        public void UpdateWebbed(bool active) => WebbedBorder.IsVisible = active;
    }
}
