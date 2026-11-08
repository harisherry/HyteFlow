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

        private TextBlock _cpuTempText;
        private TextBlock _gpuTempText;
        private TextBlock _vramTempText;
        private TextBlock _totalFansText;
        private TextBlock _statusFooterText;
        private TextBlock _selectedChannelHeaderText;
        private TextBlock _selectedChannelSubText;

        private Button _btnStartWindows;
        private Button _btnShowEmpty;
        private Button _tempSourceBtn;
        private readonly Dictionary<ChassisZone, Button> _zoneAssignButtons = new Dictionary<ChassisZone, Button>();
        private readonly Dictionary<int, Button> _fanCountButtons = new Dictionary<int, Button>();
        private readonly Dictionary<FanControlMode, Button> _globalModeButtons = new Dictionary<FanControlMode, Button>();

        private FanChannelModel _selectedChannel;
        private ChassisZone? _activeZoneFilter = null;
        private readonly List<ChannelCardView> _cardViews = new List<ChannelCardView>();
        private System.Windows.Forms.NotifyIcon _trayIcon;

        public MainWindow(HardwareEngine engine, bool startMinimized)
        {
            _engine = engine;

            Title = "HyteFlow — Universal Fan & Chassis Telemetry Studio";
            Width = 1360;
            Height = 860;
            MinWidth = 1120;
            MinHeight = 720;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = StudioTheme.BgCanvas;
            Foreground = StudioTheme.TextPrimary;
            FontFamily = new FontFamily("Segoe UI");

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

        private void BuildUserInterface()
        {
            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });

            var topBar = new Border
            {
                Background = StudioTheme.BgPanel,
                BorderBrush = StudioTheme.BorderBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(20, 0, 20, 0)
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
                        Width = 40,
                        Height = 40,
                        Margin = new Thickness(0, 0, 12, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                    brandStack.Children.Add(img);
                }
                catch { }
            }
            else
            {
                var accentBar = new Border
                {
                    Width = 5,
                    Height = 28,
                    CornerRadius = new CornerRadius(2),
                    Background = StudioTheme.AccentOrange,
                    Margin = new Thickness(0, 0, 12, 0)
                };
                brandStack.Children.Add(accentBar);
            }

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var appTitle = new TextBlock
            {
                Text = "HYTEFLOW STUDIO",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = StudioTheme.TextPrimary
            };
            var hwSubtitle = new TextBlock
            {
                Text = $"HYTE Y70  •  {_engine.MotherboardName.ToUpperInvariant()}  •  {_engine.SuperIoChipName.ToUpperInvariant()}  •  CORSAIR COMMANDER PRO",
                FontSize = 10.5,
                FontFamily = new FontFamily("Consolas"),
                Foreground = StudioTheme.TextMuted
            };
            titleStack.Children.Add(appTitle);
            titleStack.Children.Add(hwSubtitle);
            brandStack.Children.Add(titleStack);
            DockPanel.SetDock(brandStack, Dock.Left);
            topDock.Children.Add(brandStack);

            var actionsStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            if (!_engine.IsAdmin)
            {
                var btnAdmin = CreateHeaderButton("⚡ UNLOCK SUPER I/O (ADMIN)", true, StudioTheme.AccentAmber);
                btnAdmin.ToolTip = "Restart HyteFlow with Administrator privileges so PawnIO can directly control all 6 Gigabyte motherboard headers.";
                btnAdmin.Click += (s, e) => StartupManager.RestartAsAdmin();
                actionsStack.Children.Add(btnAdmin);
            }

            _btnShowEmpty = CreateHeaderButton(
                _engine.Config.ShowEmptyHeaders ? "◉ ALL 14 HEADERS SHOWN" : "◎ HIDE 0-RPM HEADERS",
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
                _engine.Config.StartWithWindows ? "✓ START WITH WINDOWS: ON" : "START WITH WINDOWS: OFF",
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

            var btnFixLegacy = CreateHeaderButton("🔧 FIX FANCONTROL", false, StudioTheme.TextMuted);
            btnFixLegacy.Click += (s, e) =>
            {
                HardwareEngine.RepairLegacyFanControlConfig(out string rep);
                _statusFooterText.Text = rep;
                MessageBox.Show(rep, "Legacy FanControl Config Repaired", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            actionsStack.Children.Add(btnFixLegacy);

            DockPanel.SetDock(actionsStack, Dock.Right);
            topDock.Children.Add(actionsStack);
            topBar.Child = topDock;
            Grid.SetRow(topBar, 0);
            rootGrid.Children.Add(topBar);

            var stripBar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(14, 17, 22)),
                BorderBrush = StudioTheme.BorderBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(20, 0, 20, 0)
            };
            var stripDock = new DockPanel { LastChildFill = false };

            var metricsStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            metricsStack.Children.Add(CreateTelemetryMetricBox("CPU PACKAGE (" + ShortCpuName(_engine.CpuName) + ")", out _cpuTempText, StudioTheme.AccentOrange));
            metricsStack.Children.Add(CreateTelemetryMetricBox("GPU CORE (" + ShortGpuName(_engine.GpuName) + ")", out _gpuTempText, StudioTheme.AccentCyan));
            metricsStack.Children.Add(CreateTelemetryMetricBox("RTX 5080 VRAM JUNCTION", out _vramTempText, StudioTheme.AccentGreen));
            metricsStack.Children.Add(CreateTelemetryMetricBox("INSTALLED TOPOLOGY", out _totalFansText, StudioTheme.TextPrimary));
            DockPanel.SetDock(metricsStack, Dock.Left);
            stripDock.Children.Add(metricsStack);

            var presetStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            presetStack.Children.Add(new TextBlock
            {
                Text = "GLOBAL PROFILE:",
                FontSize = 11,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Foreground = StudioTheme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            });

            AddGlobalPresetButton(presetStack, FanControlMode.SmartCurve, "SMART CURVES");
            AddGlobalPresetButton(presetStack, FanControlMode.Silent, "SILENT");
            AddGlobalPresetButton(presetStack, FanControlMode.Balanced, "BALANCED");
            AddGlobalPresetButton(presetStack, FanControlMode.Performance, "PERFORMANCE");
            AddGlobalPresetButton(presetStack, FanControlMode.FullBlast, "100% MAX BLAST");

            DockPanel.SetDock(presetStack, Dock.Right);
            stripDock.Children.Add(presetStack);
            stripBar.Child = stripDock;
            Grid.SetRow(stripBar, 1);
            rootGrid.Children.Add(stripBar);

            var mainGrid = new Grid { Margin = new Thickness(18, 14, 18, 14) };
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(530) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var leftGrid = new Grid();
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.05, GridUnitType.Star) });
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

            var blueprintCard = new Border
            {
                Background = StudioTheme.BgPanel,
                BorderBrush = StudioTheme.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12)
            };
            var bpDock = new DockPanel();

            var topBpBar = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
            var filterWrap = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
            filterWrap.Children.Add(CreateFilterChip("ALL", null));
            filterWrap.Children.Add(CreateFilterChip("TOP", ChassisZone.Top));
            filterWrap.Children.Add(CreateFilterChip("SIDE", ChassisZone.Side));
            filterWrap.Children.Add(CreateFilterChip("BOTTOM", ChassisZone.Bottom));
            filterWrap.Children.Add(CreateFilterChip("REAR", ChassisZone.Rear));
            filterWrap.Children.Add(CreateFilterChip("CPU", ChassisZone.CpuAio));
            filterWrap.Children.Add(CreateFilterChip("GPU", ChassisZone.Gpu));
            DockPanel.SetDock(filterWrap, Dock.Left);
            topBpBar.Children.Add(filterWrap);

            var caseCtrlStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnCaseCycle = new Button
            {
                Content = "3D CASE: HYTE Y70 ▾",
                FontSize = 9.5,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(7, 4, 7, 4),
                Margin = new Thickness(0, 0, 4, 0),
                Cursor = Cursors.Hand,
                Background = StudioTheme.BgCard,
                Foreground = StudioTheme.AccentCyan,
                BorderBrush = StudioTheme.AccentCyan,
                BorderThickness = new Thickness(1)
            };
            btnCaseCycle.Click += (s, e) =>
            {
                int idx = CaseCatalog.Presets.IndexOf(_blueprint.CurrentCaseModel);
                var nextModel = CaseCatalog.Presets[(idx + 1) % CaseCatalog.Presets.Count];
                _blueprint.CurrentCaseModel = nextModel;
                _engine.Config.ChassisPreset = nextModel.Id;
                _engine.SaveConfig();
                string shortCase = nextModel.DisplayName.Split('(')[0].Trim().ToUpperInvariant();
                btnCaseCycle.Content = $"3D CASE: {shortCase} ▾";
                _statusFooterText.Text = $"Switched 3D Chassis Representation to: {nextModel.DisplayName}";
            };
            caseCtrlStack.Children.Add(btnCaseCycle);

            var btnDownloadCase = new Button
            {
                Content = "🌐 GET CASE 3D",
                FontSize = 9.5,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(7, 4, 7, 4),
                Cursor = Cursors.Hand,
                Background = StudioTheme.BgCard,
                Foreground = StudioTheme.AccentOrange,
                BorderBrush = StudioTheme.AccentOrange,
                BorderThickness = new Thickness(1),
                ToolTip = "Type any PC case model name to download its 3D studio representation from the web."
            };
            btnDownloadCase.Click += async (s, e) =>
            {
                string input = PromptForCaseName();
                if (!string.IsNullOrWhiteSpace(input))
                {
                    btnDownloadCase.Content = "⏳ FETCHING 3D...";
                    _statusFooterText.Text = $"Downloading 3D studio render for '{input}'...";
                    var downloaded = await CaseCatalog.DownloadCaseRenderAsync(input);
                    _blueprint.CurrentCaseModel = downloaded;
                    _engine.Config.ChassisPreset = downloaded.Id;
                    _engine.SaveConfig();
                    string shortCase = downloaded.DisplayName.Split('(')[0].Trim().ToUpperInvariant();
                    btnCaseCycle.Content = $"3D CASE: {shortCase} ▾";
                    btnDownloadCase.Content = "🌐 GET CASE 3D";
                    _statusFooterText.Text = $"Loaded 3D Chassis Model: {downloaded.DisplayName}";
                }
            };
            caseCtrlStack.Children.Add(btnDownloadCase);
            DockPanel.SetDock(caseCtrlStack, Dock.Right);
            topBpBar.Children.Add(caseCtrlStack);

            DockPanel.SetDock(topBpBar, Dock.Top);
            bpDock.Children.Add(topBpBar);

            _blueprint = new HyteY70BlueprintControl(_engine);
            _blueprint.ZoneClicked += OnBlueprintZoneClicked;
            bpDock.Children.Add(_blueprint);
            blueprintCard.Child = bpDock;
            Grid.SetRow(blueprintCard, 0);
            leftGrid.Children.Add(blueprintCard);

            var inspectorCard = BuildInspectorCard();
            Grid.SetRow(inspectorCard, 2);
            leftGrid.Children.Add(inspectorCard);

            Grid.SetColumn(leftGrid, 0);
            mainGrid.Children.Add(leftGrid);

            var rightBorder = new Border
            {
                Background = StudioTheme.BgPanel,
                BorderBrush = StudioTheme.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14)
            };
            var rightDock = new DockPanel();

            var rightHeaderDock = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
            var rightTitle = new TextBlock
            {
                Text = "DETECTED FAN CONTROLLERS & CHASSIS HEADERS",
                FontSize = 12,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Foreground = StudioTheme.TextPrimary,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(rightTitle, Dock.Left);
            rightHeaderDock.Children.Add(rightTitle);

            var rightHint = new TextBlock
            {
                Text = "Click [⚡ LOCATE 100%] to pulse & identify fan location",
                FontSize = 11,
                Foreground = StudioTheme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(rightHint, Dock.Right);
            rightHeaderDock.Children.Add(rightHint);

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

            var footerBar = new Border
            {
                Background = StudioTheme.BgPanel,
                BorderBrush = StudioTheme.BorderBrush,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 0, 20, 0)
            };
            var footerDock = new DockPanel();
            _statusFooterText = new TextBlock
            {
                Text = $"Ready  •  Controllers: {string.Join("  |  ", _engine.DetectedUsbControllers)}  •  Motherboard LPC: {_engine.SuperIoChipName}",
                FontSize = 11,
                FontFamily = new FontFamily("Consolas"),
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

        private Border BuildInspectorCard()
        {
            var card = new Border
            {
                Background = StudioTheme.BgPanel,
                BorderBrush = StudioTheme.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14)
            };

            var dock = new DockPanel();

            var headerDock = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            var titleStack = new StackPanel();
            _selectedChannelHeaderText = new TextBlock
            {
                Text = "SELECTED CHANNEL",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = StudioTheme.TextPrimary
            };
            _selectedChannelSubText = new TextBlock
            {
                Text = "Select any fan card on the right to edit its physical case position & thermal curve",
                FontSize = 11,
                FontFamily = new FontFamily("Consolas"),
                Foreground = StudioTheme.AccentOrange
            };
            titleStack.Children.Add(_selectedChannelHeaderText);
            titleStack.Children.Add(_selectedChannelSubText);
            DockPanel.SetDock(titleStack, Dock.Left);
            headerDock.Children.Add(titleStack);

            var btnPulseSelected = CreateSmallActionButton("⚡ PULSE 100% TO LOCATE", StudioTheme.AccentCyan);
            btnPulseSelected.Click += (s, e) =>
            {
                if (_selectedChannel != null)
                {
                    _engine.TriggerPulseLocate(_selectedChannel, 6);
                    _statusFooterText.Text = $"Pulsing {_selectedChannel.Nickname} to 100% for 6s — watch/listen to your Hyte Y70 case and click its position below!";
                }
            };
            DockPanel.SetDock(btnPulseSelected, Dock.Right);
            headerDock.Children.Add(btnPulseSelected);

            DockPanel.SetDock(headerDock, Dock.Top);
            dock.Children.Add(headerDock);

            var locRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 6) };
            locRow.Children.Add(new TextBlock
            {
                Text = "CASE POSITION:",
                Width = 98,
                FontSize = 10.5,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
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
                Text = "FANS ON HEADER:",
                Width = 98,
                FontSize = 10.5,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Foreground = StudioTheme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center
            });
            for (int c = 1; c <= 4; c++)
            {
                int countCopy = c;
                var b = CreateMiniOptionButton($"{c}x");
                b.Click += (s, e) =>
                {
                    if (_selectedChannel != null)
                    {
                        _selectedChannel.PhysicalFanCount = countCopy;
                        UpdateInspectorPanel();
                        UpdateTelemetryHeader();
                        _blueprint.InvalidateVisual();
                        _engine.SaveConfig();
                    }
                };
                _fanCountButtons[c] = b;
                multStack.Children.Add(b);
            }
            DockPanel.SetDock(multStack, Dock.Left);
            configRow.Children.Add(multStack);

            var tempStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            tempStack.Children.Add(new TextBlock
            {
                Text = "SENSOR:",
                FontSize = 10.0,
                FontFamily = new FontFamily("Consolas"),
                Foreground = StudioTheme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 5, 0)
            });
            _tempSourceBtn = new Button
            {
                Content = "Smart Mix (CPU/GPU) ▾",
                Width = 152,
                Height = 23,
                FontSize = 10.0,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
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

            _selectedChannelHeaderText.Text = $"{_selectedChannel.Nickname}  ({_selectedChannel.HeaderName})";
            _selectedChannelSubText.Text = $"{_selectedChannel.ZoneLabel}  •  {_selectedChannel.TopologyLabel}  •  {_selectedChannel.CurrentRpm} RPM ({_selectedChannel.CurrentDutyPercent:0}% PWM)";
            _curveEditor.Channel = _selectedChannel;

            foreach (var kv in _zoneAssignButtons)
            {
                bool active = _selectedChannel.Zone == kv.Key;
                kv.Value.Background = active ? StudioTheme.AccentOrange : StudioTheme.BgCard;
                kv.Value.Foreground = active ? Brushes.Black : StudioTheme.TextPrimary;
                kv.Value.BorderBrush = active ? StudioTheme.AccentOrange : StudioTheme.BorderBrush;
            }

            foreach (var kv in _fanCountButtons)
            {
                bool active = _selectedChannel.PhysicalFanCount == kv.Key;
                kv.Value.Background = active ? StudioTheme.AccentCyan : StudioTheme.BgCard;
                kv.Value.Foreground = active ? Brushes.Black : StudioTheme.TextPrimary;
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
            var pulsing = _engine.Channels.FirstOrDefault(c => c.IsPulsing) ?? _selectedChannel;
            if (pulsing != null && (pulsing.IsPulsing || Keyboard.Modifiers == ModifierKeys.Shift))
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
            else if (_selectedChannel != null)
            {
                _selectedChannel.Zone = clickedZone;
                _engine.SaveConfig();
                UpdateInspectorPanel();
                foreach (var cv in _cardViews) cv.RefreshTelemetry();
                _blueprint.InvalidateVisual();
                _statusFooterText.Text = $"Assigned [{_selectedChannel.Nickname}] to {_selectedChannel.ZoneLabel}.";
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
            int spinningHeaders = _engine.Channels.Count(c => c.IsSpinning);
            _totalFansText.Text = $"{totalPhysicalFans} FANS ({spinningHeaders}/{_engine.Channels.Count} HDRS)";

            if (_selectedChannel != null)
            {
                _selectedChannelSubText.Text = $"{_selectedChannel.ZoneLabel}  •  {_selectedChannel.TopologyLabel}  •  {_selectedChannel.CurrentRpm} RPM ({_selectedChannel.CurrentDutyPercent:0}% PWM)";
            }
        }

        private void UpdateHeaderToggleButtons()
        {
            _btnShowEmpty.Content = _engine.Config.ShowEmptyHeaders ? "◉ ALL 14 HEADERS SHOWN" : "◎ HIDE 0-RPM HEADERS";
            _btnShowEmpty.BorderBrush = _engine.Config.ShowEmptyHeaders ? StudioTheme.AccentCyan : StudioTheme.BorderBrush;

            _btnStartWindows.Content = _engine.Config.StartWithWindows ? "✓ START WITH WINDOWS: ON" : "START WITH WINDOWS: OFF";
            _btnStartWindows.BorderBrush = _engine.Config.StartWithWindows ? StudioTheme.AccentGreen : StudioTheme.BorderBrush;
            _btnStartWindows.Foreground = _engine.Config.StartWithWindows ? StudioTheme.AccentGreen : StudioTheme.TextMuted;
        }

        private void UpdateGlobalPresetButtons()
        {
            foreach (var kv in _globalModeButtons)
            {
                bool active = _engine.Config.GlobalPreset == kv.Key;
                kv.Value.Background = active ? StudioTheme.AccentOrange : StudioTheme.BgCard;
                kv.Value.Foreground = active ? Brushes.Black : StudioTheme.TextPrimary;
                kv.Value.BorderBrush = active ? StudioTheme.AccentOrange : StudioTheme.BorderBrush;
            }
        }

        private void AddGlobalPresetButton(Panel parent, FanControlMode mode, string label)
        {
            var btn = new Button
            {
                Content = label,
                FontSize = 11,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(4, 0, 0, 0),
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
                FontSize = 9.5,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(5, 3, 5, 3),
                Margin = new Thickness(0, 0, 3, 0),
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
            string shortLabel = zone == ChassisZone.CpuAio ? "CPU/AIO" : zone.ToString().ToUpperInvariant();
            var btn = new Button
            {
                Content = shortLabel,
                FontSize = 10.5,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(9, 4, 9, 4),
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
                FontSize = 10,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(7, 3, 7, 3),
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
                FontSize = 11,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(11, 6, 11, 6),
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
                FontSize = 10.5,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
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
                BorderBrush = StudioTheme.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(12, 5, 12, 5),
                Margin = new Thickness(0, 0, 10, 0)
            };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 9.5,
                FontFamily = new FontFamily("Consolas"),
                Foreground = StudioTheme.TextMuted
            });
            valueBlock = new TextBlock
            {
                Text = "--",
                FontSize = 15,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Foreground = valueBrush
            };
            stack.Children.Add(valueBlock);
            border.Child = stack;
            return border;
        }

        private static string ShortCpuName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "CPU";
            return raw.Replace("AMD ", "").Replace("Processor", "").Trim();
        }

        private static string ShortGpuName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "GPU";
            return raw.Replace("NVIDIA ", "").Replace("GeForce ", "").Trim();
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
                    Text = "HyteFlow — Universal 3D Fan Studio",
                    Icon = trayIco,
                    Visible = true
                };
                var menu = new System.Windows.Forms.ContextMenuStrip();
                menu.Items.Add("Open HyteFlow Studio", null, (s, e) => { Show(); WindowState = WindowState.Normal; Activate(); });
                menu.Items.Add("-");
                menu.Items.Add("Profile: Smart Curves", null, (s, e) => Dispatcher.Invoke(() => _engine.ApplyGlobalPreset(FanControlMode.SmartCurve)));
                menu.Items.Add("Profile: Silent", null, (s, e) => Dispatcher.Invoke(() => _engine.ApplyGlobalPreset(FanControlMode.Silent)));
                menu.Items.Add("Profile: Balanced", null, (s, e) => Dispatcher.Invoke(() => _engine.ApplyGlobalPreset(FanControlMode.Balanced)));
                menu.Items.Add("Profile: 100% Max Blast", null, (s, e) => Dispatcher.Invoke(() => _engine.ApplyGlobalPreset(FanControlMode.FullBlast)));
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

        private string PromptForCaseName()
        {
            var dlg = new Window
            {
                Title = "Download 3D PC Case Representation",
                Width = 420,
                Height = 190,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = StudioTheme.BgPanel,
                Foreground = StudioTheme.TextPrimary,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };
            var stack = new StackPanel { Margin = new Thickness(18) };
            stack.Children.Add(new TextBlock
            {
                Text = "ENTER YOUR PC CASE BRAND & MODEL:",
                FontSize = 11.5,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Foreground = StudioTheme.AccentCyan,
                Margin = new Thickness(0, 0, 0, 6)
            });
            stack.Children.Add(new TextBlock
            {
                Text = "Examples: HYTE Y70, Lian Li O11 Vision, NZXT H9 Flow, Corsair 5000D, Phanteks NV7",
                FontSize = 10.5,
                Foreground = StudioTheme.TextMuted,
                Margin = new Thickness(0, 0, 0, 10)
            });
            var tb = new TextBox
            {
                Text = "Lian Li O11 Dynamic EVO",
                Height = 28,
                FontSize = 12,
                Padding = new Thickness(6, 3, 6, 3),
                Background = StudioTheme.BgCard,
                Foreground = StudioTheme.TextPrimary,
                BorderBrush = StudioTheme.BorderBrush
            };
            stack.Children.Add(tb);

            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
            string result = null;
            var btnOk = CreateHeaderButton("🌐 DOWNLOAD & APPLY 3D VIEW", true, StudioTheme.AccentOrange);
            btnOk.Click += (s, e) => { result = tb.Text; dlg.Close(); };
            btnRow.Children.Add(btnOk);
            stack.Children.Add(btnRow);
            dlg.Content = stack;
            dlg.ShowDialog();
            return result;
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
                BorderBrush = StudioTheme.BorderBrush,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 9),
                Cursor = Cursors.Hand
            };
            RootElement.MouseLeftButtonDown += (s, e) => _window.SelectChannel(Channel);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(235) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(148) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });

            _rotor = new SpinningFanControl
            {
                Rpm = Channel.CurrentRpm,
                DutyPercent = Channel.CurrentDutyPercent,
                IsPulsing = Channel.IsPulsing
            };
            Grid.SetColumn(_rotor, 0);
            grid.Children.Add(_rotor);

            var idStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 8, 0) };
            _nameText = new TextBlock
            {
                Text = Channel.Nickname,
                FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                Foreground = StudioTheme.TextPrimary,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            _originText = new TextBlock
            {
                Text = $"{Channel.HardwareOrigin}  •  {Channel.TopologyLabel}",
                FontSize = 10.5,
                FontFamily = new FontFamily("Consolas"),
                Foreground = StudioTheme.TextMuted,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 1, 0, 4)
            };

            _zoneBadgeBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(6, 2, 6, 2),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 107, 43)),
                BorderBrush = StudioTheme.AccentOrange,
                BorderThickness = new Thickness(1)
            };
            _zoneBadgeText = new TextBlock
            {
                Text = $"{Channel.ZoneLabel} ({Channel.AirflowRole})",
                FontSize = 9.5,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Foreground = StudioTheme.AccentOrange
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
                Text = $"{Channel.CurrentRpm} RPM",
                FontSize = 16,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Foreground = Channel.IsSpinning ? StudioTheme.AccentGreen : StudioTheme.TextDim
            };
            _dutyText = new TextBlock
            {
                Text = $"{Channel.CurrentDutyPercent:0}% PWM  •  {Channel.ModeLabel}",
                FontSize = 10.5,
                FontFamily = new FontFamily("Consolas"),
                Foreground = StudioTheme.TextMuted
            };
            rpmStack.Children.Add(_rpmText);
            rpmStack.Children.Add(_dutyText);
            Grid.SetColumn(rpmStack, 2);
            grid.Children.Add(rpmStack);

            var ctrlStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 14, 0) };
            var sliderHeader = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
            sliderHeader.Children.Add(new TextBlock
            {
                Text = "SPEED % / LOCATION:",
                FontSize = 9.5,
                FontFamily = new FontFamily("Consolas"),
                Foreground = StudioTheme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center
            });

            _zoneQuickBtn = new Button
            {
                Width = 112,
                Height = 20,
                FontSize = 9.5,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(Color.FromRgb(16, 20, 26)),
                Foreground = StudioTheme.AccentOrange,
                BorderBrush = StudioTheme.BorderBrush,
                BorderThickness = new Thickness(1),
                Content = $"LOC: {Channel.Zone.ToString().ToUpperInvariant()} ▾"
            };
            _zoneQuickBtn.Click += (s, e) =>
            {
                e.Handled = true;
                int idx = Array.IndexOf(AllZones, Channel.Zone);
                Channel.Zone = AllZones[(idx + 1) % AllZones.Length];
                RefreshTelemetry();
                _window.SelectChannel(Channel);
                _window.NotifyCardsRefresh();
                _engine.SaveConfig();
            };
            sliderHeader.Children.Add(_zoneQuickBtn);
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
                Content = "⚡ LOCATE 100%",
                FontSize = 10.5,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Height = 34,
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(Color.FromRgb(18, 28, 38)),
                Foreground = StudioTheme.AccentCyan,
                BorderBrush = StudioTheme.AccentCyan,
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
            RootElement.BorderBrush = selected ? StudioTheme.AccentOrange : StudioTheme.BorderBrush;
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
            _rpmText.Text = Channel.StatusText;
            _rpmText.Foreground = Channel.IsPulsing ? StudioTheme.AccentCyan :
                                  (Channel.IsSpinning ? StudioTheme.AccentGreen : StudioTheme.TextDim);

            _dutyText.Text = $"{Channel.CurrentDutyPercent:0}% PWM  •  {Channel.ModeLabel}";

            _suppressSliderEvent = true;
            if (!_dutySlider.IsMouseCaptureWithin)
                _dutySlider.Value = Channel.CurrentDutyPercent;
            _zoneQuickBtn.Content = $"LOC: {(Channel.Zone == ChassisZone.CpuAio ? "CPU/AIO" : Channel.Zone.ToString().ToUpperInvariant())} ▾";
            _suppressSliderEvent = false;

            _rotor.Rpm = Channel.CurrentRpm;
            _rotor.DutyPercent = Channel.CurrentDutyPercent;
            _rotor.IsPulsing = Channel.IsPulsing;
            _rotor.InvalidateVisual();

            if (Channel.IsPulsing)
            {
                _locateBtn.Content = $"LOCATING ({Channel.PulseSecondsRemaining}s)";
                _locateBtn.Background = StudioTheme.AccentCyan;
                _locateBtn.Foreground = Brushes.Black;
                RootElement.BorderBrush = StudioTheme.AccentCyan;
            }
            else
            {
                _locateBtn.Content = "⚡ LOCATE 100%";
                _locateBtn.Background = new SolidColorBrush(Color.FromRgb(18, 28, 38));
                _locateBtn.Foreground = StudioTheme.AccentCyan;
            }
        }
    }
}
