using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections;
using System.Windows.Forms;
using GenieClient.Genie;
using Microsoft.VisualBasic.CompilerServices;

namespace GenieClient
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            LocalDirectory.ApplicationName = Application.ProductName;
            LocalDirectory.ApplicationVersion = Application.ProductVersion;
            CoreError.ErrorHandler = GenieError.Error;

            // Connection callbacks
            Connection.OnExitRequested = Application.Exit;

            // Game callbacks
            Game.PlaySound = Sound.PlayWaveFile;
            Game.FetchImageHandler = FileHandler.FetchImage;
            Game.ParsePluginTextHandler = ParsePluginText;

            // Command callbacks — Sound
            Command.PlayWaveFile = Sound.PlayWaveFile;
            Command.PlayWaveSystem = Sound.PlayWaveSystem;
            Command.StopPlaying = Sound.StopPlaying;

            // Command callbacks — Macros (wired after MacroList is created by FormMain)
            Command.MacroKeyToString = key => ((Keys)Conversions.ToInteger(key)).ToString();
            Command.MacroValueAction = val => ((Macros.Macro)val).sAction;

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(context.Configuration, services);
                })
                .Build();

            var services = host.Services;
            var formMain = services.GetRequiredService<FormMain>();

            // Wire Macro callbacks now that FormMain (and MacroList) are created
            Command.MacroSave = () => ((Macros)formMain.m_oGlobals.MacroList).Save();
            Command.MacroLoad = () => ((Macros)formMain.m_oGlobals.MacroList).Load();
            Command.MacroAdd = (key, action) => ((Macros)formMain.m_oGlobals.MacroList).Add(key, action);
            Command.MacroRemove = key => ((Macros)formMain.m_oGlobals.MacroList).Remove(key);

            formMain.DirectConnect(args);
            Application.Run(formMain);
        }

        private static string ParsePluginText(string sText, string sWindow, ArrayList pluginList, bool pluginsEnabled)
        {
            if (!pluginsEnabled)
                return sText;
            foreach (object oPlugin in pluginList)
            {
                if (oPlugin is GeniePlugin.Interfaces.IPlugin legacyPlugin)
                {
                    if (legacyPlugin.Enabled)
                    {
                        try
                        {
                            sText = legacyPlugin.ParseText(sText, sWindow);
                        }
                        catch (System.Exception ex)
                        {
                            CoreError.Error("Plugin.ParseText", ex.Message, ex.ToString());
                            legacyPlugin.Enabled = false;
                        }
                    }
                }
                else if (oPlugin is GeniePlugin.Plugins.IPlugin modernPlugin)
                {
                    if (modernPlugin.Enabled)
                    {
                        try
                        {
                            sText = modernPlugin.ParseText(sText, sWindow);
                        }
                        catch (System.Exception ex)
                        {
                            CoreError.Error("Plugin.ParseText", ex.Message, ex.ToString());
                            modernPlugin.Enabled = false;
                        }
                    }
                }
            }
            return sText;
        }

        private static void ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {
            services.AddSingleton<FormMain>();
        }
    }
}
