using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using GenieClient.Genie;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace GenieClient
{
    // Plugin management: loading, unloading, plugin menus, plugin text/input/variable parsing, plugin exceptions.
    public partial class FormMain
    {
        private List<PluginServices.AvailablePlugin> m_oPlugins = new List<PluginServices.AvailablePlugin>();
        private Dictionary<string, string> m_oPluginNameToFile = new Dictionary<string, string>();
        private LegacyPluginHost _m_oLegacyPluginHost;
        private PluginHost _m_oPluginHost;

        private LegacyPluginHost m_oLegacyPluginHost
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _m_oLegacyPluginHost;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_m_oLegacyPluginHost != null)
                {
                    _m_oLegacyPluginHost.EventEchoText -= Plugin_EventEchoText;
                    _m_oLegacyPluginHost.EventSendText -= Plugin_EventSendText;
                    _m_oLegacyPluginHost.EventVariableChanged -= PluginHost_EventVariableChanged;
                }

                _m_oLegacyPluginHost = value;
                if (_m_oLegacyPluginHost != null)
                {
                    _m_oLegacyPluginHost.EventEchoText += Plugin_EventEchoText;
                    _m_oLegacyPluginHost.EventSendText += Plugin_EventSendText;
                    _m_oLegacyPluginHost.EventVariableChanged += PluginHost_EventVariableChanged;
                }
            }
        }

        private PluginHost m_oPluginHost
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _m_oPluginHost;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_m_oPluginHost != null)
                {
                    _m_oPluginHost.EventEchoText -= Plugin_EventEchoText;
                    _m_oPluginHost.EventSendText -= Plugin_EventSendText;
                    _m_oPluginHost.EventVariableChanged -= PluginHost_EventVariableChanged;
                }

                _m_oPluginHost = value;
                if (_m_oPluginHost != null)
                {
                    _m_oPluginHost.EventEchoText += Plugin_EventEchoText;
                    _m_oPluginHost.EventSendText += Plugin_EventSendText;
                    _m_oPluginHost.EventVariableChanged += PluginHost_EventVariableChanged;
                }
            }
        }

        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        private int LoadPlugins()
        {

            string sPluginPath = m_oGlobals.Config.PluginDir;
            if (m_bDebugPlugin)
            {
                sPluginPath = Application.StartupPath;
            }
            if (!Directory.Exists(sPluginPath))
            {
                //if the plugin path doesn't exist, let the user know and return
                string argsText1 = "Plugin Path Not Found! No Plugins were loaded. Please create a path at " + sPluginPath + System.Environment.NewLine;
                Genie.Game.WindowTarget argoTargetWindow1 = Genie.Game.WindowTarget.Main;
                AddText(argsText1, oTargetWindow: argoTargetWindow1);

                return 0;
            }
            // Get list of plugins
            var oAvailablePlugins = PluginServices.FindPlugins(sPluginPath);
            m_oPlugins.Clear();
            if (!Information.IsNothing(oAvailablePlugins))
            {
                m_oPlugins.AddRange(oAvailablePlugins);
            }

            if (m_oPlugins.Count == 0)
            {
                SafeUpdatePluginsMenuList();
                return 0;
            }

            m_oGlobals.PluginList.Clear();
            m_oPluginNameToFile.Clear();
            foreach (PluginServices.AvailablePlugin loadingPlugin in m_oPlugins)
            {
                switch (loadingPlugin.Interface)
                {
                    case PluginServices.Interfaces.Legacy:
                        GeniePlugin.Interfaces.IPlugin legacyPlugin = (GeniePlugin.Interfaces.IPlugin)PluginServices.CreateInstance(loadingPlugin);
                        LoadLegacyPlugin(legacyPlugin, loadingPlugin.AssemblyPath, loadingPlugin.Key);
                        break;
                    case PluginServices.Interfaces.Modern:
                        GeniePlugin.Plugins.IPlugin modernPlugin = (GeniePlugin.Plugins.IPlugin)PluginServices.CreateInstance(loadingPlugin);
                        LoadPlugin(modernPlugin, loadingPlugin.AssemblyPath, loadingPlugin.Key);
                        break;
                    default:
                        break;
                }
            }

            SafeUpdatePluginsMenuList();
            return m_oGlobals.PluginList.Count;
        }
        private void LoadLegacyPlugin(GeniePlugin.Interfaces.IPlugin Plugin, string AssemblyPath, string Key)
        {
            if (m_oPluginNameToFile.ContainsKey(Plugin.Name))
            {
                string DuplicateText = $"Duplicate Plugin Detected: {Plugin.Name}. \r\n" +
                    $"{m_oPluginNameToFile[Plugin.Name]} is the file which loaded. You can view its version in the Plugin menu.\r\n" +
                    $"{Path.GetFileName(AssemblyPath)} was not loaded. It reports its version as {Plugin.Version}.\r\n" +
                    $"Your plugin directory is at {Path.GetDirectoryName(AssemblyPath)}\r\n";
                AddText(DuplicateText, m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor());
            }
            else
            {
                m_oPluginNameToFile.Add(Plugin.Name, Path.GetFileName(AssemblyPath));
                string argsText = "Loading Plugin: " + Plugin.Name + ", Version: " + Plugin.Version + "...";
                Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                AddText(argsText, oTargetWindow: argoTargetWindow);
                VerifyAndLoadPlugin(Plugin, Key);
                if (m_oGlobals.PluginList.Contains(Plugin))
                {
                    string argsText1 = "OK" + System.Environment.NewLine;
                    Genie.Game.WindowTarget argoTargetWindow1 = Genie.Game.WindowTarget.Main;
                    AddText(argsText1, oTargetWindow: argoTargetWindow1);
                }
                else
                {
                    string argsText3 = "Failed" + System.Environment.NewLine;
                    Genie.Game.WindowTarget argoTargetWindow3 = Genie.Game.WindowTarget.Main;
                    AddText(argsText3, oTargetWindow: argoTargetWindow3);
                }
            }

            Application.DoEvents();
        }

        private void LoadPlugin(GeniePlugin.Plugins.IPlugin Plugin, string AssemblyPath, string Key)
        {
            if (m_oPluginNameToFile.ContainsKey(Plugin.Name))
            {
                string DuplicateText = $"Duplicate Plugin Detected: {Plugin.Name}. \r\n" +
                    $"{m_oPluginNameToFile[Plugin.Name]} is the file which loaded. You can view its version in the Plugin menu.\r\n" +
                    $"{Path.GetFileName(AssemblyPath)} was not loaded. It reports its version as {Plugin.Version}.\r\n" +
                    $"Your plugin directory is at {Path.GetDirectoryName(AssemblyPath)}\r\n";
                AddText(DuplicateText, m_oGlobals.PresetList["scriptecho"].FgColor.ToDrawingColor(), m_oGlobals.PresetList["scriptecho"].BgColor.ToDrawingColor());
            }
            else
            {
                m_oPluginNameToFile.Add(Plugin.Name, Path.GetFileName(AssemblyPath));
                string argsText = "Loading Plugin: " + Plugin.Name + ", Version: " + Plugin.Version + "...";
                Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
                AddText(argsText, oTargetWindow: argoTargetWindow);
                VerifyAndLoadPlugin(Plugin, Key);
                if (m_oGlobals.PluginList.Contains(Plugin))
                {
                    string argsText1 = "OK" + System.Environment.NewLine;
                    Genie.Game.WindowTarget argoTargetWindow1 = Genie.Game.WindowTarget.Main;
                    AddText(argsText1, oTargetWindow: argoTargetWindow1);
                }
                else
                {
                    string argsText3 = "Failed" + System.Environment.NewLine;
                    Genie.Game.WindowTarget argoTargetWindow3 = Genie.Game.WindowTarget.Main;
                    AddText(argsText3, oTargetWindow: argoTargetWindow3);
                }
            }
            Application.DoEvents();
        }

        private void LoadPlugin(string filename)
        {
            if (!filename.Contains(@"\"))
            {

                string sPluginPath = m_oGlobals.Config.PluginDir;
                if (m_bDebugPlugin)
                {
                    sPluginPath = Application.StartupPath;
                }

                try
                {
                    filename = Path.Combine(sPluginPath, filename);
                }
#pragma warning disable CS0168
                catch (ArgumentException ex)
#pragma warning restore CS0168
                {
                    AppendText("Plugin not found: " + filename + System.Environment.NewLine);
                    return;
                }
            }

            if (!File.Exists(filename))
            {
                AppendText("Plugin not found: " + filename + System.Environment.NewLine);
                return;
            }

            var oAvalabilePlugin = PluginServices.FindPlugin(filename, "GeniePlugin.Interfaces.IPlugin");
            if (Information.IsNothing(oAvalabilePlugin.Key))
            {
                AppendText("Plugin not found: " + filename + System.Environment.NewLine);
                return;
            }

            AppendText("Plugin loading: " + filename + System.Environment.NewLine);
            GeniePlugin.Interfaces.IPlugin oPlugin = (GeniePlugin.Interfaces.IPlugin)PluginServices.CreateInstance(oAvalabilePlugin);
            string argsText = PluginServices.GetMD5HashFromFile(filename);
            string strKey = Utility.GenerateKeyHash(argsText);
            UnloadPlugin(oPlugin.Name, strKey);
            if (!m_oPluginNameToFile.ContainsKey(oPlugin.Name))
            {
                m_oPluginNameToFile.Add(oPlugin.Name, Path.GetFileName(filename));
            }

            VerifyAndLoadPlugin(oPlugin, strKey);
        }

        private void EnableOrDisablePluginByFilename(string filename, bool value)
        {
            foreach (KeyValuePair<string, string> kvp in m_oPluginNameToFile)
            {
                if ((kvp.Value.ToLower() ?? "") == (filename.ToLower() ?? ""))
                {
                    string argsText = PluginServices.GetMD5HashFromFile(kvp.Value);
                    string strKey = Utility.GenerateKeyHash(argsText);
                    AppendText(Conversions.ToString("Plugin " + Interaction.IIf(value, "enable", "disable") + ": " + kvp.Key + System.Environment.NewLine));
                    EnableOrDisablePlugin(kvp.Key, value);
                }
            }
        }

        private void EnableOrDisablePlugin(string name, bool value)
        {
            foreach (object oPlugin in m_oGlobals.PluginList)
            {
                if (oPlugin is GeniePlugin.Interfaces.IPlugin)
                {
                    if (((oPlugin as GeniePlugin.Interfaces.IPlugin).Name ?? "") == (name ?? ""))
                    {
                        (oPlugin as GeniePlugin.Interfaces.IPlugin).Enabled = value;
                    }
                }
                else if (oPlugin is GeniePlugin.Plugins.IPlugin)
                {
                    if (((oPlugin as GeniePlugin.Plugins.IPlugin).Name ?? "") == (name ?? ""))
                    {
                        (oPlugin as GeniePlugin.Plugins.IPlugin).Enabled = value;
                    }
                }
            }
        }

        private void UnloadPlugin(string filename)
        {
            foreach (KeyValuePair<string, string> kvp in m_oPluginNameToFile)
            {
                if ((kvp.Value.ToLower() ?? "") == (filename.ToLower() ?? ""))
                {

                    string sPluginPath = m_oGlobals.Config.PluginDir;
                    if (m_bDebugPlugin)
                    {
                        sPluginPath = Application.StartupPath;
                    }

                    string argsText = PluginServices.GetMD5HashFromFile(Path.Combine(sPluginPath, kvp.Value));
                    string strKey = Utility.GenerateKeyHash(argsText);
                    AppendText("Plugin unload: " + kvp.Key + System.Environment.NewLine);
                    if (m_oPluginNameToFile.ContainsKey(kvp.Value))
                    {
                        m_oPluginNameToFile.Remove(kvp.Value);
                    }

                    UnloadPlugin(kvp.Key, strKey);
                }
            }
        }

        private void UnloadPluginByName(string name)
        {
            foreach (KeyValuePair<string, string> kvp in m_oPluginNameToFile)
            {
                if ((kvp.Key.ToLower() ?? "") == (name.ToLower() ?? ""))
                {

                    string sPluginPath = m_oGlobals.Config.PluginDir;
                    if (m_bDebugPlugin)
                    {
                        sPluginPath = Application.StartupPath;
                    }

                    string argsText = PluginServices.GetMD5HashFromFile(Path.Combine(sPluginPath, kvp.Value));
                    string strKey = Utility.GenerateKeyHash(argsText);
                    AppendText("Plugin unload: " + kvp.Key + System.Environment.NewLine);
                    if (m_oPluginNameToFile.ContainsKey(kvp.Value))
                    {
                        m_oPluginNameToFile.Remove(kvp.Value);
                    }

                    UnloadPlugin(kvp.Key, strKey);
                }
            }
        }

        private string PluginFileName(string name)
        {
            foreach (KeyValuePair<string, string> kvp in m_oPluginNameToFile)
            {
                if ((kvp.Key.ToLower() ?? "") == (name.ToLower() ?? ""))
                {
                    return kvp.Value;
                }
            }

            return null;
        }

        private void UnloadPlugin(string name, string key)
        {
            int RemoveIndex = -1;
            int I = 0;
            foreach (object oPlugin in m_oGlobals.PluginList)
            {
                if (oPlugin is GeniePlugin.Interfaces.IPlugin)
                {
                    if (((oPlugin as GeniePlugin.Interfaces.IPlugin).Name ?? "") == (name ?? ""))
                    {
                        (oPlugin as GeniePlugin.Interfaces.IPlugin).ParentClosing();
                        RemoveIndex = I;
                    }
                }
                else if (oPlugin is GeniePlugin.Plugins.IPlugin)
                {
                    if (((oPlugin as GeniePlugin.Plugins.IPlugin).Name ?? "") == (name ?? ""))
                    {
                        (oPlugin as GeniePlugin.Plugins.IPlugin).ParentClosing();
                        RemoveIndex = I;
                    }
                }
                I += 1;
            }

            if (RemoveIndex > -1)
            {
                m_oGlobals.PluginList.RemoveAt(RemoveIndex);
            }

            RemoveIndex = -1;
            I = 0;
            foreach (PluginServices.AvailablePlugin oPlugin in m_oPlugins)
            {
                if ((oPlugin.Key ?? "") == (key ?? ""))
                {
                    RemoveIndex = I;
                }

                I += 1;
            }

            if (RemoveIndex > -1)
            {
                m_oPlugins.RemoveAt(RemoveIndex);
            }
        }

        private void UnloadPlugins()
        {
            foreach (object oPlugin in m_oGlobals.PluginList)
            {
                if (oPlugin is GeniePlugin.Interfaces.IPlugin)
                    (oPlugin as GeniePlugin.Interfaces.IPlugin).ParentClosing();
                else if (oPlugin is GeniePlugin.Plugins.IPlugin)
                    (oPlugin as GeniePlugin.Plugins.IPlugin).ParentClosing();
            }
            m_oGlobals.PluginList.Clear();
            m_oPlugins.Clear();
        }

        private void ListPlugins()
        {
            AppendText("Plugins loaded:" + System.Environment.NewLine);
            foreach (object oPlugin in m_oGlobals.PluginList)
            {
                if (!Information.IsNothing(oPlugin))
                {
                    if (oPlugin is GeniePlugin.Interfaces.IPlugin)
                    {
                        AppendText(Conversions.ToString(Constants.vbTab + (oPlugin as GeniePlugin.Interfaces.IPlugin).Name + " " + (oPlugin as GeniePlugin.Interfaces.IPlugin).Version + " - " + Interaction.IIf((oPlugin as GeniePlugin.Interfaces.IPlugin).Enabled, "Enabled", "Disabled") + System.Environment.NewLine));
                        AppendText(Constants.vbTab + Constants.vbTab + m_oPluginNameToFile[(oPlugin as GeniePlugin.Interfaces.IPlugin).Name] + System.Environment.NewLine);
                    }
                    else if (oPlugin is GeniePlugin.Plugins.IPlugin)
                    {
                        AppendText(Conversions.ToString(Constants.vbTab + (oPlugin as GeniePlugin.Plugins.IPlugin).Name + " " + (oPlugin as GeniePlugin.Plugins.IPlugin).Version + " - " + Interaction.IIf((oPlugin as GeniePlugin.Plugins.IPlugin).Enabled, "Enabled", "Disabled") + System.Environment.NewLine));
                        AppendText(Constants.vbTab + Constants.vbTab + m_oPluginNameToFile[(oPlugin as GeniePlugin.Plugins.IPlugin).Name] + System.Environment.NewLine);
                    }
                }
            }

            AppendText(System.Environment.NewLine);
        }

        private void VerifyAndLoadPlugin(GeniePlugin.Interfaces.IPlugin plugin, string pluginkey)
        {
            if (!Information.IsNothing(plugin))
            {
                m_oGlobals.PluginList.Add(plugin);
                try
                {
                    m_oLegacyPluginHost.PluginKey = pluginkey;
                    plugin.Initialize(m_oLegacyPluginHost);
                }
                catch (Exception ex)
                {
                    ShowDialogPluginException(plugin, "Plugin", ex);
                    if (!Information.IsNothing(plugin))
                        plugin.Enabled = false;
                }
            }
        }

        private void VerifyAndLoadPlugin(GeniePlugin.Plugins.IPlugin plugin, string pluginkey)
        {
            if (!Information.IsNothing(plugin))
            {
                m_oGlobals.PluginList.Add(plugin);
                try
                {
                    m_oPluginHost.PluginKey = pluginkey;
                    plugin.Initialize(m_oPluginHost);
                }
                catch (Exception ex)
                {
                    ShowDialogPluginException(plugin, "Plugin", ex);
                    if (!Information.IsNothing(plugin))
                        plugin.Enabled = false;
                }
            }
        }
        private void Plugin_EventEchoText(string sText, Color oColor, Color oBgColor)
        {
            Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
            string argsTargetWindow = "";
            AddText(sText, oColor, oBgColor, oTargetWindow: argoTargetWindow, sTargetWindow: argsTargetWindow);
        }

        private void AutoMapper_EventEchoText(string sText, GenieColor oColor, GenieColor oBgColor)
        {
            Genie.Game.WindowTarget argoTargetWindow = Genie.Game.WindowTarget.Main;
            string argsTargetWindow = "";
            AddText(sText, oColor.ToDrawingColor(), oBgColor.ToDrawingColor(), oTargetWindow: argoTargetWindow, sTargetWindow: argsTargetWindow);
        }

        private void Plugin_EventSendText(string sText, string sPlugin)
        {
            SafePluginSendText(sText, sPlugin, false);
        }

        public delegate void PluginSendTextDelegate(string Text, string Plugin, bool ToQueue, bool DoCommand);

        public void SafePluginSendText(string sText, string sScript, bool bToQueue)
        {
            if (InvokeRequired == true)
            {
                var parameters = new object[] { sText, sScript, false, false };
                Invoke(new PluginSendTextDelegate(Script_EventSendText), parameters);
            }
            else
            {
                Script_EventSendText(sText, sScript, false, false);
            }
        }

        // \x and @
        public delegate void ParseInputBoxDelegate(string sText);

        public void SafeParseInputBox(string sText)
        {
            if (InvokeRequired == true)
            {
                var parameters = new[] { sText };
                Invoke(new ParseInputBoxDelegate(ParseInputBox), parameters);
            }
            else
            {
                ParseInputBox(sText);
            }
        }

        private void ParseInputBox(string sText)
        {
            try
            {
                if (sText.Contains(@"\@"))
                    sText = sText.Replace(@"\@", "§#§");
                if (sText.Contains(@"\x"))
                {
                    sText = sText.Replace(@"\x", "");
                    if (sText.Contains("@"))
                    {
                        TextBoxInput.Text = sText.Replace("@", "");
                        TextBoxInput.SelectionLength = 0;
                        TextBoxInput.SelectionStart = sText.IndexOf("@");
                    }
                    else
                    {
                        TextBoxInput.Text = sText;
                        TextBoxInput.SelectionLength = 0;
                        TextBoxInput.SelectionStart = int.MaxValue;
                    }
                }
                else if (sText.Contains("@"))
                {
                    int iLen = TextBoxInput.SelectionStart;
                    TextBoxInput.SelectionLength = 0;
                    TextBoxInput.SelectedText = sText.Replace("@", "");
                    TextBoxInput.SelectionLength = 0;
                    TextBoxInput.SelectionStart = iLen + sText.IndexOf("@");
                }

                if (sText.Contains("§#§"))
                    sText = sText.Replace("§#§", "@");
                TextBoxInput.Focus();
            }
            /* TODO ERROR: Skipped IfDirectiveTrivia */
            catch (Exception ex)
            {
                HandleGenieException("ParseInputBox", ex.Message, ex.ToString());
                /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
            }
        }

        public delegate void UpdatePluginsMenuListDelegate();
        public void SafeUpdatePluginsMenuList()
        {
            if (InvokeRequired)
            {
                Invoke(new UpdatePluginsMenuListDelegate(UpdatePluginsMenuList));
            }
            else
            {
                UpdatePluginsMenuList();
            }
        }
        public void UpdatePluginsMenuList()
        {
            PluginsToolStripMenuItem.DropDownItems.Clear();
            ToolStripMenuItem pluginDialogItem;
            pluginDialogItem = new ToolStripMenuItem();
            pluginDialogItem.BackColor = m_oGlobals.PresetList["ui.menu"].BgColor.ToDrawingColor();
            pluginDialogItem.ForeColor = m_oGlobals.PresetList["ui.menu"].FgColor.ToDrawingColor();
            pluginDialogItem.Name = "ToolStripMenuItemPluginDialog";
            pluginDialogItem.Text = "&Plugins...";
            pluginDialogItem.Click += PluginDialogItem_Click;
            PluginsToolStripMenuItem.DropDownItems.Add(pluginDialogItem);

            ToolStripMenuItem pluginUpdateItem;
            pluginUpdateItem = new ToolStripMenuItem();
            pluginUpdateItem.BackColor = m_oGlobals.PresetList["ui.menu"].BgColor.ToDrawingColor();
            pluginUpdateItem.ForeColor = m_oGlobals.PresetList["ui.menu"].FgColor.ToDrawingColor();
            pluginUpdateItem.Name = "ToolStripMenuItemPluginDialog";
            pluginUpdateItem.Text = "&Update Plugins";
            pluginUpdateItem.Click += updatePluginsToolStripMenuItem_Click;
            PluginsToolStripMenuItem.DropDownItems.Add(pluginUpdateItem);

            ToolStripMenuItem pluginSeparator = new ToolStripMenuItem();
            pluginSeparator.BackColor = m_oGlobals.PresetList["ui.menu"].BgColor.ToDrawingColor();
            pluginSeparator.ForeColor = m_oGlobals.PresetList["ui.menu"].FgColor.ToDrawingColor();
            pluginSeparator.Name = "ToolStripMenuItemPluginSeparator";
            PluginsToolStripMenuItem.DropDownItems.Add(pluginSeparator);
            int I = 1;
            foreach (object oPlugin in m_oGlobals.PluginList)
            {
                if (!Information.IsNothing(oPlugin))
                {
                    pluginDialogItem = new ToolStripMenuItem();
                    pluginDialogItem.BackColor = m_oGlobals.PresetList["ui.menu"].BgColor.ToDrawingColor();
                    pluginDialogItem.ForeColor = m_oGlobals.PresetList["ui.menu"].FgColor.ToDrawingColor();
                    if (oPlugin is GeniePlugin.Interfaces.IPlugin)
                    {
                        pluginDialogItem.Name = "ToolStripMenuItemPlugin" + (oPlugin as GeniePlugin.Interfaces.IPlugin).Name;
                        pluginDialogItem.Text = (oPlugin as GeniePlugin.Interfaces.IPlugin).Name;
                    }
                    else if (oPlugin is GeniePlugin.Plugins.IPlugin)
                    {
                        pluginDialogItem.Name = "ToolStripMenuItemPlugin" + (oPlugin as GeniePlugin.Plugins.IPlugin).Name;
                        pluginDialogItem.Text = (oPlugin as GeniePlugin.Plugins.IPlugin).Name;
                    }
                    pluginDialogItem.Tag = oPlugin;
                    // ti.Checked = oPlugin.Enabled
                    pluginDialogItem.Click += PluginMenuItem_Click;
                    PluginsToolStripMenuItem.DropDownItems.Add(pluginDialogItem);
                    I += 1;
                }
            }

            m_PluginDialog.ReloadList();
        }

        private void PluginDialogItem_Click(object sender, EventArgs e)
        {
            m_PluginDialog.MdiParent = this;
            m_PluginDialog.Show();
        }

        private void PluginMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem)
            {
                ToolStripMenuItem mi = (ToolStripMenuItem)sender;
                if (!Information.IsNothing(mi.Tag))
                {
                    if (mi.Tag is GeniePlugin.Interfaces.IPlugin)
                    {
                        GeniePlugin.Interfaces.IPlugin oPlugin = (GeniePlugin.Interfaces.IPlugin)mi.Tag;
                        try
                        {
                            oPlugin.Show();
                        }
                        catch (Exception ex)
                        {
                            ShowDialogPluginException(oPlugin, "Show", ex);
                            oPlugin.Enabled = false;
                        }
                    }
                }
            }
        }

        // SafeParsePluginText is for #parse only. Real version is in Game.vb
        public delegate void ParsePluginTextDelegate(string sText, string sWindow);

        public void SafeParsePluginText(string sText, string sWindow)
        {
            if (m_oGlobals.PluginsEnabled == false)
                return;
            if (InvokeRequired == true)
            {
                var parameters = new[] { sText, sWindow };
                Invoke(new ParsePluginTextDelegate(ParsePluginText), parameters);
            }
            else
            {
                ParsePluginText(sText, sWindow);
            }
        }

        private void ParsePluginText(string sText, string sWindow)
        {
            if (m_oGlobals.PluginsEnabled == false)
                return;
            try
            {
                if (m_oGlobals.Config.bAutoMapper)
                    m_oAutoMapper.ParseText(sText);
            }
            catch (Exception ex)
            {
                ShowDialogAutoMapperException("ParseText", ex);
            }

            foreach (object oPlugin in m_oGlobals.PluginList)
            {
                if (oPlugin is GeniePlugin.Interfaces.IPlugin)
                {
                    try
                    {
                        if ((oPlugin as GeniePlugin.Interfaces.IPlugin).Enabled)
                            (oPlugin as GeniePlugin.Interfaces.IPlugin).ParseText(sText, sWindow);
                    }
                    /* TODO ERROR: Skipped IfDirectiveTrivia */
                    catch (Exception ex)
                    {
                        ShowDialogPluginException((oPlugin as GeniePlugin.Interfaces.IPlugin), "ParseText", ex);
                        (oPlugin as GeniePlugin.Interfaces.IPlugin).Enabled = false;
                        /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
                    }
                }
                else if (oPlugin is GeniePlugin.Plugins.IPlugin)
                {
                    try
                    {
                        if ((oPlugin as GeniePlugin.Plugins.IPlugin).Enabled)
                            (oPlugin as GeniePlugin.Plugins.IPlugin).ParseText(sText, sWindow);
                    }
                    /* TODO ERROR: Skipped IfDirectiveTrivia */
                    catch (Exception ex)
                    {
                        ShowDialogPluginException((oPlugin as GeniePlugin.Plugins.IPlugin), "ParseText", ex);
                        (oPlugin as GeniePlugin.Plugins.IPlugin).Enabled = false;
                        /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
                    }
                }
            }
        }

        public delegate string ParsePluginInputDelegate(string sText);

        public string SafeParsePluginInput(string sText)
        {
            if (m_oGlobals.PluginsEnabled == false)
                return sText;
            if (InvokeRequired == true)
            {
                var parameters = new[] { sText };
                return Conversions.ToString(Invoke(new ParsePluginInputDelegate(ParsePluginInput), parameters));
            }
            else
            {
                return ParsePluginInput(sText);
            }
        }

        private string ParsePluginInput(string sText)
        {
            if (m_oGlobals.PluginsEnabled == false)
                return sText;
            try
            {
                if (m_oGlobals.Config.bAutoMapper)
                    m_oAutoMapper.ParseInput(sText);
            }
            catch (Exception ex)
            {
                ShowDialogAutoMapperException("ParseInput", ex);
            }

            foreach (object oPlugin in m_oGlobals.PluginList)
            {
                if (oPlugin is GeniePlugin.Interfaces.IPlugin)
                {
                    try
                    {
                        if ((oPlugin as GeniePlugin.Interfaces.IPlugin).Enabled | sText.StartsWith(Conversions.ToString(m_oGlobals.Config.cMyCommandChar)))
                        {
                            sText = (oPlugin as GeniePlugin.Interfaces.IPlugin).ParseInput(sText);
                        }
                    }
                    /* TODO ERROR: Skipped IfDirectiveTrivia */
                    catch (Exception ex)
                    {
                        ShowDialogPluginException((oPlugin as GeniePlugin.Interfaces.IPlugin), "Input", ex);
                        (oPlugin as GeniePlugin.Interfaces.IPlugin).Enabled = false;
                        /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
                    }
                }
                else if (oPlugin is GeniePlugin.Plugins.IPlugin)
                {
                    try
                    {
                        if ((oPlugin as GeniePlugin.Plugins.IPlugin).Enabled | sText.StartsWith(Conversions.ToString(m_oGlobals.Config.cMyCommandChar)))
                        {
                            sText = (oPlugin as GeniePlugin.Plugins.IPlugin).ParseInput(sText);
                        }
                    }
                    /* TODO ERROR: Skipped IfDirectiveTrivia */
                    catch (Exception ex)
                    {
                        ShowDialogPluginException((oPlugin as GeniePlugin.Plugins.IPlugin), "Input", ex);
                        (oPlugin as GeniePlugin.Plugins.IPlugin).Enabled = false;
                        /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
                    }
                }
            }

            return sText;
        }

        public delegate void ParsePluginVariableDelegate(string sVariable);

        public void SafeParsePluginVariable(string sVariable)
        {
            if (m_oGlobals.PluginsEnabled == false)
                return;
            if (InvokeRequired == true)
            {
                var parameters = new[] { sVariable };
                Invoke(new ParsePluginVariableDelegate(ParsePluginVariable), parameters);
            }
            else
            {
                ParsePluginVariable(sVariable);
            }
        }

        private void ParsePluginVariable(string sVariable)
        {
            if (m_oGlobals.PluginsEnabled == false)
                return;
            string sVar = sVariable;
            if (sVar.StartsWith("$"))
            {
                sVar = sVar.Substring(1);
            }

            try
            {
                if (m_oGlobals.Config.bAutoMapper)
                {
                    m_oAutoMapper.VariableChanged(sVar);
                    MapperSettings.VariableChanged(sVar);
                }

            }
            catch (Exception ex)
            {
                ShowDialogAutoMapperException("VariableChanged", ex);
            }

            foreach (object oPlugin in m_oGlobals.PluginList)
            {
                if (oPlugin is GeniePlugin.Interfaces.IPlugin)
                {
                    try
                    {
                        if ((oPlugin as GeniePlugin.Interfaces.IPlugin).Enabled)
                            (oPlugin as GeniePlugin.Interfaces.IPlugin).VariableChanged(sVar);
                    }
                    /* TODO ERROR: Skipped IfDirectiveTrivia */
                    catch (Exception ex)
                    {
                        ShowDialogPluginException((oPlugin as GeniePlugin.Interfaces.IPlugin), "VariableChanged", ex);
                        (oPlugin as GeniePlugin.Interfaces.IPlugin).Enabled = false;
                        /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
                    }
                }
                else if (oPlugin is GeniePlugin.Interfaces.IPlugin)
                {
                    try
                    {
                        if ((oPlugin as GeniePlugin.Plugins.IPlugin).Enabled)
                            (oPlugin as GeniePlugin.Plugins.IPlugin).VariableChanged(sVar);
                    }
                    /* TODO ERROR: Skipped IfDirectiveTrivia */
                    catch (Exception ex)
                    {
                        ShowDialogPluginException((oPlugin as GeniePlugin.Plugins.IPlugin), "VariableChanged", ex);
                        (oPlugin as GeniePlugin.Plugins.IPlugin).Enabled = false;
                        /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
                    }
                }
            }
        }

        public delegate void ParsePluginXMLDelegate(string sXML);

        public void SafeParseXMLVariable(string sXML)
        {
            if (m_oGlobals.PluginsEnabled == false)
                return;
            if (InvokeRequired == true)
            {
                var parameters = new[] { sXML };
                Invoke(new ParsePluginXMLDelegate(ParsePluginXML), parameters);
            }
            else
            {
                ParsePluginXML(sXML);
            }
        }

        private void ParsePluginXML(string sXML)
        {
            foreach (object oPlugin in m_oGlobals.PluginList)
            {
                if (oPlugin is GeniePlugin.Interfaces.IPlugin)
                {
                    try
                    {
                        if ((oPlugin as GeniePlugin.Interfaces.IPlugin).Enabled)
                            (oPlugin as GeniePlugin.Interfaces.IPlugin).ParseXML(sXML);
                    }
                    /* TODO ERROR: Skipped IfDirectiveTrivia */
                    catch (Exception ex)
                    {
                        ShowDialogPluginException((oPlugin as GeniePlugin.Interfaces.IPlugin), "ParseXML", ex);
                        (oPlugin as GeniePlugin.Interfaces.IPlugin).Enabled = false;
                        /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
                    }
                }
                else if (oPlugin is GeniePlugin.Plugins.IPlugin)
                {
                    try
                    {
                        if ((oPlugin as GeniePlugin.Plugins.IPlugin).Enabled)
                            (oPlugin as GeniePlugin.Plugins.IPlugin).ParseXML(sXML);
                    }
                    /* TODO ERROR: Skipped IfDirectiveTrivia */
                    catch (Exception ex)
                    {
                        ShowDialogPluginException((oPlugin as GeniePlugin.Plugins.IPlugin), "ParseXML", ex);
                        (oPlugin as GeniePlugin.Plugins.IPlugin).Enabled = false;
                        /* TODO ERROR: Skipped ElseDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
                    }
                }
            }
        }

        private void Plugin_ParsePluginXML(string xml)
        {
            SafeParseXMLVariable(xml);
        }

        private FormPlugins _m_PluginDialog;

        private FormPlugins m_PluginDialog
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _m_PluginDialog;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_m_PluginDialog != null)
                {
                    _m_PluginDialog.LoadPlugin -= FormPlugin_LoadPlugin;
                    _m_PluginDialog.UnloadPluginByName -= FormPlugin_UnloadPluginByName;
                    _m_PluginDialog.ReloadPluginByName -= FormPlugin_ReloadPluginByName;
                    _m_PluginDialog.ReloadPlugins -= FormPlugin_ReloadPlugins;
                }

                _m_PluginDialog = value;
                if (_m_PluginDialog != null)
                {
                    _m_PluginDialog.LoadPlugin += FormPlugin_LoadPlugin;
                    _m_PluginDialog.UnloadPluginByName += FormPlugin_UnloadPluginByName;
                    _m_PluginDialog.ReloadPluginByName += FormPlugin_ReloadPluginByName;
                    _m_PluginDialog.ReloadPlugins += FormPlugin_ReloadPlugins;
                }
            }
        }

        private void FormPlugin_LoadPlugin(string filename)
        {
            LoadPlugin(filename.Trim());
            SafeUpdatePluginsMenuList();
        }

        private void FormPlugin_UnloadPlugin(string filename)
        {
            UnloadPlugin(filename.Trim());
            SafeUpdatePluginsMenuList();
        }

        private void FormPlugin_UnloadPluginByName(string name)
        {
            UnloadPluginByName(name.Trim());
            SafeUpdatePluginsMenuList();
        }

        private void FormPlugin_ReloadPluginByName(string name)
        {
            string sPluginPath = m_oGlobals.Config.PluginDir;
            if (m_bDebugPlugin)
            {
                sPluginPath = Application.StartupPath;
            }

            string sFileName = PluginFileName(name);
            if (!Information.IsNothing(sFileName))
            {
                UnloadPluginByName(name.Trim());
                string sTemp = Path.Combine(sPluginPath, sFileName);
                LoadPlugin(sTemp);
                SafeUpdatePluginsMenuList();
            }
        }

        private void FormPlugin_ReloadPlugins()
        {
            AppendText("Reloading plugins ..." + System.Environment.NewLine);
            LoadPlugins();
            m_oOutputMain.RichTextBoxOutput.EndTextUpdate();
        }

        private void FormPlugin_DisablePlugin(string filename)
        {
            EnableOrDisablePluginByFilename(filename, false);
            SafeUpdatePluginsMenuList();
        }

        private void FormPlugin_EnablePlugin(string filename)
        {
            EnableOrDisablePluginByFilename(filename, true);
            SafeUpdatePluginsMenuList();
        }


        public delegate void PrintDialogPluginExceptionDelegate(GeniePlugin.Interfaces.IPlugin plugin, string section, Exception ex);

        private void HandleLegacyPluginException(GeniePlugin.Interfaces.IPlugin plugin, string section, Exception ex)
        {
            if (InvokeRequired == true)
            {
                var parameters = new object[] { plugin, section, ex };
                Invoke(new PrintDialogPluginExceptionDelegate(ShowDialogPluginException), parameters);
            }
            else
            {
                ShowDialogPluginException(plugin, section, ex);
            }
        }

        private void HandlePluginException(GeniePlugin.Plugins.IPlugin plugin, string section, Exception ex)
        {
            if (InvokeRequired == true)
            {
                var parameters = new object[] { plugin, section, ex };
                Invoke(new PrintDialogPluginExceptionDelegate(ShowDialogPluginException), parameters);
            }
            else
            {
                ShowDialogPluginException(plugin, section, ex);
            }
        }

        private void ShowDialogAutoMapperException(string section, Exception ex)
        {
            if (My.MyProject.Forms.DialogException.Visible == false)
            {
                var sbDetails = new StringBuilder();
                sbDetails.Append("AutoMapper Action:     ");
                sbDetails.Append(section);
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append(ex.Message);
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append("----------------------------------------------");
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append(ex.ToString());
                My.MyProject.Forms.DialogException.Show(this, sbDetails.ToString(), "There was an unexpected error in the AutoMapper. This may be due to a programming bug.");
            }
        }

        private void ShowDialogPluginException(GeniePlugin.Interfaces.IPlugin plugin, string section, Exception ex)
        {
            if (My.MyProject.Forms.DialogException.Visible == false)
            {
                string sPluginName = "Unknown";
                string sPluginVersion = "Unknown";
                if (!Information.IsNothing(plugin))
                {
                    sPluginName = Conversions.ToString(Interaction.IIf(Information.IsNothing(plugin.Name), "Unknown", plugin.Name));
                    sPluginVersion = Conversions.ToString(Interaction.IIf(Information.IsNothing(plugin.Version), "Unknown", plugin.Version));
                }

                var sbDetails = new StringBuilder();
                sbDetails.Append("Plugin Name:           ");
                sbDetails.Append(sPluginName);
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append("Plugin Version         ");
                sbDetails.Append(sPluginVersion);
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append("Plugin Action:         ");
                sbDetails.Append(section);
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append(ex.Message);
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append("----------------------------------------------");
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append(ex.ToString());
                My.MyProject.Forms.DialogException.Show(this, sbDetails.ToString(), "There was an unexpected error in the plugin " + sPluginName + ". This may be due to a programming bug.", "The plugin has been disabled.", "Please report the details of this error to the plugin author. You may also want to make sure you are running the latest version of this plugin.");
            }
        }

        private void ShowDialogPluginException(GeniePlugin.Plugins.IPlugin plugin, string section, Exception ex)
        {
            if (My.MyProject.Forms.DialogException.Visible == false)
            {
                string sPluginName = "Unknown";
                string sPluginVersion = "Unknown";
                if (!Information.IsNothing(plugin))
                {
                    sPluginName = Conversions.ToString(Interaction.IIf(Information.IsNothing(plugin.Name), "Unknown", plugin.Name));
                    sPluginVersion = Conversions.ToString(Interaction.IIf(Information.IsNothing(plugin.Version), "Unknown", plugin.Version));
                }

                var sbDetails = new StringBuilder();
                sbDetails.Append("Plugin Name:           ");
                sbDetails.Append(sPluginName);
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append("Plugin Version         ");
                sbDetails.Append(sPluginVersion);
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append("Plugin Action:         ");
                sbDetails.Append(section);
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append(ex.Message);
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append("----------------------------------------------");
                sbDetails.Append(System.Environment.NewLine);
                sbDetails.Append(ex.ToString());
                My.MyProject.Forms.DialogException.Show(this, sbDetails.ToString(), "There was an unexpected error in the plugin " + sPluginName + ". This may be due to a programming bug.", "The plugin has been disabled.", "Please report the details of this error to the plugin author. You may also want to make sure you are running the latest version of this plugin.");
            }
        }
    }
}
