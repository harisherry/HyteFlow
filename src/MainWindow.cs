using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace HyteFlow
{
    public class MainWindow : Window
    {
        private readonly HardwareEngine _engine;
        private readonly DispatcherTimer _telemetryTimer;
        private readonly DispatcherTimer _animTimer;

        private HyteY70BlueprintControl _blueprint;
        private FanCurveEditorControl _curveEditor;
        private StackPanel _cardsContainer;

        private TextBlock _hwSubtitleText;
        private TextBlock _cpuTempText;
        private TextBlock _gpuTempText;
        private TextBlock _vramTempText;
        private TextBlock _totalFansText;
        private TextBlock _statusFooterText;
        private TextBlock _selectedChannelHeaderText;
        private TextBlock _selectedChannelSubText;

        private Button _btnStartWindows;
        private Button _btnShowEmpty;
        private Button _btnCaseCycle;
        private Button _btnViewMode;
        private Button _tempSourceBtn;
        private readonly Dictionary<ChassisZone, Button> _zoneAssignButtons = new Dictionary<ChassisZone, Button>();
        private readonly Dictionary<int, Button> _fanCountButtons = new Dictionary<int, Button>();
        private readonly Dictionary<int, Button> _fanSizeButtons = new Dictionary<int, Button>();
        private readonly Dictionary<FanControlMode, Button> _globalModeButtons = new Dictionary<FanControlMode, Button>();

        private FanChannelModel _selectedChannel;
        private ChassisZone? _activeZoneFilter = null;
        private readonly List<ChannelCardView> _cardViews = new List<ChannelCardView>();
        private System.Windows.Forms.NotifyIcon _trayIcon;

        public MainWindow(HardwareEngine engine, bool startMinimized)
        {
            _engine = engine;

            Title = "HyteFlow Studio — Universal 3D Chassis & Thermal Control";
            Width = 1400;
            Height = 880;
            MinWidth = 1160;
            MinHeight = 740;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = StudioTheme.BgCanvas;
            Foreground = StudioTheme.TextPrimary;
            FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI");

            ApplyAppleButtonTemplate();
            BuildUserInterface();
            SetupSystemTray();

            _selectedChannel = _engine.Channels.FirstOrDefault(c => c.IsSpinning) ?? _engine.Channels.FirstOrDefault();
            RebuildChannelCards();
            UpdateInspectorPanel();
            UpdateTelemetryHeader();

            _telemetryTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
            _telemetryTimer.Tick += (s, e) =>
            {
                _engine.TickPulseTimers();
                _engine.UpdateAll(1.0);
                UpdateTelemetryHeader();
                foreach (var cv in _cardViews)
                    cv.RefreshTelemetry();
                _curveEditor.InvalidateVisual();
                _blueprint.InvalidateVisual();
            };
            _telemetryTimer.Start();

            _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _animTimer.Tick += (s, e) =>
            {
                _blueprint.RotorAngle = (_blueprint.RotorAngle + 7.5) % 360.0;
                _blueprint.InvalidateVisual();
                foreach (var cv in _cardViews)
                    cv.TickAnimation();
            };
            _animTimer.Start();

            if (startMinimized)
            {
                WindowState = WindowState.Minimized;
                Hide();
            }

            Closing += OnWindowClosing;
        }

        private void ApplyAppleButtonTemplate()
        {
            // Give all WPF Buttons smooth pill/rounded-rect borders with no ugly Windows 95 chrome
            var style = new Style(typeof(Button));
            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "BtnBorder";
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(7));
            borderFactory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            borderFactory.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            borderFactory.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            borderFactory.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentFactory);

            template.VisualTree = borderFactory;
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            Resources.Add(typeof(Button), style);
        }

        private void BuildUserInterface()
        {
            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(66) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(62) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(34) });

            // 1. TOP APPLE STUDIO HEADER BAR
            var topBar = new Border
            {
                Background = StudioTheme.BgPanel,
                BorderBrush = StudioTheme.BorderSubtle,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(22, 0, 22, 0)
            };
            var topDock = new DockPanel { LastChildFill = false };

            var brandStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            string iconPngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "app_icon.png");
            if (File.Exists(iconPngPath))
            {
                try
                {
                    var iconBmp = new BitmapImage(new Uri(iconPngPath, UriKind.Absolute));
                    Icon = iconBmp;
                    var img = new Image
                    {
                        Source = iconBmp,
                        Width = 38,
                        Height = 38,
                        Margin = new Thickness(0, 0, 14, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                    brandStack.Children.Add(img);
                }
                catch { }
            }

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var appTitle = new TextBlock
            {
                Text = "HyteFlow Studio",
                FontSize = 17,
                FontWeight = FontWeights.SemiBold,
                Foreground = StudioTheme.TextPrimary
            };
            var initialModel = CaseCatalog.FindOrCreatePreset(_engine.Config.ChassisPreset);
            _hwSubtitleText = new TextBlock
            {
                Text = $"{initialModel.DisplayName.Split('(')[0].Trim()}  •  {_engine.MotherboardName}  •  {_engine.SuperIoChipName}  •  {HardwareEngine.ShortHardwareLabel(_engine.GpuName)}",
                FontSize = 11,
                Foreground = StudioTheme.TextMuted
            };
            titleStack.Children.Add(appTitle);
            titleStack.Children.Add(_hwSubtitleText);
            brandStack.Children.Add(titleStack);
            DockPanel.SetDock(brandStack, Dock.Left);
            topDock.Children.Add(brandStack);

            var actionsStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            if (!_engine.IsAdmin)
            {
                var btnAdmin = CreateHeaderButton("⚡ Unlock Super I/O (Admin)", true, StudioTheme.AccentAmber);
                btnAdmin.ToolTip = "Restart HyteFlow with Administrator privileges so PawnIO can directly control all motherboard PWM headers.";
                btnAdmin.Click += (s, e) => StartupManager.RestartAsAdmin();
                actionsStack.Children.Add(btnAdmin);
            }

            var btnAutoMap = CreateHeaderButton("✦ Auto-Place Fans", true, StudioTheme.AccentCyan);
            btnAutoMap.ToolTip = "Automatically detect spinning headers and place them into the optimal 3D chassis zones (Top, Side, Bottom, Rear, CPU, GPU).";
            btnAutoMap.Click += (s, e) =>
            {
                int count = _engine.AutoDetectAndPlaceInstalledFans(onlyIfUnmapped: false);
                RebuildChannelCards();
                UpdateInspectorPanel();
                UpdateTelemetryHeader();
                _blueprint.InvalidateVisual();
                _statusFooterText.Text = $"Auto-Placed {count} active fan groups across 3D chassis zones.";
            };
            actionsStack.Children.Add(btnAutoMap);

            var btnAddFans = CreateHeaderButton("＋ Add Fans", true, StudioTheme.AccentBlue);
            btnAddFans.ToolTip = "Add a physical or splitter fan bank (1x–4x 120mm/140mm) to any zone on your 3D case.";
            btnAddFans.Click += (s, e) => ShowAddFanGroupDialog();
            actionsStack.Children.Add(btnAddFans);

            _btnShowEmpty = CreateHeaderButton(
                _engine.Config.ShowEmptyHeaders ? "◉ All Headers Shown" : "◎ Hide 0-RPM Headers",
                _engine.Config.ShowEmptyHeaders,
                StudioTheme.AccentCyan);
            _btnShowEmpty.Click += (s, e) =>
            {
                _engine.Config.ShowEmptyHeaders = !_engine.Config.ShowEmptyHeaders;
                UpdateHeaderToggleButtons();
                RebuildChannelCards();
                _engine.SaveConfig();
            };
            actionsStack.Children.Add(_btnShowEmpty);

            _btnStartWindows = CreateHeaderButton(
                _engine.Config.StartWithWindows ? "✓ Start with Windows" : "Start with Windows: Off",
                _engine.Config.StartWithWindows,
                StudioTheme.AccentGreen);
            _btnStartWindows.Click += (s, e) =>
            {
                bool target = !_engine.Config.StartWithWindows;
                if (StartupManager.SetStartupEnabled(target, false, out string msg))
                {
                    _engine.Config.StartWithWindows = target;
                    UpdateHeaderToggleButtons();
                    _engine.SaveConfig();
                    _statusFooterText.Text = msg;
                }
            };
            actionsStack.Children.Add(_btnStartWindows);

            DockPanel.SetDock(actionsStack, Dock.Right);
            topDock.Children.Add(actionsStack);
            topBar.Child = topDock;
            Grid.SetRow(topBar, 0);
            rootGrid.Children.Add(topBar);

            // 2. TELEMETRY WIDGET BAR + SEGMENTED ACOUSTIC PROFILES
            var stripBar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(14, 16, 22)),
                BorderBrush = StudioTheme.BorderSubtle,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(22, 0, 22, 0)
            };
            var stripDock = new DockPanel { LastChildFill = false };

            string shortCpu = HardwareEngine.ShortHardwareLabel(_engine.CpuName);
            string shortGpu = HardwareEngine.ShortHardwareLabel(_engine.GpuName);

            var metricsStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            metricsStack.Children.Add(CreateTelemetryMetricBox($"CPU PACKAGE ({shortCpu})", out _cpuTempText, StudioTheme.AccentOrange));
            metricsStack.Children.Add(CreateTelemetryMetricBox($"GPU CORE ({shortGpu})", out _gpuTempText, StudioTheme.AccentCyan));
            metricsStack.Children.Add(CreateTelemetryMetricBox($"{shortGpu.ToUpperInvariant()} HOTSPOT / MEM", out _vramTempText, StudioTheme.AccentGreen));
            metricsStack.Children.Add(CreateTelemetryMetricBox("INSTALLED TOPOLOGY", out _totalFansText, StudioTheme.TextPrimary));
            DockPanel.SetDock(metricsStack, Dock.Left);
            stripDock.Children.Add(metricsStack);

            var presetStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            presetStack.Children.Add(new TextBlock
            {
                Text = "Acoustic Profile",
                FontSize = 11.5,
                FontWeight = FontWeights.Medium,
                Foreground = StudioTheme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            });

            AddGlobalPresetButton(presetStack, FanControlMode.SmartCurve, "Smart Curves");
            AddGlobalPresetButton(presetStack, FanControlMode.Silent, "Silent");
            AddGlobalPresetButton(presetStack, FanControlMode.Balanced, "Balanced");
            AddGlobalPresetButton(presetStack, FanControlMode.Performance, "Performance");
            AddGlobalPresetButton(presetStack, FanControlMode.FullBlast, "Max Blast");

            DockPanel.SetDock(presetStack, Dock.Right);
            stripDock.Children.Add(presetStack);
            stripBar.Child = stripDock;
            Grid.SetRow(stripBar, 1);
            rootGrid.Children.Add(stripBar);

            // 3. MAIN SPLIT WORKSPACE
            var mainGrid = new Grid { Margin = new Thickness(20, 16, 20, 16) };
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(560) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var leftGrid = new Grid();
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.12, GridUnitType.Star) });
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14) });
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.96, GridUnitType.Star) });

            var blueprintCard = new Border
            {
                Background = StudioTheme.BgPanel,
                BorderBrush = StudioTheme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(14)
            };
            var bpDock = new DockPanel();

            var topBpBar = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            var filterWrap = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
            filterWrap.Children.Add(CreateFilterChip("All", null));
            filterWrap.Children.Add(CreateFilterChip("Top", ChassisZone.Top));
            filterWrap.Children.Add(CreateFilterChip("Side", ChassisZone.Side));
            filterWrap.Children.Add(CreateFilterChip("Bottom", ChassisZone.Bottom));
            filterWrap.Children.Add(CreateFilterChip("Rear", ChassisZone.Rear));
            filterWrap.Children.Add(CreateFilterChip("CPU", ChassisZone.CpuAio));
            filterWrap.Children.Add(CreateFilterChip("GPU", ChassisZone.Gpu));
            DockPanel.SetDock(filterWrap, Dock.Left);
            topBpBar.Children.Add(filterWrap);

            var caseCtrlStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            _blueprint = new HyteY70BlueprintControl(_engine);
            _blueprint.ZoneClicked += OnBlueprintZoneClicked;

            string initialShortCase = _blueprint.CurrentCaseModel.DisplayName.Split('(')[0].Trim();
            if (initialShortCase.Length > 15) initialShortCase = initialShortCase.Substring(0, 15).Trim();

            _btnCaseCycle = new Button
            {
                Content = $"{initialShortCase} ▾",
                FontSize = 10.0,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(7, 4, 7, 4),
                Margin = new Thickness(0, 0, 4, 0),
                Cursor = Cursors.Hand,
                Background = StudioTheme.BgCard,
                Foreground = StudioTheme.AccentCyan,
                BorderBrush = StudioTheme.BorderBrush,
                BorderThickness = new Thickness(1),
                ToolTip = "Cycle through built-in 3D PC Case models"
            };
            _btnCaseCycle.Click += (s, e) =>
            {
                int idx = CaseCatalog.Presets.IndexOf(_blueprint.CurrentCaseModel);
                var nextModel = CaseCatalog.Presets[(idx + 1) % CaseCatalog.Presets.Count];
                ApplySelectedCaseModel(nextModel);
            };
            caseCtrlStack.Children.Add(_btnCaseCycle);

            _btnViewMode = new Button
            {
                Content = _blueprint.WireframeOrbitMode ? "3D CAD" : "3D Render",
                FontSize = 10.0,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(7, 4, 7, 4),
                Margin = new Thickness(0, 0, 4, 0),
                Cursor = Cursors.Hand,
                Background = StudioTheme.BgCard,
                Foreground = StudioTheme.TextPrimary,
                BorderBrush = StudioTheme.BorderBrush,
                BorderThickness = new Thickness(1),
                ToolTip = "Toggle between 3D Studio Render and Isometric 3D Architectural CAD Wireframe"
            };
            _btnViewMode.Click += (s, e) =>
            {
                _blueprint.WireframeOrbitMode = !_blueprint.WireframeOrbitMode;
                _engine.Config.UseProcedural3D = _blueprint.WireframeOrbitMode;
                _btnViewMode.Content = _blueprint.WireframeOrbitMode ? "3D CAD" : "3D Render";
                _blueprint.InvalidateVisual();
                _engine.SaveConfig();
            };
            caseCtrlStack.Children.Add(_btnViewMode);

            var btnDownloadCase = new Button
            {
                Content = "🌐 Cases",
                FontSize = 10.0,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(8, 4, 8, 4),
                Cursor = Cursors.Hand,
                Background = StudioTheme.AccentBlue,
                Foreground = Brushes.White,
                BorderBrush = StudioTheme.AccentBlue,
                BorderThickness = new Thickness(1),
                ToolTip = "Browse the 3D Case Catalog or search & pull any PC case model."
            };
            btnDownloadCase.Click += (s, e) => ShowCaseCatalogDialog();
            caseCtrlStack.Children.Add(btnDownloadCase);

            DockPanel.SetDock(caseCtrlStack, Dock.Right);
            topBpBar.Children.Add(caseCtrlStack);

            DockPanel.SetDock(topBpBar, Dock.Top);
            bpDock.Children.Add(topBpBar);
            bpDock.Children.Add(_blueprint);

            blueprintCard.Child = bpDock;
            Grid.SetRow(blueprintCard, 0);
            leftGrid.Children.Add(blueprintCard);

            var inspectorCard = BuildInspectorCard();
            Grid.SetRow(inspectorCard, 2);
            leftGrid.Children.Add(inspectorCard);

            Grid.SetColumn(leftGrid, 0);
            mainGrid.Children.Add(leftGrid);

            // RIGHT PANEL: FAN CHANNELS & CONTROLLERS
            var rightBorder = new Border
            {
                Background = StudioTheme.BgPanel,
                BorderBrush = StudioTheme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16)
            };
            var rightDock = new DockPanel();

            var rightHeaderDock = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
            var rightTitle = new TextBlock
            {
                Text = "Active Fan Channels & Chassis Zones",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = StudioTheme.TextPrimary,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(rightTitle, Dock.Left);
            rightHeaderDock.Children.Add(rightTitle);

            var rightActions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var rightHint = new TextBlock
            {
                Text = "Click [⚡ Locate] to pulse 100%  •  ",
                FontSize = 11.5,
                Foreground = StudioTheme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center
            };
            rightActions.Children.Add(rightHint);

            var btnQuickAdd = CreateSmallActionButton("＋ Add Fan Group", StudioTheme.AccentBlue);
            btnQuickAdd.Click += (s, e) => ShowAddFanGroupDialog();
            rightActions.Children.Add(btnQuickAdd);

            DockPanel.SetDock(rightActions, Dock.Right);
            rightHeaderDock.Children.Add(rightActions);

            DockPanel.SetDock(rightHeaderDock, Dock.Top);
            rightDock.Children.Add(rightHeaderDock);

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            _cardsContainer = new StackPanel();
            scrollViewer.Content = _cardsContainer;
            rightDock.Children.Add(scrollViewer);

            rightBorder.Child = rightDock;
            Grid.SetColumn(rightBorder, 2);
            mainGrid.Children.Add(rightBorder);

            Grid.SetRow(mainGrid, 2);
            rootGrid.Children.Add(mainGrid);

            // 4. STATUS FOOTER BAR
            var footerBar = new Border
            {
                Background = StudioTheme.BgPanel,
                BorderBrush = StudioTheme.BorderSubtle,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(22, 0, 22, 0)
            };
            var footerDock = new DockPanel();
            _statusFooterText = new TextBlock
            {
                Text = $"Ready  •  3D Chassis: {_blueprint.CurrentCaseModel.DisplayName}  •  Motherboard LPC: {_engine.SuperIoChipName}",
                FontSize = 11,
                Foreground = StudioTheme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center
            };
            footerDock.Children.Add(_statusFooterText);
            footerBar.Child = footerDock;
            Grid.SetRow(footerBar, 3);
            rootGrid.Children.Add(footerBar);

            Content = rootGrid;
            UpdateGlobalPresetButtons();
        }

        private void ApplySelectedCaseModel(CaseModelDefinition model)
        {
            if (model == null) return;
            _blueprint.CurrentCaseModel = model;
            _engine.Config.ChassisPreset = model.Id;
            _engine.SaveConfig();

            string shortCase = model.DisplayName.Split('(')[0].Trim();
            if (shortCase.Length > 15) shortCase = shortCase.Substring(0, 15).Trim();
            _btnCaseCycle.Content = $"{shortCase} ▾";
            _hwSubtitleText.Text = $"{model.DisplayName.Split('(')[0].Trim()}  •  {_engine.MotherboardName}  •  {_engine.SuperIoChipName}  •  {HardwareEngine.ShortHardwareLabel(_engine.GpuName)}";
            _statusFooterText.Text = $"Loaded 3D Chassis Model: {model.DisplayName}";
        }

        private Border BuildInspectorCard()
        {
            var card = new Border
            {
                Background = StudioTheme.BgPanel,
                BorderBrush = StudioTheme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16)
            };

            var dock = new DockPanel();

            var headerDock = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            headerDock.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerDock.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
            _selectedChannelHeaderText = new TextBlock
            {
                Text = "Selected Fan Channel",
                FontSize = 14.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = StudioTheme.TextPrimary,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            _selectedChannelSubText = new TextBlock
            {
                Text = "Select any fan card on the right to edit its physical case position & thermal curve",
                FontSize = 11.0,
                Foreground = StudioTheme.AccentCyan,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            titleStack.Children.Add(_selectedChannelHeaderText);
            titleStack.Children.Add(_selectedChannelSubText);
            Grid.SetColumn(titleStack, 0);
            headerDock.Children.Add(titleStack);

            var inspectorActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top };
            var btnPulseSelected = CreateSmallActionButton("⚡ Pulse 100%", StudioTheme.AccentCyan);
            btnPulseSelected.Margin = new Thickness(0, 0, 6, 0);
            btnPulseSelected.Click += (s, e) =>
            {
                if (_selectedChannel != null)
                {
                    _engine.TriggerPulseLocate(_selectedChannel, 6);
                    _statusFooterText.Text = $"Pulsing {_selectedChannel.Nickname} to 100% for 6s — watch or listen to your case and click its position below!";
                }
            };
            inspectorActions.Children.Add(btnPulseSelected);

            var btnRemoveSelected = CreateSmallActionButton("✕ Remove", StudioTheme.TextMuted);
            btnRemoveSelected.ToolTip = "Remove custom fan group or unmap this header";
            btnRemoveSelected.Click += (s, e) =>
            {
                if (_selectedChannel != null)
                {
                    string removedName = _selectedChannel.Nickname;
                    _engine.RemoveCustomOrUnpinChannel(_selectedChannel);
                    _selectedChannel = _engine.Channels.FirstOrDefault(c => c.IsSpinning || c.ForcePinVisible) ?? _engine.Channels.FirstOrDefault();
                    RebuildChannelCards();
                    UpdateInspectorPanel();
                    UpdateTelemetryHeader();
                    _blueprint.InvalidateVisual();
                    _statusFooterText.Text = $"Unmapped / removed '{removedName}'.";
                }
            };
            inspectorActions.Children.Add(btnRemoveSelected);

            Grid.SetColumn(inspectorActions, 1);
            headerDock.Children.Add(inspectorActions);

            DockPanel.SetDock(headerDock, Dock.Top);
            dock.Children.Add(headerDock);

            var locRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 7) };
            locRow.Children.Add(new TextBlock
            {
                Text = "Case Zone:",
                Width = 86,
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                Foreground = StudioTheme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center
            });

            foreach (var z in new[] { ChassisZone.Top, ChassisZone.Side, ChassisZone.Bottom, ChassisZone.Rear, ChassisZone.CpuAio, ChassisZone.Gpu })
            {
                var btn = CreateZoneTagButton(z);
                _zoneAssignButtons[z] = btn;
                locRow.Children.Add(btn);
            }
            DockPanel.SetDock(locRow, Dock.Top);
            dock.Children.Add(locRow);

            var configRow = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            var multStack = new StackPanel { Orientation = Orientation.Horizontal };
            multStack.Children.Add(new TextBlock
            {
                Text = "Fan Count:",
                Width = 86,
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                Foreground = StudioTheme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center
            });
            for (int c = 1; c <= 4; c++)
            {
                int countCopy = c;
                var b = CreateMiniOptionButton($"{c}×");
                b.Click += (s, e) =>
                {
                    if (_selectedChannel != null)
                    {
                        _selectedChannel.PhysicalFanCount = countCopy;
                        UpdateInspectorPanel();
                        UpdateTelemetryHeader();
                        foreach (var cv in _cardViews) cv.RefreshTelemetry();
                        _blueprint.InvalidateVisual();
                        _engine.SaveConfig();
                    }
                };
                _fanCountButtons[c] = b;
                multStack.Children.Add(b);
            }

            multStack.Children.Add(new TextBlock
            {
                Text = "Size:",
                FontSize = 11,
                Foreground = StudioTheme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 5, 0)
            });
            foreach (int sz in new[] { 120, 140 })
            {
                int szCopy = sz;
                var b = CreateMiniOptionButton($"{sz}mm");
                b.Click += (s, e) =>
                {
                    if (_selectedChannel != null)
                    {
                        _selectedChannel.FanSizeMm = szCopy;
                        UpdateInspectorPanel();
                        foreach (var cv in _cardViews) cv.RefreshTelemetry();
                        _engine.SaveConfig();
                    }
                };
                _fanSizeButtons[sz] = b;
                multStack.Children.Add(b);
            }

            DockPanel.SetDock(multStack, Dock.Left);
            configRow.Children.Add(multStack);

            var tempStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            tempStack.Children.Add(new TextBlock
            {
                Text = "Sensor:",
                FontSize = 11,
                Foreground = StudioTheme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 5, 0)
            });
            _tempSourceBtn = new Button
            {
                Content = "Smart Mix (CPU/GPU) ▾",
                Width = 148,
                Height = 24,
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                Background = StudioTheme.BgCard,
                Foreground = StudioTheme.AccentCyan,
                BorderBrush = StudioTheme.BorderBrush,
                BorderThickness = new Thickness(1)
            };
            _tempSourceBtn.Click += (s, e) =>
            {
                if (_selectedChannel == null || _engine.TempSensors.Count == 0) return;
                int idx = -1;
                for (int i = 0; i < _engine.TempSensors.Count; i++)
                {
                    if (_engine.TempSensors[i].Id == _selectedChannel.TempSourceId) { idx = i; break; }
                }
                int next = (idx + 1) % _engine.TempSensors.Count;
                _selectedChannel.TempSourceId = _engine.TempSensors[next].Id;
                UpdateInspectorPanel();
                _curveEditor.InvalidateVisual();
                _engine.SaveConfig();
            };
            tempStack.Children.Add(_tempSourceBtn);
            DockPanel.SetDock(tempStack, Dock.Right);
            configRow.Children.Add(tempStack);

            DockPanel.SetDock(configRow, Dock.Top);
            dock.Children.Add(configRow);

            _curveEditor = new FanCurveEditorControl(_engine);
            _curveEditor.CurveModified += () =>
            {
                if (_selectedChannel != null)
                {
                    _engine.UpdateAll(0.2);
                    foreach (var cv in _cardViews) cv.RefreshTelemetry();
                }
            };
            dock.Children.Add(_curveEditor);

            card.Child = dock;
            return card;
        }

        public void SelectChannel(FanChannelModel channel)
        {
            _selectedChannel = channel;
            foreach (var cv in _cardViews)
                cv.SetSelected(cv.Channel == _selectedChannel);
            UpdateInspectorPanel();
        }

        private void UpdateInspectorPanel()
        {
            if (_selectedChannel == null) return;

            string displayTitle = _selectedChannel.Nickname;
            if (displayTitle.Length > 34) displayTitle = displayTitle.Substring(0, 34).Trim() + "…";

            _selectedChannelHeaderText.Text = displayTitle;
            _selectedChannelSubText.Text = $"{_selectedChannel.ZoneLabel}  •  {_selectedChannel.TopologyLabel}  •  {_selectedChannel.CurrentRpm:N0} RPM ({_selectedChannel.CurrentDutyPercent:0}% PWM)";
            _curveEditor.Channel = _selectedChannel;

            foreach (var kv in _zoneAssignButtons)
            {
                bool active = _selectedChannel.Zone == kv.Key;
                kv.Value.Background = active ? StudioTheme.AccentBlue : StudioTheme.BgCard;
                kv.Value.Foreground = active ? Brushes.White : StudioTheme.TextPrimary;
                kv.Value.BorderBrush = active ? StudioTheme.AccentBlue : StudioTheme.BorderBrush;
            }

            foreach (var kv in _fanCountButtons)
            {
                bool active = _selectedChannel.PhysicalFanCount == kv.Key;
                kv.Value.Background = active ? StudioTheme.AccentCyan : StudioTheme.BgCard;
                kv.Value.Foreground = active ? Brushes.Black : StudioTheme.TextPrimary;
                kv.Value.BorderBrush = active ? StudioTheme.AccentCyan : StudioTheme.BorderBrush;
            }

            foreach (var kv in _fanSizeButtons)
            {
                bool active = _selectedChannel.FanSizeMm == kv.Key;
                kv.Value.Background = active ? StudioTheme.AccentBlue : StudioTheme.BgCard;
                kv.Value.Foreground = active ? Brushes.White : StudioTheme.TextPrimary;
                kv.Value.BorderBrush = active ? StudioTheme.AccentBlue : StudioTheme.BorderBrush;
            }

            var ts = _engine.TempSensors.FirstOrDefault(t => t.Id == _selectedChannel.TempSourceId) ?? _engine.TempSensors.FirstOrDefault();
            if (ts != null)
            {
                string shortTs = ts.Name.Length > 20 ? ts.Name.Substring(0, 20) + ".." : ts.Name;
                _tempSourceBtn.Content = shortTs + " ▾";
            }
        }

        private void OnBlueprintZoneClicked(ChassisZone clickedZone)
        {
            var pulsing = _engine.Channels.FirstOrDefault(c => c.IsPulsing);
            if (pulsing != null)
            {
                pulsing.Zone = clickedZone;
                _engine.SaveConfig();
                UpdateInspectorPanel();
                foreach (var cv in _cardViews) cv.RefreshTelemetry();
                _blueprint.InvalidateVisual();
                _statusFooterText.Text = $"Assigned [{pulsing.Nickname}] to {pulsing.ZoneLabel}!";
                return;
            }

            var firstInZone = _engine.Channels.FirstOrDefault(c => c.Zone == clickedZone);
            if (firstInZone != null)
            {
                SelectChannel(firstInZone);
                _blueprint.SelectedZoneFilter = clickedZone;
                _blueprint.InvalidateVisual();
            }
            else
            {
                // Zone is empty: offer to either assign selected unmapped header or add a new fan group right into this zone
                if (_selectedChannel != null && _selectedChannel.Zone == ChassisZone.Unassigned)
                {
                    _selectedChannel.Zone = clickedZone;
                    _selectedChannel.ForcePinVisible = true;
                    _engine.SaveConfig();
                    RebuildChannelCards();
                    UpdateInspectorPanel();
                    UpdateTelemetryHeader();
                    _blueprint.InvalidateVisual();
                    _statusFooterText.Text = $"Placed [{_selectedChannel.Nickname}] into {clickedZone} zone.";
                }
                else
                {
                    ShowAddFanGroupDialog(clickedZone);
                }
            }
        }

        private void RebuildChannelCards()
        {
            _cardsContainer.Children.Clear();
            _cardViews.Clear();

            var filtered = _engine.Channels.Where(c =>
            {
                if (_activeZoneFilter.HasValue && c.Zone != _activeZoneFilter.Value)
                    return false;
                if (!_engine.Config.ShowEmptyHeaders && !c.IsSpinning && !c.ForcePinVisible && !c.IsPulsing)
                    return false;
                return true;
            }).ToList();

            if (filtered.Count == 0)
            {
                filtered = _engine.Channels.ToList();
            }

            foreach (var ch in filtered)
            {
                var cardView = new ChannelCardView(ch, _engine, this);
                cardView.SetSelected(ch == _selectedChannel);
                _cardViews.Add(cardView);
                _cardsContainer.Children.Add(cardView.RootElement);
            }
        }

        private void UpdateTelemetryHeader()
        {
            _cpuTempText.Text = $"{_engine.CpuTempC:0.0} °C";
            _gpuTempText.Text = $"{_engine.GpuCoreTempC:0.0} °C";
            _vramTempText.Text = $"{_engine.GpuMemTempC:0.0} °C";

            var activeChannels = _engine.Channels.Where(c => c.IsSpinning || c.ForcePinVisible).ToList();
            int totalPhysicalFans = activeChannels.Sum(c => c.PhysicalFanCount);
            int activeGroups = activeChannels.Count;
            _totalFansText.Text = $"{totalPhysicalFans} FANS ({activeGroups} ZONES)";

            if (_selectedChannel != null)
            {
                _selectedChannelSubText.Text = $"{_selectedChannel.ZoneLabel}  •  {_selectedChannel.TopologyLabel}  •  {_selectedChannel.CurrentRpm:N0} RPM ({_selectedChannel.CurrentDutyPercent:0}% PWM)";
            }
        }

        private void UpdateHeaderToggleButtons()
        {
            _btnShowEmpty.Content = _engine.Config.ShowEmptyHeaders ? "◉ All Headers Shown" : "◎ Hide 0-RPM Headers";
            _btnShowEmpty.BorderBrush = _engine.Config.ShowEmptyHeaders ? StudioTheme.AccentCyan : StudioTheme.BorderBrush;

            _btnStartWindows.Content = _engine.Config.StartWithWindows ? "✓ Start with Windows" : "Start with Windows: Off";
            _btnStartWindows.BorderBrush = _engine.Config.StartWithWindows ? StudioTheme.AccentGreen : StudioTheme.BorderBrush;
            _btnStartWindows.Foreground = _engine.Config.StartWithWindows ? StudioTheme.AccentGreen : StudioTheme.TextMuted;
        }

        private void UpdateGlobalPresetButtons()
        {
            foreach (var kv in _globalModeButtons)
            {
                bool active = _engine.Config.GlobalPreset == kv.Key;
                kv.Value.Background = active ? StudioTheme.AccentBlue : StudioTheme.BgCard;
                kv.Value.Foreground = active ? Brushes.White : StudioTheme.TextPrimary;
                kv.Value.BorderBrush = active ? StudioTheme.AccentBlue : StudioTheme.BorderBrush;
            }
        }

        private void AddGlobalPresetButton(Panel parent, FanControlMode mode, string label)
        {
            var btn = new Button
            {
                Content = label,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(5, 0, 0, 0),
                Cursor = Cursors.Hand,
                Background = StudioTheme.BgCard,
                Foreground = StudioTheme.TextPrimary,
                BorderBrush = StudioTheme.BorderBrush,
                BorderThickness = new Thickness(1)
            };
            btn.Click += (s, e) =>
            {
                _engine.ApplyGlobalPreset(mode);
                UpdateGlobalPresetButtons();
                foreach (var cv in _cardViews) cv.RefreshTelemetry();
                _statusFooterText.Text = $"Applied Global Acoustic Profile: {label}";
            };
            _globalModeButtons[mode] = btn;
            parent.Children.Add(btn);
        }

        private Button CreateFilterChip(string title, ChassisZone? zone)
        {
            var btn = new Button
            {
                Content = title,
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(7, 3, 7, 3),
                Margin = new Thickness(0, 0, 4, 0),
                Cursor = Cursors.Hand,
                Background = StudioTheme.BgCard,
                Foreground = StudioTheme.TextPrimary,
                BorderBrush = StudioTheme.BorderBrush,
                BorderThickness = new Thickness(1)
            };
            btn.Click += (s, e) =>
            {
                _activeZoneFilter = zone;
                _blueprint.SelectedZoneFilter = zone;
                _blueprint.InvalidateVisual();
                RebuildChannelCards();
            };
            return btn;
        }

        private Button CreateZoneTagButton(ChassisZone zone)
        {
            string shortLabel = zone == ChassisZone.CpuAio ? "CPU/AIO" : zone.ToString();
            var btn = new Button
            {
                Content = shortLabel,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 5, 0),
                Cursor = Cursors.Hand,
                Background = StudioTheme.BgCard,
                Foreground = StudioTheme.TextPrimary,
                BorderBrush = StudioTheme.BorderBrush,
                BorderThickness = new Thickness(1)
            };
            btn.Click += (s, e) =>
            {
                if (_selectedChannel != null)
                {
                    _selectedChannel.Zone = zone;
                    _selectedChannel.ForcePinVisible = true;
                    UpdateInspectorPanel();
                    foreach (var cv in _cardViews) cv.RefreshTelemetry();
                    _blueprint.InvalidateVisual();
                    _engine.SaveConfig();
                }
            };
            return btn;
        }

        private static Button CreateMiniOptionButton(string text)
        {
            return new Button
            {
                Content = text,
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 4, 0),
                Cursor = Cursors.Hand,
                Background = StudioTheme.BgCard,
                Foreground = StudioTheme.TextPrimary,
                BorderBrush = StudioTheme.BorderBrush,
                BorderThickness = new Thickness(1)
            };
        }

        private static Button CreateHeaderButton(string text, bool active, Brush accent)
        {
            return new Button
            {
                Content = text,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(8, 0, 0, 0),
                Cursor = Cursors.Hand,
                Background = StudioTheme.BgCard,
                Foreground = active ? accent : StudioTheme.TextPrimary,
                BorderBrush = active ? accent : StudioTheme.BorderBrush,
                BorderThickness = new Thickness(1)
            };
        }

        private static Button CreateSmallActionButton(string text, Brush accent)
        {
            return new Button
            {
                Content = text,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(10, 5, 10, 5),
                Cursor = Cursors.Hand,
                Background = StudioTheme.BgCard,
                Foreground = accent,
                BorderBrush = accent,
                BorderThickness = new Thickness(1)
            };
        }

        private static Border CreateTelemetryMetricBox(string label, out TextBlock valueBlock, Brush valueBrush)
        {
            var border = new Border
            {
                Background = StudioTheme.BgCard,
                BorderBrush = StudioTheme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(0, 0, 10, 0)
            };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = StudioTheme.TextMuted
            });
            valueBlock = new TextBlock
            {
                Text = "--",
                FontSize = 15.5,
                FontFamily = new FontFamily("Cascadia Mono, Consolas"),
                FontWeight = FontWeights.Bold,
                Foreground = valueBrush
            };
            stack.Children.Add(valueBlock);
            border.Child = stack;
            return border;
        }

        private void ShowAddFanGroupDialog(ChassisZone defaultZone = ChassisZone.Bottom)
        {
            var dlg = new Window
            {
                Title = "Add Chassis Fan Group to 3D Studio",
                Width = 460,
                Height = 320,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = StudioTheme.BgPanel,
                Foreground = StudioTheme.TextPrimary,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };

            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock
            {
                Text = "Add Installed Fans to Your 3D Chassis",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = StudioTheme.TextPrimary,
                Margin = new Thickness(0, 0, 0, 4)
            });
            stack.Children.Add(new TextBlock
            {
                Text = "Select where the fans are mounted and how many fans are daisy-chained in that zone:",
                FontSize = 11.5,
                Foreground = StudioTheme.TextMuted,
                Margin = new Thickness(0, 0, 0, 14)
            });

            // Zone selector
            var zoneRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            zoneRow.Children.Add(new TextBlock { Text = "Case Zone:", Width = 95, VerticalAlignment = VerticalAlignment.Center, Foreground = StudioTheme.TextMuted });
            var zoneCombo = new ComboBox
            {
                Width = 280,
                Height = 28,
                FontSize = 12
            };
            var zoneItems = new[] { ChassisZone.Top, ChassisZone.Side, ChassisZone.Bottom, ChassisZone.Rear, ChassisZone.CpuAio, ChassisZone.Gpu };
            foreach (var z in zoneItems)
                zoneCombo.Items.Add(z);
            zoneCombo.SelectedItem = defaultZone == ChassisZone.Unassigned ? ChassisZone.Bottom : defaultZone;
            zoneRow.Children.Add(zoneCombo);
            stack.Children.Add(zoneRow);

            // Count & Size selector
            var countRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            countRow.Children.Add(new TextBlock { Text = "Fan Count:", Width = 95, VerticalAlignment = VerticalAlignment.Center, Foreground = StudioTheme.TextMuted });
            var countCombo = new ComboBox { Width = 110, Height = 28, FontSize = 12, Margin = new Thickness(0, 0, 16, 0) };
            for (int i = 1; i <= 4; i++) countCombo.Items.Add($"{i}x Fans");
            countCombo.SelectedIndex = (defaultZone == ChassisZone.Rear) ? 0 : 2; // Default 3x for Top/Side/Bottom, 1x for Rear
            countRow.Children.Add(countCombo);

            countRow.Children.Add(new TextBlock { Text = "Size:", Width = 44, VerticalAlignment = VerticalAlignment.Center, Foreground = StudioTheme.TextMuted });
            var sizeCombo = new ComboBox { Width = 110, Height = 28, FontSize = 12 };
            sizeCombo.Items.Add("120 mm");
            sizeCombo.Items.Add("140 mm");
            sizeCombo.SelectedIndex = 0;
            countRow.Children.Add(sizeCombo);
            stack.Children.Add(countRow);

            // Name input
            var nameRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 18) };
            nameRow.Children.Add(new TextBlock { Text = "Group Name:", Width = 95, VerticalAlignment = VerticalAlignment.Center, Foreground = StudioTheme.TextMuted });
            var nameBox = new TextBox
            {
                Text = $"{zoneCombo.SelectedItem} ARGB Fan Bank",
                Width = 280,
                Height = 28,
                FontSize = 12,
                Padding = new Thickness(6, 4, 6, 4),
                Background = StudioTheme.BgCard,
                Foreground = StudioTheme.TextPrimary,
                BorderBrush = StudioTheme.BorderBrush
            };
            zoneCombo.SelectionChanged += (s, e) =>
            {
                if (zoneCombo.SelectedItem is ChassisZone cz)
                {
                    nameBox.Text = $"{cz} ARGB Fan Bank";
                    countCombo.SelectedIndex = (cz == ChassisZone.Rear) ? 0 : 2;
                }
            };
            nameRow.Children.Add(nameBox);
            stack.Children.Add(nameRow);

            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnCancel = CreateHeaderButton("Cancel", false, StudioTheme.TextMuted);
            btnCancel.Click += (s, e) => dlg.Close();
            btnRow.Children.Add(btnCancel);

            var btnAdd = CreateHeaderButton("＋ Place Fans in 3D Case", true, StudioTheme.AccentBlue);
            btnAdd.Background = StudioTheme.AccentBlue;
            btnAdd.Foreground = Brushes.White;
            btnAdd.Click += (s, e) =>
            {
                ChassisZone chosenZone = (zoneCombo.SelectedItem is ChassisZone z) ? z : ChassisZone.Bottom;
                int count = countCombo.SelectedIndex + 1;
                int sizeMm = sizeCombo.SelectedIndex == 1 ? 140 : 120;
                var created = _engine.AddCustomFanGroup(nameBox.Text, chosenZone, count, sizeMm);
                RebuildChannelCards();
                SelectChannel(created);
                UpdateTelemetryHeader();
                _blueprint.InvalidateVisual();
                _statusFooterText.Text = $"Added {count}× {sizeMm}mm fans to {chosenZone} zone.";
                dlg.Close();
            };
            btnRow.Children.Add(btnAdd);
            stack.Children.Add(btnRow);

            dlg.Content = stack;
            dlg.ShowDialog();
        }

        private void ShowCaseCatalogDialog()
        {
            var dlg = new Window
            {
                Title = "3D PC Case Studio Catalog & Downloader",
                Width = 560,
                Height = 460,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = StudioTheme.BgPanel,
                Foreground = StudioTheme.TextPrimary,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };

            var root = new DockPanel { Margin = new Thickness(20) };

            var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
            header.Children.Add(new TextBlock
            {
                Text = "Select or Pull Any 3D PC Case Model",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = StudioTheme.TextPrimary
            });
            header.Children.Add(new TextBlock
            {
                Text = "Pick a studio-calibrated 3D chassis below, or type any PC case name to pull & generate its 3D view:",
                FontSize = 11.5,
                Foreground = StudioTheme.TextMuted,
                Margin = new Thickness(0, 3, 0, 0)
            });
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);

            // Search / Custom Pull bar at bottom
            var searchBar = new DockPanel { Margin = new Thickness(0, 12, 0, 0) };
            var tbCustom = new TextBox
            {
                Text = _blueprint.CurrentCaseModel.DisplayName.Split('(')[0].Trim(),
                Height = 32,
                FontSize = 12.5,
                Padding = new Thickness(8, 5, 8, 5),
                Background = StudioTheme.BgCard,
                Foreground = StudioTheme.TextPrimary,
                BorderBrush = StudioTheme.BorderBrush,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            var btnPullCustom = CreateHeaderButton("🌐 Pull & Apply 3D Case", true, StudioTheme.AccentBlue);
            btnPullCustom.Background = StudioTheme.AccentBlue;
            btnPullCustom.Foreground = Brushes.White;
            btnPullCustom.Click += async (s, e) =>
            {
                string query = tbCustom.Text;
                if (!string.IsNullOrWhiteSpace(query))
                {
                    btnPullCustom.Content = "⏳ Building 3D...";
                    var model = await CaseCatalog.DownloadCaseRenderAsync(query);
                    ApplySelectedCaseModel(model);
                    dlg.Close();
                }
            };
            DockPanel.SetDock(btnPullCustom, Dock.Right);
            searchBar.Children.Add(btnPullCustom);
            searchBar.Children.Add(tbCustom);
            DockPanel.SetDock(searchBar, Dock.Bottom);
            root.Children.Add(searchBar);

            // Preset list
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var listStack = new StackPanel();
            foreach (var preset in CaseCatalog.Presets)
            {
                var pCopy = preset;
                bool isCurrent = pCopy.Id == _blueprint.CurrentCaseModel.Id;
                var itemBorder = new Border
                {
                    Background = isCurrent ? StudioTheme.BgCardHover : StudioTheme.BgCard,
                    BorderBrush = isCurrent ? StudioTheme.AccentBlue : StudioTheme.BorderSubtle,
                    BorderThickness = new Thickness(1.2),
                    CornerRadius = new CornerRadius(9),
                    Padding = new Thickness(12, 9, 12, 9),
                    Margin = new Thickness(0, 0, 0, 7),
                    Cursor = Cursors.Hand
                };

                var itemDock = new DockPanel();
                var textStack = new StackPanel();
                textStack.Children.Add(new TextBlock
                {
                    Text = pCopy.DisplayName,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = StudioTheme.TextPrimary
                });
                textStack.Children.Add(new TextBlock
                {
                    Text = $"{pCopy.FormFactor}  •  Supports up to {pCopy.MaxTopFans + pCopy.MaxSideFans + pCopy.MaxBottomFans + pCopy.MaxRearFans} Fans ({pCopy.MaxTopFans} Top, {pCopy.MaxSideFans} Side, {pCopy.MaxBottomFans} Bottom, {pCopy.MaxRearFans} Rear)",
                    FontSize = 10.5,
                    Foreground = StudioTheme.TextMuted
                });
                DockPanel.SetDock(textStack, Dock.Left);
                itemDock.Children.Add(textStack);

                var btnSelect = CreateSmallActionButton(isCurrent ? "✓ Active" : "Apply 3D", isCurrent ? StudioTheme.AccentGreen : StudioTheme.AccentCyan);
                btnSelect.VerticalAlignment = VerticalAlignment.Center;
                btnSelect.Click += (s, e) =>
                {
                    ApplySelectedCaseModel(pCopy);
                    dlg.Close();
                };
                DockPanel.SetDock(btnSelect, Dock.Right);
                itemDock.Children.Add(btnSelect);

                itemBorder.Child = itemDock;
                itemBorder.MouseLeftButtonDown += (s, e) =>
                {
                    ApplySelectedCaseModel(pCopy);
                    dlg.Close();
                };
                listStack.Children.Add(itemBorder);
            }
            scroll.Content = listStack;
            root.Children.Add(scroll);

            dlg.Content = root;
            dlg.ShowDialog();
        }

        private void SetupSystemTray()
        {
            try
            {
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hyteflow.ico");
                var trayIco = File.Exists(icoPath)
                    ? new System.Drawing.Icon(icoPath)
                    : System.Drawing.SystemIcons.Application;

                _trayIcon = new System.Windows.Forms.NotifyIcon
                {
                    Text = "HyteFlow Studio — Universal 3D Fan Control",
                    Icon = trayIco,
                    Visible = true
                };
                var menu = new System.Windows.Forms.ContextMenuStrip();
                menu.Items.Add("Open HyteFlow Studio", null, (s, e) => { Show(); WindowState = WindowState.Normal; Activate(); });
                menu.Items.Add("-");
                menu.Items.Add("Profile: Smart Curves", null, (s, e) => Dispatcher.Invoke(() => _engine.ApplyGlobalPreset(FanControlMode.SmartCurve)));
                menu.Items.Add("Profile: Silent", null, (s, e) => Dispatcher.Invoke(() => _engine.ApplyGlobalPreset(FanControlMode.Silent)));
                menu.Items.Add("Profile: Balanced", null, (s, e) => Dispatcher.Invoke(() => _engine.ApplyGlobalPreset(FanControlMode.Balanced)));
                menu.Items.Add("Profile: Max Blast", null, (s, e) => Dispatcher.Invoke(() => _engine.ApplyGlobalPreset(FanControlMode.FullBlast)));
                menu.Items.Add("-");
                menu.Items.Add("Exit HyteFlow", null, (s, e) => Dispatcher.Invoke(Close));
                _trayIcon.ContextMenuStrip = menu;
                _trayIcon.DoubleClick += (s, e) =>
                {
                    Show();
                    WindowState = WindowState.Normal;
                    Activate();
                };
            }
            catch { }
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _engine.SaveConfig();
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
            _telemetryTimer?.Stop();
            _animTimer?.Stop();
            _engine.Dispose();
        }

        public void NotifyCardsRefresh()
        {
            UpdateInspectorPanel();
            UpdateTelemetryHeader();
            _blueprint.InvalidateVisual();
        }
    }

    public class ChannelCardView
    {
        public FanChannelModel Channel { get; }
        public Border RootElement { get; }

        private readonly HardwareEngine _engine;
        private readonly MainWindow _window;
        private readonly SpinningFanControl _rotor;
        private readonly TextBlock _nameText;
        private readonly TextBlock _originText;
        private readonly TextBlock _zoneBadgeText;
        private readonly Border _zoneBadgeBorder;
        private readonly TextBlock _rpmText;
        private readonly TextBlock _dutyText;
        private readonly Slider _dutySlider;
        private readonly Button _locateBtn;
        private readonly Button _zoneQuickBtn;
        private readonly Button _fanCountQuickBtn;
        private bool _suppressSliderEvent;

        private static readonly ChassisZone[] AllZones = new[]
        {
            ChassisZone.Top, ChassisZone.Side, ChassisZone.Bottom,
            ChassisZone.Rear, ChassisZone.CpuAio, ChassisZone.Gpu, ChassisZone.Unassigned
        };

        public ChannelCardView(FanChannelModel channel, HardwareEngine engine, MainWindow window)
        {
            Channel = channel;
            _engine = engine;
            _window = window;

            RootElement = new Border
            {
                Background = StudioTheme.BgCard,
                BorderBrush = StudioTheme.BorderSubtle,
                BorderThickness = new Thickness(1.4),
                CornerRadius = new CornerRadius(11),
                Padding = new Thickness(14, 11, 14, 11),
                Margin = new Thickness(0, 0, 0, 10),
                Cursor = Cursors.Hand
            };
            RootElement.MouseLeftButtonDown += (s, e) => _window.SelectChannel(Channel);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(225) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(136) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(116) });

            _rotor = new SpinningFanControl
            {
                Rpm = Channel.CurrentRpm,
                DutyPercent = Channel.CurrentDutyPercent,
                IsPulsing = Channel.IsPulsing,
                IsIntake = Channel.Zone == ChassisZone.Side || Channel.Zone == ChassisZone.Bottom || Channel.Zone == ChassisZone.Gpu
            };
            Grid.SetColumn(_rotor, 0);
            grid.Children.Add(_rotor);

            var idStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 10, 0) };
            _nameText = new TextBlock
            {
                Text = Channel.Nickname,
                FontSize = 13.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = StudioTheme.TextPrimary,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            _originText = new TextBlock
            {
                Text = $"{Channel.HardwareOrigin}  •  {Channel.TopologyLabel}",
                FontSize = 11,
                Foreground = StudioTheme.TextMuted,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 1, 0, 5)
            };

            bool isMapped = Channel.Zone != ChassisZone.Unassigned;
            _zoneBadgeBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(7, 2, 7, 2),
                Background = isMapped
                    ? new SolidColorBrush(Color.FromArgb(36, 10, 132, 255))
                    : new SolidColorBrush(Color.FromArgb(36, 255, 159, 10)),
                BorderBrush = isMapped ? StudioTheme.AccentBlue : StudioTheme.AccentOrange,
                BorderThickness = new Thickness(1)
            };
            _zoneBadgeText = new TextBlock
            {
                Text = $"{Channel.ZoneLabel} ({Channel.AirflowRole})",
                FontSize = 9.5,
                FontFamily = new FontFamily("Cascadia Mono, Consolas"),
                FontWeight = FontWeights.SemiBold,
                Foreground = isMapped ? StudioTheme.AccentCyan : StudioTheme.AccentOrange
            };
            _zoneBadgeBorder.Child = _zoneBadgeText;

            idStack.Children.Add(_nameText);
            idStack.Children.Add(_originText);
            idStack.Children.Add(_zoneBadgeBorder);
            Grid.SetColumn(idStack, 1);
            grid.Children.Add(idStack);

            var rpmStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            _rpmText = new TextBlock
            {
                Text = Channel.StatusText,
                FontSize = 16,
                FontFamily = new FontFamily("Cascadia Mono, Consolas"),
                FontWeight = FontWeights.Bold,
                Foreground = Channel.IsSpinning ? StudioTheme.AccentGreen : StudioTheme.TextDim
            };
            _dutyText = new TextBlock
            {
                Text = $"{Channel.CurrentDutyPercent:0}% PWM  •  {Channel.ModeLabel}",
                FontSize = 11,
                Foreground = StudioTheme.TextMuted
            };
            rpmStack.Children.Add(_rpmText);
            rpmStack.Children.Add(_dutyText);
            Grid.SetColumn(rpmStack, 2);
            grid.Children.Add(rpmStack);

            var ctrlStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 12, 0) };
            var sliderHeader = new Grid { Margin = new Thickness(0, 0, 0, 5) };
            sliderHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sliderHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var lblDuty = new TextBlock
            {
                Text = "PWM / Zone:",
                FontSize = 10.5,
                Foreground = StudioTheme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lblDuty, 0);
            sliderHeader.Children.Add(lblDuty);

            var pillRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _fanCountQuickBtn = new Button
            {
                Width = 34,
                Height = 20,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 4, 0),
                Cursor = Cursors.Hand,
                Background = StudioTheme.BgPill,
                Foreground = StudioTheme.AccentCyan,
                BorderBrush = StudioTheme.BorderBrush,
                BorderThickness = new Thickness(1),
                Content = $"{Channel.PhysicalFanCount}×",
                ToolTip = "Click to cycle number of physical fans (1x–4x) on this header"
            };
            _fanCountQuickBtn.Click += (s, e) =>
            {
                e.Handled = true;
                Channel.PhysicalFanCount = (Channel.PhysicalFanCount % 4) + 1;
                RefreshTelemetry();
                _window.SelectChannel(Channel);
                _window.NotifyCardsRefresh();
                _engine.SaveConfig();
            };
            pillRow.Children.Add(_fanCountQuickBtn);

            _zoneQuickBtn = new Button
            {
                Width = 80,
                Height = 20,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                Background = StudioTheme.BgPill,
                Foreground = StudioTheme.TextPrimary,
                BorderBrush = StudioTheme.BorderBrush,
                BorderThickness = new Thickness(1),
                Content = $"{(Channel.Zone == ChassisZone.CpuAio ? "CPU/AIO" : Channel.Zone.ToString())} ▾"
            };
            _zoneQuickBtn.Click += (s, e) =>
            {
                e.Handled = true;
                int idx = Array.IndexOf(AllZones, Channel.Zone);
                Channel.Zone = AllZones[(idx + 1) % AllZones.Length];
                Channel.ForcePinVisible = true;
                RefreshTelemetry();
                _window.SelectChannel(Channel);
                _window.NotifyCardsRefresh();
                _engine.SaveConfig();
            };
            pillRow.Children.Add(_zoneQuickBtn);

            Grid.SetColumn(pillRow, 1);
            sliderHeader.Children.Add(pillRow);
            ctrlStack.Children.Add(sliderHeader);

            _dutySlider = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                Value = Channel.CurrentDutyPercent,
                IsSnapToTickEnabled = true,
                TickFrequency = 1
            };
            _dutySlider.ValueChanged += (s, e) =>
            {
                if (_suppressSliderEvent) return;
                Channel.ManualDutyPercent = _dutySlider.Value;
                Channel.Mode = FanControlMode.ManualFixed;
                _window.SelectChannel(Channel);
                _engine.UpdateAll(0.1);
                RefreshTelemetry();
            };
            ctrlStack.Children.Add(_dutySlider);
            Grid.SetColumn(ctrlStack, 3);
            grid.Children.Add(ctrlStack);

            _locateBtn = new Button
            {
                Content = "⚡ Locate 100%",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Height = 34,
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(Color.FromRgb(18, 28, 42)),
                Foreground = StudioTheme.AccentCyan,
                BorderBrush = StudioTheme.AccentBlue,
                BorderThickness = new Thickness(1)
            };
            _locateBtn.Click += (s, e) =>
            {
                e.Handled = true;
                _window.SelectChannel(Channel);
                _engine.TriggerPulseLocate(Channel, 6);
                RefreshTelemetry();
            };
            Grid.SetColumn(_locateBtn, 4);
            grid.Children.Add(_locateBtn);

            RootElement.Child = grid;
        }

        public void SetSelected(bool selected)
        {
            if (Channel.IsPulsing)
            {
                RootElement.BorderBrush = StudioTheme.AccentCyan;
                return;
            }
            RootElement.BorderBrush = selected ? StudioTheme.AccentBlue : StudioTheme.BorderSubtle;
            RootElement.Background = selected ? StudioTheme.BgCardHover : StudioTheme.BgCard;
        }

        public void TickAnimation()
        {
            if (Channel.IsSpinning || Channel.IsPulsing)
            {
                double speedDegPerFrame = Math.Max(3.0, (Channel.CurrentRpm / 2000.0) * 18.0);
                if (Channel.IsPulsing) speedDegPerFrame = 24.0;
                _rotor.Angle = (_rotor.Angle + speedDegPerFrame) % 360.0;
                _rotor.InvalidateVisual();
            }
        }

        public void RefreshTelemetry()
        {
            _nameText.Text = Channel.Nickname;
            _originText.Text = $"{Channel.HardwareOrigin}  •  {Channel.TopologyLabel}";
            _zoneBadgeText.Text = $"{Channel.ZoneLabel} ({Channel.AirflowRole})";

            bool isMapped = Channel.Zone != ChassisZone.Unassigned;
            _zoneBadgeBorder.Background = isMapped
                ? new SolidColorBrush(Color.FromArgb(36, 10, 132, 255))
                : new SolidColorBrush(Color.FromArgb(36, 255, 159, 10));
            _zoneBadgeBorder.BorderBrush = isMapped ? StudioTheme.AccentBlue : StudioTheme.AccentOrange;
            _zoneBadgeText.Foreground = isMapped ? StudioTheme.AccentCyan : StudioTheme.AccentOrange;

            _rpmText.Text = Channel.StatusText;
            _rpmText.Foreground = Channel.IsPulsing ? StudioTheme.AccentCyan :
                                  (Channel.IsSpinning ? StudioTheme.AccentGreen : StudioTheme.TextDim);

            _dutyText.Text = $"{Channel.CurrentDutyPercent:0}% PWM  •  {Channel.ModeLabel}";

            _suppressSliderEvent = true;
            if (!_dutySlider.IsMouseCaptureWithin)
                _dutySlider.Value = Channel.CurrentDutyPercent;
            _zoneQuickBtn.Content = $"{(Channel.Zone == ChassisZone.CpuAio ? "CPU/AIO" : Channel.Zone.ToString())} ▾";
            _fanCountQuickBtn.Content = $"{Channel.PhysicalFanCount}×";
            _suppressSliderEvent = false;

            _rotor.Rpm = Channel.CurrentRpm;
            _rotor.DutyPercent = Channel.CurrentDutyPercent;
            _rotor.IsPulsing = Channel.IsPulsing;
            _rotor.IsIntake = Channel.Zone == ChassisZone.Side || Channel.Zone == ChassisZone.Bottom || Channel.Zone == ChassisZone.Gpu;
            _rotor.InvalidateVisual();

            if (Channel.IsPulsing)
            {
                _locateBtn.Content = $"Locating ({Channel.PulseSecondsRemaining}s)";
                _locateBtn.Background = StudioTheme.AccentCyan;
                _locateBtn.Foreground = Brushes.Black;
                RootElement.BorderBrush = StudioTheme.AccentCyan;
            }
            else
            {
                _locateBtn.Content = "⚡ Locate 100%";
                _locateBtn.Background = new SolidColorBrush(Color.FromRgb(18, 28, 42));
                _locateBtn.Foreground = StudioTheme.AccentCyan;
            }
        }
    }
}
