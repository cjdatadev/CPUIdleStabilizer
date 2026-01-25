===============================================================================
   _____ _____  _    _ _____     _ _      _____ _        _     _ _ _             
  / ____|  __ \| |  | |_   _|   | | |    / ____| |      | |   (_) (_)            
 | |    | |__) | |  | | | |   __| | | __| (___ | |_ __ _| |__  _| |_ _______ _ __ 
 | |    |  ___/| |  | | | |  / _` | |/ _` \___ \| __/ _` | '_ \| | | |_  / _ \ '__|
 | |____| |    | |__| |_| |_| (_| | | (_| |___) | || (_| | |_) | | | |/ /  __/ |   
  \_____|_|     \____/|_____|\__,_|_|\__,_|_____/ \__\__,_|_.__/|_|_|_/___\___|_|   
                                                                                   
===============================================================================

Prevents CPU Idle Instability by Maintaining a Low, Steady Load.

Version: 1.0.1
GitHub:  https://github.com/cjdatadev/CPUIdleStabilizer/
Reddit:  https://www.reddit.com/r/AMDHelp/comments/1qmag98/ryzen_5800x_b550_idle_crash_issue_created_a/

-------------------------------------------------------------------------------
[ OVERVIEW ]
-------------------------------------------------------------------------------
Modern CPUs (Ryzen 5000/7000/9000, Intel C-States) can become unstable when 
transitioning to deep low-power idle states (C6+). This causes random freezes, 
black screens, or restarts when the PC is idle.

CPUIdleStabilizer prevents the CPU from entering these states by running a 
controlled, lightweight workload on all cores.

-------------------------------------------------------------------------------
[ INSTALLATION / BUILDS ]
-------------------------------------------------------------------------------
1. STANDALONE VERSION (Recommended)
   - File: CPUIdleStabilizer.exe (~80 MB)
   - Requirements: None (Runs on Windows 10/11 out of the box).
   - Details: Includes the .NET 8 runtime embedded.

2. LIGHTWEIGHT VERSION
   - File: CPUIdleStabilizer.exe (~3 MB)
   - Requirements: .NET 8 Desktop Runtime must be installed.
   - Details: Much smaller, but depends on external runtime.

-------------------------------------------------------------------------------
[ HOW TO USE ]
-------------------------------------------------------------------------------
1. START: Click the large START button (Status turns Green).
2. TARGET LOAD: Slider 1% to 10% (Recommended: 3% - 5%).
3. ECO MODE: (Recommended: ON) Adds jitter to prevent fixed-frequency resonance.
4. AUTOSTART: Enable to launch with Windows.
5. MINIMIZE: "Hide to Tray" to run silently in background.

-------------------------------------------------------------------------------
[ COMMAND LINE INTERFACE ]
-------------------------------------------------------------------------------
CPUIdleStabilizer.exe --cli --target 3 --eco on

--cli              Headless mode (no UI)
--target <1-10>    Load percentage
--eco <on|off>     Enable/Disable jitter
--autostart <o/f>  Register Startup
--help             Show help

-------------------------------------------------------------------------------
[ GITHUB STAR ]
-------------------------------------------------------------------------------
If this tool helped solve your idle crashes, please give the project a star on 
GitHub to help others find it!

https://github.com/cjdatadev/CPUIdleStabilizer/

-------------------------------------------------------------------------------
[ DEVELOPER INFO ]
-------------------------------------------------------------------------------
Developer: cjdatadev
License:   MIT
Source:    C# / .NET 8 (WinForms)
===============================================================================
