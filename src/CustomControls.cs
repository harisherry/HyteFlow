using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HyteFlow
{
    public static class StudioTheme
    {
        public static readonly Color BgCanvasColor = Color.FromRgb(10, 12, 15);
        public static readonly Color BgPanelColor = Color.FromRgb(16, 19, 24);
        public static readonly Color BgCardColor = Color.FromRgb(22, 26, 33);
        public static readonly Color BgCardHoverColor = Color.FromRgb(30, 36, 46);
        public static readonly Color BorderColor = Color.FromRgb(38, 46, 58);
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
            Width = 48;
            Height = 48;
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext dc)
        {
            DrawTurbine(dc, ActualWidth / 2.0, ActualHeight / 2.0, Math.Min(ActualWidth, ActualHeight) / 2.0 - 3.0, Angle, Rpm, DutyPercent, IsPulsing, false);
        }

        public static void DrawTurbine(
            DrawingContext dc,
            double cx,
            double cy,
            double r,
            double angleDeg,
            int rpm,
            double dutyPercent,
            bool isPulsing,
            bool isIntakeCyan)
        {
            if (r <= 4) return;

            bool spinning = rpm > 0 || isPulsing;
            Color primaryGlow = isPulsing ? Color.FromRgb(0, 235, 255) :
                                isIntakeCyan ? Color.FromRgb(0, 210, 255) :
                                Color.FromRgb(255, 107, 43);

            if (spinning)
            {
                var glowBrush = new RadialGradientBrush(
                    Color.FromArgb(isPulsing ? (byte)95 : (byte)50, primaryGlow.R, primaryGlow.G, primaryGlow.B),
                    Color.FromArgb(0, primaryGlow.R, primaryGlow.G, primaryGlow.B));
                dc.DrawEllipse(glowBrush, null, new Point(cx, cy), r * 1.22, r * 1.22);
            }

            dc.DrawEllipse(
                new SolidColorBrush(Color.FromArgb(220, 12, 15, 20)),
                new Pen(new SolidColorBrush(Color.FromRgb(42, 50, 64)), 2.2),
                new Point(cx, cy), r, r);

            if (spinning && dutyPercent > 1)
            {
                double sweep = Math.Min(359.0, Math.Max(45.0, (dutyPercent / 100.0) * 359.0));
                var arcGeom = CreateArcGeometry(cx, cy, r - 1.2, -90, -90 + sweep);
                dc.DrawGeometry(null, new Pen(new SolidColorBrush(primaryGlow), 2.4), arcGeom);
            }

            dc.PushTransform(new RotateTransform(angleDeg, cx, cy));
            var bladeBrush = spinning
                ? new LinearGradientBrush(
                    Color.FromArgb(220, 200, 212, 226),
                    Color.FromArgb(145, primaryGlow.R, primaryGlow.G, primaryGlow.B),
                    new Point(0, 0), new Point(1, 1))
                : (Brush)new SolidColorBrush(Color.FromArgb(70, 100, 116, 139));

            int blades = 9;
            double bladeLen = r * 0.78;
            for (int i = 0; i < blades; i++)
            {
                dc.PushTransform(new RotateTransform(i * (360.0 / blades), cx, cy));
                var sg = new StreamGeometry();
                using (var ctx = sg.Open())
                {
                    ctx.BeginFigure(new Point(cx, cy - r * 0.22), true, true);
                    ctx.QuadraticBezierTo(new Point(cx + r * 0.28, cy - bladeLen * 0.62), new Point(cx + r * 0.08, cy - bladeLen), true, false);
                    ctx.QuadraticBezierTo(new Point(cx - r * 0.16, cy - bladeLen * 0.82), new Point(cx - r * 0.09, cy - r * 0.22), true, false);
                }
                sg.Freeze();
                dc.DrawGeometry(bladeBrush, null, sg);
                dc.Pop();
            }
            dc.Pop();

            double hubR = r * 0.30;
            var hubGrad = new RadialGradientBrush(Color.FromRgb(75, 85, 102), Color.FromRgb(24, 29, 38));
            dc.DrawEllipse(hubGrad, new Pen(new SolidColorBrush(primaryGlow), 1.3), new Point(cx, cy), hubR, hubR);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(12, 15, 20)), null, new Point(cx, cy), hubR * 0.35, hubR * 0.35);
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
        private readonly Dictionary<ChassisZone, Rect> _zoneHitRects = new Dictionary<ChassisZone, Rect>();
        private ChassisZone? _hoveredZone;
        private BitmapSource _caseBitmap;
        private CaseModelDefinition _currentCaseModel;

        public ChassisZone? SelectedZoneFilter { get; set; }
        public double RotorAngle { get; set; }
        public bool WireframeOrbitMode { get; set; } = false;

        public event Action<ChassisZone> ZoneClicked;

        public CaseModelDefinition CurrentCaseModel
        {
            get => _currentCaseModel;
            set
            {
                _currentCaseModel = value ?? CaseCatalog.Presets[0];
                _caseBitmap = CaseCatalog.LoadCaseImage(_currentCaseModel);
                InvalidateVisual();
            }
        }

        public HyteY70BlueprintControl(HardwareEngine engine)
        {
            _engine = engine;
            CurrentCaseModel = CaseCatalog.Presets.FirstOrDefault(p => p.Id == (_engine.Config.ChassisPreset ?? "hyte-y70"))
                               ?? CaseCatalog.Presets[0];

            Cursor = Cursors.Hand;
            MouseMove += OnMouseMove;
            MouseLeave += (s, e) => { _hoveredZone = null; InvalidateVisual(); };
            MouseLeftButtonDown += OnMouseLeftButtonDown;
        }

        public void ReloadCaseBitmap()
        {
            _caseBitmap = CaseCatalog.LoadCaseImage(_currentCaseModel);
            InvalidateVisual();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point pt = e.GetPosition(this);
            ChassisZone? hit = null;
            foreach (var kv in _zoneHitRects)
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
            foreach (var kv in _zoneHitRects)
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

            _zoneHitRects.Clear();

            dc.DrawRoundedRectangle(
                new SolidColorBrush(Color.FromRgb(11, 13, 18)),
                new Pen(StudioTheme.BorderBrush, 1.0),
                new Rect(0, 0, w, h), 8, 8);

            double stagePadTop = 24;
            double stageW = w - 12;
            double stageH = h - stagePadTop - 6;
            Rect stageRect = new Rect(6, stagePadTop, stageW, stageH);

            if (_caseBitmap != null && !WireframeOrbitMode)
            {
                dc.PushClip(new RectangleGeometry(new Rect(1, 1, w - 2, h - 2), 7, 7));

                double imgAspect = _caseBitmap.PixelWidth / (double)Math.Max(1, _caseBitmap.PixelHeight);
                double boxAspect = stageW / Math.Max(1, stageH);
                Rect imgRect;
                if (boxAspect > imgAspect)
                {
                    double drawH = stageH * 1.04;
                    double drawW = drawH * imgAspect;
                    imgRect = new Rect(stageRect.Left + (stageW - drawW) / 2.0, stageRect.Top - stageH * 0.02, drawW, drawH);
                }
                else
                {
                    double drawW = stageW * 1.04;
                    double drawH = drawW / imgAspect;
                    imgRect = new Rect(stageRect.Left - stageW * 0.02, stageRect.Top + (stageH - drawH) / 2.0, drawW, drawH);
                }

                dc.DrawImage(_caseBitmap, imgRect);
                dc.Pop();
            }
            else
            {
                DrawProcedural3DCaseHull(dc, stageRect);
            }

            DrawText(dc, $"3D STUDIO: {_currentCaseModel.DisplayName.Split('(')[0].Trim().ToUpperInvariant()}  •  CLICK ANY 3D ZONE TO ASSIGN OR FILTER",
                12, 5, 9.2, StudioTheme.FontMono, StudioTheme.TextMuted);

            GetZoneStats(ChassisZone.Top, out int topFans, out int topRpm, out double topDuty, out bool topPulse);
            GetZoneStats(ChassisZone.Side, out int sideFans, out int sideRpm, out double sideDuty, out bool sidePulse);
            GetZoneStats(ChassisZone.Bottom, out int botFans, out int botRpm, out double botDuty, out bool botPulse);
            GetZoneStats(ChassisZone.Rear, out int rearFans, out int rearRpm, out double rearDuty, out bool rearPulse);
            GetZoneStats(ChassisZone.CpuAio, out int cpuFans, out int cpuRpm, out double cpuDuty, out bool cpuPulse);
            GetZoneStats(ChassisZone.Gpu, out int gpuFans, out int gpuRpm, out double gpuDuty, out bool gpuPulse);

            double rBase = Math.Min(w, h) * 0.068;

            Point sideP1 = MapPt(w, h, 0.625, 0.30);
            Point sideP2 = MapPt(w, h, 0.625, 0.44);
            Point sideP3 = MapPt(w, h, 0.625, 0.58);
            Draw3DPerspectiveTurbine(dc, sideP1, rBase * 0.88, 0.62, 0.96, -5, RotorAngle, sideRpm, sideDuty, sidePulse, true);
            Draw3DPerspectiveTurbine(dc, sideP2, rBase * 0.88, 0.62, 0.96, -5, RotorAngle + 40, sideRpm, sideDuty, sidePulse, true);
            Draw3DPerspectiveTurbine(dc, sideP3, rBase * 0.88, 0.62, 0.96, -5, RotorAngle + 80, sideRpm, sideDuty, sidePulse, true);

            Point topP1 = MapPt(w, h, 0.36, 0.175);
            Point topP2 = MapPt(w, h, 0.45, 0.182);
            Point topP3 = MapPt(w, h, 0.54, 0.189);
            Draw3DPerspectiveTurbine(dc, topP1, rBase * 0.92, 0.98, 0.42, 4, RotorAngle, topRpm, topDuty, topPulse, false);
            Draw3DPerspectiveTurbine(dc, topP2, rBase * 0.92, 0.98, 0.42, 4, RotorAngle + 30, topRpm, topDuty, topPulse, false);
            Draw3DPerspectiveTurbine(dc, topP3, rBase * 0.92, 0.98, 0.42, 4, RotorAngle + 60, topRpm, topDuty, topPulse, false);

            Point botP1 = MapPt(w, h, 0.36, 0.745);
            Point botP2 = MapPt(w, h, 0.45, 0.770);
            Point botP3 = MapPt(w, h, 0.54, 0.795);
            Draw3DPerspectiveTurbine(dc, botP1, rBase * 0.92, 0.98, 0.38, 11, RotorAngle, botRpm, botDuty, botPulse, true);
            Draw3DPerspectiveTurbine(dc, botP2, rBase * 0.92, 0.98, 0.38, 11, RotorAngle + 25, botRpm, botDuty, botPulse, true);
            Draw3DPerspectiveTurbine(dc, botP3, rBase * 0.92, 0.98, 0.38, 11, RotorAngle + 50, botRpm, botDuty, botPulse, true);

            Point rearCenter = MapPt(w, h, 0.305, 0.345);
            Draw3DPerspectiveTurbine(dc, rearCenter, rBase * 1.05, 0.58, 0.98, -6, RotorAngle, rearRpm, rearDuty, rearPulse, false);

            Point cpuCenter = MapPt(w, h, 0.455, 0.375);
            Draw3DAioPumpBlock(dc, cpuCenter, topP3, rBase * 0.85, _engine.CpuTempC, cpuRpm, cpuPulse);

            Point gpuCenter = MapPt(w, h, 0.445, 0.585);
            Draw3DVerticalGpuCard(dc, gpuCenter, rBase * 2.2, rBase * 0.95, _engine.GpuCoreTempC, gpuRpm, gpuDuty, gpuPulse);

            DrawZoneHudBadge(dc, ChassisZone.Top, new Rect(w * 0.31, h * 0.088, w * 0.36, 26), topP2, "TOP 360 RAD", topFans, topRpm, topDuty, topPulse, false);
            DrawZoneHudBadge(dc, ChassisZone.Side, new Rect(w * 0.68, h * 0.41, w * 0.30, 38), sideP2, "SIDE INTAKE", sideFans, sideRpm, sideDuty, sidePulse, true);
            DrawZoneHudBadge(dc, ChassisZone.Bottom, new Rect(w * 0.30, h * 0.86, w * 0.40, 26), botP2, "COLD-FLOOR", botFans, botRpm, botDuty, botPulse, true);
            DrawZoneHudBadge(dc, ChassisZone.Rear, new Rect(w * 0.02, h * 0.28, w * 0.25, 38), rearCenter, "REAR EXHAUST", rearFans, rearRpm, rearDuty, rearPulse, false);
            DrawZoneHudBadge(dc, ChassisZone.CpuAio, new Rect(w * 0.02, h * 0.45, w * 0.25, 38), cpuCenter, $"CPU AIO ({_engine.CpuTempC:0}°C)", cpuFans, cpuRpm, cpuDuty, cpuPulse, false);
            DrawZoneHudBadge(dc, ChassisZone.Gpu, new Rect(w * 0.02, h * 0.62, w * 0.25, 38), gpuCenter, $"RTX 5080 ({_engine.GpuCoreTempC:0}°C)", gpuFans, gpuRpm, gpuDuty, gpuPulse, true);
        }

        private static Point MapPt(double w, double h, double nx, double ny)
            => new Point(w * nx, h * ny);

        private void Draw3DPerspectiveTurbine(
            DrawingContext dc,
            Point center,
            double radius,
            double scaleX,
            double scaleY,
            double tiltDeg,
            double bladeAngle,
            int rpm,
            double duty,
            bool isPulsing,
            bool isIntakeCyan)
        {
            var tg = new TransformGroup();
            tg.Children.Add(new TranslateTransform(-center.X, -center.Y));
            tg.Children.Add(new ScaleTransform(scaleX, scaleY));
            tg.Children.Add(new RotateTransform(tiltDeg));
            tg.Children.Add(new TranslateTransform(center.X, center.Y));

            dc.PushTransform(tg);
            SpinningFanControl.DrawTurbine(dc, center.X, center.Y, radius, bladeAngle, rpm, duty, isPulsing, isIntakeCyan);
            dc.Pop();
        }

        private void Draw3DAioPumpBlock(DrawingContext dc, Point center, Point radTarget, double radius, double cpuTemp, int rpm, bool pulsing)
        {
            var tubePenOuter = new Pen(new SolidColorBrush(Color.FromRgb(28, 34, 44)), 5.0);
            var tubePenHighlight = new Pen(new SolidColorBrush(Color.FromArgb(120, 0, 210, 255)), 1.5);
            var tubeGeom = new StreamGeometry();
            using (var ctx = tubeGeom.Open())
            {
                ctx.BeginFigure(new Point(center.X + radius * 0.6, center.Y - radius * 0.2), false, false);
                ctx.BezierTo(
                    new Point(center.X + radius * 2.2, center.Y - radius * 0.8),
                    new Point(radTarget.X + radius * 1.2, radTarget.Y + radius * 1.4),
                    new Point(radTarget.X, radTarget.Y + 4),
                    true, false);
            }
            tubeGeom.Freeze();
            dc.DrawGeometry(null, tubePenOuter, tubeGeom);
            dc.DrawGeometry(null, tubePenHighlight, tubeGeom);

            Color ringCol = pulsing ? StudioTheme.AccentCyanColor : StudioTheme.AccentOrangeColor;
            dc.DrawEllipse(
                new SolidColorBrush(Color.FromRgb(10, 13, 18)),
                new Pen(new SolidColorBrush(ringCol), 2.5),
                center, radius, radius);
            dc.DrawEllipse(
                null,
                new Pen(new SolidColorBrush(Color.FromArgb(160, 0, 210, 255)), 1.5) { DashStyle = DashStyles.Dash },
                center, radius * 0.80, radius * 0.80);

            DrawText(dc, $"{cpuTemp:0}°C", center.X - 15, center.Y - 9, 11.0, StudioTheme.FontMono, StudioTheme.TextPrimary);
            DrawText(dc, "AIO LCD", center.X - 16, center.Y + 4, 7.5, StudioTheme.FontMono, StudioTheme.AccentCyan);
        }

        private void Draw3DVerticalGpuCard(DrawingContext dc, Point center, double halfW, double halfH, double gpuTemp, int rpm, double duty, bool pulsing)
        {
            dc.PushTransform(new SkewTransform(0, 6, center.X, center.Y));

            Rect cardRect = new Rect(center.X - halfW, center.Y - halfH, halfW * 2, halfH * 2);
            var shroudGrad = new LinearGradientBrush(
                Color.FromArgb(230, 24, 29, 38),
                Color.FromArgb(235, 13, 16, 22),
                new Point(0, 0), new Point(0, 1));
            Color borderCol = pulsing ? StudioTheme.AccentCyanColor : Color.FromRgb(64, 76, 94);

            dc.DrawRoundedRectangle(shroudGrad, new Pen(new SolidColorBrush(borderCol), 1.8), cardRect, 6, 6);

            Rect ledBar = new Rect(cardRect.Left + 10, cardRect.Top + 3, cardRect.Width - 20, 4);
            dc.DrawRoundedRectangle(StudioTheme.AccentCyan, null, ledBar, 2, 2);

            double fanR = halfH * 0.68;
            for (int i = -1; i <= 1; i++)
            {
                double fx = center.X + i * (halfW * 0.62);
                double fy = center.Y + 3;
                SpinningFanControl.DrawTurbine(dc, fx, fy, fanR, RotorAngle * (i == 0 ? -1 : 1), rpm, duty, pulsing, true);
            }

            dc.Pop();
        }

        private void DrawZoneHudBadge(
            DrawingContext dc,
            ChassisZone zone,
            Rect badgeRect,
            Point target3DPoint,
            string title,
            int fanCount,
            int avgRpm,
            double avgDuty,
            bool isPulsing,
            bool isRightOrBottom)
        {
            _zoneHitRects[zone] = badgeRect;

            bool isSelected = SelectedZoneFilter == zone;
            bool isHovered = _hoveredZone == zone;

            Color accent = isPulsing ? StudioTheme.AccentCyanColor :
                           isSelected ? StudioTheme.AccentOrangeColor :
                           isHovered ? Color.FromRgb(180, 195, 215) :
                           (avgRpm > 0 ? Color.FromRgb(0, 210, 255) : Color.FromRgb(70, 82, 100));

            Point pillAnchor = new Point(
                badgeRect.Left + badgeRect.Width / 2.0,
                badgeRect.Top + badgeRect.Height / 2.0);
            var leaderPen = new Pen(new SolidColorBrush(Color.FromArgb(isSelected || isHovered || isPulsing ? (byte)190 : (byte)95, accent.R, accent.G, accent.B)), 1.2);
            dc.DrawLine(leaderPen, pillAnchor, target3DPoint);
            dc.DrawEllipse(new SolidColorBrush(accent), null, target3DPoint, 3.0, 3.0);

            var bgFill = new SolidColorBrush(
                isPulsing ? Color.FromArgb(225, 8, 38, 52) :
                isSelected ? Color.FromArgb(230, 42, 22, 14) :
                Color.FromArgb(215, 13, 17, 23));

            dc.DrawRoundedRectangle(bgFill, new Pen(new SolidColorBrush(accent), isSelected || isPulsing ? 1.8 : 1.1), badgeRect, 5, 5);

            string stats = fanCount > 0 ? $"{fanCount}x • {avgRpm} RPM" : "CLICK TO MAP";
            if (badgeRect.Height <= 30)
            {
                DrawText(dc, $"{title}: {stats} ({avgDuty:0}%)", badgeRect.Left + 8, badgeRect.Top + 6, 9.5, StudioTheme.FontMono, StudioTheme.TextPrimary);
            }
            else
            {
                DrawText(dc, title, badgeRect.Left + 7, badgeRect.Top + 4, 9.5, StudioTheme.FontUIBold,
                    isSelected ? StudioTheme.AccentOrange : StudioTheme.TextPrimary);
                DrawText(dc, stats, badgeRect.Left + 7, badgeRect.Top + 19, 9.2, StudioTheme.FontMono,
                    avgRpm > 0 ? StudioTheme.AccentGreen : StudioTheme.TextDim);
            }
        }

        private void DrawProcedural3DCaseHull(DrawingContext dc, Rect stage)
        {
            var wirePen = new Pen(new SolidColorBrush(Color.FromRgb(56, 72, 96)), 1.6);
            double cx = stage.Left + stage.Width * 0.48;
            double cy = stage.Top + stage.Height * 0.50;
            double w = stage.Width * 0.52;
            double h = stage.Height * 0.72;

            Point tl = new Point(cx - w * 0.48, cy - h * 0.48);
            Point tr = new Point(cx + w * 0.48, cy - h * 0.44);
            Point br = new Point(cx + w * 0.48, cy + h * 0.46);
            Point bl = new Point(cx - w * 0.48, cy + h * 0.42);
            dc.DrawLine(wirePen, tl, tr);
            dc.DrawLine(wirePen, tr, br);
            dc.DrawLine(wirePen, br, bl);
            dc.DrawLine(wirePen, bl, tl);
        }

        private void GetZoneStats(ChassisZone zone, out int totalFans, out int avgRpm, out double avgDuty, out bool hasPulsing)
        {
            var list = _engine.Channels.Where(c => c.Zone == zone).ToList();
            totalFans = list.Sum(c => c.PhysicalFanCount);
            avgRpm = list.Any(c => c.IsSpinning) ? (int)Math.Round(list.Where(c => c.IsSpinning).Average(c => c.CurrentRpm)) : 0;
            avgDuty = list.Any() ? list.Average(c => c.CurrentDutyPercent) : 0;
            hasPulsing = list.Any(c => c.IsPulsing);
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
