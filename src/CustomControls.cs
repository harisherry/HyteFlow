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
        // Apple macOS Dark Glass / Titanium Pro Palette
        public static readonly Color BgCanvasColor = Color.FromRgb(10, 11, 15);
        public static readonly Color BgPanelColor = Color.FromRgb(18, 20, 27);
        public static readonly Color BgCardColor = Color.FromRgb(25, 28, 37);
        public static readonly Color BgCardHoverColor = Color.FromRgb(33, 37, 49);
        public static readonly Color BgPillColor = Color.FromRgb(32, 36, 46);
        public static readonly Color BorderColor = Color.FromRgb(46, 52, 66);
        public static readonly Color BorderSubtleColor = Color.FromRgb(36, 40, 52);
        public static readonly Color BorderActiveColor = Color.FromRgb(10, 132, 255);

        // Apple System Dark Mode Accents
        public static readonly Color AccentBlueColor = Color.FromRgb(10, 132, 255);     // #0A84FF Apple System Blue
        public static readonly Color AccentCyanColor = Color.FromRgb(100, 210, 255);    // #64D2FF Apple System Teal/Cyan
        public static readonly Color AccentOrangeColor = Color.FromRgb(255, 159, 10);   // #FF9F0A Apple System Orange
        public static readonly Color AccentGreenColor = Color.FromRgb(48, 209, 88);     // #30D158 Apple System Green
        public static readonly Color AccentAmberColor = Color.FromRgb(255, 214, 10);    // #FFD60A Apple System Yellow
        public static readonly Color AccentPurpleColor = Color.FromRgb(191, 90, 242);   // #BF5AF2 Apple System Purple

        public static readonly Color TextPrimaryColor = Color.FromRgb(245, 247, 250);
        public static readonly Color TextMutedColor = Color.FromRgb(152, 162, 179);
        public static readonly Color TextDimColor = Color.FromRgb(102, 112, 133);

        public static readonly SolidColorBrush BgCanvas = Freeze(new SolidColorBrush(BgCanvasColor));
        public static readonly SolidColorBrush BgPanel = Freeze(new SolidColorBrush(BgPanelColor));
        public static readonly SolidColorBrush BgCard = Freeze(new SolidColorBrush(BgCardColor));
        public static readonly SolidColorBrush BgCardHover = Freeze(new SolidColorBrush(BgCardHoverColor));
        public static readonly SolidColorBrush BgPill = Freeze(new SolidColorBrush(BgPillColor));
        public static readonly SolidColorBrush BorderBrush = Freeze(new SolidColorBrush(BorderColor));
        public static readonly SolidColorBrush BorderSubtle = Freeze(new SolidColorBrush(BorderSubtleColor));
        public static readonly SolidColorBrush BorderActive = Freeze(new SolidColorBrush(BorderActiveColor));

        public static readonly SolidColorBrush AccentBlue = Freeze(new SolidColorBrush(AccentBlueColor));
        public static readonly SolidColorBrush AccentCyan = Freeze(new SolidColorBrush(AccentCyanColor));
        public static readonly SolidColorBrush AccentOrange = Freeze(new SolidColorBrush(AccentOrangeColor));
        public static readonly SolidColorBrush AccentGreen = Freeze(new SolidColorBrush(AccentGreenColor));
        public static readonly SolidColorBrush AccentAmber = Freeze(new SolidColorBrush(AccentAmberColor));
        public static readonly SolidColorBrush AccentPurple = Freeze(new SolidColorBrush(AccentPurpleColor));

        public static readonly SolidColorBrush TextPrimary = Freeze(new SolidColorBrush(TextPrimaryColor));
        public static readonly SolidColorBrush TextMuted = Freeze(new SolidColorBrush(TextMutedColor));
        public static readonly SolidColorBrush TextDim = Freeze(new SolidColorBrush(TextDimColor));

        public static readonly Typeface FontUI = new Typeface(new FontFamily("Segoe UI Variable Display, Segoe UI"), FontStyles.Normal, FontWeights.Medium, FontStretches.Normal);
        public static readonly Typeface FontUIBold = new Typeface(new FontFamily("Segoe UI Variable Display, Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        public static readonly Typeface FontMono = new Typeface(new FontFamily("Cascadia Mono, Consolas"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

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
        public bool IsIntake { get; set; } = true;

        public SpinningFanControl()
        {
            Width = 50;
            Height = 50;
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext dc)
        {
            DrawTurbine(dc, ActualWidth / 2.0, ActualHeight / 2.0, Math.Min(ActualWidth, ActualHeight) / 2.0 - 3.0, Angle, Rpm, DutyPercent, IsPulsing, IsIntake);
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
            Color primaryGlow = isPulsing ? Color.FromRgb(100, 235, 255) :
                                isIntakeCyan ? StudioTheme.AccentCyanColor :
                                StudioTheme.AccentBlueColor;

            if (spinning)
            {
                var glowBrush = new RadialGradientBrush(
                    Color.FromArgb(isPulsing ? (byte)110 : (byte)58, primaryGlow.R, primaryGlow.G, primaryGlow.B),
                    Color.FromArgb(0, primaryGlow.R, primaryGlow.G, primaryGlow.B));
                dc.DrawEllipse(glowBrush, null, new Point(cx, cy), r * 1.28, r * 1.28);
            }

            // Outer anodized turbine housing ring
            dc.DrawEllipse(
                new SolidColorBrush(Color.FromArgb(230, 14, 16, 22)),
                new Pen(new SolidColorBrush(Color.FromArgb(190, 58, 66, 84)), 2.0),
                new Point(cx, cy), r, r);

            // Inner ARGB infinity halo ring
            if (spinning && dutyPercent > 1)
            {
                double sweep = Math.Min(359.5, Math.Max(60.0, (dutyPercent / 100.0) * 359.5));
                var arcGeom = CreateArcGeometry(cx, cy, r - 1.2, -90, -90 + sweep);
                dc.DrawGeometry(null, new Pen(new SolidColorBrush(primaryGlow), 2.4), arcGeom);
            }

            dc.PushTransform(new RotateTransform(angleDeg, cx, cy));
            var bladeBrush = spinning
                ? new LinearGradientBrush(
                    Color.FromArgb(235, 226, 234, 246),
                    Color.FromArgb(165, primaryGlow.R, primaryGlow.G, primaryGlow.B),
                    new Point(0, 0), new Point(1, 1))
                : (Brush)new SolidColorBrush(Color.FromArgb(75, 110, 122, 142));

            int blades = 9;
            double bladeLen = r * 0.79;
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

            // Brushed aluminum center hub
            double hubR = r * 0.30;
            var hubGrad = new RadialGradientBrush(Color.FromRgb(88, 98, 116), Color.FromRgb(22, 26, 34));
            dc.DrawEllipse(hubGrad, new Pen(new SolidColorBrush(primaryGlow), 1.4), new Point(cx, cy), hubR, hubR);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(14, 16, 22)), null, new Point(cx, cy), hubR * 0.34, hubR * 0.34);
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
            CurrentCaseModel = CaseCatalog.FindOrCreatePreset(_engine.Config.ChassisPreset);
            WireframeOrbitMode = _engine.Config.UseProcedural3D;

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

            // Apple Pro Dark Glass Stage Container
            var stageBg = new LinearGradientBrush(
                Color.FromRgb(11, 13, 18),
                Color.FromRgb(15, 18, 25),
                new Point(0, 0), new Point(0, 1));
            dc.DrawRoundedRectangle(
                stageBg,
                new Pen(StudioTheme.BorderSubtle, 1.0),
                new Rect(0, 0, w, h), 12, 12);

            double stagePadTop = 26;
            double stageW = w - 12;
            double stageH = h - stagePadTop - 6;
            Rect stageRect = new Rect(6, stagePadTop, stageW, stageH);

            bool usingStudioPhoto = _caseBitmap != null && !WireframeOrbitMode;
            if (usingStudioPhoto)
            {
                dc.PushClip(new RectangleGeometry(new Rect(1, 1, w - 2, h - 2), 11, 11));

                double imgAspect = _caseBitmap.PixelWidth / (double)Math.Max(1, _caseBitmap.PixelHeight);
                double boxAspect = stageW / Math.Max(1, stageH);
                Rect imgRect;
                if (boxAspect > imgAspect)
                {
                    double drawH = stageH * 1.04;
                    double drawW = drawH * imgAspect;
                    imgRect = new Rect(stageRect.Left + (stageW - drawW) / 2.0, stageRect.Top - stageH * 0.01, drawW, drawH);
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
                DrawAppleProcedural3DChassis(dc, stageRect, _currentCaseModel);
            }

            string viewModeTag = usingStudioPhoto ? "3D STUDIO RENDER" : "3D ARCHITECTURAL CAD";
            DrawText(dc, $"{_currentCaseModel.DisplayName.ToUpperInvariant()}  •  {viewModeTag}",
                14, 7, 9.5, StudioTheme.FontMono, StudioTheme.TextMuted);

            GetZoneStats(ChassisZone.Top, out int topFans, out int topRpm, out double topDuty, out bool topPulse);
            GetZoneStats(ChassisZone.Side, out int sideFans, out int sideRpm, out double sideDuty, out bool sidePulse);
            GetZoneStats(ChassisZone.Bottom, out int botFans, out int botRpm, out double botDuty, out bool botPulse);
            GetZoneStats(ChassisZone.Rear, out int rearFans, out int rearRpm, out double rearDuty, out bool rearPulse);
            GetZoneStats(ChassisZone.CpuAio, out int cpuFans, out int cpuRpm, out double cpuDuty, out bool cpuPulse);
            GetZoneStats(ChassisZone.Gpu, out int gpuFans, out int gpuRpm, out double gpuDuty, out bool gpuPulse);

            double rBase = Math.Min(w, h) * 0.066;

            // Dynamically render exact number of physical fans installed in each zone
            // 1. SIDE INTAKE BANK (1x..4x vertical stack)
            Point sideCenter = RenderDynamicFanBank(
                dc, w, h,
                startNorm: new Point(0.625, 0.30),
                endNorm: new Point(0.625, 0.58),
                defaultCenterNorm: new Point(0.625, 0.44),
                installedCount: sideFans,
                defaultGhostCount: 3,
                radius: rBase * 0.88,
                scaleX: 0.62, scaleY: 0.96, tiltDeg: -5,
                rpm: sideRpm, duty: sideDuty, isPulsing: sidePulse, isIntakeCyan: true);

            // 2. TOP RADIATOR / EXHAUST BANK (1x..4x along top ceiling)
            int effectiveTopFans = topFans > 0 ? topFans : (cpuFans > 1 ? cpuFans : 0);
            int effectiveTopRpm = topRpm > 0 ? topRpm : cpuRpm;
            double effectiveTopDuty = topFans > 0 ? topDuty : cpuDuty;
            bool effectiveTopPulse = topPulse || cpuPulse;

            Point topCenter = RenderDynamicFanBank(
                dc, w, h,
                startNorm: new Point(0.355, 0.172),
                endNorm: new Point(0.545, 0.190),
                defaultCenterNorm: new Point(0.45, 0.181),
                installedCount: effectiveTopFans,
                defaultGhostCount: 3,
                radius: rBase * 0.90,
                scaleX: 0.98, scaleY: 0.42, tiltDeg: 4,
                rpm: effectiveTopRpm, duty: effectiveTopDuty, isPulsing: effectiveTopPulse, isIntakeCyan: false);

            // 3. BOTTOM PSU SHROUD / COLD-FLOOR BANK (1x..4x along bottom floor)
            double botYStart = _currentCaseModel.HasShroudLcdScreen ? 0.665 : 0.745;
            double botYEnd = _currentCaseModel.HasShroudLcdScreen ? 0.705 : 0.795;
            Point botCenter = RenderDynamicFanBank(
                dc, w, h,
                startNorm: new Point(0.36, botYStart),
                endNorm: new Point(0.54, botYEnd),
                defaultCenterNorm: new Point(0.45, (botYStart + botYEnd) * 0.5),
                installedCount: botFans,
                defaultGhostCount: 3,
                radius: rBase * 0.88,
                scaleX: 0.98, scaleY: 0.38, tiltDeg: 10,
                rpm: botRpm, duty: botDuty, isPulsing: botPulse, isIntakeCyan: true);

            // 4. REAR EXHAUST FAN (1x..2x on rear grille)
            Point rearCenter = RenderDynamicFanBank(
                dc, w, h,
                startNorm: new Point(0.305, 0.325),
                endNorm: new Point(0.305, 0.385),
                defaultCenterNorm: new Point(0.305, 0.345),
                installedCount: rearFans,
                defaultGhostCount: 1,
                radius: rBase * 1.00,
                scaleX: 0.58, scaleY: 0.98, tiltDeg: -6,
                rpm: rearRpm, duty: rearDuty, isPulsing: rearPulse, isIntakeCyan: false);

            // 5. CPU AIO PUMP BLOCK
            Point cpuCenter = MapPt(w, h, 0.455, 0.375);
            Draw3DAioPumpBlock(dc, cpuCenter, MapPt(w, h, 0.535, 0.19), rBase * 0.84, _engine.CpuTempC, cpuRpm > 0 ? cpuRpm : topRpm, cpuPulse);

            // 6. GPU COOLING SHROUD (with dynamic fan count 1x..3x)
            Point gpuCenter = MapPt(w, h, 0.445, 0.545);
            Draw3DVerticalGpuCard(dc, gpuCenter, rBase * 2.15, rBase * 0.88, _engine.GpuCoreTempC, gpuRpm, gpuDuty, gpuPulse, Math.Max(1, Math.Min(3, gpuFans > 0 ? gpuFans : 3)));

            // 7. If case has Shroud LCD Screen (e.g. Thermalright TL-M10 Vision), draw live telemetry overlay on shroud LCD
            if (_currentCaseModel.HasShroudLcdScreen)
            {
                DrawVisionShroudLcdTelemetry(dc, w, h);
            }

            // 8. Apple Glass HUD Callout Badges
            string shortGpu = HardwareEngine.ShortHardwareLabel(_engine.GpuName);
            if (shortGpu.Length > 12) shortGpu = shortGpu.Substring(0, 12).Trim();

            DrawZoneHudBadge(dc, ChassisZone.Top, new Rect(w * 0.29, h * 0.075, w * 0.39, 26), topCenter, "TOP RADIATOR", effectiveTopFans, effectiveTopRpm, effectiveTopDuty, effectiveTopPulse);
            DrawZoneHudBadge(dc, ChassisZone.Side, new Rect(w * 0.69, h * 0.40, w * 0.29, 40), sideCenter, "SIDE INTAKE", sideFans, sideRpm, sideDuty, sidePulse);
            DrawZoneHudBadge(dc, ChassisZone.Bottom, new Rect(w * 0.29, h * 0.875, w * 0.42, 26), botCenter, "BOTTOM INTAKE", botFans, botRpm, botDuty, botPulse);
            DrawZoneHudBadge(dc, ChassisZone.Rear, new Rect(w * 0.02, h * 0.25, w * 0.25, 40), rearCenter, "REAR EXHAUST", rearFans, rearRpm, rearDuty, rearPulse);
            DrawZoneHudBadge(dc, ChassisZone.CpuAio, new Rect(w * 0.02, h * 0.43, w * 0.25, 40), cpuCenter, $"CPU AIO ({_engine.CpuTempC:0}°C)", cpuFans, cpuRpm, cpuDuty, cpuPulse);
            DrawZoneHudBadge(dc, ChassisZone.Gpu, new Rect(w * 0.02, h * 0.61, w * 0.25, 40), gpuCenter, $"{shortGpu} ({_engine.GpuCoreTempC:0}°C)", gpuFans, gpuRpm, gpuDuty, gpuPulse);
        }

        private Point RenderDynamicFanBank(
            DrawingContext dc,
            double w,
            double h,
            Point startNorm,
            Point endNorm,
            Point defaultCenterNorm,
            int installedCount,
            int defaultGhostCount,
            double radius,
            double scaleX,
            double scaleY,
            double tiltDeg,
            int rpm,
            double duty,
            bool isPulsing,
            bool isIntakeCyan)
        {
            int countToDraw = installedCount > 0 ? Math.Min(4, installedCount) : defaultGhostCount;
            double rAdjusted = countToDraw >= 4 ? radius * 0.78 : radius;

            for (int i = 0; i < countToDraw; i++)
            {
                double t = countToDraw == 1 ? 0.5 : (double)i / (countToDraw - 1);
                Point ptNorm = countToDraw == 1
                    ? defaultCenterNorm
                    : new Point(
                        startNorm.X + (endNorm.X - startNorm.X) * t,
                        startNorm.Y + (endNorm.Y - startNorm.Y) * t);

                Point screenPt = MapPt(w, h, ptNorm.X, ptNorm.Y);
                if (installedCount > 0)
                {
                    Draw3DPerspectiveTurbine(
                        dc,
                        screenPt,
                        rAdjusted,
                        scaleX,
                        scaleY,
                        tiltDeg,
                        RotorAngle + i * 32.0,
                        rpm,
                        duty,
                        isPulsing,
                        isIntakeCyan);
                }
                else
                {
                    // Subtle ghost mounting ring showing available slot when 0 fans assigned to this zone
                    var tg = new TransformGroup();
                    tg.Children.Add(new TranslateTransform(-screenPt.X, -screenPt.Y));
                    tg.Children.Add(new ScaleTransform(scaleX, scaleY));
                    tg.Children.Add(new RotateTransform(tiltDeg));
                    tg.Children.Add(new TranslateTransform(screenPt.X, screenPt.Y));
                    dc.PushTransform(tg);
                    dc.DrawEllipse(
                        new SolidColorBrush(Color.FromArgb(40, 15, 20, 28)),
                        new Pen(new SolidColorBrush(Color.FromArgb(75, 100, 116, 139)), 1.2) { DashStyle = DashStyles.Dash },
                        screenPt, rAdjusted * 0.85, rAdjusted * 0.85);
                    dc.Pop();
                }
            }

            return MapPt(w, h, defaultCenterNorm.X, defaultCenterNorm.Y);
        }

        private void DrawVisionShroudLcdTelemetry(DrawingContext dc, double w, double h)
        {
            // Overlay live CPU/GPU readout badge near the bottom-left PSU shroud LCD screen area
            Point lcdPt = MapPt(w, h, 0.38, 0.76);
            dc.PushTransform(new SkewTransform(0, 14, lcdPt.X, lcdPt.Y));
            Rect lcdBadge = new Rect(lcdPt.X - 58, lcdPt.Y - 13, 116, 24);
            dc.DrawRoundedRectangle(
                new SolidColorBrush(Color.FromArgb(190, 6, 18, 28)),
                new Pen(new SolidColorBrush(Color.FromArgb(180, 100, 210, 255)), 1.0),
                lcdBadge, 4, 4);
            DrawText(dc, $"LCD  CPU {_engine.CpuTempC:0}°C  GPU {_engine.GpuCoreTempC:0}°C",
                lcdBadge.Left + 6, lcdBadge.Top + 6, 8.0, StudioTheme.FontMono, StudioTheme.AccentCyan);
            dc.Pop();
        }

        private void DrawAppleProcedural3DChassis(DrawingContext dc, Rect stage, CaseModelDefinition model)
        {
            double cx = stage.Left + stage.Width * 0.48;
            double cy = stage.Top + stage.Height * 0.48;
            double cw = stage.Width * 0.54;
            double ch = stage.Height * 0.74;

            // Isometric 3D box vertices
            Point fTL = new Point(cx - cw * 0.46, cy - ch * 0.46);
            Point fTR = new Point(cx + cw * 0.14, cy - ch * 0.40);
            Point fBR = new Point(cx + cw * 0.14, cy + ch * 0.46);
            Point fBL = new Point(cx - cw * 0.46, cy + ch * 0.34);

            Point rTR = new Point(cx + cw * 0.46, cy - ch * 0.47);
            Point rBR = new Point(cx + cw * 0.46, cy + ch * 0.35);
            Point bTL = new Point(cx - cw * 0.14, cy - ch * 0.53);

            var hullFill = new LinearGradientBrush(
                Color.FromArgb(210, 20, 24, 34),
                Color.FromArgb(230, 10, 12, 18),
                new Point(0, 0), new Point(1, 1));
            var glassFill = new LinearGradientBrush(
                Color.FromArgb(65, 100, 210, 255),
                Color.FromArgb(15, 10, 132, 255),
                new Point(0, 0), new Point(1, 1));

            var edgePen = new Pen(new SolidColorBrush(Color.FromRgb(75, 90, 115)), 1.8);
            var glassPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 100, 210, 255)), 1.4);

            // Back interior panel
            var backGeom = CreateQuad(bTL, rTR, rBR, fBL);
            dc.DrawGeometry(hullFill, edgePen, backGeom);

            // Top radiator ceiling plane
            var topGeom = CreateQuad(fTL, fTR, rTR, bTL);
            dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(160, 24, 29, 40)), edgePen, topGeom);

            // Left tempered glass side panel
            var leftGlass = CreateQuad(fTL, fTR, fBR, fBL);
            dc.DrawGeometry(glassFill, glassPen, leftGlass);

            // Front panoramic glass panel
            var frontGlass = CreateQuad(fTR, rTR, rBR, fBR);
            dc.DrawGeometry(glassFill, glassPen, frontGlass);

            // Bottom PSU shroud chamber
            Point sTL = new Point(fTL.X, fBL.Y - ch * 0.16);
            Point sTR = new Point(fTR.X, fBR.Y - ch * 0.16);
            var shroudGeom = CreateQuad(sTL, sTR, fBR, fBL);
            dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(235, 14, 17, 24)), edgePen, shroudGeom);
        }

        private static StreamGeometry CreateQuad(Point p1, Point p2, Point p3, Point p4)
        {
            var sg = new StreamGeometry();
            using (var ctx = sg.Open())
            {
                ctx.BeginFigure(p1, true, true);
                ctx.LineTo(p2, true, false);
                ctx.LineTo(p3, true, false);
                ctx.LineTo(p4, true, false);
            }
            sg.Freeze();
            return sg;
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
            var tubePenHighlight = new Pen(new SolidColorBrush(Color.FromArgb(140, 100, 210, 255)), 1.4);
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

            Color ringCol = pulsing ? StudioTheme.AccentCyanColor : StudioTheme.AccentBlueColor;
            dc.DrawEllipse(
                new SolidColorBrush(Color.FromRgb(12, 15, 22)),
                new Pen(new SolidColorBrush(ringCol), 2.4),
                center, radius, radius);
            dc.DrawEllipse(
                null,
                new Pen(new SolidColorBrush(Color.FromArgb(170, 100, 210, 255)), 1.4) { DashStyle = DashStyles.Dash },
                center, radius * 0.80, radius * 0.80);

            DrawText(dc, $"{cpuTemp:0}°C", center.X - 14, center.Y - 9, 10.5, StudioTheme.FontMono, StudioTheme.TextPrimary);
            DrawText(dc, "AIO LCD", center.X - 16, center.Y + 4, 7.2, StudioTheme.FontMono, StudioTheme.AccentCyan);
        }

        private void Draw3DVerticalGpuCard(DrawingContext dc, Point center, double halfW, double halfH, double gpuTemp, int rpm, double duty, bool pulsing, int fanCount)
        {
            dc.PushTransform(new SkewTransform(0, 6, center.X, center.Y));

            Rect cardRect = new Rect(center.X - halfW, center.Y - halfH, halfW * 2, halfH * 2);
            var shroudGrad = new LinearGradientBrush(
                Color.FromArgb(232, 26, 31, 42),
                Color.FromArgb(238, 14, 17, 24),
                new Point(0, 0), new Point(0, 1));
            Color borderCol = pulsing ? StudioTheme.AccentCyanColor : Color.FromRgb(68, 80, 100);

            dc.DrawRoundedRectangle(shroudGrad, new Pen(new SolidColorBrush(borderCol), 1.6), cardRect, 7, 7);

            Rect ledBar = new Rect(cardRect.Left + 10, cardRect.Top + 3, cardRect.Width - 20, 3.5);
            dc.DrawRoundedRectangle(StudioTheme.AccentBlue, null, ledBar, 2, 2);

            double fanR = halfH * 0.66;
            int count = Math.Max(1, Math.Min(3, fanCount));
            for (int i = 0; i < count; i++)
            {
                double normOffset = count == 1 ? 0 : ((i / (double)(count - 1)) * 2.0 - 1.0);
                double fx = center.X + normOffset * (halfW * 0.62);
                double fy = center.Y + 3;
                SpinningFanControl.DrawTurbine(dc, fx, fy, fanR, RotorAngle * (i == 1 ? -1 : 1), rpm, duty, pulsing, true);
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
            bool isPulsing)
        {
            _zoneHitRects[zone] = badgeRect;

            bool isSelected = SelectedZoneFilter == zone;
            bool isHovered = _hoveredZone == zone;

            Color accent = isPulsing ? StudioTheme.AccentCyanColor :
                           isSelected ? StudioTheme.AccentBlueColor :
                           isHovered ? Color.FromRgb(200, 215, 235) :
                           (fanCount > 0 ? StudioTheme.AccentCyanColor : Color.FromRgb(78, 90, 110));

            Point pillAnchor = new Point(
                badgeRect.Left + badgeRect.Width / 2.0,
                badgeRect.Top + badgeRect.Height / 2.0);
            var leaderPen = new Pen(new SolidColorBrush(Color.FromArgb(isSelected || isHovered || isPulsing ? (byte)200 : (byte)95, accent.R, accent.G, accent.B)), 1.2);
            dc.DrawLine(leaderPen, pillAnchor, target3DPoint);
            dc.DrawEllipse(new SolidColorBrush(accent), null, target3DPoint, 3.0, 3.0);

            var bgFill = new SolidColorBrush(
                isPulsing ? Color.FromArgb(232, 10, 42, 62) :
                isSelected ? Color.FromArgb(235, 14, 34, 64) :
                Color.FromArgb(220, 17, 20, 28));

            dc.DrawRoundedRectangle(bgFill, new Pen(new SolidColorBrush(accent), isSelected || isPulsing ? 1.8 : 1.0), badgeRect, 7, 7);

            string stats = fanCount > 0 ? $"{fanCount}× • {avgRpm} RPM" : "+ CLICK TO ADD";
            if (badgeRect.Height <= 30)
            {
                DrawText(dc, $"{title}: {stats} ({avgDuty:0}%)", badgeRect.Left + 8, badgeRect.Top + 6, 9.2, StudioTheme.FontMono, StudioTheme.TextPrimary);
            }
            else
            {
                DrawText(dc, title, badgeRect.Left + 8, badgeRect.Top + 4, 9.2, StudioTheme.FontUIBold,
                    isSelected ? StudioTheme.AccentCyan : StudioTheme.TextPrimary);
                DrawText(dc, stats, badgeRect.Left + 8, badgeRect.Top + 19, 9.0, StudioTheme.FontMono,
                    fanCount > 0 ? StudioTheme.AccentGreen : StudioTheme.TextDim);
            }
        }

        private void GetZoneStats(ChassisZone zone, out int totalFans, out int avgRpm, out double avgDuty, out bool hasPulsing)
        {
            var list = _engine.Channels.Where(c => c.Zone == zone).ToList();
            totalFans = list.Sum(c => c.PhysicalFanCount);
            avgRpm = list.Any(c => c.CurrentRpm > 0) ? (int)Math.Round(list.Where(c => c.CurrentRpm > 0).Average(c => c.CurrentRpm)) : 0;
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

        private const double PadLeft = 42;
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
                if ((p - pos).Length <= 13)
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
                new SolidColorBrush(Color.FromRgb(13, 15, 21)),
                new Pen(StudioTheme.BorderSubtle, 1.0),
                new Rect(0, 0, ActualWidth, ActualHeight), 10, 10);

            var gridPen = new Pen(new SolidColorBrush(Color.FromRgb(26, 30, 40)), 1.0);

            for (int pct = 0; pct <= 100; pct += 25)
            {
                Point p = TempDutyToScreen(plot, MinTemp, pct);
                dc.DrawLine(gridPen, new Point(plot.Left, p.Y), new Point(plot.Right, p.Y));
                DrawText(dc, $"{pct}%", 8, p.Y - 6, 9.2, StudioTheme.FontMono, StudioTheme.TextDim);
            }

            for (int tc = 30; tc <= 90; tc += 15)
            {
                Point p = TempDutyToScreen(plot, tc, 0);
                dc.DrawLine(gridPen, new Point(p.X, plot.Top), new Point(p.X, plot.Bottom));
                DrawText(dc, $"{tc}°C", p.X - 12, plot.Bottom + 6, 9.2, StudioTheme.FontMono, StudioTheme.TextDim);
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
                Color.FromArgb(75, 10, 132, 255),
                Color.FromArgb(4, 10, 132, 255),
                new Point(0, 0), new Point(0, 1));
            dc.DrawGeometry(areaBrush, null, fillGeom);

            var linePen = new Pen(StudioTheme.AccentBlue, 2.4);
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
            var crossPen = new Pen(new SolidColorBrush(Color.FromArgb(130, 100, 210, 255)), 1.0) { DashStyle = DashStyles.Dash };
            dc.DrawLine(crossPen, new Point(livePt.X, plot.Top), new Point(livePt.X, plot.Bottom));
            dc.DrawLine(crossPen, new Point(plot.Left, livePt.Y), new Point(plot.Right, livePt.Y));
            dc.DrawEllipse(StudioTheme.AccentCyan, new Pen(Brushes.Black, 1.5), livePt, 5.5, 5.5);

            for (int i = 0; i < pts.Count; i++)
            {
                Point np = TempDutyToScreen(plot, pts[i].TempC, pts[i].DutyPercent);
                bool active = i == _draggingIndex;
                dc.DrawEllipse(
                    active ? StudioTheme.AccentCyan : StudioTheme.BgCard,
                    new Pen(StudioTheme.AccentBlue, 2.2),
                    np, active ? 6.5 : 5.2, active ? 6.5 : 5.2);

                DrawText(dc, $"{pts[i].TempC:0}°/{pts[i].DutyPercent:0}%",
                    Math.Min(plot.Right - 44, Math.Max(plot.Left, np.X - 18)),
                    Math.Max(4, np.Y - 18),
                    8.8, StudioTheme.FontMono, StudioTheme.TextMuted);
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
