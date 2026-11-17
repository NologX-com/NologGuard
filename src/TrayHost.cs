using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace NologGuard
{
    /// <summary>
    /// Owns background proxy/DoH runtime, tray icon, pause timers, broadcast IPC window,
    /// and lazy MainWindow lifecycle so background memory stays minimal when minimized to tray.
    /// </summary>
    public sealed class TrayHost : IDisposable
    {
        public static int WmShowMe = 0;
        public static int WmExitApp = 0;

        public const int PauseModeNone = 0;
        public const int PauseModeAdBlock = 1;
        public const int PauseModeShield = 2;

        private const int WmQueryEndSession = 0x0011;
        private const int WmEndSession = 0x0016;
        private const int MaxLogEntries = 200;

        private readonly Dispatcher _dispatcher;
        private readonly AppConfig _config;
        private readonly DohResolver _resolver;
        private readonly ProxyServer _proxy;
        private readonly bool _autoProtectOnStart;

        private readonly object _logLock = new object();
        private readonly List<string> _logHistory = new List<string>();

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
        private BroadcastWindow _broadcastWindow;

        private MainWindow _window;
        private DispatcherTimer _pauseTimer;
        private DispatcherTimer _trimTimer;
        private DispatcherTimer _idleTrimTimer;

        private bool _isProtected;
        private bool _isShuttingDown;
        private bool _trayHintShown;
        private bool _disposed;
        private string _lastWarning = string.Empty;

        private int _pauseMode = PauseModeNone;
        private bool _pausedSavedAdBlock = true;
        private DateTime _pauseUntilUtc = DateTime.MinValue;

        public event Action StateChanged;
        public event Action<string> LogAdded;

        public TrayHost(AppConfig config, bool autoProtectOnStart)
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _config = config ?? AppConfig.CreateDefault();
            _autoProtectOnStart = autoProtectOnStart;

            _resolver = new DohResolver(DohProvider.FindById(_config.DohProviderId, _config.CustomDohUrl));
            _proxy = new ProxyServer(_resolver, _config);
            _proxy.OnLog += AddLog;

            _iconOff = WpfTheme.CreateShieldIcon(false);
            _iconOn = WpfTheme.CreateShieldIcon(true);

            _broadcastWindow = new BroadcastWindow(this);
            InitializeTray();
            RefreshTrayLocalization();
        }

        public AppConfig Config { get { return _config; } }
        public DohResolver Resolver { get { return _resolver; } }
        public ProxyServer Proxy { get { return _proxy; } }
        public bool IsProtected { get { return _isProtected; } }
        public bool IsShuttingDown { get { return _isShuttingDown; } }
        public int PauseMode { get { return _pauseMode; } }
        public DateTime PauseUntilUtc { get { return _pauseUntilUtc; } }
        public string LastWarning { get { return _lastWarning; } }

        public System.Drawing.Icon GetIcon(bool active)
        {
            return active ? _iconOn : _iconOff;
        }

        public string T(string key)
        {
            return Strings.Get(_config.Language, key);
        }

        public void Start(bool startMinimized)
        {
            if (startMinimized)
            {
                ToggleProtection(true);
                ScheduleTrayMemoryTrim(false);
                StartIdleTrimTimer();
                return;
            }

            ShowMainWindow();
            if (_autoProtectOnStart || _config.AutoProtectOnLaunch)
            {
                ToggleProtection(true);
            }
        }

        public void ShowMainWindow()
        {
            if (_isShuttingDown) return;

            StopIdleTrimTimer();
            if (_trimTimer != null) _trimTimer.Stop();

            if (_window == null)
            {
                _window = new MainWindow(this);
                _window.Closed += HandleMainWindowClosed;
                if (Application.Current != null)
                {
                    Application.Current.MainWindow = _window;
                }
                _window.Show();
            }
            else
            {
                if (_window.WindowState == WindowState.Minimized)
                {
                    _window.WindowState = WindowState.Normal;
                }
                _window.Show();
            }
            _window.Activate();
        }

        public void NotifyMinimizedToTray()
        {
            if (!_trayHintShown && _trayIcon != null)
            {
                _trayHintShown = true;
                _trayIcon.ShowBalloonTip(2000, "NologGuard", T("tray_running"), Forms.ToolTipIcon.Info);
            }
        }

        public List<string> GetLogHistorySnapshot()
        {
            lock (_logLock)
            {
                return new List<string>(_logHistory);
            }
        }

        public void ClearLogs()
        {
            lock (_logLock)
            {
                _logHistory.Clear();
            }
        }

        public void AddLog(string msg)
        {
            string line = DateTime.Now.ToString("HH:mm:ss") + "  " + msg;
            lock (_logLock)
            {
                if (_logHistory.Count >= MaxLogEntries)
                {
                    _logHistory.RemoveRange(0, _logHistory.Count - MaxLogEntries + 1);
                }
                _logHistory.Add(line);
            }

            Action<string> handler = LogAdded;
            if (handler != null)
            {
                handler(line);
            }
        }

        public bool ToggleProtection(bool enable)
        {
            return ToggleProtectionInternal(enable, true);
        }

        public bool ToggleProtectionInternal(bool enable, bool clearPause)
        {
            if (clearPause) ClearPauseState();
            if (enable == _isProtected) return true;

            if (enable)
            {
                try
                {
                    DohProvider provider = DohProvider.FindById(_config.DohProviderId, _config.CustomDohUrl);
                    _resolver.SetProvider(provider);
                    _proxy.UpdateSettings(_config);
                    _proxy.Start(_config.ProxyPort);

                    string warning;
                    WindowsNetworkHelper.ApplyProtection(_config, _proxy.ListenPort, out warning);
                    _lastWarning = warning ?? string.Empty;
                    _isProtected = true;

                    if (!string.IsNullOrEmpty(_lastWarning))
                    {
                        AddLog("[WARN] " + _lastWarning);
                    }
                    AddLog(string.Format("[ON] Proxy 127.0.0.1:{0} | DoH: {1}", _proxy.ListenPort, provider.DisplayName));
                }
                catch (Exception ex)
                {
                    _proxy.Stop();
                    WindowsNetworkHelper.RestoreAll();
                    _isProtected = false;
                    _lastWarning = string.Empty;
                    if (_window != null)
                    {
                        MessageBox.Show(_window, ex.Message, "NologGuard Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (_trayIcon != null)
                    {
                        _trayIcon.ShowBalloonTip(3000, "NologGuard Error", ex.Message, Forms.ToolTipIcon.Error);
                    }
                    UpdateTrayStatus();
                    RaiseStateChanged();
                    return false;
                }
            }
            else
            {
                _proxy.Stop();
                WindowsNetworkHelper.RestoreAll();
                _isProtected = false;
                _lastWarning = string.Empty;
                AddLog("[OFF] Shield disabled and network settings restored.");
            }

            UpdateTrayStatus();
            RaiseStateChanged();
            return true;
        }

        public void ApplyConfigChanges(bool reapplySystemNetwork)
        {
            DohProvider provider = DohProvider.FindById(_config.DohProviderId, _config.CustomDohUrl);
            _resolver.SetProvider(provider);
            _proxy.UpdateSettings(_config);

            if (_isProtected && reapplySystemNetwork)
            {
                string warning;
                WindowsNetworkHelper.ApplyProtection(_config, _proxy.ListenPort, out warning);
                _lastWarning = warning ?? string.Empty;
            }

            UpdateTrayStatus();
            RaiseStateChanged();
        }

        public void PauseAdBlockTemporary(int minutes)
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

            _proxy.UpdateSettings(_config);
            EnsurePauseTimerRunning();
            UpdatePauseTrayUi();
            UpdateTrayStatus();
            RaiseStateChanged();
            AddLog(string.Format("[PAUSE] AdBlock paused for {0} min.", minutes));
        }

        public void PauseShieldTemporary(int minutes)
        {
            if (_pauseMode == PauseModeAdBlock)
            {
                _config.EnableAdBlock = _pausedSavedAdBlock;
                _config.Save();
            }

            _pauseMode = PauseModeShield;
            _pauseUntilUtc = DateTime.UtcNow.AddMinutes(minutes);
            ToggleProtectionInternal(false, false);
            EnsurePauseTimerRunning();
            UpdatePauseTrayUi();
            UpdateTrayStatus();
            RaiseStateChanged();
            AddLog(string.Format("[PAUSE] Shield paused for {0} min.", minutes));
        }

        public void ResumeFromPause()
        {
            int oldMode = _pauseMode;
            ClearPauseState();

            if (oldMode == PauseModeAdBlock)
            {
                _config.EnableAdBlock = _pausedSavedAdBlock;
                _config.Save();
                ApplyConfigChanges(false);
                AddLog("[RESUME] AdBlock auto-resumed.");
            }
            else if (oldMode == PauseModeShield)
            {
                ToggleProtectionInternal(true, false);
                AddLog("[RESUME] Shield auto-resumed.");
            }

            UpdatePauseTrayUi();
            UpdateTrayStatus();
            RaiseStateChanged();
        }

        public void ClearPauseState()
        {
            _pauseMode = PauseModeNone;
            _pauseUntilUtc = DateTime.MinValue;
            if (_pauseTimer != null) _pauseTimer.Stop();
            UpdatePauseTrayUi();
        }

        public void RefreshTrayLocalization()
        {
            if (_trayOpenItem == null) return;
            _trayOpenItem.Text = T("tray_open");
            _trayToggleItem.Text = T("tray_toggle");
            _trayPauseAb5Item.Text = T("tray_pause_ab5");
            _trayPauseShield15Item.Text = T("tray_pause_shield15");
            _trayResumeItem.Text = T("tray_resume");
            _trayExitItem.Text = T("tray_exit");

            UpdatePauseTrayUi();
            UpdateTrayStatus();
        }

        public bool RestartAsAdmin(Window ownerWindow, bool askConfirmation)
        {
            if (askConfirmation && ownerWindow != null)
            {
                MessageBoxResult res = MessageBox.Show(
                    ownerWindow,
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
                ShutdownAndExit();
                return true;
            }

            if (wasProtected) ToggleProtection(true);
            return false;
        }

        public void ShutdownAndExit()
        {
            if (_isShuttingDown) return;
            _isShuttingDown = true;

            if (_window != null)
            {
                try { _window.Close(); } catch { }
                _window = null;
            }

            Dispose();

            if (Application.Current != null)
            {
                Application.Current.Shutdown();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _isShuttingDown = true;

            if (_pauseTimer != null) { _pauseTimer.Stop(); _pauseTimer = null; }
            if (_trimTimer != null) { _trimTimer.Stop(); _trimTimer = null; }
            if (_idleTrimTimer != null) { _idleTrimTimer.Stop(); _idleTrimTimer = null; }

            if (_isProtected)
            {
                _proxy.Stop();
                WindowsNetworkHelper.RestoreAll();
                _isProtected = false;
            }

            if (_broadcastWindow != null)
            {
                _broadcastWindow.ReleaseHandleSafely();
                _broadcastWindow = null;
            }

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }

            if (_trayMenu != null) { _trayMenu.Dispose(); _trayMenu = null; }
            _proxy.OnLog -= AddLog;
            _proxy.Dispose();
            _resolver.Dispose();
            if (_iconOff != null) { _iconOff.Dispose(); _iconOff = null; }
            if (_iconOn != null) { _iconOn.Dispose(); _iconOn = null; }
        }

        private void HandleMainWindowClosed(object sender, EventArgs e)
        {
            MainWindow closed = sender as MainWindow;
            if (closed != null)
            {
                closed.Closed -= HandleMainWindowClosed;
            }

            if (ReferenceEquals(_window, sender))
            {
                _window = null;
            }

            if (Application.Current != null && ReferenceEquals(Application.Current.MainWindow, sender))
            {
                Application.Current.MainWindow = null;
            }

            if (!_isShuttingDown)
            {
                ScheduleTrayMemoryTrim(true);
                StartIdleTrimTimer();
            }
        }

        private void ScheduleTrayMemoryTrim(bool fullGcOnFirstPass)
        {
            if (_trimTimer == null)
            {
                _trimTimer = new DispatcherTimer(DispatcherPriority.Normal);
                _trimTimer.Interval = TimeSpan.FromMilliseconds(350);
                _trimTimer.Tick += delegate(object s, EventArgs e)
                {
                    _trimTimer.Stop();
                    if (_window == null && !_isShuttingDown)
                    {
                        WindowsNetworkHelper.TrimMemory(true);
                    }
                };
            }
            _trimTimer.Stop();
            _trimTimer.Start();

            if (!fullGcOnFirstPass)
            {
                WindowsNetworkHelper.TrimMemory(false);
            }
        }

        private void StartIdleTrimTimer()
        {
            if (_isShuttingDown) return;
            if (_idleTrimTimer == null)
            {
                _idleTrimTimer = new DispatcherTimer(DispatcherPriority.Background);
                _idleTrimTimer.Interval = TimeSpan.FromSeconds(15);
                _idleTrimTimer.Tick += delegate(object s, EventArgs e)
                {
                    if (_window != null || _isShuttingDown)
                    {
                        _idleTrimTimer.Stop();
                        return;
                    }
                    WindowsNetworkHelper.TrimMemory(false);
                };
            }
            _idleTrimTimer.Start();
        }

        private void StopIdleTrimTimer()
        {
            if (_idleTrimTimer != null)
            {
                _idleTrimTimer.Stop();
            }
        }

        private void InitializeTray()
        {
            _trayMenu = new Forms.ContextMenuStrip();
            _trayOpenItem = new Forms.ToolStripMenuItem();
            _trayOpenItem.Click += delegate(object s, EventArgs e) { _dispatcher.BeginInvoke(new Action(ShowMainWindow)); };

            _trayToggleItem = new Forms.ToolStripMenuItem();
            _trayToggleItem.Click += delegate(object s, EventArgs e) { _dispatcher.BeginInvoke(new Action(delegate() { ToggleProtection(!_isProtected); })); };

            _trayPauseAb5Item = new Forms.ToolStripMenuItem();
            _trayPauseAb5Item.Click += delegate(object s, EventArgs e) { _dispatcher.BeginInvoke(new Action(delegate() { PauseAdBlockTemporary(5); })); };

            _trayPauseShield15Item = new Forms.ToolStripMenuItem();
            _trayPauseShield15Item.Click += delegate(object s, EventArgs e) { _dispatcher.BeginInvoke(new Action(delegate() { PauseShieldTemporary(15); })); };

            _trayResumeItem = new Forms.ToolStripMenuItem();
            _trayResumeItem.Visible = false;
            _trayResumeItem.Click += delegate(object s, EventArgs e) { _dispatcher.BeginInvoke(new Action(ResumeFromPause)); };

            _trayExitItem = new Forms.ToolStripMenuItem();
            _trayExitItem.Click += delegate(object s, EventArgs e) { _dispatcher.BeginInvoke(new Action(ShutdownAndExit)); };

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
            _trayIcon.DoubleClick += delegate(object s, EventArgs e) { _dispatcher.BeginInvoke(new Action(ShowMainWindow)); };
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
                        UpdateTrayStatus();
                        RaiseStateChanged();
                    }
                };
            }
            _pauseTimer.Start();
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

        private void UpdateTrayStatus()
        {
            if (_trayIcon == null) return;

            if (_isProtected)
            {
                DohProvider p = DohProvider.FindById(_config.DohProviderId, _config.CustomDohUrl);
                _trayIcon.Icon = _iconOn;
                string txt = "NologGuard - Protected (" + p.Host + ")";
                if (_pauseMode == PauseModeAdBlock)
                {
                    int mins = Math.Max(1, (int)Math.Ceiling((_pauseUntilUtc - DateTime.UtcNow).TotalMinutes));
                    txt = "NologGuard - " + string.Format(T("tray_paused_msg"), mins);
                }
                _trayIcon.Text = txt.Length > 63 ? txt.Substring(0, 63) : txt;
            }
            else
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

        private void RaiseStateChanged()
        {
            Action handler = StateChanged;
            if (handler != null)
            {
                handler();
            }
        }

        /// <summary>
        /// Hidden top-level helper HWND that handles HWND_BROADCAST wake/exit messages
        /// and OS session shutdown events even while MainWindow is closed in tray.
        /// </summary>
        private sealed class BroadcastWindow : Forms.NativeWindow
        {
            private readonly TrayHost _owner;

            public BroadcastWindow(TrayHost owner)
            {
                _owner = owner;
                Forms.CreateParams cp = new Forms.CreateParams();
                cp.Caption = "NologGuardTrayHost";
                cp.Style = 0;
                cp.ExStyle = 0x80; // WS_EX_TOOLWINDOW
                CreateHandle(cp);
            }

            public void ReleaseHandleSafely()
            {
                if (Handle != IntPtr.Zero)
                {
                    DestroyHandle();
                }
            }

            protected override void WndProc(ref Forms.Message m)
            {
                if (WmShowMe != 0 && m.Msg == WmShowMe)
                {
                    _owner._dispatcher.BeginInvoke(new Action(_owner.ShowMainWindow));
                    m.Result = IntPtr.Zero;
                    return;
                }

                if (WmExitApp != 0 && m.Msg == WmExitApp)
                {
                    _owner._dispatcher.BeginInvoke(new Action(_owner.ShutdownAndExit));
                    m.Result = IntPtr.Zero;
                    return;
                }

                if (m.Msg == WmQueryEndSession)
                {
                    m.Result = new IntPtr(1);
                    return;
                }

                if (m.Msg == WmEndSession && m.WParam != IntPtr.Zero)
                {
                    WindowsNetworkHelper.RestoreAll();
                    _owner._dispatcher.BeginInvoke(new Action(_owner.ShutdownAndExit));
                    m.Result = IntPtr.Zero;
                    return;
                }

                base.WndProc(ref m);
            }
        }
    }
}
