using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace NologGuard
{
    public static class Program
    {
        private const int HwndBroadcast = 0xFFFF;

        [STAThread]
        public static void Main(string[] args)
        {
            bool startMinimized = false;
            bool recoverOnly = false;
            bool exitRunning = false;
            bool relaunchWait = false;
            bool enableShieldOnStart = false;

            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "--minimized", StringComparison.OrdinalIgnoreCase))
                    {
                        startMinimized = true;
                    }
                    else if (string.Equals(args[i], "--recover", StringComparison.OrdinalIgnoreCase))
                    {
                        recoverOnly = true;
                    }
                    else if (string.Equals(args[i], "--exit", StringComparison.OrdinalIgnoreCase))
                    {
                        exitRunning = true;
                    }
                    else if (string.Equals(args[i], "--relaunch", StringComparison.OrdinalIgnoreCase))
                    {
                        relaunchWait = true;
                    }
                    else if (string.Equals(args[i], "--enable-shield", StringComparison.OrdinalIgnoreCase))
                    {
                        enableShieldOnStart = true;
                    }
                }
            }

            TrayHost.WmShowMe = RegisterWindowMessage("WM_NOLOGGUARD_SHOWME");
            TrayHost.WmExitApp = RegisterWindowMessage("WM_NOLOGGUARD_EXIT");

            if (exitRunning)
            {
                if (TrayHost.WmExitApp != 0)
                {
                    PostMessage((IntPtr)HwndBroadcast, TrayHost.WmExitApp, IntPtr.Zero, IntPtr.Zero);
                    using (SingleInstanceLock waitExit = SingleInstanceLock.TryAcquire(
                        SingleInstanceLock.AppMutexName,
                        true,
                        3000))
                    {
                        // Wait briefly for running instance to cleanly disable protection and exit
                    }
                }
                return;
            }

            if (recoverOnly)
            {
                WindowsNetworkHelper.EmergencyReset();
                return;
            }

            using (SingleInstanceLock instanceLock = SingleInstanceLock.TryAcquire(
                SingleInstanceLock.AppMutexName,
                relaunchWait,
                5000))
            {
                if (instanceLock == null)
                {
                    if (TrayHost.WmShowMe != 0)
                    {
                        PostMessage((IntPtr)HwndBroadcast, TrayHost.WmShowMe, IntPtr.Zero, IntPtr.Zero);
                    }
                    return;
                }

                WindowsNetworkHelper.RecoverIfDirty();

                AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
                {
                    WindowsNetworkHelper.RestoreAll();
                };

                SystemEvents.SessionEnding += delegate(object s, SessionEndingEventArgs e)
                {
                    WindowsNetworkHelper.RestoreAll();
                };

                AppConfig config = AppConfig.Load();
                Application app = new Application();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                app.DispatcherUnhandledException += delegate(object s, DispatcherUnhandledExceptionEventArgs e)
                {
                    WindowsNetworkHelper.RestoreAll();
                };

                using (TrayHost host = new TrayHost(config, enableShieldOnStart))
                {
                    host.Start(startMinimized);
                    app.Run();
                }
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}
