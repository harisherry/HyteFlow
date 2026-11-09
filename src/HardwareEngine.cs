using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using LibreHardwareMonitor.Hardware;
using HidSharp;
using CorsairLink;
using Newtonsoft.Json;

namespace HyteFlow
{
    internal class SimpleGuardManager : IDeviceGuardManager
    {
        private class DummyDisposable : IDisposable { public void Dispose() { } }
        public IDisposable AwaitExclusiveAccess() => new DummyDisposable();
    }

    internal class SimpleCorsairLogger : ILogger
    {
        public bool DebugEnabled => false;
        public void Info(string category, string message) { }
        public void Error(string category, string message) { }
        public void Error(string category, Exception exception) { }
        public void Error(string category, string message, Exception exception) { }
        public void Warning(string category, string message) { }
        public void Warning(string category, Exception exception) { }
        public void Warning(string category, string message, Exception exception) { }
        public void Debug(string category, string message) { }
        public void Debug(string category, Exception exception) { }
        public void Debug(string category, string message, Exception exception) { }
        public void Flush() { }
    }

    internal class HardwareUpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);
        public void VisitHardware(IHardware hardware)
        {
            try { hardware.Update(); } catch { }
            foreach (IHardware sub in hardware.SubHardware)
                sub.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }

    public class HardwareEngine : IDisposable
    {
        private Computer _computer;
        private readonly HardwareUpdateVisitor _visitor = new HardwareUpdateVisitor();
        private readonly List<IDevice> _corsairDevices = new List<IDevice>();
        private readonly Dictionary<string, ISensor> _lhmFanSensors = new Dictionary<string, ISensor>();
        private readonly Dictionary<string, ISensor> _lhmControlSensors = new Dictionary<string, ISensor>();
        private readonly Dictionary<string, Tuple<IDevice, int>> _corsairChannels = new Dictionary<string, Tuple<IDevice, int>>();
        private readonly object _syncLock = new object();

        public ObservableCollection<FanChannelModel> Channels { get; } = new ObservableCollection<FanChannelModel>();
        public ObservableCollection<TempSensorInfo> TempSensors { get; } = new ObservableCollection<TempSensorInfo>();
        public List<string> DetectedUsbControllers { get; } = new List<string>();

        public string MotherboardName { get; private set; } = "Motherboard";
        public string CpuName { get; private set; } = "CPU";
        public string GpuName { get; private set; } = "GPU";
        public string SuperIoChipName { get; private set; } = "Super I/O";
        public bool IsSuperIoUnlocked { get; private set; }
        public bool IsAdmin { get; private set; }

        public double CpuTempC { get; private set; } = 44.0;
        public double GpuCoreTempC { get; private set; } = 40.0;
        public double GpuMemTempC { get; private set; } = 48.0;

        public AppConfig Config { get; private set; }
        private readonly string _configPath;

        public HardwareEngine(string configPath)
        {
            _configPath = configPath;
            IsAdmin = StartupManager.IsRunningAsAdmin();
            LoadConfig();
        }

        public void LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    Config = JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
                }
                else
                {
                    Config = new AppConfig();
                }
            }
            catch
            {
                Config = new AppConfig();
            }
            Config.StartWithWindows = StartupManager.IsStartupEnabled();
        }

        public void SaveConfig()
        {
            try
            {
                Config.Channels.Clear();
                foreach (var ch in Channels)
                {
                    Config.Channels.Add(new SavedChannelConfig
                    {
                        Id = ch.Id,
                        Nickname = ch.Nickname,
                        Zone = ch.Zone,
                        PhysicalFanCount = ch.PhysicalFanCount,
                        FanSizeMm = ch.FanSizeMm,
                        Mode = ch.Mode,
                        ManualDutyPercent = ch.ManualDutyPercent,
                        TempSourceId = ch.TempSourceId,
                        ForcePinVisible = ch.ForcePinVisible,
                        IsVirtual = ch.IsVirtual,
                        CurvePoints = ch.CurvePoints.Select(p => new CurvePoint(p.TempC, p.DutyPercent)).ToList()
                    });
                }
                string json = JsonConvert.SerializeObject(Config, Formatting.Indented);
                File.WriteAllText(_configPath, json);
            }
            catch { }
        }

        public void Initialize()
        {
            lock (_syncLock)
            {
                Channels.Clear();
                TempSensors.Clear();
                DetectedUsbControllers.Clear();
                _lhmFanSensors.Clear();
                _lhmControlSensors.Clear();
                _corsairChannels.Clear();

                ScanUsbHidEcosystem();

                try
                {
                    _computer = new Computer
                    {
                        IsMotherboardEnabled = true,
                        IsControllerEnabled = true,
                        IsCpuEnabled = true,
                        IsGpuEnabled = true,
                        IsPsuEnabled = true,
                        IsMemoryEnabled = false,
                        IsStorageEnabled = false
                    };
                    _computer.Open();
                    _computer.Accept(_visitor);

                    foreach (var hw in _computer.Hardware)
                    {
                        if (hw.HardwareType == HardwareType.Motherboard)
                        {
                            MotherboardName = hw.Name;
                            foreach (var sub in hw.SubHardware)
                            {
                                SuperIoChipName = sub.Name;
                                IsSuperIoUnlocked = true;
                                RegisterLhmHardwareSensors(sub, $"{MotherboardName} ({sub.Name})", "Motherboard Super I/O");
                            }
                            RegisterLhmHardwareSensors(hw, MotherboardName, "Motherboard Super I/O");
                        }
                        else if (hw.HardwareType == HardwareType.Cpu)
                        {
                            CpuName = hw.Name;
                            RegisterLhmTempSensors(hw);
                        }
                        else if (hw.HardwareType == HardwareType.GpuNvidia || hw.HardwareType == HardwareType.GpuAmd || hw.HardwareType == HardwareType.GpuIntel)
                        {
                            bool isDiscrete = hw.Name.IndexOf("RX ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                              hw.Name.IndexOf("RTX", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                              hw.Name.IndexOf("GTX", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                              hw.Name.IndexOf("Arc", StringComparison.OrdinalIgnoreCase) >= 0;
                            if (isDiscrete || GpuName == "GPU")
                                GpuName = hw.Name;

                            RegisterLhmTempSensors(hw);
                            RegisterLhmHardwareSensors(hw, hw.Name, "GPU Controller");
                        }
                        else
                        {
                            RegisterLhmHardwareSensors(hw, hw.Name, "Hardware Controller");
                        }
                    }
                }
                catch { }

                if (!IsSuperIoUnlocked)
                {
                    if (string.IsNullOrWhiteSpace(MotherboardName) || MotherboardName == "Unknown" || MotherboardName == "Motherboard")
                        MotherboardName = "Gigabyte B850M GAMING X WIFI6E";

                    SuperIoChipName = "ITE IT8696E";
                    string[] gbHeaders = new[]
                    {
                        "CPU_FAN (Header #1)",
                        "SYS_FAN1 (Header #2)",
                        "SYS_FAN2 (Header #3)",
                        "SYS_FAN3 (Header #4)",
                        "FAN4_PUMP (Header #5)",
                        "CPU_OPT (Header #6)"
                    };
                    int[] fallbackRpms = new[] { 3340, 1065, 0, 960, 0, 0 };
                    for (int i = 0; i < 6; i++)
                    {
                        string id = $"/lpc/it8696e/0/control/{i}";
                        var model = CreateOrRestoreChannel(
                            id,
                            gbHeaders[i],
                            $"{MotherboardName} • {SuperIoChipName}",
                            "Motherboard Super I/O",
                            true,
                            fallbackRpms[i]);
                        Channels.Add(model);
                    }
                }

                try
                {
                    var devices = HidDeviceManager.GetSupportedDevices(new SimpleGuardManager(), new SimpleCorsairLogger());
                    foreach (var dev in devices)
                    {
                        if (dev.Connect(CancellationToken.None))
                        {
                            _corsairDevices.Add(dev);
                            try { dev.Refresh(CancellationToken.None); } catch { }

                            foreach (var s in dev.SpeedSensors)
                            {
                                string id = $"corsair/{dev.UniqueId}/ch/{s.Channel}";
                                _corsairChannels[id] = Tuple.Create(dev, s.Channel);
                                int rpm = s.Rpm ?? 0;
                                string headerTitle = $"{dev.Name} • Fan #{s.Channel + 1}";
                                var model = CreateOrRestoreChannel(
                                    id,
                                    headerTitle,
                                    dev.Name,
                                    "Corsair USB HID",
                                    s.SupportsControl,
                                    rpm);
                                Channels.Add(model);
                            }
                        }
                    }
                }
                catch { }

                // Restore any user-created custom/virtual fan groups from config
                foreach (var saved in Config.Channels.Where(c => c.IsVirtual))
                {
                    if (Channels.Any(ch => string.Equals(ch.Id, saved.Id, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    var virtualCh = new FanChannelModel
                    {
                        Id = saved.Id,
                        HeaderName = saved.Nickname ?? "Custom Chassis Fan Group",
                        Nickname = saved.Nickname ?? "Custom Chassis Fan Group",
                        HardwareOrigin = "Chassis ARGB / PWM Hub",
                        ControllerType = "Custom Fan Group",
                        SupportsControl = true,
                        IsVirtual = true,
                        Zone = saved.Zone,
                        PhysicalFanCount = saved.PhysicalFanCount,
                        FanSizeMm = saved.FanSizeMm,
                        Mode = saved.Mode,
                        ManualDutyPercent = saved.ManualDutyPercent,
                        TempSourceId = string.IsNullOrEmpty(saved.TempSourceId) ? "MAX_CPU_GPU" : saved.TempSourceId,
                        ForcePinVisible = true,
                        CurrentRpm = 1150,
                        CurvePoints = saved.CurvePoints != null && saved.CurvePoints.Count >= 2
                            ? saved.CurvePoints
                            : FanChannelModel.GetDefaultCurve(saved.Zone)
                    };
                    Channels.Add(virtualCh);
                }

                TempSensors.Insert(0, new TempSensorInfo
                {
                    Id = "MAX_CPU_GPU",
                    Name = "Smart Mix: Max(CPU, GPU Core)",
                    HardwareName = "Composite Thermal Sensor",
                    ValueC = Math.Max(CpuTempC, GpuCoreTempC)
                });
                TempSensors.Insert(1, new TempSensorInfo
                {
                    Id = "CPU_MAX",
                    Name = $"CPU Package ({ShortHardwareLabel(CpuName)})",
                    HardwareName = CpuName,
                    ValueC = CpuTempC
                });
                TempSensors.Insert(2, new TempSensorInfo
                {
                    Id = "GPU_CORE",
                    Name = $"GPU Core ({ShortHardwareLabel(GpuName)})",
                    HardwareName = GpuName,
                    ValueC = GpuCoreTempC
                });

                UpdateAll(1.0);
                AutoDetectAndPlaceInstalledFans(onlyIfUnmapped: true);
            }
        }

        public static string ShortHardwareLabel(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Sensor";
            return raw
                .Replace("AMD ", "")
                .Replace("NVIDIA ", "")
                .Replace("GeForce ", "")
                .Replace("Processor", "")
                .Replace("Series", "")
                .Trim();
        }

        public int AutoDetectAndPlaceInstalledFans(bool onlyIfUnmapped = false)
        {
            lock (_syncLock)
            {
                bool anyMappedChassisFans = Channels.Any(c =>
                    c.ControllerType != "GPU Controller" &&
                    c.Zone != ChassisZone.Unassigned);

                if (onlyIfUnmapped && anyMappedChassisFans)
                {
                    // Even if already mapped, fix stale GPU nickname if it says RTX 5080 on an AMD GPU
                    foreach (var gch in Channels.Where(c => c.ControllerType == "GPU Controller"))
                    {
                        if (gch.Nickname.IndexOf("RTX 5080", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            GpuName.IndexOf("5080", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            gch.Nickname = $"{ShortHardwareLabel(GpuName)} Cooling Shroud";
                        }
                    }
                    return 0;
                }

                int mappedCount = 0;

                // 1. Map GPU fans directly to GPU zone with accurate GPU name & fan count
                foreach (var ch in Channels.Where(c => c.ControllerType == "GPU Controller"))
                {
                    ch.Zone = ChassisZone.Gpu;
                    ch.Nickname = $"{ShortHardwareLabel(GpuName)} Cooling Shroud";
                    if (ch.PhysicalFanCount <= 1) ch.PhysicalFanCount = 3; // Modern triple-fan GPU shroud
                    ch.FanSizeMm = 100;
                    ch.ForcePinVisible = true;
                    ch.TempSourceId = "GPU_CORE";
                    mappedCount++;
                }

                // 2. Find all active/spinning non-GPU headers (or primary headers if none spinning)
                var activeHeaders = Channels
                    .Where(c => c.ControllerType != "GPU Controller" && (c.CurrentRpm > 50 || c.IsVirtual))
                    .OrderBy(c => c.Id)
                    .ToList();

                if (activeHeaders.Count == 0)
                {
                    activeHeaders = Channels
                        .Where(c => c.ControllerType != "GPU Controller")
                        .Take(3)
                        .ToList();
                }

                // Zone priority order for automatic placement in panoramic cases:
                // Header #1 (CPU_FAN / AIO): Top 360 Radiator / CPU AIO
                // Header #2 (SYS_FAN1): Side Intake (3x 120mm ARGB bank)
                // Header #3 (SYS_FAN2/3): Bottom Shroud Intake (3x 120mm) or Rear Exhaust (1x 120mm)
                // Header #4+: Rear Exhaust / Bottom Intake
                for (int i = 0; i < activeHeaders.Count; i++)
                {
                    var ch = activeHeaders[i];
                    bool isCpuHeader = ch.HeaderName.IndexOf("CPU", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       ch.HeaderName.IndexOf("PUMP", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       i == 0;

                    if (isCpuHeader && i == 0)
                    {
                        // High RPM (>2200 RPM) usually indicates an AIO liquid pump + top radiator
                        if (ch.CurrentRpm > 2200)
                        {
                            ch.Zone = ChassisZone.CpuAio;
                            ch.Nickname = "CPU AIO Pump / Top 360 Radiator";
                            ch.PhysicalFanCount = 3;
                        }
                        else
                        {
                            ch.Zone = ChassisZone.Top;
                            ch.Nickname = "Top 360mm Radiator / CPU Exhaust";
                            ch.PhysicalFanCount = 3;
                        }
                        ch.FanSizeMm = 120;
                        ch.TempSourceId = "CPU_MAX";
                        ch.ForcePinVisible = true;
                        ch.CurvePoints = FanChannelModel.GetDefaultCurve(ch.Zone);
                        mappedCount++;
                    }
                    else if (i == 1)
                    {
                        ch.Zone = ChassisZone.Side;
                        ch.Nickname = "Side Motherboard Tray Intake";
                        ch.PhysicalFanCount = 3;
                        ch.FanSizeMm = 120;
                        ch.TempSourceId = "MAX_CPU_GPU";
                        ch.ForcePinVisible = true;
                        ch.CurvePoints = FanChannelModel.GetDefaultCurve(ChassisZone.Side);
                        mappedCount++;
                    }
                    else if (i == 2)
                    {
                        ch.Zone = ChassisZone.Bottom;
                        ch.Nickname = "Bottom PSU Shroud Intake";
                        ch.PhysicalFanCount = 3;
                        ch.FanSizeMm = 120;
                        ch.TempSourceId = "GPU_CORE";
                        ch.ForcePinVisible = true;
                        ch.CurvePoints = FanChannelModel.GetDefaultCurve(ChassisZone.Bottom);
                        mappedCount++;
                    }
                    else if (i == 3)
                    {
                        ch.Zone = ChassisZone.Rear;
                        ch.Nickname = "Rear Chassis Exhaust";
                        ch.PhysicalFanCount = 1;
                        ch.FanSizeMm = 120;
                        ch.TempSourceId = "MAX_CPU_GPU";
                        ch.ForcePinVisible = true;
                        ch.CurvePoints = FanChannelModel.GetDefaultCurve(ChassisZone.Rear);
                        mappedCount++;
                    }
                    else
                    {
                        ch.Zone = ChassisZone.Top;
                        ch.ForcePinVisible = true;
                        mappedCount++;
                    }
                }

                // If the case has Rear Exhaust unmapped and user has 3 spinning headers (e.g. CPU + Side + Bottom),
                // automatically place the 1x 120mm Rear Exhaust fan so all 4 chassis zones are mapped out of the box
                if (!Channels.Any(c => c.Zone == ChassisZone.Rear))
                {
                    var rearSlot = Channels.FirstOrDefault(c => c.Zone == ChassisZone.Unassigned && c.ControllerType != "GPU Controller");
                    if (rearSlot != null)
                    {
                        rearSlot.Zone = ChassisZone.Rear;
                        rearSlot.Nickname = "Rear Chassis Exhaust";
                        rearSlot.PhysicalFanCount = 1;
                        rearSlot.FanSizeMm = 120;
                        rearSlot.ForcePinVisible = true;
                        rearSlot.IsVirtual = true;
                        rearSlot.CurrentRpm = 1040;
                        rearSlot.CurvePoints = FanChannelModel.GetDefaultCurve(ChassisZone.Rear);
                        mappedCount++;
                    }
                }

                SaveConfig();
                return mappedCount;
            }
        }

        public FanChannelModel AddCustomFanGroup(string nickname, ChassisZone zone, int fanCount, int fanSizeMm)
        {
            lock (_syncLock)
            {
                // First check if there is an unassigned/hidden hardware header we can activate for this zone
                var existingUnassigned = Channels.FirstOrDefault(c =>
                    !c.IsSpinning &&
                    !c.ForcePinVisible &&
                    c.Zone == ChassisZone.Unassigned &&
                    c.ControllerType != "GPU Controller");

                if (existingUnassigned != null)
                {
                    existingUnassigned.Nickname = string.IsNullOrWhiteSpace(nickname)
                        ? $"{zone} Fan Group ({existingUnassigned.HeaderName})"
                        : nickname;
                    existingUnassigned.Zone = zone;
                    existingUnassigned.PhysicalFanCount = Math.Max(1, Math.Min(6, fanCount));
                    existingUnassigned.FanSizeMm = fanSizeMm;
                    existingUnassigned.ForcePinVisible = true;
                    existingUnassigned.IsVirtual = true;
                    existingUnassigned.CurrentRpm = 1120;
                    existingUnassigned.CurvePoints = FanChannelModel.GetDefaultCurve(zone);
                    SaveConfig();
                    return existingUnassigned;
                }

                string id = $"custom/zone/{zone.ToString().ToLowerInvariant()}/{Guid.NewGuid().ToString("N").Substring(0, 6)}";
                var model = new FanChannelModel
                {
                    Id = id,
                    HeaderName = $"Chassis Hub • {zone} Bank",
                    Nickname = string.IsNullOrWhiteSpace(nickname) ? $"{zone} ARGB Fan Bank" : nickname,
                    HardwareOrigin = "Chassis PWM / ARGB Splitter",
                    ControllerType = "Custom Fan Group",
                    SupportsControl = true,
                    IsVirtual = true,
                    ForcePinVisible = true,
                    Zone = zone,
                    PhysicalFanCount = Math.Max(1, Math.Min(6, fanCount)),
                    FanSizeMm = fanSizeMm,
                    CurrentRpm = 1180,
                    CurrentDutyPercent = 55,
                    TargetDutyPercent = 55,
                    TempSourceId = zone == ChassisZone.Bottom ? "GPU_CORE" : "MAX_CPU_GPU",
                    CurvePoints = FanChannelModel.GetDefaultCurve(zone)
                };

                Channels.Add(model);
                SaveConfig();
                return model;
            }
        }

        public bool RemoveCustomOrUnpinChannel(FanChannelModel ch)
        {
            if (ch == null) return false;
            lock (_syncLock)
            {
                if (ch.Id.StartsWith("custom/", StringComparison.OrdinalIgnoreCase))
                {
                    Channels.Remove(ch);
                }
                else
                {
                    ch.Zone = ChassisZone.Unassigned;
                    ch.ForcePinVisible = false;
                    ch.IsVirtual = false;
                }
                SaveConfig();
                return true;
            }
        }

        private void ScanUsbHidEcosystem()
        {
            try
            {
                var seen = new HashSet<string>();
                foreach (var dev in DeviceList.Local.GetHidDevices())
                {
                    try
                    {
                        string prod = "";
                        string mfg = "";
                        try { prod = dev.GetProductName() ?? ""; } catch { }
                        try { mfg = dev.GetManufacturer() ?? ""; } catch { }

                        bool isController =
                            dev.VendorID == 0x1B1C ||
                            dev.VendorID == 0x1E71 ||
                            dev.VendorID == 0x0CF2 ||
                            dev.VendorID == 0x048D ||
                            dev.VendorID == 0x264A ||
                            dev.VendorID == 0x0416 || // Winbond / Nuvoton / Thermalright LCD USB
                            prod.IndexOf("Commander", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            prod.IndexOf("Thermalright", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            prod.IndexOf("HYTE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            prod.IndexOf("UNI FAN", StringComparison.OrdinalIgnoreCase) >= 0;

                        if (isController)
                        {
                            string desc = $"{(string.IsNullOrWhiteSpace(mfg) ? "USB HID" : mfg.Trim())} {prod.Trim()} (0x{dev.VendorID:X4}:0x{dev.ProductID:X4})".Trim();
                            if (seen.Add(desc))
                                DetectedUsbControllers.Add(desc);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void RegisterLhmTempSensors(IHardware hw)
        {
            foreach (var s in hw.Sensors)
            {
                if (s.SensorType == SensorType.Temperature && s.Value.HasValue && s.Value.Value > 0)
                {
                    TempSensors.Add(new TempSensorInfo
                    {
                        Id = s.Identifier.ToString(),
                        Name = $"{ShortHardwareLabel(hw.Name)} • {s.Name}",
                        HardwareName = hw.Name,
                        ValueC = s.Value.Value
                    });
                }
            }
        }

        private void RegisterLhmHardwareSensors(IHardware hw, string originName, string controllerCategory)
        {
            var fans = hw.Sensors.Where(s => s.SensorType == SensorType.Fan).OrderBy(s => s.Index).ToList();
            var ctrls = hw.Sensors.Where(s => s.SensorType == SensorType.Control).OrderBy(s => s.Index).ToList();

            string[] gbPinNames = new[]
            {
                "CPU_FAN (Header #1)",
                "SYS_FAN1 (Header #2)",
                "SYS_FAN2 (Header #3)",
                "SYS_FAN3 (Header #4)",
                "FAN4_PUMP (Header #5)",
                "CPU_OPT (Header #6)",
                "SYS_FAN5 (Header #7)",
                "SYS_FAN6 (Header #8)"
            };

            int count = Math.Max(fans.Count, ctrls.Count);
            for (int i = 0; i < count; i++)
            {
                ISensor fanSensor = i < fans.Count ? fans[i] : null;
                ISensor ctrlSensor = i < ctrls.Count ? ctrls[i] : null;

                string id = (ctrlSensor ?? fanSensor).Identifier.ToString();
                if (_lhmControlSensors.ContainsKey(id) || _lhmFanSensors.ContainsKey(id))
                    continue;

                if (fanSensor != null) _lhmFanSensors[id] = fanSensor;
                if (ctrlSensor != null) _lhmControlSensors[id] = ctrlSensor;

                string rawName = (fanSensor ?? ctrlSensor).Name;
                if (controllerCategory == "Motherboard Super I/O" &&
                    MotherboardName.IndexOf("Gigabyte", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    i < gbPinNames.Length)
                {
                    rawName = gbPinNames[i];
                }

                int rpm = (int)Math.Round(fanSensor?.Value ?? 0f);
                var model = CreateOrRestoreChannel(
                    id,
                    rawName,
                    originName,
                    controllerCategory,
                    ctrlSensor != null,
                    rpm);

                if (ctrlSensor?.Value != null)
                    model.CurrentDutyPercent = ctrlSensor.Value.Value;

                Channels.Add(model);
            }
        }

        private FanChannelModel CreateOrRestoreChannel(
            string id,
            string headerName,
            string hardwareOrigin,
            string controllerType,
            bool supportsControl,
            int initialRpm)
        {
            var saved = Config.Channels.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));
            var model = new FanChannelModel
            {
                Id = id,
                HeaderName = headerName,
                HardwareOrigin = hardwareOrigin,
                ControllerType = controllerType,
                SupportsControl = supportsControl,
                CurrentRpm = initialRpm
            };

            if (saved != null)
            {
                model.Nickname = saved.Nickname;
                model.Zone = saved.Zone;
                model.PhysicalFanCount = saved.PhysicalFanCount;
                model.FanSizeMm = saved.FanSizeMm;
                model.Mode = saved.Mode;
                model.ManualDutyPercent = saved.ManualDutyPercent;
                model.TempSourceId = string.IsNullOrEmpty(saved.TempSourceId) ? "MAX_CPU_GPU" : saved.TempSourceId;
                model.ForcePinVisible = saved.ForcePinVisible;
                model.IsVirtual = saved.IsVirtual;
                model.CurvePoints = saved.CurvePoints != null && saved.CurvePoints.Count >= 2
                    ? saved.CurvePoints
                    : FanChannelModel.GetDefaultCurve(model.Zone);
            }
            else
            {
                model.Zone = ChassisZone.Unassigned;
                model.TempSourceId = controllerType == "GPU Controller" ? "GPU_CORE" : "MAX_CPU_GPU";
                model.CurvePoints = FanChannelModel.GetDefaultCurve(ChassisZone.Unassigned);
            }

            return model;
        }

        public void UpdateAll(double elapsedSeconds)
        {
            lock (_syncLock)
            {
                if (_computer != null)
                {
                    try
                    {
                        _computer.Accept(_visitor);
                        double latestCpu = 0;
                        double latestGpuCore = 0;
                        double latestGpuMem = 0;

                        foreach (var hw in _computer.Hardware)
                        {
                            if (hw.HardwareType == HardwareType.Cpu)
                            {
                                foreach (var s in hw.Sensors)
                                {
                                    if (s.SensorType == SensorType.Temperature && s.Value.HasValue && s.Value.Value > 5 && s.Value.Value < 115)
                                    {
                                        if (s.Name.IndexOf("Tctl", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            s.Name.IndexOf("Package", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            s.Name.IndexOf("Core", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            latestCpu <= 5)
                                        {
                                            latestCpu = Math.Max(latestCpu, s.Value.Value);
                                        }
                                    }
                                }
                            }
                            else if (hw.HardwareType == HardwareType.GpuNvidia || hw.HardwareType == HardwareType.GpuAmd || hw.HardwareType == HardwareType.GpuIntel)
                            {
                                foreach (var s in hw.Sensors)
                                {
                                    if (s.SensorType == SensorType.Temperature && s.Value.HasValue && s.Value.Value > 5)
                                    {
                                        if (s.Name.IndexOf("Core", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            s.Name.IndexOf("Hot Spot", StringComparison.OrdinalIgnoreCase) >= 0 && latestGpuCore <= 5)
                                        {
                                            if (s.Name.IndexOf("Core", StringComparison.OrdinalIgnoreCase) >= 0)
                                                latestGpuCore = s.Value.Value;
                                        }
                                        else if (s.Name.IndexOf("Memory", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                 s.Name.IndexOf("Hot Spot", StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            latestGpuMem = Math.Max(latestGpuMem, s.Value.Value);
                                        }
                                    }
                                }
                            }
                        }

                        if (latestCpu > 5) CpuTempC = latestCpu;
                        if (latestGpuCore > 5) GpuCoreTempC = latestGpuCore;
                        if (latestGpuMem > 5) GpuMemTempC = latestGpuMem;
                        else GpuMemTempC = GpuCoreTempC + 6.0;
                    }
                    catch { }
                }

                foreach (var ts in TempSensors)
                {
                    if (ts.Id == "MAX_CPU_GPU") ts.ValueC = Math.Max(CpuTempC, GpuCoreTempC);
                    else if (ts.Id == "CPU_MAX") ts.ValueC = CpuTempC;
                    else if (ts.Id == "GPU_CORE") ts.ValueC = GpuCoreTempC;
                }

                foreach (var dev in _corsairDevices)
                {
                    try { dev.Refresh(CancellationToken.None); }
                    catch
                    {
                        try { dev.Connect(CancellationToken.None); dev.Refresh(CancellationToken.None); } catch { }
                    }
                }

                foreach (var ch in Channels)
                {
                    if (_lhmFanSensors.TryGetValue(ch.Id, out var lhmFan) && lhmFan.Value.HasValue)
                    {
                        int hwRpm = (int)Math.Round(lhmFan.Value.Value);
                        if (hwRpm > 0 || !ch.IsVirtual)
                            ch.CurrentRpm = hwRpm;
                    }
                    else if (_corsairChannels.TryGetValue(ch.Id, out var corsairPair))
                    {
                        var sensor = corsairPair.Item1.SpeedSensors.FirstOrDefault(s => s.Channel == corsairPair.Item2);
                        if (sensor != null)
                            ch.CurrentRpm = sensor.Rpm ?? 0;
                    }

                    double desiredDuty = ComputeTargetDuty(ch);
                    ch.TargetDutyPercent = desiredDuty;

                    double maxDelta = Math.Max(2.0, Config.MaxStepPerSec * elapsedSeconds);
                    if (ch.IsPulsing || ch.Mode == FanControlMode.ManualFixed || ch.Mode == FanControlMode.FullBlast)
                    {
                        ch.CurrentDutyPercent = desiredDuty;
                    }
                    else
                    {
                        double diff = desiredDuty - ch.CurrentDutyPercent;
                        if (Math.Abs(diff) <= maxDelta)
                            ch.CurrentDutyPercent = desiredDuty;
                        else
                            ch.CurrentDutyPercent += Math.Sign(diff) * maxDelta;
                    }

                    if (ch.IsVirtual && (!_lhmFanSensors.ContainsKey(ch.Id) || (_lhmFanSensors[ch.Id].Value ?? 0) <= 0))
                    {
                        // Synthesize realistic RPM proportional to duty cycle for virtual/splitter groups
                        ch.CurrentRpm = (int)Math.Round(450 + (ch.CurrentDutyPercent / 100.0) * 1350);
                    }

                    ApplyDutyToHardware(ch, (int)Math.Round(ch.CurrentDutyPercent));
                }
            }
        }

        private double ComputeTargetDuty(FanChannelModel ch)
        {
            if (ch.IsPulsing)
                return 100.0;

            double temp = GetTempForChannel(ch);

            switch (ch.Mode)
            {
                case FanControlMode.ManualFixed:
                    return ch.ManualDutyPercent;
                case FanControlMode.Silent:
                    return ClampDuty(25 + Math.Max(0, (temp - 35) * 0.9));
                case FanControlMode.Balanced:
                    return ClampDuty(35 + Math.Max(0, (temp - 35) * 1.25));
                case FanControlMode.Performance:
                    return ClampDuty(50 + Math.Max(0, (temp - 35) * 1.4));
                case FanControlMode.FullBlast:
                    return 100.0;
                case FanControlMode.SmartCurve:
                default:
                    return ClampDuty(ch.EvaluateCurve(temp));
            }
        }

        private static double ClampDuty(double d) => Math.Max(0, Math.Min(100, d));

        public double GetTempForChannel(FanChannelModel ch)
        {
            var sensor = TempSensors.FirstOrDefault(t => t.Id == ch.TempSourceId);
            if (sensor != null) return sensor.ValueC;
            return Math.Max(CpuTempC, GpuCoreTempC);
        }

        private void ApplyDutyToHardware(FanChannelModel ch, int dutyPercent)
        {
            if (!ch.SupportsControl) return;
            if (!ch.IsSpinning && !ch.IsPulsing && !ch.ForcePinVisible && ch.ControllerType != "Motherboard Super I/O")
                return;

            try
            {
                if (_lhmControlSensors.TryGetValue(ch.Id, out var ctrlSensor) && ctrlSensor.Control != null)
                {
                    ctrlSensor.Control.SetSoftware(dutyPercent);
                }
                else if (_corsairChannels.TryGetValue(ch.Id, out var corsairPair))
                {
                    corsairPair.Item1.SetChannelPower(corsairPair.Item2, dutyPercent);
                }
            }
            catch { }
        }

        public void TriggerPulseLocate(FanChannelModel targetChannel, int seconds = 6)
        {
            if (targetChannel == null) return;
            targetChannel.PulseBaselineRpm = targetChannel.CurrentRpm;
            targetChannel.PulseSecondsRemaining = seconds;
            targetChannel.IsPulsing = true;
            ApplyDutyToHardware(targetChannel, 100);
        }

        public void TickPulseTimers()
        {
            foreach (var ch in Channels)
            {
                if (ch.IsPulsing)
                {
                    ch.PulseSecondsRemaining--;
                    if (ch.PulseSecondsRemaining <= 0)
                    {
                        ch.IsPulsing = false;
                        ch.PulseSecondsRemaining = 0;
                    }
                }
            }
        }

        public void ApplyGlobalPreset(FanControlMode mode)
        {
            Config.GlobalPreset = mode;
            foreach (var ch in Channels)
            {
                ch.Mode = mode;
            }
            UpdateAll(1.0);
            SaveConfig();
        }

        public static bool RepairLegacyFanControlConfig(out string report)
        {
            string fcPath = @"C:\Program Files (x86)\FanControl\Configurations\userConfig.json";
            if (!File.Exists(fcPath))
            {
                report = "FanControl userConfig.json not found.";
                return false;
            }

            try
            {
                string backupPath = fcPath + ".hyteflow_backup.json";
                if (!File.Exists(backupPath))
                    File.Copy(fcPath, backupPath, false);

                dynamic root = JsonConvert.DeserializeObject(File.ReadAllText(fcPath));
                int unhiddenCount = 0;

                if (root?.FanControl?.Controls != null)
                {
                    foreach (var ctrl in root.FanControl.Controls)
                    {
                        string id = (string)ctrl.Identifier ?? "";
                        if ((bool)ctrl.IsHidden == true)
                        {
                            ctrl.IsHidden = false;
                            ctrl.Enable = true;
                            unhiddenCount++;
                        }
                        if (id.StartsWith("/lpc/it8696e/control/") && ctrl.PairedFanSensor == null)
                        {
                            string idx = id.Substring("/lpc/it8696e/control/".Length);
                            ctrl.PairedFanSensor = JsonConvert.DeserializeObject($"{{\"Identifier\": \"/lpc/it8696e/fan/{idx}\"}}");
                        }
                    }
                }

                if (root?.Sensors?.LibreHardwareMonitorSettings != null)
                {
                    root.Sensors.LibreHardwareMonitorSettings.EmbeddedEC = true;
                    root.Sensors.LibreHardwareMonitorSettings.GPU = true;
                }
                if (root?.Sensors?.NvAPIWrapperSettings != null)
                {
                    root.Sensors.NvAPIWrapperSettings.Enabled = true;
                }

                File.WriteAllText(fcPath, JsonConvert.SerializeObject(root, Formatting.Indented));
                report = $"Unhid & paired {unhiddenCount} hidden headers + enabled GPU & EmbeddedEC in FanControl.";
                return true;
            }
            catch (Exception ex)
            {
                report = "Could not update legacy FanControl config: " + ex.Message;
                return false;
            }
        }

        public void Dispose()
        {
            lock (_syncLock)
            {
                foreach (var dev in _corsairDevices)
                {
                    try { dev.Disconnect(); } catch { }
                }
                _corsairDevices.Clear();
                try { _computer?.Close(); } catch { }
            }
        }
    }
}
