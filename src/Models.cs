using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HyteFlow
{
    public enum ChassisZone
    {
        Top = 0,
        Side = 1,
        Bottom = 2,
        Rear = 3,
        CpuAio = 4,
        Gpu = 5,
        Unassigned = 6
    }

    public enum FanControlMode
    {
        SmartCurve = 0,
        ManualFixed = 1,
        Silent = 2,
        Balanced = 3,
        Performance = 4,
        FullBlast = 5
    }

    public class CurvePoint
    {
        public double TempC { get; set; }
        public double DutyPercent { get; set; }

        public CurvePoint() { }
        public CurvePoint(double tempC, double dutyPercent)
        {
            TempC = tempC;
            DutyPercent = dutyPercent;
        }
    }

    public class SavedChannelConfig
    {
        public string Id { get; set; }
        public string Nickname { get; set; }
        public ChassisZone Zone { get; set; } = ChassisZone.Unassigned;
        public int PhysicalFanCount { get; set; } = 1;
        public int FanSizeMm { get; set; } = 120;
        public FanControlMode Mode { get; set; } = FanControlMode.SmartCurve;
        public double ManualDutyPercent { get; set; } = 50;
        public string TempSourceId { get; set; } = "MAX_CPU_GPU";
        public bool ForcePinVisible { get; set; } = false;
        public bool IsVirtual { get; set; } = false;
        public string SyncedMasterChannelId { get; set; }
        public List<CurvePoint> CurvePoints { get; set; } = new List<CurvePoint>();
    }

    public class AppConfig
    {
        public string ChassisPreset { get; set; } = "thermalright_tl_m10_vision";
        public bool StartWithWindows { get; set; } = false;
        public bool StartMinimized { get; set; } = false;
        public bool ShowEmptyHeaders { get; set; } = false;
        public bool UseProcedural3D { get; set; } = false;
        public double HysteresisC { get; set; } = 3.0;
        public double MaxStepPerSec { get; set; } = 12.0;
        public FanControlMode GlobalPreset { get; set; } = FanControlMode.SmartCurve;
        public List<SavedChannelConfig> Channels { get; set; } = new List<SavedChannelConfig>();
    }

    public class TempSensorInfo : INotifyPropertyChanged
    {
        private double _valueC;
        public string Id { get; set; }
        public string Name { get; set; }
        public string HardwareName { get; set; }

        public double ValueC
        {
            get => _valueC;
            set { if (Math.Abs(_valueC - value) > 0.05) { _valueC = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class FanChannelModel : INotifyPropertyChanged
    {
        private string _nickname;
        private ChassisZone _zone = ChassisZone.Unassigned;
        private int _physicalFanCount = 1;
        private int _fanSizeMm = 120;
        private int _currentRpm;
        private int _maxObservedRpm = 2200;
        private double _currentDutyPercent = 50;
        private double _targetDutyPercent = 50;
        private double _manualDutyPercent = 50;
        private FanControlMode _mode = FanControlMode.SmartCurve;
        private bool _isPulsing;
        private int _pulseSecondsRemaining;
        private int _pulseBaselineRpm;
        private string _tempSourceId = "MAX_CPU_GPU";
        private bool _forcePinVisible;
        private bool _isVirtual;

        public string Id { get; set; }
        public string HeaderName { get; set; }
        public string HardwareOrigin { get; set; }
        public string ControllerType { get; set; }
        public bool SupportsControl { get; set; } = true;

        public bool IsVirtual
        {
            get => _isVirtual;
            set { _isVirtual = value; OnPropertyChanged(); }
        }

        public string Nickname
        {
            get => string.IsNullOrWhiteSpace(_nickname) ? HeaderName : _nickname;
            set { _nickname = value; OnPropertyChanged(); }
        }

        public ChassisZone Zone
        {
            get => _zone;
            set { _zone = value; OnPropertyChanged(); OnPropertyChanged(nameof(ZoneLabel)); OnPropertyChanged(nameof(AirflowRole)); }
        }

        public int PhysicalFanCount
        {
            get => _physicalFanCount;
            set { _physicalFanCount = Math.Max(1, Math.Min(6, value)); OnPropertyChanged(); OnPropertyChanged(nameof(TopologyLabel)); }
        }

        public int FanSizeMm
        {
            get => _fanSizeMm;
            set { _fanSizeMm = value; OnPropertyChanged(); OnPropertyChanged(nameof(TopologyLabel)); }
        }

        public int CurrentRpm
        {
            get => _currentRpm;
            set
            {
                _currentRpm = Math.Max(0, value);
                if (_currentRpm > _maxObservedRpm) _maxObservedRpm = _currentRpm;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSpinning));
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public int MaxObservedRpm
        {
            get => _maxObservedRpm;
            set { _maxObservedRpm = value; OnPropertyChanged(); }
        }

        public double CurrentDutyPercent
        {
            get => _currentDutyPercent;
            set { _currentDutyPercent = Math.Max(0, Math.Min(100, value)); OnPropertyChanged(); }
        }

        public double TargetDutyPercent
        {
            get => _targetDutyPercent;
            set { _targetDutyPercent = Math.Max(0, Math.Min(100, value)); OnPropertyChanged(); }
        }

        public double ManualDutyPercent
        {
            get => _manualDutyPercent;
            set { _manualDutyPercent = Math.Max(0, Math.Min(100, value)); OnPropertyChanged(); }
        }

        public FanControlMode Mode
        {
            get => _mode;
            set { _mode = value; OnPropertyChanged(); OnPropertyChanged(nameof(ModeLabel)); }
        }

        public bool IsPulsing
        {
            get => _isPulsing;
            set { _isPulsing = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); }
        }

        public int PulseSecondsRemaining
        {
            get => _pulseSecondsRemaining;
            set { _pulseSecondsRemaining = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); }
        }

        public int PulseBaselineRpm
        {
            get => _pulseBaselineRpm;
            set { _pulseBaselineRpm = value; OnPropertyChanged(); }
        }

        public string TempSourceId
        {
            get => _tempSourceId;
            set { _tempSourceId = value; OnPropertyChanged(); }
        }

        public bool ForcePinVisible
        {
            get => _forcePinVisible;
            set { _forcePinVisible = value; OnPropertyChanged(); }
        }

        public List<CurvePoint> CurvePoints { get; set; } = new List<CurvePoint>();

        public bool IsSpinning => _currentRpm > 50 || _isVirtual;

        public string TopologyLabel => $"{PhysicalFanCount}× {FanSizeMm}mm" + (PhysicalFanCount > 1 ? " Daisy-Chain" : " Fan");

        public string ZoneLabel
        {
            get
            {
                switch (Zone)
                {
                    case ChassisZone.Top: return "TOP RADIATOR / EXHAUST";
                    case ChassisZone.Side: return "SIDE M/B TRAY INTAKE";
                    case ChassisZone.Bottom: return "BOTTOM SHROUD INTAKE";
                    case ChassisZone.Rear: return "REAR EXHAUST";
                    case ChassisZone.CpuAio: return "CPU / AIO COOLER";
                    case ChassisZone.Gpu: return "GPU COOLING SHROUD";
                    default: return "UNASSIGNED HEADER";
                }
            }
        }

        public string AirflowRole
        {
            get
            {
                switch (Zone)
                {
                    case ChassisZone.Top:
                    case ChassisZone.Rear:
                        return "EXHAUST";
                    case ChassisZone.Side:
                    case ChassisZone.Bottom:
                        return "INTAKE";
                    case ChassisZone.CpuAio:
                        return "LIQUID / CPU";
                    case ChassisZone.Gpu:
                        return "GPU THERMAL";
                    default:
                        return "UNMAPPED";
                }
            }
        }

        public string ModeLabel
        {
            get
            {
                switch (Mode)
                {
                    case FanControlMode.SmartCurve: return "Smart Curve";
                    case FanControlMode.ManualFixed: return $"Manual ({ManualDutyPercent:0}%)";
                    case FanControlMode.Silent: return "Silent Profile";
                    case FanControlMode.Balanced: return "Balanced Profile";
                    case FanControlMode.Performance: return "Performance";
                    case FanControlMode.FullBlast: return "100% Max Blast";
                    default: return "Auto";
                }
            }
        }

        public string StatusText
        {
            get
            {
                if (IsPulsing) return $"Locating ({PulseSecondsRemaining}s)";
                if (CurrentRpm > 0) return $"{CurrentRpm:N0} RPM";
                if (ControllerType == "GPU Controller") return "Zero-RPM Idle";
                return "0 RPM (Idle)";
            }
        }

        public static List<CurvePoint> GetDefaultCurve(ChassisZone zone)
        {
            switch (zone)
            {
                case ChassisZone.Bottom:
                case ChassisZone.Side:
                    return new List<CurvePoint>
                    {
                        new CurvePoint(30, 32),
                        new CurvePoint(45, 45),
                        new CurvePoint(60, 62),
                        new CurvePoint(72, 80),
                        new CurvePoint(82, 100)
                    };
                case ChassisZone.Top:
                case ChassisZone.CpuAio:
                    return new List<CurvePoint>
                    {
                        new CurvePoint(30, 35),
                        new CurvePoint(48, 50),
                        new CurvePoint(62, 68),
                        new CurvePoint(75, 85),
                        new CurvePoint(85, 100)
                    };
                default:
                    return new List<CurvePoint>
                    {
                        new CurvePoint(30, 30),
                        new CurvePoint(45, 42),
                        new CurvePoint(60, 60),
                        new CurvePoint(75, 82),
                        new CurvePoint(85, 100)
                    };
            }
        }

        public double EvaluateCurve(double tempC)
        {
            var pts = CurvePoints;
            if (pts == null || pts.Count == 0)
                pts = GetDefaultCurve(Zone);

            if (tempC <= pts[0].TempC) return pts[0].DutyPercent;
            if (tempC >= pts[pts.Count - 1].TempC) return pts[pts.Count - 1].DutyPercent;

            for (int i = 0; i < pts.Count - 1; i++)
            {
                var p1 = pts[i];
                var p2 = pts[i + 1];
                if (tempC >= p1.TempC && tempC <= p2.TempC)
                {
                    double span = p2.TempC - p1.TempC;
                    if (span <= 0.01) return p2.DutyPercent;
                    double t = (tempC - p1.TempC) / span;
                    return p1.DutyPercent + t * (p2.DutyPercent - p1.DutyPercent);
                }
            }
            return 50;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyChanged(string prop = null) => OnPropertyChanged(prop);
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
