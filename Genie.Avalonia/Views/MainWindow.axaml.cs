using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GenieClient.Avalonia.Converters;
using GenieClient.Avalonia.Models;
using GenieClient.Avalonia.Services;
using GenieClient.Avalonia.ViewModels;
using GenieClient.Genie;

namespace GenieClient.Avalonia.Views
{
    public partial class MainWindow : Window
    {
        // Core engines
        private Globals m_oGlobals;
        private Game m_oGame;
        private Command m_oCommand;
        private ScriptManager m_oScriptManager;
        private TriggerEngine m_oTriggerEngine;
        private GameLoop m_oGameLoop;
        private Mapper.AutoMapper m_oAutoMapper;

        // Game timer
        private DispatcherTimer _gameTimer;

        // State
        private bool _lastRowWasPrompt;
        private string m_sGenieKey = string.Empty;
        private XMLConfig m_oProfile = new XMLConfig();
        private string m_sCurrentProfileFile = string.Empty;
        private string m_sCurrentProfileName = string.Empty;
        private bool m_CommandSent = false;

        // Dock system
        private GenieDockFactory _dockFactory;
        private WindowManager _windowManager;

        // Default colors
        private static readonly GenieColor DefaultFg = GenieColor.FromName("WhiteSmoke");
        private static readonly GenieColor DefaultBg = GenieColor.Transparent;

        public MainWindow()
        {
            InitializeComponent();

            // Create core engines (mirrors FormMain constructor lines 29-67)
            m_oGlobals = new Globals();
            m_oGame = new Game(ref m_oGlobals);
            m_oCommand = new Command(ref m_oGlobals);
            m_oScriptManager = new ScriptManager(m_oGlobals, m_oCommand);
            m_oTriggerEngine = new TriggerEngine(m_oGlobals, m_oCommand, m_oScriptManager);
            m_oGameLoop = new GameLoop(m_oGlobals, m_oCommand, m_oScriptManager);
            m_oAutoMapper = new Mapper.AutoMapper(ref m_oGlobals);

            // Initialize dock system
            _dockFactory = new GenieDockFactory();
            var layout = _dockFactory.CreateLayout();
            _dockFactory.InitLayout(layout);
            DockControl.Layout = layout;
            _windowManager = new WindowManager(_dockFactory);
            _windowManager.WindowsChanged += UpdateWindowMenu;

            WireEvents();
            SetupTimer();
            SetupUI();
            InitializeHudColors();

            LocalDirectory.CheckUserDirectory();
        }

        private void SetupUI()
        {
            // Menu events — File
            MenuConnect.Click += OnMenuConnect;
            MenuDisconnect.Click += OnMenuDisconnect;
            MenuExit.Click += OnMenuExit;

            // Menu events — File > Open Directory
            MenuOpenGenieDir.Click += (_, _) => OpenDirectory(LocalDirectory.Path);
            MenuOpenScriptsDir.Click += (_, _) => OpenDirectory(m_oGlobals.Config.ScriptDir);
            MenuOpenMapsDir.Click += (_, _) => OpenDirectory(m_oGlobals.Config.MapDir);
            MenuOpenLogsDir.Click += (_, _) => OpenDirectory(Path.Combine(LocalDirectory.Path, m_oGlobals.Config.sLogDir));

            // Menu events — File > Toggles
            MenuAutoLog.Click += (_, _) => { m_oGlobals.Config.bAutoLog = !m_oGlobals.Config.bAutoLog; };
            MenuAutoReconnect.Click += (_, _) => { m_oGlobals.Config.bReconnect = !m_oGlobals.Config.bReconnect; };
            MenuIgnoresEnabled.Click += (_, _) => { m_oGlobals.Config.bGagsEnabled = !m_oGlobals.Config.bGagsEnabled; };
            MenuTriggersEnabled.Click += (_, _) => { m_oTriggerEngine.TriggersEnabled = !m_oTriggerEngine.TriggersEnabled; };
            MenuPluginsEnabled.Click += (_, _) => { m_oGlobals.PluginsEnabled = !m_oGlobals.PluginsEnabled; };
            MenuAutoMapperEnabled.Click += (_, _) => { m_oGlobals.Config.bAutoMapper = !m_oGlobals.Config.bAutoMapper; };
            MenuMuteSounds.Click += (_, _) => { m_oGlobals.Config.bPlaySounds = !m_oGlobals.Config.bPlaySounds; };
            MenuShowRaw.Click += (_, _) =>
            {
                m_oGame.ShowRawOutput = !m_oGame.ShowRawOutput;
            };

            // Menu events — Profile
            MenuLoadProfile.Click += OnMenuLoadProfile;
            MenuSaveProfile.Click += (_, _) => Command_SaveProfile();

            // Menu events — Layout
            MenuAlwaysOnTop.Click += (_, _) =>
            {
                Topmost = !Topmost;
            };

            // Menu events — Script
            MenuListScripts.Click += (_, _) =>
            {
                m_oCommand.ParseCommand("#script list");
            };
            MenuPauseAll.Click += (_, _) => m_oScriptManager.ScriptPause("");
            MenuResumeAll.Click += (_, _) => m_oScriptManager.ScriptResume("");
            MenuAbortAll.Click += (_, _) =>
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
            };

            // Menu events — Help
            MenuGitHub.Click += (_, _) => OpenUrl("https://github.com/GenieClient/Genie4");
            MenuDiscord.Click += (_, _) => OpenUrl("https://discord.gg/VKhbEMD9KX");
            MenuDocs.Click += (_, _) => OpenUrl("https://github.com/GenieClient/Genie4/wiki");
            MenuChangelog.Click += (_, _) => OpenUrl("https://github.com/GenieClient/Genie4/releases/latest");

            // Command input
            CommandInput.SendText += OnCommandSendText;
            CommandInput.ScrollPageUp += () => { /* TODO: scroll active dock panel */ };
            CommandInput.ScrollPageDown += () => { /* TODO: scroll active dock panel */ };

            // Focus input on startup
            Opened += (_, _) => CommandInput.FocusInput();

            // Initialize Window menu
            UpdateWindowMenu();
        }

        private void SetupTimer()
        {
            _gameTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
            _gameTimer.Tick += (_, _) =>
            {
                m_oGameLoop.Tick();
                if (m_oScriptManager.ScriptListUpdated)
                    m_oScriptManager.SetScriptListVariable();
                CheckReconnect();
            };
            _gameTimer.Start();
        }

        private void InitializeHudColors()
        {
            try
            {
                ApplyPresetColor("health");
                ApplyPresetColor("mana");
                ApplyPresetColor("stamina");
                ApplyPresetColor("spirit");
                ApplyPresetColor("concentration");
                ApplyPresetColor("roundtime");
                ApplyPresetColor("castbar");
            }
            catch
            {
                // Preset colors may not be initialized yet
            }

            VitalBars.HealthBar.Value = 100;
            VitalBars.ManaBar.Value = 100;
            VitalBars.FatigueBar.Value = 100;
            VitalBars.SpiritBar.Value = 100;
            VitalBars.ConcBar.Value = 100;
        }

        #region Helpers

        private bool IsVarTrue(string varName)
        {
            var val = m_oGlobals.VariableList[varName]?.ToString();
            return val == "1" || string.Equals(val, "True", StringComparison.OrdinalIgnoreCase);
        }

        private string GetProfilesDir()
        {
            return Path.Combine(LocalDirectory.Path, "Config", "Profiles");
        }

        private void OpenDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    Process.Start("open", path);
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    Process.Start("explorer.exe", path);
                else
                    Process.Start("xdg-open", path);
            }
            catch (Exception ex)
            {
                CoreError.Error("OpenDirectory", ex.Message, ex.ToString());
            }
        }

        private void OpenUrl(string url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    Process.Start("open", url);
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                else
                    Process.Start("xdg-open", url);
            }
            catch (Exception ex)
            {
                CoreError.Error("OpenUrl", ex.Message, ex.ToString());
            }
        }

        #endregion

        #region Event Wiring

        private void WireEvents()
        {
            // --- ScriptManager events ---
            m_oScriptManager.EventScriptPrintText += Script_EventPrintText;
            m_oScriptManager.EventScriptPrintError += Script_EventPrintError;
            m_oScriptManager.EventScriptSendText += Script_EventSendText;
            m_oScriptManager.EventScriptStatusChanged += Script_EventStatusChanged;

            // --- Core-to-Core: Game → ScriptManager ---
            m_oGame.EventTriggerPrompt += m_oScriptManager.TriggerPromptForScripts;
            m_oGame.EventTriggerMove += m_oScriptManager.TriggerMoveForScripts;

            // --- Core-to-Core: Command → ScriptManager ---
            m_oCommand.EventRunScript += m_oScriptManager.RunScript;
            m_oCommand.EventScriptAbort += m_oScriptManager.ScriptAbort;
            m_oCommand.EventScriptPause += m_oScriptManager.ScriptPause;
            m_oCommand.EventScriptPauseOrResume += m_oScriptManager.ScriptPauseOrResume;
            m_oCommand.EventScriptReload += m_oScriptManager.ScriptReload;
            m_oCommand.EventScriptResume += m_oScriptManager.ScriptResume;
            m_oCommand.EventScriptDebug += m_oScriptManager.ScriptDebugLevel;

            // --- Game events → UI ---
            m_oGame.EventPrintText += Game_EventPrintText;
            m_oGame.EventPrintError += Game_PrintError;
            m_oGame.EventDataRecieveEnd += Game_EventDataRecieveEnd;
            m_oGame.EventVariableChanged += Game_EventVariableChanged;
            m_oGame.EventTriggerParse += Game_EventTriggerParse;
            m_oGame.EventStatusBarUpdate += Game_EventStatusBarUpdate;
            m_oGame.EventStreamWindow += Game_EventStreamWindow;
            m_oGame.EventClearWindow += Game_EventClearWindow;
            m_oGame.EventRoundTime += Game_EventRoundTime;
            m_oGame.EventSpellTime += Game_EventSpellTime;
            m_oGame.EventClearSpellTime += Game_EventClearSpellTime;
            m_oGame.EventCastTime += Game_EventCastTime;

            // --- Command events → UI ---
            m_oCommand.EventEchoText += Command_EchoText;
            m_oCommand.EventEchoColorText += Command_EchoColorText;
            m_oCommand.EventSendText += Command_SendText;
            m_oCommand.EventConnect += Command_Connect;
            m_oCommand.EventDisconnect += Command_Disconnect;
            m_oCommand.EventExit += Command_Exit;
            m_oCommand.EventReconnect += Command_Reconnect;
            m_oCommand.EventVariableChanged += Command_EventVariableChanged;
            m_oCommand.EventClearWindow += Command_ClearWindow;
            m_oCommand.EventStatusBar += Command_StatusBar;
            m_oCommand.EventParseLine += Command_ParseLine;
            m_oCommand.EventLoadProfile += Command_LoadProfile;
            m_oCommand.EventSaveProfile += Command_SaveProfile;
            m_oCommand.EventClassChange += Command_ClassChange;
            m_oCommand.EventPresetChanged += Command_PresetChanged;
            m_oCommand.EventChangeWindowTitle += Command_ChangeWindowTitle;
            m_oCommand.EventSendRaw += Command_SendRaw;
            m_oCommand.EventAddWindow += Command_AddWindow;
            m_oCommand.EventPositionWindow += Command_PositionWindow;
            m_oCommand.EventRemoveWindow += Command_RemoveWindow;
            m_oCommand.EventCloseWindow += Command_CloseWindow;

            // --- TriggerEngine events ---
            m_oTriggerEngine.EventEchoText += TriggerEngine_EchoText;

            // --- GameLoop events ---
            m_oGameLoop.EventEndUpdate += GameLoop_EndUpdate;

            // --- AutoMapper events ---
            m_oAutoMapper.EventEchoText += AutoMapper_EchoText;
            m_oAutoMapper.EventSendText += AutoMapper_SendText;
            m_oAutoMapper.EventParseText += AutoMapper_ParseText;
        }

        #endregion

        #region Game Event Handlers

        private void Game_EventPrintText(string text, GenieColor color, GenieColor bgcolor,
            Game.WindowTarget targetwindow, string targetwindowstring,
            bool mono, bool isprompt, bool isinput)
        {
            if (targetwindow == Game.WindowTarget.Main || targetwindow == Game.WindowTarget.Unknown)
            {
                UIDispatcher.Post(() => AddText(text, color, bgcolor, mono, isprompt));
                return;
            }

            UIDispatcher.Post(() =>
            {
                var panel = _windowManager.ResolveTarget(targetwindow, targetwindowstring);
                if (panel != null && panel.Owner != null)
                {
                    AddTextToPanel(panel, text, color, bgcolor, mono);
                }
                else
                {
                    // Panel not found or hidden — follow IfClosed chain
                    string ifClosed = panel?.IfClosed;
                    if (ifClosed == "") return; // empty = discard

                    var fallback = ifClosed != null ? _windowManager.ResolveIfClosed(ifClosed) : null;
                    if (fallback != null && fallback.Owner != null)
                        AddTextToPanel(fallback, text, color, bgcolor, mono);
                    else
                        AddText(text, color, bgcolor, mono, isprompt); // final fallback = main
                }
            });
        }

        private void Game_PrintError(string text)
        {
            UIDispatcher.Post(() => AddText(text + Environment.NewLine,
                GenieColor.FromName("WhiteSmoke"), GenieColor.FromName("DarkRed")));
        }

        private void Game_EventDataRecieveEnd()
        {
            UIDispatcher.Post(FlushText);
        }

        private void Game_EventVariableChanged(string sVariable)
        {
            UIDispatcher.Post(() => UpdateVariable(sVariable));
        }

        private void Game_EventTriggerParse(string text)
        {
            m_oTriggerEngine.ParseTriggers(text);
        }

        private void Game_EventStatusBarUpdate()
        {
            UIDispatcher.Post(UpdateStatusBar);
        }

        private void Game_EventStreamWindow(object sID, object sTitle, object sIfClosed)
        {
            string id = sID?.ToString();
            string title = sTitle?.ToString();
            string ifClosed = sIfClosed?.ToString();

            if (string.IsNullOrEmpty(id) || id.Equals("main", StringComparison.OrdinalIgnoreCase))
                return;

            UIDispatcher.Post(() =>
            {
                _windowManager.GetOrCreateWindow(id, title, ifClosed);
            });
        }

        private void Game_EventClearWindow(string sWindow)
        {
            UIDispatcher.Post(() =>
            {
                if (string.IsNullOrEmpty(sWindow) || sWindow.Equals("main", StringComparison.OrdinalIgnoreCase))
                    _dockFactory.MainGamePanel.Clear();
                else
                    _windowManager.ClearWindow(sWindow);
            });
        }

        private void Game_EventRoundTime(int iTime)
        {
            UIDispatcher.Post(() =>
            {
                if (iTime == 0) return;
                if (HudPanel.RTBar.CurrentRT == 0 || iTime > HudPanel.RTBar.CurrentRT + 1)
                {
                    HudPanel.RTBar.SetRT((int)(iTime + m_oGlobals.Config.dRTOffset));
                }

                m_oScriptManager.SetRoundTimeForScripts(iTime);
                m_oGlobals.RoundTimeEnd = DateTime.Now.AddMilliseconds(
                    iTime * 1000 + m_oGlobals.Config.dRTOffset * 1000);
            });
        }

        private void Game_EventSpellTime()
        {
            UIDispatcher.Post(() =>
            {
                m_oGlobals.SpellTimeStart = DateTime.Now;
                m_oGlobals.VariableList["spellstarttime"] =
                    ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds().ToString();
            });
        }

        private void Game_EventClearSpellTime()
        {
            UIDispatcher.Post(() =>
            {
                m_oGlobals.SpellTimeStart = default;
            });
        }

        private void Game_EventCastTime()
        {
            UIDispatcher.Post(() =>
            {
                if (int.TryParse(m_oGlobals.VariableList["gametime"]?.ToString(), out int gameTime) &&
                    int.TryParse(m_oGlobals.VariableList["casttime"]?.ToString(), out int castTime) &&
                    m_oGlobals.VariableList["preparedspell"]?.ToString() != "None")
                {
                    HudPanel.CastBar.SetRT(castTime - gameTime);
                }
                else
                {
                    HudPanel.CastBar.SetRT(0);
                }
            });
        }

        #endregion

        #region Command Event Handlers

        private void Command_EchoText(string sText, string sWindow)
        {
            bool isMono = false;
            if (sText.StartsWith("mono", StringComparison.OrdinalIgnoreCase))
            {
                isMono = true;
                sText = sText.Substring(4);
            }

            string capturedText = sText;
            UIDispatcher.Post(() =>
            {
                var panel = !string.IsNullOrEmpty(sWindow) ? _windowManager.FindWindow(sWindow) : null;
                if (panel != null && panel.Owner != null)
                    AddTextToPanel(panel, capturedText, DefaultFg, DefaultBg, isMono);
                else
                    AddText(capturedText, DefaultFg, DefaultBg, isMono);
            });
        }

        private void Command_EchoColorText(string sText, GenieColor oColor, GenieColor oBgColor, string sWindow)
        {
            UIDispatcher.Post(() =>
            {
                var panel = !string.IsNullOrEmpty(sWindow) ? _windowManager.FindWindow(sWindow) : null;
                if (panel != null && panel.Owner != null)
                    AddTextToPanel(panel, sText, oColor, oBgColor);
                else
                    AddText(sText, oColor, oBgColor);
            });
        }

        private void Command_SendText(string sText, bool bUserInput, string sOrigin)
        {
            m_oGame.SendText(sText, bUserInput, sOrigin);
        }

        private void Command_Connect(string sAccountName, string sPassword, string sCharacter, string sGame, bool isLich)
        {
            UIDispatcher.Post(() => ConnectToGame(sAccountName, sPassword, sCharacter, sGame, isLich));
        }

        private void Command_Disconnect()
        {
            m_oGame.Disconnect();
        }

        private void Command_Exit()
        {
            m_oGame.Disconnect(true);
        }

        private void Command_Reconnect()
        {
            UIDispatcher.Post(ReconnectToGame);
        }

        private void Command_EventVariableChanged(string sVariable)
        {
            UIDispatcher.Post(() => UpdateVariable(sVariable));
        }

        private void Command_ClearWindow(string sWindow)
        {
            UIDispatcher.Post(() =>
            {
                if (string.IsNullOrEmpty(sWindow) || sWindow.Equals("main", StringComparison.OrdinalIgnoreCase))
                    _dockFactory.MainGamePanel.Clear();
                else
                    _windowManager.ClearWindow(sWindow);
            });
        }

        private void Command_StatusBar(string sText, int iIndex)
        {
            UIDispatcher.Post(() => StatusBarText.Text = sText);
        }

        private void Command_ParseLine(string sText)
        {
            m_oCommand.ParseCommand(sText);
        }

        private void Command_LoadProfile()
        {
            UIDispatcher.Post(() =>
            {
                if (m_oGlobals.VariableList["charactername"]?.ToString().Length > 0 &&
                    m_oGlobals.VariableList["game"]?.ToString().Length > 0)
                {
                    string sFileName = m_oGlobals.VariableList["charactername"].ToString() +
                                       m_oGlobals.VariableList["game"].ToString() + ".xml";
                    LoadProfile(sFileName);
                }
            });
        }

        private void Command_SaveProfile()
        {
            UIDispatcher.Post(() =>
            {
                if (m_sCurrentProfileName.Length > 0)
                {
                    SaveProfile(m_sCurrentProfileName);
                    string sProfile = Path.GetFileNameWithoutExtension(m_sCurrentProfileName);
                    m_oGlobals.Config.sConfigDirProfile = Path.Combine(GetProfilesDir(), sProfile);
                    LoadProfileSettings(false);
                }
                else if (m_oGame.AccountCharacter?.Length > 0 && m_oGame.AccountGame?.Length > 0)
                {
                    string name = m_oGame.AccountCharacter + m_oGame.AccountGame + ".xml";
                    SaveProfile(name);
                    m_sCurrentProfileName = name;
                    string sProfile = Path.GetFileNameWithoutExtension(name);
                    m_oGlobals.Config.sConfigDirProfile = Path.Combine(GetProfilesDir(), sProfile);
                    LoadProfileSettings(false);
                }
                else
                {
                    AddText("Unknown character or game name. Save profile failed." + Environment.NewLine,
                        DefaultFg, DefaultBg);
                }
            });
        }

        private bool SaveProfile(string fileName)
        {
            if (m_oProfile.GetValue("Genie/Profile", "Account", string.Empty).Length == 0)
            {
                m_oProfile.LoadXml("<Genie><Profile></Profile></Genie>");
            }

            m_oProfile.SetValue("Genie/Profile", "Account", m_oGame.AccountName ?? string.Empty);
            m_oGlobals.VariableList["account"] = m_oGame.AccountName ?? string.Empty;

            if (MenuSavePassword.IsChecked && m_oGame.AccountPassword?.Length > 0)
            {
                m_oProfile.SetValue("Genie/Profile", "Password",
                    Utility.EncryptString("G3" + m_oGame.AccountName.ToUpper(), m_oGame.AccountPassword));
            }
            else
            {
                m_oProfile.SetValue("Genie/Profile", "Password", "");
            }

            m_oProfile.SetValue("Genie/Profile", "Character", m_oGame.AccountCharacter ?? string.Empty);
            m_oProfile.SetValue("Genie/Profile", "Game", m_oGame.AccountGame ?? string.Empty);

            if (fileName.IndexOf(Path.DirectorySeparatorChar) == -1 && fileName.IndexOf('/') == -1)
            {
                fileName = Path.Combine(GetProfilesDir(), fileName);
            }

            m_sCurrentProfileFile = fileName;

            // Ensure Profiles directory exists
            Utility.CreateDirectory(Path.GetDirectoryName(fileName));

            bool result = m_oProfile.SaveToFile(fileName);
            if (result)
            {
                AddText($"Profile saved to \"{fileName}\".{Environment.NewLine}", DefaultFg, DefaultBg);
            }
            else
            {
                AddText($"Failed to save profile.{Environment.NewLine}",
                    GenieColor.FromName("WhiteSmoke"), GenieColor.FromName("DarkRed"));
            }
            return result;
        }

        private void Command_ClassChange()
        {
            // Stub for Phase 5 — class/highlight reapply
        }

        private void Command_PresetChanged(string sPreset)
        {
            UIDispatcher.Post(() =>
            {
                try
                {
                    ApplyPresetColor(sPreset);
                }
                catch { }
            });
        }

        private void Command_ChangeWindowTitle(string sWindow, string sComment)
        {
            if (string.IsNullOrEmpty(sWindow) || sWindow.Equals("main", StringComparison.OrdinalIgnoreCase))
                UIDispatcher.Post(() => Title = string.IsNullOrEmpty(sComment) ? "Genie 4" : $"Genie 4 - {sComment}");
        }

        private void Command_SendRaw(string sText)
        {
            m_oGame.SendText(sText);
        }

        private void Command_AddWindow(string sWindow, int sWidth, int sHeight, int? sTop, int? sLeft)
        {
            UIDispatcher.Post(() =>
            {
                _windowManager.GetOrCreateWindow(sWindow, sWindow, null);
            });
        }

        private void Command_PositionWindow(string sWindow, int? sWidth, int? sHeight, int? sTop, int? sLeft)
        {
            // Dock proportions are managed by the dock framework — position/size hints are ignored
        }

        private void Command_RemoveWindow(string sWindow)
        {
            UIDispatcher.Post(() => _windowManager.RemoveWindow(sWindow));
        }

        private void Command_CloseWindow(string sWindow)
        {
            UIDispatcher.Post(() => _windowManager.HideWindow(sWindow));
        }

        #endregion

        #region ScriptManager Event Handlers

        private void Script_EventPrintText(string sText, GenieColor oColor, GenieColor oBgColor)
        {
            UIDispatcher.Post(() => AddText(sText, oColor, oBgColor));
        }

        private void Script_EventPrintError(string sText)
        {
            UIDispatcher.Post(() => AddText(sText + Environment.NewLine,
                GenieColor.FromName("WhiteSmoke"), GenieColor.FromName("DarkRed")));
        }

        private void Script_EventSendText(string text, string script, bool toQueue, bool doCommand)
        {
            if (doCommand)
                m_oCommand.ParseCommand(text);
            else
                m_oGame.SendText(text, false, script);
        }

        private void Script_EventStatusChanged(Script sender, Script.ScriptState state)
        {
            // Stub for future phase — script toolbar
        }

        #endregion

        #region TriggerEngine / GameLoop / AutoMapper Handlers

        private void TriggerEngine_EchoText(string sText, string sWindow)
        {
            UIDispatcher.Post(() =>
            {
                var panel = !string.IsNullOrEmpty(sWindow) ? _windowManager.FindWindow(sWindow) : null;
                if (panel != null && panel.Owner != null)
                    AddTextToPanel(panel, sText, DefaultFg, DefaultBg);
                else
                    AddText(sText, DefaultFg, DefaultBg);
            });
        }

        private void GameLoop_EndUpdate()
        {
            UIDispatcher.Post(FlushText);
        }

        private void AutoMapper_EchoText(string sText, GenieColor oColor, GenieColor oBgColor)
        {
            UIDispatcher.Post(() => AddText(sText, oColor, oBgColor));
        }

        private void AutoMapper_SendText(string sText, string sSource)
        {
            m_oGame.SendText(sText);
        }

        private void AutoMapper_ParseText(string sText)
        {
            m_oCommand.ParseCommand(sText);
        }

        #endregion

        #region Text Output

        private List<TextSegment> _pendingSegments = new();
        private bool _hasPendingText;

        private void AddText(string sText, GenieColor fgColor = default, GenieColor bgColor = default,
            bool bMono = false, bool bPrompt = false)
        {
            // Handle prompt dedup
            if (bPrompt)
            {
                if (_lastRowWasPrompt) return;
                _lastRowWasPrompt = true;
            }
            else if (sText.Length > 0)
            {
                _lastRowWasPrompt = false;
            }

            // Default colors
            if (fgColor.IsEmpty || fgColor == GenieColor.Transparent)
                fgColor = DefaultFg;

            if (string.IsNullOrEmpty(sText))
            {
                FlushText();
                return;
            }

            // Apply highlights
            ApplyHighlights(ref sText, ref fgColor, ref bgColor);

            // Split on newlines and add segments
            var lines = sText.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var lineText = lines[i];
                if (lineText.EndsWith("\r"))
                    lineText = lineText.Substring(0, lineText.Length - 1);

                if (lineText.Length > 0)
                    _pendingSegments.Add(new TextSegment(lineText, fgColor, bgColor, bMono));

                // Newline found — flush this line
                if (i < lines.Length - 1)
                {
                    FlushLine();
                }
            }

            _hasPendingText = _pendingSegments.Count > 0;
        }

        private void FlushLine()
        {
            if (_pendingSegments.Count > 0)
            {
                var line = new TextLine(_pendingSegments.ToArray());
                _dockFactory.MainGamePanel.AddLine(line);
                _pendingSegments.Clear();
            }
            else
            {
                // Empty line
                _dockFactory.MainGamePanel.AddLine(new TextLine(new[] { new TextSegment("", DefaultFg, DefaultBg) }));
            }
            _hasPendingText = false;
        }

        private void FlushText()
        {
            if (_hasPendingText)
                FlushLine();
        }

        private void AddTextToPanel(OutputPanelViewModel panel, string sText,
            GenieColor fgColor, GenieColor bgColor, bool bMono = false)
        {
            if (fgColor.IsEmpty || fgColor == GenieColor.Transparent)
                fgColor = DefaultFg;

            if (string.IsNullOrEmpty(sText)) return;

            ApplyHighlights(ref sText, ref fgColor, ref bgColor);

            var lines = sText.Split('\n');
            var segments = new List<TextSegment>();

            for (int i = 0; i < lines.Length; i++)
            {
                var lineText = lines[i];
                if (lineText.EndsWith("\r"))
                    lineText = lineText.Substring(0, lineText.Length - 1);

                if (lineText.Length > 0)
                    segments.Add(new TextSegment(lineText, fgColor, bgColor, bMono));

                if (i < lines.Length - 1)
                {
                    // Flush to panel
                    if (segments.Count > 0)
                    {
                        panel.AddLine(new TextLine(segments.ToArray()));
                        segments.Clear();
                    }
                    else
                    {
                        panel.AddLine(new TextLine(new[] { new TextSegment("", DefaultFg, DefaultBg) }));
                    }
                }
            }

            // Flush remaining segments
            if (segments.Count > 0)
                panel.AddLine(new TextLine(segments.ToArray()));
        }

        private void ApplyHighlights(ref string sText, ref GenieColor fgColor, ref GenieColor bgColor)
        {
            // Apply whole-line highlights from HighlightList using RegexLine
            var highlights = m_oGlobals.HighlightList;
            if (highlights == null) return;

            try
            {
                var regex = highlights.RegexLine;
                if (regex != null)
                {
                    var match = regex.Match(sText);
                    if (match.Success)
                    {
                        var key = match.Value;
                        if (highlights.ContainsKey(key))
                        {
                            var hl = (Highlights.Highlight)highlights[key];
                            if (hl.FgColor != GenieColor.Transparent && !hl.FgColor.IsEmpty)
                                fgColor = hl.FgColor;
                            if (hl.BgColor != GenieColor.Transparent && !hl.BgColor.IsEmpty)
                                bgColor = hl.BgColor;
                        }
                    }
                }
            }
            catch
            {
                // Highlight errors should not break text output
            }
        }

        #endregion

        #region Connection

        private async void OnMenuLoadProfile(object sender, RoutedEventArgs e)
        {
            var storageProvider = StorageProvider;
            if (storageProvider == null) return;

            string profilesDir = GetProfilesDir();
            Utility.CreateDirectory(profilesDir);

            var startFolder = await storageProvider.TryGetFolderFromPathAsync(profilesDir);
            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Load Profile",
                AllowMultiple = false,
                SuggestedStartLocation = startFolder,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("XML Profiles") { Patterns = new[] { "*.xml" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            });

            if (files.Count > 0)
            {
                string filePath = files[0].Path.LocalPath;
                LoadProfile(filePath, true);
            }
        }

        private async void OnMenuConnect(object sender, RoutedEventArgs e)
        {
            var dialog = new ConnectDialog();
            await dialog.ShowDialog(this);

            if (dialog.Confirmed)
            {
                ConnectToGame(dialog.AccountName, dialog.Password, dialog.Character, dialog.Game);
            }
        }

        private void OnMenuDisconnect(object sender, RoutedEventArgs e)
        {
            m_oGame.Disconnect();
            StatusLabel.Text = "Disconnected";
            MenuConnect.IsEnabled = true;
            MenuDisconnect.IsEnabled = false;
        }

        private void OnMenuExit(object sender, RoutedEventArgs e)
        {
            m_oGame.Disconnect(true);
            Close();
        }

        private void ConnectToGame(string sAccountName, string sPassword, string sCharacter, string sGame, bool isLich = false)
        {
            m_oGame.IsLich = isLich;
            try
            {
                if (sPassword.Length > 0)
                {
                    m_oGame.Connect(m_sGenieKey, sAccountName, sPassword, sCharacter, sGame);
                    StatusLabel.Text = $"Connected: {sCharacter}";
                    MenuConnect.IsEnabled = false;
                    MenuDisconnect.IsEnabled = true;
                }
                else
                {
                    // Load profile
                    m_sCurrentProfileFile = string.Empty;
                    LoadProfile(sAccountName.Trim() + ".xml", true);
                }
            }
            catch (Exception ex)
            {
                CoreError.Error("ConnectToGame", ex.Message, ex.ToString());
            }
        }

        private void CheckReconnect()
        {
            if (!m_oGlobals.Config.bReconnect) return;
            if (m_oGame.ReconnectTime == default) return;
            if (!m_CommandSent)
            {
                AddText("Reconnect aborted! (No user input since last connect.)" + Environment.NewLine,
                    GenieColor.FromName("WhiteSmoke"), GenieColor.FromName("DarkRed"));
                if (m_oGame.IsConnected)
                    m_oGame.Disconnect();
                m_oGame.ReconnectTime = default;
                return;
            }
            if (m_oGame.ReconnectTime < DateTime.Now)
            {
                m_oGame.ReconnectTime = default;
                m_oGame.ConnectAttempts += 1;
                if (!m_oGlobals.Config.bReconnectWhenDead &&
                    m_oGlobals.VariableList["dead"]?.ToString() == "1")
                {
                    AddText("Reconnect aborted! (You seem to be dead.)" + Environment.NewLine,
                        GenieColor.FromName("WhiteSmoke"), GenieColor.FromName("DarkRed"));
                    return;
                }
                ReconnectToGame();
            }
        }

        private void ReconnectToGame()
        {
            try
            {
                if (m_oGame.AccountName?.Length > 0)
                {
                    m_oGlobals.GenieKey = m_sGenieKey;
                    m_oGlobals.GenieAccount = m_oGame.AccountName;
                    m_oGame.Connect(m_sGenieKey, m_oGame.AccountName, m_oGame.AccountPassword,
                        m_oGame.AccountCharacter, m_oGame.AccountGame);
                    StatusLabel.Text = $"Reconnected: {m_oGame.AccountCharacter}";
                    MenuConnect.IsEnabled = false;
                    MenuDisconnect.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                CoreError.Error("ReconnectToGame", ex.Message, ex.ToString());
            }
        }

        #endregion

        #region Profile Loading

        private async void LoadProfile(string fileName, bool doConnect = false)
        {
            string shortName = fileName;
            if (fileName.IndexOf(Path.DirectorySeparatorChar) == -1 && fileName.IndexOf('/') == -1)
            {
                fileName = Path.Combine(GetProfilesDir(), fileName);
            }

            if (m_oProfile.LoadFile(fileName))
            {
                AddText($"Profile \"{shortName}\" loaded.{Environment.NewLine}", DefaultFg, DefaultBg);

                string sCharacter = m_oProfile.GetValue("Genie/Profile", "Character", string.Empty);
                string sGame = m_oProfile.GetValue("Genie/Profile", "Game", string.Empty);
                if (sCharacter.Length > 0) m_oGame.AccountCharacter = sCharacter;
                if (sGame.Length > 0) m_oGame.AccountGame = sGame;

                // Set profile config directory
                string sProfile = Path.GetFileNameWithoutExtension(fileName);
                m_oGlobals.Config.sConfigDirProfile = Path.Combine(GetProfilesDir(), sProfile);

                LoadProfileSettings();

                string sAccount = m_oProfile.GetValue("Genie/Profile", "Account", string.Empty);
                string sPassword = m_oProfile.GetValue("Genie/Profile", "Password", string.Empty);
                if (sPassword.Length > 0)
                {
                    sPassword = Utility.DecryptString("G3" + sAccount.ToUpper(), sPassword);
                    MenuSavePassword.IsChecked = true;
                }
                else
                {
                    MenuSavePassword.IsChecked = false;
                }

                if (doConnect)
                {
                    if (sAccount.Length > 0 && sPassword.Length > 0)
                    {
                        ConnectToGame(sAccount, sPassword, sCharacter, sGame);
                    }
                    else
                    {
                        // Show connect dialog pre-populated with profile data
                        var dialog = new ConnectDialog();
                        dialog.InitialAccount = sAccount;
                        dialog.InitialCharacter = sCharacter;
                        dialog.InitialGame = sGame;
                        await dialog.ShowDialog(this);
                        if (dialog.Confirmed)
                        {
                            ConnectToGame(dialog.AccountName, dialog.Password, dialog.Character, dialog.Game);
                        }
                    }
                }
                else
                {
                    m_oGlobals.VariableList["account"] = sAccount;
                }

                m_sCurrentProfileFile = fileName;
                m_sCurrentProfileName = shortName;
            }
            else if (doConnect)
            {
                AddText($"Profile \"{fileName}\" not found.{Environment.NewLine}", DefaultFg, DefaultBg);
            }
        }

        private void LoadProfileSettings(bool echo = true)
        {
            string sProfileDir = m_oGlobals.Config.ConfigProfileDir;
            string sConfigDir = m_oGlobals.Config.ConfigDir;

            if (!Utility.CreateDirectory(sProfileDir))
                return;

            // Load variables
            if (echo) AddText("Loading Variables...", DefaultFg, DefaultBg);
            m_oGlobals.VariableList.ClearUser();
            m_oGlobals.VariableList.Load(Path.Combine(sProfileDir, "variables.cfg"));
            if (echo) AddText("OK" + Environment.NewLine, DefaultFg, DefaultBg);

            // Load macros (defaults then profile)
            if (echo) AddText("Loading Macros...", DefaultFg, DefaultBg);
            m_oGlobals.MacroList.Clear();
            m_oGlobals.MacroList.Load(Path.Combine(sConfigDir, "macros.cfg"));
            m_oGlobals.MacroList.Load(Path.Combine(sProfileDir, "macros.cfg"));
            if (echo) AddText("OK" + Environment.NewLine, DefaultFg, DefaultBg);

            // Load aliases (defaults then profile)
            if (echo) AddText("Loading Aliases...", DefaultFg, DefaultBg);
            m_oGlobals.AliasList.Clear();
            m_oGlobals.AliasList.Load(Path.Combine(sConfigDir, "aliases.cfg"));
            m_oGlobals.AliasList.Load(Path.Combine(sProfileDir, "aliases.cfg"));
            if (echo) AddText("OK" + Environment.NewLine, DefaultFg, DefaultBg);

            // Load classes
            if (echo) AddText("Loading Classes...", DefaultFg, DefaultBg);
            m_oGlobals.ClassList.Clear();
            m_oGlobals.ClassList.Load(Path.Combine(sProfileDir, "classes.cfg"));
            if (m_oGame.AccountCharacter.Length > 0)
            {
                if (!m_oGlobals.ClassList.ContainsKey(m_oGame.AccountCharacter.ToLower()))
                    m_oGlobals.ClassList.Add(m_oGame.AccountCharacter.ToLower(), "True");
            }
            if (m_oGame.AccountGame.Length > 0)
            {
                if (!m_oGlobals.ClassList.ContainsKey(m_oGame.AccountGame.ToLower()))
                    m_oGlobals.ClassList.Add(m_oGame.AccountGame.ToLower(), "True");
            }
            if (echo) AddText("OK" + Environment.NewLine, DefaultFg, DefaultBg);

            // Load highlights
            m_oGlobals.LoadHighlights(Path.Combine(sProfileDir, "highlights.cfg"));
            m_oGlobals.LoadHighlights(Path.Combine(sConfigDir, "highlights.cfg"));

            // Load triggers
            m_oGlobals.TriggerList.Load(Path.Combine(sProfileDir, "triggers.cfg"));
            m_oGlobals.TriggerList.Load(Path.Combine(sConfigDir, "triggers.cfg"));

            // Apply preset colors after loading
            ApplyPresetColor("all");
        }

        #endregion

        #region Command Input

        private void OnCommandSendText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            m_CommandSent = true;

            // Flush any pending text
            FlushText();

            // Parse through command engine (bSendToGame=true, bUserInput=true)
            m_oCommand.ParseCommand(text, true, true);
        }

        #endregion

        #region Variable / Status / HUD Updates

        private void UpdateVariable(string sVariable)
        {
            switch (sVariable)
            {
                // --- Vitals ---
                case "$health":
                {
                    int.TryParse(m_oGlobals.VariableList["health"]?.ToString(), out int val);
                    string text = m_oGlobals.VariableList["healthBarText"]?.ToString() ?? "";
                    VitalBars.HealthBar.BarText = text;
                    VitalBars.HealthBar.Value = val;
                    break;
                }
                case "$mana":
                {
                    int.TryParse(m_oGlobals.VariableList["mana"]?.ToString(), out int val);
                    string text = m_oGlobals.VariableList["manaBarText"]?.ToString() ?? "";
                    VitalBars.ManaBar.BarText = text;
                    VitalBars.ManaBar.Value = val;
                    break;
                }
                case "$stamina":
                {
                    int.TryParse(m_oGlobals.VariableList["stamina"]?.ToString(), out int val);
                    string text = m_oGlobals.VariableList["staminaBarText"]?.ToString() ?? "";
                    VitalBars.FatigueBar.BarText = text;
                    VitalBars.FatigueBar.Value = val;
                    break;
                }
                case "$spirit":
                {
                    int.TryParse(m_oGlobals.VariableList["spirit"]?.ToString(), out int val);
                    string text = m_oGlobals.VariableList["spiritBarText"]?.ToString() ?? "";
                    VitalBars.SpiritBar.BarText = text;
                    VitalBars.SpiritBar.Value = val;
                    break;
                }
                case "$concentration":
                {
                    int.TryParse(m_oGlobals.VariableList["concentration"]?.ToString(), out int val);
                    string text = m_oGlobals.VariableList["concentrationBarText"]?.ToString() ?? "";
                    VitalBars.ConcBar.BarText = text;
                    VitalBars.ConcBar.Value = val;
                    break;
                }

                // --- Compass ---
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
                    UpdateCompass();
                    return; // return, not break — block direction triggers

                // --- Posture ---
                case "$dead":
                case "$standing":
                case "$kneeling":
                case "$sitting":
                case "$prone":
                    UpdatePosture();
                    break;

                // --- Status effects ---
                case "$stunned":
                    HudPanel.StatusIcons.UpdateStunned(IsVarTrue("stunned"));
                    break;
                case "$bleeding":
                    HudPanel.StatusIcons.UpdateBleeding(IsVarTrue("bleeding"));
                    break;
                case "$invisible":
                    HudPanel.StatusIcons.UpdateInvisible(IsVarTrue("invisible"));
                    break;
                case "$hidden":
                    HudPanel.StatusIcons.UpdateHidden(IsVarTrue("hidden"));
                    break;
                case "$joined":
                    HudPanel.StatusIcons.UpdateJoined(IsVarTrue("joined"));
                    break;
                case "$webbed":
                    HudPanel.StatusIcons.UpdateWebbed(IsVarTrue("webbed"));
                    break;

                // --- Connected state ---
                case "$connected":
                {
                    var connected = m_oGlobals.VariableList["connected"]?.ToString();
                    if (connected == "1")
                    {
                        m_CommandSent = false;
                        StatusLabel.Text = "Connected";
                        MenuConnect.IsEnabled = false;
                        MenuDisconnect.IsEnabled = true;

                        // Set character/game variables
                        m_oGlobals.VariableList["charactername"] = m_oGame.AccountCharacter;
                        m_oGlobals.VariableList["game"] = m_oGame.AccountGame;
                        m_oGlobals.VariableList["gamename"] = m_oGame.AccountGame;
                        m_oAutoMapper.CharacterName = m_oGame.AccountCharacter;
                        m_oGame.ResetIndicators();

                        // Reset all status indicators
                        UpdatePosture();
                        HudPanel.StatusIcons.UpdateStunned(IsVarTrue("stunned"));
                        HudPanel.StatusIcons.UpdateBleeding(IsVarTrue("bleeding"));
                        HudPanel.StatusIcons.UpdateInvisible(IsVarTrue("invisible"));
                        HudPanel.StatusIcons.UpdateHidden(IsVarTrue("hidden"));
                        HudPanel.StatusIcons.UpdateJoined(IsVarTrue("joined"));
                        HudPanel.StatusIcons.UpdateWebbed(IsVarTrue("webbed"));
                    }
                    else
                    {
                        StatusLabel.Text = "Disconnected";
                        MenuConnect.IsEnabled = true;
                        MenuDisconnect.IsEnabled = false;
                    }
                    break;
                }

                case "$prompt":
                    HudPanel.StatusIcons.UpdateBleeding(IsVarTrue("bleeding"));
                    break;

                case "$charactername":
                case "charactername":
                {
                    var charName = m_oGlobals.VariableList["charactername"]?.ToString();
                    if (!string.IsNullOrEmpty(charName))
                    {
                        Title = $"Genie 4 - {charName}";
                        m_oAutoMapper.CharacterName = charName;
                        m_oGame.AccountCharacter = charName;
                    }
                    break;
                }
            }
        }

        private void UpdateCompass()
        {
            string[] dirs = { "north", "northeast", "east", "southeast",
                              "south", "southwest", "west", "northwest",
                              "up", "down", "out" };
            foreach (var dir in dirs)
                HudPanel.Compass.SetDirection(dir, IsVarTrue(dir));
        }

        private void UpdatePosture()
        {
            if (IsVarTrue("dead"))
                HudPanel.StatusIcons.SetPosture("dead");
            else if (IsVarTrue("standing"))
                HudPanel.StatusIcons.SetPosture("standing");
            else if (IsVarTrue("kneeling"))
                HudPanel.StatusIcons.SetPosture("kneeling");
            else if (IsVarTrue("sitting"))
                HudPanel.StatusIcons.SetPosture("sitting");
            else if (IsVarTrue("prone"))
                HudPanel.StatusIcons.SetPosture("prone");
        }

        private void UpdateStatusBar()
        {
            HudPanel.LeftHandLabel.Text = m_oGlobals.VariableList["lefthand"]?.ToString() ?? "Empty";
            HudPanel.RightHandLabel.Text = m_oGlobals.VariableList["righthand"]?.ToString() ?? "Empty";

            var preparedSpell = m_oGlobals.VariableList["preparedspell"]?.ToString() ?? "None";
            if (m_oGlobals.Config.bShowSpellTimer && m_oGlobals.SpellTimeStart != DateTime.MinValue)
            {
                int elapsed = Utility.GetTimeDiffInSeconds(m_oGlobals.SpellTimeStart, DateTime.Now);
                HudPanel.SpellLabel.Text = $"({elapsed}) {preparedSpell}";
            }
            else
            {
                HudPanel.SpellLabel.Text = preparedSpell;
            }
        }

        private void ApplyPresetColor(string preset)
        {
            switch (preset?.ToLowerInvariant())
            {
                case "health":
                    if (m_oGlobals.PresetList.ContainsKey("health"))
                    {
                        var p = (Globals.Presets.Preset)m_oGlobals.PresetList["health"];
                        VitalBars.HealthBar.FillBrush = p.FgColor.ToAvaloniaBrush();
                        VitalBars.HealthBar.TrackBrush = p.BgColor.ToAvaloniaBrush();
                    }
                    break;
                case "mana":
                    if (m_oGlobals.PresetList.ContainsKey("mana"))
                    {
                        var p = (Globals.Presets.Preset)m_oGlobals.PresetList["mana"];
                        VitalBars.ManaBar.FillBrush = p.FgColor.ToAvaloniaBrush();
                        VitalBars.ManaBar.TrackBrush = p.BgColor.ToAvaloniaBrush();
                    }
                    break;
                case "stamina":
                    if (m_oGlobals.PresetList.ContainsKey("stamina"))
                    {
                        var p = (Globals.Presets.Preset)m_oGlobals.PresetList["stamina"];
                        VitalBars.FatigueBar.FillBrush = p.FgColor.ToAvaloniaBrush();
                        VitalBars.FatigueBar.TrackBrush = p.BgColor.ToAvaloniaBrush();
                    }
                    break;
                case "spirit":
                    if (m_oGlobals.PresetList.ContainsKey("spirit"))
                    {
                        var p = (Globals.Presets.Preset)m_oGlobals.PresetList["spirit"];
                        VitalBars.SpiritBar.FillBrush = p.FgColor.ToAvaloniaBrush();
                        VitalBars.SpiritBar.TrackBrush = p.BgColor.ToAvaloniaBrush();
                    }
                    break;
                case "concentration":
                    if (m_oGlobals.PresetList.ContainsKey("concentration"))
                    {
                        var p = (Globals.Presets.Preset)m_oGlobals.PresetList["concentration"];
                        VitalBars.ConcBar.FillBrush = p.FgColor.ToAvaloniaBrush();
                        VitalBars.ConcBar.TrackBrush = p.BgColor.ToAvaloniaBrush();
                    }
                    break;
                case "roundtime":
                    if (m_oGlobals.PresetList.ContainsKey("roundtime"))
                    {
                        var p = (Globals.Presets.Preset)m_oGlobals.PresetList["roundtime"];
                        HudPanel.RTBar.FillBrush = p.FgColor.ToAvaloniaBrush();
                        HudPanel.RTBar.TrackBrush = p.BgColor.ToAvaloniaBrush();
                    }
                    break;
                case "castbar":
                    if (m_oGlobals.PresetList.ContainsKey("castbar"))
                    {
                        var p = (Globals.Presets.Preset)m_oGlobals.PresetList["castbar"];
                        HudPanel.CastBar.FillBrush = p.FgColor.ToAvaloniaBrush();
                        HudPanel.CastBar.TrackBrush = p.BgColor.ToAvaloniaBrush();
                    }
                    break;
                case "all":
                    ApplyPresetColor("health");
                    ApplyPresetColor("mana");
                    ApplyPresetColor("stamina");
                    ApplyPresetColor("spirit");
                    ApplyPresetColor("concentration");
                    ApplyPresetColor("roundtime");
                    ApplyPresetColor("castbar");
                    break;
            }
        }

        #endregion

        #region Window Menu

        private void UpdateWindowMenu()
        {
            MenuWindow.Items.Clear();

            var windows = _windowManager.GetAllWindowInfo();
            if (windows.Count > 0)
            {
                foreach (var info in windows)
                {
                    bool visible = _windowManager.IsWindowVisible(info.Id);
                    var item = new MenuItem
                    {
                        Header = info.Title,
                        Icon = visible ? new TextBlock { Text = "\u2713" } : null
                    };

                    var capturedInfo = info;
                    item.Click += (_, _) =>
                    {
                        if (_windowManager.IsWindowVisible(capturedInfo.Id))
                            _windowManager.HideWindow(capturedInfo.Id);
                        else
                            _windowManager.GetOrCreateWindow(capturedInfo.Id, capturedInfo.Title, capturedInfo.IfClosed);
                    };

                    MenuWindow.Items.Add(item);
                }

                MenuWindow.Items.Add(new Separator());
            }

            var addItem = new MenuItem { Header = "_New Window..." };
            addItem.Click += OnMenuAddWindow;
            MenuWindow.Items.Add(addItem);
        }

        private async void OnMenuAddWindow(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "New Window",
                Width = 300, Height = 140,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new global::Avalonia.Thickness(16),
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = "Window name:" },
                        new TextBox { Name = "NameBox", Watermark = "e.g. thoughts, combat, mywindow" },
                        new StackPanel
                        {
                            Orientation = global::Avalonia.Layout.Orientation.Horizontal,
                            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right,
                            Spacing = 8,
                            Children =
                            {
                                new Button { Content = "Create", IsDefault = true, Tag = "ok" },
                                new Button { Content = "Cancel", IsCancel = true }
                            }
                        }
                    }
                }
            };

            string result = null;
            var content = (StackPanel)dialog.Content;
            var nameBox = (TextBox)content.Children[1];
            var buttons = (StackPanel)content.Children[2];
            ((Button)buttons.Children[0]).Click += (_, _) => { result = nameBox.Text; dialog.Close(); };
            ((Button)buttons.Children[1]).Click += (_, _) => { dialog.Close(); };

            await dialog.ShowDialog(this);

            if (!string.IsNullOrWhiteSpace(result))
            {
                _windowManager.GetOrCreateWindow(result.Trim(), result.Trim(), null);
            }
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            _gameTimer?.Stop();

            if (m_oGame != null)
            {
                try { m_oGame.Disconnect(); } catch { }
            }

            base.OnClosed(e);
        }
    }
}
