using CPUIdleStabilizer.Core;
using CPUIdleStabilizer.Infra;
using CPUIdleStabilizer.UI;

namespace CPUIdleStabilizer
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();

            var settings = SettingsManager.Load();
            var controller = new LoadController();

            // Handle CLI arguments
            if (args.Length > 0 && args.Contains("--cli"))
            {
                HandleCli(args, controller, settings);
                return;
            }

            // UI Mode: Open the settings form as the main window
            Logger.Log("App starting in UI mode.");
            var trayContext = new TrayAppContext(controller, settings, startHidden: false);
            Application.Run(trayContext);
        }

        private static void HandleCli(string[] args, LoadController controller, UserSettings settings)
        {
            double target = settings.TargetTotalPercent;
            bool eco = settings.EcoMode;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--target":
                        if (i + 1 < args.Length && double.TryParse(args[i + 1], out double t))
                        {
                            target = t;
                            i++;
                        }
                        break;
                    case "--eco":
                        if (i + 1 < args.Length)
                        {
                            eco = args[i + 1].ToLower() == "on";
                            i++;
                        }
                        break;
                    case "--autostart":
                        if (i + 1 < args.Length)
                        {
                            bool enable = args[i + 1].ToLower() == "on";
                            UpdateAutostart(enable);
                            i++;
                        }
                        break;
                    case "--logpath":
                        Console.WriteLine(Logger.GetLogPath());
                        return;
                    case "--help":
                        PrintHelp();
                        return;
                }
            }

            Logger.Log($"Starting in CLI mode. Target: {target}%, Eco: {eco}");
            Console.WriteLine($"RyzenIdleStabiliser CLI Mode\nTarget: {target}%\nEco: {eco}\nPress Ctrl+C to stop.");
            
            controller.Start(target, eco);
            
            // Keep alive until Ctrl+C
            var exitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (s, e) => 
            {
                e.Cancel = true;
                exitEvent.Set();
            };
            exitEvent.WaitOne();
            
            controller.Stop();
            Logger.Log("CLI Mode stopped.");
        }

        private static void UpdateAutostart(bool enable)
        {
            const string runKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(runKey, true);
                if (key != null)
                {
                    if (enable) key.SetValue("RyzenIdleStabiliser", Application.ExecutablePath);
                    else key.DeleteValue("RyzenIdleStabiliser", false);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"CLI failed to set autostart: {ex.Message}");
            }
        }

        private static void PrintHelp()
        {
            var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
            Console.WriteLine($"RyzenIdleStabiliser v{v} Usage:");
            Console.WriteLine("  --cli              Run in CLI mode (no tray icon)");
            Console.WriteLine("  --target <num>     Set CPU target load percentage (1-10)");
            Console.WriteLine("  --eco <on|off>     Enable/Disable Eco Mode (jitter)");
            Console.WriteLine("  --autostart <on|off> Enable/Disable Start with Windows");
            Console.WriteLine("  --logpath          Print the path to the log file");
            Console.WriteLine("  --help             Show this help message");
        }
    }
}
