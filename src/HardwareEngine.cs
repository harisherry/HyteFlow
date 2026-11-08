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
        public string SuperIoChipName { get; private set; } = "ITE Super I/O";
        public bool IsSuperIoUnlocked { get; private set; }
        public bool IsAdmin { get; private set; }

        public double CpuTempC { get; private set; } = 42.0;
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
                            if (hw.HardwareType == HardwareType.GpuNvidia || GpuName == "GPU")
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
                        MotherboardName = "Gigabyte X870 EAGLE WIFI7";

                    SuperIoChipName = "ITE IT8696E (Gigabyte 800-Series)";
                    string[] gbHeaders = new[]
                    {
                        "CPU_FAN (Motherboard Header #1)",
                        "SYS_FAN1 (Motherboard Header #2)",
                        "SYS_FAN2 (Motherboard Header #3)",
                        "SYS_FAN3 (Motherboard Header #4)",
                        "FAN4_PUMP (Motherboard Header #5)",
                        "CPU_OPT (Motherboard Header #6)"
                    };
                    for (int i = 0; i < 6; i++)
                    {
                        string id = $"/lpc/it8696e/control/{i}";
                        var model = CreateOrRestoreChannel(
                            id,
                            gbHeaders[i],
                            $"{MotherboardName} • ITE IT8696E",
                            "Motherboard Super I/O",
                            true,
                            i == 0 ? 1180 : 0);
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
                    Name = $"CPU Package ({CpuName})",
                    HardwareName = CpuName,
                    ValueC = CpuTempC
                });
                TempSensors.Insert(2, new TempSensorInfo
                {
                    Id = "GPU_CORE",
                    Name = $"GPU Core ({GpuName})",
                    HardwareName = GpuName,
                    ValueC = GpuCoreTempC
                });

                ApplyInitialHyteY70SmartDefaults();
                UpdateAll(1.0);
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
                            prod.IndexOf("Commander", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            prod.IndexOf("HYTE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            prod.IndexOf("UNI FAN", StringComparison.OrdinalIgnoreCase) >= 0;

                        if (isController)
                        {
                            string desc = $"{(string.IsNullOrWhiteSpace(mfg) ? "USB HID" : mfg.Trim())} {prod.Trim()} (VID:0x{dev.VendorID:X4} PID:0x{dev.ProductID:X4})".Trim();
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
                        Name = $"{hw.Name} • {s.Name}",
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

        private void ApplyInitialHyteY70SmartDefaults()
        {
            if (Config.Channels.Any(c => c.Zone != ChassisZone.Unassigned))
                return;

            foreach (var ch in Channels)
            {
                if (ch.Id.IndexOf("/it8696e/control/0", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    ch.HeaderName.IndexOf("CPU_FAN", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ch.Zone = ChassisZone.CpuAio;
                    ch.Nickname = "AIO Radiator / CPU Fan";
                    ch.PhysicalFanCount = 3;
                    ch.FanSizeMm = 120;
                    ch.ForcePinVisible = true;
                    ch.CurvePoints = FanChannelModel.GetDefaultCurve(ChassisZone.CpuAio);
                }
                else if (ch.Id.EndsWith("/ch/2", StringComparison.OrdinalIgnoreCase))
                {
                    ch.Zone = ChassisZone.Top;
                    ch.Nickname = "Hyte Y70 Top Exhaust";
                    ch.PhysicalFanCount = 3;
                    ch.FanSizeMm = 120;
                    ch.CurvePoints = FanChannelModel.GetDefaultCurve(ChassisZone.Top);
                }
                else if (ch.Id.EndsWith("/ch/3", StringComparison.OrdinalIgnoreCase))
                {
                    ch.Zone = ChassisZone.Side;
                    ch.Nickname = "Hyte Y70 Side Intake";
                    ch.PhysicalFanCount = 3;
                    ch.FanSizeMm = 120;
                    ch.CurvePoints = FanChannelModel.GetDefaultCurve(ChassisZone.Side);
                }
                else if (ch.Id.EndsWith("/ch/4", StringComparison.OrdinalIgnoreCase))
                {
                    ch.Zone = ChassisZone.Bottom;
                    ch.Nickname = "Hyte Y70 Cold-Floor Intake";
                    ch.PhysicalFanCount = 3;
                    ch.FanSizeMm = 120;
                    ch.CurvePoints = FanChannelModel.GetDefaultCurve(ChassisZone.Bottom);
                }
                else if (ch.Id.EndsWith("/ch/5", StringComparison.OrdinalIgnoreCase))
                {
                    ch.Zone = ChassisZone.Rear;
                    ch.Nickname = "Hyte Y70 Rear Exhaust";
                    ch.PhysicalFanCount = 1;
                    ch.FanSizeMm = 140;
                    ch.CurvePoints = FanChannelModel.GetDefaultCurve(ChassisZone.Rear);
                }
                else if (ch.ControllerType == "GPU Controller")
                {
                    ch.Zone = ChassisZone.Gpu;
                    ch.Nickname = ch.HeaderName.Contains("1") ? "RTX 5080 Shroud Left/Center" : "RTX 5080 Shroud Right";
                    ch.PhysicalFanCount = 1;
                    ch.FanSizeMm = 110;
                    ch.ForcePinVisible = true;
                    ch.TempSourceId = "GPU_CORE";
                    ch.CurvePoints = FanChannelModel.GetDefaultCurve(ChassisZone.Gpu);
                }
            }
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
                        foreach (var hw in _computer.Hardware)
                        {
                            if (hw.HardwareType == HardwareType.Cpu)
                            {
                                foreach (var s in hw.Sensors)
                                {
                                    if (s.SensorType == SensorType.Temperature && s.Value.HasValue && s.Value.Value > 5)
                                        CpuTempC = s.Value.Value;
                                }
                            }
                            else if (hw.HardwareType == HardwareType.GpuNvidia)
                            {
                                foreach (var s in hw.Sensors)
                                {
                                    if (s.SensorType == SensorType.Temperature && s.Value.HasValue)
                                    {
                                        if (s.Name.IndexOf("Core", StringComparison.OrdinalIgnoreCase) >= 0)
                                            GpuCoreTempC = s.Value.Value;
                                        else if (s.Name.IndexOf("Memory", StringComparison.OrdinalIgnoreCase) >= 0)
                                            GpuMemTempC = s.Value.Value;
                                    }
                                }
                            }
                            else if (hw.HardwareType == HardwareType.GpuAmd && CpuTempC <= 5)
                            {
                                foreach (var s in hw.Sensors)
                                {
                                    if (s.SensorType == SensorType.Temperature && s.Value.HasValue && s.Value.Value > 10)
                                        CpuTempC = Math.Max(CpuTempC, s.Value.Value + 4.0);
                                }
                            }
                        }
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
                        ch.CurrentRpm = (int)Math.Round(lhmFan.Value.Value);
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
