using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using GenieClient.Genie;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace GenieClient
{
    // Layout persistence: LoadXMLConfig, SaveXMLConfig, window positioning, dock handlers, layout commands.
    public partial class FormMain
    {
        private void CreateGenieFolders()
        {
            Utility.CreateDirectory(LocalDirectory.Path + @"\Config");
            Utility.CreateDirectory(LocalDirectory.Path + @"\Config\Profiles");
            Utility.CreateDirectory(LocalDirectory.Path + @"\Config\Layout");
            Utility.CreateDirectory(LocalDirectory.Path + @"\Config\PluginKeys");
            Utility.MoveLayoutFiles();
            Utility.CreateDirectory(LocalDirectory.Path + @"\Help");
            Utility.CreateDirectory(LocalDirectory.Path + @"\Icons");
            Utility.CreateDirectory(LocalDirectory.Path + @"\Logs");
            Utility.CreateDirectory(LocalDirectory.Path + @"\Scripts");
            Utility.CreateDirectory(LocalDirectory.Path + @"\Sounds");
            Utility.CreateDirectory(LocalDirectory.Path + @"\Plugins");
            Utility.CreateDirectory(LocalDirectory.Path + @"\Maps");
        }

        private bool m_bSetDefaultLayout = false;

        private bool LoadXMLConfig(string sFileName)
        {
            if (Information.IsNothing(sFileName))
            {
                return false;
            }

            if (m_oOutputMain.Visible)
            {
                string argsText = "Layout Loaded: " + sFileName + System.Environment.NewLine;
                var argoColor = Color.WhiteSmoke;
                var argoBgColor = Color.Transparent;
                Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                string argsTargetWindow = "";
                AddText(argsText, argoColor, argoBgColor, oTargetWindow: argoTargetWindow, sTargetWindow: argsTargetWindow);
            }

            m_IsChangingLayout = true;

            // Hide all windows
            for (int J = 0, loopTo = MdiChildren.Length - 1; J <= loopTo; J++)
            {
                if (MdiChildren[J] is FormSkin)
                {
                    MdiChildren[J].Tag = false;
                }
            }

            int I = 0;
            string s = string.Empty;
            m_oConfig = new Genie.XMLConfig();
            if (m_oConfig.LoadFile(sFileName) == true)
            {
                I = m_oConfig.GetValue("Genie/Windows/Main", "Width", Width);
                if (I < MinimumSize.Width)
                {
                    I = MinimumSize.Width;
                }

                Width = I;
                I = m_oConfig.GetValue("Genie/Windows/Main", "Height", Height);
                if (I < MinimumSize.Height)
                {
                    I = MinimumSize.Height;
                }

                Height = I;
                I = m_oConfig.GetValue("Genie/Windows/Main", "Left", Left);
                Left = I;
                I = m_oConfig.GetValue("Genie/Windows/Main", "Top", Top);
                Top = I;
                if (m_oConfig.GetValue("Genie/Windows/Main", "Maximized", false) == true)
                {
                    WindowState = FormWindowState.Maximized;
                }
                else
                {
                    WindowState = FormWindowState.Normal;
                }

                if (m_oConfig.GetValue("Genie/ScriptBar", "Visible", true) == true)
                {
                    ToolStripButtons.Visible = true;
                    ShowScriptBarToolStripMenuItem.Checked = true;
                }
                else
                {
                    ToolStripButtons.Visible = false;
                    ShowScriptBarToolStripMenuItem.Checked = false;
                }

                I = m_oConfig.GetValue("Genie/Windows/Game", "Width", m_oOutputMain.Width);
                if (I < m_oOutputMain.MinimumSize.Width)
                {
                    I = m_oOutputMain.MinimumSize.Width;
                }

                m_oOutputMain.Width = I;
                I = m_oConfig.GetValue("Genie/Windows/Game", "Height", m_oOutputMain.Height);
                if (I < m_oOutputMain.MinimumSize.Height)
                {
                    I = m_oOutputMain.MinimumSize.Height;
                }

                m_oOutputMain.Height = I;
                I = m_oConfig.GetValue("Genie/Windows/Game", "Left", m_oOutputMain.Left);
                if (I < 0)
                {
                    I = 0;
                }

                m_oOutputMain.Left = I;
                I = m_oConfig.GetValue("Genie/Windows/Game", "Top", m_oOutputMain.Top);
                if (I < 0)
                {
                    I = 0;
                }

                bool bTimeStamp = m_oConfig.GetValue("Genie/Windows/Game", "TimeStamp", false);
                m_oOutputMain.TimeStamp = bTimeStamp;
                string sColorName = m_oConfig.GetValue("Genie/Windows/Game", "Colors", string.Empty);
                if (sColorName.Length > 0)
                {
                    if (sColorName.Contains(",") == true && sColorName.EndsWith(",") == false)
                    {
                        string sColor = sColorName.Substring(0, sColorName.IndexOf(",")).Trim();
                        string sBgColor = sColorName.Substring(sColorName.IndexOf(",") + 1).Trim();
                        m_oOutputMain.RichTextBoxOutput.ForeColor = Genie.ColorCode.StringToColor(sColor).ToDrawingColor();
                        m_oOutputMain.RichTextBoxOutput.BackColor = Genie.ColorCode.StringToColor(sBgColor).ToDrawingColor();
                    }
                    else
                    {
                        m_oOutputMain.RichTextBoxOutput.ForeColor = Genie.ColorCode.StringToColor(sColorName).ToDrawingColor();
                    }
                }

                bool bNameListOnly = m_oConfig.GetValue("Genie/Windows/Game", "NameListOnly", false);
                m_oOutputMain.NameListOnly = bNameListOnly;
                string sFontFamily = m_oConfig.GetValue("Genie/Windows/Game/Font", "Family", string.Empty);
                if (sFontFamily.Length > 0)
                {
                    float oFontSize = m_oConfig.GetValueSingle("Genie/Windows/Game/Font", "Size", 9);
                    string sFontStyle = m_oConfig.GetValue("Genie/Windows/Game/Font", "Style", "Regular");
                    FontStyle oFontStyle = (FontStyle)Enum.Parse(typeof(FontStyle), sFontStyle, true);
                    m_oOutputMain.TextFont = new Font(sFontFamily, oFontSize, oFontStyle);
                }

                string sMonoFontFamily = m_oConfig.GetValue("Genie/Windows/MonoFont", "Family", string.Empty);
                if (sMonoFontFamily.Length > 0)
                {
                    float oFontSize = m_oConfig.GetValueSingle("Genie/Windows/MonoFont", "Size", 9);
                    string sFontStyle = m_oConfig.GetValue("Genie/Windows/MonoFont", "Style", "Regular");
                    FontStyle oFontStyle = (FontStyle)Enum.Parse(typeof(FontStyle), sFontStyle, true);
                    m_oGlobals.Config.MonoFont = new GenieFont(sMonoFontFamily, oFontSize, (GenieFontStyle)oFontStyle);
                }

                string sInputFontFamily = m_oConfig.GetValue("Genie/Windows/InputFont", "Family", string.Empty);
                if (sInputFontFamily.Length > 0)
                {
                    float oFontSize = m_oConfig.GetValueSingle("Genie/Windows/InputFont", "Size", 9);
                    string sFontStyle = m_oConfig.GetValue("Genie/Windows/InputFont", "Style", "Regular");
                    FontStyle oFontStyle = (FontStyle)Enum.Parse(typeof(FontStyle), sFontStyle, true);
                    m_oGlobals.Config.InputFont = new GenieFont(sInputFontFamily, oFontSize, (GenieFontStyle)oFontStyle);
                    UpdateInputFont();
                }

                m_oOutputMain.Top = I;
                I = m_oConfig.GetValue("Genie/Windows", "WindowCount", 0);
                if (I > 0)
                {
                    int j = 0;
                    while (j < I)
                    {
                        j = j + 1;
                        string sName = m_oConfig.GetValue("Genie/Windows/Window" + j.ToString(), "Name", "Output");
                        string sID = m_oConfig.GetValue("Genie/Windows/Window" + j.ToString(), "ID", "nothing");
                        if ((sID ?? "") == "nothing")
                        {
                            sID = sName.ToLower();
                            if ((sID ?? "") == "inventory")
                                sID = "inv";
                        }

                        string sIfClosed = m_oConfig.GetValue("Genie/Windows/Window" + j.ToString(), "IfClosed", "nothing");
                        if ((sIfClosed ?? "") == "nothing")
                        {
                            sIfClosed = null;
                        }

                        int iWidth = m_oConfig.GetValue("Genie/Windows/Window" + j.ToString(), "Width", 100);
                        int iHeight = m_oConfig.GetValue("Genie/Windows/Window" + j.ToString(), "Height", 100);
                        int iTop = m_oConfig.GetValue("Genie/Windows/Window" + j.ToString(), "Top", 0);
                        int iLeft = m_oConfig.GetValue("Genie/Windows/Window" + j.ToString(), "Left", 0);
                        bool bVisible = m_oConfig.GetValue("Genie/Windows/Window" + j.ToString(), "Visible", true);
                        sColorName = m_oConfig.GetValue("Genie/Windows/Window" + j.ToString(), "Colors", string.Empty);
                        bTimeStamp = m_oConfig.GetValue("Genie/Windows/Window" + j.ToString(), "TimeStamp", false);
                        bNameListOnly = m_oConfig.GetValue("Genie/Windows/Window" + j.ToString(), "NameListOnly", false);
                        FormSkin oFormTemp = null;
                        sFontFamily = m_oConfig.GetValue("Genie/Windows/Window" + j.ToString() + "/Font", "Family", string.Empty);
                        if (sFontFamily.Length > 0)
                        {
                            float oFontSize = m_oConfig.GetValueSingle("Genie/Windows/Window" + j.ToString() + "/Font", "Size", 9);
                            string sFontStyle = m_oConfig.GetValue("Genie/Windows/Window" + j.ToString() + "/Font", "Style", "Regular");
                            FontStyle oFontStyle = (FontStyle)Enum.Parse(typeof(FontStyle), sFontStyle, true);
                            oFormTemp = SafeCreateOutputForm(sID, sName, sIfClosed, iWidth, iHeight, iTop, iLeft, bVisible, new Font(sFontFamily, oFontSize, oFontStyle), sColorName);
                        }
                        else
                        {
                            oFormTemp = SafeCreateOutputForm(sID, sName, sIfClosed, iWidth, iHeight, iTop, iLeft, bVisible, null, sColorName);
                        }

                        if (!Information.IsNothing(oFormTemp))
                        {
                            oFormTemp.TimeStamp = bTimeStamp;
                            oFormTemp.NameListOnly = bNameListOnly;
                        }
                    }
                }

                string sDock = m_oConfig.GetValue("Genie/ScriptBar", "Dock", "Top");
                if ((sDock.ToLower() ?? "") == "top")
                {
                    ToolStripButtons.Dock = DockStyle.Top;
                    DockTopToolStripMenuItem1.Checked = true;
                    DockBottomToolStripMenuItem1.Checked = false;
                }
                else
                {
                    ToolStripButtons.Dock = DockStyle.Bottom;
                    DockTopToolStripMenuItem1.Checked = false;
                    DockBottomToolStripMenuItem1.Checked = true;
                }

                sDock = m_oConfig.GetValue("Genie/IconBar", "Dock", "Bottom");
                if ((sDock.ToLower() ?? "") == "top")
                {
                    PanelStatus.Dock = DockStyle.Top;
                    DockTopToolStripMenuItem.Checked = true;
                    DockBottomToolStripMenuItem.Checked = false;
                }
                else
                {
                    PanelStatus.Dock = DockStyle.Bottom;
                    DockTopToolStripMenuItem.Checked = false;
                    DockBottomToolStripMenuItem.Checked = true;
                }

                sDock = m_oConfig.GetValue("Genie/HealthBar", "Dock", "Bottom");
                if ((sDock.ToLower() ?? "") == "top")
                {
                    PanelBars.Dock = DockStyle.Top;
                    DockTopToolStripMenuItem2.Checked = true;
                    DockBottomToolStripMenuItem2.Checked = false;
                }
                else
                {
                    PanelBars.Dock = DockStyle.Bottom;
                    DockTopToolStripMenuItem2.Checked = false;
                    DockBottomToolStripMenuItem2.Checked = true;
                }

                bool bShow = true;
                bShow = m_oConfig.GetValue("Genie/IconBar", "Visible", true);
                PanelStatus.Visible = bShow;
                bShow = m_oConfig.GetValue("Genie/HealthBar", "Visible", true);
                PanelBars.Visible = bShow;
                bShow = m_oConfig.GetValue("Genie/HealthBar", "Magic", true);
                SetMagicPanels(bShow);
                bShow = m_oConfig.GetValue("Genie/StatusBar", "Visible", true);
                StatusStripMain.Visible = bShow;
                SetDefaultSettings();
                m_IsChangingLayout = false;
                UpdateWindowMenuList();
                return true;
            }
            else
            {
                m_oConfig.LoadXml("<Genie><Windows></Windows><Settings></Settings></Genie>");
                SetDefaultSettings();
                m_bSetDefaultLayout = true;
                m_IsChangingLayout = false;
                UpdateWindowMenuList();
                return false;
            }
        }

        public bool SaveXMLConfig(string filename = null)
        {
            if (m_oConfig.GetValue("Genie/Windows/Game", "Name", string.Empty).Length == 0)
            {
                m_oConfig.LoadXml("<Genie><Windows></Windows><Settings></Settings></Genie>");
            }

            if (WindowState == FormWindowState.Normal)
            {
                m_oConfig.SetValue("Genie/Windows/Main", "Height", Height.ToString());
                m_oConfig.SetValue("Genie/Windows/Main", "Width", Width.ToString());
                m_oConfig.SetValue("Genie/Windows/Main", "Left", Left.ToString());
                m_oConfig.SetValue("Genie/Windows/Main", "Top", Top.ToString());
            }

            m_oConfig.SetValue("Genie/Windows/Main", "Maximized", (WindowState == FormWindowState.Maximized).ToString());
            m_oConfig.SetValue("Genie/ScriptBar", "Visible", ToolStripButtons.Visible.ToString());
            m_oConfig.SetValue("Genie/Windows/MonoFont", "Family", m_oGlobals.Config.MonoFont.FamilyName);
            m_oConfig.SetValue("Genie/Windows/MonoFont", "Size", m_oGlobals.Config.MonoFont.Size.ToString());
            m_oConfig.SetValue("Genie/Windows/MonoFont", "Style", m_oGlobals.Config.MonoFont.Style.ToString());
            m_oConfig.SetValue("Genie/Windows/InputFont", "Family", m_oGlobals.Config.InputFont.FamilyName);
            m_oConfig.SetValue("Genie/Windows/InputFont", "Size", m_oGlobals.Config.InputFont.Size.ToString());
            m_oConfig.SetValue("Genie/Windows/InputFont", "Style", m_oGlobals.Config.InputFont.Style.ToString());
            m_oConfig.SetValue("Genie/Windows/Game", "ID", "main");
            m_oConfig.SetValue("Genie/Windows/Game", "Name", m_oOutputMain.Title);
            m_oConfig.SetValue("Genie/Windows/Game", "Height", m_oOutputMain.Height.ToString());
            m_oConfig.SetValue("Genie/Windows/Game", "Width", m_oOutputMain.Width.ToString());
            m_oConfig.SetValue("Genie/Windows/Game", "Left", m_oOutputMain.Left.ToString());
            m_oConfig.SetValue("Genie/Windows/Game", "Top", m_oOutputMain.Top.ToString());
            m_oConfig.SetValue("Genie/Windows/Game", "TimeStamp", m_oOutputMain.TimeStamp.ToString());
            m_oConfig.SetValue("Genie/Windows/Game", "Colors", Genie.ColorCode.ColorToString(m_oOutputMain.RichTextBoxOutput.ForeColor, m_oOutputMain.RichTextBoxOutput.BackColor));
            m_oConfig.SetValue("Genie/Windows/Game", "NameListOnly", m_oOutputMain.NameListOnly.ToString());
            m_oConfig.SetValue("Genie/Windows/Game/Font", "Family", m_oOutputMain.TextFont.Name.ToString());
            m_oConfig.SetValue("Genie/Windows/Game/Font", "Size", m_oOutputMain.TextFont.Size.ToString());
            m_oConfig.SetValue("Genie/Windows/Game/Font", "Style", m_oOutputMain.TextFont.Style.ToString());
            RemoveDisposedForms();
            FormSkin tmpFormSkin;
            var myEnumerator = m_oFormList.GetEnumerator();
            string WindowList = string.Empty;
            int i = 0;
            while (myEnumerator.MoveNext())
            {
                tmpFormSkin = (FormSkin)myEnumerator.Current;
                i = i + 1;
                if (Information.IsNothing(tmpFormSkin.ID))
                {
                    tmpFormSkin.ID = tmpFormSkin.Title.ToLower();
                }

                m_oConfig.SetValue("Genie/Windows/Window" + i.ToString(), "ID", tmpFormSkin.ID);
                m_oConfig.SetValue("Genie/Windows/Window" + i.ToString(), "Name", tmpFormSkin.Title);
                if (!Information.IsNothing(tmpFormSkin.IfClosed))
                {
                    m_oConfig.SetValue("Genie/Windows/Window" + i.ToString(), "IfClosed", tmpFormSkin.IfClosed);
                }

                m_oConfig.SetValue("Genie/Windows/Window" + i.ToString(), "Height", tmpFormSkin.Height.ToString());
                m_oConfig.SetValue("Genie/Windows/Window" + i.ToString(), "Width", tmpFormSkin.Width.ToString());
                m_oConfig.SetValue("Genie/Windows/Window" + i.ToString(), "Left", tmpFormSkin.Left.ToString());
                m_oConfig.SetValue("Genie/Windows/Window" + i.ToString(), "Top", tmpFormSkin.Top.ToString());
                m_oConfig.SetValue("Genie/Windows/Window" + i.ToString(), "Visible", tmpFormSkin.Visible.ToString());
                m_oConfig.SetValue("Genie/Windows/Window" + i.ToString(), "TimeStamp", tmpFormSkin.TimeStamp.ToString());
                m_oConfig.SetValue("Genie/Windows/Window" + i.ToString(), "Colors", Genie.ColorCode.ColorToString(tmpFormSkin.RichTextBoxOutput.ForeColor, tmpFormSkin.RichTextBoxOutput.BackColor));
                m_oConfig.SetValue("Genie/Windows/Window" + i.ToString(), "NameListOnly", tmpFormSkin.NameListOnly.ToString());
                m_oConfig.SetValue("Genie/Windows/Window" + i.ToString() + "/Font", "Family", tmpFormSkin.TextFont.Name.ToString());
                m_oConfig.SetValue("Genie/Windows/Window" + i.ToString() + "/Font", "Size", tmpFormSkin.TextFont.Size.ToString());
                m_oConfig.SetValue("Genie/Windows/Window" + i.ToString() + "/Font", "Style", tmpFormSkin.TextFont.Style.ToString());
            }

            m_oConfig.SetValue("Genie/Windows", "WindowCount", i.ToString());
            m_oConfig.SetValue("Genie/IconBar", "Visible", PanelStatus.Visible.ToString());
            m_oConfig.SetValue("Genie/HealthBar", "Visible", PanelBars.Visible.ToString());
            m_oConfig.SetValue("Genie/HealthBar", "Magic", MagicPanelsToolStripMenuItem.Checked.ToString());
            m_oConfig.SetValue("Genie/StatusBar", "Visible", StatusStripMain.Visible.ToString());
            m_oConfig.SetValue("Genie/IconBar", "Dock", PanelStatus.Dock.ToString());
            m_oConfig.SetValue("Genie/HealthBar", "Dock", PanelBars.Dock.ToString());
            m_oConfig.SetValue("Genie/ScriptBar", "Dock", ToolStripButtons.Dock.ToString());
            if (Information.IsNothing(filename))
            {
                return m_oConfig.SaveToFile();
            }
            else
            {
                return m_oConfig.SaveToFile(filename);
            }
        }

        private bool HasSettingsChanged()
        {
            int I;
            string s = string.Empty;
            if (Information.IsNothing(m_oConfig))
            {
                return true;
            }

            if (m_oConfig.HasData == false)
            {
                return true;
            }

            if (m_oConfig.GetValue("Genie/Windows/Main", "Maximized", false) == true)
            {
                if (WindowState != FormWindowState.Maximized)
                {
                    return true;
                }
            }
            else if (WindowState != FormWindowState.Normal)
            {
                return true;
            }
            else
            {
                I = m_oConfig.GetValue("Genie/Windows/Main", "Width", Width);
                if (I < MinimumSize.Width)
                {
                    I = MinimumSize.Width;
                }

                if (Width != I)
                {
                    return true;
                }

                I = m_oConfig.GetValue("Genie/Windows/Main", "Height", Height);
                if (I < MinimumSize.Height)
                {
                    I = MinimumSize.Height;
                }

                if (Height != I)
                {
                    return true;
                }

                I = m_oConfig.GetValue("Genie/Windows/Main", "Left", Left);
                if (Left != I)
                {
                    return true;
                }

                I = m_oConfig.GetValue("Genie/Windows/Main", "Top", Top);
                if (Top != I)
                {
                    return true;
                }
            }

            I = m_oConfig.GetValue("Genie/Windows/Game", "Width", m_oOutputMain.Width);
            if (I < m_oOutputMain.MinimumSize.Width)
            {
                I = m_oOutputMain.MinimumSize.Width;
            }

            if (m_oOutputMain.Width != I)
            {
                return true;
            }

            I = m_oConfig.GetValue("Genie/Windows/Game", "Height", m_oOutputMain.Height);
            if (I < m_oOutputMain.MinimumSize.Height)
            {
                I = m_oOutputMain.MinimumSize.Height;
            }

            if (m_oOutputMain.Height != I)
            {
                return true;
            }

            I = m_oConfig.GetValue("Genie/Windows/Game", "Left", m_oOutputMain.Left);
            if (I < 0)
            {
                I = 0;
            }

            if (m_oOutputMain.Left != I)
            {
                return true;
            }

            I = m_oConfig.GetValue("Genie/Windows/Game", "Top", m_oOutputMain.Top);
            if (I < 0)
            {
                I = 0;
            }

            if (m_oOutputMain.Top != I)
            {
                return true;
            }

            I = m_oConfig.GetValue("Genie/Windows", "WindowCount", 0);
            if (I > 0)
            {
                int j = 0;
                while (j < I)
                {
                    j = j + 1;
                    string sName = m_oConfig.GetValue("Genie/Windows/Window" + j.ToString(), "Name", "Output");
                    int iWidth = m_oConfig.GetValue("Genie/Windows/Window" + j.ToString(), "Width", 100);
                    int iHeight = m_oConfig.GetValue("Genie/Windows/Window" + j.ToString(), "Height", 100);
                    int iTop = m_oConfig.GetValue("Genie/Windows/Window" + j.ToString(), "Top", 0);
                    int iLeft = m_oConfig.GetValue("Genie/Windows/Window" + j.ToString(), "Left", 0);
                    bool bVisible = m_oConfig.GetValue("Genie/Windows/Window" + j.ToString(), "Visible", true);
                    if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(HasWindowsChanged(sName, iWidth, iHeight, iTop, iLeft, bVisible), true, false)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private object HasWindowsChanged(string sID, int iWidth, int iHeight, int iTop, int iLeft, bool bIsVisible)
        {
            var oEnumerator = m_oFormList.GetEnumerator();
            FormSkin oOutput;
            while (oEnumerator.MoveNext())
            {
                oOutput = (FormSkin)oEnumerator.Current;
                if ((oOutput.ID ?? "") == (sID ?? ""))
                {
                    if (oOutput.Width != iWidth)
                    {
                        return true;
                    }

                    if (oOutput.Height != iHeight)
                    {
                        return true;
                    }

                    if (oOutput.Top != iTop)
                    {
                        return true;
                    }

                    if (oOutput.Left != iLeft)
                    {
                        return true;
                    }

                    if (oOutput.Visible != bIsVisible)
                    {
                        return true;
                    }

                    return false;
                }
            }

            return true;
        }

        private void SetDefaultSettings()
        {
            if (Information.IsNothing(m_oOutputInv))
            {
                SafeCreateOutputForm("inv", "Inventory", null, 300, 200, 10, 10, false);
            }

            if (Information.IsNothing(m_oOutputFamiliar))
            {
                SafeCreateOutputForm("familiar", "Familiar", "game", 300, 200, 10, 10, false);
            }

            if (Information.IsNothing(m_oOutputThoughts))
            {
                SafeCreateOutputForm("thoughts", "Thoughts", "game", 300, 200, 10, 10, false);
                m_oOutputThoughts.TimeStamp = true;
            }

            if (Information.IsNothing(m_oOutputLogons))
            {
                SafeCreateOutputForm("logons", "Arrivals", "", 300, 200, 10, 10, false);
                m_oOutputLogons.TimeStamp = true;
            }

            if (Information.IsNothing(m_oOutputDeath))
            {
                SafeCreateOutputForm("death", "Deaths", "", 300, 200, 10, 10, false);
                m_oOutputDeath.TimeStamp = true;
            }

            if (Information.IsNothing(m_oOutputLog))
            {
                SafeCreateOutputForm("log", "Log", null, 300, 200, 10, 10, false);
                m_oOutputLog.TimeStamp = true;
            }

            if (Information.IsNothing(m_oOutputRoom))
            {
                SafeCreateOutputForm("room", "Room", null, 300, 200, 10, 10, false);
            }

            if (Information.IsNothing(m_oOutputDebug))
            {
                SafeCreateOutputForm("debug", "Debug", null, 300, 200, 10, 10, false);
            }

            if (Information.IsNothing(FindSkinFormByID("talk")))
            {
                SafeCreateOutputForm("talk", "Talk", "conversation", 300, 200, 10, 10, false);
            }

            if (Information.IsNothing(FindSkinFormByID("whispers")))
            {
                SafeCreateOutputForm("whispers", "Whispers", "conversation", 300, 200, 10, 10, false);
            }

            if (Information.IsNothing(FindSkinFormByID("conversation")))
            {
                SafeCreateOutputForm("conversation", "Conversation", "log", 300, 200, 10, 10, false);
            }

            if (Information.IsNothing(FindSkinFormByID("raw")))
            {
                SafeCreateOutputForm("raw", "Raw", "", 300, 200, 10, 10, false);
            }

            if (Information.IsNothing(m_oOutputActiveSpells))
            {
                SafeCreateOutputForm("percWindow", "Active Spells", null, 300, 200, 10, 10, false);
            }

            if (Information.IsNothing(m_oOutputCombat))
            {
                SafeCreateOutputForm("combat", "Combat", null, 300, 200, 10, 10, false);
            }

            if (Information.IsNothing(m_oOutputPortrait))
            {
                SafeCreateOutputForm("portrait", "Portrait", null, 250, 350, 10, 10, false);
            }
        }

        public new object ClientSize
        {
            get
            {
                var oSize = base.ClientSize;
                oSize.Height = ClientHeight;
                return oSize;
            }

            set
            {
                base.ClientSize = (Size)value;
            }
        }

        public int ClientHeight
        {
            get
            {
                return Conversions.ToInteger(base.ClientSize.Height - (PanelStatus.Visible ? PanelStatus.Height : 0) - MenuStripMain.Height - (ToolStripButtons.Visible ? ToolStripButtons.Height : 0) - PanelInput.Height - (StatusStripMain.Visible ? StatusStripMain.Height : 0) - (PanelBars.Visible ? PanelBars.Height : 0));
            }
        }

        private void LayoutBasic()
        {
            m_IsChangingLayout = true;
            HideTagOutputForms();
            m_oOutputThoughts.Tag = true;
            m_oOutputRoom.Tag = true;
            m_oOutputLog.Tag = true;
            HideOutputForms();
            m_oOutputMain.Top = 0;
            m_oOutputMain.Left = 0;
            m_oOutputMain.Width = Conversions.ToInteger(Math.Floor(((Size)ClientSize).Width * 0.7));
            m_oOutputMain.Height = Conversions.ToInteger(((Size)ClientSize).Height - SystemInformation.Border3DSize.Height * 2); // - IIf(PanelStatus.Visible, PanelStatus.Height, 0) - MenuStripMain.Height - IIf(ToolStripButtons.Visible, ToolStripButtons.Height, 0) - PanelInput.Height - IIf(StatusStripMain.Visible, StatusStripMain.Height, 0) - IIf(PanelBars.Visible, PanelBars.Height, 0) - iMargin
            int h = Conversions.ToInteger(Math.Floor(m_oOutputMain.Height / (double)3));
            m_oOutputThoughts.Top = 0;
            m_oOutputThoughts.Left = m_oOutputMain.Width;
            m_oOutputThoughts.Width = Conversions.ToInteger(((Size)ClientSize).Width - m_oOutputMain.Width - SystemInformation.Border3DSize.Width * 2);
            m_oOutputThoughts.Height = h;
            m_oOutputRoom.Top = m_oOutputThoughts.Height;
            m_oOutputRoom.Left = m_oOutputMain.Width;
            m_oOutputRoom.Width = m_oOutputThoughts.Width;
            m_oOutputRoom.Height = h; // Math.Floor(h / 2)
            m_oOutputLog.Top = m_oOutputThoughts.Height + m_oOutputRoom.Height;
            m_oOutputLog.Left = m_oOutputMain.Width;
            m_oOutputLog.Width = m_oOutputThoughts.Width;
            m_oOutputLog.Height = m_oOutputMain.Height - m_oOutputLog.Top;
            m_oOutputMain.Visible = true;
            m_oOutputThoughts.Visible = true;
            m_oOutputRoom.Visible = true;
            m_oOutputLog.Visible = true;
            ShowOutputForms();
            ShowForm(m_oOutputMain);
            m_IsChangingLayout = false;
        }

        private void BasicToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutBasic();
        }

        public bool m_IsChangingLayout = false;

        private void SaveSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SaveXMLConfig(m_sConfigFile))
            {
                if (!Information.IsNothing(m_sConfigFile))
                {
                    if (m_oOutputMain.Visible)
                    {
                        string argsText = "Layout Saved: " + m_sConfigFile + System.Environment.NewLine;
                        var argoColor = Color.WhiteSmoke;
                        var argoBgColor = Color.Transparent;
                        Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                        string argsTargetWindow = "";
                        AddText(argsText, argoColor, argoBgColor, oTargetWindow: argoTargetWindow, sTargetWindow: argsTargetWindow);
                    }
                }
            }
        }

        private void ShowScriptBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripButtons.Visible = ShowScriptBarToolStripMenuItem.Checked;
        }

        private void DockTopToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ToolStripButtons.Dock = DockStyle.Top;
            if (ShowScriptBarToolStripMenuItem.Checked == false)
            {
                ShowScriptBarToolStripMenuItem.Checked = true;
                ToolStripButtons.Visible = true;
            }

            DockTopToolStripMenuItem1.Checked = true;
            DockBottomToolStripMenuItem1.Checked = false;
        }

        private void DockBottomToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ToolStripButtons.Dock = DockStyle.Bottom;
            if (ShowScriptBarToolStripMenuItem.Checked == false)
            {
                ShowScriptBarToolStripMenuItem.Checked = true;
                ToolStripButtons.Visible = true;
            }

            DockBottomToolStripMenuItem1.Checked = true;
            DockTopToolStripMenuItem1.Checked = false;
        }

        private void DockTopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PanelStatus.Dock = DockStyle.Top;
            if (IconBarToolStripMenuItem.Checked == false)
            {
                IconBarToolStripMenuItem.Checked = true;
                PanelStatus.Visible = true;
            }

            DockTopToolStripMenuItem.Checked = true;
            DockBottomToolStripMenuItem.Checked = false;
        }

        private void DockBottomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PanelStatus.Dock = DockStyle.Bottom;
            if (IconBarToolStripMenuItem.Checked == false)
            {
                IconBarToolStripMenuItem.Checked = true;
                PanelStatus.Visible = true;
            }

            DockBottomToolStripMenuItem.Checked = true;
            DockTopToolStripMenuItem.Checked = false;
        }

        private void IconBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PanelStatus.Visible = IconBarToolStripMenuItem.Checked;
        }

        private void HealthBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PanelBars.Visible = HealthBarToolStripMenuItem.Checked;
        }

        private void DockTopToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            PanelBars.Dock = DockStyle.Top;
            if (HealthBarToolStripMenuItem.Checked == false)
            {
                HealthBarToolStripMenuItem.Checked = true;
                PanelBars.Visible = true;
            }

            DockTopToolStripMenuItem2.Checked = true;
            DockBottomToolStripMenuItem2.Checked = false;
        }

        private void DockBottomToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            PanelBars.Dock = DockStyle.Bottom;
            if (HealthBarToolStripMenuItem.Checked == false)
            {
                HealthBarToolStripMenuItem.Checked = true;
                PanelBars.Visible = true;
            }

            DockBottomToolStripMenuItem2.Checked = true;
            DockTopToolStripMenuItem2.Checked = false;
        }

        private void StatusBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StatusStripMain.Visible = StatusBarToolStripMenuItem.Checked;
        }

        private void UpdateLayoutMenu()
        {
            IconBarToolStripMenuItem.Checked = PanelStatus.Visible;
            ShowScriptBarToolStripMenuItem.Checked = ToolStripButtons.Visible;
            HealthBarToolStripMenuItem.Checked = PanelBars.Visible;
            MagicPanelsToolStripMenuItem.Checked = ComponentBarsMana.Visible;
            StatusBarToolStripMenuItem.Checked = StatusStripMain.Visible;
        }

        private void LoadSettingsOpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialogLayout.InitialDirectory = Path.GetDirectoryName(LoadedLayout);
            OpenFileDialogLayout.FileName = Path.GetFileName(LoadedLayout);
            if (OpenFileDialogLayout.ShowDialog() == DialogResult.OK)
            {
                LoadLayout(OpenFileDialogLayout.FileName);
            }
        }

        public bool LoadLayout(string sFile = "")
        {
            if (Strings.Len(sFile.Trim()) == 0)
            {
                sFile = m_sConfigFile;
            }
            else if (sFile.Contains(@"\") == false)
            {
                if (sFile.ToLower().EndsWith(".xml"))
                    return false;
                sFile = m_oGlobals.Config.ConfigDir + @"\Layout\" + sFile;
                if (sFile.ToLower().EndsWith(".layout") == false)
                {
                    sFile += ".layout";
                }
            }

            if (LoadSizedXMLConfig(sFile) || LoadXMLConfig(sFile))
            {
                UpdateLayoutMenu();
                HideOutputForms();
                ShowOutputForms();
                ShowForm(m_oOutputMain);
                return true;
            }
            else
            {
                UpdateLayoutMenu();
                return false;
            }
        }

        public bool SaveLayout(string file = null)
        {
            if (Information.IsNothing(file))
            {
                file = m_sConfigFile;
            }

            if (SaveXMLConfig(file) == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void SaveSettingsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SaveFileDialogLayout.InitialDirectory = Path.GetDirectoryName(LoadedLayout);
            SaveFileDialogLayout.FileName = Path.GetFileName(LoadedLayout);
            if (SaveFileDialogLayout.ShowDialog() == DialogResult.OK)
            {
                if (SaveXMLConfig(SaveFileDialogLayout.FileName))
                {
                    string argsText = "Layout saved: " + SaveFileDialogLayout.FileName + System.Environment.NewLine;
                    var argoColor = Color.WhiteSmoke;
                    var argoBgColor = Color.Transparent;
                    Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                    string argsTargetWindow = "";
                    AddText(argsText, argoColor, argoBgColor, oTargetWindow: argoTargetWindow, sTargetWindow: argsTargetWindow);
                }
            }
        }

        private void Command_LoadLayout(string sFile)
        {
            if ((sFile ?? "") == "@windowsize@")
            {
                string argsText = "Current layout size: " + Width.ToString() + "x" + Height.ToString() + System.Environment.NewLine;
                Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                AddText(argsText, oTargetWindow: argoTargetWindow);
            }
            else if (LoadLayout(sFile) == false)
            {
                string argsText1 = "Loading layout failed: " + sFile + System.Environment.NewLine;
                var argoColor = Color.WhiteSmoke;
                var argoBgColor = Color.Transparent;
                Genie.Game.WindowTarget argoTargetWindow1 = Genie.Game.WindowTarget.Main;
                string argsTargetWindow = "";
                AddText(argsText1, argoColor, argoBgColor, oTargetWindow: argoTargetWindow1, sTargetWindow: argsTargetWindow);
            }
        }

        private void Command_SaveLayout(string sFile)
        {
            if (Strings.Len(sFile.Trim()) == 0)
            {
                if (SaveXMLConfig() == true)
                {
                    string argsText = "Layout saved: " + m_sConfigFile + System.Environment.NewLine;
                    var argoColor = Color.WhiteSmoke;
                    var argoBgColor = Color.Transparent;
                    Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                    string argsTargetWindow = "";
                    AddText(argsText, argoColor, argoBgColor, oTargetWindow: argoTargetWindow, sTargetWindow: argsTargetWindow);
                }
            }
            else if (sFile.Contains(@"\") == false)
            {
                if (sFile.ToLower().EndsWith(".xml"))
                {
                    sFile = m_oGlobals.Config.ConfigDir + @"\" + sFile;
                }
                else
                {
                    sFile = m_oGlobals.Config.ConfigDir + @"\Layout\" + sFile;
                    if (sFile.ToLower().EndsWith(".layout") == false)
                    {
                        sFile += ".layout";
                    }
                }

                if (SaveXMLConfig(sFile) == true)
                {
                    string argsText1 = "Layout Saved: " + sFile + System.Environment.NewLine;
                    var argoColor1 = Color.WhiteSmoke;
                    var argoBgColor1 = Color.Transparent;
                    Genie.Game.WindowTarget argoTargetWindow1 = Genie.Game.WindowTarget.Main;
                    string argsTargetWindow1 = "";
                    AddText(argsText1, argoColor1, argoBgColor1, oTargetWindow: argoTargetWindow1, sTargetWindow: argsTargetWindow1);
                }
            }
        }

        private void Command_EventAddWindow(string sName, int sWidth = 300, int sHeight = 200, int? sTop = 10, int? sLeft = 10)
        {
            AddWindow(sName, sWidth, sHeight, sTop, sLeft);
            UpdateWindowMenuList();
        }

        public delegate void ShowWindowDelegate();
        private void AddWindow(string sName, int sWidth = 300, int sHeight = 200, int? sTop = 10, int? sLeft = 10)
        {
            var oEnumerator = m_oFormList.GetEnumerator();
            while (oEnumerator.MoveNext())
            {
                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(((FormSkin)oEnumerator.Current).ID, sName.ToLower(), false)))
                {
                    if (InvokeRequired == true)
                    {
                        Invoke(new ShowWindowDelegate(((FormSkin)oEnumerator.Current).Show));
                    }
                    else
                    {
                        ((FormSkin)oEnumerator.Current).Show();
                    }
                    return;
                }
            }

            var fo = SafeCreateOutputForm(Conversions.ToString(sName.ToLower()), Conversions.ToString(sName), null, sWidth, sHeight, sTop.Value, sLeft.Value, true, null, "", true);
            if (!Information.IsNothing(fo))
            {
                fo.Visible = true;
            }
        }

        public delegate void PositionWindowDelegate(string sName, int? sWidth = 300, int? sHeight = 200, int? sTop = 10, int? sLeft = 10);

        private void Command_EventPositionWindow(string sName, int? sWidth = 300, int? sHeight = 200, int? sTop = 10, int? sLeft = 10)
        {
            if (InvokeRequired == true)
            {
                var parameters = new object[] { sName, sWidth, sHeight, sTop, sLeft };
                Invoke(new PositionWindowDelegate(PositionWindow), parameters);
            }
            else
            {
                PositionWindow(sName, sWidth, sHeight, sTop, sLeft);
            }
        }

        private void PositionWindow(string sName, int? sWidth = 300, int? sHeight = 200, int? sTop = 10, int? sLeft = 10)
        {
            m_IsChangingLayout = true;

            if (sName != "Game" && sName != "Main")
            {
                var oEnumerator = m_oFormList.GetEnumerator();
                while (oEnumerator.MoveNext())
                {
                    if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(((FormSkin)oEnumerator.Current).ID, sName.ToLower(), false)))
                    {
                        if (!sWidth.HasValue) { sWidth = ((FormSkin)oEnumerator.Current).Width; }
                        if (!sHeight.HasValue) { sHeight = ((FormSkin)oEnumerator.Current).Height; }
                        if (!sTop.HasValue) { sTop = ((FormSkin)oEnumerator.Current).Top; }
                        if (!sLeft.HasValue) { sLeft = ((FormSkin)oEnumerator.Current).Left; }

                        ((FormSkin)oEnumerator.Current).Hide();
                    }
                }
                if (!sWidth.HasValue) { sWidth = 0; }
                if (!sHeight.HasValue) { sHeight = 0; }
                if (!sTop.HasValue) { sTop = 0; }
                if (!sLeft.HasValue) { sLeft = 0; }

                var fo = SafeCreateOutputForm(Conversions.ToString(sName.ToLower()), Conversions.ToString(sName), null, sWidth.Value, sHeight.Value, sTop.Value, sLeft.Value, true, null, "", true);
                if (!Information.IsNothing(fo))
                {
                    fo.Visible = true;
                }
                m_IsChangingLayout = false;
                return;
            }

            int I = 0;

            if (sName == "Main") // This is the Genie client window
            {
                if (!sWidth.HasValue) { sWidth = Width; }
                I = sWidth.Value;
                if (I < MinimumSize.Width) { I = MinimumSize.Width; }
                Width = I;

                if (!sHeight.HasValue) { sHeight = Height; }
                I = sHeight.Value;
                if (I < MinimumSize.Height) { I = MinimumSize.Height; }
                Height = I;

                if (!sTop.HasValue) { sTop = Top; }
                I = sTop.Value;
                Top = I;

                if (!sLeft.HasValue) { sLeft = Left; }
                I = sLeft.Value;
                Left = I;
                m_IsChangingLayout = false;
                return;
            }
            if (sName == "Game") // This is the Main text output window
            {
                m_oOutputMain.Hide();
                if (!sWidth.HasValue) { sWidth = m_oOutputMain.Width; }
                I = sWidth.Value;
                if (I < m_oOutputMain.MinimumSize.Width) { I = m_oOutputMain.MinimumSize.Width; }
                m_oOutputMain.Width = I;

                if (!sHeight.HasValue) { sHeight = m_oOutputMain.Height; }
                I = sHeight.Value;
                if (I < m_oOutputMain.MinimumSize.Height) { I = m_oOutputMain.MinimumSize.Height; }
                m_oOutputMain.Height = I;

                if (!sTop.HasValue) { sTop = m_oOutputMain.Top; }
                I = sTop.Value;
                if (I < 0) { I = 0; }
                m_oOutputMain.Top = I;

                if (!sLeft.HasValue) { sLeft = m_oOutputMain.Left; }
                I = sLeft.Value;
                if (I < 0) { I = 0; }
                m_oOutputMain.Left = I;

                m_oOutputMain.Show();
                m_IsChangingLayout = false;
                return;
            }
        }


        private void Command_EventRemoveWindow(string sName)
        {
            FormSkin oForm = null;
            var oEnumerator = m_oFormList.GetEnumerator();
            while (oEnumerator.MoveNext())
            {
                if ((((FormSkin)oEnumerator.Current).ID ?? "") == (sName.ToLower() ?? ""))
                {
                    oForm = (FormSkin)oEnumerator.Current;
                    break;
                }
            }

            if (!Information.IsNothing(oForm))
            {
                if (oForm.UserForm)
                {
                    oForm.Unload();
                    oForm = null;
                    RemoveDisposedForms();
                    UpdateWindowMenuList();
                }
            }
        }

        private void Command_EventCloseWindow(string sName)
        {
            var oEnumerator = m_oFormList.GetEnumerator();
            while (oEnumerator.MoveNext())
            {
                if ((((FormSkin)oEnumerator.Current).ID ?? "") == (sName.ToLower() ?? ""))
                {
                    ((FormSkin)oEnumerator.Current).Hide();
                    UpdateWindowMenuList();
                    return;
                }
            }
        }

        private void FormMain_SizeChange(object sender, EventArgs e)
        {
            if (m_bIsLoading)
                return;
            if (WindowState == FormWindowState.Maximized)
            {
                if (LoadSizedXMLConfig(m_sConfigFile))
                {
                    UpdateLayoutMenu();
                    HideOutputForms();
                    ShowOutputForms();
                    ShowForm(m_oOutputMain);
                }
            }
        }

        private bool LoadSizedXMLConfig(string filename)
        {
            if (filename.Length > 0)
            {
                string sFile = SetSizeName(filename);
                if (File.Exists(sFile))
                {
                    return LoadXMLConfig(sFile);
                }
            }

            return false;
        }

        public bool SaveSizedXMLConfig(string filename)
        {
            if (filename.Length > 0)
            {
                string sFile = SetSizeName(filename);
                return SaveXMLConfig(sFile);
            }

            return false;
        }

        private bool LoadSizedProfileXMLConfig()
        {
            if (!Information.IsNothing(m_oProfile) && Conversions.ToInteger(m_oProfile.HasData) > 0)
            {
                string sConfig = m_oProfile.GetValue("Genie/Profile/Layout", "FileName", string.Empty);
                return LoadSizedXMLConfig(sConfig);
            }

            return false;
        }

        private string SetSizeName(string filepath)
        {
            int I = filepath.LastIndexOf('.');
            var sb = new StringBuilder();
            if (I > -1)
            {
                sb.Append(filepath.Substring(0, I));
                sb.Append(Width);
                sb.Append("x");
                sb.Append(Height);
                sb.Append(filepath.Substring(I));
            }

            return sb.ToString();
        }

        private void SaveSizedDefaultLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SaveSizedXMLConfig(m_sConfigFile))
            {
                if (m_oOutputMain.Visible)
                {
                    string argsText = "Layout Saved: " + SetSizeName(m_sConfigFile) + System.Environment.NewLine;
                    var argoColor = Color.WhiteSmoke;
                    var argoBgColor = Color.Transparent;
                    Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                    string argsTargetWindow = "";
                    AddText(argsText, argoColor, argoBgColor, oTargetWindow: argoTargetWindow, sTargetWindow: argsTargetWindow);
                }
            }
        }
    }
}
