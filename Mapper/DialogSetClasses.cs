using System;
using System.Windows.Forms;
using GenieClient.Genie;

namespace GenieClient
{
    public partial class DialogSetClasses
    {
        public DialogSetClasses()
        {
            InitializeComponent();
        }

        private void OK_Button_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Cancel_Button_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        public string ClassText
        {
            get
            {
                return _TextboxClasses.Text;
            }

            set
            {
                _TextboxClasses.Text = value;
            }
        }

        public void Recolor(Globals.Presets.Preset window, Globals.Presets.Preset textbox, Globals.Presets.Preset button)
        {
            BackColor = window.BgColor.ToDrawingColor();
            ForeColor = window.FgColor.ToDrawingColor();
            _TextboxClasses.ForeColor = textbox.FgColor.ToDrawingColor();
            _TextboxClasses.BackColor = textbox.BgColor.ToDrawingColor();
            OK_Button.ForeColor = button.FgColor.ToDrawingColor();
            OK_Button.BackColor = button.BgColor.ToDrawingColor();
            Cancel_Button.ForeColor = button.FgColor.ToDrawingColor();
            Cancel_Button.BackColor = button.BgColor.ToDrawingColor();
        }
    }
}