using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using GenieClient.Forms;
using GenieClient.Genie;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace GenieClient
{
    // Window management: CreateOutputForm, AddText/AddImage, window find/show/hide, EndUpdate.
    public partial class FormMain
    {
        private void EventStreamWindow(object sID, object sTitle, object sIfClosed)
        {
            if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(sID, "main", false)))
                return;
            var fo = FindSkinFormByIDOrName(Conversions.ToString(sID), Conversions.ToString(sTitle));
            if (Information.IsNothing(fo))
            {
                SafeCreateOutputForm(Conversions.ToString(sID), Conversions.ToString(sTitle), Conversions.ToString(sIfClosed), 300, 200, 10, 10, false, null, "", true);
                string argsText = Conversions.ToString("Created new window: " + sID + System.Environment.NewLine);
                Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                AddText(argsText, oTargetWindow: argoTargetWindow);
            }
            else if (Information.IsNothing(fo.IfClosed) & !Information.IsNothing(sIfClosed))
            {
                fo.IfClosed = Conversions.ToString(sIfClosed);
                string argsText1 = Conversions.ToString("Altered window: " + sID + System.Environment.NewLine);
                Genie.Game.WindowTarget argoTargetWindow1 = Genie.Game.WindowTarget.Main;
                AddText(argsText1, oTargetWindow: argoTargetWindow1);
            }
        }

        private void HideForm(Form oForm)
        {
            if (oForm is null)
            {
                throw new ArgumentNullException("form", "Unable to hide null form.");
            }
            else
            {
                if (oForm.Visible)
                {
                    oForm.Hide();
                }
            }
        }

        private void ShowForm(Form oForm)
        {
            if (oForm is null)
            {
                throw new ArgumentNullException("form", "Unable to show null form.");
            }
            else
            {
                if (!oForm.Visible)
                {
                    oForm.Show();
                }

                oForm.BringToFront();
            }
        }

        public void UpdateWindowMenuList()
        {
            WindowToolStripMenuItem.DropDownItems.Clear();
            var ti = new ToolStripMenuItem();
            ti.BackColor = m_oGlobals.PresetList["ui.menu"].BgColor.ToDrawingColor();
            ti.ForeColor = m_oGlobals.PresetList["ui.menu"].FgColor.ToDrawingColor();
            ti.Name = "ToolStripMenuItemWindowMain";
            ti.Text = "&1. " + m_oOutputMain.Text;
            ti.Tag = m_oOutputMain;
            ti.Click += WindowMenuItem_Click;
            if (m_oOutputMain.Visible)
            {
                ti.Checked = true;
            }
            m_oOutputMain.WindowMenuItem = ti;
            WindowToolStripMenuItem.DropDownItems.Add(ti);
            int I = 2;
            foreach (FormSkin fo in m_oFormList)
            {
                ti = new ToolStripMenuItem();
                ti.BackColor = m_oGlobals.PresetList["ui.menu"].BgColor.ToDrawingColor();
                ti.ForeColor = m_oGlobals.PresetList["ui.menu"].FgColor.ToDrawingColor();
                ti.Name = "ToolStripMenuItemWindow" + fo.Text;
                ti.Text = "&" + I.ToString() + ". " + fo.Text;
                ti.Tag = fo;
                ti.Click += WindowMenuItem_Click;
                if (fo.Visible)
                {
                    ti.Checked = true;
                }
                fo.WindowMenuItem = ti;
                WindowToolStripMenuItem.DropDownItems.Add(ti);
                I += 1;
            }
        }

        private void WindowMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem)
            {
                ToolStripMenuItem mi = (ToolStripMenuItem)sender;
                if (!Information.IsNothing(mi.Tag))
                {
                    if (mi.Tag is FormSkin)
                    {
                        FormSkin fo = (FormSkin)mi.Tag;
                        if (fo.Visible)
                        {
                            HideForm(fo);
                        }
                        else
                        {
                            ShowForm(fo);
                        }
                        UpdateWindowMenuList();
                    }
                }
            }
        }

        private void ShowOutputForms()
        {
            var myEnumerator = m_oFormList.GetEnumerator();
            while (myEnumerator.MoveNext())
            {
                FormSkin oForm = (FormSkin)myEnumerator.Current;
                if (!Information.IsNothing(oForm.Tag))
                {
                    if (Conversions.ToBoolean(oForm.Tag) == true)
                    {
                        ShowForm(oForm);
                    }
                }
            }
            UpdateWindowMenuList();
        }

        private void HideTagOutputForms()
        {
            var myEnumerator = m_oFormList.GetEnumerator();
            while (myEnumerator.MoveNext())
            {
                FormSkin oForm = (FormSkin)myEnumerator.Current;
                oForm.Tag = false;
            }
        }

        private void HideOutputForms()
        {
            var myEnumerator = m_oFormList.GetEnumerator();
            while (myEnumerator.MoveNext())
            {
                FormSkin oForm = (FormSkin)myEnumerator.Current;
                if (!Information.IsNothing(oForm.Tag))
                {
                    if (Conversions.ToBoolean(oForm.Tag) == false)
                    {
                        oForm.Visible = false;
                    }
                }
            }
        }

        // Private Sub ShowOutputBoxes()
        // Dim myEnumerator As IEnumerator = m_oFormList.GetEnumerator
        // While myEnumerator.MoveNext
        // Dim oForm As FormSkin = CType(myEnumerator.Current, FormSkin)
        // If Not IsNothing(oForm.Tag) Then
        // If oForm.Visible = True Then
        // oForm.ShowOutput()
        // End If
        // End If
        // End While
        // End Sub

        public delegate FormSkin CreateOutputFormDelegate(string sID, string sName, string sIfClosed, int iWidth, int iHeight, int iTop, int iLeft, bool bIsVisible, Font oFont, string sColorName, bool UpdateFormList);

        public FormSkin SafeCreateOutputForm(string sID, string sName, string sIfClosed, int iWidth, int iHeight, int iTop, int iLeft, bool bIsVisible, Font oFont = null, string sColorName = "", bool UpdateFormList = false)
        {
            if (InvokeRequired == true)
            {
                var parameters = new object[] { sID, sName, sIfClosed, iWidth, iHeight, iTop, iLeft, bIsVisible, oFont, sColorName, UpdateFormList };
                return (FormSkin)Invoke(new CreateOutputFormDelegate(CreateOutputForm), parameters);
            }
            else
            {
                return CreateOutputForm(sID, sName, sIfClosed, iWidth, iHeight, iTop, iLeft, bIsVisible, oFont, sColorName, UpdateFormList);
            }
        }

        public FormSkin CreateOutputForm(string sID, string sName, string sIfClosed, int iWidth, int iHeight, int iTop, int iLeft, bool bIsVisible, Font oFont = null, string sColorName = "", bool UpdateFormList = false)
        {
            FormSkin oForm = null;
            var oEnumerator = m_oFormList.GetEnumerator();
            while (oEnumerator.MoveNext())
            {
                if ((((FormSkin)oEnumerator.Current).ID ?? "") == (sID ?? ""))
                {
                    oForm = (FormSkin)oEnumerator.Current;
                }
            }

            if (Information.IsNothing(oForm))
            {
                var argoGlobal = m_oGlobals;
                oForm = new FormSkin(sID, sName, ref _m_oGlobals);
                oForm.EventLinkClicked += FormSkin_LinkClicked;
                oForm.MdiParent = this;
                m_oFormList.Add(oForm);
            }

            oForm.Name = "FormSkin" + sID;
            oForm.Text = sName;
            oForm.Title = sName.ToLower() == "percwindow" ? "Active Spells" : sName;
            oForm.ID = sID;
            oForm.IfClosed = sIfClosed;
            if (!Information.IsNothing(oFont))
            {
                oForm.TextFont = oFont;
            }

            oForm.RichTextBoxOutput.MonoFont = m_oGlobals.Config.MonoFont.ToDrawingFont();
            oForm.Width = iWidth;
            oForm.Height = iHeight;
            oForm.Top = iTop;
            oForm.Left = iLeft;
            oForm.Tag = bIsVisible;
            if (sColorName.Length > 0)
            {
                if (sColorName.Contains(",") == true && sColorName.EndsWith(",") == false)
                {
                    string sColor = sColorName.Substring(0, sColorName.IndexOf(",")).Trim();
                    string sBgColor = sColorName.Substring(sColorName.IndexOf(",") + 1).Trim();
                    oForm.RichTextBoxOutput.ForeColor = Genie.ColorCode.StringToColor(sColor).ToDrawingColor();
                    oForm.RichTextBoxOutput.BackColor = Genie.ColorCode.StringToColor(sBgColor).ToDrawingColor();
                }
                else
                {
                    oForm.RichTextBoxOutput.ForeColor = Genie.ColorCode.StringToColor(sColorName).ToDrawingColor();
                }
            }

            switch (sID)
            {
                case "inv":
                case "inventory":
                    {
                        m_oOutputInv = oForm;
                        oForm.UserForm = false;
                        break;
                    }

                case "familiar":
                    {
                        m_oOutputFamiliar = oForm;
                        oForm.UserForm = false;
                        break;
                    }

                case "thoughts":
                    {
                        m_oOutputThoughts = oForm;
                        oForm.UserForm = false;
                        break;
                    }

                case "logons":
                case "arrivals":
                    {
                        m_oOutputLogons = oForm;
                        oForm.UserForm = false;
                        break;
                    }

                case "deaths":
                case "death":
                    {
                        m_oOutputDeath = oForm;
                        oForm.UserForm = false;
                        break;
                    }

                case "room":
                    {
                        m_oOutputRoom = oForm;
                        oForm.UserForm = false;
                        break;
                    }

                case "log":
                    {
                        m_oOutputLog = oForm;
                        oForm.UserForm = false;
                        break;
                    }

                case "debug":
                    {
                        m_oOutputDebug = oForm;
                        oForm.UserForm = false;
                        break;
                    }
                case "percWindow":
                case "percwindow":
                    {
                        m_oOutputActiveSpells = oForm;
                        oForm.UserForm = false;
                        break;
                    }

                case "combat":
                    {
                        m_oOutputCombat = oForm;
                        oForm.UserForm = false;
                        break;
                    }

                case "portrait":
                    {
                        m_oOutputPortrait = oForm;
                        oForm.UserForm = false;
                        break;
                    }
            }

            if (UpdateFormList)
                UpdateWindowMenuList();
            return oForm;
        }

        private bool OutputFormNameExists(string sID)
        {
            var oEnumerator = m_oFormList.GetEnumerator();
            while (oEnumerator.MoveNext())
            {
                if ((((FormSkin)oEnumerator.Current).ID ?? "") == (sID ?? ""))
                {
                    return true;
                }
            }

            return false;
        }

        // Remove Disposed Objects from FormList
        public void RemoveDisposedForms()
        {
            FormSkin oForm;
            var oEnumerator = m_oFormList.GetEnumerator();
            while (oEnumerator.MoveNext())
            {
                oForm = (FormSkin)oEnumerator.Current;
                if (oForm.IsDisposed == true)
                {
                    m_oFormList.Remove(oForm);
                    RemoveDisposedForms();
                    return;
                }
            }
        }

        private void ClassCommand_EchoText(string sText, string sWindow)
        {
            try
            {
                FormSkin oFormSkin = null;
                if (sWindow.Length > 0)
                {
                    if ((sWindow.ToLower() ?? "") != "game" & (sWindow.ToLower() ?? "") != "main")
                    {
                        var oEnumerator = m_oFormList.GetEnumerator();
                        while (oEnumerator.MoveNext())
                        {
                            if ((((FormSkin)oEnumerator.Current).ID ?? "") == (sWindow.ToLower() ?? ""))
                            {
                                oFormSkin = (FormSkin)oEnumerator.Current;
                                break;
                            }
                        }
                    }
                }

                bool bMono = false;
                if (sText.ToLower().StartsWith("mono "))
                {
                    sText = sText.Substring(5);
                    bMono = true;
                }

                if (!Information.IsNothing(oFormSkin))
                {
                    var argoColor = Color.WhiteSmoke;
                    var argoBgColor = Color.Transparent;
                    AddText(sText, argoColor, argoBgColor, oFormSkin, true, bMono);
                }
                else if (sWindow.Length == 0)
                {
                    var argoColor1 = Color.WhiteSmoke;
                    var argoBgColor1 = Color.Transparent;
                    string argsTargetWindow = "";
                    AddText(sText, argoColor1, argoBgColor1, Genie.Game.WindowTarget.Main, argsTargetWindow, true, bMono);
                }
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("EchoText", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        private void ClassCommand_LinkText(string sText, string sLink, string sWindow)
        {
            try
            {
                FormSkin oFormSkin = null;
                if (sWindow.Length > 0)
                {
                    if ((sWindow.ToLower() ?? "") != "game" & (sWindow.ToLower() ?? "") != "main")
                    {
                        var oEnumerator = m_oFormList.GetEnumerator();
                        while (oEnumerator.MoveNext())
                        {
                            if ((((FormSkin)oEnumerator.Current).ID ?? "") == (sWindow.ToLower() ?? ""))
                            {
                                oFormSkin = (FormSkin)oEnumerator.Current;
                                break;
                            }
                        }
                    }
                }

                if (Information.IsNothing(oFormSkin))
                    oFormSkin = m_oOutputMain;
                SafeLinkText(sText, sLink, oFormSkin);
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("EchoText", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        public delegate void LinkTextDelegate(string sText, string sLink, FormSkin oTargetWindow);

        private void SafeLinkText(string sText, string sLink, FormSkin oTargetWindow)
        {
            if (InvokeRequired == true)
            {
                var parameters = new object[] { sText, sLink, oTargetWindow };
                Invoke(new LinkTextDelegate(LinkText), parameters);
            }
            else
            {
                LinkText(sText, sLink, oTargetWindow);
            }
        }

        private void LinkText(string sText, string sLink, FormSkin oTargetWindow)
        {
            oTargetWindow.RichTextBoxOutput.InsertLink(sText, sLink);
        }

        private void ClassCommand_EchoColorText(string sText, GenieColor oColor, GenieColor oBgColor, string sWindow)
        {
            try
            {
                FormSkin oFormSkin = null;
                if (sWindow.Length > 0)
                {
                    if ((sWindow.ToLower() ?? "") != "game" & (sWindow.ToLower() ?? "") != "main")
                    {
                        var oEnumerator = m_oFormList.GetEnumerator();
                        while (oEnumerator.MoveNext())
                        {
                            if ((((FormSkin)oEnumerator.Current).ID ?? "") == (sWindow.ToLower() ?? ""))
                            {
                                oFormSkin = (FormSkin)oEnumerator.Current;
                                break;
                            }
                        }
                    }
                }

                bool bMono = false;
                if (sText.ToLower().StartsWith("mono "))
                {
                    sText = sText.Substring(5);
                    bMono = true;
                }

                if (!Information.IsNothing(oFormSkin))
                {
                    AddText(sText, oColor.ToDrawingColor(), oBgColor.ToDrawingColor(), oFormSkin, true, bMono);
                }
                else if (sWindow.Length == 0)
                {
                    string argsTargetWindow = "";
                    AddText(sText, oColor.ToDrawingColor(), oBgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main, argsTargetWindow, true, bMono);
                }
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("EchoColorText", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        private void AppendText(string Text)
        {
            Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
            AddText(Text, oTargetWindow: argoTargetWindow);
        }

        private void AddText(string sText, [Optional, DefaultParameterValue(Genie.Game.WindowTarget.Main)] Genie.Game.WindowTarget oTargetWindow, bool bNoCache = true, bool bMono = false, bool bPrompt = false, bool bInput = false)
        {
            var argoColor = Color.WhiteSmoke;
            var argoBgColor = Color.Transparent;
            string argsTargetWindow = Conversions.ToString(bNoCache);
            AddText(sText, argoColor, argoBgColor, oTargetWindow, argsTargetWindow, bMono, bPrompt, bInput);
        }

        private void AddText(string sText, Color oColor, Color oBgColor, FormSkin oTargetWindow, bool bNoCache = true, bool bMono = false, bool bPrompt = false, bool bInput = false)
        {
            // bPrompt = false;

            if (IsDisposed)
            {
                return;
            }

            if (Information.IsNothing(oTargetWindow))
            {
                oTargetWindow = m_oOutputMain;
            }

            if (oTargetWindow.Equals(m_oOutputMain))
            {
                if (bPrompt == true)
                {
                    if (m_oGame.LastRowWasPrompt)
                    {
                        return;
                    }

                    m_oGame.LastRowWasPrompt = true;
                }
                else if (sText.Trim().Length > 0)
                {
                    if (m_oGame.LastRowWasPrompt == true)
                    {
                        if (!bInput)
                        {
                            if (sText.StartsWith(Constants.vbNewLine) == false && m_oGlobals.Config.PromptBreak)
                            {
                                sText = Constants.vbNewLine + sText;
                            }
                        }

                        m_oGame.LastRowWasPrompt = false;
                    }
                }
            }

            if (InvokeRequired == true)
            {
                var parameters = new object[] { sText, oColor, oBgColor, oTargetWindow, bNoCache, bMono };
                Invoke(new AddTextDelegate(InvokeAddText), parameters);
            }
            else
            {
                InvokeAddText(sText, oColor, oBgColor, oTargetWindow, bNoCache, bMono);
            }
        }

        private void AddText(string sText, Color oColor, Color oBgColor, [Optional, DefaultParameterValue(Genie.Game.WindowTarget.Main)] Genie.Game.WindowTarget oTargetWindow, [Optional, DefaultParameterValue("")] string sTargetWindow, bool bNoCache = true, bool bMono = false, bool bPrompt = false, bool bInput = false)
        {
            if (IsDisposed)
            {
                return;
            }

            FormSkin oFormTarget = null;
            if (!Information.IsNothing(m_oOutputMain))
            {
                switch (oTargetWindow)
                {
                    case Genie.Game.WindowTarget.Death:
                        {
                            oFormTarget = m_oOutputDeath;
                            break;
                        }

                    case Genie.Game.WindowTarget.Familiar:
                        {
                            oFormTarget = m_oOutputFamiliar;
                            break;
                        }

                    case Genie.Game.WindowTarget.Inv:
                        {
                            if (!Information.IsNothing(m_oOutputInv) && m_oOutputInv.Visible == true)
                                oFormTarget = m_oOutputInv;
                            break;
                        }

                    case Genie.Game.WindowTarget.Log:
                        {
                            oFormTarget = m_oOutputLog;
                            break;
                        }

                    case Genie.Game.WindowTarget.Logons:
                        {
                            oFormTarget = m_oOutputLogons;
                            break;
                        }

                    case Genie.Game.WindowTarget.Room:
                        {
                            if (!Information.IsNothing(m_oOutputRoom) && m_oOutputRoom.Visible == true)
                                oFormTarget = m_oOutputRoom;
                            break;
                        }

                    case Genie.Game.WindowTarget.Thoughts:
                        {
                            oFormTarget = m_oOutputThoughts;
                            break;
                        }
                    case Genie.Game.WindowTarget.Combat:
                        {
                            oFormTarget = m_oOutputCombat;
                            break;
                        }
                    case Genie.Game.WindowTarget.Portrait:
                        {
                            oFormTarget = m_oOutputPortrait;
                            break;
                        }
                    case Genie.Game.WindowTarget.ActiveSpells:
                        {
                            oFormTarget = m_oOutputActiveSpells;
                            break;
                        }
                    case Genie.Game.WindowTarget.Debug:
                        {
                            oFormTarget = m_oOutputDebug;
                            break;
                        }
                    case Genie.Game.WindowTarget.Other:
                        {
                            oFormTarget = FindSkinFormByName(sTargetWindow);
                            break;
                        }

                    default:
                        {
                            oFormTarget = m_oOutputMain;
                            break;
                        }
                }

                if (Information.IsNothing(oFormTarget))
                    return;
                if (oFormTarget.Visible == false)
                {
                    oFormTarget = FindIfClosed(oFormTarget.IfClosed);
                }

                if (Information.IsNothing(oFormTarget))
                    return;
                AddText(sText, oColor, oBgColor, oFormTarget, bNoCache, bMono, bPrompt, bInput);
            }
        }
        private void AddImage(string sImageFileName, FormSkin oTargetWindow, int width, int height)
        {
            // bPrompt = false;

            if (IsDisposed)
            {
                return;
            }

            if (Information.IsNothing(oTargetWindow))
            {
                oTargetWindow = m_oOutputMain;
            }

            if (oTargetWindow.Equals(m_oOutputMain))
            {

            }

            if (InvokeRequired == true)
            {
                var parameters = new object[] { sImageFileName, oTargetWindow, width, height };
                Invoke(new AddImageDelegate(InvokeAddImage), parameters);
            }
            else
            {
                InvokeAddImage(sImageFileName, oTargetWindow, width, height);
            }
        }

        private void AddImage(string sImageFileName, string sTargetWindow, int width, int height)
        {
            Genie.Game.WindowTarget targetWindow = string.IsNullOrEmpty(sTargetWindow) ? Genie.Game.WindowTarget.Portrait : Genie.Game.WindowTarget.Other;
            AddImage(sImageFileName, targetWindow, sTargetWindow, width, height);
        }
        private void AddImage(string sImageFileName, Genie.Game.WindowTarget oTargetWindow, string sTargetWindow, int width, int height)
        {
            if (IsDisposed)
            {
                return;
            }

            FormSkin oFormTarget = null;
            if (!Information.IsNothing(m_oOutputMain))
            {
                switch (oTargetWindow)
                {
                    case Genie.Game.WindowTarget.Portrait:
                        {
                            oFormTarget = m_oOutputPortrait;
                            break;
                        }
                    case Genie.Game.WindowTarget.Death:
                        {
                            oFormTarget = m_oOutputDeath;
                            break;
                        }

                    case Genie.Game.WindowTarget.Familiar:
                        {
                            oFormTarget = m_oOutputFamiliar;
                            break;
                        }

                    case Genie.Game.WindowTarget.Inv:
                        {
                            if (!Information.IsNothing(m_oOutputInv) && m_oOutputInv.Visible == true)
                                oFormTarget = m_oOutputInv;
                            break;
                        }

                    case Genie.Game.WindowTarget.Log:
                        {
                            oFormTarget = m_oOutputLog;
                            break;
                        }

                    case Genie.Game.WindowTarget.Logons:
                        {
                            oFormTarget = m_oOutputLogons;
                            break;
                        }

                    case Genie.Game.WindowTarget.Room:
                        {
                            if (!Information.IsNothing(m_oOutputRoom) && m_oOutputRoom.Visible == true)
                                oFormTarget = m_oOutputRoom;
                            break;
                        }

                    case Genie.Game.WindowTarget.Thoughts:
                        {
                            oFormTarget = m_oOutputThoughts;
                            break;
                        }
                    case Genie.Game.WindowTarget.Combat:
                        {
                            oFormTarget = m_oOutputCombat;
                            break;
                        }
                    case Genie.Game.WindowTarget.ActiveSpells:
                        {
                            oFormTarget = m_oOutputActiveSpells;
                            break;
                        }
                    case Genie.Game.WindowTarget.Debug:
                        {
                            oFormTarget = m_oOutputDebug;
                            break;
                        }
                    case Genie.Game.WindowTarget.Other:
                        {
                            oFormTarget = FindSkinFormByName(sTargetWindow);
                            break;
                        }

                    default:
                        {
                            oFormTarget = m_oOutputMain;
                            break;
                        }
                }

                if (Information.IsNothing(oFormTarget))
                    return;
                if (oFormTarget.Visible == false)
                {
                    oFormTarget = FindIfClosed(oFormTarget.IfClosed);
                }

                if (Information.IsNothing(oFormTarget))
                    return;
                AddImage(sImageFileName, oFormTarget, width, height);
            }
        }

        private FormSkin FindIfClosed(string IfClosed, int Depth = 0)
        {
            Depth += 1;
            if (Depth > 10)
                return null;

            // Nothing means default = main window
            // "" means ignore output
            if (Information.IsNothing(IfClosed))
                return m_oOutputMain;
            if (string.IsNullOrEmpty(IfClosed))
                return null;
            var oFormSkin = FindSkinFormByID(IfClosed);
            if (!Information.IsNothing(oFormSkin))
            {
                if (oFormSkin.Visible == false)
                {
                    return FindIfClosed(oFormSkin.IfClosed, Depth);
                }
                else
                {
                    return oFormSkin;
                }
            }

            return null;
        }

        public delegate void AddTextDelegate(string sText, Color oColor, Color oBgColor, FormSkin oTargetwindow, bool bNoCache, bool bMono);

        private void InvokeAddText(string sText, Color oColor, Color oBgColor, FormSkin oTargetWindow, bool bNoCache = true, bool bMono = false)
        {
            if (!Information.IsNothing(oTargetWindow))
            {
                oTargetWindow.RichTextBoxOutput.AddText(sText, oColor, oBgColor, bNoCache, bMono);
                oTargetWindow.RichTextBoxOutput.TryInvalidate();
            }
        }

        public delegate void AddImageDelegate(string sImageFilePath, FormSkin oTargetWindow, int width, int height);
        private async void InvokeAddImage(string sImageFilePath, FormSkin oTargetWindow, int width, int height)
        {
            if (!Information.IsNothing(oTargetWindow))
            {
                Image image = await FileHandler.GetImage(Path.Combine(m_oGlobals.Config.ArtDir, sImageFilePath), width, height);
                if (oTargetWindow == m_oOutputPortrait) m_oOutputPortrait.ClearWindow();
                oTargetWindow.RichTextBoxOutput.AddImage(image);
            }
        }

        public delegate void ClearWindowDelegate(FormSkin oTargetWindow);

        private void SafeClearWindow(FormSkin oTargetWindow)
        {
            if (InvokeRequired == true)
            {
                var parameters = new[] { oTargetWindow };
                Invoke(new ClearWindowDelegate(ClearWindow), parameters);
            }
            else
            {
                ClearWindow(oTargetWindow);
            }
        }

        private void ClearWindow(FormSkin oTargetWindow)
        {
            oTargetWindow.ClearWindow();
        }

        private void Command_EventChangeWindowTitle(string sWindow, string sComment)
        {
            SafeChangeWindowTitle(sWindow, sComment);
        }

        public delegate void ChangeWindowTitleDelegate(string sWindow, string sComment);

        private void SafeChangeWindowTitle(string sWindow, string sComment)
        {
            if (InvokeRequired == true)
            {
                var parameters = new[] { sWindow, sComment };
                Invoke(new ChangeWindowTitleDelegate(ChangeWindowTitle), parameters);
            }
            else
            {
                ChangeWindowTitle(sWindow, sComment);
            }
        }

        private void ChangeWindowTitle(string sWindow, string sComment)
        {
            FormSkin oFormSkin = null;
            if (sWindow.Length > 0)
            {
                if ((sWindow.ToLower() ?? "") == (m_oOutputMain.ID ?? ""))
                {
                    oFormSkin = m_oOutputMain;
                }
                else
                {
                    var oEnumerator = m_oFormList.GetEnumerator();
                    while (oEnumerator.MoveNext())
                    {
                        if ((((FormSkin)oEnumerator.Current).ID ?? "") == (sWindow.ToLower().Trim() ?? ""))
                        {
                            oFormSkin = (FormSkin)oEnumerator.Current;
                            break;
                        }
                    }
                }
            }

            if (!Information.IsNothing(oFormSkin))
            {
                oFormSkin.Comment = sComment;
                oFormSkin.Invalidate();
            }
        }

        private FormSkin FindSkinFormByID(string sID)
        {
            FormSkin oFormSkin = null;
            if (sID.Length > 0)
            {
                if ((sID.ToLower() ?? "") == (m_oOutputMain.ID ?? ""))
                {
                    oFormSkin = m_oOutputMain;
                }
                else
                {
                    var oEnumerator = m_oFormList.GetEnumerator();
                    while (oEnumerator.MoveNext())
                    {
                        if ((((FormSkin)oEnumerator.Current).ID ?? "") == (sID.ToLower().Trim() ?? ""))
                        {
                            oFormSkin = (FormSkin)oEnumerator.Current;
                            break;
                        }
                    }
                }
            }

            return oFormSkin;
        }

        private FormSkin FindSkinFormByIDOrName(string sID, string sWindow)
        {
            FormSkin oFormSkin = null;
            if (sID.Length > 0)
            {
                if ((sID.ToLower() ?? "") == (m_oOutputMain.ID ?? ""))
                {
                    oFormSkin = m_oOutputMain;
                }
                else
                {
                    var oEnumerator = m_oFormList.GetEnumerator();
                    while (oEnumerator.MoveNext())
                    {
                        if ((((FormSkin)oEnumerator.Current).ID ?? "") == (sID.ToLower().Trim() ?? "") | (((FormSkin)oEnumerator.Current).Title.ToLower() ?? "") == (sWindow.ToLower().Trim() ?? ""))
                        {
                            oFormSkin = (FormSkin)oEnumerator.Current;
                            break;
                        }
                    }
                }
            }

            return oFormSkin;
        }

        private FormSkin FindSkinFormByName(string sWindow)
        {
            FormSkin oFormSkin = null;
            if (sWindow.Length > 0)
            {
                if ((sWindow.ToLower() ?? "") == (m_oOutputMain.Name ?? ""))
                {
                    oFormSkin = m_oOutputMain;
                }
                else
                {
                    var oEnumerator = m_oFormList.GetEnumerator();
                    while (oEnumerator.MoveNext())
                    {
                        if ((((FormSkin)oEnumerator.Current).Title.ToLower() ?? "") == (sWindow.ToLower().Trim() ?? ""))
                        {
                            oFormSkin = (FormSkin)oEnumerator.Current;
                            break;
                        }
                    }
                }
            }

            return oFormSkin;
        }

        private void Command_EventClearWindow(string sWindow)
        {
            try
            {
                var oFormSkin = FindSkinFormByIDOrName(sWindow, sWindow);
                if (!Information.IsNothing(oFormSkin)) // Do not clear if window does not exist
                {
                    SafeClearWindow(oFormSkin);
                }
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("ClearWindow", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        private void Simutronics_EventPrintText(string sText, GenieColor oColor, GenieColor oBgColor, Genie.Game.WindowTarget oTargetWindow, string sTargetWindow, bool bMono, bool bPrompt, bool bInput)
        {
            try
            {
                AddText(sText, oColor.ToDrawingColor(), oBgColor.ToDrawingColor(), oTargetWindow, sTargetWindow, false, bMono, bPrompt, bInput); // False = Cache this
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("PrintText", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        private void Script_EventPrintText(string sText, GenieColor oColor, GenieColor oBgColor)
        {
            Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
            string argsTargetWindow = "";
            AddText(sText, oColor.ToDrawingColor(), oBgColor.ToDrawingColor(), oTargetWindow: argoTargetWindow, sTargetWindow: argsTargetWindow);
        }

        private void EndUpdate()
        {
            FormSkin oFormSkin;
            var oEnumerator = m_oFormList.GetEnumerator();
            while (oEnumerator.MoveNext())
            {
                oFormSkin = (FormSkin)oEnumerator.Current;
                oFormSkin.RichTextBoxOutput.EndTextUpdate();
            }
        }

        private void Script_EventPrintError(string sText)
        {
            var argoColor = Color.WhiteSmoke;
            var argoBgColor = Color.DarkRed;
            Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
            string argsTargetWindow = "";
            AddText(sText, argoColor, argoBgColor, oTargetWindow: argoTargetWindow, sTargetWindow: argsTargetWindow);
            // Send these errors to a different window later. For easy monitoring.
        }
    }
}
