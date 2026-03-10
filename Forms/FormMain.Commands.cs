using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using GenieClient.Genie;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace GenieClient
{
    // Miscellaneous command handlers, menu click handlers, and toolbar events.
    public partial class FormMain
    {
        private void Command_StatusBar(string sText, int iIndex)
        {
            try
            {
                ToolStripStatusLabel oLabel = null;
                switch (iIndex)
                {
                    case 1:
                        {
                            oLabel = ToolStripStatusLabel1;
                            break;
                        }

                    case 2:
                        {
                            oLabel = ToolStripStatusLabel2;
                            break;
                        }

                    case 3:
                        {
                            oLabel = ToolStripStatusLabel3;
                            break;
                        }

                    case 4:
                        {
                            oLabel = ToolStripStatusLabel4;
                            break;
                        }

                    case 5:
                        {
                            oLabel = ToolStripStatusLabel5;
                            break;
                        }

                    case 6:
                        {
                            oLabel = ToolStripStatusLabel6;
                            break;
                        }

                    case 7:
                        {
                            oLabel = ToolStripStatusLabel7;
                            break;
                        }

                    case 8:
                        {
                            oLabel = ToolStripStatusLabel8;
                            break;
                        }

                    case 9:
                        {
                            oLabel = ToolStripStatusLabel9;
                            break;
                        }

                    case 10:
                        {
                            oLabel = ToolStripStatusLabel10;
                            break;
                        }

                    default:
                        {
                            oLabel = ToolStripStatusLabel1;
                            break;
                        }
                }

                SafeSetStatusBar(sText, oLabel);
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("StatusBar", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }


        private void ConfigurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            My.MyProject.Forms.FormConfig.MdiParent = this;
            My.MyProject.Forms.FormConfig.Show();
            My.MyProject.Forms.FormConfig.BringToFront();
        }

        private void OpenGenieDiscordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utility.OpenBrowser("https://discord.gg/MtmzE2w");
        }

        private void OpenGenieWikiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utility.OpenBrowser("https://github.com/GenieClient/Genie4/wiki");
        }

        private void OpenGenieGithubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utility.OpenBrowser("https://github.com/GenieClient/Genie4/");
        }



        private void MagicPanelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetMagicPanels(MagicPanelsToolStripMenuItem.Checked);
        }

        private void SetMagicPanels(bool bVisible)
        {
            ComponentBarsMana.Visible = bVisible;
            Castbar.Visible = bVisible;
            LabelSpell.Visible = bVisible;
            LabelSpellC.Visible = bVisible;
            if (bVisible == true)
            {
                TableLayoutPanelBars.ColumnCount = 5;
                TableLayoutPanelFlow.ColumnCount = 7;
            }
            else
            {
                TableLayoutPanelBars.ColumnCount = 4;
                TableLayoutPanelFlow.ColumnCount = 5;
            }
        }

        private void ScriptExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Command_ShowScriptExplorer();
        }

        private void Command_ShowScriptExplorer()
        {
            if (My.MyProject.Forms.ScriptExplorer.Visible == false)
            {
                My.MyProject.Forms.ScriptExplorer.MdiParent = this;
                My.MyProject.Forms.ScriptExplorer.Globals = m_oGlobals;
                My.MyProject.Forms.ScriptExplorer.EventRunScript += m_oScriptManager.RunScript;
                My.MyProject.Forms.ScriptExplorer.Show();
            }

            My.MyProject.Forms.ScriptExplorer.BringToFront();
        }

        private void AutoLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_oGlobals.Config.bAutoLog = AutoLogToolStripMenuItem.Checked;
        }

        private void PluginsEnabledToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_oGlobals.PluginsEnabled = PluginsEnabledToolStripMenuItem.Checked;
        }


        private void IgnoresEnabledToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_oGlobals.Config.bGagsEnabled = IgnoresEnabledToolStripMenuItem.Checked;
        }


        private void OpenLogInEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (File.Exists(Conversions.ToString(Conversions.ToString(LocalDirectory.Path + @"\Logs\" + m_oGlobals.VariableList["charactername"]) + m_oGlobals.VariableList["game"] + "_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log")))
            {
                Interaction.Shell(Conversions.ToString(Conversions.ToString("\"" + m_oGlobals.Config.sEditor + "\" \"" + LocalDirectory.Path + @"\Logs\" + m_oGlobals.VariableList["charactername"]) + m_oGlobals.VariableList["game"] + "_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log\""), AppWinStyle.NormalFocus, false);
            }
            else
            {
                Interaction.MsgBox("No active log found.");
            }
        }

        private void MuteSoundsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_oGlobals.Config.bPlaySounds = !MuteSoundsToolStripMenuItem.Checked;
        }

        private void Command_FlashWindow()
        {
            SafeFlashWindow();
        }

        private void Command_EventMapperCommand(string cmd)
        {
            if (m_oGlobals.Config.bAutoMapper)
            {
                try
                {
                    m_oAutoMapper.ParseCommand(cmd);
                }
                catch (Exception ex)
                {
                    ShowDialogAutoMapperException("ParseCommand", ex);
                }
            }
            else
            {
                string argsText = "Mapper is currently disabled. Turn it back on in menu under [File/AutoMapper Enabled]" + System.Environment.NewLine;
                Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                AddText(argsText, oTargetWindow: argoTargetWindow);
            }
        }

        public delegate void FlashWindowDelegate();

        private void SafeFlashWindow()
        {
            if (InvokeRequired == true)
            {
                Invoke(new FlashWindowDelegate(FlashWindow));
            }
            else
            {
                FlashWindow();
            }
        }

        private void FlashWindow()
        {
            NativeMethods.FlashWindow(Handle, true);
        }

        private void ShowWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_oGlobals.Config.bAutoMapper)
            {
                m_oAutoMapper.Show();
            }
            else
            {
                string argsText = "Mapper is currently disabled. Turn it back on in menu under [File/AutoMapper Enabled]" + System.Environment.NewLine;
                Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                AddText(argsText, oTargetWindow: argoTargetWindow);
            }
        }

        private void ToolStripButtons_VisibleChanged(object sender, EventArgs e)
        {
            ToolStripButtons.Items.Clear();
            if (ToolStripButtons.Visible)
            {
                foreach (Script oScript in m_oScriptManager.ScriptList)
                {
                    if (!Information.IsNothing(oScript)) // Add it before running so put #parse and such works.
                    {
                        AddScriptToToolStrip(oScript);
                    }
                }
            }
        }

        private void TimerBgWorker_Tick(object sender, EventArgs e)
        {
            m_oGameLoop.Tick();

            if (m_oGlobals.Config.bShowSpellTimer == true && m_oGlobals.SpellTimeStart != DateTime.MinValue)
            {
                SafeSetStatusBarLabels();
            }

            SafeAddScripts();
            SafeRemoveExitedScripts();

            if (m_oScriptManager.ScriptListUpdated)
            {
                m_oScriptManager.SetScriptListVariable();
                TriggerVariableChanged("scriptlist");
            }
        }

        private void AutoMapperEnabledToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AutoMapperEnabledToolStripMenuItem.Checked = !AutoMapperEnabledToolStripMenuItem.Checked;
            m_oGlobals.Config.bAutoMapper = AutoMapperEnabledToolStripMenuItem.Checked;
        }

        // Connect Using Profile
        private void ConnectToolStripMenuItemConnectDialog_Click(object sender, EventArgs e)
        {
            My.MyProject.Forms.DialogProfileConnect.ConfigDir = m_oGlobals.Config.ConfigDir;
            My.MyProject.Forms.DialogProfileConnect.ClassicConnect = m_oGlobals.Config.bClassicConnect;
            if (My.MyProject.Forms.DialogProfileConnect.ShowDialog(this) == DialogResult.OK)
            {
                m_sCurrentProfileFile = string.Empty;
                LoadProfile(My.MyProject.Forms.DialogProfileConnect.ProfileName + ".xml", true);
            }
        }


        private void FormSkin_LinkClicked(string link, LinkClickedEventArgs e)
        {
            if (link.IndexOf("#") > -1)
            {
                string sLink = link.Substring(link.IndexOf("#") + 1);
                TextBoxInput_SendText(m_oGlobals.ParseGlobalVars(sLink));
                TextBoxInput.Focus();
            }
            if (link.StartsWith("http://") || link.StartsWith("https://"))
            {
                if (m_oGlobals.Config.bWebLinkSafety)
                {
                    link = "https://www.play.net/bounce/redirect.asp?URL=" + link;
                }
                Utility.OpenBrowser(link);
            }
        }


        private void OpenUserDataDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Interaction.Shell("explorer.exe " + LocalDirectory.Path, AppWinStyle.NormalFocus, false);
        }


        private void Command_RawToggle(string sToggle)
        {
            if (string.IsNullOrEmpty(sToggle))
            {
                m_oGame.ShowRawOutput = !m_oGame.ShowRawOutput;
            }
            else if ((sToggle.ToLower().Trim() ?? "") == "off")
            {
                m_oGame.ShowRawOutput = false;
            }
            else if ((sToggle.ToLower().Trim() ?? "") == "on")
            {
                m_oGame.ShowRawOutput = true;
            }

            string argsText = "Show Xml Output = " + m_oGame.ShowRawOutput.ToString() + System.Environment.NewLine;
            Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
            AddText(argsText, oTargetWindow: argoTargetWindow);
        }

        private void Command_ChangeIcon(string sPath)
        {
            SafeChangeIcon(sPath);
        }

        private void ChangeIcon(string sPath)
        {
            if (!sPath.Contains(@"\"))
            {
                sPath = Path.Combine(LocalDirectory.Path, sPath);
            }

            if (File.Exists(sPath))
            {
                Icon = new Icon(sPath);
            }
        }

        public delegate void ChangeIconDelegate(string sPath);

        public void SafeChangeIcon(string sPath)
        {
            if (InvokeRequired == true)
            {
                var parameters = new[] { sPath };
                Invoke(new ChangeIconDelegate(ChangeIcon), parameters);
            }
            else
            {
                ChangeIcon(sPath);
            }
        }

        private void playnetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utility.OpenBrowser("http://play.net");
        }

        private void elanthipediaToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Utility.OpenBrowser("http://elanthipedia.play.net");
        }

        private void dRServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utility.OpenBrowser("http://drservice.info");
        }

        private void lichDiscordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utility.OpenBrowser("https://discord.gg/uxZWxuX");
        }

        private void isharonsGenieSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utility.OpenBrowser("http://www.elanthia.org/GenieSettings/");
        }

        private void autoUpdateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_oGlobals.Config.AutoUpdate = !m_oGlobals.Config.AutoUpdate;
            autoUpdateToolStripMenuItem.Checked = m_oGlobals.Config.AutoUpdate;
        }

        private async void checkUpdatesOnStartupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_oGlobals.Config.CheckForUpdates = !m_oGlobals.Config.CheckForUpdates;
            checkUpdatesOnStartupToolStripMenuItem.Checked = m_oGlobals.Config.CheckForUpdates;
        }

        private async void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await Task.Run(async () =>
            {
                if (Updater.ClientIsCurrent)
                {
                    AddText("You have the latest version of Genie.\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor());
                }
                else
                {
                    AddText("An Update is Available.\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                    DialogResult response = MessageBox.Show("An Update is Available. Would you like to update?", "Rub the Bottle?", MessageBoxButtons.YesNoCancel);
                    if (response == DialogResult.Yes)
                    {
                        if (m_oGame.IsConnectedToGame)
                        {
                            response = MessageBox.Show("Genie will close and this will disconnect you from the game.", "Close Genie?", MessageBoxButtons.YesNoCancel);
                            if (response == DialogResult.Yes)
                            {
                                AddText("Saving Config and Exiting Genie to Update.", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                                m_oGlobals.Config.Save();
                                if (await Updater.RunUpdate(m_oGlobals.Config.AutoUpdateLamp))
                                {
                                    m_oGame.Disconnect(true);
                                    System.Windows.Forms.Application.Exit();
                                }
                            }
                        }
                        else
                        {
                            AddText("Saving Config and Exiting Genie to Update.", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                            m_oGlobals.Config.Save();
                            if (await Updater.RunUpdate(m_oGlobals.Config.AutoUpdateLamp))
                            {
                                System.Windows.Forms.Application.Exit();
                            }

                        }
                    }
                }
            });
        }

        private async void forceUpdateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_oGame.IsConnectedToGame)
            {
                DialogResult response = MessageBox.Show("Genie will close and this will disconnect you from the game. Are you sure?", "Close Genie?", MessageBoxButtons.YesNoCancel);
                if (response == DialogResult.Yes)
                {
                    AddText("Saving Config and Exiting Genie to Update.\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                    m_oGlobals.Config.Save();
                    if (await Updater.ForceUpdate())
                    {
                        m_oGame.Disconnect(true);
                        System.Windows.Forms.Application.Exit();
                    }
                }
            }
            else
            {
                AddText("Saving Config and Exiting Genie to Update.\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                m_oGlobals.Config.Save();
                if (await Updater.ForceUpdate())
                {
                    System.Windows.Forms.Application.Exit();
                }
            }
        }

        private async void loadTestClientToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult response = MessageBox.Show("This will force your client to the Test Release Version. Test is not considered stable and may introduce bugs. If Autoupdate is enabled it will be disabled. Checking for Updates will restore you to the Latest build. Are you sure?", "Load Test Client?", MessageBoxButtons.YesNoCancel);
            if (response == DialogResult.Yes)
            {
                if (m_oGame.IsConnectedToGame)
                {
                    response = MessageBox.Show("Genie will close and this will disconnect you from the game. Are you sure?", "Close Genie?", MessageBoxButtons.YesNoCancel);
                    if (response == DialogResult.Yes)
                    {
                        AddText("Disabling Autoupdate.\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                        m_oGlobals.Config.AutoUpdate = false;
                        m_oGlobals.Config.Save(m_oGlobals.Config.ConfigDir + @"\settings.cfg");
                        AddText("Saving Config and Exiting Genie to Update.\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                        m_oGlobals.Config.Save();
                        if (await Updater.UpdateToTest(m_oGlobals.Config.AutoUpdateLamp))
                        {
                            m_oGame.Disconnect(true);
                            System.Windows.Forms.Application.Exit();
                        }
                    }
                }
                else
                {
                    AddText("Disabling Autoupdate.\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                    m_oGlobals.Config.AutoUpdate = false;
                    m_oGlobals.Config.Save(m_oGlobals.Config.ConfigDir + @"\settings.cfg");
                    AddText("Saving Config and Exiting Genie to Update.\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                    m_oGlobals.Config.Save();
                    if (await Updater.UpdateToTest(m_oGlobals.Config.AutoUpdateLamp))
                    {
                        System.Windows.Forms.Application.Exit();
                    }
                }
            }
        }

        private async void updateMapsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult response = MessageBox.Show("This may take a moment. Update Maps?", "Update Maps?", MessageBoxButtons.YesNoCancel);
            if (response == DialogResult.Yes)
            {
                await Task.Run(async () =>
                {
                    AddText($"Saving Config and Updating Maps in {m_oGlobals.Config.MapDir}\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                    m_oGlobals.Config.Save();
                    if (await Updater.UpdateMaps(m_oGlobals.Config.MapDir, m_oGlobals.Config.AutoUpdateLamp))
                    {
                        AddText("Maps Updated.\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                    }
                    else
                    {
                        AddText("Something went wrong.\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                    }
                });
            }
        }

        private async void updatePluginsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult response = MessageBox.Show("This may take a moment. Update Plugins?\r\nNote: This will only update plugins from the Genie 4 Plugins folder..", "Update Plugins?", MessageBoxButtons.YesNoCancel);
            if (response == DialogResult.Yes)
            {
                await Task.Run(async () =>
                {
                    AddText($"Saving Config and Updating Plugins in {m_oGlobals.Config.PluginDir}\r\nRepo{m_oGlobals.Config.PluginRepo}", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                    m_oGlobals.Config.Save();
                    if (await Updater.UpdatePlugins(m_oGlobals.Config.PluginDir, m_oGlobals.Config.AutoUpdateLamp))
                    {
                        AddText("Plugins Updated.\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                        FormPlugin_ReloadPlugins();
                    }
                    else
                    {
                        AddText("Something went wrong.\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                    }
                });
            }
        }

        private async void updateScriptsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!m_oGlobals.Config.ScriptRepo.EndsWith(".zip"))
            {
                MessageBox.Show("You do not have a repository configured properly." + Environment.NewLine + "Please use \"#config scriptrepo {address of a zip file}\" to configure." + Environment.NewLine + "The URI must be a zip file.");
                return;
            }
            DialogResult response = MessageBox.Show($"This may take a moment. Update Scripts?\r\nRepo: {m_oGlobals.Config.ScriptRepo}", "Update Scripts?", MessageBoxButtons.YesNoCancel);
            if (response == DialogResult.Yes)
            {
                await Task.Run(async () =>
                {
                    AddText($"Updating Scripts in {m_oGlobals.Config.ScriptDir}\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                    if (await Updater.UpdateScripts(m_oGlobals.Config.ScriptDir, m_oGlobals.Config.ScriptRepo, m_oGlobals.Config.AutoUpdateLamp))
                    {
                        AddText("Scripts Updated.\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                    }
                    else
                    {
                        AddText("Something went wrong.\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                    }
                });
            }
        }

        private void genieToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Interaction.Shell("explorer.exe " + LocalDirectory.Path, AppWinStyle.NormalFocus, false);
        }

        private void mapsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Interaction.Shell("explorer.exe " + m_oGlobals.Config.MapDir, AppWinStyle.NormalFocus, false);
        }

        private void pluginsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Interaction.Shell("explorer.exe " + m_oGlobals.Config.PluginDir, AppWinStyle.NormalFocus, false);
        }

        private void scriptsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Interaction.Shell("explorer.exe " + m_oGlobals.Config.ScriptDir, AppWinStyle.NormalFocus, false);
        }

        private void logsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Interaction.Shell("explorer.exe " + m_oGlobals.Config.sLogDir, AppWinStyle.NormalFocus, false);
        }

        private void artToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Interaction.Shell("explorer.exe " + m_oGlobals.Config.ArtDir, AppWinStyle.NormalFocus, false);
        }

        private void toolStripMenuItemClassicConnect_Click(global::System.Object sender, global::System.EventArgs e)
        {
            m_oGlobals.Config.bClassicConnect = ClassicConnectToolStripMenuItem.Checked;
        }

        private void autoUpdateLampToolStripMenuItem_Click(global::System.Object sender, global::System.EventArgs e)
        {
            m_oGlobals.Config.AutoUpdateLamp = !m_oGlobals.Config.AutoUpdateLamp;
            autoUpdateLampToolStripMenuItem.Checked = m_oGlobals.Config.AutoUpdateLamp;
        }

        private void _ImagesEnabledToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ImagesEnabledToolStripMenuItem.Checked = !m_oGlobals.Config.bShowImages;
            m_oGlobals.Config.bShowImages = _ImagesEnabledToolStripMenuItem.Checked;
        }

        private async void UpdateImagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!m_oGlobals.Config.ScriptRepo.EndsWith(".zip"))
            {
                MessageBox.Show("You do not have a repository configured properly." + Environment.NewLine + "Please use \"#config artrepo {address of a zip file}\" to configure." + Environment.NewLine + "The URI must be a zip file.");
                return;
            }
            DialogResult response = MessageBox.Show($"This may take a moment. Update Images?\r\nRepo: {m_oGlobals.Config.ArtRepo}", "Update Images?", MessageBoxButtons.YesNoCancel);
            if (response == DialogResult.Yes)
            {
                await Task.Run(async () =>
                {
                    AddText($"Saving Config and Updating Art in {m_oGlobals.Config.ArtDir}\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                    m_oGlobals.Config.Save();
                    if (await Updater.UpdateArt(m_oGlobals.Config.ArtDir, m_oGlobals.Config.ArtRepo, m_oGlobals.Config.AutoUpdateLamp))
                    {
                        AddText("Art Updated.\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                    }
                    else
                    {
                        AddText("Something went wrong.\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                    }
                });
            }
        }

        private void ResizeInputBar(object sender, EventArgs e)
        {
            _TextBoxInput.Left = m_oOutputMain.Left;
            _TextBoxInput.Width = m_oOutputMain.Width;
        }

        private void alignInputToGameWindowToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            m_oGlobals.Config.SizeInputToGame = alignInputToGameWindowToolStripMenuItem.Checked;
            if (alignInputToGameWindowToolStripMenuItem.Checked)
            {
                _TextBoxInput.Dock = DockStyle.None;
                m_oOutputMain.RichTextBoxOutput.Resize += ResizeInputBar;
                ResizeInputBar(sender, e);
            }
            else
            {
                _TextBoxInput.Dock = DockStyle.Fill;
                m_oOutputMain.RichTextBoxOutput.Resize -= ResizeInputBar;
            }
        }

        private void alwaysOnTopToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            m_oGlobals.Config.AlwaysOnTop = alwaysOnTopToolStripMenuItem.Checked;
            this.TopMost = m_oGlobals.Config.AlwaysOnTop;
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MapperSettings.Visible = true;
            MapperSettings.BringToFront();
        }

        private void updateScriptsWithMapsToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            m_oGlobals.Config.UpdateMapperScripts = updateScriptsWithMapsToolStripMenuItem.Checked;
        }
    }
}
