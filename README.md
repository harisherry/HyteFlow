# HyteFlow Studio — Universal 3D PC Fan & Chassis Telemetry Utility

![HyteFlow Studio Preview](docs/preview.png)

**HyteFlow Studio** is a lightweight, native 64-bit Windows hardware telemetry and universal fan controller built in C# / WPF with an **Apple macOS Dark Glass / Titanium Pro** design language. It detects and controls **every fan installed in any Windows PC**—across motherboard 4-pin PWM Super I/O headers, USB HID fan hubs, and GPU cooling shrouds—while mapping each fan channel to its **physical 3D location inside the case** (`Top`, `Side`, `Bottom`, `Rear`, `CPU/AIO`, `GPU`).

---

## Key Features

- **Apple Studio Dark Glass Design Language**:
  - Translucent dark slate surfaces, refined pill-shaped controls, `Segoe UI Variable Display` / `Cascadia Mono` typography, and Apple System Blue (`#0A84FF`) & Teal (`#64D2FF`) accents.
- **Interactive 3D Case Studio & Smart 3D Case Catalog (`🌐 Cases` & `3D Render` / `3D CAD`)**:
  - Displays a high-resolution **3D studio representation** of your chassis (`Thermalright TL-M10 Vision (9.16" LCD Panoramic)`, `HYTE Y70`, `Lian Li O11 Dynamic EVO / Vision`, `NZXT H9 Flow`, `Corsair 5000D/4000D Airflow`, `Montech King 95 Pro`, `Phanteks NV7`, `Fractal Design North`) with **perspective-foreshortened 3D spinning ARGB fan turbines**, 3D AIO LCD pump block, 3D vertical GPU shroud, live PSU Shroud LCD telemetry overlay, and HUD callouts.
  - Dynamically renders the exact number of physical fans (`1×`–`4×`) installed in each zone on the 3D chassis.
  - Rejects cropped or white-background catalog thumbnails and includes a built-in **Isometric 3D Architectural CAD** renderer (`3D Render` ↔ `3D CAD`) so any PC case is always shown in true 3D perspective.
- **✦ Auto-Place Fans & ＋ Add Fans**:
  - **`✦ Auto-Place Fans`** automatically detects spinning motherboard and GPU fan headers and places them into their corresponding 3D chassis zones (`CPU/AIO`, `Top`, `Side`, `Bottom`, `Rear`, `GPU`).
  - **`＋ Add Fans`** (or clicking any `+ CLICK TO ADD` zone badge directly on the 3D view) opens the Fan Group creator to add `1×`–`4×` (`120mm` or `140mm`) fans to any zone in your case.
- **Universal Multi-Controller Detection**:
  - **Motherboard Super I/O (`LibreHardwareMonitor` + `PawnIO`)**: Direct support for modern AM5/LGA1700/LGA1851 motherboards including **Gigabyte B850 / X870 (`ITE IT8696E`, `IT8689E`, `IT87952E`)**, ASUS/MSI/ASRock **Nuvoton (`NCT6798D`, `NCT6799D`)**, and ACPI Embedded Controllers.
  - **USB HID Fan Controllers (`CorsairLink` + `HidSharp`)**: Direct USB HID communication with **Corsair Commander PRO**, **Commander CORE / XT**, **iCUE LINK System Hub**, and **Hydro Series AIOs**, plus detection for **Thermalright**, **NZXT**, **Lian Li**, and **HYTE** USB HID devices.
  - **GPU & CPU Thermal Telemetry**: Real-time monitoring and fan control for **AMD Radeon RX / Ryzen** and **NVIDIA GeForce RTX** sensors.
- **⚡ 1-Click "Locate 100%" (Pulse & Identify)**:
  - Clicking **`⚡ Locate 100%`** pulses that single fan channel to **100% speed for 6 seconds** so you can watch or listen to which fan group spins up in your case and tag its location (`Top`, `Side`, `Bottom`, `Rear`, `CPU/AIO`, `GPU`).
- **Interactive Multi-Point Fan Curve Editor**:
  - Drag control points directly on the temperature-to-PWM grid (`20°C–95°C` vs `0%–100% PWM`) with a live operating crosshair and smooth ramp-up/down hysteresis.

---

## Quick Start (Using on Any PC)

1. **Clone or Download this Repository**:
   ```powershell
   git clone https://github.com/harisherry/HyteFlow.git
   cd HyteFlow
   ```
2. **Run HyteFlow**:
   - Double-click **`Run-HyteFlow-Admin.bat`** (recommended so `PawnIO` can unlock motherboard Super I/O registers) or launch **`HyteFlow.exe`** directly.
3. **Build from Source (Optional)**:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\build.ps1
   ```

---

## Documentation

See [**HOW_TO_USE.md**](HOW_TO_USE.md) for a complete step-by-step user guide covering 3D case selection, auto-placing fans, adding custom fan banks, curve tuning, and Windows auto-startup.
