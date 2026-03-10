using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Accessibility;
using GenieClient.Forms;
using GenieClient.Genie;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace GenieClient
{
    // Imports Microsoft.Win32

    public partial class FormMain
    {
        public FormMain()
        {

            m_oGlobals = new Genie.Globals();
            m_oGame = new Genie.Game(ref _m_oGlobals);
            m_oCommand = new Genie.Command(ref _m_oGlobals);
            m_oScriptManager = new ScriptManager(_m_oGlobals, _m_oCommand);
            m_oScriptManager.EventScriptAdded += ScriptManager_ScriptAdded;
            m_oScriptManager.EventScriptPrintText += Script_EventPrintText;
            m_oScriptManager.EventScriptPrintError += Script_EventPrintError;
            m_oScriptManager.EventScriptSendText += Script_EventSendText;
            m_oScriptManager.EventScriptDebugChanged += Script_EventDebugChanged;
            m_oScriptManager.EventScriptStatusChanged += Script_EventStatusChanged;

            // Wire up ScriptManager events that were deferred during m_oGame/m_oCommand property setters
            // (m_oScriptManager was null when those setters ran).
            // Defensive -= before += to prevent double-subscription if setters ever re-run.
            _m_oGame.EventTriggerPrompt -= m_oScriptManager.TriggerPromptForScripts;
            _m_oGame.EventTriggerPrompt += m_oScriptManager.TriggerPromptForScripts;
            _m_oGame.EventTriggerMove -= m_oScriptManager.TriggerMoveForScripts;
            _m_oGame.EventTriggerMove += m_oScriptManager.TriggerMoveForScripts;
            _m_oCommand.EventRunScript -= m_oScriptManager.RunScript;
            _m_oCommand.EventRunScript += m_oScriptManager.RunScript;
            _m_oCommand.EventScriptAbort -= m_oScriptManager.ScriptAbort;
            _m_oCommand.EventScriptAbort += m_oScriptManager.ScriptAbort;
            _m_oCommand.EventScriptPause -= m_oScriptManager.ScriptPause;
            _m_oCommand.EventScriptPause += m_oScriptManager.ScriptPause;
            _m_oCommand.EventScriptPauseOrResume -= m_oScriptManager.ScriptPauseOrResume;
            _m_oCommand.EventScriptPauseOrResume += m_oScriptManager.ScriptPauseOrResume;
            _m_oCommand.EventScriptReload -= m_oScriptManager.ScriptReload;
            _m_oCommand.EventScriptReload += m_oScriptManager.ScriptReload;
            _m_oCommand.EventScriptResume -= m_oScriptManager.ScriptResume;
            _m_oCommand.EventScriptResume += m_oScriptManager.ScriptResume;
            _m_oCommand.EventScriptDebug -= m_oScriptManager.ScriptDebugLevel;
            _m_oCommand.EventScriptDebug += m_oScriptManager.ScriptDebugLevel;

            m_oTriggerEngine = new TriggerEngine(_m_oGlobals, _m_oCommand, m_oScriptManager);
            m_oTriggerEngine.EventEchoText += ClassCommand_EchoText;
            m_oGameLoop = new GameLoop(_m_oGlobals, _m_oCommand, m_oScriptManager);
            m_oGameLoop.EventEndUpdate += GameLoop_EndUpdate;
            m_oAutoMapper = new Mapper.AutoMapper(ref _m_oGlobals);
            m_oAutoMapper.SetView(new Mapper.MapForm(_m_oGlobals));
            m_oOutputMain = new FormSkin("main", "Game", ref _m_oGlobals);
            m_oLegacyPluginHost = new LegacyPluginHost(this, ref _m_oGlobals);
            m_oPluginHost = new PluginHost(this, ref _m_oGlobals);
            m_PluginDialog = new FormPlugins(ref _m_oGlobals.PluginList);
            // This call is required by the Windows Form Designer.
            InitializeComponent();
            RecolorUI();
            MapperSettings = new FormMapperSettings(ref _m_oGlobals) { MdiParent = this };
            MapperSettings.EventVariableChanged += ClassCommand_EventVariableChanged;
            MapperSettings.EventClassChange += Command_EventClassChange;

            // Add any initialization after the InitializeComponent() call.
            LocalDirectory.CheckUserDirectory();
            bool bCustomConfigFile = false;
            var al = new ArrayList();
            al = Utility.ParseArgs(Interaction.Command());
            foreach (string cmd in al)
            {
                switch (cmd)
                {
                    case "-l":
                    case "-layout":
                        {
                            bCustomConfigFile = true;
                            break;
                        }

                    case "-n":
                    case "-noupdate":
                        {
                            m_bVersionUpdated = true;
                            break;
                        }

                    case "-d":
                    case "-debug":
                        {
                            m_bDebugPlugin = true;
                            break;
                        }

                    default:
                        {
                            if (bCustomConfigFile == true)
                            {
                                if (m_sConfigFile.Length == 0)
                                {
                                    m_sConfigFile = cmd;
                                    if (m_sConfigFile.ToLower().EndsWith(".layout") == false & m_sConfigFile.ToLower().EndsWith(".xml") == false)
                                    {
                                        m_sConfigFile += ".layout";
                                    }

                                    if (m_sConfigFile.Contains(@"\") == false)
                                    {
                                        m_sConfigFile = m_oGlobals.Config.ConfigDir + @"\Layout\" + m_sConfigFile;
                                    }

                                    bCustomConfigFile = false;
                                }
                            }

                            break;
                        }
                }
            }

            CreateGenieFolders();
            if (bCustomConfigFile == false)
            {
                m_sConfigFile = m_oGlobals.Config.ConfigDir + @"\Layout\" + "default.layout";

                // TEMP MOVE TEMP MOVE TEMP MOVE
                if (File.Exists(m_oGlobals.Config.ConfigDir + @"\Layout\" + "default.layout"))
                {
                }
                // 
                else if (File.Exists(m_oGlobals.Config.ConfigDir + @"\config.xml"))
                {
                    try
                    {
                        File.Move(m_oGlobals.Config.ConfigDir + @"\config.xml", m_sConfigFile);
                    }
#pragma warning disable CS0168
                    catch (Exception ex)
#pragma warning restore CS0168
                    {
                        Interaction.MsgBox("Error: Unable to move config.xml to default.layout");
                    }
                }
            }

            IconBar.Globals = m_oGlobals;
            m_oOutputMain.MdiParent = this;
            m_oOutputMain.IsMainWindow = true;
            m_oOutputMain.UserForm = false; // Not an editable window

            // Make sure decimal separator is always "." (For script compability and such)
            if ((Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator ?? "") != ".")
            {
                Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            }

            UpdateMainWindowTitle();
        }

        private void RecolorUI()
        {
            this._MenuStripMain.BackColor = m_oGlobals.PresetList["ui.menu"].BgColor.ToDrawingColor();
            this._MenuStripMain.ForeColor = m_oGlobals.PresetList["ui.menu"].FgColor.ToDrawingColor();
            this._MenuStripMain.Renderer = new GenieClient.Forms.Components.MenuRenderer(m_oGlobals.PresetList);

            this._ToolStripButtons.BackColor = m_oGlobals.PresetList["ui.menu"].BgColor.ToDrawingColor();
            this._ToolStripButtons.ForeColor = m_oGlobals.PresetList["ui.menu"].FgColor.ToDrawingColor();
            this._ToolStripButtons.Renderer = new GenieClient.Forms.Components.MenuRenderer(m_oGlobals.PresetList);

            this._TextBoxInput.BackColor = m_oGlobals.PresetList["ui.textbox"].BgColor.ToDrawingColor();
            this._TextBoxInput.ForeColor = m_oGlobals.PresetList["ui.textbox"].FgColor.ToDrawingColor();

            this._StatusStripMain.BackColor = m_oGlobals.PresetList["ui.status"].BgColor.ToDrawingColor();
            this._StatusStripMain.ForeColor = m_oGlobals.PresetList["ui.status"].FgColor.ToDrawingColor();

            foreach (ToolStripMenuItem menu in _MenuStripMain.Items)
            {
                foreach (ToolStripItem item in menu.DropDownItems)
                {
                    item.BackColor = m_oGlobals.PresetList["ui.menu"].BgColor.ToDrawingColor();
                    item.ForeColor = m_oGlobals.PresetList["ui.menu"].FgColor.ToDrawingColor();
                    if (string.IsNullOrWhiteSpace(item.Text))
                    {
                        item.AutoSize = false;
                        item.Height = item.Height / 2;
                    }
                }
            }
        }



        public async void UpdateOnStartup()
        {
            return; // Update system disabled until further notice
            await Task.Run(async () =>
            {
                if (m_oGlobals.Config.CheckForUpdates || m_oGlobals.Config.AutoUpdate)
                {
                    if (Updater.ClientIsCurrent)
                    {
                        AddText("You are running the latest version of Genie.\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                    }
                    else
                    {

                        AddText("An Update is Available.\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                        if (m_oGlobals.Config.AutoUpdate)
                        {
                            AddText("AutoUpdate is Enabled. Exiting and launching Updater.\r\n", m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor(), Genie.Game.WindowTarget.Main);
                            if (await Updater.RunUpdate(m_oGlobals.Config.AutoUpdateLamp))
                            {
                                System.Windows.Forms.Application.Exit();
                            }
                        }
                    }
                }
            });
        }

        public void DirectConnect(string[] parameters)
        {
            if (parameters.Length > 0)
            {
                string character = "";
                string game = "";
                string host = "";
                string key = "";
                int port = 0;
                if (parameters.Length == 1)
                {

                    if (Path.GetExtension(parameters[0]).ToUpper() == ".SAL")
                    {
                        string pathToSAL = parameters[0];
                        character = Path.GetFileNameWithoutExtension(pathToSAL).Split("(")[0].Split(" ")[0].Trim(); //in case the file was auto-renamed, split off everything before a peren and/or space;
                        using (StreamReader reader = new StreamReader(pathToSAL))
                        {
                            List<string> salEntries = new List<string>();
                            while (!reader.EndOfStream)
                            {
                                salEntries.Add(reader.ReadLine());
                            }
                            parameters = salEntries.ToArray();
                        }
                    }
                    else
                    {
                        parameters = parameters[0].Split(@"/", StringSplitOptions.RemoveEmptyEntries);
                    }
                }
                foreach (string parameter in parameters)
                {
                    if (parameter.Length <= 1) continue;

                    string param = parameter[0].ToString();
                    string value = parameter.Substring(1);
                    foreach (char delimiter in "|:;-~=")
                    {
                        if (parameter.Contains(delimiter))
                        {
                            value = parameter.Split(delimiter)[1];
                            param = parameter.Split(delimiter)[0];
                            break;
                        }
                    }

                    switch (param.ToUpper())
                    {
                        case "K": //key
                        case "KEY":
                            key = value;
                            break;
                        case "H": //host
                        case "HOST":
                        case "GAMEHOST":
                            host = value;
                            break;
                        case "P": //port
                        case "PORT":
                        case "GAMEPORT":
                            int.TryParse(value, out port);
                            break;
                        case "G": //instance code
                        case "GAME":
                        case "GAMECODE":
                            game = value;
                            break;
                        case "C": //character
                        case "CHARACTER":
                            character = value;
                            break;
                        default:
                            break;
                    }
                }


                if (string.IsNullOrWhiteSpace(game) ||
                    string.IsNullOrWhiteSpace(host) ||
                    string.IsNullOrWhiteSpace(character) ||
                    port <= 0)
                {
                    PrintError("Invalid Startup Parameters detected.");
                    return;
                }
                m_sCurrentProfileFile = character + game + ".xml";
                m_oGame.AccountCharacter = character;
                m_oGame.AccountGame = game;
                SafeLoadProfile(m_sCurrentProfileFile, false);
                if (string.IsNullOrEmpty(key)) m_oGame.DirectConnect(character, game, host, port);
                else m_oGame.DirectConnect(character, game, host, port, key);
            }
        }

        private Genie.Globals _m_oGlobals;

        public Genie.Globals m_oGlobals
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _m_oGlobals;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_m_oGlobals != null)
                {
                    GenieError.EventGenieError -= HandleGenieException;
                    _m_oGlobals.Config.ConfigChanged -= Config_ConfigChanged;
                    _m_oGlobals.ConfigChanged -= Config_ConfigChanged;
                }

                _m_oGlobals = value;
                if (_m_oGlobals != null)
                {
                    GenieError.EventGenieError += HandleGenieException;
                    _m_oGlobals.Config.ConfigChanged += Config_ConfigChanged;
                    _m_oGlobals.ConfigChanged += Config_ConfigChanged;

                }
            }
        }

        private Genie.Game _m_oGame;

        public Genie.Game m_oGame
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _m_oGame;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_m_oGame != null)
                {
                    _m_oGame.EventParseXML -= Plugin_ParsePluginXML;
                    _m_oGame.EventStreamWindow -= EventStreamWindow;
                    _m_oGame.EventVariableChanged -= ClassCommand_EventVariableChanged;
                    _m_oGame.EventPrintError -= PrintError;

                    // Clear Window
                    _m_oGame.EventClearWindow -= Command_EventClearWindow;

                    // Simutronics Print
                    _m_oGame.EventPrintText -= Simutronics_EventPrintText;

                    // Private Sub Simutronics_EventClearWindow(ByoTargetWindow As Genie.Game.WindowTarget) Handles oGame.EventClearWindow
                    // Try
                    // Dim oFormSkin As FormSkin = Nothing

                    // Select Case oTargetWindow
                    // Case Genie.Game.WindowTarget.Death
                    // oFormSkin = m_oOutputDeath
                    // Case Genie.Game.WindowTarget.Familiar
                    // oFormSkin = m_oOutputFamiliar
                    // Case Genie.Game.WindowTarget.Inv
                    // oFormSkin = m_oOutputInv
                    // Case Genie.Game.WindowTarget.Logons
                    // oFormSkin = m_oOutputLogons
                    // Case Genie.Game.WindowTarget.Room
                    // oFormSkin = m_oOutputRoom
                    // Case Genie.Game.WindowTarget.Thoughts
                    // oFormSkin = m_oOutputThoughts
                    // Case Genie.Game.WindowTarget.Log
                    // oFormSkin = m_oOutputLog
                    // Case Genie.Game.WindowTarget.Main
                    // oFormSkin = m_oOutputMain
                    // End Select

                    // If Not IsNothing(oFormSkin) Then ' Do not clear if window does not exist
                    // SafeClearWindow(oFormSkin)
                    // End If
                    // #If Not Debug Then
                    // Catch ex As Exception
                    // HandleGenieException("Game ClearWindow Exception: " & ex.ToString)
                    // #Else
                    // Finally
                    // #End If
                    // End Try
                    // End Sub

                    _m_oGame.EventDataRecieveEnd -= Simutronics_EventEndUpdate;
                    GenieError.EventGenieError -= HandleGenieException;
                    GenieError.EventGenieLegacyPluginError -= HandleLegacyPluginException;
                    GenieError.EventGeniePluginError -= HandlePluginException;
                    _m_oGame.EventTriggerParse -= Game_EventTriggerParse;
                    _m_oGame.EventStatusBarUpdate -= Game_EventStatusBarUpdate;
                    _m_oGame.EventClearSpellTime -= Game_EventClearSpellTime;
                    _m_oGame.EventSpellTime -= Game_EventSpellTime;
                    _m_oGame.EventCastTime -= Game_EventCastTime;
                    _m_oGame.EventRoundTime -= Game_EventRoundtime;
                    if (m_oScriptManager != null)
                    {
                        _m_oGame.EventTriggerPrompt -= m_oScriptManager.TriggerPromptForScripts;
                        _m_oGame.EventTriggerMove -= m_oScriptManager.TriggerMoveForScripts;
                    }
                }

                _m_oGame = value;
                if (_m_oGame != null)
                {
                    _m_oGame.EventParseXML += Plugin_ParsePluginXML;
                    _m_oGame.EventStreamWindow += EventStreamWindow;
                    _m_oGame.EventVariableChanged += ClassCommand_EventVariableChanged;
                    _m_oGame.EventPrintError += PrintError;
                    _m_oGame.EventClearWindow += Command_EventClearWindow;
                    _m_oGame.EventPrintText += Simutronics_EventPrintText;
                    _m_oGame.EventAddImage += AddImage;
                    _m_oGame.EventDataRecieveEnd += Simutronics_EventEndUpdate;
                    GenieError.EventGenieError += HandleGenieException;
                    GenieError.EventGeniePluginError += HandlePluginException;
                    _m_oGame.EventTriggerParse += Game_EventTriggerParse;
                    _m_oGame.EventStatusBarUpdate += Game_EventStatusBarUpdate;
                    _m_oGame.EventClearSpellTime += Game_EventClearSpellTime;
                    _m_oGame.EventSpellTime += Game_EventSpellTime;
                    _m_oGame.EventCastTime += Game_EventCastTime;
                    _m_oGame.EventRoundTime += Game_EventRoundtime;
                    if (m_oScriptManager != null)
                    {
                        _m_oGame.EventTriggerPrompt -= m_oScriptManager.TriggerPromptForScripts;
                        _m_oGame.EventTriggerPrompt += m_oScriptManager.TriggerPromptForScripts;
                        _m_oGame.EventTriggerMove -= m_oScriptManager.TriggerMoveForScripts;
                        _m_oGame.EventTriggerMove += m_oScriptManager.TriggerMoveForScripts;
                    }
                }
            }
        }

        private ScriptManager m_oScriptManager;
        private TriggerEngine m_oTriggerEngine;
        private GameLoop m_oGameLoop;

        private Genie.Command _m_oCommand;

        public Genie.Command m_oCommand
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _m_oCommand;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_m_oCommand != null)
                {
                    _m_oCommand.ListPlugins -= ListPlugins;
                    _m_oCommand.LoadPlugin -= FormPlugin_LoadPlugin;
                    _m_oCommand.UnloadPlugin -= FormPlugin_UnloadPlugin;
                    _m_oCommand.ReloadPlugins -= FormPlugin_ReloadPlugins;
                    _m_oCommand.DisablePlugin -= FormPlugin_DisablePlugin;
                    _m_oCommand.EnablePlugin -= FormPlugin_EnablePlugin;
                    _m_oCommand.EventAddImage -= AddImage;
                    _m_oCommand.EventEchoText -= ClassCommand_EchoText;
                    _m_oCommand.EventLinkText -= ClassCommand_LinkText;
                    _m_oCommand.EventEchoColorText -= ClassCommand_EchoColorText;
                    _m_oCommand.EventSendRaw -= ClassCommand_SendRaw;
                    _m_oCommand.EventSendText -= ClassCommand_SendText;
                    _m_oCommand.EventListScripts -= Command_EventListScripts;
                    _m_oCommand.EventParseLine -= ClassCommand_ParseText;
                    if (m_oScriptManager != null)
                    {
                        _m_oCommand.EventRunScript -= m_oScriptManager.RunScript;
                        _m_oCommand.EventScriptAbort -= m_oScriptManager.ScriptAbort;
                        _m_oCommand.EventScriptPause -= m_oScriptManager.ScriptPause;
                        _m_oCommand.EventScriptPauseOrResume -= m_oScriptManager.ScriptPauseOrResume;
                        _m_oCommand.EventScriptReload -= m_oScriptManager.ScriptReload;
                        _m_oCommand.EventScriptResume -= m_oScriptManager.ScriptResume;
                        _m_oCommand.EventScriptDebug -= m_oScriptManager.ScriptDebugLevel;
                    }
                    _m_oCommand.EventVariableChanged -= ClassCommand_EventVariableChanged;
                    _m_oCommand.EventChangeWindowTitle -= Command_EventChangeWindowTitle;
                    _m_oCommand.EventClearWindow -= Command_EventClearWindow;
                    _m_oCommand.EventScriptVariables -= Command_ScriptVariables;
                    _m_oCommand.EventScriptTrace -= Command_ScriptTrace;
                    _m_oCommand.EventStatusBar -= Command_StatusBar;
                    _m_oCommand.EventReconnect -= ReconnectToGame;
                    _m_oCommand.EventConnect -= ConnectToGame;
                    _m_oCommand.EventDisconnect -= DisconnectFromGame;
                    _m_oCommand.EventExit -= DisconnectAndExit;
                    _m_oCommand.EventClassChange -= Command_EventClassChange;
                    _m_oCommand.EventPresetChanged -= ClassCommand_PresetChanged;
                    _m_oCommand.EventShowScriptExplorer -= Command_ShowScriptExplorer;
                    _m_oCommand.EventLoadLayout -= Command_LoadLayout;
                    _m_oCommand.EventSaveLayout -= Command_SaveLayout;
                    _m_oCommand.EventAddWindow -= Command_EventAddWindow;
                    _m_oCommand.EventPositionWindow -= Command_EventPositionWindow;
                    _m_oCommand.EventRemoveWindow -= Command_EventRemoveWindow;
                    _m_oCommand.EventCloseWindow -= Command_EventCloseWindow;
                    _m_oCommand.EventFlashWindow -= Command_FlashWindow;
                    _m_oCommand.EventMapperCommand -= Command_EventMapperCommand;
                    _m_oCommand.EventLoadProfile -= Command_LoadProfile;
                    _m_oCommand.EventSaveProfile -= Command_SaveProfile;
                    _m_oCommand.EventRawToggle -= Command_RawToggle;
                    _m_oCommand.EventChangeIcon -= Command_ChangeIcon;
                    _m_oCommand.LaunchBrowser -= Command_LaunchBrowser;
                }

                _m_oCommand = value;
                if (_m_oCommand != null)
                {
                    _m_oCommand.ListPlugins += ListPlugins;
                    _m_oCommand.LoadPlugin += FormPlugin_LoadPlugin;
                    _m_oCommand.UnloadPlugin += FormPlugin_UnloadPlugin;
                    _m_oCommand.ReloadPlugins += FormPlugin_ReloadPlugins;
                    _m_oCommand.DisablePlugin += FormPlugin_DisablePlugin;
                    _m_oCommand.EnablePlugin += FormPlugin_EnablePlugin;
                    _m_oCommand.EventAddImage += AddImage;
                    _m_oCommand.EventEchoText += ClassCommand_EchoText;
                    _m_oCommand.EventLinkText += ClassCommand_LinkText;
                    _m_oCommand.EventEchoColorText += ClassCommand_EchoColorText;
                    _m_oCommand.EventSendRaw += ClassCommand_SendRaw;
                    _m_oCommand.EventSendText += ClassCommand_SendText;
                    _m_oCommand.EventListScripts += Command_EventListScripts;
                    _m_oCommand.EventParseLine += ClassCommand_ParseText;
                    if (m_oScriptManager != null)
                    {
                        _m_oCommand.EventRunScript -= m_oScriptManager.RunScript;
                        _m_oCommand.EventRunScript += m_oScriptManager.RunScript;
                        _m_oCommand.EventScriptAbort -= m_oScriptManager.ScriptAbort;
                        _m_oCommand.EventScriptAbort += m_oScriptManager.ScriptAbort;
                        _m_oCommand.EventScriptPause -= m_oScriptManager.ScriptPause;
                        _m_oCommand.EventScriptPause += m_oScriptManager.ScriptPause;
                        _m_oCommand.EventScriptPauseOrResume -= m_oScriptManager.ScriptPauseOrResume;
                        _m_oCommand.EventScriptPauseOrResume += m_oScriptManager.ScriptPauseOrResume;
                        _m_oCommand.EventScriptReload -= m_oScriptManager.ScriptReload;
                        _m_oCommand.EventScriptReload += m_oScriptManager.ScriptReload;
                        _m_oCommand.EventScriptResume -= m_oScriptManager.ScriptResume;
                        _m_oCommand.EventScriptResume += m_oScriptManager.ScriptResume;
                        _m_oCommand.EventScriptDebug -= m_oScriptManager.ScriptDebugLevel;
                        _m_oCommand.EventScriptDebug += m_oScriptManager.ScriptDebugLevel;
                    }
                    _m_oCommand.EventVariableChanged += ClassCommand_EventVariableChanged;
                    _m_oCommand.EventChangeWindowTitle += Command_EventChangeWindowTitle;
                    _m_oCommand.EventClearWindow += Command_EventClearWindow;
                    _m_oCommand.EventScriptVariables += Command_ScriptVariables;
                    _m_oCommand.EventScriptTrace += Command_ScriptTrace;
                    _m_oCommand.EventStatusBar += Command_StatusBar;
                    _m_oCommand.EventReconnect += ReconnectToGame;
                    _m_oCommand.EventConnect += ConnectToGame;
                    _m_oCommand.EventDisconnect += DisconnectFromGame;
                    _m_oCommand.EventExit += DisconnectAndExit;
                    _m_oCommand.EventClassChange += Command_EventClassChange;
                    _m_oCommand.EventPresetChanged += ClassCommand_PresetChanged;
                    _m_oCommand.EventShowScriptExplorer += Command_ShowScriptExplorer;
                    _m_oCommand.EventLoadLayout += Command_LoadLayout;
                    _m_oCommand.EventSaveLayout += Command_SaveLayout;
                    _m_oCommand.EventAddWindow += Command_EventAddWindow;
                    _m_oCommand.EventPositionWindow += Command_EventPositionWindow;
                    _m_oCommand.EventRemoveWindow += Command_EventRemoveWindow;
                    _m_oCommand.EventCloseWindow += Command_EventCloseWindow;
                    _m_oCommand.EventFlashWindow += Command_FlashWindow;
                    _m_oCommand.EventMapperCommand += Command_EventMapperCommand;
                    _m_oCommand.EventLoadProfile += Command_LoadProfile;
                    _m_oCommand.EventSaveProfile += Command_SaveProfile;
                    _m_oCommand.EventRawToggle += Command_RawToggle;
                    _m_oCommand.EventChangeIcon += Command_ChangeIcon;
                    _m_oCommand.LaunchBrowser += Command_LaunchBrowser;
                }
            }
        }

        private FormMapperSettings MapperSettings { get; set; }

        private Mapper.AutoMapper _m_oAutoMapper;

        public Mapper.AutoMapper m_oAutoMapper
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _m_oAutoMapper;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_m_oAutoMapper != null)
                {
                    _m_oAutoMapper.EventEchoText -= AutoMapper_EventEchoText;
                    _m_oAutoMapper.EventSendText -= Plugin_EventSendText;
                    _m_oAutoMapper.EventParseText -= ClassCommand_ParseText;
                    _m_oAutoMapper.EventVariableChanged -= PluginHost_EventVariableChanged;
                }

                _m_oAutoMapper = value;
                if (_m_oAutoMapper != null)
                {
                    _m_oAutoMapper.EventEchoText += AutoMapper_EventEchoText;
                    _m_oAutoMapper.EventSendText += Plugin_EventSendText;
                    _m_oAutoMapper.EventParseText += ClassCommand_ParseText;
                    _m_oAutoMapper.EventVariableChanged += PluginHost_EventVariableChanged;
                }
            }
        }

        private Genie.XMLConfig m_oConfig = new Genie.XMLConfig();
        private FormSkin _m_oOutputMain;

        private FormSkin m_oOutputMain
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _m_oOutputMain;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_m_oOutputMain != null)
                {
                    _m_oOutputMain.EventLinkClicked -= FormSkin_LinkClicked;
                }

                _m_oOutputMain = value;
                if (_m_oOutputMain != null)
                {
                    _m_oOutputMain.EventLinkClicked += FormSkin_LinkClicked;
                }
            }
        }

        private FormSkin m_oOutputInv;
        private FormSkin m_oOutputFamiliar;
        private FormSkin m_oOutputThoughts;
        private FormSkin m_oOutputLogons;
        private FormSkin m_oOutputDeath;
        private FormSkin m_oOutputRoom;
        private FormSkin m_oOutputLog;
        private FormSkin m_oOutputDebug;
        private FormSkin m_oOutputActiveSpells;
        private FormSkin m_oOutputCombat;
        private FormSkin m_oOutputPortrait;
        private Genie.Collections.ArrayList m_oFormList = new Genie.Collections.ArrayList();
        private string m_sConfigFile = string.Empty;
        // private string m_sUpdateVersion = string.Empty;
        // private bool m_bIsUpdateMajor = false;
        private string m_sGenieKey = string.Empty;
        // Private WithEvents m_oWorker As New System.ComponentModel.BackgroundWorker
        // Private m_bRunWorker As Boolean = True

        public Genie.Collections.ArrayList FormList
        {
            get
            {
                return m_oFormList;
            }
        }

        public FormSkin OutputMain
        {
            get
            {
                return m_oOutputMain;
            }
        }



        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        public bool bCloseAllDocument = false;
        public bool bCloseNow = false;
        private const int WM_CLOSE = 0x10;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_CLOSE)
            {
                bCloseAllDocument = true;
                if (!Information.IsNothing(m_oAutoMapper))
                {
                    m_oAutoMapper.IsClosing = true;
                }

                foreach (object oPlugin in m_oGlobals.PluginList)
                {
                    if (oPlugin is GeniePlugin.Interfaces.IPlugin)
                    {
                        try
                        {
                            if ((oPlugin as GeniePlugin.Interfaces.IPlugin).Enabled)
                                (oPlugin as GeniePlugin.Interfaces.IPlugin).ParentClosing();
                        }
                        /* TODO ERROR: Skipped IfDirectiveTrivia */
                        catch (Exception ex)
                        {
                            ShowDialogPluginException((oPlugin as GeniePlugin.Interfaces.IPlugin), "ParentClosing", ex);
                            (oPlugin as GeniePlugin.Interfaces.IPlugin).Enabled = false;
                            /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
                        }
                    }
                    else if (oPlugin is GeniePlugin.Plugins.IPlugin)
                    {
                        try
                        {
                            if ((oPlugin as GeniePlugin.Plugins.IPlugin).Enabled)
                                (oPlugin as GeniePlugin.Plugins.IPlugin).ParentClosing();
                        }
                        /* TODO ERROR: Skipped IfDirectiveTrivia */
                        catch (Exception ex)
                        {
                            ShowDialogPluginException((oPlugin as GeniePlugin.Plugins.IPlugin), "ParentClosing", ex);
                            (oPlugin as GeniePlugin.Plugins.IPlugin).Enabled = false;
                            /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
                        }
                    }
                }
            }

            base.WndProc(ref m);
        }

        private void FormMain_Activated(object sender, EventArgs e)
        {
            TextBoxInput.Focus();
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (bCloseNow == false)
            {
                if (m_oGame.IsConnected == true & m_oGlobals.Config.bIgnoreCloseAlert == false)
                {
                    if (Interaction.MsgBox("You are connected to the game. Are you sure you want to close?", MsgBoxStyle.YesNo, "Genie") == MsgBoxResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                }

                // If m_oOutputMain.Visible = True Then ' Check so we have windows first.
                // If HasSettingsChanged() = True Then
                // If MsgBox("Your window settings has changed. Do you wish to save them?", MsgBoxStyle.YesNo, "Genie") = MsgBoxResult.Yes Then
                // SaveXMLConfig()
                // End If
                // End If
                // End If
            }
        }

        private void FormMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (My.MyProject.Forms.FormConfig.Visible == false | TextBoxInput.Focused == true)
            {
                var macroKey = (Genie.KeyCode.Keys)(int)e.KeyData;
                if (m_oGlobals.MacroList.Contains(macroKey) == true)
                {
                    m_oCommand.ParseCommand(((Genie.Macros.Macro)m_oGlobals.MacroList[macroKey]).sAction, true, true);
                    string argsText = "";
                    var argoColor = Color.Transparent;
                    var argoBgColor = Color.Transparent;
                    Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                    string argsTargetWindow = "";
                    AddText(argsText, argoColor, argoBgColor, oTargetWindow: argoTargetWindow, sTargetWindow: argsTargetWindow); // For some stupid reason we need this. Probably because EndUpdate is fired before we are ready in the other thread.
                    EndUpdate();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }

        private bool m_bLastKeyWasTab = false;

        private void TextBoxInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
            {
                if (TextBoxInput.Text.StartsWith(Conversions.ToString(m_oGlobals.Config.ScriptChar)) == true & TextBoxInput.Text.EndsWith(" ") == false)
                {
                    if (m_bLastKeyWasTab == true)
                    {
                        ListScripts(TextBoxInput.Text.Substring(1) + $"*.{m_oGlobals.Config.ScriptExtension}");
                        m_bLastKeyWasTab = false;
                    }
                    else if (TextBoxInput.Text.Length > 1)
                    {
                        string sTempRow;
                        sTempRow = FindScript(TextBoxInput.Text.Substring(1));
                        if (sTempRow.Length > 0)
                        {
                            TextBoxInput.Text = "." + sTempRow;
                            TextBoxInput.SelectionStart = TextBoxInput.TextLength;
                            if (sTempRow.EndsWith(" ") == false)
                            {
                                m_bLastKeyWasTab = true;
                            }
                            else
                            {
                                Interaction.Beep();
                            } // Sound a tone.
                        }
                        else
                        {
                            m_bLastKeyWasTab = true;
                        }
                    }
                }
                else if (TextBoxInput.Text.ToLower().StartsWith("#edit "))
                {
                    if (m_bLastKeyWasTab == true)
                    {
                        ListScripts(TextBoxInput.Text.Substring(6) + $"*.{m_oGlobals.Config.ScriptExtension}");
                        m_bLastKeyWasTab = false;
                    }
                    else if (TextBoxInput.Text.Length > 1)
                    {
                        string sTempRow;
                        sTempRow = FindScript(TextBoxInput.Text.Substring(6));
                        if (sTempRow.Length > 0)
                        {
                            TextBoxInput.Text = "#edit " + sTempRow;
                            TextBoxInput.SelectionStart = TextBoxInput.TextLength;
                            if (sTempRow.EndsWith(" ") == false)
                            {
                                m_bLastKeyWasTab = true;
                            }
                            else
                            {
                                Interaction.Beep();
                            } // Sound a tone.
                        }
                        else
                        {
                            m_bLastKeyWasTab = true;
                        }
                    }
                }
                else if (TextBoxInput.Text.EndsWith(" ") == false)
                {
                    if (m_bLastKeyWasTab == true)
                    {
                        // Select next
                        if (!Information.IsNothing(TextBoxInput.Tag))
                        {
                            if (m_oGlobals.AliasList.AcquireReaderLock())
                            {
                                try
                                {
                                    var oMatchList = new Genie.Collections.ArrayList();
                                    int I = 0;
                                    int iCurrentId = 0;
                                    foreach (DictionaryEntry de in m_oGlobals.AliasList)
                                    {
                                        if (de.Key.ToString().StartsWith(TextBoxInput.Tag.ToString()))
                                        {
                                            var argvalue = de.Key;
                                            oMatchList.Add(argvalue);
                                            I += 1;
                                            if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(de.Key, TextBoxInput.Text.ToString(), false)))	// Current
                                            {
                                                iCurrentId = I;
                                            }
                                        }
                                    }

                                    if (iCurrentId > oMatchList.Count - 1)
                                    {
                                        iCurrentId = 0; // Restart at 0
                                    }

                                    if (oMatchList.Count > 0)
                                    {
                                        m_bLastKeyWasTab = true;
                                        TextBoxInput.Text = oMatchList[iCurrentId].ToString();
                                        TextBoxInput.SelectionStart = TextBoxInput.TextLength;
                                    }
                                }
                                finally
                                {
                                    m_oGlobals.AliasList.ReleaseReaderLock();
                                }
                            }
                            else
                            {
                                ShowDialogException("Alias KeyDown", "Unable to acquire reader lock.");
                            }
                        }
                    }
                    else if (m_oGlobals.AliasList.AcquireReaderLock())
                    {
                        try
                        {
                            TextBoxInput.Tag = TextBoxInput.Text; // Save match pattern
                            var oMatchList = new Genie.Collections.ArrayList();
                            foreach (DictionaryEntry de in m_oGlobals.AliasList)
                            {
                                if (de.Key.ToString().StartsWith(TextBoxInput.Text))
                                {
                                    var argvalue1 = de.Key;
                                    oMatchList.Add(argvalue1);
                                }
                            }

                            if (oMatchList.Count == 1)
                            {
                                TextBoxInput.Text = oMatchList[0].ToString() + " ";
                                TextBoxInput.SelectionStart = TextBoxInput.TextLength;
                            }
                            else if (oMatchList.Count > 0)
                            {
                                m_bLastKeyWasTab = true;
                                TextBoxInput.Text = oMatchList[0].ToString();
                                TextBoxInput.SelectionStart = TextBoxInput.TextLength;
                            }
                        }
                        finally
                        {
                            m_oGlobals.AliasList.ReleaseReaderLock();
                        }
                    }
                    else
                    {
                        ShowDialogException("Alias KeyDown", "Unable to acquire reader lock.");
                    }
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (m_bLastKeyWasTab == true)
            {
                TextBoxInput.Tag = null;
                m_bLastKeyWasTab = false;
            }
        }

        public void ListScripts(string sPattern)
        {
            string sFile;
            int i = 0;

            // Run through all files in a directory
            string argsText = "Scripts matching: " + sPattern + System.Environment.NewLine;
            Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
            AddText(argsText, oTargetWindow: argoTargetWindow);
            if (sPattern.IndexOf(@"\") == -1)
            {
                string sLocation = m_oGlobals.Config.ScriptDir;
                if (sLocation.EndsWith(@"\"))
                {
                    sPattern = sLocation + sPattern;
                }
                else
                {
                    sPattern = sLocation + @"\" + sPattern;
                }
            }

            sFile = FileSystem.Dir(sPattern);
            while (!string.IsNullOrEmpty(sFile))
            {
                i += 1;
                string argsText1 = Constants.vbTab + sFile + System.Environment.NewLine;
                Genie.Game.WindowTarget argoTargetWindow1 = Genie.Game.WindowTarget.Main;
                AddText(argsText1, oTargetWindow: argoTargetWindow1);
                sFile = FileSystem.Dir();
            }

            string argsText2 = System.Environment.NewLine + "Found " + i + " files." + System.Environment.NewLine;
            Genie.Game.WindowTarget argoTargetWindow2 = Genie.Game.WindowTarget.Main;
            AddText(argsText2, oTargetWindow: argoTargetWindow2);
        }

        public string FindScript(string sPattern)
        {
            string sDir = string.Empty;
            string sFile = string.Empty;
            string sMin = string.Empty;
            int i = 0;
            if (sPattern.IndexOf(@"\") == -1)
            {
                string sLocation = m_oGlobals.Config.ScriptDir;
                if (sLocation.EndsWith(@"\"))
                {
                    sPattern = sLocation + sPattern;
                }
                else
                {
                    sPattern = sLocation + @"\" + sPattern;
                }
            }

            sDir = FileSystem.Dir(sPattern + $"*.{m_oGlobals.Config.ScriptExtension}", Constants.vbArchive);
            while (!string.IsNullOrEmpty(sDir))
            {
                i += 1;
                sFile = sDir;
                sMin = FindMinString(sMin, sFile);
                sDir = FileSystem.Dir();
            }

            if (i == 1)
            {
                return sFile.ToLower().Replace($".{m_oGlobals.Config.ScriptExtension}", "") + " ";
            }
            else if (Strings.Len(sMin) > 0)
            {
                return sMin;
            }
            else
            {
                return "";
            }
        }

        private string FindMinString(string s1, string s2)
        {
            long i;
            long iMinLen;
            if (s1.Length == 0) // First loop
            {
                return s2;
            }
            else
            {
                iMinLen = s1.Length;
                if (iMinLen > s2.Length)
                {
                    iMinLen = s2.Length;
                }

                var loopTo = iMinLen - 1;
                for (i = 0; i <= loopTo; i++)
                {
                    if (s1[Conversions.ToInteger(i)] != s2[Conversions.ToInteger(i)])
                    {
                        break;
                    }
                }

                if (i > 0)
                {
                    return s1.Substring(0, Conversions.ToInteger(i));
                }
                else
                {
                    return "";
                }
            }
        }


        private void FormMain_Load(object sender, EventArgs e)
        {
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            Application.ThreadException += Application_ThreadException;
            /* TODO ERROR: Skipped EndIfDirectiveTrivia */
            // AddHandler SystemEvents.DisplaySettingsChanged, AddressOf FormMain_SizeChange

            foreach (Control ctl in Controls)
            {
                if (ctl is MdiClient)
                {
                    ctl.BackColor = BackColor;
                }
            }

            if (!LoadSizedXMLConfig(m_sConfigFile))
            {
                LoadXMLConfig(m_sConfigFile);
            }

            if (m_bSetDefaultLayout == true)
            {
                PanelStatus.Visible = true;
                PanelBars.Visible = true;
                MagicPanelsToolStripMenuItem.Checked = true;
                SetMagicPanels(true);
                StatusStripMain.Visible = true;
                LayoutBasic();
            }

            ShowForm(m_oOutputMain);
            ShowOutputForms();
            TextBoxInput.Focus();
            Application.DoEvents();

            AppendText("Using Encoding: " + Encoding.Default.EncodingName + System.Environment.NewLine);
            AppendText("Genie User Data Path: " + LocalDirectory.Path + System.Environment.NewLine + System.Environment.NewLine);

            // AppendText(vbNewLine & _
            // "THIS SOFTWARE AND THE ACCOMPANYING FILES ARE SENT ""AS IS"" AND WITHOUT WARRANTY AS TO PERFORMANCE OF MERCHANTABILITY OR ANY OTHER WARRANTIES WHETHER EXPRESSED OR IMPLIED." & vbNewLine & _
            // "The software authors will not be held liable for any damage to your computer system, data files, gaming environment, or for any actions brought against you for using this software. The user must assume the entire risk of running this software." & vbNewLine & _
            // "You may not redistribute this software in any way shape or form without the written permission from the author." & vbNewLine & _
            // vbNewLine & _
            // "BY USING THIS SOFTWARE YOU AGREE TO THE ABOVE STATED TERMS " & vbNewLine & vbNewLine)

            Application.DoEvents();
            AppendText("Loading Settings...");
            m_oGlobals.Config.Load(m_oGlobals.Config.ConfigDir + @"\settings.cfg");
            AppendText("OK" + System.Environment.NewLine);
            Application.DoEvents();
            AppendText("Loading Presets...");
            m_oGlobals.PresetList.Load(m_oGlobals.Config.ConfigDir + @"\presets.cfg");
            string argsPreset = "all";
            PresetChanged(argsPreset);
            RecolorUI();
            AppendText("OK" + System.Environment.NewLine);
            Application.DoEvents();
            AppendText("Loading Global Variables...");
            m_oGlobals.VariableList.Load(m_oGlobals.Config.ConfigDir + @"\variables.cfg");
            AppendText("OK" + System.Environment.NewLine);
            Application.DoEvents();
            AppendText("Loading Highlights...");
            m_oGlobals.LoadHighlights(m_oGlobals.Config.ConfigDir + @"\highlights.cfg");
            AppendText("OK" + System.Environment.NewLine);
            Application.DoEvents();
            AppendText("Loading Names...");
            m_oGlobals.NameList.Load(m_oGlobals.Config.ConfigDir + @"\names.cfg");
            AppendText("OK" + System.Environment.NewLine);
            Application.DoEvents();
            AppendText("Loading Macros...");
            m_oGlobals.MacroList.Load(m_oGlobals.Config.ConfigDir + @"\macros.cfg");
            AppendText("OK" + System.Environment.NewLine);
            Application.DoEvents();
            AppendText("Loading Aliases...");
            m_oGlobals.AliasList.Load(m_oGlobals.Config.ConfigDir + @"\aliases.cfg");
            AppendText("OK" + System.Environment.NewLine);
            Application.DoEvents();
            AppendText("Loading Substitutes...");
            m_oGlobals.SubstituteList.Load(m_oGlobals.Config.ConfigDir + @"\substitutes.cfg");
            AppendText("OK" + System.Environment.NewLine);
            Application.DoEvents();
            AppendText("Loading Gags...");
            m_oGlobals.GagList.Load(m_oGlobals.Config.ConfigDir + @"\gags.cfg");
            AppendText("OK" + System.Environment.NewLine);
            Application.DoEvents();
            AppendText("Loading Triggers...");
            m_oGlobals.TriggerList.Load(m_oGlobals.Config.ConfigDir + @"\triggers.cfg");
            AppendText("OK" + System.Environment.NewLine);
            Application.DoEvents();
            AppendText("Loading Classes...");
            m_oGlobals.ClassList.Load(m_oGlobals.Config.ConfigDir + @"\classes.cfg");
            AppendText("OK" + System.Environment.NewLine);
            Application.DoEvents();
            int I = LoadPlugins();
            Application.DoEvents();
            UpdateOnStartup();
            Application.DoEvents();

            m_oOutputMain.RichTextBoxOutput.EndTextUpdate();

            // MsgBox("OK")
            // Display RichTextBox
            // ShowOutputBoxes()
            // m_oOutputMain.ShowOutput()

            UpdateLayoutMenu();

            /* TODO ERROR: Skipped IfDirectiveTrivia */
            if (m_bVersionUpdated == true)
            {
                My.MyProject.Forms.DialogChangelog.ShowDialog(this);
            }
            /* TODO ERROR: Skipped EndIfDirectiveTrivia */
            TextBoxInput.Focus();
            InitWorkerThread();
            m_bIsLoading = false;

            // m_oGame.ParseGameRow("*** <pushBold/>BLAH<popBold/> ***")
            m_oOutputMain.RichTextBoxOutput.EndTextUpdate();

            // TEMP TEMP TEMP
            // m_PluginDialog.ShowDialog(Me)

        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        /* TODO ERROR: Skipped RegionDirectiveTrivia */

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */


        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        // m_oScriptManager.ScriptList, m_oScriptManager.ScriptListNew, m_oScriptManager.ScriptListUpdated, TickScripts, RemoveExitedScripts
        // moved to ScriptManager

        public delegate void RemoveExitedScriptsDelegate();

        private void SafeRemoveExitedScripts()
        {
            try
            {
                if (InvokeRequired == true)
                {
                    Invoke(new RemoveExitedScriptsDelegate(m_oScriptManager.RemoveExitedScripts));
                }
                else
                {
                    m_oScriptManager.RemoveExitedScripts();
                }
            }
#pragma warning disable CS0168
            catch (Exception ex)
#pragma warning restore CS0168
            {
            } // Don't care. Close
        }

        private void SetScriptDebugLevel(ToolStripSplitButton oButton, int DebugLevel)
        {
            if (oButton.DropDownItems.ContainsKey("Debuglevel"))
            {
                ToolStripMenuItem oDebugItem = (ToolStripMenuItem)oButton.DropDownItems["Debuglevel"];
                foreach (object oItem in oDebugItem.DropDownItems)
                {
                    if (oItem is ToolStripMenuItem)
                    {
                        if (!Information.IsNothing(((ToolStripMenuItem)oItem).Tag) && ((ToolStripMenuItem)oItem).Tag is int)
                        {
                            if ((int)((ToolStripMenuItem)oItem).Tag == DebugLevel)
                            {
                                ((ToolStripMenuItem)oItem).Checked = true;
                            }
                            else
                            {
                                ((ToolStripMenuItem)oItem).Checked = false;
                            }
                        }
                    }
                }
            }
        }

        private void SetScriptName(ToolStripSplitButton oButton)
        {
            if (!Information.IsNothing(oButton.Tag))
            {
                if (oButton.Tag is Script)
                {
                    Script oScriptRef = (Script)oButton.Tag;
                    if (oScriptRef.DebugLevel > 0)
                    {
                        oButton.Text = oScriptRef.ScriptName + " (Debug: " + oScriptRef.DebugLevel + ")";
                    }
                    else
                    {
                        oButton.Text = oScriptRef.ScriptName;
                    }
                }
            }
        }

        private void ScriptManager_ScriptAdded(Script oScript)
        {
            AddScriptToToolStrip(oScript);
        }

        private void AddScriptToToolStrip(Script oScript)
        {
            if (ToolStripButtons.Visible == false)
                return;
            if (Monitor.TryEnter(ToolStripButtons.Items))
            {
                try
                {
                    var oScriptButton = new ToolStripSplitButton();
                    oScriptButton.Name = "Script_" + oScript.ScriptID;
                    oScriptButton.Text = "";
                    oScriptButton.Image = My.Resources.Resources.control_play;
                    oScriptButton.Tag = oScript;
                    oScriptButton.ButtonClick += ScriptButton_Click;
                    var oMenuItemResume = new ToolStripMenuItem();
                    oMenuItemResume.Text = "Resume";
                    oMenuItemResume.Image = My.Resources.Resources.control_play;
                    oMenuItemResume.Click += ScriptButtonResume_Click;
                    var oMenuItemPause = new ToolStripMenuItem();
                    oMenuItemPause.Text = "Pause";
                    oMenuItemPause.Image = My.Resources.Resources.control_pause;
                    oMenuItemPause.Click += ScriptButtonPause_Click;
                    var oMenuItemAbort = new ToolStripMenuItem();
                    oMenuItemAbort.Text = "Abort";
                    oMenuItemAbort.Image = My.Resources.Resources.control_stop;
                    oMenuItemAbort.Click += ScriptButtonAbort_Click;
                    var oMenuItemTrace = new ToolStripMenuItem();
                    oMenuItemTrace.Text = "Show Trace";
                    oMenuItemTrace.Click += ScriptButtonTrace_Click;
                    var oMenuItemVars = new ToolStripMenuItem();
                    oMenuItemVars.Text = "Show Vars";
                    oMenuItemVars.Click += ScriptButtonVars_Click;
                    var oMenuItemEdit = new ToolStripMenuItem();
                    oMenuItemEdit.Text = "Edit";
                    oMenuItemEdit.Click += ScriptButtonEdit_Click;
                    var oMenuItemDebug = new ToolStripMenuItem();
                    oMenuItemDebug.Name = "Debuglevel";
                    oMenuItemDebug.Text = "Debug";
                    var oMenuItemDebug0 = new ToolStripMenuItem();
                    oMenuItemDebug0.Text = "0. Debug off (Default)";
                    oMenuItemDebug0.Checked = true;
                    oMenuItemDebug0.Tag = 0;
                    var oMenuItemDebug1 = new ToolStripMenuItem();
                    oMenuItemDebug1.Text = "1. Goto, gosub, return, labels";
                    oMenuItemDebug1.Tag = 1;
                    var oMenuItemDebug2 = new ToolStripMenuItem();
                    oMenuItemDebug2.Text = "2. Pause, wait, waitfor, move";
                    oMenuItemDebug2.Tag = 2;
                    var oMenuItemDebug3 = new ToolStripMenuItem();
                    oMenuItemDebug3.Text = "3. If evaluations";
                    oMenuItemDebug3.Tag = 3;
                    var oMenuItemDebug4 = new ToolStripMenuItem();
                    oMenuItemDebug4.Text = "4. Math, evalmath, counter, variables";
                    oMenuItemDebug4.Tag = 4;
                    var oMenuItemDebug5 = new ToolStripMenuItem();
                    oMenuItemDebug5.Text = "5. Actions";
                    oMenuItemDebug5.Tag = 5;
                    var oMenuItemDebug10 = new ToolStripMenuItem();
                    oMenuItemDebug10.Text = "10. Raw script lines";
                    oMenuItemDebug10.Tag = 10;
                    oMenuItemDebug0.Click += ScriptButtonDebuglevel_Click;
                    oMenuItemDebug1.Click += ScriptButtonDebuglevel_Click;
                    oMenuItemDebug2.Click += ScriptButtonDebuglevel_Click;
                    oMenuItemDebug3.Click += ScriptButtonDebuglevel_Click;
                    oMenuItemDebug4.Click += ScriptButtonDebuglevel_Click;
                    oMenuItemDebug5.Click += ScriptButtonDebuglevel_Click;
                    oMenuItemDebug10.Click += ScriptButtonDebuglevel_Click;
                    oMenuItemDebug.DropDownItems.AddRange(new ToolStripItem[] { oMenuItemDebug0, oMenuItemDebug1, oMenuItemDebug2, oMenuItemDebug3, oMenuItemDebug4, oMenuItemDebug5, oMenuItemDebug10 });
                    oScriptButton.DropDownItems.AddRange(new ToolStripItem[] { oMenuItemResume, oMenuItemPause, oMenuItemAbort, new ToolStripSeparator(), oMenuItemDebug, oMenuItemTrace, oMenuItemVars, oMenuItemEdit });
                    Script oScriptRef = (Script)oScriptButton.Tag;
                    ToolStripButtons.Items.Add(oScriptButton);
                    SetScriptName(oScriptButton);
                    oMenuItemEdit.Text = "Edit " + oScriptRef.FileName;
                }
                catch (Exception ex)
                {
                    GenieError.Error("AddScriptToToolstrip in MainForm", ex.Message, ex.StackTrace);
                }
                finally
                {
                    Monitor.Exit(ToolStripButtons.Items);
                }
            }
            else
            {
                throw new Exception("Unable to lock toolstrip for addscripttotoolstrip()");
            }
        }

        // LoadScript moved to ScriptManager

        private void Script_EventDebugChanged(Script sender, int iLevel)
        {
            if (ToolStripButtons.Visible == false)
                return;
            if (Monitor.TryEnter(ToolStripButtons.Items))
            {
                try
                {
                    foreach (object oItem in ToolStripButtons.Items)
                    {
                        if (oItem is ToolStripSplitButton)
                        {
                            if (((ToolStripSplitButton)oItem).Tag is Script && (((Script)((ToolStripSplitButton)oItem).Tag).ScriptID ?? "") == (sender.ScriptID ?? ""))
                            {
                                ToolStripSplitButton argoButton = (ToolStripSplitButton)oItem;
                                SetScriptDebugLevel(argoButton, iLevel);
                                ToolStripSplitButton argoButton1 = (ToolStripSplitButton)oItem;
                                SetScriptName(argoButton1);
                            }
                        }
                    }
                }
                // Catch ex As Exception
                // Throw (ex)
                finally
                {
                    Monitor.Exit(ToolStripButtons.Items);
                }
            }
            else
            {
                throw new Exception("Unable to lock toolstrip for eventdebugchanged()");
            }
        }

        private void Script_EventStatusChanged(Script sender, Script.ScriptState state)
        {
            if (ToolStripButtons.Visible == false)
                return;
            if (Monitor.TryEnter(ToolStripButtons.Items))
            {
                try
                {
                    foreach (object oItem in ToolStripButtons.Items)
                    {
                        if (oItem is ToolStripSplitButton)
                        {
                            if (((ToolStripSplitButton)oItem).Tag is Script && (((Script)((ToolStripSplitButton)oItem).Tag).ScriptID ?? "") == (sender.ScriptID ?? ""))
                            {
                                if (state == Script.ScriptState.finished)
                                {
                                    ToolStripButtons.Items.Remove((ToolStripSplitButton)oItem);
                                }
                                else if (state == Script.ScriptState.pausing)
                                {
                                    ((ToolStripSplitButton)oItem).Image = My.Resources.Resources.control_pause;
                                }
                                else if (state == Script.ScriptState.running)
                                {
                                    ((ToolStripSplitButton)oItem).Image = My.Resources.Resources.control_play;
                                }

                                break;
                            }
                        }
                    }
                }
                // Catch ex As Exception
                // Throw (ex)
                finally
                {
                    Monitor.Exit(ToolStripButtons.Items);
                }
            }
            else
            {
                throw new Exception("Unable to lock toolstrip for eventdebugchanged()");
            }
        }

        private void ScriptButton_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripSplitButton)
            {
                ToolStripSplitButton oButton = (ToolStripSplitButton)sender;
                if (!Information.IsNothing(oButton.Tag))
                {
                    Script oMyScript = (Script)oButton.Tag;
                    if (oMyScript.ScriptPaused == true)
                    {
                        oMyScript.ResumeScript();
                    }
                    // oButton.Image = Global.Genie.My.Resources.Resources.control_play
                    else
                    {
                        oMyScript.PauseScript();
                        // oButton.Image = Global.Genie.My.Resources.Resources.control_pause
                    }
                }
            }
        }

        private void ScriptButtonDebuglevel_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem)
            {
                ToolStripMenuItem oMenuItem = (ToolStripMenuItem)sender;
                if (oMenuItem.OwnerItem is ToolStripMenuItem)
                {
                    int iDebugLevel = 0;
                    if (!Information.IsNothing(oMenuItem.Tag) && oMenuItem.Tag is int)
                    {
                        iDebugLevel = Conversions.ToInteger(oMenuItem.Tag);
                    }

                    oMenuItem = (ToolStripMenuItem)oMenuItem.OwnerItem;
                    if (oMenuItem.OwnerItem is ToolStripSplitButton)
                    {
                        ToolStripSplitButton oButton = (ToolStripSplitButton)oMenuItem.OwnerItem;
                        if (!Information.IsNothing(oButton.Tag))
                        {
                            Script oScript = (Script)oButton.Tag;
                            oScript.DebugLevel = iDebugLevel;
                            string argsText = "[Script debuglevel set to " + iDebugLevel.ToString() + " for script: " + oScript.FileName + "]" + System.Environment.NewLine;
                            var argoColor = Color.White;
                            var argoBgColor = Color.Transparent;
                            Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                            string argsTargetWindow = "";
                            AddText(argsText, argoColor, argoBgColor, oTargetWindow: argoTargetWindow, sTargetWindow: argsTargetWindow);
                            Genie.Game.WindowTarget argoTargetWindow1 = Genie.Game.WindowTarget.Main;
                            AddText(System.Environment.NewLine, oTargetWindow: argoTargetWindow1);
                        }
                    }
                }
            }
        }

        private void ScriptButtonResume_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem)
            {
                ToolStripMenuItem oMenuItem = (ToolStripMenuItem)sender;
                if (oMenuItem.OwnerItem is ToolStripSplitButton)
                {
                    ToolStripSplitButton oButton = (ToolStripSplitButton)oMenuItem.OwnerItem;
                    if (!Information.IsNothing(oButton.Tag))
                    {
                        ((Script)oButton.Tag).ResumeScript();
                    }
                }
            }
        }

        private void ScriptButtonPause_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem)
            {
                ToolStripMenuItem oMenuItem = (ToolStripMenuItem)sender;
                if (oMenuItem.OwnerItem is ToolStripSplitButton)
                {
                    ToolStripSplitButton oButton = (ToolStripSplitButton)oMenuItem.OwnerItem;
                    if (!Information.IsNothing(oButton.Tag))
                    {
                        ((Script)oButton.Tag).PauseScript();
                    }
                }
            }
        }

        private void ScriptButtonAbort_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem)
            {
                ToolStripMenuItem oMenuItem = (ToolStripMenuItem)sender;
                if (oMenuItem.OwnerItem is ToolStripSplitButton)
                {
                    ToolStripSplitButton oButton = (ToolStripSplitButton)oMenuItem.OwnerItem;
                    if (!Information.IsNothing(oButton.Tag))
                    {
                        ((Script)oButton.Tag).AbortScript();
                    }
                }
            }
        }

        private void ScriptButtonEdit_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem)
            {
                ToolStripMenuItem oMenuItem = (ToolStripMenuItem)sender;
                if (oMenuItem.OwnerItem is ToolStripSplitButton)
                {
                    ToolStripSplitButton oButton = (ToolStripSplitButton)oMenuItem.OwnerItem;
                    if (!Information.IsNothing(oButton.Tag))
                    {
                        string sTemp = ((Script)oButton.Tag).FileName;
                        if (sTemp.ToLower().EndsWith($".{m_oGlobals.Config.ScriptExtension}") == false)
                        {
                            sTemp += $".{m_oGlobals.Config.ScriptExtension}";
                        }

                        if (sTemp.IndexOf(@"\") == -1)
                        {
                            sTemp = LocalDirectory.Path + @"\Scripts\" + sTemp;
                        }

                        Interaction.Shell("\"" + m_oGlobals.Config.sEditor + "\" \"" + sTemp, AppWinStyle.NormalFocus, false);
                    }
                }
            }
        }

        private void ScriptButtonTrace_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem)
            {
                ToolStripMenuItem oMenuItem = (ToolStripMenuItem)sender;
                if (oMenuItem.OwnerItem is ToolStripSplitButton)
                {
                    ToolStripSplitButton oButton = (ToolStripSplitButton)oMenuItem.OwnerItem;
                    if (!Information.IsNothing(oButton.Tag))
                    {
                        Script oMyScript = (Script)oButton.Tag;
                        string argsText = System.Environment.NewLine + "Script trace for " + oMyScript.FileName + ":" + System.Environment.NewLine;
                        Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                        AddText(argsText, oTargetWindow: argoTargetWindow);
                        string argsText1 = oMyScript.TraceList;
                        var argoColor = Color.WhiteSmoke;
                        var argoBgColor = Color.Transparent;
                        Genie.Game.WindowTarget argoTargetWindow1 = Genie.Game.WindowTarget.Main;
                        string argsTargetWindow = "";
                        AddText(argsText1, argoColor, argoBgColor, oTargetWindow: argoTargetWindow1, sTargetWindow: argsTargetWindow);
                    }
                }
            }
        }

        private void ScriptButtonVars_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem)
            {
                ToolStripMenuItem oMenuItem = (ToolStripMenuItem)sender;
                if (oMenuItem.OwnerItem is ToolStripSplitButton)
                {
                    ToolStripSplitButton oButton = (ToolStripSplitButton)oMenuItem.OwnerItem;
                    if (!Information.IsNothing(oButton.Tag))
                    {
                        Script oMyScript = (Script)oButton.Tag;
                        string argsText = Conversions.ToString(oMyScript.ScriptName + Interaction.IIf(oMyScript.ScriptPaused, "(Paused)", "") + ": " + oMyScript.RunTimeSeconds.ToString("#.#0") + " seconds. " + oMyScript.State + " (" + oMyScript.FileName + ")" + System.Environment.NewLine);
                        Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                        AddText(argsText, oTargetWindow: argoTargetWindow);
                        foreach (string sRow in oMyScript.VariableList.Split(Conversions.ToChar(System.Environment.NewLine)))
                        {
                            AddText(sRow, oTargetWindow: argoTargetWindow);
                        }
                    }
                }
            }
        }

        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        private void InitWorkerThread()
        {
            // m_oWorker.RunWorkerAsync()
            TimerBgWorker.Start();
        }

        // Private Sub BackgroundWorker_OnWork(ByVal sender As Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles m_oWorker.DoWork
        // If System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator <> "." Then
        // System.Threading.Thread.CurrentThread.CurrentCulture = New System.Globalization.CultureInfo("en-US")
        // End If

        // While m_bRunWorker = True
        // Try
        // RunQueueCommand(oGlobals.Events.Poll(), "Event")

        // Dim iSent As Integer = 0
        // Dim sCommandQueue As String = oGlobals.CommandQueue.Poll(HasRoundtime())
        // While sCommandQueue.Length > 0
        // RunQueueCommand(sCommandQueue, "Queue")
        // iSent += 1

        // If iSent >= oGlobals.Config.iTypeAhead Then
        // oGlobals.CommandQueue.SetNextTime(0.5)
        // Exit While
        // End If

        // sCommandQueue = oGlobals.CommandQueue.Poll(HasRoundtime())
        // End While

        // TickScripts()

        // If oGlobals.Config.bShowSpellTimer = True AndAlso oGlobals.oSpellTimeStart <> System.DateTime.MinValue Then
        // SafeSetStatusBarLabels()
        // End If

        // SafeAddScripts()
        // RemoveExitedScripts()

        // 'SafeRemoveExitedScripts()
        // 'SafeAddScripts()

        // System.Threading.Thread.Sleep(10)
        // #If Not Debug Then
        // Catch ex As Exception
        // HandleGenieException("BackgroundWorker Exception: " & ex.ToString)
        // #Else
        // Finally
        // #End If
        // End Try
        // End While
        // End Sub

        public delegate void AddScriptsDelegate();

        private void SafeAddScripts()
        {
            try
            {
                if (IsDisposed)
                {
                    return;
                }

                if (InvokeRequired == true)
                {
                    Invoke(new AddScriptsDelegate(m_oScriptManager.AddScripts));
                }
                else
                {
                    m_oScriptManager.AddScripts();
                }
            }
#pragma warning disable CS0168
            catch (Exception ex)
#pragma warning restore CS0168
            {
            } // Don't care
        }
        // Private Sub AddScripts()
        // Try
        // Monitor.Enter(m_oNewScripts)

        // For Each oItem As Object In m_oNewScripts
        // If TypeOf oItem Is Script Then
        // Dim oScriptAs Script = DirectCast(oItem, Script)
        // If oScriptRef.ScriptDone = False Then
        // AddScriptToToolStrip(oScriptRef)
        // End If
        // End If
        // Next

        // m_oNewScripts.Clear()
        // Catch ex As Exception
        // Throw (ex)
        // Finally
        // Monitor.Exit(m_oNewScripts)
        // End Try
        // End Sub

        // AddScripts moved to ScriptManager
        // RunQueueCommand moved to GameLoop

        private void GameLoop_EndUpdate()
        {
            string argsText = "";
            var argoColor = Color.Transparent;
            var argoBgColor = Color.Transparent;
            Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
            string argsTargetWindow = "";
            AddText(argsText, argoColor, argoBgColor, oTargetWindow: argoTargetWindow, sTargetWindow: argsTargetWindow); // Flush pending text output
            EndUpdate();
        }


        private void TextBoxInput_SendText(string sText)
        {
            try
            {
                m_CommandSent = true;

                string argsText = "";
                var argoColor = Color.Transparent;
                var argoBgColor = Color.Transparent;
                Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                string argsTargetWindow = "";
                m_oCommand.ParseCommand(sText, true, true);
                AddText(argsText, argoColor, argoBgColor, oTargetWindow: argoTargetWindow, sTargetWindow: argsTargetWindow);

                EndUpdate();
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("SendText", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }


        private void SendTextInputBox(string sText)
        {
            if (sText.Contains(@"\x") | sText.Contains("@"))
            {
                SafeParseInputBox(sText);
            }
        }

        private void ClassCommand_SendRaw(string sText)
        {
            m_oGame.SendRaw(sText);
        }

        private void ClassCommand_SendText(string sText, bool bUserInput, string sOrigin)
        {
            try
            {
                sText = sText.Replace(@"\@", "¤");
                if (sText.Contains(@"\x") | sText.Contains("@"))
                {
                    SendTextInputBox(sText);
                }
                else
                {
                    sText = sText.Replace("¤", "@");
                    sText = SafeParsePluginInput(sText);
                    if (sText.Length > 0)
                    {
                        m_CommandSent = true;
                        m_oGame.SendText(sText, bUserInput, sOrigin);
                        if (m_oGlobals.Config.bTriggerOnInput == true)
                        {
                            m_oTriggerEngine.ParseTriggers(sText);
                        }
                        //lastrow 
                    }
                }
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("SendText", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        // ParseTriggers moved to TriggerEngine

        // SetScriptListVariable moved to ScriptManager

        private void Command_EventListScripts(string sFilter)
        {
            try
            {
                if ((sFilter.ToLower() ?? "") == "all")
                {
                    sFilter = string.Empty;
                }

                string argsText = System.Environment.NewLine + "Active scripts: " + System.Environment.NewLine;
                Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                AddText(argsText, oTargetWindow: argoTargetWindow);
                int I = 0;
                // Scripts
                if (m_oScriptManager.ScriptList.AcquireReaderLock())
                {
                    Debug.Print("ScriptList Lock aquired by ListScripts()");
                    try
                    {
                        foreach (Script oScript in m_oScriptManager.ScriptList)
                        {
                            if (oScript.ScriptName.Length > 0)
                            {
                                if (sFilter.Length == 0 | (oScript.ScriptName ?? "") == (sFilter ?? ""))
                                {
                                    string argsText1 = Conversions.ToString(Conversions.ToString(oScript.ScriptName + Interaction.IIf(oScript.ScriptPaused, "(Paused)", "")) + Interaction.IIf(oScript.DebugLevel > 0, " [Debuglevel: " + oScript.DebugLevel.ToString() + "]", "") + ": " + oScript.RunTimeSeconds.ToString("#.#0") + " seconds. " + oScript.State + " (" + oScript.FileName + ")" + System.Environment.NewLine);
                                    Genie.Game.WindowTarget argoTargetWindow1 = Genie.Game.WindowTarget.Main;
                                    AddText(argsText1, oTargetWindow: argoTargetWindow1);
                                    I += 1;
                                }
                            }
                        }

                        if (I == 0)
                        {
                            string argsText2 = "None." + System.Environment.NewLine;
                            Genie.Game.WindowTarget argoTargetWindow2 = Genie.Game.WindowTarget.Main;
                            AddText(argsText2, oTargetWindow: argoTargetWindow2);
                        }
                    }
                    finally
                    {
                        m_oScriptManager.ScriptList.ReleaseReaderLock();
                        Debug.Print("ScriptList Lock released by ListScripts()");
                    }
                }
                else
                {
                    ShowDialogException("ListScripts", "Unable to acquire reader lock.");
                }
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                ClassCommand_EchoText("Error in ListScripts", "Debug");
                ClassCommand_EchoText("---------------------", "Debug");
                ClassCommand_EchoText(ex.Message, "Debug");
                ClassCommand_EchoText("---------------------", "Debug");
                ClassCommand_EchoText(ex.ToString(), "Debug");
                ClassCommand_EchoText("---------------------", "Debug");
            }
        }

        // TriggerAction moved to TriggerEngine

        private void ClassCommand_ParseText(string sText)
        {
            try
            {
                /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
                if (sText.Trim().Length > 0)
                {
                    m_oTriggerEngine.ParseTriggers(sText, false);
                    SafeParsePluginText(sText, "main");
                }
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("ParseText", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        // ClassCommand_RunScript moved to ScriptManager.RunScript

        public void TriggerVariableChanged(string sVariableName) // When variables change
        {
            switch (sVariableName)
            {
                case "$health":
                    {
                        int barValue = Conversions.ToInteger(m_oGlobals.VariableList["health"]);
                        string barText = m_oGlobals.VariableList["healthBarText"].ToString();
                        var bar = ComponentBarsHealth;
                        bar.BarText = barText;
                        SetBarValue(barValue, bar);
                        break;
                    }

                case "$mana":
                    {
                        int barValue = Conversions.ToInteger(m_oGlobals.VariableList["mana"]);
                        string barText = m_oGlobals.VariableList["manaBarText"].ToString();
                        var bar = ComponentBarsMana;
                        bar.BarText = barText;
                        SetBarValue(barValue, bar);
                        break;
                    }

                case "$stamina":
                    {
                        int barValue = Conversions.ToInteger(m_oGlobals.VariableList["stamina"]);
                        string barText = m_oGlobals.VariableList["staminaBarText"].ToString();
                        var bar = ComponentBarsFatigue;
                        bar.BarText = barText;
                        SetBarValue(barValue, bar);
                        break;
                    }

                case "$spirit":
                    {
                        int barValue = Conversions.ToInteger(m_oGlobals.VariableList["spirit"]);
                        string barText = m_oGlobals.VariableList["spiritBarText"].ToString();
                        var bar = ComponentBarsSpirit;
                        bar.BarText = barText;
                        SetBarValue(barValue, bar);
                        break;
                    }

                case "$concentration":
                    {
                        int barValue = Conversions.ToInteger(m_oGlobals.VariableList["concentration"]);
                        string barText = m_oGlobals.VariableList["concentrationBarText"].ToString();
                        var bar = ComponentBarsConc;
                        bar.BarText = barText;
                        SetBarValue(barValue, bar);
                        break;
                    }

                case "compass":
                case "$north":
                case "$northeast":
                case "$east":
                case "$southeast":
                case "$south":
                case "$southwest":
                case "$west":
                case "$northwest":
                case "$up":
                case "$down":
                case "$out":
                    {
                        IconBar.PictureBoxCompass.Invalidate();
                        return; // Block direction triggers (They clear before changing.)
                    }

                case "$dead":
                case "$standing":
                case "$kneeling":
                case "$sitting":
                case "$prone":
                    {
                        IconBar.UpdateStatusBox();
                        break;
                    }

                case "$stunned":
                    {
                        IconBar.UpdateStunned();
                        break;
                    }

                case "$bleeding":
                    {
                        IconBar.UpdateBleeding();
                        break;
                    }

                case "$invisible":
                    {
                        IconBar.UpdateInvisible();
                        break;
                    }

                case "$hidden":
                    {
                        IconBar.UpdateHidden();
                        break;
                    }

                case "$joined":
                    {
                        IconBar.UpdateJoined();
                        break;
                    }

                case "$webbed":
                    {
                        IconBar.UpdateWebbed();
                        break;
                    }

                case "$connected":
                    {
                        string argsValue = Conversions.ToString(m_oGlobals.VariableList["connected"]);
                        bool bConnected = Utility.StringToBoolean(argsValue);
                        ComponentBarsHealth.IsConnected = bConnected;
                        ComponentBarsMana.IsConnected = bConnected;
                        ComponentBarsFatigue.IsConnected = bConnected;
                        ComponentBarsSpirit.IsConnected = bConnected;
                        ComponentBarsConc.IsConnected = bConnected;
                        IconBar.IsConnected = bConnected;
                        oRTControl.IsConnected = bConnected;
                        Castbar.IsConnected = bConnected;
                        m_CommandSent = false;
                        m_oGlobals.VariableList["charactername"] = m_oGame.AccountCharacter;
                        m_oGlobals.VariableList["game"] = m_oGame.AccountGame;
                        m_oGlobals.VariableList["gamename"] = m_oGame.AccountGame;
                        m_oAutoMapper.CharacterName = m_oGame.AccountCharacter;
                        m_sCurrentProfileName = m_oGame.AccountCharacter + m_oGame.AccountGame + ".xml";
                        m_oGame.ResetIndicators();
                        IconBar.UpdateStatusBox();
                        IconBar.UpdateStunned();
                        IconBar.UpdateBleeding();
                        IconBar.UpdateInvisible();
                        IconBar.UpdateHidden();
                        IconBar.UpdateJoined();
                        IconBar.UpdateWebbed();
                        if (m_oGame.IsConnectedToGame)
                        {
                            if (!string.IsNullOrWhiteSpace(m_oGlobals.Config.ConnectScript)) ClassCommand_SendText(m_oGlobals.Config.ScriptChar + m_oGlobals.Config.ConnectScript, false, "Connected");
                            if (m_oGlobals.VariableList.ContainsKey("connectscript")) ClassCommand_SendText(m_oGlobals.Config.ScriptChar + m_oGlobals.Config.ConnectScript, false, "Connected");
                        }
                        SafeUpdateMainWindowTitle();
                        break;
                    }

                case "$prompt": // Safety
                    {
                        IconBar.UpdateBleeding();
                        break;
                    }

                case "$charactername":
                    {
                        SafeUpdateMainWindowTitle();
                        m_oAutoMapper.CharacterName = m_oGlobals.VariableList["charactername"].ToString();
                        m_oGame.AccountCharacter = m_oGlobals.VariableList["charactername"].ToString();
                        if (m_oGlobals.VariableList["charactername"].ToString().Length > 0)
                        {
                            m_sCurrentProfileName = m_oGame.AccountCharacter + m_oGame.AccountGame + ".xml";
                        }

                        break;
                    }
                /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
                case "$gamename":
                    {
                        // SafeLoadProfile(oGlobals.VariableList("charactername").ToString & oGlobals.VariableList("gamename").ToString & ".xml", False)
                        SafeUpdateMainWindowTitle();
                        break;
                    }
            }

            m_oTriggerEngine.EvalTriggers(sVariableName);

            if (m_oTriggerEngine.TriggersEnabled)
            {
                if (m_oScriptManager.ScriptList.AcquireReaderLock())
                {
                    try
                    {
                        foreach (Script oScript in m_oScriptManager.ScriptList)
                            oScript.TriggerVariableChanged(sVariableName);
                    }
                    catch (Exception ex)
                    {
                        ClassCommand_EchoText("Error in TriggerVariableChange", "Debug");
                        ClassCommand_EchoText("---------------------", "Debug");
                        ClassCommand_EchoText(ex.Message, "Debug");
                        ClassCommand_EchoText("---------------------", "Debug");
                        ClassCommand_EchoText(ex.ToString(), "Debug");
                        ClassCommand_EchoText("---------------------", "Debug");
                    }
                    finally
                    {
                        m_oScriptManager.ScriptList.ReleaseReaderLock();
                    }
                }
                else
                {
                    ShowDialogException("TriggerVariableChanged", "Unable to acquire reader lock.");
                }
            }
        }

        public delegate void UpdateMainWindowTitleDelegate();

        private void SafeUpdateMainWindowTitle()
        {
            if (InvokeRequired == true)
            {
                Invoke(new UpdateMainWindowTitleDelegate(UpdateMainWindowTitle));
            }
            else
            {
                UpdateMainWindowTitle();
            }
        }

        private void UpdateMainWindowTitle()
        {
            string strTitle = string.Empty;
            if (!Information.IsNothing(m_oGlobals.VariableList["gamename"]) && m_oGlobals.VariableList["gamename"].ToString().Length > 0)
            {
                strTitle += m_oGlobals.VariableList["gamename"].ToString() + ": ";
            }

            if (!Information.IsNothing(m_oGlobals.VariableList["charactername"]) && m_oGlobals.VariableList["charactername"].ToString().Length > 0)
            {
                strTitle += m_oGlobals.VariableList["charactername"].ToString() + " ";
            }

            if (m_oGame.IsConnected)
            {
                strTitle += "[Connected]" + " - ";
            }
            else
            {
                strTitle += "[Not connected]" + " - ";
            }

            strTitle += "Genie " + My.MyProject.Application.Info.Version.ToString();
            Text = strTitle;
        }

        private void PluginHost_EventVariableChanged(string sVariable)
        {
            try
            {
                TriggerVariableChanged(sVariable);
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("VariableChanged", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        private void ClassCommand_EventVariableChanged(string sVariable)
        {
            try
            {
                TriggerVariableChanged(sVariable);
                SafeParsePluginVariable(sVariable);
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("VariableChanged", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        private void PrintError(string sText)
        {
            string argsText = sText + System.Environment.NewLine;
            var argoColor = Color.Red;
            var argoBgColor = Color.Transparent;
            Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
            string argsTargetWindow = "";
            AddText(argsText, argoColor, argoBgColor, oTargetWindow: argoTargetWindow, sTargetWindow: argsTargetWindow);
        }


        private void Simutronics_EventEndUpdate()
        {
            try
            {
                string argsText = "";
                var argoColor = Color.Transparent;
                var argoBgColor = Color.Transparent;
                Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                string argsTargetWindow = "";
                AddText(argsText, argoColor, argoBgColor, oTargetWindow: argoTargetWindow, sTargetWindow: argsTargetWindow); // For some stupid reason we need this. Probably because EndUpdate is fired before we are ready in the other thread.
                EndUpdate();
                m_oGame.SetBufferEnd();
                m_oScriptManager.SetBufferEndForScripts();
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("EndUpdate", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }


        private void Command_ScriptVariables(string sScript, string sFilter)
        {
            try
            {
                if ((sScript.ToLower() ?? "") == "all")
                {
                    sScript = string.Empty;
                }

                // Scripts
                if (m_oScriptManager.ScriptList.AcquireReaderLock())
                {
                    Debug.Print("ScriptList Lock aquired by ScriptTrace()");
                    try
                    {
                        foreach (Script oScript in m_oScriptManager.ScriptList)
                        {
                            if (oScript.ScriptName.Length > 0)
                            {
                                if (sScript.Length == 0 | (oScript.ScriptName ?? "") == (sScript ?? ""))
                                {
                                    string argsText = Conversions.ToString(oScript.ScriptName + Interaction.IIf(oScript.ScriptPaused, "(Paused)", "") + ": " + oScript.RunTimeSeconds.ToString("#.#0") + " seconds. " + oScript.State + " (" + oScript.FileName + ")" + System.Environment.NewLine);
                                    Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                                    AddText(argsText, oTargetWindow: argoTargetWindow);
                                    foreach (string sRow in oScript.VariableList.Split(Conversions.ToChar(System.Environment.NewLine)))
                                    {
                                        if (sFilter.Length == 0 | sRow.ToLower().Contains(sFilter.ToLower()))
                                        {
                                            Genie.Game.WindowTarget argoTargetWindow1 = Genie.Game.WindowTarget.Main;
                                            AddText(sRow, oTargetWindow: argoTargetWindow1);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        m_oScriptManager.ScriptList.ReleaseReaderLock();
                        Debug.Print("ScriptList Lock released by ScriptTrace()");
                    }
                }
                else
                {
                    ShowDialogException("ScriptVariables", "Unable to acquire reader lock.");
                }
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("ScriptVariables", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        private void Command_ScriptTrace(string sScript)
        {
            try
            {
                if ((sScript.ToLower() ?? "") == "all")
                {
                    sScript = string.Empty;
                }

                // Scripts
                if (m_oScriptManager.ScriptList.AcquireReaderLock())
                {
                    Debug.Print("ScriptList Lock aquired by ScriptTrace()");
                    try
                    {
                        foreach (Script oScript in m_oScriptManager.ScriptList)
                        {
                            if (oScript.ScriptName.Length > 0)
                            {
                                if (sScript.Length == 0 | (oScript.ScriptName ?? "") == (sScript ?? ""))
                                {
                                    string argsText = Conversions.ToString(oScript.ScriptName + Interaction.IIf(oScript.ScriptPaused, "(Paused)", "") + ": " + oScript.RunTimeSeconds.ToString("#.#0") + " seconds. " + oScript.State + " (" + oScript.FileName + ")" + System.Environment.NewLine);
                                    Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                                    AddText(argsText, oTargetWindow: argoTargetWindow);
                                    string argsText1 = oScript.TraceList + System.Environment.NewLine;
                                    Genie.Game.WindowTarget argoTargetWindow1 = Genie.Game.WindowTarget.Main;
                                    AddText(argsText1, oTargetWindow: argoTargetWindow1);
                                }
                            }
                        }
                    }
                    finally
                    {
                        m_oScriptManager.ScriptList.ReleaseReaderLock();
                        Debug.Print("ScriptList Lock released by ScriptTrace()");
                    }
                }
                else
                {
                    ShowDialogException("ScriptTrace", "Unable to acquire reader lock.");
                }
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("ScriptTrace", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        // Command_ScriptAbort, Command_ScriptPause, Command_ScriptPauseOrResume,
        // Command_ScriptReload, Command_ScriptResume, Command_ScriptDebugLevel
        // moved to ScriptManager


        public delegate void PrintDialogExceptionDelegate(string section, string message, string description);

        private void HandleGenieException(string section, string message, string description = null)
        {
            if (InvokeRequired == true)
            {
                var parameters = new[] { section, message, description };
                Invoke(new PrintDialogExceptionDelegate(ShowDialogException), parameters);
            }
            else
            {
                ShowDialogException(section, message, description);
            }
        }

        private void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ShowDialogException("Main", e.Exception.Message, e.Exception.ToString());
        }

        public delegate void ShowDialogExceptionDelegate(string section, string message, string description = null);
        private void ShowDialogException(string section, string message, string description = null)
        {
            if (InvokeRequired == true)
            {
                var parameters = new object[] { section, message, description };
                Invoke(new ShowDialogExceptionDelegate(ThreadSafeShowDialogException), parameters);
            }
            else
            {
                ThreadSafeShowDialogException(section, message, description);
            }

        }
        private void ThreadSafeShowDialogException(string section, string message, string description = null)
        {
            if (My.MyProject.Forms.DialogException.Visible == false)
            {
                var sbDetails = new StringBuilder();
                sbDetails.Append("Action:                ");
                sbDetails.Append(section);
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append(message);
                if (!Information.IsNothing(description))
                {
                    sbDetails.Append(System.Environment.NewLine);
                    sbDetails.Append(System.Environment.NewLine);
                    sbDetails.Append("----------------------------------------------");
                    sbDetails.Append(System.Environment.NewLine);
                    sbDetails.Append(description);
                }

                My.MyProject.Forms.DialogException.Show(this, section + System.Environment.NewLine + message + System.Environment.NewLine + System.Environment.NewLine + description);
            }
        }


        private void Game_EventTriggerParse(string sText)
        {
            try
            {
                m_oTriggerEngine.ParseTriggers(sText);
            }
            // SafeParsePluginText(sText)
            // Debug.WriteLine("ClassCommand_SendText)")
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("TriggerParse", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        private void Game_EventStatusBarUpdate()
        {
            try
            {
                SafeSetStatusBarLabels();
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("StatusBarUpdate", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        public delegate void SetStatusBarLabelsDelegate();

        private void SafeSetStatusBarLabels()
        {
            if (InvokeRequired == true)
            {
                Invoke(new SetStatusBarLabelsDelegate(SetStatusBarLabels));
            }
            else
            {
                SetStatusBarLabels();
            }
        }

        private void SetStatusBarLabels()
        {
            LabelLHC.Text = Conversions.ToString(m_oGlobals.VariableList["lefthand"]);
            LabelRHC.Text = Conversions.ToString(m_oGlobals.VariableList["righthand"]);
            if (m_oGlobals.Config.bShowSpellTimer == true && m_oGlobals.SpellTimeStart != DateTime.MinValue)
            {
                var argoDateEnd = DateTime.Now;
                LabelSpellC.Text = Conversions.ToString("(" + Utility.GetTimeDiffInSeconds(m_oGlobals.SpellTimeStart, argoDateEnd) + ") " + m_oGlobals.VariableList["preparedspell"]);
            }
            else
            {
                LabelSpellC.Text = Conversions.ToString(m_oGlobals.VariableList["preparedspell"]);
            }
        }

        public delegate void ClearSpellTimeDelegate();

        private void Game_EventClearSpellTime()
        {
            try
            {
                if (InvokeRequired == true)
                {
                    Invoke(new ClearSpellTimeDelegate(ClearSpellTime));
                }
                else
                {
                    ClearSpellTime();
                }
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("ClearSpellTime", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        public delegate void SetSpellTimeDelegate();

        private void Game_EventSpellTime()
        {
            try
            {
                if (InvokeRequired == true)
                {
                    Invoke(new SetSpellTimeDelegate(SetSpellTime));
                }
                else
                {
                    SetSpellTime();
                }
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("SpellTime", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        private void SetSpellTime()
        {
            m_oGlobals.SpellTimeStart = DateTime.Now;
            m_oGlobals.VariableList["spellstarttime"] = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds().ToString();
        }

        private void ClearSpellTime()
        {
            m_oGlobals.SpellTimeStart = default;
        }

        private void SetCastTime()
        {
            int gameTime;
            int castTime;
            if (int.TryParse(m_oGlobals.VariableList["gametime"].ToString(), out gameTime) && int.TryParse(m_oGlobals.VariableList["casttime"].ToString(), out castTime) && m_oGlobals.VariableList["preparedspell"].ToString() != "None")
            {
                Castbar.SetRT(castTime - gameTime);
            }
            else
            {
                Castbar.SetRT(0);
            }
        }

        public delegate void SetRoundtimeDelegate(int iTime);

        private void Game_EventRoundtime(int iTime)
        {
            try
            {
                if (InvokeRequired == true)
                {
                    Invoke(new SetRoundtimeDelegate(SetRoundTime), iTime);
                }
                else
                {
                    SetRoundTime(iTime);
                }
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("RoundTime", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }
        public delegate void SetCastTimeDelegate();
        private void Game_EventCastTime()
        {
            try
            {
                if (InvokeRequired == true)
                {
                    Invoke(new SetCastTimeDelegate(SetCastTime));
                }
                else
                {
                    SetCastTime();
                }
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("CastTime", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        public delegate void SetBarValueDelegate(int iValue, ComponentBars oBar);

        private void SetBarValue(int iValue, ComponentBars oBar)
        {
            if (InvokeRequired == true)
            {
                var parameters = new object[] { iValue, oBar };
                Invoke(new SetBarValueDelegate(InvokeSetBarValue), parameters);
            }
            else
            {
                InvokeSetBarValue(iValue, oBar);
            }
        }

        private void InvokeSetBarValue(int iValue, ComponentBars oBar)
        {
            oBar.Value = iValue;
        }

        public delegate void SetStatusBarDelegate(string sText, ToolStripStatusLabel oLabel);

        private void SafeSetStatusBar(string sText, ToolStripStatusLabel oLabel)
        {
            if (InvokeRequired == true)
            {
                var parameters = new object[] { sText, oLabel };
                Invoke(new SetStatusBarDelegate(SetStatusBar), parameters);
            }
            else
            {
                SetStatusBar(sText, oLabel);
            }
        }

        private void SetStatusBar(string sText, ToolStripStatusLabel oLabel)
        {
            if (oLabel.Visible == false)
            {
                oLabel.Visible = true;
            }

            oLabel.Text = sText;
        }

        private void Script_EventSendText(string Text, string Script, bool ToQueue, bool DoCommand)
        {
            try
            {
                bool bSendText = true;
                if (Text.StartsWith(Conversions.ToString(m_oGlobals.Config.cCommandChar))) // Don't send text to game that start with #
                {
                    bSendText = false;
                }
                if (!ToQueue)
                {
                    m_oCommand.ParseCommand(Text, bSendText, false, Script);
                }
                else
                {
                    int iPauseTime = 0;
                    string sNumber = string.Empty;
                    foreach (char c in Text.ToCharArray())
                    {
                        if (Information.IsNumeric(c) | c == '.')
                        {
                            sNumber += Conversions.ToString(c);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (sNumber.Length > 0 & (sNumber ?? "") != ".")
                    {
                        Text = Text.Substring(sNumber.Length).Trim();
                        iPauseTime = int.Parse(sNumber);
                    }

                    double argdDelay = iPauseTime;
                    string argsAction = m_oGlobals.ParseGlobalVars(Text);
                    m_oGlobals.CommandQueue.AddToQueue(argdDelay, argsAction, true, DoCommand, DoCommand);
                }
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("SendText", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        // Game_EventTriggerPrompt and Game_EventTriggerMove moved to ScriptManager
        // HasRoundTime moved to GameLoop


        private void SetRoundTime(int iTime)
        {
            if (iTime == 0)
                return;
            if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(oRTControl.CurrentRT, 0, false) | iTime > oRTControl.CurrentRT + 1))
            {
                oRTControl.SetRT((int)(iTime + m_oGlobals.Config.dRTOffset));
            }

            m_oScriptManager.SetRoundTimeForScripts(iTime);

            m_oGlobals.RoundTimeEnd = DateTime.Now.AddMilliseconds(iTime * 1000 + m_oGlobals.Config.dRTOffset * 1000);
        }

        public void InputKeyDown(KeyEventArgs e)
        {
            if (bSendingKey == true)
                return;
            bSendingKey = true;
            string sKeyString = string.Empty;
            TextBoxInput.Focus();
            var switchExpr = e.KeyData;
            switch (switchExpr)
            {
                case Keys.Back:
                    {
                        sKeyString = "{BACKSPACE}";
                        break;
                    }

                case Keys.Delete:
                    {
                        sKeyString = "{DELETE}";
                        break;
                    }

                case Keys.Up:
                    {
                        sKeyString = "{UP}";
                        break;
                    }

                case Keys.Down:
                    {
                        sKeyString = "{DOWN}";
                        break;
                    }

                case Keys.Left:
                    {
                        sKeyString = "{LEFT}";
                        break;
                    }

                case Keys.Right:
                    {
                        sKeyString = "{RIGHT}";
                        break;
                    }
            }

            if (sKeyString.Length > 0)
            {
                SendKeys.SendWait(sKeyString);
            }

            bSendingKey = false;
        }

        public void InputKeyPress(KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ' | char.IsControl(e.KeyChar) == false & char.IsWhiteSpace(e.KeyChar) == false)
            {
                TextBoxInput.SelectedText = Conversions.ToString(e.KeyChar);
            }

            TextBoxInput.Focus();
        }

        private void AbortAllScriptsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AbortAllScripts();
        }

        private void AbortAllScripts()
        {
            if (m_oScriptManager.ScriptList.AcquireReaderLock())
            {
                try
                {
                    foreach (Script oScript in m_oScriptManager.ScriptList)
                        oScript.AbortScript();
                }
                finally
                {
                    m_oScriptManager.ScriptList.ReleaseReaderLock();
                }
            }
            else
            {
                ShowDialogException("AbortAllScripts", "Unable to acquire reader lock.");
            }
        }

        private void PauseAllScriptsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PauseAllScripts();
        }

        private void PauseAllScripts()
        {
            m_oScriptManager.ScriptPause("");
        }

        private void ResumeAllScriptsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResumeAllScripts();
        }

        private void ResumeAllScripts()
        {
            m_oScriptManager.ScriptResume("");
        }

        private void ChangelogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utility.OpenBrowser("https://github.com/GenieClient/Genie4/releases/latest");
        }


        private FormSkin oCurrentActiveForm = null;

        public FormSkin ActiveFormSkin
        {
            get
            {
                return oCurrentActiveForm;
            }

            set
            {
                oCurrentActiveForm = value;
            }
        }

        private bool bSendingKey = false;

        private void TextBoxInput_PageUp()
        {
            if (bSendingKey == true)
                return;
            bSendingKey = true;
            if (!Information.IsNothing(oCurrentActiveForm))
            {
                oCurrentActiveForm.RichTextBoxOutput.Focus();
                SendKeys.SendWait("{PGUP}");
                TextBoxInput.Focus();
            }

            bSendingKey = false;
        }

        private void TextBoxInput_PageDown()
        {
            if (bSendingKey == true)
                return;
            bSendingKey = true;
            if (!Information.IsNothing(oCurrentActiveForm))
            {
                oCurrentActiveForm.RichTextBoxOutput.Focus();
                SendKeys.SendWait("{PGDN}");
                TextBoxInput.Focus();
            }

            bSendingKey = false;
        }

        private void TextBoxInput_CtrlPageUp()
        {
            if (bSendingKey == true)
                return;
            bSendingKey = true;
            if (!Information.IsNothing(oCurrentActiveForm))
            {
                oCurrentActiveForm.RichTextBoxOutput.Focus();
                oCurrentActiveForm.RichTextBoxOutput.Select(0, 0);
                TextBoxInput.Focus();
            }

            bSendingKey = false;
        }

        private void TextBoxInput_CtrlPageDown()
        {
            if (bSendingKey == true)
                return;
            bSendingKey = true;
            if (!Information.IsNothing(oCurrentActiveForm))
            {
                oCurrentActiveForm.RichTextBoxOutput.Focus();
                oCurrentActiveForm.RichTextBoxOutput.Select(oCurrentActiveForm.RichTextBoxOutput.Text.Length, 0);
                TextBoxInput.Focus();
            }

            bSendingKey = false;
        }

        private void ToolStripMenuItemSpecialPaste_Click(object sender, EventArgs e)
        {
            TextBoxInput.Focus();
            if (Clipboard.ContainsText() == true)
            {
                TextBoxInput.Paste(Clipboard.GetText().Replace(Constants.vbCr, ";").Replace(Constants.vbLf, "").TrimEnd());
            }
        }

        private void ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var x = DateTime.Now;
            for (int I = 0; I <= 100; I++)
            {
                string argsText = "Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum." + System.Environment.NewLine;
                bool argbIsPrompt = false;
                Genie.Game.WindowTarget argoWindowTarget = 0;
                m_oGame.PrintTextWithParse(argsText, bIsPrompt: argbIsPrompt, oWindowTarget: argoWindowTarget);
                string argsText1 = "Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum." + System.Environment.NewLine + System.Environment.NewLine;
                bool argbIsPrompt1 = false;
                Genie.Game.WindowTarget argoWindowTarget1 = 0;
                m_oGame.PrintTextWithParse(argsText1, bIsPrompt: argbIsPrompt1, oWindowTarget: argoWindowTarget1);
            }

            var y = DateTime.Now;
            var dur = new TimeSpan();
            dur = y - x;
            string argsText2 = "Total duration: " + dur.TotalMilliseconds.ToString() + " milliseconds." + System.Environment.NewLine;
            var argoColor = Color.Green;
            var argoBgColor = Color.Transparent;
            Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
            string argsTargetWindow = "";
            AddText(argsText2, argoColor, argoBgColor, oTargetWindow: argoTargetWindow, sTargetWindow: argsTargetWindow);
        }

        private void ToolStripMenuItemTriggers_Click(object sender, EventArgs e)
        {
            m_oTriggerEngine.TriggersEnabled = ToolStripMenuItemTriggers.Checked;
        }

        private void ToolStripMenuItemShowXML_Click(object sender, EventArgs e)
        {
            m_oGame.ShowRawOutput = ToolStripMenuItemShowXML.Checked;
            if (m_oGame.ShowRawOutput)
                AddWindow("Raw");
        }

        private bool m_bVersionUpdated = false;
        private bool m_bIsLoading = true;
        private bool m_bDebugPlugin = false;

        public delegate void UpdateMonoFontDelegate();

        private void SafeUpdateMonoFont()
        {
            if (InvokeRequired == true)
            {
                Invoke(new UpdateMonoFontDelegate(UpdateMonoFont));
            }
            else
            {
                UpdateMonoFont();
            }
        }

        private void UpdateMonoFont()
        {
            m_oOutputMain.RichTextBoxOutput.MonoFont = m_oGlobals.Config.MonoFont.ToDrawingFont();
            var oEnumerator = m_oFormList.GetEnumerator();
            while (oEnumerator.MoveNext())
            {
                FormSkin oForm = (FormSkin)oEnumerator.Current;
                oForm.RichTextBoxOutput.MonoFont = m_oGlobals.Config.MonoFont.ToDrawingFont();
            }
        }

        public delegate void UpdateInputFontDelegate();

        private void SafeUpdateInputFont()
        {
            if (InvokeRequired == true)
            {
                Invoke(new UpdateInputFontDelegate(UpdateInputFont));
            }
            else
            {
                UpdateInputFont();
            }
        }

        private void UpdateInputFont()
        {
            TextBoxInput.Font = m_oGlobals.Config.InputFont.ToDrawingFont();
            PanelInput.Height = TextBoxInput.FontHeight + 6;
        }

        private void Config_ConfigChanged(Genie.Config.ConfigFieldUpdated oField)
        {
            switch (oField)
            {
                case Genie.Config.ConfigFieldUpdated.MonoFont:
                    {
                        SafeUpdateMonoFont();
                        break;
                    }

                case Genie.Config.ConfigFieldUpdated.InputFont:
                    {
                        SafeUpdateInputFont();
                        break;
                    }

                case Genie.Config.ConfigFieldUpdated.Autolog:
                    {
                        AutoLogToolStripMenuItem.Checked = m_oGlobals.Config.bAutoLog;
                        break;
                    }
                case Genie.Config.ConfigFieldUpdated.ClassicConnect:
                    {
                        ClassicConnectToolStripMenuItem.Checked = m_oGlobals.Config.bClassicConnect;
                        break;
                    }

                case Genie.Config.ConfigFieldUpdated.Reconnect:
                    {
                        AutoReconnectToolStripMenuItem.Checked = m_oGlobals.Config.bReconnect;
                        break;
                    }

                case Genie.Config.ConfigFieldUpdated.KeepInput:
                    {
                        TextBoxInput.KeepInput = m_oGlobals.Config.bKeepInput;
                        break;
                    }

                case Genie.Config.ConfigFieldUpdated.Muted:
                    {
                        MuteSoundsToolStripMenuItem.Checked = !m_oGlobals.Config.bPlaySounds;
                        break;
                    }

                case Genie.Config.ConfigFieldUpdated.AutoMapper:
                    {
                        AutoMapperEnabledToolStripMenuItem.Checked = m_oGlobals.Config.bAutoMapper;
                        m_oAutoMapper.UpdatePanelBackgroundColor();
                        break;
                    }

                case Genie.Config.ConfigFieldUpdated.LogDir:
                    {
                        m_oGlobals.Log.LogDirectory = m_oGlobals.Config.sLogDir;
                        break;
                    }

                case Genie.Config.ConfigFieldUpdated.CheckForUpdates:
                    {
                        checkUpdatesOnStartupToolStripMenuItem.Checked = m_oGlobals.Config.CheckForUpdates;
                        break;
                    }

                case Genie.Config.ConfigFieldUpdated.AutoUpdate:
                    {
                        autoUpdateToolStripMenuItem.Checked = m_oGlobals.Config.AutoUpdate;
                        break;
                    }
                case Genie.Config.ConfigFieldUpdated.AutoUpdateLamp:
                    {
                        autoUpdateLampToolStripMenuItem.Checked = m_oGlobals.Config.AutoUpdateLamp;
                        break;
                    }
                case Genie.Config.ConfigFieldUpdated.ImagesEnabled:
                    {
                        _ImagesEnabledToolStripMenuItem.Checked = m_oGlobals.Config.bShowImages;
                        break;
                    }
                case Genie.Config.ConfigFieldUpdated.SizeInputToGame:
                    {
                        alignInputToGameWindowToolStripMenuItem.Checked = m_oGlobals.Config.SizeInputToGame;
                        break;
                    }

                case Genie.Config.ConfigFieldUpdated.UpdateMapperScripts:
                    {
                        updateScriptsWithMapsToolStripMenuItem.Checked = m_oGlobals.Config.UpdateMapperScripts;
                        break;
                    }
                case Genie.Config.ConfigFieldUpdated.AlwaysOnTop:
                    {
                        alwaysOnTopToolStripMenuItem.Checked = m_oGlobals.Config.AlwaysOnTop;
                        break;
                    }
            }
        }

        private void Command_EventClassChange()
        {
            var al = new ArrayList();
            if (m_oGlobals.ClassList.AcquireReaderLock())
            {
                try
                {
                    foreach (object key in m_oGlobals.ClassList.Keys)
                        al.Add(key);
                }
                finally
                {
                    m_oGlobals.ClassList.ReleaseReaderLock();
                    foreach (string k in al)
                    {
                        var key = k;

                        // Highlights
                        bool bState = bool.Parse(Conversions.ToString(m_oGlobals.ClassList[key]));
                        if ((key ?? "") == "default")
                            key = "";
                        m_oGlobals.HighlightList.ToggleClass(key, bState);
                        m_oGlobals.HighlightList.RebuildLineIndex();
                        m_oGlobals.HighlightList.RebuildStringIndex();
                        m_oGlobals.HighlightBeginsWithList.ToggleClass(key, bState);
                        m_oGlobals.HighlightRegExpList.ToggleClass(key, bState);

                        // Triggers
                        m_oGlobals.TriggerList.ToggleClass(key, bState);

                        // Subs
                        m_oGlobals.SubstituteList.ToggleClass(key, bState);

                        // Gags
                        m_oGlobals.GagList.ToggleClass(key, bState);
                    }
                }
            }
            else
            {
                throw new Exception("Unable to aquire reader lock.");
            }
        }

        private void LoadSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadLayout(m_sConfigFile);
        }

        private void AutoReconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_oGlobals.Config.bReconnect = AutoReconnectToolStripMenuItem.Checked;
        }

        private void ClassicConnectToolStripMenuItem_Click(global::System.Object sender, global::System.EventArgs e)
        {
            m_oGlobals.Config.bClassicConnect = ClassicConnectToolStripMenuItem.Checked;
        }

        private void TimerReconnect_Tick(object sender, EventArgs e)
        {
            CheckReconnect();
            CheckUserIdleTime();
            CheckServerIdleTime();
        }

        private const int ResponseTimeoutServer = 10;
        private const int ResponseTimeoutUser = 60;
        private bool m_bCheckServerResponse = false;
        private bool m_bCheckUserResponse = false;

        private void CheckServerIdleTime()
        {
            if (!m_oGame.IsConnected)
                return;
            if (m_oGlobals.Config.iServerActivityTimeout == 0)
                return;
            var argoDateStart = m_oGame.LastServerActivity;
            var argoDateEnd = DateTime.Now;
            int iDiff = Utility.GetTimeDiffInSeconds(argoDateStart, argoDateEnd);
            if (m_bCheckServerResponse)
            {
                if (iDiff >= m_oGlobals.Config.iServerActivityTimeout)
                {
                    if (iDiff >= m_oGlobals.Config.iServerActivityTimeout + ResponseTimeoutServer)
                    {
                        m_bCheckServerResponse = false;
                        if (m_oGlobals.Config.bReconnect == true)
                        {
                            string argsText = Utility.GetTimeStamp() + " Connection timeout. Attempting to reconnect.";
                            PrintError(argsText);
                            m_oGame.ReconnectTime = DateTime.Now;
                        }
                        else
                        {
                            string argsText1 = Utility.GetTimeStamp() + " Connection timeout.";
                            PrintError(argsText1);
                            m_oGame.Disconnect();
                        }
                    }
                }
                else
                {
                    /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
                    m_bCheckServerResponse = false;
                }
            }
            else if (iDiff >= m_oGlobals.Config.iServerActivityTimeout)
            {
                /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
                m_bCheckServerResponse = true;
                m_oGame.SendRaw(m_oGlobals.Config.sServerActivityCommand + System.Environment.NewLine);
            }
        }

        private void CheckUserIdleTime()
        {
            if (Information.IsNothing(m_oGame))
                return;
            if (!m_oGame.IsConnected)
                return;
            if (m_oGlobals.Config.iUserActivityTimeout == 0)
                return;
            var argoDateStart = m_oGame.LastUserActivity;
            var argoDateEnd = DateTime.Now;
            int iDiff = Utility.GetTimeDiffInSeconds(argoDateStart, argoDateEnd);
            if (m_bCheckUserResponse)
            {
                if (iDiff >= m_oGlobals.Config.iUserActivityTimeout)
                {
                    if (iDiff >= m_oGlobals.Config.iUserActivityTimeout + ResponseTimeoutUser)
                    {
                        m_oGame.SendText(m_oGlobals.Config.sUserActivityCommand, false, "IDLE TIMER");
                    }
                }
                else
                {
                    /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
                    string argkey = "useridle";
                    string argvalue = "0";
                    m_oGlobals.VariableList.Add(argkey, argvalue, Genie.Globals.Variables.VariableType.Ignore);
                    TriggerVariableChanged("$useridle");
                    m_bCheckUserResponse = false;
                }
            }
            else if (iDiff >= m_oGlobals.Config.iUserActivityTimeout)
            {
                string argsText = System.Environment.NewLine + Utility.GetTimeStamp() + " GENIE HAS FLAGGED YOU AS IDLE. PLEASE RESPOND!";
                PrintError(argsText);
                string argkey1 = "useridle";
                string argvalue1 = "1";
                m_oGlobals.VariableList.Add(argkey1, argvalue1, Genie.Globals.Variables.VariableType.Reserved);
                TriggerVariableChanged("$useridle");
                SafeFlashWindow();
                Interaction.Beep();
                m_bCheckUserResponse = true;
            }
        }

        private void ListAllScriptsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Command_EventListScripts("");
        }

        private void TraceAllScriptsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string argsScript = "";
            Command_ScriptTrace(argsScript);
        }

        public delegate void PresetChangedDelegate(string sPreset);

        public void SafePresetChanged(string sPreset)
        {
            if (InvokeRequired == true)
            {
                var parameters = new[] { sPreset };
                Invoke(new PresetChangedDelegate(PresetChanged), parameters);
            }
            else
            {
                PresetChanged(sPreset);
            }
        }

        private void PresetChanged(string sPreset)
        {
            try
            {
                if (!Information.IsNothing(m_oGlobals.PresetList))
                {
                    if (sPreset.StartsWith("automapper")) { sPreset = "automapper"; }
                    if (sPreset.StartsWith("ui")) { sPreset = "ui"; }
                    switch (sPreset)
                    {
                        case "roundtime":
                            {
                                oRTControl.ForegroundColor = m_oGlobals.PresetList["roundtime"].FgColor.ToDrawingColor();
                                oRTControl.BackgroundColorRT = m_oGlobals.PresetList["roundtime"].BgColor.ToDrawingColor();
                                oRTControl.Refresh();
                                break;
                            }

                        case "castbar":
                            {
                                Castbar.ForegroundColor = m_oGlobals.PresetList["castbar"].FgColor.ToDrawingColor();
                                Castbar.BackgroundColorRT = m_oGlobals.PresetList["castbar"].BgColor.ToDrawingColor();
                                Castbar.Refresh();
                                break;
                            }
                        case "automapper":
                            {
                                m_oAutoMapper.UpdatePanelBackgroundColor();
                                break;
                            }
                        case "ui":
                            {
                                RecolorUI();
                                break;
                            }
                        case "health":
                            {
                                ComponentBarsHealth.ForegroundColor = m_oGlobals.PresetList["health"].FgColor.ToDrawingColor();
                                ComponentBarsHealth.BackgroundColor = m_oGlobals.PresetList["health"].BgColor.ToDrawingColor();
                                ComponentBarsHealth.BorderColor = m_oGlobals.PresetList["health"].BgColor.ToDrawingColor();
                                ComponentBarsHealth.Refresh();
                                break;
                            }

                        case "mana":
                            {
                                ComponentBarsMana.ForegroundColor = m_oGlobals.PresetList["mana"].FgColor.ToDrawingColor();
                                ComponentBarsMana.BackgroundColor = m_oGlobals.PresetList["mana"].BgColor.ToDrawingColor();
                                ComponentBarsMana.BorderColor = m_oGlobals.PresetList["mana"].BgColor.ToDrawingColor();
                                ComponentBarsMana.Refresh();
                                break;
                            }

                        case "stamina":
                            {
                                ComponentBarsFatigue.ForegroundColor = m_oGlobals.PresetList["stamina"].FgColor.ToDrawingColor();
                                ComponentBarsFatigue.BackgroundColor = m_oGlobals.PresetList["stamina"].BgColor.ToDrawingColor();
                                ComponentBarsFatigue.BorderColor = m_oGlobals.PresetList["stamina"].BgColor.ToDrawingColor();
                                ComponentBarsFatigue.Refresh();
                                break;
                            }

                        case "spirit":
                            {
                                ComponentBarsSpirit.ForegroundColor = m_oGlobals.PresetList["spirit"].FgColor.ToDrawingColor();
                                ComponentBarsSpirit.BackgroundColor = m_oGlobals.PresetList["spirit"].BgColor.ToDrawingColor();
                                ComponentBarsSpirit.BorderColor = m_oGlobals.PresetList["spirit"].BgColor.ToDrawingColor();
                                ComponentBarsSpirit.Refresh();
                                break;
                            }

                        case "concentration":
                            {
                                ComponentBarsConc.ForegroundColor = m_oGlobals.PresetList["concentration"].FgColor.ToDrawingColor();
                                ComponentBarsConc.BackgroundColor = m_oGlobals.PresetList["concentration"].BgColor.ToDrawingColor();
                                ComponentBarsConc.BorderColor = m_oGlobals.PresetList["concentration"].BgColor.ToDrawingColor();
                                ComponentBarsConc.Refresh();
                                break;
                            }

                        case "all":
                            {
                                oRTControl.ForegroundColor = m_oGlobals.PresetList["roundtime"].FgColor.ToDrawingColor();
                                oRTControl.BackgroundColorRT = m_oGlobals.PresetList["roundtime"].BgColor.ToDrawingColor();
                                oRTControl.Refresh();
                                Castbar.ForegroundColor = m_oGlobals.PresetList["castbar"].FgColor.ToDrawingColor();
                                Castbar.BackgroundColorRT = m_oGlobals.PresetList["castbar"].BgColor.ToDrawingColor();
                                Castbar.Refresh();
                                ComponentBarsHealth.ForegroundColor = m_oGlobals.PresetList["health"].FgColor.ToDrawingColor();
                                ComponentBarsHealth.BackgroundColor = m_oGlobals.PresetList["health"].BgColor.ToDrawingColor();
                                ComponentBarsHealth.BorderColor = m_oGlobals.PresetList["health"].BgColor.ToDrawingColor();
                                ComponentBarsHealth.Refresh();
                                ComponentBarsMana.ForegroundColor = m_oGlobals.PresetList["mana"].FgColor.ToDrawingColor();
                                ComponentBarsMana.BackgroundColor = m_oGlobals.PresetList["mana"].BgColor.ToDrawingColor();
                                ComponentBarsMana.BorderColor = m_oGlobals.PresetList["mana"].BgColor.ToDrawingColor();
                                ComponentBarsMana.Refresh();
                                ComponentBarsFatigue.ForegroundColor = m_oGlobals.PresetList["stamina"].FgColor.ToDrawingColor();
                                ComponentBarsFatigue.BackgroundColor = m_oGlobals.PresetList["stamina"].BgColor.ToDrawingColor();
                                ComponentBarsFatigue.BorderColor = m_oGlobals.PresetList["stamina"].BgColor.ToDrawingColor();
                                ComponentBarsFatigue.Refresh();
                                ComponentBarsSpirit.ForegroundColor = m_oGlobals.PresetList["spirit"].FgColor.ToDrawingColor();
                                ComponentBarsSpirit.BackgroundColor = m_oGlobals.PresetList["spirit"].BgColor.ToDrawingColor();
                                ComponentBarsSpirit.BorderColor = m_oGlobals.PresetList["spirit"].BgColor.ToDrawingColor();
                                ComponentBarsSpirit.Refresh();
                                ComponentBarsConc.ForegroundColor = m_oGlobals.PresetList["concentration"].FgColor.ToDrawingColor();
                                ComponentBarsConc.BackgroundColor = m_oGlobals.PresetList["concentration"].BgColor.ToDrawingColor();
                                ComponentBarsConc.BorderColor = m_oGlobals.PresetList["concentration"].BgColor.ToDrawingColor();
                                ComponentBarsConc.Refresh();
                                break;
                            }
                    }
                }
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("PresetChanged", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        private void ClassCommand_PresetChanged(string sPreset)
        {
            try
            {
                SafePresetChanged(sPreset);
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("PresetChanged", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

    }
}
