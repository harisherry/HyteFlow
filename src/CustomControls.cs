using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace HyteFlow
{
    public static class StudioTheme
    {
        public static readonly Color BgCanvasColor = Color.FromRgb(10, 12, 15);
        public static readonly Color BgPanelColor = Color.FromRgb(17, 20, 25);
        public static readonly Color BgCardColor = Color.FromRgb(23, 27, 34);
        public static readonly Color BgCardHoverColor = Color.FromRgb(30, 35, 44);
        public static readonly Color BorderColor = Color.FromRgb(38, 45, 56);
        public static readonly Color BorderActiveColor = Color.FromRgb(255, 107, 43);
        public static readonly Color AccentOrangeColor = Color.FromRgb(255, 107, 43);
        public static readonly Color AccentCyanColor = Color.FromRgb(0, 210, 255);
        public static readonly Color AccentGreenColor = Color.FromRgb(16, 185, 129);
        public static readonly Color AccentAmberColor = Color.FromRgb(245, 158, 11);
        public static readonly Color TextPrimaryColor = Color.FromRgb(241, 245, 249);
        public static readonly Color TextMutedColor = Color.FromRgb(148, 163, 184);
        public static readonly Color TextDimColor = Color.FromRgb(100, 116, 139);

        public static readonly SolidColorBrush BgCanvas = Freeze(new SolidColorBrush(BgCanvasColor));
        public static readonly SolidColorBrush BgPanel = Freeze(new SolidColorBrush(BgPanelColor));
        public static readonly SolidColorBrush BgCard = Freeze(new SolidColorBrush(BgCardColor));
        public static readonly SolidColorBrush BgCardHover = Freeze(new SolidColorBrush(BgCardHoverColor));
        public static readonly SolidColorBrush BorderBrush = Freeze(new SolidColorBrush(BorderColor));
        public static readonly SolidColorBrush BorderActive = Freeze(new SolidColorBrush(BorderActiveColor));
        public static readonly SolidColorBrush AccentOrange = Freeze(new SolidColorBrush(AccentOrangeColor));
        public static readonly SolidColorBrush AccentCyan = Freeze(new SolidColorBrush(AccentCyanColor));
        public static readonly SolidColorBrush AccentGreen = Freeze(new SolidColorBrush(AccentGreenColor));
        public static readonly SolidColorBrush AccentAmber = Freeze(new SolidColorBrush(AccentAmberColor));
        public static readonly SolidColorBrush TextPrimary = Freeze(new SolidColorBrush(TextPrimaryColor));
        public static readonly SolidColorBrush TextMuted = Freeze(new SolidColorBrush(TextMutedColor));
        public static readonly SolidColorBrush TextDim = Freeze(new SolidColorBrush(TextDimColor));

        public static readonly Typeface FontUI = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        public static readonly Typeface FontUIBold = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        public static readonly Typeface FontMono = new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        private static T Freeze<T>(T freezable) where T : Freezable
        {
            if (freezable.CanFreeze) freezable.Freeze();
            return freezable;
        }
    }

    public class SpinningFanControl : FrameworkElement
    {
        public double Angle { get; set; }
        public int Rpm { get; set; }
        public double DutyPercent { get; set; }
        public bool IsPulsing { get; set; }

        public SpinningFanControl()
        {
            Width = 46;
            Height = 46;
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext dc)
        {
            double cx = ActualWidth / 2.0;
            double cy = ActualHeight / 2.0;
            double r = Math.Min(cx, cy) - 3.0;
            if (r <= 4) return;

            var ringPen = new Pen(new SolidColorBrush(Color.FromRgb(40, 48, 60)), 2.5);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(14, 17, 22)), ringPen, new Point(cx, cy), r, r);

            var accentBrush = IsPulsing ? StudioTheme.AccentCyan :
                              (Rpm > 0 ? StudioTheme.AccentOrange : StudioTheme.TextDim);

            if (DutyPercent > 1)
            {
                double sweep = Math.Min(359.0, (DutyPercent / 100.0) * 359.0);
                var arcGeom = CreateArcGeometry(cx, cy, r, -90, -90 + sweep);
                dc.DrawGeometry(null, new Pen(accentBrush, 2.5), arcGeom);
            }

            dc.PushTransform(new RotateTransform(Angle, cx, cy));
            var bladeBrush = Rpm > 0
                ? (IsPulsing ? new SolidColorBrush(Color.FromArgb(210, 0, 210, 255)) : new SolidColorBrush(Color.FromArgb(190, 226, 232, 240)))
                : new SolidColorBrush(Color.FromArgb(75, 100, 116, 139));

            int blades = 7;
            double bladeLen = r * 0.74;
            for (int i = 0; i < blades; i++)
            {
                dc.PushTransform(new RotateTransform(i * (360.0 / blades), cx, cy));
                var sg = new StreamGeometry();
                using (var ctx = sg.Open())
                {
                    ctx.BeginFigure(new Point(cx, cy - 3), true, true);
                    ctx.QuadraticBezierTo(new Point(cx + 6, cy - bladeLen * 0.65), new Point(cx + 2, cy - bladeLen), true, false);
                    ctx.QuadraticBezierTo(new Point(cx - 4, cy - bladeLen * 0.8), new Point(cx - 2, cy - 3), true, false);
                }
                sg.Freeze();
                dc.DrawGeometry(bladeBrush, null, sg);
                dc.Pop();
            }
            dc.Pop();

            dc.DrawEllipse(StudioTheme.BgCard, new Pen(accentBrush, 1.5), new Point(cx, cy), r * 0.28, r * 0.28);
        }

        public static StreamGeometry CreateArcGeometry(double cx, double cy, double r, double startAngleDeg, double endAngleDeg)
        {
            double startRad = startAngleDeg * Math.PI / 180.0;
            double endRad = endAngleDeg * Math.PI / 180.0;
            Point startPt = new Point(cx + r * Math.Cos(startRad), cy + r * Math.Sin(startRad));
            Point endPt = new Point(cx + r * Math.Cos(endRad), cy + r * Math.Sin(endRad));
            bool isLarge = (endAngleDeg - startAngleDeg) > 180.0;

            var sg = new StreamGeometry();
            using (var ctx = sg.Open())
            {
                ctx.BeginFigure(startPt, false, false);
                ctx.ArcTo(endPt, new Size(r, r), 0, isLarge, SweepDirection.Clockwise, true, false);
            }
            sg.Freeze();
            return sg;
        }
    }

    public class HyteY70BlueprintControl : FrameworkElement
    {
        private readonly HardwareEngine _engine;
        private readonly Dictionary<ChassisZone, Rect> _zoneRects = new Dictionary<ChassisZone, Rect>();
        private ChassisZone? _hoveredZone;

        public ChassisZone? SelectedZoneFilter { get; set; }
        public double RotorAngle { get; set; }

        public event Action<ChassisZone> ZoneClicked;

        public HyteY70BlueprintControl(HardwareEngine engine)
        {
            _engine = engine;
            Cursor = Cursors.Hand;
            MouseMove += OnMouseMove;
            MouseLeave += (s, e) => { _hoveredZone = null; InvalidateVisual(); };
            MouseLeftButtonDown += OnMouseLeftButtonDown;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point pt = e.GetPosition(this);
            ChassisZone? hit = null;
            foreach (var kv in _zoneRects)
            {
                if (kv.Value.Contains(pt))
                {
                    hit = kv.Key;
                    break;
                }
            }
            if (_hoveredZone != hit)
            {
                _hoveredZone = hit;
                InvalidateVisual();
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point pt = e.GetPosition(this);
            foreach (var kv in _zoneRects)
            {
                if (kv.Value.Contains(pt))
                {
                    ZoneClicked?.Invoke(kv.Key);
                    break;
                }
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            double w = ActualWidth;
            double h = ActualHeight;
            if (w < 100 || h < 100) return;

            _zoneRects.Clear();

            dc.DrawRoundedRectangle(
                new SolidColorBrush(Color.FromRgb(12, 15, 20)),
                new Pen(StudioTheme.BorderBrush, 1.0),
                new Rect(0, 0, w, h), 8, 8);

            var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(18, 148, 163, 184)), 1.0);
            for (double gx = 24; gx < w; gx += 24)
                dc.DrawLine(gridPen, new Point(gx, 0), new Point(gx, h));
            for (double gy = 24; gy < h; gy += 24)
                dc.DrawLine(gridPen, new Point(0, gy), new Point(w, gy));

            double padX = 16;
            double padY = 28;
            double caseW = w - padX * 2;
            double caseH = h - padY - 12;

            Rect caseRect = new Rect(padX, padY, caseW, caseH);
            dc.DrawRoundedRectangle(
                new SolidColorBrush(Color.FromArgb(180, 16, 20, 26)),
                new Pen(new SolidColorBrush(Color.FromRgb(56, 66, 82)), 2.0),
                caseRect, 6, 6);

            DrawText(dc, "HYTE Y70 DUAL-CHAMBER TOPOLOGY  •  CLICK ANY ZONE TO FILTER / ASSIGN",
                padX + 2, 8, 10.0, StudioTheme.FontMono, StudioTheme.TextMuted);

            Rect topRect = new Rect(padX + caseW * 0.20, padY + 8, caseW * 0.55, caseH * 0.22);
            Rect sideRect = new Rect(padX + caseW * 0.77, padY + 10, caseW * 0.21, caseH * 0.84);
            Rect botRect = new Rect(padX + caseW * 0.20, padY + caseH * 0.75, caseW * 0.55, caseH * 0.22);
            Rect rearRect = new Rect(padX + 8, padY + caseH * 0.20, caseW * 0.17, caseH * 0.36);
            Rect cpuRect = new Rect(padX + caseW * 0.27, padY + caseH * 0.32, caseW * 0.42, caseH * 0.20);
            Rect gpuRect = new Rect(padX + caseW * 0.22, padY + caseH * 0.54, caseW * 0.51, caseH * 0.19);

            _zoneRects[ChassisZone.Top] = topRect;
            _zoneRects[ChassisZone.Side] = sideRect;
            _zoneRects[ChassisZone.Bottom] = botRect;
            _zoneRects[ChassisZone.Rear] = rearRect;
            _zoneRects[ChassisZone.CpuAio] = cpuRect;
            _zoneRects[ChassisZone.Gpu] = gpuRect;

            DrawZoneBox(dc, ChassisZone.Top, topRect, "TOP EXHAUST / 360 RAD", 3, true);
            DrawZoneBox(dc, ChassisZone.Side, sideRect, "SIDE M/B INTAKE", 3, false);
            DrawZoneBox(dc, ChassisZone.Bottom, botRect, "BOTTOM COLD-FLOOR", 3, true);
            DrawZoneBox(dc, ChassisZone.Rear, rearRect, "REAR EXHAUST", 1, false);
            DrawZoneBox(dc, ChassisZone.CpuAio, cpuRect, $"CPU / AIO  ({_engine.CpuTempC:0}°C)", 2, true);
            DrawZoneBox(dc, ChassisZone.Gpu, gpuRect, $"RTX 5080 VERTICAL  ({_engine.GpuCoreTempC:0}°C)", 2, true);
        }

        private void DrawZoneBox(DrawingContext dc, ChassisZone zone, Rect rect, string title, int slotCount, bool horizontalSlots)
        {
            var zoneChannels = _engine.Channels.Where(c => c.Zone == zone).ToList();
            bool hasPulsing = zoneChannels.Any(c => c.IsPulsing);
            bool isSelected = SelectedZoneFilter == zone;
            bool isHovered = _hoveredZone == zone;

            int totalFans = zoneChannels.Sum(c => c.PhysicalFanCount);
            int avgRpm = zoneChannels.Any(c => c.IsSpinning)
                ? (int)Math.Round(zoneChannels.Where(c => c.IsSpinning).Average(c => c.CurrentRpm))
                : 0;
            double avgDuty = zoneChannels.Any()
                ? zoneChannels.Average(c => c.CurrentDutyPercent)
                : 0;

            Color borderCol = hasPulsing ? StudioTheme.AccentCyanColor :
                              isSelected ? StudioTheme.AccentOrangeColor :
                              isHovered ? Color.FromRgb(148, 163, 184) :
                              (avgRpm > 0 ? Color.FromRgb(45, 72, 95) : StudioTheme.BorderColor);

            Color fillCol = hasPulsing ? Color.FromArgb(45, 0, 210, 255) :
                            isSelected ? Color.FromArgb(35, 255, 107, 43) :
                            isHovered ? Color.FromArgb(35, 51, 65, 85) :
                            Color.FromArgb(190, 20, 25, 33);

            dc.DrawRoundedRectangle(
                new SolidColorBrush(fillCol),
                new Pen(new SolidColorBrush(borderCol), isSelected || hasPulsing ? 2.0 : 1.2),
                rect, 6, 6);

            double rotorRadius = Math.Min(rect.Width, rect.Height) * 0.22;
            rotorRadius = Math.Max(9, Math.Min(16, rotorRadius));

            for (int i = 0; i < slotCount; i++)
            {
                double fx, fy;
                if (horizontalSlots)
                {
                    double step = rect.Width / (slotCount + 1);
                    fx = rect.Left + step * (i + 1);
                    fy = rect.Top + rect.Height * 0.50;
                }
                else
                {
                    double step = rect.Height / (slotCount + 1);
                    fx = rect.Left + rect.Width * 0.35;
                    fy = rect.Top + step * (i + 1);
                }
                DrawMiniRotor(dc, fx, fy, rotorRadius, avgRpm > 0, hasPulsing);
            }

            var titleBrush = hasPulsing ? StudioTheme.AccentCyan :
                             isSelected ? StudioTheme.AccentOrange : StudioTheme.TextPrimary;
            DrawText(dc, title, rect.Left + 8, rect.Top + 5, 10.0, StudioTheme.FontUIBold, titleBrush);

            string bottomStat = zoneChannels.Count == 0
                ? "UNMAPPED"
                : (rect.Width < 130
                    ? $"{totalFans}x • {avgRpm} RPM"
                    : $"{totalFans}x FANS • {avgRpm} RPM ({avgDuty:0}%)");

            var statBrush = avgRpm > 0 ? StudioTheme.AccentGreen : StudioTheme.TextDim;
            if (hasPulsing) statBrush = StudioTheme.AccentCyan;
            DrawText(dc, bottomStat, rect.Left + 8, rect.Bottom - 16, 9.5, StudioTheme.FontMono, statBrush);
        }

        private void DrawMiniRotor(DrawingContext dc, double cx, double cy, double r, bool spinning, bool pulsing)
        {
            var ringColor = pulsing ? StudioTheme.AccentCyanColor :
                            spinning ? Color.FromRgb(56, 189, 248) : Color.FromRgb(45, 55, 72);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(140, 10, 12, 16)), new Pen(new SolidColorBrush(ringColor), 1.2), new Point(cx, cy), r, r);

            double angle = spinning ? RotorAngle : 0;
            dc.PushTransform(new RotateTransform(angle, cx, cy));
            var bladePen = new Pen(new SolidColorBrush(spinning ? ringColor : Color.FromRgb(60, 70, 85)), 1.5);
            for (int b = 0; b < 4; b++)
            {
                double rad = b * Math.PI / 2.0;
                dc.DrawLine(bladePen,
                    new Point(cx - Math.Cos(rad) * r * 0.7, cy - Math.Sin(rad) * r * 0.7),
                    new Point(cx + Math.Cos(rad) * r * 0.7, cy + Math.Sin(rad) * r * 0.7));
            }
            dc.Pop();
        }

        private static void DrawText(DrawingContext dc, string text, double x, double y, double size, Typeface face, Brush brush)
        {
            var ft = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                face,
                size,
                brush,
                1.25);
            dc.DrawText(ft, new Point(x, y));
        }
    }

    public class FanCurveEditorControl : FrameworkElement
    {
        private FanChannelModel _channel;
        private readonly HardwareEngine _engine;
        private int _draggingIndex = -1;

        private const double PadLeft = 40;
        private const double PadRight = 18;
        private const double PadTop = 18;
        private const double PadBottom = 26;
        private const double MinTemp = 20.0;
        private const double MaxTemp = 95.0;

        public event Action CurveModified;

        public FanChannelModel Channel
        {
            get => _channel;
            set { _channel = value; InvalidateVisual(); }
        }

        public FanCurveEditorControl(HardwareEngine engine)
        {
            _engine = engine;
            Cursor = Cursors.Cross;
            MouseLeftButtonDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseUp;
        }

        private Rect GetPlotRect()
        {
            double w = Math.Max(60, ActualWidth - PadLeft - PadRight);
            double h = Math.Max(60, ActualHeight - PadTop - PadBottom);
            return new Rect(PadLeft, PadTop, w, h);
        }

        private Point TempDutyToScreen(Rect plot, double tempC, double duty)
        {
            double tx = (tempC - MinTemp) / (MaxTemp - MinTemp);
            double ty = 1.0 - (duty / 100.0);
            tx = Math.Max(0, Math.Min(1, tx));
            ty = Math.Max(0, Math.Min(1, ty));
            return new Point(plot.Left + tx * plot.Width, plot.Top + ty * plot.Height);
        }

        private void ScreenToTempDuty(Rect plot, Point pt, out double tempC, out double duty)
        {
            double tx = (pt.X - plot.Left) / plot.Width;
            double ty = 1.0 - ((pt.Y - plot.Top) / plot.Height);
            tempC = Math.Round(MinTemp + Math.Max(0, Math.Min(1, tx)) * (MaxTemp - MinTemp));
            duty = Math.Round(Math.Max(0, Math.Min(1, ty)) * 100.0);
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_channel == null || _channel.CurvePoints == null) return;
            Rect plot = GetPlotRect();
            Point pos = e.GetPosition(this);

            for (int i = 0; i < _channel.CurvePoints.Count; i++)
            {
                Point p = TempDutyToScreen(plot, _channel.CurvePoints[i].TempC, _channel.CurvePoints[i].DutyPercent);
                if ((p - pos).Length <= 12)
                {
                    _draggingIndex = i;
                    _channel.Mode = FanControlMode.SmartCurve;
                    CaptureMouse();
                    e.Handled = true;
                    break;
                }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_draggingIndex < 0 || _channel == null) return;
            Rect plot = GetPlotRect();
            ScreenToTempDuty(plot, e.GetPosition(this), out double t, out double d);

            var pts = _channel.CurvePoints;
            if (_draggingIndex > 0)
                t = Math.Max(pts[_draggingIndex - 1].TempC + 2, t);
            if (_draggingIndex < pts.Count - 1)
                t = Math.Min(pts[_draggingIndex + 1].TempC - 2, t);

            pts[_draggingIndex].TempC = t;
            pts[_draggingIndex].DutyPercent = d;
            InvalidateVisual();
            CurveModified?.Invoke();
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggingIndex >= 0)
            {
                _draggingIndex = -1;
                ReleaseMouseCapture();
                _engine.SaveConfig();
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            Rect plot = GetPlotRect();
            dc.DrawRoundedRectangle(
                new SolidColorBrush(Color.FromRgb(12, 15, 20)),
                new Pen(StudioTheme.BorderBrush, 1.0),
                new Rect(0, 0, ActualWidth, ActualHeight), 6, 6);

            var gridPen = new Pen(new SolidColorBrush(Color.FromRgb(26, 32, 42)), 1.0);

            for (int pct = 0; pct <= 100; pct += 25)
            {
                Point p = TempDutyToScreen(plot, MinTemp, pct);
                dc.DrawLine(gridPen, new Point(plot.Left, p.Y), new Point(plot.Right, p.Y));
                DrawText(dc, $"{pct}%", 6, p.Y - 6, 9.5, StudioTheme.FontMono, StudioTheme.TextDim);
            }

            for (int tc = 30; tc <= 90; tc += 15)
            {
                Point p = TempDutyToScreen(plot, tc, 0);
                dc.DrawLine(gridPen, new Point(p.X, plot.Top), new Point(p.X, plot.Bottom));
                DrawText(dc, $"{tc}°C", p.X - 12, plot.Bottom + 6, 9.5, StudioTheme.FontMono, StudioTheme.TextDim);
            }

            if (_channel == null) return;

            var pts = _channel.CurvePoints;
            if (pts == null || pts.Count < 2)
                pts = FanChannelModel.GetDefaultCurve(_channel.Zone);

            var fillGeom = new StreamGeometry();
            using (var ctx = fillGeom.Open())
            {
                Point first = TempDutyToScreen(plot, pts[0].TempC, pts[0].DutyPercent);
                ctx.BeginFigure(new Point(plot.Left, plot.Bottom), true, true);
                ctx.LineTo(new Point(plot.Left, first.Y), false, false);
                foreach (var cp in pts)
                    ctx.LineTo(TempDutyToScreen(plot, cp.TempC, cp.DutyPercent), true, false);
                Point last = TempDutyToScreen(plot, pts[pts.Count - 1].TempC, pts[pts.Count - 1].DutyPercent);
                ctx.LineTo(new Point(plot.Right, last.Y), false, false);
                ctx.LineTo(new Point(plot.Right, plot.Bottom), false, false);
            }
            fillGeom.Freeze();
            var areaBrush = new LinearGradientBrush(
                Color.FromArgb(65, 255, 107, 43),
                Color.FromArgb(4, 255, 107, 43),
                new Point(0, 0), new Point(0, 1));
            dc.DrawGeometry(areaBrush, null, fillGeom);

            var linePen = new Pen(StudioTheme.AccentOrange, 2.2);
            Point prev = new Point(plot.Left, TempDutyToScreen(plot, pts[0].TempC, pts[0].DutyPercent).Y);
            foreach (var cp in pts)
            {
                Point cur = TempDutyToScreen(plot, cp.TempC, cp.DutyPercent);
                dc.DrawLine(linePen, prev, cur);
                prev = cur;
            }
            dc.DrawLine(linePen, prev, new Point(plot.Right, prev.Y));

            double liveTemp = _engine.GetTempForChannel(_channel);
            double liveDuty = _channel.CurrentDutyPercent;
            Point livePt = TempDutyToScreen(plot, liveTemp, liveDuty);
            var crossPen = new Pen(new SolidColorBrush(Color.FromArgb(120, 0, 210, 255)), 1.0) { DashStyle = DashStyles.Dash };
            dc.DrawLine(crossPen, new Point(livePt.X, plot.Top), new Point(livePt.X, plot.Bottom));
            dc.DrawLine(crossPen, new Point(plot.Left, livePt.Y), new Point(plot.Right, livePt.Y));
            dc.DrawEllipse(StudioTheme.AccentCyan, new Pen(Brushes.Black, 1.5), livePt, 5.5, 5.5);

            for (int i = 0; i < pts.Count; i++)
            {
                Point np = TempDutyToScreen(plot, pts[i].TempC, pts[i].DutyPercent);
                bool active = i == _draggingIndex;
                dc.DrawEllipse(
                    active ? StudioTheme.AccentCyan : StudioTheme.BgCard,
                    new Pen(StudioTheme.AccentOrange, 2.2),
                    np, active ? 6.5 : 5.0, active ? 6.5 : 5.0);

                DrawText(dc, $"{pts[i].TempC:0}°/{pts[i].DutyPercent:0}%",
                    Math.Min(plot.Right - 44, Math.Max(plot.Left, np.X - 18)),
                    Math.Max(4, np.Y - 18),
                    9.0, StudioTheme.FontMono, StudioTheme.TextMuted);
            }
        }

        private static void DrawText(DrawingContext dc, string text, double x, double y, double size, Typeface face, Brush brush)
        {
            var ft = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                face,
                size,
                brush,
                1.25);
            dc.DrawText(ft, new Point(x, y));
        }
    }
}
