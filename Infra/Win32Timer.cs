using System.Runtime.InteropServices;

namespace CPUIdleStabliser.Infra
{
    public static class Win32Timer
    {
        [InitializOnLoad]
        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint period);

        [DllImport("winmm.dll")]
        private static extern uint timeEndPeriod(uint period);

        private static uint _currentPeriod = 0;

        public static void SetPrecision(uint period)
        {
            try 
            { 
                if (_currentPeriod != 0) ResetPrecision();
                timeBeginPeriod(period); 
                _currentPeriod = period;
            } catch { }
        }

        public static void ResetPrecision()
        {
            try 
            { 
                if (_currentPeriod != 0)
                {
                    timeEndPeriod(_currentPeriod); 
                    _currentPeriod = 0;
                }
            } catch { }
        }
    }

    internal class InitializOnLoadAttribute : Attribute { }
}
