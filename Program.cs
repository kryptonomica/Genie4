using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using GenieClient.Genie;
using GenieClient.Mapper;

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

            // Key converter callback (WinForms KeysConverter → portable Keys enum)
            KeyCode.StringToKeyConverter = sHotkey =>
                (KeyCode.Keys)(int)KeyCodeWinForms.StringToKey(sHotkey);

            // Connection callbacks
            Connection.OnExitRequested = Application.Exit;

            // Audio service (shared by Game and Command)
            var audio = new WinFormsAudioService();
            Game.Audio = audio;
            Command.Audio = audio;

            // Game callbacks
            Game.FetchImageHandler = FileHandler.FetchImage;
            Game.ParsePluginTextHandler = ParsePluginText;

            // AutoMapper factory
            AutoMapper.CreateMapView = globals => new MapForm(globals);

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(context.Configuration, services);
                })
                .Build();

            var services = host.Services;
            var formMain = services.GetRequiredService<FormMain>();

            // AutoMapper show handler (needs formMain reference for MDI layout)
            AutoMapper.ShowHandler = mapper =>
            {
                var mapForm = mapper.View as MapForm;
                if (mapForm == null) return;

                if (!mapForm.Visible)
                {
                    mapForm.MdiParent = formMain;
                    mapForm.Top = 0;
                    mapForm.Height = formMain.ClientHeight - SystemInformation.Border3DSize.Height * 2;
                    Size clientSize = (Size)formMain.ClientSize;
                    mapForm.Left = Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(clientSize.Width / 2 - SystemInformation.Border3DSize.Width);
                    mapForm.Width = Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(clientSize.Width - SystemInformation.Border3DSize.Width * 2 - mapForm.Left);
                    mapForm.Show();
                }

                mapForm.BringToFront();
            };

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
