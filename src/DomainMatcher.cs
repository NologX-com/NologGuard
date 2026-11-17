using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace NologGuard
{
    /// <summary>
    /// Thread-safe matcher for exclusion rules (exact domains, wildcards, and private networks).
    /// </summary>
    public sealed class DomainMatcher
    {
        private readonly HashSet<string> _exactRules;
        private readonly List<string> _suffixRules;

        public DomainMatcher(IEnumerable<string> rawRules)
        {
            _exactRules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _suffixRules = new List<string>();

            if (rawRules == null) return;

            foreach (string raw in rawRules)
            {
                if (string.IsNullOrEmpty(raw)) continue;
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;

                int commentIdx = line.IndexOf(" #", StringComparison.Ordinal);
                if (commentIdx > 0) line = line.Substring(0, commentIdx).Trim();

                line = NormalizeHost(line);
                if (line.Length == 0) continue;

                if (line.StartsWith("*.", StringComparison.Ordinal))
                {
                    string baseDomain = line.Substring(2);
                    if (baseDomain.Length > 0)
                    {
                        _exactRules.Add(baseDomain);
                        _suffixRules.Add("." + baseDomain);
                    }
                }
                else
                {
                    _exactRules.Add(line);
                }
            }
        }

        public static string NormalizeHost(string host)
        {
            if (string.IsNullOrEmpty(host)) return string.Empty;
            string s = host.Trim().ToLowerInvariant();
            if (s.StartsWith("[", StringComparison.Ordinal) && s.EndsWith("]", StringComparison.Ordinal) && s.Length > 2)
            {
                s = s.Substring(1, s.Length - 2);
            }
            return s.TrimEnd('.');
        }

        public static bool IsPrivateOrLoopbackIp(IPAddress ip)
        {
            if (ip == null) return false;
            if (IPAddress.IsLoopback(ip)) return true;

            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] b = ip.GetAddressBytes();
                if (b[0] == 10) return true;
                if (b[0] == 127) return true;
                if (b[0] == 169 && b[1] == 254) return true;
                if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true;
                if (b[0] == 192 && b[1] == 168) return true;
                if (b[0] == 100 && b[1] >= 64 && b[1] <= 127) return true;
                return false;
            }

            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal) return true;
                byte[] b = ip.GetAddressBytes();
                if ((b[0] & 0xFE) == 0xFC) return true;
                return false;
            }

            return false;
        }

        public static bool IsLocalOnlyHostname(string host)
        {
            string h = NormalizeHost(host);
            if (h.Length == 0) return true;

            IPAddress ip;
            if (IPAddress.TryParse(h, out ip))
            {
                return IsPrivateOrLoopbackIp(ip);
            }

            if (h == "localhost" || !h.Contains(".")) return true;

            return h.EndsWith(".local", StringComparison.Ordinal) ||
                   h.EndsWith(".lan", StringComparison.Ordinal) ||
                   h.EndsWith(".internal", StringComparison.Ordinal) ||
                   h.EndsWith(".home.arpa", StringComparison.Ordinal) ||
                   h.EndsWith(".ts.net", StringComparison.Ordinal);
        }

        public bool IsExcluded(string host)
        {
            string h = NormalizeHost(host);
            if (h.Length == 0) return false;

            IPAddress ip;
            if (IPAddress.TryParse(h, out ip))
            {
                if (IsPrivateOrLoopbackIp(ip)) return true;
                return _exactRules.Contains(h);
            }

            if (h == "localhost") return true;
            if (_exactRules.Contains(h)) return true;

            for (int i = 0; i < _suffixRules.Count; i++)
            {
                if (h.EndsWith(_suffixRules[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public string BuildWindowsProxyOverride()
        {
            List<string> items = new List<string>();
            items.Add("<local>");
            items.Add("localhost");
            items.Add("127.*");
            items.Add("10.*");
            items.Add("192.168.*");
            items.Add("*.local");
            items.Add("*.ts.net");

            HashSet<string> seen = new HashSet<string>(items, StringComparer.OrdinalIgnoreCase);

            foreach (string exact in _exactRules)
            {
                if (!seen.Contains(exact) && !exact.Contains(":"))
                {
                    seen.Add(exact);
                    items.Add(exact);
                }
            }

            for (int i = 0; i < _suffixRules.Count; i++)
            {
                string pat = "*" + _suffixRules[i];
                if (!seen.Contains(pat))
                {
                    seen.Add(pat);
                    items.Add(pat);
                }
            }

            return string.Join(";", items.ToArray());
        }
    }
}
