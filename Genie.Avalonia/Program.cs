using System;
using System.Collections;
using Avalonia;
using GenieClient.Avalonia.Services;
using GenieClient.Genie;

namespace GenieClient.Avalonia
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Core engine wiring (mirrors WinForms Program.cs)
            LocalDirectory.ApplicationName = "Genie";
            LocalDirectory.ApplicationVersion = "4.0.0.0";
            CoreError.ErrorHandler = LogError;

            // Key converter callback (Avalonia → portable Keys enum)
            KeyCode.StringToKeyConverter = AvaloniaKeyConverter.ConvertString;

            // Connection exit callback
            Connection.OnExitRequested = () =>
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() => Environment.Exit(0));

            // Audio service (no-op for MVP)
            var audio = new NoOpAudioService();
            Game.Audio = audio;
            Command.Audio = audio;

            // Game callbacks
            Game.FetchImageHandler = null; // No images in Phase 1
            Game.ParsePluginTextHandler = ParsePluginText;

            // AutoMapper — unwired for Phase 1 (Phase 6)
            // AutoMapper.CreateMapView = ...
            // AutoMapper.ShowHandler = ...

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

        private static void LogError(string section, string message, string description)
        {
            Console.Error.WriteLine($"[{section}] {message}");
            if (!string.IsNullOrEmpty(description))
                Console.Error.WriteLine($"  {description}");
        }

        private static string ParsePluginText(string sText, string sWindow, ArrayList pluginList, bool pluginsEnabled)
        {
            if (!pluginsEnabled)
                return sText;

            foreach (object oPlugin in pluginList)
            {
                if (oPlugin is GeniePlugin.Interfaces.IPlugin plugin && plugin.Enabled)
                {
                    try
                    {
                        sText = plugin.ParseText(sText, sWindow);
                    }
                    catch (Exception ex)
                    {
                        CoreError.Error("Plugin.ParseText", ex.Message, ex.ToString());
                        plugin.Enabled = false;
                    }
                }
            }

            return sText;
        }
    }
}
