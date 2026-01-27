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

            SettingsManager.CleanupLegacyData();
            var settings = SettingsManager.Load();
            var controller = new LoadController();

            // Handle CLI arguments
            bool isAutostart = args.Contains("--autostart", StringComparer.OrdinalIgnoreCase);
            if (args.Length > 0 && args.Contains("--cli"))
            {
                HandleCli(args, controller, settings);
                return;
            }

            // UI Mode: Open the settings form as the main window
            Logger.Log($"App starting in UI mode. Autostart: {isAutostart}");
            bool startHidden = args.Contains("--minimized", StringComparer.OrdinalIgnoreCase);
            var trayContext = new TrayAppContext(controller, settings, startHidden, isAutostart);
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
                            string val = args[i + 1].ToLower();
                            if (val == "on" || val == "off")
                            {
                                bool enable = val == "on";
                                SettingsManager.SetAutostart(enable, settings.StartMinimized);
                                i++;
                            }
                            // else it's just the flag --autostart without a value, which we handle above
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
            Console.WriteLine($"CPUIdleStabilizer CLI Mode\nTarget: {target}%\nEco: {eco}\nPress Ctrl+C to stop.");
            
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

        private static void PrintHelp()
        {
            var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.1";
            Console.WriteLine($"CPUIdleStabilizer v{v}");
            Console.WriteLine("GitHub: https://github.com/cjdatadev/CPUIdleStabilizer/");
            Console.WriteLine("\nUsage:");
            Console.WriteLine("  --cli              Run in CLI mode (no tray icon)");
            Console.WriteLine("  --target <num>     Set CPU target load percentage (1-10)");
            Console.WriteLine("  --eco <on|off>     Enable/Disable Eco Mode (jitter)");
            Console.WriteLine("  --autostart <on|off> Enable/Disable Start with Windows");
            Console.WriteLine("  --logpath          Print the path to the log file");
            Console.WriteLine("  --help             Show this help message");
        }
    }
}
