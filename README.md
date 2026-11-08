# HyteFlow Studio — Universal PC Fan & Chassis Telemetry Utility

![HyteFlow Studio Preview](docs/preview.png)

**HyteFlow Studio** is a lightweight, native 64-bit Windows hardware telemetry and universal fan controller built in C# / WPF. It is designed to detect and control **every fan installed in any Windows PC**—across motherboard 4-pin PWM Super I/O headers, USB HID fan hubs, and GPU cooling shrouds—while mapping each fan channel to its **physical location inside the case** (`TOP`, `SIDE`, `BOTTOM`, `REAR`, `CPU/AIO`, `GPU`).

---

## Key Features

- **Universal Multi-Controller Detection**:
  - **Motherboard Super I/O (`LibreHardwareMonitor` + `PawnIO`)**: Direct support for modern AM5/LGA1700/LGA1851 motherboards including **Gigabyte B850 / X870 (`ITE IT8696E`, `IT8689E`, `IT87952E`)**, ASUS/MSI/ASRock **Nuvoton (`NCT6798D`, `NCT6799D`)**, and ACPI Embedded Controllers.
  - **USB HID Fan Controllers (`CorsairLink` + `HidSharp`)**: Direct USB HID communication with **Corsair Commander PRO**, **Commander CORE / XT**, **iCUE LINK System Hub**, and **Hydro Series AIOs**, with automatic USB bus reconnect recovery. Also scans for **NZXT**, **Lian Li**, **HYTE**, and **ITE** USB HID devices.
  - **GPU & CPU Thermal Telemetry**: Real-time monitoring and fan control for **NVIDIA GeForce** (including RTX 40/50-series Core & VRAM Junction temperatures and dual shroud fans) and **AMD Ryzen / Radeon** sensors.
- **Interactive Chassis Blueprint & Physical Location Mapping**:
  - Visualizes your case's physical cooling zones (**Top Exhaust / 360mm Radiator**, **Side M/B Tray Intake**, **Bottom Cold-Floor Intake**, **Rear Exhaust**, **CPU / AIO**, and **Vertical GPU**).
  - Supports **daisy-chained and splitter fan topologies** (`1x`, `2x`, `3x`, `4x` fans per header) so your total physical fan count and per-zone airflow are tracked accurately.
- **⚡ 1-Click "Locate 100%" (Pulse & Identify)**:
  - Since motherboard and USB hub headers only report pin numbers (`SYS_FAN1`, `Fan #3`) and not where a fan is physically mounted in your case, clicking **`⚡ LOCATE 100%`** pulses that single fan channel to **100% speed for 6 seconds**. Watch or listen to which fan group spins up in your case, then click **`TOP`**, **`SIDE`**, **`BOTTOM`**, **`REAR`**, **`CPU/AIO`**, or **`GPU`** to tag its location.
- **Interactive Multi-Point Fan Curve Editor**:
  - Drag control points directly on the temperature-to-PWM grid (`20°C–95°C` vs `0%–100% PWM`) with a live operating crosshair and smooth ramp-up/down hysteresis.
  - Bind any fan channel to **Smart Mix: Max(CPU, GPU)**, **CPU Package**, or **GPU Core** temperature.
- **Start with Windows & System Tray Integration**:
  - One-click **`START WITH WINDOWS`** toggle registers Windows logon auto-start (using Windows Task Scheduler with highest privileges when running as Admin so there is no UAC prompt at boot, plus HKCU Run registry fallback).
  - Supports silent background startup via `HyteFlow.exe --minimized` and quick profile switching from the Windows System Tray.

---

## Quick Start (Using on Any PC)

1. **Clone or Download this Repository**:
   ```powershell
   git clone https://github.com/harisherry/HyteFlow.git
   cd HyteFlow
   ```
2. **Run HyteFlow**:
   - Double-click **`Run-HyteFlow-Admin.bat`** (recommended so `PawnIO` can unlock motherboard Super I/O registers) or launch **`HyteFlow.exe`** directly.
   - *Note*: For low-level motherboard Super I/O control on Windows 11 with Memory Integrity (HVCI) enabled, ensure [PawnIO](https://pawnio.eu/) is installed (included with modern LibreHardwareMonitor / FanControl setups).
3. **Build from Source (Optional)**:
   - HyteFlow compiles with zero external SDK downloads using the built-in Windows / Visual Studio Roslyn compiler:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\build.ps1
   ```

---

## Documentation

See [**HOW_TO_USE.md**](HOW_TO_USE.md) for a complete step-by-step user guide covering fan location discovery, curve tuning, multi-PC deployment, and Windows auto-startup.
