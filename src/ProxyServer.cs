using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NologGuard
{
    /// <summary>
    /// High-performance local HTTP/HTTPS CONNECT proxy that resolves domains over DoH,
    /// blocks ads/trackers before DNS lookup, bypasses excluded domains, and splits TLS SNI.
    /// </summary>
    public sealed class ProxyServer : IDisposable
    {
        private static readonly byte[] OkConnectResponseBytes =
            Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");

        private static readonly ConcurrentBag<byte[]> SmallRelayPool = new ConcurrentBag<byte[]>();
        private static int _smallRelayPoolCount;
        private const int SmallRelayBufSize = 8192;
        private const int MaxSmallRelayPool = 32;

        private static readonly ConcurrentBag<byte[]> LargeScratchPool = new ConcurrentBag<byte[]>();
        private static int _largeScratchPoolCount;
        private const int LargeScratchBufSize = 32768;
        private const int MaxLargeScratchPool = 16;

        private readonly IDnsResolver _resolver;
        private readonly object _configLock = new object();

        private DomainMatcher _matcher;
        private DomainBlocklist _blocklist;
        private string _blocklistSignature;
        private bool _enableAdBlock;
        private SniSplitMode _splitMode;
        private int _chunkSize;
        private int _chunkDelayMs;
        private bool _logDomainsInUi;

        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private Task _acceptLoop;

        private long _activeConnections;
        private long _protectedConnections;
        private long _blockedConnections;
        private long _excludedConnections;

        public event Action<string> OnLog;

        public ProxyServer(IDnsResolver resolver, AppConfig config)
        {
            _resolver = resolver;
            UpdateSettings(config);
        }

        public bool IsRunning { get { return _listener != null && _cts != null && !_cts.IsCancellationRequested; } }
        public int ListenPort { get; private set; }
        public long ActiveConnections { get { return Interlocked.Read(ref _activeConnections); } }
        public long ProtectedConnections { get { return Interlocked.Read(ref _protectedConnections); } }
        public long BlockedConnections { get { return Interlocked.Read(ref _blockedConnections); } }
        public long ExcludedConnections { get { return Interlocked.Read(ref _excludedConnections); } }

        public int ActiveBlockRuleCount
        {
            get
            {
                lock (_configLock)
                {
                    return _blocklist != null ? _blocklist.BlockedRuleCount : 0;
                }
            }
        }

        public int ActiveAllowRuleCount
        {
            get
            {
                lock (_configLock)
                {
                    return _blocklist != null ? _blocklist.AllowedRuleCount : 0;
                }
            }
        }

        public bool IsAdBlockHostBlocked(string host)
        {
            lock (_configLock)
            {
                return _enableAdBlock && _blocklist != null && _blocklist.IsBlocked(host);
            }
        }

        public void SetCustomBlocklist(DomainBlocklist blocklist, bool enabled)
        {
            lock (_configLock)
            {
                _blocklist = blocklist ?? new DomainBlocklist();
                _blocklistSignature = null;
                _enableAdBlock = enabled;
            }
        }

        public void UpdateSettings(AppConfig config)
        {
            if (config == null) return;

            string sig = ComputeBlocklistSignature(config);
            DomainBlocklist reusedBlocklist = null;
            lock (_configLock)
            {
                if (_blocklist != null && string.Equals(_blocklistSignature, sig, StringComparison.Ordinal))
                {
                    reusedBlocklist = _blocklist;
                }
            }

            DomainBlocklist effectiveBlocklist = reusedBlocklist ?? DomainBlocklist.Build(config.BlocklistPreset, config.CustomBlockRules);
            DomainMatcher newMatcher = new DomainMatcher(config.Whitelist);

            lock (_configLock)
            {
                _matcher = newMatcher;
                _blocklist = effectiveBlocklist;
                _blocklistSignature = sig;
                _enableAdBlock = config.EnableAdBlock;
                _splitMode = config.SplitMode;
                _chunkSize = config.ChunkSize;
                _chunkDelayMs = config.ChunkDelayMs;
                _logDomainsInUi = config.LogDomainsInUi;
            }
        }

        private static string ComputeBlocklistSignature(AppConfig config)
        {
            string preset = (config.BlocklistPreset ?? "starter").Trim().ToLowerInvariant();
            string stamp = "builtin";
            if (!string.Equals(preset, "starter", StringComparison.Ordinal))
            {
                string path = DomainBlocklist.ResolveExistingCacheFile(preset, AppConfig.AppDataDir);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(path);
                        stamp = fi.Length.ToString() + ":" + fi.LastWriteTimeUtc.Ticks.ToString();
                    }
                    catch
                    {
                        stamp = "err";
                    }
                }
                else
                {
                    stamp = "missing";
                }
            }

            string custom = config.CustomBlockRules != null
                ? string.Join("\n", config.CustomBlockRules.ToArray())
                : string.Empty;
            return preset + "|" + stamp + "|" + custom;
        }

        public void Start(int port)
        {
            if (IsRunning) return;

            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start(128);
            ListenPort = ((IPEndPoint)_listener.LocalEndpoint).Port;

            _acceptLoop = Task.Run(delegate() { return AcceptLoopAsync(_cts.Token); });
        }

        public void Stop()
        {
            CancellationTokenSource cts = _cts;
            TcpListener listener = _listener;
            _cts = null;
            _listener = null;

            if (cts != null)
            {
                try { cts.Cancel(); } catch { }
            }

            if (listener != null)
            {
                try { listener.Stop(); } catch { }
            }

            if (_acceptLoop != null)
            {
                try { _acceptLoop.Wait(300); } catch { }
                _acceptLoop = null;
            }

            int spin = 0;
            while (Interlocked.Read(ref _activeConnections) > 0 && spin < 15)
            {
                Thread.Sleep(10);
                spin++;
            }

            if (cts != null)
            {
                try { cts.Dispose(); } catch { }
            }
        }

        public void ResetCounters()
        {
            Interlocked.Exchange(ref _protectedConnections, 0);
            Interlocked.Exchange(ref _blockedConnections, 0);
            Interlocked.Exchange(ref _excludedConnections, 0);
        }

        public async Task<bool> VerifyLoopbackAdBlockAsync(CancellationToken ct)
        {
            if (!IsRunning) return false;
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(IPAddress.Loopback, ListenPort).ConfigureAwait(false);
                    NetworkStream ns = client.GetStream();
                    byte[] req = Encoding.ASCII.GetBytes("CONNECT ads.doubleclick.net:443 HTTP/1.1\r\nHost: ads.doubleclick.net:443\r\n\r\n");
                    await ns.WriteAsync(req, 0, req.Length, ct).ConfigureAwait(false);
                    await ns.FlushAsync(ct).ConfigureAwait(false);

                    byte[] buf = new byte[256];
                    int read = await ReadWithTimeoutAsync(ns, buf, 0, buf.Length, 2000, ct).ConfigureAwait(false);
                    if (read <= 0) return false;
                    string resp = Encoding.ASCII.GetString(buf, 0, read);
                    return resp.StartsWith("HTTP/1.1 403", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                TcpClient client = null;
                try
                {
                    TcpListener listener = _listener;
                    if (listener == null) break;
                    client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    if (ct.IsCancellationRequested) break;
                    continue;
                }
                catch
                {
                    if (ct.IsCancellationRequested) break;
                    continue;
                }

                TcpClient accepted = client;
                Task ignored = Task.Run(delegate() { return HandleClientAsync(accepted, ct); });
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            Interlocked.Increment(ref _activeConnections);
            TcpClient upstream = null;
            CancellationTokenRegistration cancelReg = default(CancellationTokenRegistration);

            try
            {
                cancelReg = ct.Register(delegate()
                {
                    try { client.Close(); } catch { }
                    TcpClient up = upstream;
                    if (up != null)
                    {
                        try { up.Close(); } catch { }
                    }
                });

                client.NoDelay = true;
                try { client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true); } catch { }
                client.ReceiveTimeout = 30000;
                client.SendTimeout = 30000;

                NetworkStream clientStream = client.GetStream();

                HttpHeaderRead header = await ReadHttpRequestHeaderAsync(clientStream, ct).ConfigureAwait(false);
                if (header == null)
                {
                    return;
                }

                string method = header.Method;
                string targetHost;
                int targetPort;
                string forwardRequestBytesHeader = null;

                if (string.Equals(method, "CONNECT", StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryParseHostPort(header.Target, 443, out targetHost, out targetPort))
                    {
                        await SendErrorResponseAsync(clientStream, "400 Bad Request").ConfigureAwait(false);
                        return;
                    }
                }
                else
                {
                    if (!TryRewritePlainHttpRequest(header, out targetHost, out targetPort, out forwardRequestBytesHeader))
                    {
                        await SendErrorResponseAsync(clientStream, "400 Bad Request").ConfigureAwait(false);
                        return;
                    }
                }

                DomainMatcher matcher;
                DomainBlocklist blocklist;
                bool enableAdBlock;
                SniSplitMode splitMode;
                int chunkSize;
                int chunkDelayMs;
                bool logDomains;

                lock (_configLock)
                {
                    matcher = _matcher;
                    blocklist = _blocklist;
                    enableAdBlock = _enableAdBlock;
                    splitMode = _splitMode;
                    chunkSize = _chunkSize;
                    chunkDelayMs = _chunkDelayMs;
                    logDomains = _logDomainsInUi;
                }

                if (enableAdBlock && blocklist != null && !matcher.IsExcluded(targetHost) && blocklist.IsBlocked(targetHost))
                {
                    Interlocked.Increment(ref _blockedConnections);
                    Log(logDomains ? "[BLOCKED] " + targetHost : "[BLOCKED] ad/tracker");
                    await SendErrorResponseAsync(clientStream, "403 Forbidden").ConfigureAwait(false);
                    return;
                }

                IPAddress[] ips = await _resolver.ResolveAsync(targetHost, ct).ConfigureAwait(false);
                if (ips == null || ips.Length == 0)
                {
                    Log(logDomains ? "[DNS-ERR] " + targetHost : "[DNS-ERR] resolve failed");
                    await SendErrorResponseAsync(clientStream, "502 Bad Gateway").ConfigureAwait(false);
                    return;
                }

                if (targetPort == ListenPort)
                {
                    for (int i = 0; i < ips.Length; i++)
                    {
                        if (IPAddress.IsLoopback(ips[i]))
                        {
                            Log("[BLOCKED-LOOP] " + targetHost + ":" + targetPort);
                            await SendErrorResponseAsync(clientStream, "403 Forbidden").ConfigureAwait(false);
                            return;
                        }
                    }
                }

                upstream = await ConnectUpstreamAsync(ips, targetPort, ct).ConfigureAwait(false);
                if (upstream == null)
                {
                    Log(logDomains ? "[CONN-ERR] " + targetHost + ":" + targetPort : "[CONN-ERR] upstream connect failed");
                    await SendErrorResponseAsync(clientStream, "502 Bad Gateway").ConfigureAwait(false);
                    return;
                }
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                upstream.NoDelay = true;
                try { upstream.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true); } catch { }
                NetworkStream upstreamStream = upstream.GetStream();

                if (string.Equals(method, "CONNECT", StringComparison.OrdinalIgnoreCase))
                {
                    await clientStream.WriteAsync(OkConnectResponseBytes, 0, OkConnectResponseBytes.Length, ct).ConfigureAwait(false);
                    await clientStream.FlushAsync(ct).ConfigureAwait(false);

                    byte[] trailing = header.TrailingBytes;
                    header = null;

                    byte[] initialBuf = RentBuffer(LargeScratchPool, ref _largeScratchPoolCount, LargeScratchBufSize);
                    bool initialBufTainted = false;
                    try
                    {
                        int totalRead = 0;

                        if (trailing != null && trailing.Length > 0)
                        {
                            int copyLen = Math.Min(trailing.Length, initialBuf.Length);
                            Buffer.BlockCopy(trailing, 0, initialBuf, 0, copyLen);
                            totalRead = copyLen;
                            trailing = null;
                        }
                        else
                        {
                            totalRead = await ReadWithTimeoutAsync(clientStream, initialBuf, 0, initialBuf.Length, 5000, ct).ConfigureAwait(false);
                            if (totalRead < 0)
                            {
                                initialBufTainted = true;
                            }
                        }

                        if (totalRead <= 0)
                        {
                            return;
                        }

                        if (TlsClientHelloParser.IsTlsClientHelloPrefix(initialBuf, totalRead))
                        {
                            int expectedLen = TlsClientHelloParser.GetExpectedTlsRecordLength(initialBuf, totalRead);
                            int cap = Math.Min(initialBuf.Length, expectedLen);
                            while (totalRead < cap)
                            {
                                int more = await ReadWithTimeoutAsync(clientStream, initialBuf, totalRead, cap - totalRead, 1000, ct).ConfigureAwait(false);
                                if (more < 0)
                                {
                                    initialBufTainted = true;
                                    break;
                                }
                                if (more == 0) break;
                                totalRead += more;
                            }
                        }

                        SniParseInfo sni = TlsClientHelloParser.Parse(initialBuf, totalRead);
                        string effectiveHost = (sni.SniFound && !string.IsNullOrEmpty(sni.Hostname)) ? sni.Hostname : targetHost;
                        bool excluded = matcher.IsExcluded(effectiveHost) || matcher.IsExcluded(targetHost);

                        if (enableAdBlock && blocklist != null && !excluded && blocklist.IsBlocked(effectiveHost))
                        {
                            Interlocked.Increment(ref _blockedConnections);
                            Log(logDomains ? "[BLOCKED-SNI] " + effectiveHost : "[BLOCKED-SNI] ad/tracker");
                            return;
                        }

                        if (excluded || splitMode == SniSplitMode.Off || !sni.IsTlsHandshake)
                        {
                            await upstreamStream.WriteAsync(initialBuf, 0, totalRead, ct).ConfigureAwait(false);
                            await upstreamStream.FlushAsync(ct).ConfigureAwait(false);

                            if (excluded)
                            {
                                Interlocked.Increment(ref _excludedConnections);
                                Log(logDomains ? "[BYPASS] " + effectiveHost : "[BYPASS] direct");
                            }
                            else
                            {
                                Log(logDomains ? "[DIRECT] " + effectiveHost : "[DIRECT] passthrough");
                            }
                        }
                        else
                        {
                            List<ByteSlice> slices = TlsClientHelloParser.BuildSplitSlices(totalRead, splitMode, chunkSize, sni);
                            for (int i = 0; i < slices.Count; i++)
                            {
                                ByteSlice slice = slices[i];
                                await upstreamStream.WriteAsync(initialBuf, slice.Offset, slice.Count, ct).ConfigureAwait(false);
                                await upstreamStream.FlushAsync(ct).ConfigureAwait(false);

                                if (chunkDelayMs > 0 && i + 1 < slices.Count)
                                {
                                    await Task.Delay(chunkDelayMs, ct).ConfigureAwait(false);
                                }
                            }

                            Interlocked.Increment(ref _protectedConnections);
                            Log(logDomains
                                ? string.Format("[SNI-SPLIT x{0}] {1} ({2}B)", slices.Count, effectiveHost, totalRead)
                                : string.Format("[SNI-SPLIT x{0}] ({1}B)", slices.Count, totalRead));
                        }
                    }
                    finally
                    {
                        if (!initialBufTainted)
                        {
                            ReturnBuffer(LargeScratchPool, ref _largeScratchPoolCount, MaxLargeScratchPool, initialBuf);
                        }
                        initialBuf = null;
                    }
                }
                else
                {
                    byte[] reqHeaderBytes = Encoding.ASCII.GetBytes(forwardRequestBytesHeader);
                    forwardRequestBytesHeader = null;
                    await upstreamStream.WriteAsync(reqHeaderBytes, 0, reqHeaderBytes.Length, ct).ConfigureAwait(false);
                    if (header.TrailingBytes != null && header.TrailingBytes.Length > 0)
                    {
                        await upstreamStream.WriteAsync(header.TrailingBytes, 0, header.TrailingBytes.Length, ct).ConfigureAwait(false);
                    }
                    await upstreamStream.FlushAsync(ct).ConfigureAwait(false);
                    header = null;

                    Interlocked.Increment(ref _excludedConnections);
                    Log(logDomains ? "[HTTP] " + targetHost : "[HTTP] passthrough");
                }

                matcher = null;
                blocklist = null;
                ips = null;

                Task c2u = RelayStreamAsync(clientStream, client.Client, upstreamStream, upstream.Client, ct);
                Task u2c = RelayStreamAsync(upstreamStream, upstream.Client, clientStream, client.Client, ct);
                await Task.WhenAll(c2u, u2c).ConfigureAwait(false);
            }
            catch
            {
            }
            finally
            {
                try { cancelReg.Dispose(); } catch { }
                Interlocked.Decrement(ref _activeConnections);
                if (upstream != null)
                {
                    try { upstream.Close(); } catch { }
                }
                try { client.Close(); } catch { }
            }
        }

        private static async Task<TcpClient> ConnectUpstreamAsync(IPAddress[] ips, int port, CancellationToken ct)
        {
            int limit = Math.Min(ips.Length, 4);
            for (int i = 0; i < limit; i++)
            {
                if (ct.IsCancellationRequested) return null;
                IPAddress ip = ips[i];
                TcpClient tcp = new TcpClient(ip.AddressFamily);
                try
                {
                    using (CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                    {
                        Task connectTask = tcp.ConnectAsync(ip, port);
                        Task delayTask = Task.Delay(6000, timeoutCts.Token);
                        Task done = await Task.WhenAny(connectTask, delayTask).ConfigureAwait(false);
                        timeoutCts.Cancel();
                        if (done == connectTask && !connectTask.IsFaulted && tcp.Connected)
                        {
                            return tcp;
                        }
                        ObserveFault(connectTask);
                    }
                }
                catch
                {
                }
                try { tcp.Close(); } catch { }
            }
            return null;
        }

        private static byte[] RentBuffer(ConcurrentBag<byte[]> pool, ref int count, int size)
        {
            byte[] buf;
            if (pool.TryTake(out buf))
            {
                Interlocked.Decrement(ref count);
                return buf;
            }
            return new byte[size];
        }

        private static void ReturnBuffer(ConcurrentBag<byte[]> pool, ref int count, int maxPool, byte[] buf)
        {
            if (buf == null) return;
            if (Interlocked.Increment(ref count) <= maxPool)
            {
                pool.Add(buf);
            }
            else
            {
                Interlocked.Decrement(ref count);
            }
        }

        private static async Task RelayStreamAsync(
            NetworkStream src,
            Socket srcSocket,
            NetworkStream dst,
            Socket dstSocket,
            CancellationToken ct)
        {
            byte[] buf = RentBuffer(SmallRelayPool, ref _smallRelayPoolCount, SmallRelayBufSize);
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    int read = await src.ReadAsync(buf, 0, buf.Length, ct).ConfigureAwait(false);
                    if (read <= 0)
                    {
                        break;
                    }
                    await dst.WriteAsync(buf, 0, read, ct).ConfigureAwait(false);
                }
            }
            catch
            {
            }
            finally
            {
                ReturnBuffer(SmallRelayPool, ref _smallRelayPoolCount, MaxSmallRelayPool, buf);
                try { dstSocket.Shutdown(SocketShutdown.Send); } catch { }
            }
        }

        private static void ObserveFault(Task task)
        {
            if (task == null) return;
            task.ContinueWith(
                delegate(Task t) { var ignored = t.Exception; },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        /// <summary>
        /// Reads from stream with timeout without leaking timers.
        /// Returns &gt;0 bytes read, 0 on clean EOF, or -1 on timeout (caller must not recycle buffer while readTask is pending).
        /// </summary>
        private static async Task<int> ReadWithTimeoutAsync(
            NetworkStream stream,
            byte[] buffer,
            int offset,
            int count,
            int timeoutMs,
            CancellationToken ct)
        {
            using (CancellationTokenSource delayCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                Task<int> readTask = stream.ReadAsync(buffer, offset, count, ct);
                Task delayTask = Task.Delay(timeoutMs, delayCts.Token);
                Task winner = await Task.WhenAny(readTask, delayTask).ConfigureAwait(false);
                if (winner != readTask)
                {
                    ObserveFault(readTask);
                    return -1;
                }
                delayCts.Cancel();
                return await readTask.ConfigureAwait(false);
            }
        }

        private sealed class HttpHeaderRead
        {
            public string Method;
            public string Target;
            public string Version;
            public string[] HeaderLines;
            public byte[] TrailingBytes;
        }

        private static async Task<HttpHeaderRead> ReadHttpRequestHeaderAsync(NetworkStream stream, CancellationToken ct)
        {
            byte[] buf = RentBuffer(LargeScratchPool, ref _largeScratchPoolCount, LargeScratchBufSize);
            bool tainted = false;
            try
            {
                int total = 0;
                int headerEnd = -1;

                while (total < buf.Length)
                {
                    int read = await ReadWithTimeoutAsync(stream, buf, total, buf.Length - total, 10000, ct).ConfigureAwait(false);
                    if (read < 0)
                    {
                        tainted = true;
                        return null;
                    }
                    if (read == 0) return null;
                    total += read;

                    headerEnd = FindHeaderTerminator(buf, total);
                    if (headerEnd >= 0) break;
                }

                if (headerEnd < 0) return null;

                string headerText = Encoding.ASCII.GetString(buf, 0, headerEnd - 4);
                string[] lines = headerText.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                if (lines.Length == 0) return null;

                string[] parts = lines[0].Split(' ');
                if (parts.Length < 3) return null;

                int trailingLen = total - headerEnd;
                byte[] trailingBytes = null;
                if (trailingLen > 0)
                {
                    trailingBytes = new byte[trailingLen];
                    Buffer.BlockCopy(buf, headerEnd, trailingBytes, 0, trailingLen);
                }

                return new HttpHeaderRead
                {
                    Method = parts[0].Trim(),
                    Target = parts[1].Trim(),
                    Version = parts[2].Trim(),
                    HeaderLines = lines,
                    TrailingBytes = trailingBytes
                };
            }
            finally
            {
                if (!tainted)
                {
                    ReturnBuffer(LargeScratchPool, ref _largeScratchPoolCount, MaxLargeScratchPool, buf);
                }
            }
        }

        private static int FindHeaderTerminator(byte[] buf, int length)
        {
            for (int i = 3; i < length; i++)
            {
                if (buf[i - 3] == 13 && buf[i - 2] == 10 && buf[i - 1] == 13 && buf[i] == 10)
                {
                    return i + 1;
                }
            }
            return -1;
        }

        private static bool TryParseHostPort(string authority, int defaultPort, out string host, out int port)
        {
            host = string.Empty;
            port = defaultPort;
            if (string.IsNullOrEmpty(authority)) return false;

            string s = authority.Trim();
            if (s.StartsWith("[", StringComparison.Ordinal))
            {
                int closeBracket = s.IndexOf(']');
                if (closeBracket <= 1) return false;
                host = s.Substring(1, closeBracket - 1);
                if (closeBracket + 1 < s.Length && s[closeBracket + 1] == ':')
                {
                    int p;
                    if (!int.TryParse(s.Substring(closeBracket + 2), out p) || p < 1 || p > 65535) return false;
                    port = p;
                }
                return true;
            }

            int colon = s.LastIndexOf(':');
            if (colon > 0 && s.IndexOf(':') == colon)
            {
                host = s.Substring(0, colon);
                int p;
                if (!int.TryParse(s.Substring(colon + 1), out p) || p < 1 || p > 65535) return false;
                port = p;
                return host.Length > 0;
            }

            host = s;
            return host.Length > 0;
        }

        private static bool TryRewritePlainHttpRequest(
            HttpHeaderRead header,
            out string targetHost,
            out int targetPort,
            out string rewrittenHeaders)
        {
            targetHost = string.Empty;
            targetPort = 80;
            rewrittenHeaders = null;

            string requestTarget = header.Target;
            string hostHeaderVal = null;

            for (int i = 1; i < header.HeaderLines.Length; i++)
            {
                string line = header.HeaderLines[i];
                if (line.StartsWith("Host:", StringComparison.OrdinalIgnoreCase))
                {
                    hostHeaderVal = line.Substring(5).Trim();
                    break;
                }
            }

            Uri uri;
            if (Uri.TryCreate(requestTarget, UriKind.Absolute, out uri))
            {
                if (!string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase)) return false;
                targetHost = uri.Host;
                targetPort = uri.Port > 0 ? uri.Port : 80;
                requestTarget = string.IsNullOrEmpty(uri.PathAndQuery) ? "/" : uri.PathAndQuery;
            }
            else if (!string.IsNullOrEmpty(hostHeaderVal))
            {
                if (!TryParseHostPort(hostHeaderVal, 80, out targetHost, out targetPort)) return false;
            }
            else
            {
                return false;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(header.Method).Append(' ').Append(requestTarget).Append(' ').Append(header.Version).Append("\r\n");

            for (int i = 1; i < header.HeaderLines.Length; i++)
            {
                string line = header.HeaderLines[i];
                if (line.Length == 0) continue;
                if (line.StartsWith("Proxy-Connection:", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("Connection:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                sb.Append(line).Append("\r\n");
            }
            sb.Append("Connection: close\r\n\r\n");
            rewrittenHeaders = sb.ToString();
            return true;
        }

        private static async Task SendErrorResponseAsync(NetworkStream stream, string statusLine)
        {
            try
            {
                byte[] bytes = Encoding.ASCII.GetBytes("HTTP/1.1 " + statusLine + "\r\nConnection: close\r\nContent-Length: 0\r\n\r\n");
                await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                await stream.FlushAsync().ConfigureAwait(false);
            }
            catch
            {
            }
        }

        private void Log(string message)
        {
            Action<string> handler = OnLog;
            if (handler != null)
            {
                try { handler(message); } catch { }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
