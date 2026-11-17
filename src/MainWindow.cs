using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace NologGuard
{
    public sealed class MainWindow : Window
    {
        public static int WmShowMe = 0;
        public static int WmExitApp = 0;

        private const int WmQueryEndSession = 0x0011;
        private const int WmEndSession = 0x0016;
        private const int MaxLogEntries = 300;
        private const int PauseModeNone = 0;
        private const int PauseModeAdBlock = 1;
        private const int PauseModeShield = 2;

        private readonly AppConfig _config;
        private readonly DohResolver _resolver;
        private readonly ProxyServer _proxy;
        private readonly bool _startMinimized;

        private bool _isProtected;
        private bool _updatingUi;
        private bool _forceClose;
        private bool _trayHintShown;

        private int _pauseMode = PauseModeNone;
        private bool _pausedSavedAdBlock = true;
        private DateTime _pauseUntilUtc = DateTime.MinValue;
        private DispatcherTimer _pauseTimer;
        private DispatcherTimer _trimTimer;

        private System.Drawing.Icon _iconOff;
        private System.Drawing.Icon _iconOn;
        private Forms.NotifyIcon _trayIcon;
        private Forms.ContextMenuStrip _trayMenu;
        private Forms.ToolStripMenuItem _trayOpenItem;
        private Forms.ToolStripMenuItem _trayToggleItem;
        private Forms.ToolStripMenuItem _trayPauseAb5Item;
        private Forms.ToolStripMenuItem _trayPauseShield15Item;
        private Forms.ToolStripMenuItem _trayResumeItem;
        private Forms.ToolStripMenuItem _trayExitItem;

        private DispatcherTimer _statsTimer;
        private readonly object _logLock = new object();
        private readonly Queue<string> _pendingLogs = new Queue<string>();
        private readonly List<string> _logHistory = new List<string>();

        // Header
        private TextBlock _txtSubtitle;
        private Border _badgeRole;
        private TextBlock _txtRoleBadge;
        private Button _btnLang;
        private Popup _langPopup;
        private StackPanel _langMenuPanel;
        private Button _btnMinimize;
        private Button _btnClose;

        // Nav Tabs
        private Button[] _tabButtons;
        private UIElement[] _pages;
        private int _activeTab;

        // Dashboard
        private Ellipse _statusDot;
        private TextBlock _txtStatusBadge;
        private Button _btnPower;
        private TextBlock _txtPowerBtn;
        private TextBlock _txtStatusHint;
        private TextBlock _txtDohLabel;
        private ComboBox _cmbDoh;
        private Button _btnTestDoh;
        private TextBlock _txtDohTestResult;
        private TextBlock _txtSniLabel;
        private ComboBox _cmbSniMode;
        private TextBlock _txtChkSysProxy;
        private CheckBox _chkSysProxy;
        private TextBlock _txtChkAdBlock;
        private CheckBox _chkAdBlock;
        private TextBlock _txtChkSysDoh;
        private CheckBox _chkSysDoh;
        private TextBlock _txtChkBlockQuic;
        private CheckBox _chkBlockQuic;
        private Button _btnWhyAdmin;
        private Button _btnRunAdmin;
        private TextBlock _txtStatProtectedVal;
        private TextBlock _txtStatProtectedTitle;
        private TextBlock _txtStatBlockedVal;
        private TextBlock _txtStatBlockedTitle;
        private TextBlock _txtStatDohVal;
        private TextBlock _txtStatDohTitle;
        private TextBlock _txtStatExcludedVal;
        private TextBlock _txtStatExcludedTitle;
        private TextBlock _txtDisclaimer;

        // AdBlock Tab
        private TextBlock _txtAbPresetLabel;
        private ComboBox _cmbAbPreset;
        private Button _btnAbUpdate;
        private StackPanel _abCustomUrlPanel;
        private TextBlock _txtAbCustomUrlLabel;
        private TextBox _tbAbCustomUrl;
        private TextBlock _txtAbStatus;
        private TextBlock _txtAbNote;
        private TextBlock _txtAbDesc;
        private TextBox _tbAbQuickAdd;
        private Button _btnAbQuickAdd;
        private TextBox _tbBlockRules;
        private Button _btnAbReset;
        private Button _btnAbSave;

        // Whitelist Tab
        private TextBlock _txtWhitelistDesc;
        private TextBox _tbWlQuickAdd;
        private Button _btnWlQuickAdd;
        private TextBox _tbWhitelist;
        private Button _btnWlReset;
        private Button _btnWlSave;
        private TextBlock _txtWlSaved;

        // Settings Tab
        private TextBlock _txtSetPortLabel;
        private TextBox _tbPort;
        private TextBlock _txtSetChunkLabel;
        private TextBox _tbChunk;
        private TextBlock _txtSetDelayLabel;
        private TextBox _tbDelay;
        private TextBlock _txtSetCustomDohLabel;
        private TextBox _tbCustomDoh;
        private TextBlock _txtSetLangLabel;
        private ComboBox _cmbLang;
        private TextBlock _txtChkTray;
        private CheckBox _chkMinToTray;
        private TextBlock _txtChkStartup;
        private CheckBox _chkStartup;
        private TextBlock _txtChkAutoProtect;
        private CheckBox _chkAutoProtect;
        private TextBlock _txtChkLogDomains;
        private CheckBox _chkLogDomains;
        private Button _btnSaveSettings;
        private TextBlock _txtSettingsStatus;
        private Button _btnCheckUpdate;
        private TextBlock _txtUpdateStatus;
        private Button _btnEmergencyRecover;

        // Log Tab
        private TextBlock _txtLogNote;
        private Button _btnClearLog;
        private TextBox _tbLog;

        public MainWindow(AppConfig config, bool startMinimized)
            : this(config, startMinimized, false)
        {
        }

        public MainWindow(AppConfig config, bool startMinimized, bool autoEnableShield)
        {
            _config = config ?? AppConfig.CreateDefault();
            _startMinimized = startMinimized;
            _resolver = new DohResolver(DohProvider.FindById(_config.DohProviderId, _config.CustomDohUrl));
            _proxy = new ProxyServer(_resolver, _config);
            _proxy.OnLog += HandleProxyLog;

            _iconOff = WpfTheme.CreateShieldIcon(false);
            _iconOn = WpfTheme.CreateShieldIcon(true);

            InitializeWindow();
            BuildLayout();
            InitializeTray();
            BindConfigToControls();
            ApplyLocalization();
            SwitchTab(0);

            _statsTimer = new DispatcherTimer();
            _statsTimer.Interval = TimeSpan.FromMilliseconds(500);
            _statsTimer.Tick += HandleStatsTick;
            _statsTimer.Start();

            if (_startMinimized)
            {
                WindowState = WindowState.Minimized;
                ShowInTaskbar = false;
                _statsTimer.Stop();
                Loaded += delegate(object s, RoutedEventArgs e)
                {
                    Hide();
                    ToggleProtection(true);
                    ScheduleTrayMemoryTrim();
                };
            }
            else if (autoEnableShield || _config.AutoProtectOnLaunch)
            {
                Loaded += delegate(object s, RoutedEventArgs e)
                {
                    ToggleProtection(true);
                };
            }
        }

        private string T(string key)
        {
            return Strings.Get(_config.Language, key);
        }

        private void InitializeWindow()
        {
            Title = "NologGuard";
            Width = 612;
            Height = 682;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.CanMinimize;
            FontFamily = WpfTheme.UiFont;
            Resources.Add(typeof(System.Windows.Controls.Primitives.ScrollBar), WpfTheme.CreateDarkScrollBarStyle());
            UpdateWindowIcon(false);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowInteropHelper helper = new WindowInteropHelper(this);
            HwndSource src = HwndSource.FromHwnd(helper.Handle);
            if (src != null)
            {
                src.AddHook(WndProcHook);
            }
        }

        private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (WmShowMe != 0 && msg == WmShowMe)
            {
                RestoreFromTray();
                handled = true;
                return IntPtr.Zero;
            }

            if (WmExitApp != 0 && msg == WmExitApp)
            {
                _forceClose = true;
                Close();
                handled = true;
                return IntPtr.Zero;
            }

            if (msg == WmQueryEndSession)
            {
                _forceClose = true;
                handled = true;
                return new IntPtr(1);
            }

            if (msg == WmEndSession && wParam != IntPtr.Zero)
            {
                _forceClose = true;
                ShutdownSafely();
                Close();
                handled = true;
                return IntPtr.Zero;
            }

            return IntPtr.Zero;
        }

        private void UpdateWindowIcon(bool active)
        {
            try
            {
                System.Drawing.Icon ico = active ? _iconOn : _iconOff;
                if (ico != null)
                {
                    Icon = Imaging.CreateBitmapSourceFromHIcon(
                        ico.Handle,
                        Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                }
            }
            catch
            {
            }
        }

        private void BuildLayout()
        {
            Border outer = new Border();
            outer.Background = WpfTheme.BgWindow;
            outer.BorderBrush = WpfTheme.BorderSubtle;
            outer.BorderThickness = new Thickness(1);
            outer.CornerRadius = new CornerRadius(14);

            Grid rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Border header = BuildHeader();
            Grid.SetRow(header, 0);
            rootGrid.Children.Add(header);

            Border tabBar = BuildTabBar();
            Grid.SetRow(tabBar, 1);
            rootGrid.Children.Add(tabBar);

            Grid pagesHost = new Grid();
            pagesHost.Margin = new Thickness(18, 8, 18, 16);
            Grid.SetRow(pagesHost, 2);

            _pages = new UIElement[5];
            _pages[0] = WrapScroll(BuildDashboardPage());
            _pages[1] = BuildAdBlockPage();
            _pages[2] = BuildWhitelistPage();
            _pages[3] = WrapScroll(BuildSettingsPage());
            _pages[4] = BuildLogPage();

            for (int i = 0; i < _pages.Length; i++)
            {
                pagesHost.Children.Add(_pages[i]);
            }
            rootGrid.Children.Add(pagesHost);

            outer.Child = rootGrid;
            Content = outer;
        }

        private static ScrollViewer WrapScroll(UIElement content)
        {
            ScrollViewer sv = new ScrollViewer();
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            sv.Content = content;
            return sv;
        }

        private Border BuildHeader()
        {
            Border header = new Border();
            header.Background = Brushes.Transparent;
            header.Padding = new Thickness(18, 10, 14, 6);
            header.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.ButtonState == MouseButtonState.Pressed) DragMove();
            };

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            Border logoBadge = new Border();
            logoBadge.Width = 34;
            logoBadge.Height = 34;
            logoBadge.CornerRadius = new CornerRadius(9);
            logoBadge.Margin = new Thickness(0, 0, 11, 0);
            logoBadge.Background = new LinearGradientBrush(
                WpfTheme.AccentGreenColor,
                Color.FromRgb(14, 116, 144),
                45.0);

            Path shieldPath = new Path();
            shieldPath.Data = Geometry.Parse("M 12,2 L 20,5.5 L 19,13 C 18.5,17 15.5,20 12,21.5 C 8.5,20 5.5,17 5,13 L 4,5.5 Z");
            shieldPath.Stroke = Brushes.White;
            shieldPath.StrokeThickness = 1.8;
            shieldPath.Fill = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255));
            shieldPath.Width = 22;
            shieldPath.Height = 22;
            shieldPath.Stretch = Stretch.Uniform;
            shieldPath.HorizontalAlignment = HorizontalAlignment.Center;
            shieldPath.VerticalAlignment = VerticalAlignment.Center;
            logoBadge.Child = shieldPath;

            StackPanel titleCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            StackPanel titleRow = new StackPanel { Orientation = Orientation.Horizontal, FlowDirection = FlowDirection.LeftToRight };
            TextBlock lblTitle = new TextBlock
            {
                Text = "NologGuard",
                FontSize = 15.5,
                FontWeight = FontWeights.Bold,
                Foreground = WpfTheme.TextPrimary
            };
            TextBlock lblVer = new TextBlock
            {
                Text = "v" + AppConfig.AppVersion,
                FontSize = 10.5,
                Foreground = WpfTheme.TextMuted,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(6, 0, 0, 2)
            };
            TextBlock lblAuthor = new TextBlock
            {
                Text = "\u2022  by NologX.com",
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = WpfTheme.AccentCyan,
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(6, 0, 0, 2)
            };
            lblAuthor.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                try { Process.Start("https://nologx.com"); } catch { }
            };
            titleRow.Children.Add(lblTitle);
            titleRow.Children.Add(lblVer);
            titleRow.Children.Add(lblAuthor);

            _txtSubtitle = new TextBlock
            {
                FontSize = 11,
                Foreground = WpfTheme.TextSecondary,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 1, 8, 0)
            };
            titleCol.Children.Add(titleRow);
            titleCol.Children.Add(_txtSubtitle);

            left.Children.Add(logoBadge);
            left.Children.Add(titleCol);
            Grid.SetColumn(left, 0);
            g.Children.Add(left);

            StackPanel right = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            bool isAdmin = WindowsNetworkHelper.IsAdministrator();
            _badgeRole = new Border
            {
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Background = isAdmin ? WpfTheme.AccentGreenDark : WpfTheme.BgCardAlt
            };
            _txtRoleBadge = new TextBlock
            {
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = isAdmin ? new SolidColorBrush(Color.FromRgb(167, 243, 208)) : WpfTheme.TextSecondary
            };
            _badgeRole.Child = _txtRoleBadge;

            _btnLang = new Button
            {
                Style = WpfTheme.CreateFlatButtonStyle(WpfTheme.BgCardAlt, WpfTheme.BorderSubtle, new CornerRadius(6)),
                Padding = new Thickness(9, 4, 9, 4),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            _langPopup = new Popup
            {
                PlacementTarget = _btnLang,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade
            };
            Border langDrop = new Border
            {
                Background = WpfTheme.BgCard,
                BorderBrush = WpfTheme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(4),
                Margin = new Thickness(0, 4, 0, 0),
                MaxHeight = 330,
                MinWidth = 172
            };
            ScrollViewer langScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            _langMenuPanel = new StackPanel();
            RebuildLanguageMenu();
            langScroll.Content = _langMenuPanel;
            langDrop.Child = langScroll;
            _langPopup.Child = langDrop;

            _btnLang.Click += delegate(object s, RoutedEventArgs e)
            {
                RebuildLanguageMenu();
                _langPopup.IsOpen = !_langPopup.IsOpen;
            };

            _btnMinimize = new Button
            {
                Content = "\u2500",
                Style = WpfTheme.CreateFlatButtonStyle(Brushes.Transparent, WpfTheme.BgCardAlt, new CornerRadius(6)),
                BorderThickness = new Thickness(0),
                Width = 30,
                Height = 28,
                Padding = new Thickness(0),
                FontSize = 11,
                Margin = new Thickness(0, 0, 2, 0)
            };
            _btnMinimize.Click += delegate(object s, RoutedEventArgs e) { WindowState = WindowState.Minimized; };

            _btnClose = new Button
            {
                Content = "\u2715",
                Style = WpfTheme.CreateFlatButtonStyle(Brushes.Transparent, WpfTheme.AccentRed, new CornerRadius(6)),
                BorderThickness = new Thickness(0),
                Width = 30,
                Height = 28,
                Padding = new Thickness(0),
                FontSize = 12
            };
            _btnClose.Click += delegate(object s, RoutedEventArgs e) { Close(); };

            right.Children.Add(_badgeRole);
            right.Children.Add(_btnLang);
            right.Children.Add(_btnMinimize);
            right.Children.Add(_btnClose);
            Grid.SetColumn(right, 1);
            g.Children.Add(right);

            header.Child = g;
            return header;
        }

        private Border BuildTabBar()
        {
            Border bar = new Border();
            bar.Margin = new Thickness(18, 2, 18, 6);
            bar.Background = WpfTheme.BgCard;
            bar.BorderBrush = WpfTheme.BorderSubtle;
            bar.BorderThickness = new Thickness(1);
            bar.CornerRadius = new CornerRadius(10);
            bar.Padding = new Thickness(4);

            Grid g = new Grid();
            _tabButtons = new Button[5];
            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                Button tb = new Button
                {
                    Style = WpfTheme.CreateFlatButtonStyle(Brushes.Transparent, WpfTheme.BgCardAlt, new CornerRadius(7)),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(4, 7, 4, 7),
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = WpfTheme.TextSecondary
                };
                tb.Click += delegate(object s, RoutedEventArgs e) { SwitchTab(idx); };
                Grid.SetColumn(tb, i);
                _tabButtons[i] = tb;
                g.Children.Add(tb);
            }

            bar.Child = g;
            return bar;
        }

        private Border MakeCard(Thickness padding, Thickness margin)
        {
            return new Border
            {
                Background = WpfTheme.BgCard,
                BorderBrush = WpfTheme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = padding,
                Margin = margin
            };
        }

        private UIElement BuildDashboardPage()
        {
            StackPanel stack = new StackPanel();

            Border hero = MakeCard(new Thickness(16, 11, 16, 11), new Thickness(0, 0, 0, 8));
            StackPanel heroStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

            StackPanel badgeRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 7)
            };
            _statusDot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = WpfTheme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 7, 0)
            };
            _txtStatusBadge = new TextBlock
            {
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                Foreground = WpfTheme.TextSecondary
            };
            badgeRow.Children.Add(_statusDot);
            badgeRow.Children.Add(_txtStatusBadge);

            _btnPower = new Button
            {
                Width = 264,
                Height = 44,
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            ControlTemplate powerTpl = new ControlTemplate(typeof(Button));
            FrameworkElementFactory pBd = new FrameworkElementFactory(typeof(Border));
            pBd.Name = "pbd";
            pBd.SetValue(Border.CornerRadiusProperty, new CornerRadius(22));
            pBd.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            pBd.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            pBd.SetValue(Border.BorderThicknessProperty, new Thickness(1.5));
            FrameworkElementFactory pCp = new FrameworkElementFactory(typeof(ContentPresenter));
            pCp.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            pCp.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            pBd.AppendChild(pCp);
            powerTpl.VisualTree = pBd;
            Trigger pHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            pHover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.92, "pbd"));
            pHover.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(2.2), "pbd"));
            powerTpl.Triggers.Add(pHover);
            _btnPower.Template = powerTpl;

            StackPanel powerContent = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Path powerGlyph = new Path
            {
                Data = Geometry.Parse("M 12,3 L 12,11 M 7.5,5.8 A 7.5,7.5 0 1 0 16.5,5.8"),
                Stroke = Brushes.White,
                StrokeThickness = 2.3,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Width = 18,
                Height = 18,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 0, 10, 0)
            };
            _txtPowerBtn = new TextBlock
            {
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            powerContent.Children.Add(powerGlyph);
            powerContent.Children.Add(_txtPowerBtn);
            _btnPower.Content = powerContent;
            _btnPower.Click += delegate(object s, RoutedEventArgs e) { ToggleProtection(!_isProtected); };

            _txtStatusHint = new TextBlock
            {
                FontSize = 11.5,
                Foreground = WpfTheme.TextSecondary,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 7, 0, 0)
            };

            heroStack.Children.Add(badgeRow);
            heroStack.Children.Add(_btnPower);
            heroStack.Children.Add(_txtStatusHint);
            hero.Child = heroStack;
            stack.Children.Add(hero);

            Border opts = MakeCard(new Thickness(14, 11, 14, 11), new Thickness(0, 0, 0, 8));
            StackPanel optStack = new StackPanel();
            Grid.SetIsSharedSizeScope(optStack, true);

            Grid dohRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            dohRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 104, SharedSizeGroup = "OptLabel" });
            dohRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            dohRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _txtDohLabel = new TextBlock { FontSize = 12.5, Foreground = WpfTheme.TextSecondary, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            _cmbDoh = new ComboBox { Style = WpfTheme.CreateDarkComboBoxStyle(), Margin = new Thickness(0, 0, 8, 0) };
            for (int i = 0; i < DohProvider.Presets.Length; i++)
            {
                _cmbDoh.Items.Add(DohProvider.Presets[i].DisplayName);
            }
            _cmbDoh.Items.Add("Custom DoH URL");
            _cmbDoh.SelectionChanged += HandleDashboardComboChanged;

            _btnTestDoh = new Button
            {
                Style = WpfTheme.CreateFlatButtonStyle(WpfTheme.BgCardAlt, WpfTheme.BorderSubtle, new CornerRadius(7)),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Height = 32,
                Padding = new Thickness(12, 0, 12, 0)
            };
            _btnTestDoh.Click += HandleTestDohClick;

            Grid.SetColumn(_txtDohLabel, 0);
            Grid.SetColumn(_cmbDoh, 1);
            Grid.SetColumn(_btnTestDoh, 2);
            dohRow.Children.Add(_txtDohLabel);
            dohRow.Children.Add(_cmbDoh);
            dohRow.Children.Add(_btnTestDoh);
            optStack.Children.Add(dohRow);

            _txtDohTestResult = new TextBlock
            {
                FontSize = 11.5,
                Foreground = WpfTheme.AccentCyan,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8),
                Visibility = Visibility.Collapsed
            };
            optStack.Children.Add(_txtDohTestResult);

            Grid sniRow = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            sniRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 104, SharedSizeGroup = "OptLabel" });
            sniRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _txtSniLabel = new TextBlock { FontSize = 12.5, Foreground = WpfTheme.TextSecondary, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            _cmbSniMode = new ComboBox { Style = WpfTheme.CreateDarkComboBoxStyle() };
            _cmbSniMode.SelectionChanged += HandleDashboardComboChanged;

            Grid.SetColumn(_txtSniLabel, 0);
            Grid.SetColumn(_cmbSniMode, 1);
            sniRow.Children.Add(_txtSniLabel);
            sniRow.Children.Add(_cmbSniMode);
            optStack.Children.Add(sniRow);

            optStack.Children.Add(new Border { Height = 1, Background = WpfTheme.BorderSubtle, Margin = new Thickness(0, 0, 0, 10) });

            optStack.Children.Add(MakeToggleRow(out _txtChkSysProxy, out _chkSysProxy, HandleDashboardToggleChanged));
            optStack.Children.Add(MakeToggleRow(out _txtChkAdBlock, out _chkAdBlock, HandleDashboardToggleChanged));
            optStack.Children.Add(MakeToggleRow(out _txtChkSysDoh, out _chkSysDoh, HandleAdminToggleChanged));
            optStack.Children.Add(MakeToggleRow(out _txtChkBlockQuic, out _chkBlockQuic, HandleAdminToggleChanged));

            Grid adminRow = new Grid { Margin = new Thickness(0, 6, 0, 0) };
            adminRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            adminRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _btnWhyAdmin = new Button
            {
                Style = WpfTheme.CreateFlatButtonStyle(Brushes.Transparent, WpfTheme.BgCardAlt, new CornerRadius(7)),
                BorderThickness = new Thickness(0),
                Foreground = WpfTheme.AccentCyan,
                FontSize = 11.5,
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(6, 4, 8, 4)
            };
            _btnWhyAdmin.Click += delegate(object s, RoutedEventArgs e)
            {
                MessageBox.Show(this, T("why_admin_body"), T("why_admin_title"), MessageBoxButton.OK, MessageBoxImage.Information);
            };

            _btnRunAdmin = new Button
            {
                Style = WpfTheme.CreateFlatButtonStyle(WpfTheme.BgCardAlt, WpfTheme.BorderSubtle, new CornerRadius(7)),
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Right,
                Visibility = WindowsNetworkHelper.IsAdministrator() ? Visibility.Collapsed : Visibility.Visible
            };
            _btnRunAdmin.Click += delegate(object s, RoutedEventArgs e) { PromptAndRestartAdmin(false); };

            Grid.SetColumn(_btnWhyAdmin, 0);
            Grid.SetColumn(_btnRunAdmin, 1);
            adminRow.Children.Add(_btnWhyAdmin);
            adminRow.Children.Add(_btnRunAdmin);
            optStack.Children.Add(adminRow);

            opts.Child = optStack;
            stack.Children.Add(opts);

            Grid metricsGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            for (int i = 0; i < 4; i++)
            {
                metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            metricsGrid.Children.Add(MakeStatCard(0, new Thickness(0, 0, 6, 0), WpfTheme.AccentGreen, out _txtStatProtectedVal, out _txtStatProtectedTitle));
            metricsGrid.Children.Add(MakeStatCard(1, new Thickness(3, 0, 3, 0), WpfTheme.AccentRose, out _txtStatBlockedVal, out _txtStatBlockedTitle));
            metricsGrid.Children.Add(MakeStatCard(2, new Thickness(3, 0, 3, 0), WpfTheme.AccentCyan, out _txtStatDohVal, out _txtStatDohTitle));
            metricsGrid.Children.Add(MakeStatCard(3, new Thickness(6, 0, 0, 0), WpfTheme.AccentAmber, out _txtStatExcludedVal, out _txtStatExcludedTitle));
            stack.Children.Add(metricsGrid);

            Border noteCard = new Border
            {
                Background = WpfTheme.BgInput,
                BorderBrush = WpfTheme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 9, 12, 9)
            };
            _txtDisclaimer = new TextBlock
            {
                FontSize = 11,
                Foreground = WpfTheme.TextSecondary,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 16
            };
            noteCard.Child = _txtDisclaimer;
            stack.Children.Add(noteCard);

            return stack;
        }

        private Grid MakeToggleRow(out TextBlock label, out CheckBox toggle, RoutedEventHandler onChanged)
        {
            Grid row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            label = new TextBlock
            {
                FontSize = 12.5,
                Foreground = WpfTheme.TextPrimary,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            toggle = new CheckBox { Style = WpfTheme.CreateToggleSwitchStyle() };
            toggle.Checked += onChanged;
            toggle.Unchecked += onChanged;

            Grid.SetColumn(label, 0);
            Grid.SetColumn(toggle, 1);
            row.Children.Add(label);
            row.Children.Add(toggle);
            return row;
        }

        private Border MakeStatCard(int col, Thickness margin, Brush valColor, out TextBlock valTxt, out TextBlock titleTxt)
        {
            Border card = MakeCard(new Thickness(10, 9, 10, 9), margin);
            Grid.SetColumn(card, col);

            StackPanel sp = new StackPanel();
            valTxt = new TextBlock
            {
                Text = "0",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = valColor
            };
            titleTxt = new TextBlock
            {
                FontSize = 11,
                Foreground = WpfTheme.TextSecondary,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 2, 0, 0)
            };
            sp.Children.Add(valTxt);
            sp.Children.Add(titleTxt);
            card.Child = sp;
            return card;
        }

        private UIElement BuildAdBlockPage()
        {
            Grid g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Border topCard = MakeCard(new Thickness(14, 12, 14, 12), new Thickness(0, 0, 0, 10));
            StackPanel topStack = new StackPanel();

            Grid presetRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            presetRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            presetRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            presetRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _txtAbPresetLabel = new TextBlock { FontSize = 12.5, Foreground = WpfTheme.TextSecondary, VerticalAlignment = VerticalAlignment.Center };
            _cmbAbPreset = new ComboBox { Style = WpfTheme.CreateDarkComboBoxStyle(), Margin = new Thickness(0, 0, 8, 0) };
            for (int i = 0; i < BlocklistPreset.Presets.Length; i++)
            {
                _cmbAbPreset.Items.Add(BlocklistPreset.Presets[i].DisplayName);
            }
            _cmbAbPreset.SelectionChanged += HandleAdBlockPresetChanged;

            _btnAbUpdate = new Button
            {
                Style = WpfTheme.CreateFlatButtonStyle(WpfTheme.AccentBlue, new SolidColorBrush(Color.FromRgb(29, 78, 216)), new CornerRadius(7)),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Height = 32
            };
            _btnAbUpdate.Click += HandleUpdateBlocklistClick;

            Grid.SetColumn(_txtAbPresetLabel, 0);
            Grid.SetColumn(_cmbAbPreset, 1);
            Grid.SetColumn(_btnAbUpdate, 2);
            presetRow.Children.Add(_txtAbPresetLabel);
            presetRow.Children.Add(_cmbAbPreset);
            presetRow.Children.Add(_btnAbUpdate);

            _abCustomUrlPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 8), Visibility = Visibility.Collapsed };
            _txtAbCustomUrlLabel = new TextBlock
            {
                FontSize = 11.5,
                Foreground = WpfTheme.TextSecondary,
                Margin = new Thickness(0, 0, 0, 4)
            };
            _tbAbCustomUrl = new TextBox
            {
                Style = WpfTheme.CreateDarkTextBoxStyle(true),
                Height = 32,
                FontSize = 11.5
            };
            _abCustomUrlPanel.Children.Add(_txtAbCustomUrlLabel);
            _abCustomUrlPanel.Children.Add(_tbAbCustomUrl);

            _txtAbStatus = new TextBlock { FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = WpfTheme.AccentCyan };
            _txtAbNote = new TextBlock { FontSize = 11, Foreground = WpfTheme.TextMuted, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 0) };

            topStack.Children.Add(presetRow);
            topStack.Children.Add(_abCustomUrlPanel);
            topStack.Children.Add(_txtAbStatus);
            topStack.Children.Add(_txtAbNote);
            topCard.Child = topStack;
            Grid.SetRow(topCard, 0);
            g.Children.Add(topCard);

            _txtAbDesc = new TextBlock
            {
                FontSize = 12,
                Foreground = WpfTheme.TextSecondary,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(_txtAbDesc, 1);
            g.Children.Add(_txtAbDesc);

            Grid addRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _tbAbQuickAdd = new TextBox { Style = WpfTheme.CreateDarkTextBoxStyle(false), Height = 32, Margin = new Thickness(0, 0, 8, 0) };
            _tbAbQuickAdd.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) { HandleQuickAddBlockRule(s, e); }
            };
            _btnAbQuickAdd = new Button
            {
                Style = WpfTheme.CreateFlatButtonStyle(WpfTheme.BgCardAlt, WpfTheme.BorderSubtle, new CornerRadius(7)),
                Height = 32,
                FontWeight = FontWeights.SemiBold
            };
            _btnAbQuickAdd.Click += HandleQuickAddBlockRule;
            Grid.SetColumn(_tbAbQuickAdd, 0);
            Grid.SetColumn(_btnAbQuickAdd, 1);
            addRow.Children.Add(_tbAbQuickAdd);
            addRow.Children.Add(_btnAbQuickAdd);
            Grid.SetRow(addRow, 2);
            g.Children.Add(addRow);

            _tbBlockRules = new TextBox
            {
                Style = WpfTheme.CreateDarkTextBoxStyle(true),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.NoWrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontSize = 12.5,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(_tbBlockRules, 3);
            g.Children.Add(_tbBlockRules);

            Grid footer = new Grid();
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _btnAbReset = new Button
            {
                Style = WpfTheme.CreateFlatButtonStyle(WpfTheme.BgCardAlt, WpfTheme.BorderSubtle, new CornerRadius(7)),
                Height = 34
            };
            _btnAbReset.Click += delegate(object s, RoutedEventArgs e)
            {
                _tbBlockRules.Text = string.Join(Environment.NewLine, AppConfig.CreateDefaultBlockRules().ToArray());
            };
            _btnAbSave = new Button
            {
                Style = WpfTheme.CreateFlatButtonStyle(WpfTheme.AccentGreenDark, WpfTheme.AccentGreen, new CornerRadius(7)),
                Height = 34,
                FontWeight = FontWeights.Bold
            };
            _btnAbSave.Click += HandleSaveBlockRules;

            Grid.SetColumn(_btnAbReset, 0);
            Grid.SetColumn(_btnAbSave, 2);
            footer.Children.Add(_btnAbReset);
            footer.Children.Add(_btnAbSave);
            Grid.SetRow(footer, 4);
            g.Children.Add(footer);

            return g;
        }

        private UIElement BuildWhitelistPage()
        {
            Grid g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _txtWhitelistDesc = new TextBlock
            {
                FontSize = 12,
                Foreground = WpfTheme.TextSecondary,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(_txtWhitelistDesc, 0);
            g.Children.Add(_txtWhitelistDesc);

            Grid addRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _tbWlQuickAdd = new TextBox { Style = WpfTheme.CreateDarkTextBoxStyle(false), Height = 32, Margin = new Thickness(0, 0, 8, 0) };
            _tbWlQuickAdd.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) { HandleQuickAddWhitelist(s, e); }
            };
            _btnWlQuickAdd = new Button
            {
                Style = WpfTheme.CreateFlatButtonStyle(WpfTheme.BgCardAlt, WpfTheme.BorderSubtle, new CornerRadius(7)),
                Height = 32,
                FontWeight = FontWeights.SemiBold
            };
            _btnWlQuickAdd.Click += HandleQuickAddWhitelist;
            Grid.SetColumn(_tbWlQuickAdd, 0);
            Grid.SetColumn(_btnWlQuickAdd, 1);
            addRow.Children.Add(_tbWlQuickAdd);
            addRow.Children.Add(_btnWlQuickAdd);
            Grid.SetRow(addRow, 1);
            g.Children.Add(addRow);

            _tbWhitelist = new TextBox
            {
                Style = WpfTheme.CreateDarkTextBoxStyle(true),
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontSize = 12.5,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(_tbWhitelist, 2);
            g.Children.Add(_tbWhitelist);

            Grid footer = new Grid();
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _btnWlReset = new Button
            {
                Style = WpfTheme.CreateFlatButtonStyle(WpfTheme.BgCardAlt, WpfTheme.BorderSubtle, new CornerRadius(7)),
                Height = 34
            };
            _btnWlReset.Click += delegate(object s, RoutedEventArgs e)
            {
                _tbWhitelist.Text = string.Join(Environment.NewLine, AppConfig.CreateDefaultWhitelist().ToArray());
            };
            _txtWlSaved = new TextBlock
            {
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = WpfTheme.AccentGreen,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 12, 0)
            };
            _btnWlSave = new Button
            {
                Style = WpfTheme.CreateFlatButtonStyle(WpfTheme.AccentGreenDark, WpfTheme.AccentGreen, new CornerRadius(7)),
                Height = 34,
                FontWeight = FontWeights.Bold
            };
            _btnWlSave.Click += HandleSaveWhitelist;

            Grid.SetColumn(_btnWlReset, 0);
            Grid.SetColumn(_txtWlSaved, 1);
            Grid.SetColumn(_btnWlSave, 2);
            footer.Children.Add(_btnWlReset);
            footer.Children.Add(_txtWlSaved);
            footer.Children.Add(_btnWlSave);
            Grid.SetRow(footer, 3);
            g.Children.Add(footer);

            return g;
        }

        private UIElement BuildSettingsPage()
        {
            StackPanel stack = new StackPanel();
            Border card = MakeCard(new Thickness(16, 13, 16, 13), new Thickness(0, 0, 0, 10));
            StackPanel sp = new StackPanel();

            sp.Children.Add(MakeNumericRow(out _txtSetPortLabel, out _tbPort));
            sp.Children.Add(MakeNumericRow(out _txtSetChunkLabel, out _tbChunk));
            sp.Children.Add(MakeNumericRow(out _txtSetDelayLabel, out _tbDelay));

            _txtSetCustomDohLabel = new TextBlock
            {
                FontSize = 12.5,
                Foreground = WpfTheme.TextSecondary,
                Margin = new Thickness(0, 4, 0, 4)
            };
            _tbCustomDoh = new TextBox
            {
                Style = WpfTheme.CreateDarkTextBoxStyle(false),
                Height = 33,
                Margin = new Thickness(0, 0, 0, 10)
            };
            sp.Children.Add(_txtSetCustomDohLabel);
            sp.Children.Add(_tbCustomDoh);

            Grid langRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            langRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            langRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });

            _txtSetLangLabel = new TextBlock
            {
                FontSize = 12.5,
                Foreground = WpfTheme.TextSecondary,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 12, 0)
            };
            _cmbLang = new ComboBox { Style = WpfTheme.CreateDarkComboBoxStyle() };
            for (int i = 0; i < Strings.Languages.Length; i++)
            {
                _cmbLang.Items.Add(Strings.Languages[i].NativeName);
            }
            _cmbLang.SelectionChanged += delegate(object s, SelectionChangedEventArgs e)
            {
                if (_updatingUi) return;
                int idx = _cmbLang.SelectedIndex;
                if (idx >= 0 && idx < Strings.Languages.Length)
                {
                    SelectLanguage(Strings.Languages[idx].Code);
                }
            };
            Grid.SetColumn(_txtSetLangLabel, 0);
            Grid.SetColumn(_cmbLang, 1);
            langRow.Children.Add(_txtSetLangLabel);
            langRow.Children.Add(_cmbLang);
            sp.Children.Add(langRow);

            sp.Children.Add(new Border { Height = 1, Background = WpfTheme.BorderSubtle, Margin = new Thickness(0, 0, 0, 8) });

            sp.Children.Add(MakeToggleRow(out _txtChkTray, out _chkMinToTray, delegate { }));
            sp.Children.Add(MakeToggleRow(out _txtChkStartup, out _chkStartup, delegate { }));
            sp.Children.Add(MakeToggleRow(out _txtChkAutoProtect, out _chkAutoProtect, delegate { }));
            sp.Children.Add(MakeToggleRow(out _txtChkLogDomains, out _chkLogDomains, delegate { }));

            Grid saveRow = new Grid { Margin = new Thickness(0, 10, 0, 0) };
            saveRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            saveRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _btnSaveSettings = new Button
            {
                Style = WpfTheme.CreateFlatButtonStyle(WpfTheme.AccentGreenDark, WpfTheme.AccentGreen, new CornerRadius(7)),
                Height = 34,
                FontWeight = FontWeights.Bold
            };
            _btnSaveSettings.Click += HandleSaveSettings;
            _txtSettingsStatus = new TextBlock
            {
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = WpfTheme.AccentGreen,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0)
            };
            Grid.SetColumn(_btnSaveSettings, 0);
            Grid.SetColumn(_txtSettingsStatus, 1);
            saveRow.Children.Add(_btnSaveSettings);
            saveRow.Children.Add(_txtSettingsStatus);
            sp.Children.Add(saveRow);

            card.Child = sp;
            stack.Children.Add(card);

            Border updateCard = MakeCard(new Thickness(14, 9, 14, 9), new Thickness(0, 0, 0, 10));
            Grid updateRow = new Grid();
            updateRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            updateRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _btnCheckUpdate = new Button
            {
                Style = WpfTheme.CreateFlatButtonStyle(WpfTheme.BgCardAlt, WpfTheme.BorderSubtle, new CornerRadius(7)),
                Height = 32,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            };
            _btnCheckUpdate.Click += HandleCheckUpdateClick;

            _txtUpdateStatus = new TextBlock
            {
                FontSize = 11.5,
                Foreground = WpfTheme.TextSecondary,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(12, 0, 0, 0)
            };
            Grid.SetColumn(_btnCheckUpdate, 0);
            Grid.SetColumn(_txtUpdateStatus, 1);
            updateRow.Children.Add(_btnCheckUpdate);
            updateRow.Children.Add(_txtUpdateStatus);
            updateCard.Child = updateRow;
            stack.Children.Add(updateCard);

            _btnEmergencyRecover = new Button
            {
                Style = WpfTheme.CreateFlatButtonStyle(new SolidColorBrush(Color.FromRgb(127, 29, 29)), WpfTheme.AccentRed, new CornerRadius(9)),
                Height = 38,
                FontWeight = FontWeights.SemiBold
            };
            _btnEmergencyRecover.Click += delegate(object s, RoutedEventArgs e)
            {
                if (_isProtected) ToggleProtection(false);
                WindowsNetworkHelper.EmergencyReset();
                _txtSettingsStatus.Text = T("recover_ok");
            };
            stack.Children.Add(_btnEmergencyRecover);

            return stack;
        }

        private Grid MakeNumericRow(out TextBlock label, out TextBox tb)
        {
            Grid row = new Grid { Margin = new Thickness(0, 0, 0, 7) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });

            label = new TextBlock
            {
                FontSize = 12.5,
                Foreground = WpfTheme.TextSecondary,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 12, 0)
            };
            tb = new TextBox
            {
                Style = WpfTheme.CreateDarkTextBoxStyle(true),
                Height = 31,
                TextAlignment = TextAlignment.Right
            };
            Grid.SetColumn(label, 0);
            Grid.SetColumn(tb, 1);
            row.Children.Add(label);
            row.Children.Add(tb);
            return row;
        }

        private UIElement BuildLogPage()
        {
            Grid g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid top = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _txtLogNote = new TextBlock
            {
                FontSize = 11.5,
                Foreground = WpfTheme.TextSecondary,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            _btnClearLog = new Button
            {
                Style = WpfTheme.CreateFlatButtonStyle(WpfTheme.BgCardAlt, WpfTheme.BorderSubtle, new CornerRadius(7)),
                Height = 30
            };
            _btnClearLog.Click += delegate(object s, RoutedEventArgs e)
            {
                _logHistory.Clear();
                _tbLog.Clear();
            };

            Grid.SetColumn(_txtLogNote, 0);
            Grid.SetColumn(_btnClearLog, 1);
            top.Children.Add(_txtLogNote);
            top.Children.Add(_btnClearLog);
            Grid.SetRow(top, 0);
            g.Children.Add(top);

            _tbLog = new TextBox
            {
                Style = WpfTheme.CreateDarkTextBoxStyle(true),
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Foreground = new SolidColorBrush(Color.FromRgb(186, 230, 253)),
                FontSize = 11.5
            };
            Grid.SetRow(_tbLog, 1);
            g.Children.Add(_tbLog);

            return g;
        }

        private void InitializeTray()
        {
            _trayMenu = new Forms.ContextMenuStrip();
            _trayOpenItem = new Forms.ToolStripMenuItem();
            _trayOpenItem.Click += delegate(object s, EventArgs e) { Dispatcher.BeginInvoke(new Action(RestoreFromTray)); };

            _trayToggleItem = new Forms.ToolStripMenuItem();
            _trayToggleItem.Click += delegate(object s, EventArgs e) { Dispatcher.BeginInvoke(new Action(delegate() { ToggleProtection(!_isProtected); })); };

            _trayPauseAb5Item = new Forms.ToolStripMenuItem();
            _trayPauseAb5Item.Click += delegate(object s, EventArgs e) { Dispatcher.BeginInvoke(new Action(delegate() { PauseAdBlockTemporary(5); })); };

            _trayPauseShield15Item = new Forms.ToolStripMenuItem();
            _trayPauseShield15Item.Click += delegate(object s, EventArgs e) { Dispatcher.BeginInvoke(new Action(delegate() { PauseShieldTemporary(15); })); };

            _trayResumeItem = new Forms.ToolStripMenuItem();
            _trayResumeItem.Visible = false;
            _trayResumeItem.Click += delegate(object s, EventArgs e) { Dispatcher.BeginInvoke(new Action(ResumeFromPause)); };

            _trayExitItem = new Forms.ToolStripMenuItem();
            _trayExitItem.Click += delegate(object s, EventArgs e) { Dispatcher.BeginInvoke(new Action(ExitCompletely)); };

            _trayMenu.Items.Add(_trayOpenItem);
            _trayMenu.Items.Add(_trayToggleItem);
            _trayMenu.Items.Add(new Forms.ToolStripSeparator());
            _trayMenu.Items.Add(_trayPauseAb5Item);
            _trayMenu.Items.Add(_trayPauseShield15Item);
            _trayMenu.Items.Add(_trayResumeItem);
            _trayMenu.Items.Add(new Forms.ToolStripSeparator());
            _trayMenu.Items.Add(_trayExitItem);

            _trayIcon = new Forms.NotifyIcon();
            _trayIcon.Icon = _iconOff;
            _trayIcon.Text = "NologGuard - Off";
            _trayIcon.ContextMenuStrip = _trayMenu;
            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += delegate(object s, EventArgs e) { Dispatcher.BeginInvoke(new Action(RestoreFromTray)); };
        }

        private void BindConfigToControls()
        {
            _updatingUi = true;
            try
            {
                int dohIdx = DohProvider.Presets.Length;
                for (int i = 0; i < DohProvider.Presets.Length; i++)
                {
                    if (string.Equals(DohProvider.Presets[i].Id, _config.DohProviderId, StringComparison.OrdinalIgnoreCase))
                    {
                        dohIdx = i;
                        break;
                    }
                }
                _cmbDoh.SelectedIndex = dohIdx;
                _chkSysProxy.IsChecked = _config.ApplySystemProxy;
                _chkAdBlock.IsChecked = _config.EnableAdBlock;
                _chkSysDoh.IsChecked = _config.ApplySystemDoh;
                _chkBlockQuic.IsChecked = _config.BlockQuicFirewall;

                int abIdx = 0;
                for (int i = 0; i < BlocklistPreset.Presets.Length; i++)
                {
                    if (string.Equals(BlocklistPreset.Presets[i].Id, _config.BlocklistPreset, StringComparison.OrdinalIgnoreCase))
                    {
                        abIdx = i;
                        break;
                    }
                }
                _cmbAbPreset.SelectedIndex = abIdx;
                _tbAbCustomUrl.Text = _config.CustomBlockUrl ?? string.Empty;
                _tbBlockRules.Text = string.Join(Environment.NewLine, _config.CustomBlockRules.ToArray());
                _tbWhitelist.Text = string.Join(Environment.NewLine, _config.Whitelist.ToArray());

                _tbPort.Text = _config.ProxyPort.ToString();
                _tbChunk.Text = _config.ChunkSize.ToString();
                _tbDelay.Text = _config.ChunkDelayMs.ToString();
                _tbCustomDoh.Text = _config.CustomDohUrl;
                _chkMinToTray.IsChecked = _config.MinimizeToTray;
                _chkStartup.IsChecked = _config.StartWithWindows;
                _chkAutoProtect.IsChecked = _config.AutoProtectOnLaunch;
                _chkLogDomains.IsChecked = _config.LogDomainsInUi;
            }
            finally
            {
                _updatingUi = false;
            }
        }

        private void ApplyLocalization()
        {
            _updatingUi = true;
            try
            {
                _txtSubtitle.Text = T("subtitle");
                _txtRoleBadge.Text = WindowsNetworkHelper.IsAdministrator() ? T("badge_admin") : T("badge_user");
                _btnLang.Content = _config.Language.ToUpperInvariant() + " \u25BE";

                bool rtl = Strings.IsRightToLeft(_config.Language);
                FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                if (_langPopup != null && _langPopup.Child is FrameworkElement)
                {
                    ((FrameworkElement)_langPopup.Child).FlowDirection = FlowDirection.LeftToRight;
                }
                if (_cmbDoh != null) _cmbDoh.FlowDirection = FlowDirection.LeftToRight;
                if (_txtStatusHint != null) _txtStatusHint.FlowDirection = FlowDirection.LeftToRight;
                if (_tbPort != null) _tbPort.FlowDirection = FlowDirection.LeftToRight;
                if (_tbChunk != null) _tbChunk.FlowDirection = FlowDirection.LeftToRight;
                if (_tbDelay != null) _tbDelay.FlowDirection = FlowDirection.LeftToRight;
                if (_tbCustomDoh != null) _tbCustomDoh.FlowDirection = FlowDirection.LeftToRight;
                if (_tbAbCustomUrl != null) _tbAbCustomUrl.FlowDirection = FlowDirection.LeftToRight;
                if (_tbAbQuickAdd != null) _tbAbQuickAdd.FlowDirection = FlowDirection.LeftToRight;
                if (_tbBlockRules != null) _tbBlockRules.FlowDirection = FlowDirection.LeftToRight;
                if (_tbWlQuickAdd != null) _tbWlQuickAdd.FlowDirection = FlowDirection.LeftToRight;
                if (_tbWhitelist != null) _tbWhitelist.FlowDirection = FlowDirection.LeftToRight;
                if (_tbLog != null) _tbLog.FlowDirection = FlowDirection.LeftToRight;

                if (_cmbLang != null)
                {
                    for (int i = 0; i < Strings.Languages.Length; i++)
                    {
                        if (string.Equals(Strings.Languages[i].Code, _config.Language, StringComparison.OrdinalIgnoreCase))
                        {
                            _cmbLang.SelectedIndex = i;
                            break;
                        }
                    }
                }

                _tabButtons[0].Content = T("tab_dashboard");
                _tabButtons[1].Content = T("tab_adblock");
                _tabButtons[2].Content = T("tab_whitelist");
                _tabButtons[3].Content = T("tab_settings");
                _tabButtons[4].Content = T("tab_log");

                _txtDohLabel.Text = T("lbl_doh");
                _btnTestDoh.Content = T("btn_test_doh");
                _txtSniLabel.Text = T("lbl_sni_mode");

                int prevSniIdx = (int)_config.SplitMode;
                _cmbSniMode.Items.Clear();
                _cmbSniMode.Items.Add(T("mode_smart"));
                _cmbSniMode.Items.Add(T("mode_fixed"));
                _cmbSniMode.Items.Add(T("mode_off"));
                _cmbSniMode.SelectedIndex = Math.Max(0, Math.Min(2, prevSniIdx));

                _txtChkSysProxy.Text = T("chk_sys_proxy");
                _txtChkAdBlock.Text = T("chk_adblock");
                _txtChkSysDoh.Text = T("chk_sys_doh");
                _txtChkBlockQuic.Text = T("chk_block_quic");
                _btnWhyAdmin.Content = T("btn_why_admin");
                _btnRunAdmin.Content = T("btn_admin");

                _txtStatProtectedTitle.Text = T("stat_protected");
                _txtStatBlockedTitle.Text = T("stat_blocked");
                _txtStatDohTitle.Text = T("stat_doh");
                _txtStatExcludedTitle.Text = T("stat_excluded");
                _txtDisclaimer.Text = T("disclaimer");

                _txtAbPresetLabel.Text = T("ab_preset");
                _txtAbCustomUrlLabel.Text = T("ab_custom_url");
                _btnAbUpdate.Content = T("ab_update");
                _txtAbDesc.Text = T("ab_desc");
                _btnAbQuickAdd.Content = T("btn_add");
                _btnAbReset.Content = T("btn_reset");
                _btnAbSave.Content = T("btn_save");
                _txtAbNote.Text = T("ab_note");
                RefreshAdBlockStatusLabel();

                _txtWhitelistDesc.Text = T("wl_desc");
                _btnWlQuickAdd.Content = T("btn_add");
                _btnWlReset.Content = T("btn_reset");
                _btnWlSave.Content = T("btn_save");

                _txtSetPortLabel.Text = T("set_port");
                _txtSetChunkLabel.Text = T("set_chunk");
                _txtSetDelayLabel.Text = T("set_delay");
                _txtSetCustomDohLabel.Text = T("set_custom_doh");
                _txtSetLangLabel.Text = T("set_lang");
                _txtChkTray.Text = T("chk_tray");
                _txtChkStartup.Text = T("chk_startup");
                _txtChkAutoProtect.Text = T("chk_auto_protect");
                _txtChkLogDomains.Text = T("chk_log_ui");
                _btnSaveSettings.Content = T("btn_save");
                _btnCheckUpdate.Content = T("btn_check_update");
                _btnEmergencyRecover.Content = T("btn_recover");

                _txtLogNote.Text = T("log_note");
                _btnClearLog.Content = T("btn_clear");

                _trayOpenItem.Text = T("tray_open");
                _trayToggleItem.Text = T("tray_toggle");
                _trayPauseAb5Item.Text = T("tray_pause_ab5");
                _trayPauseShield15Item.Text = T("tray_pause_shield15");
                _trayResumeItem.Text = T("tray_resume");
                _trayExitItem.Text = T("tray_exit");

                UpdatePauseTrayUi();
                UpdateProtectionStatusUi();
            }
            finally
            {
                _updatingUi = false;
            }
        }

        private void SelectLanguage(string code)
        {
            if (!Strings.IsSupported(code)) return;
            _config.Language = code;
            _config.Save();
            ApplyLocalization();
        }

        private void RebuildLanguageMenu()
        {
            if (_langMenuPanel == null) return;
            _langMenuPanel.Children.Clear();

            for (int i = 0; i < Strings.Languages.Length; i++)
            {
                LanguageInfo lang = Strings.Languages[i];
                bool selected = string.Equals(lang.Code, _config.Language, StringComparison.OrdinalIgnoreCase);

                Button item = new Button
                {
                    Style = WpfTheme.CreateFlatButtonStyle(
                        selected ? WpfTheme.AccentGreenDark : Brushes.Transparent,
                        WpfTheme.BgCardAlt,
                        new CornerRadius(6)),
                    BorderThickness = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(10, 6, 10, 6),
                    FontSize = 12,
                    FontWeight = selected ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = selected ? Brushes.White : WpfTheme.TextPrimary,
                    Content = (selected ? "\u2713  " : "     ") + lang.NativeName + " (" + lang.Code + ")"
                };

                string targetCode = lang.Code;
                item.Click += delegate(object s, RoutedEventArgs e)
                {
                    _langPopup.IsOpen = false;
                    SelectLanguage(targetCode);
                };
                _langMenuPanel.Children.Add(item);
            }
        }

        private void SwitchTab(int index)
        {
            _activeTab = index;
            for (int i = 0; i < _pages.Length; i++)
            {
                _pages[i].Visibility = (i == index) ? Visibility.Visible : Visibility.Collapsed;
                _tabButtons[i].Background = (i == index) ? WpfTheme.BgCardAlt : Brushes.Transparent;
                _tabButtons[i].Foreground = (i == index) ? WpfTheme.AccentCyan : WpfTheme.TextSecondary;
            }
            _txtWlSaved.Text = string.Empty;
            _txtSettingsStatus.Text = string.Empty;
        }

        private void ToggleProtection(bool enable)
        {
            ToggleProtectionInternal(enable, true);
        }

        private void ToggleProtectionInternal(bool enable, bool clearPause)
        {
            if (clearPause) ClearPauseState();
            if (enable == _isProtected) return;

            if (enable)
            {
                try
                {
                    SyncDashboardToConfig();
                    DohProvider provider = DohProvider.FindById(_config.DohProviderId, _config.CustomDohUrl);
                    _resolver.SetProvider(provider);
                    _proxy.UpdateSettings(_config);
                    _proxy.Start(_config.ProxyPort);

                    string warning;
                    WindowsNetworkHelper.ApplyProtection(_config, _proxy.ListenPort, out warning);
                    _isProtected = true;

                    if (!string.IsNullOrEmpty(warning))
                    {
                        HandleProxyLog("[WARN] " + warning);
                        ShowDashboardWarning(warning);
                    }
                    else
                    {
                        ClearDashboardWarning();
                    }
                    HandleProxyLog(string.Format("[ON] Proxy 127.0.0.1:{0} | DoH: {1}", _proxy.ListenPort, provider.DisplayName));
                }
                catch (Exception ex)
                {
                    _proxy.Stop();
                    WindowsNetworkHelper.RestoreAll();
                    _isProtected = false;
                    MessageBox.Show(this, ex.Message, "NologGuard Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                _proxy.Stop();
                WindowsNetworkHelper.RestoreAll();
                _isProtected = false;
                ClearDashboardWarning();
                HandleProxyLog("[OFF] Shield disabled and network settings restored.");
            }

            UpdateProtectionStatusUi();
        }

        private void PauseAdBlockTemporary(int minutes)
        {
            if (_pauseMode == PauseModeShield)
            {
                ToggleProtectionInternal(true, false);
            }

            if (_pauseMode == PauseModeNone)
            {
                _pausedSavedAdBlock = _config.EnableAdBlock;
            }

            _pauseMode = PauseModeAdBlock;
            _pauseUntilUtc = DateTime.UtcNow.AddMinutes(minutes);
            _config.EnableAdBlock = false;
            _config.Save();

            _updatingUi = true;
            _chkAdBlock.IsChecked = false;
            _updatingUi = false;

            _proxy.UpdateSettings(_config);
            RefreshAdBlockStatusLabel();
            EnsurePauseTimerRunning();
            UpdatePauseTrayUi();
            HandleProxyLog(string.Format("[PAUSE] AdBlock paused for {0} min.", minutes));
        }

        private void PauseShieldTemporary(int minutes)
        {
            if (_pauseMode == PauseModeAdBlock)
            {
                _config.EnableAdBlock = _pausedSavedAdBlock;
                _config.Save();
                _updatingUi = true;
                _chkAdBlock.IsChecked = _config.EnableAdBlock;
                _updatingUi = false;
            }

            _pauseMode = PauseModeShield;
            _pauseUntilUtc = DateTime.UtcNow.AddMinutes(minutes);
            ToggleProtectionInternal(false, false);
            EnsurePauseTimerRunning();
            UpdatePauseTrayUi();
            UpdateProtectionStatusUi();
            HandleProxyLog(string.Format("[PAUSE] Shield paused for {0} min.", minutes));
        }

        private void ResumeFromPause()
        {
            int oldMode = _pauseMode;
            ClearPauseState();

            if (oldMode == PauseModeAdBlock)
            {
                _config.EnableAdBlock = _pausedSavedAdBlock;
                _config.Save();
                _updatingUi = true;
                _chkAdBlock.IsChecked = _config.EnableAdBlock;
                _updatingUi = false;
                ApplyDashboardChange();
                HandleProxyLog("[RESUME] AdBlock auto-resumed.");
            }
            else if (oldMode == PauseModeShield)
            {
                ToggleProtectionInternal(true, false);
                HandleProxyLog("[RESUME] Shield auto-resumed.");
            }
            UpdatePauseTrayUi();
        }

        private void EnsurePauseTimerRunning()
        {
            if (_pauseTimer == null)
            {
                _pauseTimer = new DispatcherTimer();
                _pauseTimer.Interval = TimeSpan.FromSeconds(15);
                _pauseTimer.Tick += delegate(object s, EventArgs e)
                {
                    if (_pauseMode == PauseModeNone)
                    {
                        _pauseTimer.Stop();
                        return;
                    }
                    if (DateTime.UtcNow >= _pauseUntilUtc)
                    {
                        ResumeFromPause();
                    }
                    else
                    {
                        UpdatePauseTrayUi();
                    }
                };
            }
            _pauseTimer.Start();
        }

        private void ClearPauseState()
        {
            _pauseMode = PauseModeNone;
            _pauseUntilUtc = DateTime.MinValue;
            if (_pauseTimer != null) _pauseTimer.Stop();
            UpdatePauseTrayUi();
        }

        private void UpdatePauseTrayUi()
        {
            if (_trayResumeItem == null) return;
            bool paused = _pauseMode != PauseModeNone;
            _trayResumeItem.Visible = paused;
            if (paused)
            {
                int mins = Math.Max(1, (int)Math.Ceiling((_pauseUntilUtc - DateTime.UtcNow).TotalMinutes));
                _trayResumeItem.Text = T("tray_resume") + " (" + string.Format(T("tray_paused_msg"), mins) + ")";
            }
            else
            {
                _trayResumeItem.Text = T("tray_resume");
            }
        }

        private void UpdateProtectionStatusUi()
        {
            _txtPowerBtn.Text = _isProtected ? T("btn_disable") : T("btn_enable");

            if (_isProtected)
            {
                DohProvider p = DohProvider.FindById(_config.DohProviderId, _config.CustomDohUrl);
                _statusDot.Fill = (_pauseMode == PauseModeAdBlock) ? WpfTheme.AccentAmber : WpfTheme.AccentGreen;
                _txtStatusBadge.Text = T("status_on");
                _txtStatusBadge.Foreground = WpfTheme.AccentGreen;

                _btnPower.Background = new LinearGradientBrush(
                    Color.FromRgb(16, 185, 129),
                    Color.FromRgb(5, 150, 105),
                    90.0);
                _btnPower.BorderBrush = new SolidColorBrush(Color.FromRgb(110, 231, 183));

                _txtStatusHint.Text = string.Format("127.0.0.1:{0}  \u2022  {1}  \u2022  {2}",
                    _proxy.ListenPort, p.Host, _cmbSniMode.SelectedItem ?? string.Empty);

                UpdateWindowIcon(true);
                if (_trayIcon != null)
                {
                    _trayIcon.Icon = _iconOn;
                    string txt = "NologGuard - Protected (" + p.Host + ")";
                    if (_pauseMode == PauseModeAdBlock)
                    {
                        int mins = Math.Max(1, (int)Math.Ceiling((_pauseUntilUtc - DateTime.UtcNow).TotalMinutes));
                        txt = "NologGuard - " + string.Format(T("tray_paused_msg"), mins);
                    }
                    _trayIcon.Text = txt.Length > 63 ? txt.Substring(0, 63) : txt;
                }
            }
            else
            {
                _statusDot.Fill = (_pauseMode == PauseModeShield) ? WpfTheme.AccentAmber : WpfTheme.TextMuted;
                _txtStatusBadge.Text = T("status_off");
                _txtStatusBadge.Foreground = WpfTheme.TextSecondary;

                _btnPower.Background = new LinearGradientBrush(
                    Color.FromRgb(37, 99, 235),
                    Color.FromRgb(29, 78, 216),
                    90.0);
                _btnPower.BorderBrush = new SolidColorBrush(Color.FromRgb(96, 165, 250));

                _txtStatusHint.Text = T("status_hint_off");

                UpdateWindowIcon(false);
                if (_trayIcon != null)
                {
                    _trayIcon.Icon = _iconOff;
                    string txt = "NologGuard - Disabled";
                    if (_pauseMode == PauseModeShield)
                    {
                        int mins = Math.Max(1, (int)Math.Ceiling((_pauseUntilUtc - DateTime.UtcNow).TotalMinutes));
                        txt = "NologGuard - " + string.Format(T("tray_paused_msg"), mins);
                    }
                    _trayIcon.Text = txt.Length > 63 ? txt.Substring(0, 63) : txt;
                }
            }

            if (_tbPort != null) _tbPort.IsEnabled = !_isProtected;
        }

        private void SyncDashboardToConfig()
        {
            int idx = _cmbDoh.SelectedIndex;
            if (idx >= 0 && idx < DohProvider.Presets.Length)
            {
                _config.DohProviderId = DohProvider.Presets[idx].Id;
            }
            else
            {
                _config.DohProviderId = "custom";
            }

            if (_cmbSniMode.SelectedIndex >= 0 && _cmbSniMode.SelectedIndex <= 2)
            {
                _config.SplitMode = (SniSplitMode)_cmbSniMode.SelectedIndex;
            }

            _config.ApplySystemProxy = _chkSysProxy.IsChecked == true;
            _config.EnableAdBlock = _chkAdBlock.IsChecked == true;
            _config.ApplySystemDoh = _chkSysDoh.IsChecked == true;
            _config.BlockQuicFirewall = _chkBlockQuic.IsChecked == true;
            _config.Save();
        }

        private void HandleDashboardComboChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_updatingUi) return;
            if (sender == _cmbDoh && _txtDohTestResult != null)
            {
                _txtDohTestResult.Text = string.Empty;
                _txtDohTestResult.Visibility = Visibility.Collapsed;
            }
            ApplyDashboardChange();
        }

        private void HandleDashboardToggleChanged(object sender, RoutedEventArgs e)
        {
            if (_updatingUi) return;
            if (sender == _chkAdBlock && _pauseMode == PauseModeAdBlock)
            {
                ClearPauseState();
            }
            ApplyDashboardChange();
        }

        private void ShowDashboardWarning(string warning)
        {
            if (_txtDohTestResult == null || string.IsNullOrEmpty(warning)) return;
            _txtDohTestResult.Foreground = WpfTheme.AccentAmber;
            _txtDohTestResult.Text = "\u26A0 " + warning;
            _txtDohTestResult.Visibility = Visibility.Visible;
        }

        private void ClearDashboardWarning()
        {
            if (_txtDohTestResult == null) return;
            if (_txtDohTestResult.Text != null && _txtDohTestResult.Text.StartsWith("\u26A0", StringComparison.Ordinal))
            {
                _txtDohTestResult.Text = string.Empty;
                _txtDohTestResult.Visibility = Visibility.Collapsed;
            }
        }

        private void ApplyDashboardChange()
        {
            SyncDashboardToConfig();
            DohProvider provider = DohProvider.FindById(_config.DohProviderId, _config.CustomDohUrl);
            _resolver.SetProvider(provider);
            _proxy.UpdateSettings(_config);
            RefreshAdBlockStatusLabel();
            if (_isProtected)
            {
                string warn;
                WindowsNetworkHelper.ApplyProtection(_config, _proxy.ListenPort, out warn);
                if (!string.IsNullOrEmpty(warn)) ShowDashboardWarning(warn);
                else ClearDashboardWarning();
                UpdateProtectionStatusUi();
            }
        }

        private void HandleAdminToggleChanged(object sender, RoutedEventArgs e)
        {
            if (_updatingUi) return;
            CheckBox chk = sender as CheckBox;
            if (chk != null && chk.IsChecked == true && !WindowsNetworkHelper.IsAdministrator())
            {
                SyncDashboardToConfig();
                if (PromptAndRestartAdmin(true))
                {
                    return;
                }
                _updatingUi = true;
                chk.IsChecked = false;
                _updatingUi = false;
            }

            SyncDashboardToConfig();
            if (_isProtected)
            {
                if (chk != null && chk.IsChecked != true)
                {
                    WindowsNetworkHelper.RestoreAll();
                }
                string warn;
                WindowsNetworkHelper.ApplyProtection(_config, _proxy.ListenPort, out warn);
                if (!string.IsNullOrEmpty(warn)) ShowDashboardWarning(warn);
                else ClearDashboardWarning();
            }
        }

        private bool PromptAndRestartAdmin(bool askConfirmation)
        {
            if (askConfirmation)
            {
                MessageBoxResult res = MessageBox.Show(
                    this,
                    T("admin_needed_msg"),
                    T("admin_needed_title"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes)
                {
                    return false;
                }
            }

            bool wasProtected = _isProtected;
            if (_isProtected) ToggleProtection(false);
            string args = wasProtected ? "--relaunch --enable-shield" : "--relaunch";
            if (WindowsNetworkHelper.RestartAsAdministrator(args))
            {
                SingleInstanceLock.ReleaseCurrent();
                _forceClose = true;
                Application.Current.Shutdown();
                return true;
            }

            if (wasProtected) ToggleProtection(true);
            return false;
        }

        private async void HandleTestDohClick(object sender, RoutedEventArgs e)
        {
            _btnTestDoh.IsEnabled = false;
            _txtDohTestResult.Visibility = Visibility.Visible;
            _txtDohTestResult.Foreground = WpfTheme.TextSecondary;
            _txtDohTestResult.Text = "...";

            try
            {
                SyncDashboardToConfig();
                DohProvider provider = DohProvider.FindById(_config.DohProviderId, _config.CustomDohUrl);
                _resolver.SetProvider(provider);

                Tuple<bool, int, string> res = await _resolver.ProbeLatencyAsync(CancellationToken.None);
                bool abStatic = _proxy.IsAdBlockHostBlocked("doubleclick.net");
                bool loopbackOk = false;
                if (_isProtected && _config.EnableAdBlock)
                {
                    loopbackOk = await _proxy.VerifyLoopbackAdBlockAsync(CancellationToken.None);
                }

                string abVerdict;
                if (!_config.EnableAdBlock)
                {
                    abVerdict = "OFF (" + _proxy.ActiveBlockRuleCount + ")";
                }
                else if (_isProtected)
                {
                    abVerdict = loopbackOk
                        ? ("403 Loopback OK (" + _proxy.ActiveBlockRuleCount + ")")
                        : ("Loopback ERR (" + _proxy.ActiveBlockRuleCount + ")");
                }
                else
                {
                    abVerdict = (abStatic ? "Ready (" : "Empty (") + _proxy.ActiveBlockRuleCount + ")";
                }

                if (res.Item1)
                {
                    _txtDohTestResult.Foreground = (_isProtected && _config.EnableAdBlock && !loopbackOk)
                        ? WpfTheme.AccentAmber
                        : WpfTheme.AccentGreen;
                    _txtDohTestResult.Text = string.Format(T("diag_ok_prefix"), provider.Host, res.Item2, abVerdict);
                }
                else
                {
                    _txtDohTestResult.Foreground = WpfTheme.AccentRose;
                    _txtDohTestResult.Text = string.Format("\u2715 ERR \u2022 {0} ms \u2022 {1} | AdBlock: {2}", res.Item2, res.Item3, abVerdict);
                }
            }
            finally
            {
                _btnTestDoh.IsEnabled = true;
            }
        }

        private async void HandleCheckUpdateClick(object sender, RoutedEventArgs e)
        {
            _btnCheckUpdate.IsEnabled = false;
            _txtUpdateStatus.Foreground = WpfTheme.TextSecondary;
            _txtUpdateStatus.Text = T("upd_checking");

            try
            {
                UpdateCheckResult res = await UpdateChecker.CheckLatestAsync(AppConfig.GitHubRepo, AppConfig.AppVersion, CancellationToken.None);
                if (res.Success)
                {
                    string ver = (res.LatestVersion ?? string.Empty).Trim();
                    if (ver.StartsWith("v", StringComparison.OrdinalIgnoreCase)) ver = ver.Substring(1);
                    if (res.UpdateAvailable)
                    {
                        _txtUpdateStatus.Foreground = WpfTheme.AccentGreen;
                        _txtUpdateStatus.Text = string.Format(T("upd_available"), ver);
                        if (!string.IsNullOrEmpty(res.ReleaseUrl) &&
                            res.ReleaseUrl.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase))
                        {
                            try { Process.Start(res.ReleaseUrl); } catch { }
                        }
                    }
                    else
                    {
                        _txtUpdateStatus.Foreground = WpfTheme.AccentCyan;
                        _txtUpdateStatus.Text = string.Format(T("upd_latest"), AppConfig.AppVersion);
                    }
                }
                else
                {
                    _txtUpdateStatus.Foreground = WpfTheme.AccentRose;
                    _txtUpdateStatus.Text = string.Format(T("upd_err"), res.ErrorMessage);
                }
            }
            finally
            {
                _btnCheckUpdate.IsEnabled = true;
            }
        }

        private void RefreshAdBlockStatusLabel()
        {
            if (_cmbAbPreset == null || _btnAbUpdate == null || _txtAbStatus == null) return;

            int idx = _cmbAbPreset.SelectedIndex;
            BlocklistPreset preset = (idx >= 0 && idx < BlocklistPreset.Presets.Length)
                ? BlocklistPreset.Presets[idx]
                : BlocklistPreset.Presets[0];

            bool isStarter = string.Equals(preset.Id, "starter", StringComparison.OrdinalIgnoreCase);
            bool isCustom = string.Equals(preset.Id, "custom", StringComparison.OrdinalIgnoreCase);

            if (_abCustomUrlPanel != null)
            {
                _abCustomUrlPanel.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
            }

            _btnAbUpdate.IsEnabled = !isStarter;
            _btnAbUpdate.Content = isStarter ? T("ab_builtin") : T("ab_update");
            _btnAbUpdate.Background = isStarter ? WpfTheme.BgCardAlt : WpfTheme.AccentBlue;

            string counts = string.Format(T("ab_rules_count"), _proxy.ActiveBlockRuleCount, _proxy.ActiveAllowRuleCount);
            if (_config.EnableAdBlock)
            {
                _txtAbStatus.Foreground = WpfTheme.AccentGreen;
                _txtAbStatus.Text = "\u25CF  " + counts;
            }
            else
            {
                _txtAbStatus.Foreground = WpfTheme.AccentAmber;
                _txtAbStatus.Text = "\u23F8  " + T("ab_paused") + " \u2022 " + counts;
            }
        }

        private void HandleAdBlockPresetChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_updatingUi) return;
            int idx = _cmbAbPreset.SelectedIndex;
            if (idx >= 0 && idx < BlocklistPreset.Presets.Length)
            {
                _config.BlocklistPreset = BlocklistPreset.Presets[idx].Id;
                if (_tbAbCustomUrl != null)
                {
                    _config.CustomBlockUrl = (_tbAbCustomUrl.Text ?? string.Empty).Trim();
                }
                _config.Save();
                _proxy.UpdateSettings(_config);
                RefreshAdBlockStatusLabel();
            }
        }

        private async void HandleUpdateBlocklistClick(object sender, RoutedEventArgs e)
        {
            int idx = _cmbAbPreset.SelectedIndex;
            BlocklistPreset preset = (idx >= 0 && idx < BlocklistPreset.Presets.Length)
                ? BlocklistPreset.Presets[idx]
                : BlocklistPreset.Presets[0];

            _config.BlocklistPreset = preset.Id;
            if (_tbAbCustomUrl != null)
            {
                _config.CustomBlockUrl = (_tbAbCustomUrl.Text ?? string.Empty).Trim();
            }
            _config.Save();

            string effectiveUrl = BlocklistPreset.ResolveEffectiveUrl(preset.Id, _config.CustomBlockUrl);
            if (string.IsNullOrEmpty(effectiveUrl))
            {
                _proxy.UpdateSettings(_config);
                RefreshAdBlockStatusLabel();
                return;
            }

            _btnAbUpdate.IsEnabled = false;
            _cmbAbPreset.IsEnabled = false;
            _txtAbStatus.Foreground = WpfTheme.TextSecondary;
            _txtAbStatus.Text = "Downloading " + preset.DisplayName + "...";

            try
            {
                Tuple<bool, int, string> res = await DomainBlocklist.DownloadPresetToCacheAsync(effectiveUrl, CancellationToken.None);
                _proxy.UpdateSettings(_config);

                if (res.Item1)
                {
                    RefreshAdBlockStatusLabel();
                }
                else
                {
                    _txtAbStatus.Foreground = WpfTheme.AccentRose;
                    _txtAbStatus.Text = "Error: " + res.Item3;
                }
            }
            finally
            {
                _cmbAbPreset.IsEnabled = true;
                int curIdx = _cmbAbPreset.SelectedIndex;
                BlocklistPreset cur = (curIdx >= 0 && curIdx < BlocklistPreset.Presets.Length)
                    ? BlocklistPreset.Presets[curIdx]
                    : BlocklistPreset.Presets[0];
                _btnAbUpdate.IsEnabled = !string.Equals(cur.Id, "starter", StringComparison.OrdinalIgnoreCase);
            }
        }

        private void HandleQuickAddBlockRule(object sender, RoutedEventArgs e)
        {
            string entry = (_tbAbQuickAdd.Text ?? string.Empty).Trim();
            if (entry.Length == 0) return;

            if (_tbBlockRules.Text.Length > 0 && !_tbBlockRules.Text.EndsWith("\n", StringComparison.Ordinal))
            {
                _tbBlockRules.AppendText(Environment.NewLine);
            }
            _tbBlockRules.AppendText(entry);
            _tbAbQuickAdd.Clear();
            HandleSaveBlockRules(sender, e);
        }

        private void HandleSaveBlockRules(object sender, RoutedEventArgs e)
        {
            string[] lines = _tbBlockRules.Text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            _config.CustomBlockRules = new List<string>(lines);
            if (_tbAbCustomUrl != null)
            {
                _config.CustomBlockUrl = (_tbAbCustomUrl.Text ?? string.Empty).Trim();
            }
            _config.Save();
            _proxy.UpdateSettings(_config);
            _txtAbStatus.Foreground = WpfTheme.AccentGreen;
            _txtAbStatus.Text = T("ab_saved") + " (" + string.Format(T("ab_rules_count"), _proxy.ActiveBlockRuleCount, _proxy.ActiveAllowRuleCount) + ")";
        }

        private void HandleQuickAddWhitelist(object sender, RoutedEventArgs e)
        {
            string entry = (_tbWlQuickAdd.Text ?? string.Empty).Trim();
            if (entry.Length == 0) return;

            if (_tbWhitelist.Text.Length > 0 && !_tbWhitelist.Text.EndsWith("\n", StringComparison.Ordinal))
            {
                _tbWhitelist.AppendText(Environment.NewLine);
            }
            _tbWhitelist.AppendText(entry);
            _tbWlQuickAdd.Clear();
            HandleSaveWhitelist(sender, e);
        }

        private void HandleSaveWhitelist(object sender, RoutedEventArgs e)
        {
            string[] lines = _tbWhitelist.Text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            _config.Whitelist = new List<string>(lines);
            _config.Save();
            _proxy.UpdateSettings(_config);

            if (_isProtected && _config.ApplySystemProxy)
            {
                string warn;
                WindowsNetworkHelper.ApplyProtection(_config, _proxy.ListenPort, out warn);
            }

            _txtWlSaved.Text = T("wl_saved");
        }

        private void HandleSaveSettings(object sender, RoutedEventArgs e)
        {
            int port;
            if (int.TryParse(_tbPort.Text, out port)) _config.ProxyPort = port;
            int chunk;
            if (int.TryParse(_tbChunk.Text, out chunk)) _config.ChunkSize = chunk;
            int delay;
            if (int.TryParse(_tbDelay.Text, out delay)) _config.ChunkDelayMs = delay;

            _config.CustomDohUrl = (_tbCustomDoh.Text ?? string.Empty).Trim();
            _config.MinimizeToTray = _chkMinToTray.IsChecked == true;
            _config.StartWithWindows = _chkStartup.IsChecked == true;
            _config.AutoProtectOnLaunch = _chkAutoProtect.IsChecked == true;
            _config.LogDomainsInUi = _chkLogDomains.IsChecked == true;
            _config.Save();

            _tbPort.Text = _config.ProxyPort.ToString();
            _tbChunk.Text = _config.ChunkSize.ToString();
            _tbDelay.Text = _config.ChunkDelayMs.ToString();

            WindowsNetworkHelper.SetStartWithWindows(_config.StartWithWindows);
            _resolver.SetProvider(DohProvider.FindById(_config.DohProviderId, _config.CustomDohUrl));
            _proxy.UpdateSettings(_config);
            _txtSettingsStatus.Text = T("set_saved");
        }

        private void HandleProxyLog(string msg)
        {
            string line = DateTime.Now.ToString("HH:mm:ss") + "  " + msg;
            lock (_logLock)
            {
                if (_pendingLogs.Count > MaxLogEntries)
                {
                    _pendingLogs.Dequeue();
                }
                _pendingLogs.Enqueue(line);
            }
        }

        private void HandleStatsTick(object sender, EventArgs e)
        {
            if (!IsVisible) return;

            _txtStatProtectedVal.Text = _proxy.ProtectedConnections.ToString();
            _txtStatBlockedVal.Text = _proxy.BlockedConnections.ToString();
            _txtStatDohVal.Text = string.Format("{0} ({1})", _resolver.TotalQueries, _resolver.CacheHits);
            _txtStatExcludedVal.Text = _proxy.ExcludedConnections.ToString();

            List<string> batch = null;
            lock (_logLock)
            {
                if (_pendingLogs.Count > 0)
                {
                    batch = new List<string>(_pendingLogs);
                    _pendingLogs.Clear();
                }
            }

            if (batch != null && batch.Count > 0)
            {
                _logHistory.AddRange(batch);
                if (_logHistory.Count > MaxLogEntries)
                {
                    _logHistory.RemoveRange(0, _logHistory.Count - MaxLogEntries);
                }
                _tbLog.Text = string.Join(Environment.NewLine, _logHistory.ToArray());
                _tbLog.ScrollToEnd();
            }
        }

        private void ScheduleTrayMemoryTrim()
        {
            if (_trimTimer == null)
            {
                _trimTimer = new DispatcherTimer(DispatcherPriority.Normal);
                _trimTimer.Interval = TimeSpan.FromMilliseconds(400);
                _trimTimer.Tick += delegate(object s, EventArgs e)
                {
                    _trimTimer.Stop();
                    if (!IsVisible)
                    {
                        WindowsNetworkHelper.TrimMemory();
                    }
                };
            }
            _trimTimer.Stop();
            _trimTimer.Start();

            Task.Run(async delegate()
            {
                await Task.Delay(900).ConfigureAwait(false);
                bool stillHidden = false;
                try
                {
                    stillHidden = Dispatcher.Invoke(delegate() { return !IsVisible; });
                }
                catch
                {
                }
                if (stillHidden)
                {
                    WindowsNetworkHelper.TrimMemory();
                }
            });
        }

        private void RestoreFromTray()
        {
            if (_trimTimer != null) _trimTimer.Stop();
            ShowInTaskbar = true;
            Show();
            WindowState = WindowState.Normal;
            Activate();
            HandleStatsTick(this, EventArgs.Empty);
            if (_statsTimer != null) _statsTimer.Start();
        }

        private void ExitCompletely()
        {
            _forceClose = true;
            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_forceClose && _config.MinimizeToTray)
            {
                e.Cancel = true;
                if (_statsTimer != null) _statsTimer.Stop();
                Hide();
                ShowInTaskbar = false;
                ScheduleTrayMemoryTrim();
                if (!_trayHintShown && _trayIcon != null)
                {
                    _trayHintShown = true;
                    _trayIcon.ShowBalloonTip(2000, "NologGuard", T("tray_running"), Forms.ToolTipIcon.Info);
                }
                return;
            }

            ShutdownSafely();
            base.OnClosing(e);
        }

        private void ShutdownSafely()
        {
            if (_statsTimer != null)
            {
                _statsTimer.Stop();
                _statsTimer = null;
            }
            if (_pauseTimer != null)
            {
                _pauseTimer.Stop();
                _pauseTimer = null;
            }
            if (_trimTimer != null)
            {
                _trimTimer.Stop();
                _trimTimer = null;
            }

            if (_isProtected)
            {
                _proxy.Stop();
                WindowsNetworkHelper.RestoreAll();
                _isProtected = false;
            }

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }

            if (_proxy != null) { _proxy.Dispose(); }
            if (_resolver != null) { _resolver.Dispose(); }
            if (_iconOff != null) { _iconOff.Dispose(); }
            if (_iconOn != null) { _iconOn.Dispose(); }
        }
    }
}
