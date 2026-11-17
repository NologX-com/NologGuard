using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Principal;
using Microsoft.Win32;

namespace NologGuard
{
    [DataContract]
    public sealed class AdapterDnsBackup
    {
        [DataMember] public string InterfaceName { get; set; }
        [DataMember] public string InterfaceGuid { get; set; }
        [DataMember] public string PrevIpv4NameServer { get; set; }
        [DataMember] public string PrevIpv6NameServer { get; set; }
    }

    [DataContract]
    public sealed class BackupState
    {
        [DataMember] public bool ProxyModified { get; set; }
        [DataMember] public int PrevProxyEnable { get; set; }
        [DataMember] public string PrevProxyServer { get; set; }
        [DataMember] public string PrevProxyOverride { get; set; }
        [DataMember] public int OurProxyPort { get; set; }
        [DataMember] public bool FirewallRuleAdded { get; set; }
        [DataMember] public bool SystemDohModified { get; set; }
        [DataMember] public List<AdapterDnsBackup> DnsBackups { get; set; }
    }

    /// <summary>
    /// Safely applies and restores Windows System Proxy, Firewall QUIC block rule,
    /// Windows 11 DoH configuration, startup registry entries, and tray memory trimming.
    /// </summary>
    public static class WindowsNetworkHelper
    {
        private const string InternetSettingsKey = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRunValueName = "NologGuard";
        private const string QuicRuleName = "NologGuard_Block_QUIC_UDP_443";
        private const int InternetOptionSettingsChanged = 39;
        private const int InternetOptionRefresh = 37;

        public static bool IsAdministrator()
        {
            try
            {
                using (WindowsIdentity id = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(id);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool RestartAsAdministrator(string extraArgs)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = Process.GetCurrentProcess().MainModule.FileName;
                psi.WorkingDirectory = Path.GetDirectoryName(psi.FileName);
                psi.Arguments = extraArgs ?? string.Empty;
                psi.UseShellExecute = true;
                psi.Verb = "runas";
                Process.Start(psi);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsWindows11OrNewer()
        {
            try
            {
                Version v = Environment.OSVersion.Version;
                return v.Major >= 10 && v.Build >= 22000;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Compacts GC heaps and releases excess working-set pages when minimized to system tray.
        /// </summary>
        public static void TrimMemory()
        {
            try
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
                GC.Collect();
                IntPtr self = GetCurrentProcess();
                SetProcessWorkingSetSize(self, new IntPtr(-1), new IntPtr(-1));
                EmptyWorkingSet(self);
            }
            catch
            {
            }
        }

        public static void ApplyProtection(AppConfig config, int activePort, out string warningMessage)
        {
            List<string> warnings = new List<string>();
            BackupState state = LoadBackupState() ?? new BackupState();

            if (config.ApplySystemProxy)
            {
                ApplySystemProxyInternal(state, activePort, new DomainMatcher(config.Whitelist).BuildWindowsProxyOverride());
            }

            if (config.BlockQuicFirewall)
            {
                if (IsAdministrator())
                {
                    if (SetQuicFirewallBlock(true))
                    {
                        state.FirewallRuleAdded = true;
                    }
                    else
                    {
                        warnings.Add("Could not add Windows Firewall QUIC rule.");
                    }
                }
                else
                {
                    warnings.Add("QUIC Firewall block skipped (Administrator required)");
                }
            }

            if (config.ApplySystemDoh)
            {
                if (IsAdministrator())
                {
                    DohProvider provider = DohProvider.FindById(config.DohProviderId, config.CustomDohUrl);
                    if (provider.SystemDnsIpv4 != null && provider.SystemDnsIpv4.Length > 0)
                    {
                        ApplySystemDohInternal(state, provider);
                    }
                }
                else
                {
                    warnings.Add("System-wide DoH skipped (Administrator required; proxy DoH active)");
                }
            }

            warningMessage = warnings.Count > 0 ? string.Join(" | ", warnings.ToArray()) : null;
            SaveBackupState(state);
        }

        public static void RestoreAll()
        {
            BackupState state = LoadBackupState();
            if (state != null)
            {
                if (state.ProxyModified)
                {
                    RestoreSystemProxyInternal(state);
                    state.ProxyModified = false;
                }

                bool admin = IsAdministrator();
                if (state.FirewallRuleAdded && admin)
                {
                    SetQuicFirewallBlock(false);
                    state.FirewallRuleAdded = false;
                }

                if (state.SystemDohModified && admin)
                {
                    RestoreSystemDohInternal(state);
                    state.SystemDohModified = false;
                }

                if (!state.ProxyModified && !state.FirewallRuleAdded && !state.SystemDohModified)
                {
                    DeleteBackupState();
                }
                else
                {
                    SaveBackupState(state);
                }
                return;
            }

            if (IsAdministrator())
            {
                SetQuicFirewallBlock(false);
            }
        }

        public static void RecoverIfDirty()
        {
            BackupState state = LoadBackupState();
            if (state == null) return;

            if (state.ProxyModified)
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(InternetSettingsKey, false))
                {
                    if (key != null)
                    {
                        string currentServer = key.GetValue("ProxyServer") as string ?? string.Empty;
                        if (currentServer.StartsWith("127.0.0.1:", StringComparison.Ordinal))
                        {
                            RestoreSystemProxyInternal(state);
                        }
                    }
                }
                state.ProxyModified = false;
            }

            bool admin = IsAdministrator();
            if (state.FirewallRuleAdded && admin)
            {
                SetQuicFirewallBlock(false);
                state.FirewallRuleAdded = false;
            }

            if (state.SystemDohModified && admin)
            {
                RestoreSystemDohInternal(state);
                state.SystemDohModified = false;
            }

            if (!state.ProxyModified && !state.FirewallRuleAdded && !state.SystemDohModified)
            {
                DeleteBackupState();
            }
            else
            {
                SaveBackupState(state);
            }
        }

        public static void EmergencyReset()
        {
            BackupState state = LoadBackupState();
            if (state != null)
            {
                RestoreAll();
            }
            else
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(InternetSettingsKey, true))
                {
                    if (key != null)
                    {
                        key.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
                    }
                }
                RefreshWinINet();
                if (IsAdministrator())
                {
                    SetQuicFirewallBlock(false);
                }
            }
        }

        public static void SetStartWithWindows(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key == null) return;
                    if (enabled)
                    {
                        string exe = Process.GetCurrentProcess().MainModule.FileName;
                        key.SetValue(AppRunValueName, "\"" + exe + "\" --minimized", RegistryValueKind.String);
                    }
                    else
                    {
                        if (key.GetValue(AppRunValueName) != null)
                        {
                            key.DeleteValue(AppRunValueName, false);
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private static void ApplySystemProxyInternal(BackupState state, int port, string proxyOverride)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(InternetSettingsKey, true))
            {
                if (key == null) return;

                string currentServer = key.GetValue("ProxyServer") as string ?? string.Empty;
                int currentEnable = 0;
                object enableObj = key.GetValue("ProxyEnable");
                if (enableObj is int) currentEnable = (int)enableObj;

                if (!state.ProxyModified && !currentServer.StartsWith("127.0.0.1:", StringComparison.Ordinal))
                {
                    state.PrevProxyEnable = currentEnable;
                    state.PrevProxyServer = currentServer;
                    state.PrevProxyOverride = key.GetValue("ProxyOverride") as string ?? string.Empty;
                }

                state.ProxyModified = true;
                state.OurProxyPort = port;
                SaveBackupState(state);

                key.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);
                key.SetValue("ProxyServer", "127.0.0.1:" + port, RegistryValueKind.String);
                key.SetValue("ProxyOverride", proxyOverride ?? "<local>", RegistryValueKind.String);
            }

            RefreshWinINet();
        }

        private static void RestoreSystemProxyInternal(BackupState state)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(InternetSettingsKey, true))
                {
                    if (key != null)
                    {
                        key.SetValue("ProxyEnable", state.PrevProxyEnable, RegistryValueKind.DWord);

                        if (string.IsNullOrEmpty(state.PrevProxyServer))
                        {
                            key.DeleteValue("ProxyServer", false);
                        }
                        else
                        {
                            key.SetValue("ProxyServer", state.PrevProxyServer, RegistryValueKind.String);
                        }

                        if (string.IsNullOrEmpty(state.PrevProxyOverride))
                        {
                            key.DeleteValue("ProxyOverride", false);
                        }
                        else
                        {
                            key.SetValue("ProxyOverride", state.PrevProxyOverride, RegistryValueKind.String);
                        }
                    }
                }
                RefreshWinINet();
            }
            catch
            {
            }
        }

        private static bool SetQuicFirewallBlock(bool enable)
        {
            RunNetsh("advfirewall firewall delete rule name=\"" + QuicRuleName + "\"");
            if (!enable)
            {
                return true;
            }
            int code = RunNetsh("advfirewall firewall add rule name=\"" + QuicRuleName + "\" dir=out action=block protocol=UDP remoteport=443 enable=yes");
            return code == 0;
        }

        private static void ApplySystemDohInternal(BackupState state, DohProvider provider)
        {
            List<NetworkInterface> adapters = FindActivePhysicalAdapters();
            if (adapters.Count == 0) return;

            if (!state.SystemDohModified)
            {
                state.DnsBackups = new List<AdapterDnsBackup>();
                for (int i = 0; i < adapters.Count; i++)
                {
                    NetworkInterface nic = adapters[i];
                    AdapterDnsBackup bak = new AdapterDnsBackup();
                    bak.InterfaceName = nic.Name;
                    bak.InterfaceGuid = nic.Id;
                    bak.PrevIpv4NameServer = ReadInterfaceNameServer(false, nic.Id);
                    bak.PrevIpv6NameServer = ReadInterfaceNameServer(true, nic.Id);
                    state.DnsBackups.Add(bak);
                }
                state.SystemDohModified = true;
                SaveBackupState(state);
            }

            RegisterDohTemplates(provider);

            for (int i = 0; i < adapters.Count; i++)
            {
                NetworkInterface nic = adapters[i];
                if (provider.SystemDnsIpv4 != null && provider.SystemDnsIpv4.Length > 0)
                {
                    SetAdapterDns(false, nic.Name, nic.Id, provider.SystemDnsIpv4, provider.Url);
                }
                if (provider.SystemDnsIpv6 != null && provider.SystemDnsIpv6.Length > 0)
                {
                    SetAdapterDns(true, nic.Name, nic.Id, provider.SystemDnsIpv6, provider.Url);
                }
            }
        }

        private static void RestoreSystemDohInternal(BackupState state)
        {
            if (state.DnsBackups == null) return;
            for (int i = 0; i < state.DnsBackups.Count; i++)
            {
                AdapterDnsBackup bak = state.DnsBackups[i];
                RestoreAdapterDns(false, bak.InterfaceName, bak.PrevIpv4NameServer);
                RestoreAdapterDns(true, bak.InterfaceName, bak.PrevIpv6NameServer);
            }
        }

        private static void RegisterDohTemplates(DohProvider provider)
        {
            List<string> list = new List<string>();
            if (provider.SystemDnsIpv4 != null) list.AddRange(provider.SystemDnsIpv4);
            if (provider.SystemDnsIpv6 != null) list.AddRange(provider.SystemDnsIpv6);
            string[] all = list.ToArray();

            for (int i = 0; i < all.Length; i++)
            {
                string ip = all[i];
                string setCmd = string.Format("dns set encryption server={0} dohtemplate={1} autoupgrade=yes udpfallback=no", ip, provider.Url);
                if (RunNetsh(setCmd) != 0)
                {
                    string addCmd = string.Format("dns add encryption server={0} dohtemplate={1} autoupgrade=yes udpfallback=no", ip, provider.Url);
                    RunNetsh(addCmd);
                }
            }
        }

        private static void SetAdapterDns(bool ipv6, string nicName, string nicGuid, string[] servers, string dohUrl)
        {
            string family = ipv6 ? "ipv6" : "ipv4";
            RunNetsh(string.Format("interface {0} set dnsservers name=\"{1}\" static {2} primary validate=no", family, nicName, servers[0]));
            for (int i = 1; i < servers.Length; i++)
            {
                RunNetsh(string.Format("interface {0} add dnsservers name=\"{1}\" {2} index={3} validate=no", family, nicName, servers[i], i + 1));
            }

            string sub = ipv6 ? "Doh6" : "Doh";
            for (int i = 0; i < servers.Length; i++)
            {
                string regPath = string.Format(
                    @"SYSTEM\CurrentControlSet\Services\Dnscache\InterfaceSpecificParameters\{0}\DohInterfaceSettings\{1}\{2}",
                    nicGuid, sub, servers[i]);
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.CreateSubKey(regPath))
                    {
                        if (key != null)
                        {
                            key.SetValue("DohFlags", 17L, RegistryValueKind.QWord);
                            key.SetValue("DohTemplate", dohUrl, RegistryValueKind.String);
                        }
                    }
                }
                catch
                {
                }
            }
        }

        private static void RestoreAdapterDns(bool ipv6, string nicName, string prevNameServer)
        {
            string family = ipv6 ? "ipv6" : "ipv4";
            if (string.IsNullOrEmpty(prevNameServer))
            {
                RunNetsh(string.Format("interface {0} set dnsservers name=\"{1}\" source=dhcp", family, nicName));
                return;
            }

            string[] servers = prevNameServer.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (servers.Length == 0)
            {
                RunNetsh(string.Format("interface {0} set dnsservers name=\"{1}\" source=dhcp", family, nicName));
                return;
            }

            RunNetsh(string.Format("interface {0} set dnsservers name=\"{1}\" static {2} primary validate=no", family, nicName, servers[0]));
            for (int i = 1; i < servers.Length; i++)
            {
                RunNetsh(string.Format("interface {0} add dnsservers name=\"{1}\" {2} index={3} validate=no", family, nicName, servers[i], i + 1));
            }
        }

        private static string ReadInterfaceNameServer(bool ipv6, string interfaceGuid)
        {
            string sub = ipv6
                ? @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters\Interfaces\" + interfaceGuid
                : @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\" + interfaceGuid;
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(sub, false))
                {
                    if (key == null) return string.Empty;
                    return (key.GetValue("NameServer") as string) ?? string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static List<NetworkInterface> FindActivePhysicalAdapters()
        {
            List<NetworkInterface> list = new List<NetworkInterface>();
            NetworkInterface[] all = NetworkInterface.GetAllNetworkInterfaces();
            for (int i = 0; i < all.Length; i++)
            {
                NetworkInterface nic = all[i];
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                string desc = (nic.Description ?? string.Empty).ToLowerInvariant();
                string name = (nic.Name ?? string.Empty).ToLowerInvariant();
                if (desc.Contains("vmware") || desc.Contains("virtual") || desc.Contains("hyper-v") ||
                    desc.Contains("tailscale") || name.Contains("tailscale") || desc.Contains("tap-"))
                {
                    continue;
                }

                IPInterfaceProperties props = nic.GetIPProperties();
                if (props.GatewayAddresses != null && props.GatewayAddresses.Count > 0)
                {
                    list.Add(nic);
                }
            }
            return list;
        }

        private static int RunNetsh(string args)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("netsh.exe", args);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                using (Process proc = Process.Start(psi))
                {
                    proc.WaitForExit(5000);
                    return proc.ExitCode;
                }
            }
            catch
            {
                return -1;
            }
        }

        private static void RefreshWinINet()
        {
            InternetSetOption(IntPtr.Zero, InternetOptionSettingsChanged, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, InternetOptionRefresh, IntPtr.Zero, 0);
        }

        private static BackupState LoadBackupState()
        {
            try
            {
                string path = AppConfig.BackupStatePath;
                if (!File.Exists(path)) return null;
                byte[] raw = File.ReadAllBytes(path);
                using (MemoryStream ms = new MemoryStream(raw))
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(BackupState));
                    return ser.ReadObject(ms) as BackupState;
                }
            }
            catch
            {
                return null;
            }
        }

        private static void SaveBackupState(BackupState state)
        {
            try
            {
                string path = AppConfig.BackupStatePath;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                using (MemoryStream ms = new MemoryStream())
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(BackupState));
                    ser.WriteObject(ms, state);
                    File.WriteAllBytes(path, ms.ToArray());
                }
            }
            catch
            {
            }
        }

        private static void DeleteBackupState()
        {
            try
            {
                if (File.Exists(AppConfig.BackupStatePath))
                {
                    File.Delete(AppConfig.BackupStatePath);
                }
            }
            catch
            {
            }
        }

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);
    }
}
