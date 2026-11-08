# How to Use HyteFlow Studio on Any PC

This guide walks you through setting up **HyteFlow Studio**, identifying where each fan is physically located inside your case, tuning custom thermal curves, and configuring automatic Windows startup on any PC.

---

## 1. Launching HyteFlow & Unlocking Motherboard Headers

1. **Standard Launch**: Run `HyteFlow.exe`.
   - USB HID controllers (such as **Corsair Commander PRO / CORE / iCUE LINK**) and **NVIDIA / AMD GPU sensors** work immediately even without Administrator rights.
2. **Administrator Launch (Recommended for Motherboard 4-Pin Headers)**:
   - Motherboard Super I/O chips (such as the **ITE IT8696E / IT8689E** on Gigabyte B850/X870 boards or **Nuvoton NCT6798D** on ASUS/MSI/ASRock boards) require Administrator privileges to read/write hardware registers via `PawnIO`.
   - Click the amber **`⚡ UNLOCK SUPER I/O (ADMIN)`** button in the top bar of HyteFlow (or launch via `Run-HyteFlow-Admin.bat`) to restart with full motherboard header control.

---

## 2. Finding & Tagging Physical Fan Locations (Top, Bottom, Side, Rear)

Motherboard pins (`CPU_FAN`, `SYS_FAN1`–`SYS_FAN4`) and USB hubs (`Commander PRO Fan #1`–`#6`) do not automatically know where you mounted each fan inside your chassis. HyteFlow makes mapping them effortless:

1. **Show All Headers (Optional)**:
   - By default, HyteFlow hides empty `0 RPM` headers to keep the interface clean. Click **`◎ HIDE 0-RPM HEADERS`** in the top bar (switching it to **`◉ ALL HEADERS SHOWN`**) if you want to inspect every pin on your motherboard and USB hubs.
2. **Pulse a Fan to Identify It**:
   - On any fan card on the right panel, click **`⚡ LOCATE 100%`**.
   - That specific header will immediately ramp to **100% speed for 6 seconds** (displaying a cyan `LOCATING (6s)` badge).
   - Look inside your case (or listen) to see which fan bank sped up.
3. **Assign Its Physical Case Zone**:
   - While the fan is selected (or pulsing), click its physical zone either:
     - Directly on the **HYTE Y70 Dual-Chamber Blueprint** on the left (`TOP EXHAUST / 360 RAD`, `SIDE M/B INTAKE`, `BOTTOM COLD-FLOOR`, `REAR EXHAUST`, `CPU / AIO`, or `RTX VERTICAL`), **or**
     - Using the **`CASE POSITION:`** buttons (`TOP`, `SIDE`, `BOTTOM`, `REAR`, `CPU/AIO`, `GPU`) in the bottom-left inspector panel, **or**
     - Clicking the **`LOC: ... ▾`** button on the fan card itself.
4. **Set the Daisy-Chain / Splitter Fan Count (`1x`–`4x`)**:
   - If you have 3 fans daisy-chained together on a single cable (for example, 3x 120mm fans on the bottom cold-floor or top 360mm radiator), select the fan and click **`3x`** next to **`FANS ON HEADER:`** in the bottom-left inspector.
   - The blueprint and top bar (`INSTALLED TOPOLOGY`) will now accurately reflect the total number of physical fans spinning in that zone.

---

## 3. Controlling Fan Speeds & Customizing Thermal Curves

### Option A: Global 1-Click Acoustic Profiles
In the upper telemetry bar next to **`GLOBAL PROFILE:`**, click any preset to switch all controllable fans at once:
- **`SMART CURVES`**: Follows each channel's custom temperature curve.
- **`SILENT`**: Low-noise curve optimized for idle/desktop work.
- **`BALANCED`**: Everyday gaming and workstation balance.
- **`PERFORMANCE`**: Aggressive cooling ramp for heavy CPU/GPU loads.
- **`100% MAX BLAST`**: Immediately locks all fans to 100% maximum RPM.

### Option B: Manual Fixed Speed (%)
- Drag the **`SPEED %`** slider on any fan card on the right.
- That channel immediately switches to **`MANUAL (XX%)`** mode and holds that exact duty cycle.

### Option C: Custom Interactive Temperature Curve
1. Click any fan card on the right to open its curve in the **bottom-left Curve Editor**.
2. Click the **`SENSOR: ... ▾`** button to choose which temperature drives that fan zone:
   - **`Smart Mix: Max(CPU, GPU Core)`** *(Recommended for case intake/exhaust fans so they respond to both CPU and GPU gaming heat)*
   - **`CPU Package`** *(Recommended for CPU coolers / AIO radiators)*
   - **`GPU Core`** *(Recommended for bottom cold-floor fans blowing directly into the GPU)*
3. **Drag the orange control points** (`Temp °C` horizontal axis vs `PWM %` vertical axis).
   - The **cyan crosshair dot** shows the live temperature and current PWM output in real time.
   - All changes are automatically saved to `hyteflow_config.json`.

---

## 4. Starting Automatically with Windows & System Tray Mode

1. Click **`START WITH WINDOWS: OFF`** in the top-right header bar so it turns green (**`✓ START WITH WINDOWS: ON`**).
   - When enabled while running as Administrator, HyteFlow creates a Windows Scheduled Task (`HyteFlow_UniversalFanControl`) with highest privileges so it launches at Windows login without a UAC popup.
   - It also registers a user logon entry in `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
2. **System Tray Operation**:
   - Look for the **HyteFlow** icon in your Windows System Tray (notification area).
   - Right-click the tray icon at any time to switch between `Smart Curves`, `Silent`, `Balanced`, and `100% Max Blast`, or double-click it to bring the studio window back up.
   - Launching `HyteFlow.exe --minimized` starts HyteFlow directly in the system tray in the background.

---

## 5. Moving HyteFlow to Another PC

To use HyteFlow on any other PC:
1. Copy the folder (or `git clone` your repository) onto the new PC.
2. Delete `hyteflow_config.json` if you want HyteFlow to scan the new PC's motherboard, GPU, and USB controllers from scratch.
3. Run `Run-HyteFlow-Admin.bat` and use **`⚡ LOCATE 100%`** to map each fan header on the new PC in under a minute.
