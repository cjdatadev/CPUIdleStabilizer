using System.Diagnostics;

namespace CPUIdleStabilizer.Core
{
    public class LoadWorker
    {
        private readonly int _coreIndex;
        private double _dutyCyclePercent;
        private readonly object _lock = new object();
        
        public double DutyCyclePercent 
        { 
            get { lock(_lock) return _dutyCyclePercent; }
            set { lock(_lock) _dutyCyclePercent = value; }
        }

        private readonly bool _ecoMode;
        private readonly CancellationToken _token;
        private readonly Random _random;

        public LoadWorker(int coreIndex, double dutyCyclePercent, bool ecoMode, CancellationToken token)
        {
            _coreIndex = coreIndex;
            DutyCyclePercent = dutyCyclePercent;
            _ecoMode = ecoMode;
            _token = token;
            _random = new Random();
        }

        public void Run()
        {
            // Dedicated thread priority
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            
            var stopwatch = new Stopwatch();
            const int intervalMs = 1000; // 1 second interval for minimal overhead
            long intervalTicks = (long)(intervalMs * (Stopwatch.Frequency / 1000.0));
            
            // Initial anchor
            long cycleEndTicks = Stopwatch.GetTimestamp() + intervalTicks;

            while (!_token.IsCancellationRequested)
            {
                stopwatch.Restart();

                double currentDutyCycle = DutyCyclePercent;
                if (_ecoMode)
                {
                    // Add jitter: random +/- 2.5%
                    double jitter = (_random.NextDouble() * 0.05) - 0.025;
                    currentDutyCycle = Math.Max(0, DutyCyclePercent + jitter);
                }

                long busyTicks = (long)(currentDutyCycle * intervalTicks);
                
                // 1. WORK PHASE
                while (stopwatch.ElapsedTicks < busyTicks)
                {
                    if (_token.IsCancellationRequested) return;
                    
                    // Light computation pulse
                    for (int i = 0; i < 10; i++)
                    {
                        double a = _random.NextDouble();
                        _ = Math.Sqrt(a * a + 1.0);
                    }
                }

                // 2. WAIT PHASE (Pure Sleep)
                // Calculate how much time is left in the 100ms interval
                long currentTicks = Stopwatch.GetTimestamp();
                long remainingTicks = cycleEndTicks - currentTicks;
                int remainingMs = (int)(remainingTicks / (Stopwatch.Frequency / 1000.0));

                if (remainingMs > 1)
                {
                    try { _token.WaitHandle.WaitOne(remainingMs); }
                    catch { return; }
                }
                else
                {
                    // If we're late or have tiny time left, just yield once
                    Thread.Yield();
                }

                // Advance anchor for next cycle to maintain average frequency
                cycleEndTicks += intervalTicks;
                
                // Reset anchor if we drifted too far behind (> 200ms) to prevent burst catch-up
                if (Stopwatch.GetTimestamp() > cycleEndTicks + (intervalTicks * 2))
                {
                    cycleEndTicks = Stopwatch.GetTimestamp() + intervalTicks;
                }
            }
        }
    }
}
