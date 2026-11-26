using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;

namespace NologGuard
{
    public enum SniSplitMode
    {
        SmartSni = 0,
        FixedChunks = 1,
        Off = 2
    }

    public sealed class DohProvider
    {
        public string Id { get; private set; }
        public string DisplayName { get; private set; }
        public string Host { get; private set; }
        public ushort Port { get; private set; }
        public string Path { get; private set; }
        public string[] BootstrapIps { get; private set; }
        public string[] SystemDnsIpv4 { get; private set; }
        public string[] SystemDnsIpv6 { get; private set; }

        public DohProvider(
            string id,
            string displayName,
            string host,
            string path,
            string[] bootstrapIps,
            string[] systemDnsIpv4,
            string[] systemDnsIpv6)
            : this(id, displayName, host, 443, path, bootstrapIps, systemDnsIpv4, systemDnsIpv6)
        {
        }

        public DohProvider(
            string id,
            string displayName,
            string host,
            ushort port,
            string path,
            string[] bootstrapIps,
            string[] systemDnsIpv4,
            string[] systemDnsIpv6)
        {
            Id = id;
            DisplayName = displayName;
            Host = host;
            Port = port == 0 ? (ushort)443 : port;
            Path = path;
            BootstrapIps = bootstrapIps;
            SystemDnsIpv4 = systemDnsIpv4;
            SystemDnsIpv6 = systemDnsIpv6;
        }

        public string Url
        {
            get
            {
                string authority = Port == 443 ? Host : (Host + ":" + Port);
                return "https://" + authority + Path;
            }
        }

        public static readonly DohProvider[] Presets = new DohProvider[]
        {
            new DohProvider(
                "quad9",
                "Quad9 Security (9.9.9.9)",
                "dns.quad9.net",
                "/dns-query",
                new string[] { "9.9.9.9", "149.112.112.112", "2620:fe::fe", "2620:fe::9" },
                new string[] { "9.9.9.9", "149.112.112.112" },
                new string[] { "2620:fe::fe", "2620:fe::9" }),
            new DohProvider(
                "quad9-ecs",
                "Quad9 ECS (9.9.9.11)",
                "dns11.quad9.net",
                "/dns-query",
                new string[] { "9.9.9.11", "149.112.112.11", "2620:fe::11", "2620:fe::fe:11" },
                new string[] { "9.9.9.11", "149.112.112.11" },
                new string[] { "2620:fe::11", "2620:fe::fe:11" }),
            new DohProvider(
                "cloudflare",
                "Cloudflare (1.1.1.1)",
                "cloudflare-dns.com",
                "/dns-query",
                new string[] { "1.1.1.1", "1.0.0.1", "2606:4700:4700::1111", "2606:4700:4700::1001" },
                new string[] { "1.1.1.1", "1.0.0.1" },
                new string[] { "2606:4700:4700::1111", "2606:4700:4700::1001" }),
            new DohProvider(
                "google",
                "Google Public DNS (8.8.8.8)",
                "dns.google",
                "/dns-query",
                new string[] { "8.8.8.8", "8.8.4.4", "2001:4860:4860::8888", "2001:4860:4860::8844" },
                new string[] { "8.8.8.8", "8.8.4.4" },
                new string[] { "2001:4860:4860::8888", "2001:4860:4860::8844" }),
            new DohProvider(
                "adguard",
                "AdGuard DNS (Ad-blocking)",
                "dns.adguard-dns.com",
                "/dns-query",
                new string[] { "94.140.14.14", "94.140.15.15", "2a10:50c0::ad1:ff", "2a10:50c0::ad2:ff" },
                new string[] { "94.140.14.14", "94.140.15.15" },
                new string[] { "2a10:50c0::ad1:ff", "2a10:50c0::ad2:ff" })
        };

        public static DohProvider FindById(string id, string customUrl)
        {
            if (string.Equals(id, "custom", StringComparison.OrdinalIgnoreCase))
            {
                Uri uri;
                if (Uri.TryCreate(customUrl, UriKind.Absolute, out uri) &&
                    string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(uri.Host))
                {
                    ushort port = (ushort)(uri.Port > 0 && uri.Port <= 65535 ? uri.Port : 443);
                    string path = string.IsNullOrEmpty(uri.PathAndQuery) ? "/dns-query" : uri.PathAndQuery;
                    string cleanHost = DomainMatcher.NormalizeHost(uri.Host);

                    string[] boot = new string[0];
                    string[] sysV4 = new string[0];
                    string[] sysV6 = new string[0];

                    IPAddress ipLit;
                    if (IPAddress.TryParse(cleanHost, out ipLit))
                    {
                        string ipStr = ipLit.ToString();
                        boot = new string[] { ipStr };
                        if (port == 443)
                        {
                            if (ipLit.AddressFamily == AddressFamily.InterNetwork)
                            {
                                sysV4 = new string[] { ipStr };
                            }
                            else if (ipLit.AddressFamily == AddressFamily.InterNetworkV6)
                            {
                                sysV6 = new string[] { ipStr };
                            }
                        }
                    }

                    string labelHost = port == 443 ? uri.Host : (uri.Host + ":" + port);
                    return new DohProvider(
                        "custom",
                        "Custom (" + labelHost + ")",
                        uri.Host,
                        port,
                        path,
                        boot,
                        sysV4,
                        sysV6);
                }
            }

            for (int i = 0; i < Presets.Length; i++)
            {
                if (string.Equals(Presets[i].Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return Presets[i];
                }
            }

            return Presets[0];
        }
    }

    [DataContract]
    public sealed class AppConfig
    {
        public const string AppVersion = "1.2.4";
        public const string UserAgent = "NologGuard/1.2.4 (NologX.com)";
        public const string GitHubRepo = "NologX-com/NologGuard";

        [DataMember] public string Language { get; set; }
        [DataMember] public int ProxyPort { get; set; }
        [DataMember] public string DohProviderId { get; set; }
        [DataMember] public string CustomDohUrl { get; set; }
        [DataMember] public int SniSplitModeValue { get; set; }
        [DataMember] public int ChunkSize { get; set; }
        [DataMember] public int ChunkDelayMs { get; set; }
        [DataMember] public bool ApplySystemProxy { get; set; }
        [DataMember] public bool ApplySystemDoh { get; set; }
        [DataMember] public bool BlockQuicFirewall { get; set; }
        [DataMember] public bool MinimizeToTray { get; set; }
        [DataMember] public bool StartWithWindows { get; set; }
        [DataMember] public bool AutoProtectOnLaunch { get; set; }
        [DataMember] public bool LogDomainsInUi { get; set; }
        [DataMember] public bool EnableAdBlock { get; set; }
        [DataMember] public string BlocklistPreset { get; set; }
        [DataMember] public string CustomBlockUrl { get; set; }
        [DataMember] public List<string> CustomBlockRules { get; set; }
        [DataMember] public List<string> Whitelist { get; set; }

        public SniSplitMode SplitMode
        {
            get
            {
                if (Enum.IsDefined(typeof(SniSplitMode), SniSplitModeValue))
                {
                    return (SniSplitMode)SniSplitModeValue;
                }
                return SniSplitMode.SmartSni;
            }
            set { SniSplitModeValue = (int)value; }
        }

        public static string AppDataDir
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "NologGuard");
            }
        }

        public static string ConfigFilePath
        {
            get { return Path.Combine(AppDataDir, "config.json"); }
        }

        public static string BackupStatePath
        {
            get { return Path.Combine(AppDataDir, "backup_state.json"); }
        }

        public static List<string> CreateDefaultWhitelist()
        {
            return new List<string>
            {
                "# Local & LAN exclusions (always bypassed automatically)",
                "localhost",
                "127.0.0.1",
                "::1",
                "*.local",
                "*.lan",
                "*.internal",
                "*.ts.net",
                "# Add banking / trusted domains below if needed, e.g.:",
                "# *.gov.vn"
            };
        }

        public static List<string> CreateDefaultBlockRules()
        {
            return new List<string>
            {
                "# Custom AdBlock / Anti-Tracking rules (on top of selected preset)",
                "# Add a domain to block it, or prefix with @@ to allow/unblock:",
                "# ads.example.com",
                "@@googleapis.com"
            };
        }

        public static AppConfig CreateDefault()
        {
            AppConfig cfg = new AppConfig();
            cfg.Language = Strings.ResolveSystemLanguage();
            cfg.ProxyPort = 8899;
            cfg.DohProviderId = "quad9";
            cfg.CustomDohUrl = "https://dns.quad9.net/dns-query";
            cfg.SplitMode = SniSplitMode.SmartSni;
            cfg.ChunkSize = 2;
            cfg.ChunkDelayMs = 2;
            cfg.ApplySystemProxy = true;
            cfg.ApplySystemDoh = false;
            cfg.BlockQuicFirewall = false;
            cfg.MinimizeToTray = true;
            cfg.StartWithWindows = false;
            cfg.AutoProtectOnLaunch = false;
            cfg.LogDomainsInUi = true;
            cfg.EnableAdBlock = true;
            cfg.BlocklistPreset = "starter";
            cfg.CustomBlockUrl = "https://raw.githubusercontent.com/bigdargon/hostsVN/master/hosts";
            cfg.CustomBlockRules = CreateDefaultBlockRules();
            cfg.Whitelist = CreateDefaultWhitelist();
            return cfg;
        }

        public void Normalize()
        {
            if (!Strings.IsSupported(Language)) Language = "en";
            if (ProxyPort < 1024 || ProxyPort > 65535) ProxyPort = 8899;
            if (string.IsNullOrEmpty(DohProviderId)) DohProviderId = "quad9";
            if (string.IsNullOrEmpty(CustomDohUrl)) CustomDohUrl = "https://dns.quad9.net/dns-query";
            if (ChunkSize < 1 || ChunkSize > 64) ChunkSize = 2;
            if (ChunkDelayMs < 0 || ChunkDelayMs > 50) ChunkDelayMs = 2;
            if (string.IsNullOrEmpty(BlocklistPreset))
            {
                BlocklistPreset = "starter";
                EnableAdBlock = true;
            }
            if (string.IsNullOrEmpty(CustomBlockUrl))
            {
                CustomBlockUrl = "https://raw.githubusercontent.com/bigdargon/hostsVN/master/hosts";
            }
            if (CustomBlockRules == null) CustomBlockRules = CreateDefaultBlockRules();
            if (Whitelist == null) Whitelist = CreateDefaultWhitelist();
        }

        public static AppConfig Load(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return CreateDefault();
                }

                byte[] raw = File.ReadAllBytes(filePath);
                using (MemoryStream ms = new MemoryStream(raw))
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(AppConfig));
                    AppConfig cfg = ser.ReadObject(ms) as AppConfig;
                    if (cfg == null) return CreateDefault();
                    cfg.Normalize();
                    return cfg;
                }
            }
            catch
            {
                return CreateDefault();
            }
        }

        public static AppConfig Load()
        {
            return Load(ConfigFilePath);
        }

        public void Save(string filePath)
        {
            Normalize();
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(AppConfig));
                ser.WriteObject(ms, this);
                File.WriteAllBytes(filePath, ms.ToArray());
            }
        }

        public void Save()
        {
            Save(ConfigFilePath);
        }
    }

    /// <summary>
    /// Cross-elevation single-instance mutex helper with wait support during UAC relaunch.
    /// </summary>
    public sealed class SingleInstanceLock : IDisposable
    {
        public const string AppMutexName = @"Local\NologGuard_SingleInstance_Mutex";

        private static SingleInstanceLock _current;
        private Mutex _mutex;
        private bool _owns;

        public static SingleInstanceLock Current { get { return _current; } }

        private SingleInstanceLock(Mutex mutex, bool owns)
        {
            _mutex = mutex;
            _owns = owns;
        }

        public static SingleInstanceLock TryAcquire(string name, bool waitForPrevious, int maxWaitMs)
        {
            int elapsed = 0;
            while (true)
            {
                bool createdNew;
                Mutex m = new Mutex(true, name, out createdNew);
                if (createdNew)
                {
                    SingleInstanceLock lockObj = new SingleInstanceLock(m, true);
                    _current = lockObj;
                    return lockObj;
                }

                bool grabbed = false;
                try
                {
                    grabbed = m.WaitOne(0, false);
                }
                catch (AbandonedMutexException)
                {
                    grabbed = true;
                }
                catch
                {
                }

                if (grabbed)
                {
                    SingleInstanceLock lockObj = new SingleInstanceLock(m, true);
                    _current = lockObj;
                    return lockObj;
                }

                m.Dispose();

                if (!waitForPrevious || elapsed >= maxWaitMs)
                {
                    return null;
                }

                Thread.Sleep(50);
                elapsed += 50;
            }
        }

        public static void ReleaseCurrent()
        {
            SingleInstanceLock cur = _current;
            _current = null;
            if (cur != null)
            {
                cur.Dispose();
            }
        }

        public void Dispose()
        {
            Mutex m = _mutex;
            _mutex = null;
            if (m != null)
            {
                if (_owns)
                {
                    _owns = false;
                    try { m.ReleaseMutex(); } catch { }
                }
                try { m.Dispose(); } catch { }
            }
            if (object.ReferenceEquals(_current, this))
            {
                _current = null;
            }
        }
    }
}
