using System.Diagnostics;
using CPUIdleStabilizer.Infra;

namespace CPUIdleStabilizer.Core
{
    public class LoadController
    {
        private CancellationTokenSource? _cts;
        private List<Thread> _threads = new List<Thread>();
        private List<LoadWorker> _workers = new List<LoadWorker>();
        
        public bool IsRunning { get; private set; }
        public int CoreCount { get; }
        public double TargetTotalPercent { get; private set; }
        public bool EcoMode { get; private set; }

        public LoadController()
        {
            CoreCount = Environment.ProcessorCount;
        }

        public void Start(double targetTotalPercent, bool ecoMode)
        {
            if (IsRunning) Stop();

            // Set high precision system timer
            CPUIdleStabilizer.Infra.Win32Timer.SetPrecision(1);

            // Set process priority as per requirements
            try
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
            }
            catch { /* Security or OS limitations */ }

            TargetTotalPercent = Math.Clamp(targetTotalPercent, 1.0, 10.0);
            EcoMode = ecoMode;
            _cts = new CancellationTokenSource();
            
            // If each core runs at X%, the total average is X%
            double perCoreDutyCycle = TargetTotalPercent / 100.0;
            
            _threads.Clear();
            _workers.Clear();
            int intervalMs = 1000; // Matching LoadWorker interval
            
            for (int i = 0; i < CoreCount; i++)
            {
                int coreIndex = i;
                // Stagger each core's start within the 100ms window
                int staggerMs = (i * intervalMs) / CoreCount;

                var worker = new LoadWorker(coreIndex, perCoreDutyCycle, EcoMode, _cts.Token);
                _workers.Add(worker);

                var thread = new Thread(() => 
                {
                    try
                    {
                        if (staggerMs > 0) Thread.Sleep(staggerMs);
                    }
                    catch { return; }

                    worker.Run();
                });
                
                thread.IsBackground = true;
                thread.Priority = ThreadPriority.Lowest;
                thread.Name = $"CPUWorker_{i}";
                _threads.Add(thread);
                thread.Start();
            }

            IsRunning = true;
        }

        public void Stop()
        {
            if (!IsRunning) return;

            _cts?.Cancel();
            try
            {
                // Give threads a moment to exit
                foreach (var t in _threads)
                {
                    if (t.IsAlive) t.Join(200);
                }
            }
            catch { /* Ignore */ }
            finally
            {
                CPUIdleStabilizer.Infra.Win32Timer.ResetPrecision();
                _cts?.Dispose();
                _cts = null;
                _threads.Clear();
                _workers.Clear();
                IsRunning = false;
            }
        }

        public void UpdateSettings(double targetTotalPercent, bool ecoMode)
        {
            TargetTotalPercent = Math.Clamp(targetTotalPercent, 1.0, 10.0);
            EcoMode = ecoMode;

            if (IsRunning)
            {
                double perCoreDutyCycle = TargetTotalPercent / 100.0;
                foreach (var worker in _workers)
                {
                    worker.DutyCyclePercent = perCoreDutyCycle;
                }
                Logger.Log($"Dynamic target update: {TargetTotalPercent}%");
            }
        }
    }
}
