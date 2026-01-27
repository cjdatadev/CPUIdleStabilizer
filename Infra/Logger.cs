namespace CPUIdleStabilizer.Infra
{
    public static class Logger
    {
        private static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CPUIdleStabilizer");
        private static readonly string LogDir = Path.Combine(AppDataPath, "logs");
        private static readonly string LogFile = Path.Combine(LogDir, "app.log");
        private static readonly object _lock = new object();
        private const long MaxFileSizeBytes = 1 * 1024 * 1024; // 1MB
        private const int MaxOldFiles = 3;

        static Logger()
        {
            try
            {
                if (!Directory.Exists(LogDir)) Directory.CreateDirectory(LogDir);

                // Migration check: If new log directory is empty, look for old logs
                string oldAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RyzenIdleStabiliser");
                string oldLogDir = Path.Combine(oldAppDataPath, "logs");

                if (Directory.Exists(oldLogDir) && Directory.GetFiles(LogDir).Length == 0)
                {
                    foreach (string file in Directory.GetFiles(oldLogDir))
                    {
                        try
                        {
                            string dest = Path.Combine(LogDir, Path.GetFileName(file));
                            File.Move(file, dest, true);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        public static void Log(string message)
        {
            lock (_lock)
            {
                try
                {
                    CheckRotation();
                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
                    File.AppendAllText(LogFile, logEntry);
                }
                catch { }
            }
        }

        private static void CheckRotation()
        {
            if (!File.Exists(LogFile)) return;

            var info = new FileInfo(LogFile);
            if (info.Length < MaxFileSizeBytes) return;

            // Roll existing files
            for (int i = MaxOldFiles - 1; i >= 1; i--)
            {
                string oldFile = Path.Combine(LogDir, $"app.{i}.log");
                string nextFile = Path.Combine(LogDir, $"app.{i + 1}.log");
                if (File.Exists(oldFile))
                {
                    if (i + 1 > MaxOldFiles) File.Delete(oldFile);
                    else File.Move(oldFile, nextFile, true);
                }
            }

            File.Move(LogFile, Path.Combine(LogDir, "app.1.log"), true);
        }

        public static string GetLogPath() => LogFile;
    }
}
