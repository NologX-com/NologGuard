using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NologGuard
{
    public sealed class BlocklistPreset
    {
        public string Id { get; private set; }
        public string DisplayName { get; private set; }
        public string Url { get; private set; }

        public BlocklistPreset(string id, string displayName, string url)
        {
            Id = id;
            DisplayName = displayName;
            Url = url;
        }

        public static readonly BlocklistPreset[] Presets = new BlocklistPreset[]
        {
            new BlocklistPreset("starter", "Built-in Starter (Offline, Fast)", string.Empty),
            new BlocklistPreset("hagezi-light", "HaGeZi Multi LIGHT (Balanced)", "https://raw.githubusercontent.com/hagezi/dns-blocklists/main/adblock/light.txt"),
            new BlocklistPreset("hagezi-pro", "HaGeZi Multi PRO (Strict)", "https://raw.githubusercontent.com/hagezi/dns-blocklists/main/adblock/pro.txt"),
            new BlocklistPreset("oisd-small", "OISD Small (Safe & Clean)", "https://small.oisd.nl/"),
            new BlocklistPreset("stevenblack", "StevenBlack Unified Hosts", "https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts"),
            new BlocklistPreset("hostsvn", "hostsVN (Vietnam Ads & Trackers)", "https://raw.githubusercontent.com/bigdargon/hostsVN/master/hosts"),
            new BlocklistPreset("custom", "Custom Filter URL (https://...)", string.Empty)
        };

        public static BlocklistPreset FindById(string id)
        {
            for (int i = 0; i < Presets.Length; i++)
            {
                if (string.Equals(Presets[i].Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return Presets[i];
                }
            }
            return Presets[0];
        }

        public static string ResolveEffectiveUrl(string presetId, string customUrl)
        {
            if (string.Equals(presetId, "custom", StringComparison.OrdinalIgnoreCase))
            {
                return (customUrl ?? string.Empty).Trim();
            }
            return FindById(presetId).Url;
        }
    }

    /// <summary>
    /// Fast O(label-count) domain-level AdBlocker and Anti-Tracking engine supporting
    /// plain domains, hosts-file lines, AdBlock ||domain^ rules, and @@ exceptions.
    /// </summary>
    public sealed class DomainBlocklist
    {
        public static readonly string[] StarterDomains = new string[]
        {
            "doubleclick.net",
            "googlesyndication.com",
            "googleadservices.com",
            "adservice.google.com",
            "pagead2.googlesyndication.com",
            "2mdn.net",
            "adnxs.com",
            "criteo.com",
            "criteo.net",
            "taboola.com",
            "outbrain.com",
            "pubmatic.com",
            "rubiconproject.com",
            "openx.net",
            "casalemedia.com",
            "advertising.com",
            "adroll.com",
            "mgid.com",
            "popads.net",
            "popcash.net",
            "propellerads.com",
            "ads.yahoo.com",
            "ads.tiktok.com",
            "ads-api.twitter.com",
            "ads.pinterest.com",
            "adcolony.com",
            "unityads.unity3d.com",
            "applovin.com",
            "vungle.com",
            "inmobi.com",
            "google-analytics.com",
            "ssl.google-analytics.com",
            "analytics.google.com",
            "pixel.facebook.com",
            "an.facebook.com",
            "analytics.tiktok.com",
            "scorecardresearch.com",
            "quantserve.com",
            "hotjar.com",
            "hotjar.io",
            "clarity.ms",
            "mc.yandex.ru",
            "mixpanel.com",
            "amplitude.com",
            "segment.io",
            "segment.com",
            "branch.io",
            "appsflyer.com",
            "adjust.com",
            "app-measurement.com",
            "crashlytics.com",
            "bugsnag.com",
            "newrelic.com",
            "nr-data.net",
            "omtrdc.net",
            "demdex.net",
            "moatads.com",
            "doubleverify.com",
            "adsafeprotected.com",
            "telemetry.microsoft.com",
            "vortex.data.microsoft.com",
            "watson.telemetry.microsoft.com"
        };

        private readonly HashSet<string> _blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public int BlockedRuleCount { get { return _blocked.Count; } }
        public int AllowedRuleCount { get { return _allowed.Count; } }

        public static string CacheFilePath
        {
            get { return Path.Combine(AppConfig.AppDataDir, "blocklist_cache.txt"); }
        }

        public static DomainBlocklist Build(string presetId, IEnumerable<string> customRules)
        {
            DomainBlocklist bl = new DomainBlocklist();
            bl.LoadLines(StarterDomains);

            if (!string.Equals(presetId, "starter", StringComparison.OrdinalIgnoreCase) &&
                File.Exists(CacheFilePath))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(CacheFilePath, Encoding.UTF8))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            bl.AddRuleLine(line);
                        }
                    }
                }
                catch
                {
                }
            }

            if (customRules != null)
            {
                bl.LoadLines(customRules);
            }

            return bl;
        }

        public void LoadLines(IEnumerable<string> lines)
        {
            if (lines == null) return;
            foreach (string line in lines)
            {
                AddRuleLine(line);
            }
        }

        public void AddRuleLine(string rawLine)
        {
            if (string.IsNullOrEmpty(rawLine)) return;
            string line = rawLine.Trim();
            if (line.Length == 0) return;

            char first = line[0];
            if (first == '#' || first == '!' || first == '[' || first == '/') return;

            if (line.Contains("##") || line.Contains("#@#") || line.Contains("#?#")) return;

            int hashIdx = line.IndexOf('#');
            if (hashIdx > 0)
            {
                line = line.Substring(0, hashIdx).Trim();
                if (line.Length == 0) return;
            }

            bool isAllow = false;
            if (line.StartsWith("@@", StringComparison.Ordinal))
            {
                isAllow = true;
                line = line.Substring(2).Trim();
            }

            int caretIdx = line.IndexOf('^');
            if (caretIdx >= 0)
            {
                string afterCaret = line.Substring(caretIdx + 1).Trim();
                if (afterCaret.Length > 0 &&
                    !string.Equals(afterCaret, "$all", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(afterCaret, "$important", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(afterCaret, "$document", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                line = line.Substring(0, caretIdx);
            }
            else if (line.Contains("$") || line.Contains("/"))
            {
                return;
            }

            if (line.StartsWith("||", StringComparison.Ordinal))
            {
                line = line.Substring(2);
            }
            else if (line.StartsWith("*.", StringComparison.Ordinal))
            {
                line = line.Substring(2);
            }

            if (line.IndexOf(' ') >= 0 || line.IndexOf('\t') >= 0)
            {
                string[] parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) return;
                if (parts[0] != "0.0.0.0" && parts[0] != "127.0.0.1" && parts[0] != "::" && parts[0] != "::1")
                {
                    return;
                }
                line = parts[1];
            }

            string domain = DomainMatcher.NormalizeHost(line);
            if (!IsValidBlockDomain(domain)) return;

            if (isAllow)
            {
                _allowed.Add(domain);
            }
            else
            {
                _blocked.Add(domain);
            }
        }

        public bool IsBlocked(string host)
        {
            if (_blocked.Count == 0) return false;
            string h = DomainMatcher.NormalizeHost(host);
            if (h.Length == 0) return false;

            IPAddress ip;
            if (IPAddress.TryParse(h, out ip)) return false;
            if (DomainMatcher.IsLocalOnlyHostname(h)) return false;

            if (_allowed.Count > 0 && MatchesDomainOrParent(_allowed, h))
            {
                return false;
            }

            return MatchesDomainOrParent(_blocked, h);
        }

        private static bool MatchesDomainOrParent(HashSet<string> set, string normalizedHost)
        {
            if (set.Contains(normalizedHost)) return true;

            int dot = normalizedHost.IndexOf('.');
            while (dot >= 0 && dot + 1 < normalizedHost.Length)
            {
                string parent = normalizedHost.Substring(dot + 1);
                if (set.Contains(parent)) return true;
                dot = normalizedHost.IndexOf('.', dot + 1);
            }
            return false;
        }

        private static bool IsValidBlockDomain(string domain)
        {
            if (string.IsNullOrEmpty(domain) || domain.Length > 253) return false;
            if (!domain.Contains(".") || domain.Contains("*")) return false;
            if (domain == "localhost" || domain == "localhost.localdomain" ||
                domain == "local" || domain == "broadcasthost" ||
                domain == "ip6-localhost" || domain == "ip6-loopback")
            {
                return false;
            }

            IPAddress ip;
            if (IPAddress.TryParse(domain, out ip)) return false;

            for (int i = 0; i < domain.Length; i++)
            {
                char c = domain[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' || c == '.' || c == '_';
                if (!ok) return false;
            }
            return true;
        }

        public static async Task<Tuple<bool, int, string>> DownloadPresetToCacheAsync(string url, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(url))
            {
                return Tuple.Create(true, StarterDomains.Length, "Using built-in starter list.");
            }

            try
            {
                const int maxBytes = 25 * 1024 * 1024;
                byte[] bytes = await WinHttpHelper.GetBytesAsync(url, maxBytes, ct).ConfigureAwait(false);

                string dir = Path.GetDirectoryName(CacheFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllBytes(CacheFilePath, bytes);
                DomainBlocklist parsed = Build("custom", null);
                return Tuple.Create(true, parsed.BlockedRuleCount, "OK");
            }
            catch (Exception ex)
            {
                return Tuple.Create(false, 0, ex.Message);
            }
        }
    }
}
