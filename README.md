# CPUIdleStabliser

**Prevents CPU Idle Instability by Maintaining a Low, Steady Load.**

## Overview
Modern CPUs (especially Ryzen 5000/7000/9000 series and ensuring Intel C-State stability) can sometimes become unstable when transitioning to extremely deep low-power idle states (`C6` or lower). This can cause random system freezes, black screens (BSoD), or restarts when the computer is doing absolutely nothing.

**CPUIdleStabliser** solves this by preventing the CPU from entering these problematic deep sleep states. It runs a very lightweight, controlled workload on *all* cores to keep them just active enough to remain stable, without consuming excess power or generating heat.

![CPUIdleStabliser](/app_icon.png)

## How It Works (Technical Details)

This application is **NOT** a stress test. It is a precision Load generator.

### The Mechanism
The application creates a dedicated low-priority thread for every logical core in your system. Each thread operates on a precise 1-second (1000ms) cycle using a **Pulse Width Modulation (PWM)** algorithm:

1.  **Work Phase (Duty Cycle):**
    -   For a small fraction of the second (e.g., 5% = 50ms), the thread performs basic arithmetic (`Math.Sqrt`) in a tight loop.
    -   Measurements are taken using high-resolution performance counters (`Stopwatch`) for microsecond accuracy.

2.  **Wait Phase (Sleep):**
    -   For the remainder of the second (e.g., 950ms), the thread strictly sleeps (`Thread.Sleep`), yielding all resources back to the OS.
    -   This allows the CPU to throttle down but prevents it from "parking" or turning off completely logic gates that trigger instability.

### Stabilization Features
-   **Staggered Start:** The threads do not fire all at once. Their start times are staggered across the 1000ms window. This ensures a consistent, flat power draw rather than "spiky" usage, which is healthier for the VRMs (Voltage Regulator Modules).
-   **Eco Mode (Jitter):** Introduces randomization to the load target (e.g., 5% ± 0.2%). This prevents resonance patterns and fixed-frequency loads that might interfere with other hardware sensors.
-   **System Priority:** All threads run at `Lowest` priority. Any other application (games, browser, etc.) will instantly take precedence. You will **not** lose FPS in games.

## Safety & Impact

### Is this safe?
**Yes, absolutely.**
-   **No Heat:** The load is typically 1-5%. This is comparable to moving your mouse rapidly or watching a YouTube video. It does not generate significant heat.
-   **No Damage:** It performs standard mathematical operations. It does not use "power virus" instruction sets like AVX or Prime95 FFTs.
-   **Battery Life:** On laptops, this *will* prevent deep sleep, so battery usage will be slightly higher (similar to browsing the web). On desktops, the impact is negligible (< 5-10 Watts).

## Usage

1.  **Start:** Click the large **START** button. The status bar will turn Green.
2.  **Settings:**
    -   **Target CPU Load:** Slider from 1% to 10%. (Recommended: **3% - 5%**).
    -   **Eco Mode:** Check to enable load randomization (Recommended: **ON**).
    -   **Start with Windows:** Adds the app to your Startup items.
3.  **Minimize:** Click "Hide to Tray" (or just close the window) to send it to the system tray. It runs silently in the background.

## Developer Info
-   **Framework:** .NET 8 (WinForms)
-   **Architecture:** x64 optimized
-   **License:** MIT
-   **Developer:** cjdatadev
