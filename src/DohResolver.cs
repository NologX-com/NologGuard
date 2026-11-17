using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NologGuard
{
    public interface IDnsResolver
    {
        Task<IPAddress[]> ResolveAsync(string host, CancellationToken ct);
    }

    /// <summary>
    /// Zero-dependency HTTP/2 DNS-over-HTTPS resolver built on Windows WinHTTP.
    /// Uses direct bootstrap IPs for built-in providers to eliminate plain-DNS leaks.
    /// </summary>
    public sealed class DohResolver : IDnsResolver, IDisposable
    {
        private static readonly IPAddress[] EmptyAddresses = new IPAddress[0];

        private readonly ConcurrentDictionary<string, CacheEntry> _cache =
            new ConcurrentDictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, Task<IPAddress[]>> _inflight =
            new ConcurrentDictionary<string, Task<IPAddress[]>>(StringComparer.OrdinalIgnoreCase);

        private sealed class CacheEntry
        {
            public IPAddress[] Addresses;
            public DateTime ExpiresUtc;
        }

        private IntPtr _sessionHandle;
        private readonly object _providerLock = new object();
        private DohProvider _provider;
        private long _totalQueries;
        private long _cacheHits;
        private bool _disposed;

        public DohResolver(DohProvider provider)
        {
            _provider = provider ?? DohProvider.Presets[0];
            _sessionHandle = WinHttpHelper.CreateHttp2Session(AppConfig.UserAgent, 4000, 5000);
        }

        public long TotalQueries { get { return Interlocked.Read(ref _totalQueries); } }
        public long CacheHits { get { return Interlocked.Read(ref _cacheHits); } }

        public DohProvider CurrentProvider
        {
            get
            {
                lock (_providerLock) { return _provider; }
            }
        }

        public void SetProvider(DohProvider provider)
        {
            if (provider == null) return;
            lock (_providerLock)
            {
                if (!string.Equals(_provider.Url, provider.Url, StringComparison.OrdinalIgnoreCase))
                {
                    _provider = provider;
                    _cache.Clear();
                }
            }
        }

        public void ClearCache()
        {
            _cache.Clear();
        }

        public async Task<IPAddress[]> ResolveAsync(string host, CancellationToken ct)
        {
            string normalized = DomainMatcher.NormalizeHost(host);
            if (string.IsNullOrEmpty(normalized))
            {
                return new IPAddress[0];
            }

            IPAddress literalIp;
            if (IPAddress.TryParse(normalized, out literalIp))
            {
                return new IPAddress[] { literalIp };
            }

            if (DomainMatcher.IsLocalOnlyHostname(normalized))
            {
                return await ResolveSystemFallbackAsync(normalized).ConfigureAwait(false);
            }

            Interlocked.Increment(ref _totalQueries);

            CacheEntry cached;
            if (_cache.TryGetValue(normalized, out cached) && cached.ExpiresUtc > DateTime.UtcNow)
            {
                Interlocked.Increment(ref _cacheHits);
                return cached.Addresses;
            }

            Task<IPAddress[]> inflightTask = _inflight.GetOrAdd(
                normalized,
                delegate(string key) { return ResolveAndCacheAsync(key, ct); });

            try
            {
                return await inflightTask.ConfigureAwait(false);
            }
            finally
            {
                Task<IPAddress[]> removed;
                _inflight.TryRemove(normalized, out removed);
            }
        }

        public async Task<Tuple<bool, int, string>> ProbeLatencyAsync(CancellationToken ct)
        {
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                DohProvider provider = CurrentProvider;
                DnsWireCodec.DnsParseResult res = await QuerySingleTypeAsync(provider, "example.com", DnsWireCodec.TypeA, ct).ConfigureAwait(false);
                sw.Stop();
                if (res != null && res.Addresses.Count > 0)
                {
                    return Tuple.Create(true, (int)sw.ElapsedMilliseconds, res.Addresses[0].ToString());
                }
                return Tuple.Create(false, (int)sw.ElapsedMilliseconds, "No DNS answer");
            }
            catch (Exception ex)
            {
                sw.Stop();
                return Tuple.Create(false, (int)sw.ElapsedMilliseconds, ex.Message);
            }
        }

        private async Task<IPAddress[]> ResolveAndCacheAsync(string host, CancellationToken ct)
        {
            DohProvider provider = CurrentProvider;
            DnsWireCodec.DnsParseResult aRes = await QuerySingleTypeAsync(provider, host, DnsWireCodec.TypeA, ct).ConfigureAwait(false);

            List<IPAddress> list = new List<IPAddress>();
            int ttl = 60;
            bool cacheNegative = false;

            if (aRes != null && aRes.Addresses.Count > 0)
            {
                list.AddRange(aRes.Addresses);
                ttl = aRes.MinTtlSeconds;
            }
            else if (aRes != null && aRes.RCode == 3)
            {
                // NXDOMAIN: domain does not exist for any record type; skip redundant AAAA roundtrip.
                cacheNegative = true;
                ttl = 20;
            }
            else
            {
                DnsWireCodec.DnsParseResult aaaaRes = await QuerySingleTypeAsync(provider, host, DnsWireCodec.TypeAAAA, ct).ConfigureAwait(false);
                if (aaaaRes != null && aaaaRes.Addresses.Count > 0)
                {
                    list.AddRange(aaaaRes.Addresses);
                    ttl = aaaaRes.MinTtlSeconds;
                }
                else if (aaaaRes != null && (aaaaRes.RCode == 0 || aaaaRes.RCode == 3))
                {
                    cacheNegative = true;
                    ttl = 15;
                }
            }

            if (list.Count == 0)
            {
                if (cacheNegative)
                {
                    if (_cache.Count > 4096) PruneExpiredCache();
                    _cache[host] = new CacheEntry
                    {
                        Addresses = EmptyAddresses,
                        ExpiresUtc = DateTime.UtcNow.AddSeconds(ttl)
                    };
                }
                return EmptyAddresses;
            }

            if (_cache.Count > 4096)
            {
                PruneExpiredCache();
            }

            IPAddress[] result = list.ToArray();
            _cache[host] = new CacheEntry
            {
                Addresses = result,
                ExpiresUtc = DateTime.UtcNow.AddSeconds(ttl)
            };

            return result;
        }

        private void PruneExpiredCache()
        {
            DateTime now = DateTime.UtcNow;
            foreach (KeyValuePair<string, CacheEntry> kv in _cache)
            {
                if (kv.Value.ExpiresUtc <= now)
                {
                    CacheEntry ignored;
                    _cache.TryRemove(kv.Key, out ignored);
                }
            }

            if (_cache.Count > 4096)
            {
                _cache.Clear();
            }
        }

        private Task<DnsWireCodec.DnsParseResult> QuerySingleTypeAsync(
            DohProvider provider,
            string host,
            ushort qType,
            CancellationToken ct)
        {
            return Task.Run(delegate()
            {
                ct.ThrowIfCancellationRequested();
                byte[] query = DnsWireCodec.BuildQuery(host, qType);

                string[] targets = (provider.BootstrapIps != null && provider.BootstrapIps.Length > 0)
                    ? provider.BootstrapIps
                    : new string[] { provider.Host };

                Exception lastEx = null;
                for (int i = 0; i < targets.Length; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        if (_disposed || _sessionHandle == IntPtr.Zero)
                        {
                            throw new ObjectDisposedException("DohResolver");
                        }

                        string hostHeader = provider.Port == 443
                            ? provider.Host
                            : (provider.Host + ":" + provider.Port);
                        string headers = "Host: " + hostHeader + "\r\n" +
                                         "Content-Type: application/dns-message\r\n" +
                                         "Accept: application/dns-message\r\n";

                        string connectTarget = targets[i];
                        if (connectTarget.IndexOf(':') >= 0 && !connectTarget.StartsWith("[", StringComparison.Ordinal))
                        {
                            connectTarget = "[" + connectTarget + "]";
                        }

                        int status;
                        byte[] rawResp = WinHttpHelper.ExecuteRequest(
                            _sessionHandle,
                            "POST",
                            connectTarget,
                            provider.Port,
                            provider.Path,
                            headers,
                            query,
                            65536,
                            out status);

                        if (status != 200)
                        {
                            throw new IOException("DoH HTTP status " + status);
                        }

                        DnsWireCodec.DnsParseResult parsed = DnsWireCodec.ParseResponse(rawResp, qType);
                        if (parsed.RCode == 0 || parsed.RCode == 3)
                        {
                            return parsed;
                        }
                    }
                    catch (Exception ex)
                    {
                        lastEx = ex;
                    }
                }

                if (lastEx != null)
                {
                    throw lastEx;
                }

                return new DnsWireCodec.DnsParseResult
                {
                    RCode = -1,
                    Addresses = new List<IPAddress>(),
                    MinTtlSeconds = 60
                };
            }, ct);
        }

        private static async Task<IPAddress[]> ResolveSystemFallbackAsync(string host)
        {
            try
            {
                IPAddress[] addrs = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
                return addrs ?? new IPAddress[0];
            }
            catch
            {
                return new IPAddress[0];
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            WinHttpHelper.CloseHandle(_sessionHandle);
            _sessionHandle = IntPtr.Zero;
        }
    }
}
