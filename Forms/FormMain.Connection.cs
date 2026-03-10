using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GenieClient.Genie;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace GenieClient
{
    // Connection management, reconnect logic, and profile loading/saving.
    public partial class FormMain
    {
        private void ConnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (My.MyProject.Forms.DialogConnect.ShowDialog(this) == DialogResult.OK)
            {
                string argsAccountName = My.MyProject.Forms.DialogConnect.AccountName;
                string argsPassword = My.MyProject.Forms.DialogConnect.Password;
                string argsCharacter = My.MyProject.Forms.DialogConnect.Character;
                string argsGame = My.MyProject.Forms.DialogConnect.Game;
                ConnectToGame(argsAccountName, argsPassword, argsCharacter, argsGame);
                SavePasswordToolStripMenuItem.Checked = My.MyProject.Forms.DialogConnect.CheckBoxSavePassword.Checked;
            }
        }

        private static DateTime m_oNullTime = DateTime.Parse("0001-01-01");
        private bool m_CommandSent = false;

        public void CheckReconnect()
        {
            if (!m_oGlobals.Config.bReconnect)
                return;
            if (Information.IsNothing(m_oGame))
                return;
            if (m_oGame.ReconnectTime == m_oNullTime)
                return;
            if (m_CommandSent == false)
            {
                string argsText = "Reconnect aborted! (No user input since last connect.)" + System.Environment.NewLine;
                PrintError(argsText);
                if (m_oGame.IsConnected)
                {
                    m_oGame.Disconnect();
                }

                m_oGame.ReconnectTime = default;
                return;
            }

            if (m_oGame.ReconnectTime < DateTime.Now) // Connect now
            {
                m_oGame.ReconnectTime = default;
                m_oGame.ConnectAttempts += 1;
                if (m_oGlobals.Config.bReconnectWhenDead == false)
                {
                    if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(m_oGlobals.VariableList["dead"], 1, false)))
                    {
                        string argsText1 = "Reconnect aborted! (You seem to be dead.)" + System.Environment.NewLine;
                        PrintError(argsText1);
                        return;
                    }
                }

                ReconnectToGame();
            }
        }

        private void ReconnectToGame()
        {
            try
            {
                if (m_oGame.AccountName.Length > 0)
                {
                    m_oGlobals.GenieKey = m_sGenieKey;
                    m_oGlobals.GenieAccount = m_oGame.AccountName;
                    string argsAccountName = m_oGame.AccountName;
                    string argsPassword = m_oGame.AccountPassword;
                    string argsCharacter = m_oGame.AccountCharacter;
                    string argsGame = m_oGame.AccountGame;
                    m_oGame.Connect(m_sGenieKey, argsAccountName, argsPassword, argsCharacter, argsGame);
                }
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("ReconnectToGame", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        private void ConnectToGame(string sAccountName, string sPassword, string sCharacter, string sGame, bool isLich = false)
        {
            m_oGame.IsLich = isLich;
            try
            {
                if (sPassword.Length > 0)
                {
                    m_oGame.Connect(m_sGenieKey, sAccountName, sPassword, sCharacter, sGame);
                }
                else
                {
                    // Load profile
                    m_sCurrentProfileFile = string.Empty;
                    SafeLoadProfile(sAccountName.Trim() + ".xml", true);
                }
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("ConnectToGame", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        private void DisconnectFromGame()
        {
            m_oGame.Disconnect();
        }

        private void DisconnectAndExit()
        {
            m_oGame.Disconnect(true);
        }

        private string m_sCurrentProfileFile = string.Empty;
        private Genie.XMLConfig m_oProfile = new Genie.XMLConfig();

        public delegate void LoadProfileDelegate(string FileName, bool DoConnect);

        private void SafeLoadProfile(string FileName, bool DoConnect)
        {
            if (InvokeRequired == true)
            {
                var parameters = new object[] { FileName, DoConnect };
                Invoke(new LoadProfileDelegate(LoadProfile), parameters);
            }
            else
            {
                LoadProfile(FileName, DoConnect);
            }
        }

        private void Command_LoadProfile()
        {
            if (m_oGlobals.VariableList["charactername"].ToString().Length > 0 & m_oGlobals.VariableList["game"].ToString().Length > 0)
            {
                string sFileName = m_oGlobals.VariableList["charactername"].ToString() + m_oGlobals.VariableList["game"].ToString() + ".xml";
                LoadProfile(sFileName);
            }
            else
            {
                string argsText = "Unknown character or game name. Load profile failed." + System.Environment.NewLine;
                Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                AddText(argsText, oTargetWindow: argoTargetWindow);
            }
        }

        public string LoadedLayout
        {
            get
            {
                if (m_oConfig.ConfigFile.Length > 0)
                {
                    return m_oConfig.ConfigFile;
                }
                else
                {
                    return m_sConfigFile;
                }
            }
        }

        private void LoadProfile(string FileName, bool DoConnect = false)
        {
            string ShortName = FileName;
            if (FileName.IndexOf(@"\") == -1)
            {
                FileName = m_oGlobals.Config.ConfigDir + @"\Profiles\" + FileName;
            }

            // Only load if profile changed
            // If m_sCurrentProfile <> FileName Then

            if (m_oProfile.LoadFile(FileName) == true)
            {
                string argsText = "Profile \"" + ShortName + "\" loaded." + System.Environment.NewLine;
                Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                AddText(argsText, oTargetWindow: argoTargetWindow);
                string sConfig = m_oProfile.GetValue("Genie/Profile/Layout", "FileName", string.Empty);
                if (sConfig.Length > 0 & (sConfig ?? "") != (m_oConfig.ConfigFile ?? ""))
                {
                    LoadLayout(sConfig);
                }

                string sCharacter = m_oProfile.GetValue("Genie/Profile", "Character", string.Empty);
                string sGame = m_oProfile.GetValue("Genie/Profile", "Game", string.Empty);
                if (sCharacter.Length > 0)
                    m_oGame.AccountCharacter = sCharacter;
                if (sGame.Length > 0)
                    m_oGame.AccountGame = sGame;
                string sProfile = string.Empty;
                sProfile = FileName.Substring(FileName.LastIndexOf(@"\") + 1).Replace(".xml", "");
                m_oGlobals.Config.sConfigDirProfile = m_oGlobals.Config.ConfigDir + @"\Profiles\" + sProfile;
                LoadProfileSettings();
                string sAccount = m_oProfile.GetValue("Genie/Profile", "Account", string.Empty);

                string sPassword = m_oProfile.GetValue("Genie/Profile", "Password", string.Empty);
                if (sPassword.Length > 0)
                {
                    string argsPassword = "G3" + sAccount.ToUpper();
                    sPassword = Utility.DecryptString(argsPassword, sPassword);
                    SavePasswordToolStripMenuItem.Checked = true;
                }
                else
                {
                    SavePasswordToolStripMenuItem.Checked = false;
                }

                if (DoConnect == true)
                {
                    if (sAccount.Length > 0 & sPassword.Length > 0)
                    {
                        ConnectToGame(sAccount, sPassword, sCharacter, sGame, m_oGame.IsLich);
                    }
                    else
                    {
                        My.MyProject.Forms.DialogConnect.AccountName = sAccount;
                        My.MyProject.Forms.DialogConnect.Password = "";
                        My.MyProject.Forms.DialogConnect.Character = sCharacter;
                        My.MyProject.Forms.DialogConnect.Game = sGame;
                        if (My.MyProject.Forms.DialogConnect.ShowDialog(this) == DialogResult.OK)
                        {
                            string argsAccountName = My.MyProject.Forms.DialogConnect.AccountName;
                            string argsPassword1 = My.MyProject.Forms.DialogConnect.Password;
                            string argsCharacter = My.MyProject.Forms.DialogConnect.Character;
                            string argsGame = My.MyProject.Forms.DialogConnect.Game;
                            ConnectToGame(argsAccountName, argsPassword1, argsCharacter, argsGame);
                            SavePasswordToolStripMenuItem.Checked = My.MyProject.Forms.DialogConnect.CheckBoxSavePassword.Checked;
                        }
                    }
                }
                else
                {
                    m_oGlobals.VariableList["account"] = sAccount;
                }

                m_sCurrentProfileFile = FileName;
            }
            else if (DoConnect)
            {
                // Connect to non existing profile?
                string argsText1 = "Profile \"" + FileName + "\" not found." + System.Environment.NewLine;
                Genie.Game.WindowTarget argoTargetWindow1 = Genie.Game.WindowTarget.Main;
                AddText(argsText1, oTargetWindow: argoTargetWindow1);
            }
            // End If

        }

        private void LoadProfileSettings(bool echo = true)
        {
            if (Utility.CreateDirectory(m_oGlobals.Config.ConfigProfileDir))
            {
                if (echo)
                {
                    string argsText = "Loading Variables...";
                    var argoColor = Color.WhiteSmoke;
                    var argoBgColor = Color.Transparent;
                    Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                    string argsTargetWindow = "";
                    AddText(argsText, argoColor, argoBgColor, oTargetWindow: argoTargetWindow, sTargetWindow: argsTargetWindow);
                }

                m_oGlobals.VariableList.ClearUser();
                m_oGlobals.VariableList.Load(m_oGlobals.Config.ConfigProfileDir + @"\variables.cfg");
                if (echo)
                {
                    string argsText1 = "OK" + System.Environment.NewLine;
                    var argoColor1 = Color.WhiteSmoke;
                    var argoBgColor1 = Color.Transparent;
                    Genie.Game.WindowTarget argoTargetWindow1 = Genie.Game.WindowTarget.Main;
                    string argsTargetWindow1 = "";
                    AddText(argsText1, argoColor1, argoBgColor1, oTargetWindow: argoTargetWindow1, sTargetWindow: argsTargetWindow1);
                }

                if (echo)
                {
                    string argsText2 = "Loading Macros...";
                    var argoColor2 = Color.WhiteSmoke;
                    var argoBgColor2 = Color.Transparent;
                    Genie.Game.WindowTarget argoTargetWindow2 = Genie.Game.WindowTarget.Main;
                    string argsTargetWindow2 = "";
                    AddText(argsText2, argoColor2, argoBgColor2, oTargetWindow: argoTargetWindow2, sTargetWindow: argsTargetWindow2);
                }

                m_oGlobals.MacroList.Clear();
                m_oGlobals.MacroList.Load(m_oGlobals.Config.ConfigDir + @"\macros.cfg"); // Load default macros
                m_oGlobals.MacroList.Load(m_oGlobals.Config.ConfigProfileDir + @"\macros.cfg");
                if (echo)
                {
                    string argsText3 = "OK" + System.Environment.NewLine;
                    var argoColor3 = Color.WhiteSmoke;
                    var argoBgColor3 = Color.Transparent;
                    Genie.Game.WindowTarget argoTargetWindow3 = Genie.Game.WindowTarget.Main;
                    string argsTargetWindow3 = "";
                    AddText(argsText3, argoColor3, argoBgColor3, oTargetWindow: argoTargetWindow3, sTargetWindow: argsTargetWindow3);
                }

                if (echo)
                {
                    string argsText4 = "Loading Aliases...";
                    var argoColor4 = Color.WhiteSmoke;
                    var argoBgColor4 = Color.Transparent;
                    Genie.Game.WindowTarget argoTargetWindow4 = Genie.Game.WindowTarget.Main;
                    string argsTargetWindow4 = "";
                    AddText(argsText4, argoColor4, argoBgColor4, oTargetWindow: argoTargetWindow4, sTargetWindow: argsTargetWindow4);
                }

                m_oGlobals.AliasList.Clear();
                m_oGlobals.AliasList.Load(m_oGlobals.Config.ConfigDir + @"\aliases.cfg"); // Load default aliases
                m_oGlobals.AliasList.Load(m_oGlobals.Config.ConfigProfileDir + @"\aliases.cfg");
                if (echo)
                {
                    string argsText5 = "OK" + System.Environment.NewLine;
                    var argoColor5 = Color.WhiteSmoke;
                    var argoBgColor5 = Color.Transparent;
                    Genie.Game.WindowTarget argoTargetWindow5 = Genie.Game.WindowTarget.Main;
                    string argsTargetWindow5 = "";
                    AddText(argsText5, argoColor5, argoBgColor5, oTargetWindow: argoTargetWindow5, sTargetWindow: argsTargetWindow5);
                }

                if (echo)
                {
                    string argsText6 = "Loading Classes...";
                    var argoColor6 = Color.WhiteSmoke;
                    var argoBgColor6 = Color.Transparent;
                    Genie.Game.WindowTarget argoTargetWindow6 = Genie.Game.WindowTarget.Main;
                    string argsTargetWindow6 = "";
                    AddText(argsText6, argoColor6, argoBgColor6, oTargetWindow: argoTargetWindow6, sTargetWindow: argsTargetWindow6);
                }

                m_oGlobals.ClassList.Clear();
                m_oGlobals.ClassList.Load(m_oGlobals.Config.ConfigProfileDir + @"\classes.cfg");
                if (m_oGame.AccountCharacter.Length > 0)
                {
                    if (!m_oGlobals.ClassList.ContainsKey(m_oGame.AccountCharacter.ToLower()))
                    {
                        string argsValue = Conversions.ToString(true);
                        m_oGlobals.ClassList.Add(m_oGame.AccountCharacter.ToLower(), argsValue);
                    }
                }

                if (m_oGame.AccountGame.Length > 0)
                {
                    if (!m_oGlobals.ClassList.ContainsKey(m_oGame.AccountGame.ToLower()))
                    {
                        string argsValue1 = Conversions.ToString(true);
                        m_oGlobals.ClassList.Add(m_oGame.AccountGame.ToLower(), argsValue1);
                    }
                }

                if (echo)
                {
                    string argsText7 = "OK" + System.Environment.NewLine;
                    var argoColor7 = Color.WhiteSmoke;
                    var argoBgColor7 = Color.Transparent;
                    Genie.Game.WindowTarget argoTargetWindow7 = Genie.Game.WindowTarget.Main;
                    string argsTargetWindow7 = "";
                    AddText(argsText7, argoColor7, argoBgColor7, oTargetWindow: argoTargetWindow7, sTargetWindow: argsTargetWindow7);
                }
            }
        }

        private void Command_LaunchBrowser(string url)
        {
            Utility.OpenBrowser(url);
        }

        private string m_sCurrentProfileName = string.Empty;

        private void Command_SaveProfile()
        {
            if (m_sCurrentProfileName.Length > 0)
            {
                SaveProfile(m_sCurrentProfileName);
                string sProfile = m_sCurrentProfileName.Substring(m_sCurrentProfileName.LastIndexOf(@"\") + 1).Replace(".xml", "");
                m_oGlobals.Config.sConfigDirProfile = m_oGlobals.Config.ConfigDir + @"\Profiles\" + sProfile;
                LoadProfileSettings(false);
            }
            else
            {
                string argsText = "Unknown character or game name. Save profile failed." + System.Environment.NewLine;
                Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                AddText(argsText, oTargetWindow: argoTargetWindow);
            }
        }

        private bool SaveProfile(string FileName = null)
        {
            if (m_oProfile.GetValue("Genie/Profile", "Account", string.Empty).Length == 0)
            {
                m_oProfile.LoadXml("<Genie><Profile></Profile></Genie>");
            }

            m_oProfile.SetValue("Genie/Profile", "Account", m_oGame.AccountName);
            m_oGlobals.VariableList["account"] = m_oGame.AccountName;
            if (SavePasswordToolStripMenuItem.Checked == true)
            {
                string argsPassword = "G3" + m_oGame.AccountName.ToUpper();
                string argsText = m_oGame.AccountPassword;
                m_oProfile.SetValue("Genie/Profile", "Password", Utility.EncryptString(argsPassword, argsText));
            }
            else
            {
                m_oProfile.SetValue("Genie/Profile", "Password", "");
            }

            m_oProfile.SetValue("Genie/Profile", "Character", m_oGame.AccountCharacter);
            m_oProfile.SetValue("Genie/Profile", "Game", m_oGame.AccountGame);
            string sLayout = m_oConfig.ConfigFile;
            if (sLayout.Contains(m_oGlobals.Config.ConfigDir))
            {
                sLayout = sLayout.Substring(sLayout.LastIndexOf(@"\") + 1);
            }

            m_oProfile.SetValue("Genie/Profile/Layout", "FileName", sLayout);
            m_sCurrentProfileFile = FileName;
            if (Information.IsNothing(FileName))
            {
                return m_oProfile.SaveToFile();
            }
            else
            {
                if (FileName.IndexOf(@"\") == -1)
                {
                    FileName = m_oGlobals.Config.ConfigDir + @"\Profiles\" + FileName;
                }

                return m_oProfile.SaveToFile(FileName);
            }
        }

        private void LoadProfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialogProfile.InitialDirectory = LocalDirectory.Path + @"\Config\Profiles";
            if (OpenFileDialogProfile.ShowDialog() == DialogResult.OK)
            {
                m_sCurrentProfileName = OpenFileDialogProfile.FileName;
                LoadProfile(m_sCurrentProfileName);
            }
        }

        private void SaveProfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_sCurrentProfileName.Length > 0)
            {
                Command_SaveProfile();
            }
            else
            {
                Interaction.MsgBox("Unknown character and game. Could not save profile.");
            }
        }

        private void SavePasswordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SavePasswordToolStripMenuItem.Checked)
            {
                Interaction.MsgBox("Using this option will save your password so that anyone with access to your computer and/or files may connect to your character.", MsgBoxStyle.Exclamation, "CAUTION!");
            }
        }
    }
}
