using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NologGuard;

namespace NologGuardTests
{
    public static class Program
    {
        private static int _passed = 0;
        private static int _failed = 0;

        public static int Main(string[] args)
        {
            bool runLive = args != null && args.Length > 0 && args[0] == "--live";

            RunTest("DnsWireCodec_BuildAndParseA", TestDnsWireCodecA);
            RunTest("DnsWireCodec_ParseCompressedAndAAAA", TestDnsWireCodecAAAA);
            RunTest("DnsWireCodec_MalformedSafety", TestDnsWireCodecMalformed);
            RunTest("DomainMatcher_WildcardsAndPrivateIps", TestDomainMatcher);
            RunTest("TlsClientHelloParser_StandardAndPostQuantumLarge", TestTlsClientHelloParser);
            RunTest("TlsClientHelloParser_SplitSlicesContiguous", TestSplitSlicesContiguous);
            RunTest("AppConfig_SaveLoadNormalize", TestAppConfigRoundTrip);
            RunTest("DohProvider_CustomPortAndIpBootstrap", TestDohProviderCustomPortAndIp);
            RunTest("ProxyServer_LoopbackSniSplitAndBypass", TestProxyLoopbackSplitAndBypass);
            RunTest("ProxyServer_FragmentedLargeClientHelloBuffering", TestProxyFragmentedLargeClientHello);
            RunTest("ProxyServer_FixedChunksAndPlainHttp", TestProxyFixedChunksAndPlainHttp);
            RunTest("DomainBlocklist_FormatsAndExceptions", TestDomainBlocklistFormats);
            RunTest("DomainBlocklist_CompactHashDedupeAndSeal", TestDomainBlocklistCompactHashDedupe);
            RunTest("DomainBlocklist_PerPresetCacheIsolation", TestDomainBlocklistPerPresetIsolation);
            RunTest("BlocklistPreset_HostsVnAndCustomUrl", TestBlocklistPresetHostsVnAndCustom);
            RunTest("ProxyServer_AdBlockAndWhitelistPrecedence", TestProxyAdBlockAndPrecedence);
            RunTest("ProxyServer_VerifyLoopbackAdBlock", TestProxyVerifyLoopbackAdBlock);
            RunTest("ProxyServer_StopAbortsActiveKeepAliveTunnels", TestProxyStopAbortsActiveTunnels);
            RunTest("TlsClientHelloParser_CoalescedRecordHandshakeBound", TestTlsCoalescedRecordBound);
            RunTest("SingleInstanceLock_RelaunchWaitsAndAcquires", TestSingleInstanceLockRelaunch);
            RunTest("UpdateChecker_VersionAndJsonParsing", TestUpdateCheckerParsing);
            RunTest("Strings_All32LanguagesComplete", TestAllLanguagesComplete);

            if (runLive)
            {
                RunTest("Live_Quad9_DoH_And_HttpsTunnel", TestLiveQuad9AndHttps);
            }

            Console.WriteLine();
            Console.WriteLine(string.Format("Results: {0} passed, {1} failed", _passed, _failed));
            return _failed == 0 ? 0 : 1;
        }

        private static void RunTest(string name, Action action)
        {
            try
            {
                action();
                _passed++;
                Console.WriteLine("[PASS] " + name);
            }
            catch (Exception ex)
            {
                _failed++;
                Console.WriteLine("[FAIL] " + name + " -> " + ex.Message);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception("Assertion failed: " + message);
        }

        private static void TestDnsWireCodecA()
        {
            byte[] q = DnsWireCodec.BuildQuery("example.com", DnsWireCodec.TypeA);
            Assert(q.Length == 29, "Expected 29 bytes query for example.com");
            Assert(q[2] == 0x01 && q[3] == 0x00, "RD bit should be set");

            List<byte> resp = new List<byte>();
            resp.AddRange(new byte[] { 0x00, 0x00, 0x81, 0x80, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00 });
            for (int i = 12; i < q.Length; i++) resp.Add(q[i]);
            resp.AddRange(new byte[] {
                0xC0, 0x0C,
                0x00, 0x01,
                0x00, 0x01,
                0x00, 0x00, 0x01, 0x2C,
                0x00, 0x04,
                93, 184, 216, 34
            });

            DnsWireCodec.DnsParseResult parsed = DnsWireCodec.ParseResponse(resp.ToArray(), DnsWireCodec.TypeA);
            Assert(parsed.RCode == 0, "RCode should be 0");
            Assert(parsed.Addresses.Count == 1, "Should have 1 IPv4 answer");
            Assert(parsed.Addresses[0].ToString() == "93.184.216.34", "IP mismatch");
            Assert(parsed.MinTtlSeconds == 300, "TTL should be 300");
        }

        private static void TestDnsWireCodecAAAA()
        {
            byte[] q = DnsWireCodec.BuildQuery("ipv6.test", DnsWireCodec.TypeAAAA);
            List<byte> resp = new List<byte>();
            resp.AddRange(new byte[] { 0x00, 0x00, 0x81, 0x80, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00 });
            for (int i = 12; i < q.Length; i++) resp.Add(q[i]);
            resp.AddRange(new byte[] {
                0xC0, 0x0C, 0x00, 28, 0x00, 0x01, 0x00, 0x00, 0x00, 0x05, 0x00, 16,
                0x26, 0x06, 0x28, 0x00, 0x02, 0x20, 0x00, 0x01, 0x02, 0x48, 0x18, 0x93, 0x25, 0xc8, 0x19, 0x46
            });

            DnsWireCodec.DnsParseResult parsed = DnsWireCodec.ParseResponse(resp.ToArray(), DnsWireCodec.TypeAAAA);
            Assert(parsed.Addresses.Count == 1, "Should parse 1 IPv6 address");
            Assert(parsed.Addresses[0].AddressFamily == AddressFamily.InterNetworkV6, "Must be IPv6");
            Assert(parsed.MinTtlSeconds == 30, "TTL below 30 should clamp to 30");
        }

        private static void TestDnsWireCodecMalformed()
        {
            DnsWireCodec.DnsParseResult r1 = DnsWireCodec.ParseResponse(new byte[] { 1, 2, 3 }, DnsWireCodec.TypeA);
            Assert(r1.Addresses.Count == 0 && r1.RCode == -1, "Short packet safely rejected");

            byte[] bad = new byte[] { 0, 0, 0x81, 0x80, 0, 1, 0, 1, 0, 0, 0, 0, 0xC0 };
            DnsWireCodec.DnsParseResult r2 = DnsWireCodec.ParseResponse(bad, DnsWireCodec.TypeA);
            Assert(r2.Addresses.Count == 0, "Truncated pointer must not throw");
        }

        private static void TestDomainMatcher()
        {
            DomainMatcher m = new DomainMatcher(new string[] {
                "# Comment line",
                "",
                "*.gov.vn",
                "exact.example.org # inline comment",
                "198.51.100.10"
            });

            Assert(m.IsExcluded("chinhphu.gov.vn"), "Wildcard should match subdomain");
            Assert(m.IsExcluded("a.b.gov.vn"), "Wildcard should match nested subdomain");
            Assert(m.IsExcluded("gov.vn"), "Wildcard should match apex domain");
            Assert(m.IsExcluded("EXACT.EXAMPLE.ORG."), "Case-insensitive & trailing dot match");
            Assert(!m.IsExcluded("notgov.vn"), "Should not match unrelated suffix");
            Assert(m.IsExcluded("127.0.0.1"), "Loopback always excluded");
            Assert(m.IsExcluded("192.168.1.50"), "LAN RFC1918 always excluded");
            Assert(m.IsExcluded("100.100.100.100"), "Tailscale CGNAT IP excluded");
            Assert(m.IsExcluded("198.51.100.10"), "Explicit IP excluded");
            Assert(DomainMatcher.IsLocalOnlyHostname("mybox.ts.net"), "Tailscale MagicDNS is local-only");
            Assert(DomainMatcher.IsLocalOnlyHostname("printer"), "Single-label host is local-only");
            Assert(!DomainMatcher.IsLocalOnlyHostname("wikipedia.org"), "Public domain is not local-only");
        }

        private static byte[] BuildSyntheticClientHello(string sniHost, int leadingPaddingExtLen)
        {
            byte[] hostBytes = Encoding.ASCII.GetBytes(sniHost);
            List<byte> exts = new List<byte>();

            if (leadingPaddingExtLen > 0)
            {
                exts.Add(0x00); exts.Add(0x33);
                exts.Add((byte)((leadingPaddingExtLen >> 8) & 0xFF));
                exts.Add((byte)(leadingPaddingExtLen & 0xFF));
                for (int i = 0; i < leadingPaddingExtLen; i++) exts.Add((byte)(i & 0xFF));
            }

            int sniListLen = 3 + hostBytes.Length;
            int sniExtLen = 2 + sniListLen;
            exts.Add(0x00); exts.Add(0x00);
            exts.Add((byte)((sniExtLen >> 8) & 0xFF));
            exts.Add((byte)(sniExtLen & 0xFF));
            exts.Add((byte)((sniListLen >> 8) & 0xFF));
            exts.Add((byte)(sniListLen & 0xFF));
            exts.Add(0x00);
            exts.Add((byte)((hostBytes.Length >> 8) & 0xFF));
            exts.Add((byte)(hostBytes.Length & 0xFF));
            exts.AddRange(hostBytes);

            List<byte> body = new List<byte>();
            body.Add(0x03); body.Add(0x03);
            for (int i = 0; i < 32; i++) body.Add(0xAA);
            body.Add(0x00);
            body.Add(0x00); body.Add(0x02); body.Add(0x13); body.Add(0x01);
            body.Add(0x01); body.Add(0x00);
            body.Add((byte)((exts.Count >> 8) & 0xFF));
            body.Add((byte)(exts.Count & 0xFF));
            body.AddRange(exts);

            int hsLen = body.Count;
            int recLen = 4 + hsLen;

            List<byte> pkt = new List<byte>();
            pkt.Add(0x16); pkt.Add(0x03); pkt.Add(0x01);
            pkt.Add((byte)((recLen >> 8) & 0xFF));
            pkt.Add((byte)(recLen & 0xFF));
            pkt.Add(0x01);
            pkt.Add((byte)((hsLen >> 16) & 0xFF));
            pkt.Add((byte)((hsLen >> 8) & 0xFF));
            pkt.Add((byte)(hsLen & 0xFF));
            pkt.AddRange(body);

            return pkt.ToArray();
        }

        private static void TestTlsClientHelloParser()
        {
            byte[] standard = BuildSyntheticClientHello("privacy.example.com", 0);
            SniParseInfo info1 = TlsClientHelloParser.Parse(standard, standard.Length);
            Assert(info1.IsTlsHandshake && info1.SniFound, "Must find SNI in standard ClientHello");
            Assert(info1.Hostname == "privacy.example.com", "SNI hostname mismatch");
            Assert(Encoding.ASCII.GetString(standard, info1.SniOffset, info1.SniLength) == "privacy.example.com", "Exact SNI offset mismatch");

            byte[] largePq = BuildSyntheticClientHello("pq.cloudflare.com", 1600);
            Assert(largePq.Length > 1650, "Synthetic PQ ClientHello must exceed 1500B MTU");
            SniParseInfo info2 = TlsClientHelloParser.Parse(largePq, largePq.Length);
            Assert(info2.SniFound && info2.Hostname == "pq.cloudflare.com", "Must find SNI after 1600B extension");
            Assert(info2.SniOffset > 1600, "SNI offset must reflect position after key_share");
        }

        private static void TestSplitSlicesContiguous()
        {
            byte[] large = BuildSyntheticClientHello("example.org", 1500);
            SniParseInfo sni = TlsClientHelloParser.Parse(large, large.Length);

            foreach (SniSplitMode mode in new SniSplitMode[] { SniSplitMode.SmartSni, SniSplitMode.FixedChunks, SniSplitMode.Off })
            {
                List<ByteSlice> slices = TlsClientHelloParser.BuildSplitSlices(large.Length, mode, 2, sni);
                int cursor = 0;
                for (int i = 0; i < slices.Count; i++)
                {
                    Assert(slices[i].Offset == cursor, "Gap/overlap in slices for mode " + mode);
                    Assert(slices[i].Count > 0, "Zero-length slice not allowed");
                    cursor += slices[i].Count;
                }
                Assert(cursor == large.Length, "Slices did not cover full payload for mode " + mode);

                if (mode == SniSplitMode.SmartSni)
                {
                    Assert(slices.Count == 3, "SmartSni should produce 3 slices");
                    int sniMid = sni.SniOffset + (sni.SniLength / 2);
                    Assert(slices[0].Count == 2 && slices[1].Offset + slices[1].Count == sniMid, "Must split header & SNI midpoint");
                }
            }
        }

        private static void TestAppConfigRoundTrip()
        {
            string tmp = Path.Combine(Path.GetTempPath(), "nologguard_test_cfg_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                AppConfig cfg = AppConfig.CreateDefault();
                cfg.Language = "ja";
                cfg.ProxyPort = 9123;
                cfg.DohProviderId = "cloudflare";
                cfg.SplitMode = SniSplitMode.FixedChunks;
                cfg.ChunkSize = 4;
                cfg.ChunkDelayMs = 3;
                cfg.AutoProtectOnLaunch = true;
                cfg.BlocklistPreset = "hostsvn";
                cfg.CustomBlockUrl = "https://custom.filter.test/hosts.txt";
                cfg.Whitelist.Add("*.vietcombank.com.vn");
                cfg.Save(tmp);

                AppConfig loaded = AppConfig.Load(tmp);
                Assert(loaded.Language == "ja", "Language roundtrip");
                Assert(loaded.ProxyPort == 9123, "Port roundtrip");
                Assert(loaded.DohProviderId == "cloudflare", "Provider roundtrip");
                Assert(loaded.SplitMode == SniSplitMode.FixedChunks, "SplitMode roundtrip");
                Assert(loaded.ChunkSize == 4 && loaded.ChunkDelayMs == 3, "Chunk params roundtrip");
                Assert(loaded.AutoProtectOnLaunch, "AutoProtectOnLaunch roundtrip");
                Assert(loaded.BlocklistPreset == "hostsvn", "BlocklistPreset roundtrip");
                Assert(loaded.CustomBlockUrl == "https://custom.filter.test/hosts.txt", "CustomBlockUrl roundtrip");
                Assert(loaded.Whitelist.Contains("*.vietcombank.com.vn"), "Whitelist roundtrip");
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }

        private sealed class StubLoopbackResolver : IDnsResolver
        {
            public Task<IPAddress[]> ResolveAsync(string host, CancellationToken ct)
            {
                return Task.FromResult(new IPAddress[] { IPAddress.Loopback });
            }
        }

        private static void TestProxyLoopbackSplitAndBypass()
        {
            AppConfig cfg = AppConfig.CreateDefault();
            cfg.SplitMode = SniSplitMode.SmartSni;
            cfg.ChunkDelayMs = 1;
            cfg.Whitelist = new List<string> { "allowed.example.test" };

            IDnsResolver resolver = new StubLoopbackResolver();
            using (ProxyServer proxy = new ProxyServer(resolver, cfg))
            {
                proxy.Start(0);
                int proxyPort = proxy.ListenPort;

                TcpListener upstreamListener = new TcpListener(IPAddress.Loopback, 0);
                upstreamListener.Start();
                int upstreamPort = ((IPEndPoint)upstreamListener.LocalEndpoint).Port;

                try
                {
                    byte[] ch1 = BuildSyntheticClientHello("blocked.example.test", 0);
                    byte[] recv1 = SendViaConnectProxy(proxyPort, "blocked.example.test", upstreamListener, upstreamPort, ch1);
                    Assert(BytesEqual(ch1, recv1), "Protected ClientHello bytes corrupted during split");
                    Assert(proxy.ProtectedConnections == 1, "Expected ProtectedConnections == 1");

                    byte[] ch2 = BuildSyntheticClientHello("allowed.example.test", 0);
                    byte[] recv2 = SendViaConnectProxy(proxyPort, "allowed.example.test", upstreamListener, upstreamPort, ch2);
                    Assert(BytesEqual(ch2, recv2), "Whitelisted ClientHello bytes corrupted");
                    Assert(proxy.ExcludedConnections == 1, "Expected ExcludedConnections == 1");

                    byte[] ch3 = BuildSyntheticClientHello("local.test", 0);
                    byte[] recv3 = SendViaConnectProxy(proxyPort, "127.0.0.1", upstreamListener, upstreamPort, ch3);
                    Assert(BytesEqual(ch3, recv3), "Loopback ClientHello bytes corrupted");
                    Assert(proxy.ExcludedConnections == 2, "Expected ExcludedConnections == 2");
                }
                finally
                {
                    upstreamListener.Stop();
                }
            }
        }

        private static void TestProxyFragmentedLargeClientHello()
        {
            AppConfig cfg = AppConfig.CreateDefault();
            cfg.SplitMode = SniSplitMode.SmartSni;
            cfg.ChunkDelayMs = 1;
            cfg.Whitelist = new List<string>();

            IDnsResolver resolver = new StubLoopbackResolver();
            using (ProxyServer proxy = new ProxyServer(resolver, cfg))
            {
                proxy.Start(0);
                TcpListener upstreamListener = new TcpListener(IPAddress.Loopback, 0);
                upstreamListener.Start();
                int upstreamPort = ((IPEndPoint)upstreamListener.LocalEndpoint).Port;

                try
                {
                    byte[] largeCh = BuildSyntheticClientHello("largepq.example.test", 1700);
                    byte[] receivedByUpstream = null;

                    Task acceptTask = Task.Run(async delegate()
                    {
                        using (TcpClient up = await upstreamListener.AcceptTcpClientAsync())
                        using (NetworkStream s = up.GetStream())
                        using (MemoryStream ms = new MemoryStream())
                        {
                            byte[] b = new byte[4096];
                            while (ms.Length < largeCh.Length)
                            {
                                int n = await s.ReadAsync(b, 0, b.Length);
                                if (n <= 0) break;
                                ms.Write(b, 0, n);
                            }
                            receivedByUpstream = ms.ToArray();
                            byte[] ack = Encoding.ASCII.GetBytes("OK_ACK");
                            await s.WriteAsync(ack, 0, ack.Length);
                        }
                    });

                    using (TcpClient client = new TcpClient())
                    {
                        client.Connect(IPAddress.Loopback, proxy.ListenPort);
                        client.NoDelay = true;
                        NetworkStream cs = client.GetStream();
                        byte[] req = Encoding.ASCII.GetBytes("CONNECT largepq.example.test:" + upstreamPort + " HTTP/1.1\r\nHost: largepq.example.test:" + upstreamPort + "\r\n\r\n");
                        cs.Write(req, 0, req.Length);

                        byte[] hdrBuf = new byte[256];
                        int hn = cs.Read(hdrBuf, 0, hdrBuf.Length);
                        string respStr = Encoding.ASCII.GetString(hdrBuf, 0, hn);
                        Assert(respStr.StartsWith("HTTP/1.1 200"), "Expected 200 Connection Established");

                        int firstPart = 600;
                        cs.Write(largeCh, 0, firstPart);
                        cs.Flush();
                        Thread.Sleep(15);
                        cs.Write(largeCh, firstPart, largeCh.Length - firstPart);
                        cs.Flush();

                        byte[] ackBuf = new byte[32];
                        int an = cs.Read(ackBuf, 0, ackBuf.Length);
                        Assert(Encoding.ASCII.GetString(ackBuf, 0, an) == "OK_ACK", "Upstream echo mismatch");
                    }

                    acceptTask.Wait(3000);
                    Assert(BytesEqual(largeCh, receivedByUpstream), "Large fragmented ClientHello mismatch");
                    Assert(proxy.ProtectedConnections == 1, "Should still split large fragmented ClientHello");
                }
                finally
                {
                    upstreamListener.Stop();
                }
            }
        }

        private static void TestProxyFixedChunksAndPlainHttp()
        {
            AppConfig cfg = AppConfig.CreateDefault();
            cfg.SplitMode = SniSplitMode.FixedChunks;
            cfg.ChunkSize = 3;
            cfg.ChunkDelayMs = 1;
            cfg.Whitelist = new List<string>();

            IDnsResolver resolver = new StubLoopbackResolver();
            using (ProxyServer proxy = new ProxyServer(resolver, cfg))
            {
                proxy.Start(0);
                TcpListener upstreamListener = new TcpListener(IPAddress.Loopback, 0);
                upstreamListener.Start();
                int upstreamPort = ((IPEndPoint)upstreamListener.LocalEndpoint).Port;

                try
                {
                    byte[] ch = BuildSyntheticClientHello("chunked.example.test", 200);
                    byte[] recv = SendViaConnectProxy(proxy.ListenPort, "chunked.example.test", upstreamListener, upstreamPort, ch);
                    Assert(BytesEqual(ch, recv), "FixedChunks ClientHello bytes corrupted");
                    Assert(proxy.ProtectedConnections == 1, "Expected ProtectedConnections == 1");

                    string capturedHttp = null;
                    Task httpServer = Task.Run(async delegate()
                    {
                        using (TcpClient up = await upstreamListener.AcceptTcpClientAsync())
                        using (NetworkStream s = up.GetStream())
                        {
                            byte[] b = new byte[2048];
                            int n = await s.ReadAsync(b, 0, b.Length);
                            capturedHttp = Encoding.ASCII.GetString(b, 0, n);
                            byte[] resp = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nOK");
                            await s.WriteAsync(resp, 0, resp.Length);
                        }
                    });

                    using (TcpClient client = new TcpClient())
                    {
                        client.Connect(IPAddress.Loopback, proxy.ListenPort);
                        NetworkStream cs = client.GetStream();
                        byte[] req = Encoding.ASCII.GetBytes(
                            "GET http://plain.example.test:" + upstreamPort + "/hello?x=1 HTTP/1.1\r\n" +
                            "Host: plain.example.test:" + upstreamPort + "\r\n" +
                            "Proxy-Connection: keep-alive\r\n\r\n");
                        cs.Write(req, 0, req.Length);

                        using (MemoryStream ms = new MemoryStream())
                        {
                            byte[] rbuf = new byte[1024];
                            int rn;
                            while ((rn = cs.Read(rbuf, 0, rbuf.Length)) > 0) ms.Write(rbuf, 0, rn);
                            string respStr = Encoding.ASCII.GetString(ms.ToArray());
                            Assert(respStr.EndsWith("OK"), "Plain HTTP response mismatch");
                        }
                    }

                    httpServer.Wait(3000);
                    Assert(capturedHttp != null && capturedHttp.StartsWith("GET /hello?x=1 HTTP/1.1"), "Origin-form rewrite failed");
                    Assert(!capturedHttp.Contains("Proxy-Connection:"), "Proxy-Connection header should be stripped");
                }
                finally
                {
                    upstreamListener.Stop();
                }
            }
        }

        private static void TestDomainBlocklistFormats()
        {
            DomainBlocklist bl = new DomainBlocklist();
            bl.LoadLines(new string[] {
                "! Adblock comment",
                "# Hosts comment",
                "||doubleclick.net^",
                "||safe.doubleclick.net^",
                "@@||safe.doubleclick.net^",
                "0.0.0.0 tracker.example.com",
                "127.0.0.1 pixel.example.org # inline",
                "*.popads.net",
                "||facebook.com^$third-party",
                "||news.example.com/ad-banner.js",
                "example.net##.ad-banner"
            });

            Assert(bl.IsBlocked("doubleclick.net"), "Exact ABP domain blocked");
            Assert(bl.IsBlocked("ad.sub.doubleclick.net"), "Subdomain blocked via parent walk");
            Assert(!bl.IsBlocked("safe.doubleclick.net"), "@@ exception must unblock domain");
            Assert(!bl.IsBlocked("cdn.safe.doubleclick.net"), "@@ exception must unblock subdomain");
            Assert(bl.IsBlocked("tracker.example.com"), "Hosts 0.0.0.0 format blocked");
            Assert(bl.IsBlocked("pixel.example.org"), "Hosts 127.0.0.1 + inline comment blocked");
            Assert(bl.IsBlocked("cdn.popads.net"), "Wildcard domain blocked");
            Assert(!bl.IsBlocked("facebook.com"), "$third-party rule must not over-block first-party domain");
            Assert(!bl.IsBlocked("news.example.com"), "Path rule must not over-block domain");
            Assert(!bl.IsBlocked("example.net"), "Cosmetic rule must not over-block domain");
            Assert(!bl.IsBlocked("localhost"), "Localhost never blocked");
            Assert(!bl.IsBlocked("127.0.0.1"), "IP literal never blocked");
        }

        private static void TestDomainBlocklistCompactHashDedupe()
        {
            DomainBlocklist bl = new DomainBlocklist();
            bl.LoadLines(new string[] {
                "||Ads.Example.Test^",
                "0.0.0.0 ads.example.test",
                "127.0.0.1 ADS.EXAMPLE.TEST",
                "tracker-1.example.test"
            });
            bl.Seal();

            Assert(bl.BlockedRuleCount == 2, "Duplicate case-variant rules must deduplicate to 2, got " + bl.BlockedRuleCount);
            Assert(bl.IsBlocked("sub.ADS.example.test"), "Compact hash must match mixed-case subdomains");
            Assert(!bl.IsBlocked("tracker-2.example.test"), "Sibling domain must not false-match");

            bl.AddRuleLine("tracker-2.example.test");
            Assert(bl.IsBlocked("tracker-2.example.test"), "Appending rules after Seal must re-seal seamlessly");
            Assert(bl.BlockedRuleCount == 3, "Expected 3 unique rules after append, got " + bl.BlockedRuleCount);

            DomainBlocklist bulk = new DomainBlocklist();
            for (int i = 0; i < 5000; i++)
            {
                bulk.AddRuleLine("ad-" + i + ".bulk-test.example");
                if ((i & 1) == 0) bulk.AddRuleLine("0.0.0.0 AD-" + i + ".bulk-test.example");
            }
            bulk.Seal();
            Assert(bulk.BlockedRuleCount == 5000, "Expected 5000 deduplicated bulk rules, got " + bulk.BlockedRuleCount);
            Assert(bulk.IsBlocked("sub.ad-4321.bulk-test.example"), "Bulk rule lookup failed");
            Assert(!bulk.IsBlocked("ad-5001.bulk-test.example"), "Bulk rule unexpected match");
        }

        private static void TestBlocklistPresetHostsVnAndCustom()
        {
            BlocklistPreset vn = BlocklistPreset.FindById("hostsvn");
            Assert(vn.Id == "hostsvn", "hostsVN preset must exist");
            Assert(vn.Url == "https://raw.githubusercontent.com/bigdargon/hostsVN/master/hosts", "hostsVN URL mismatch");

            Assert(BlocklistPreset.ResolveEffectiveUrl("starter", "https://example.com/list.txt") == string.Empty,
                "Starter must resolve empty URL");
            Assert(BlocklistPreset.ResolveEffectiveUrl("hostsvn", "https://example.com/list.txt") == vn.Url,
                "hostsVN must use built-in preset URL");
            Assert(BlocklistPreset.ResolveEffectiveUrl("custom", "  https://my.filter.test/hosts.txt  ") == "https://my.filter.test/hosts.txt",
                "Custom preset must trim and use CustomBlockUrl");
        }

        private static void TestProxyAdBlockAndPrecedence()
        {
            AppConfig cfg = AppConfig.CreateDefault();
            cfg.EnableAdBlock = true;
            cfg.Whitelist = new List<string> { "allowed-ad.doubleclick.net" };

            DomainBlocklist bl = new DomainBlocklist();
            bl.LoadLines(new string[] { "||doubleclick.net^", "||sni-tracker.test^" });

            IDnsResolver resolver = new StubLoopbackResolver();
            using (ProxyServer proxy = new ProxyServer(resolver, cfg))
            {
                proxy.SetCustomBlocklist(bl, true);
                proxy.Start(0);

                using (TcpClient c1 = new TcpClient())
                {
                    c1.Connect(IPAddress.Loopback, proxy.ListenPort);
                    NetworkStream s1 = c1.GetStream();
                    byte[] req = Encoding.ASCII.GetBytes("CONNECT stats.doubleclick.net:443 HTTP/1.1\r\nHost: stats.doubleclick.net:443\r\n\r\n");
                    s1.Write(req, 0, req.Length);

                    byte[] respBuf = new byte[256];
                    int n = s1.Read(respBuf, 0, respBuf.Length);
                    string resp = Encoding.ASCII.GetString(respBuf, 0, n);
                    Assert(resp.StartsWith("HTTP/1.1 403 Forbidden"), "Blocked CONNECT should return 403");
                }
                Assert(proxy.BlockedConnections == 1, "Expected BlockedConnections == 1");

                using (TcpClient c2 = new TcpClient())
                {
                    c2.Connect(IPAddress.Loopback, proxy.ListenPort);
                    NetworkStream s2 = c2.GetStream();
                    byte[] req = Encoding.ASCII.GetBytes("GET http://ad.doubleclick.net/pixel.js HTTP/1.1\r\nHost: ad.doubleclick.net\r\n\r\n");
                    s2.Write(req, 0, req.Length);

                    byte[] respBuf = new byte[256];
                    int n = s2.Read(respBuf, 0, respBuf.Length);
                    string resp = Encoding.ASCII.GetString(respBuf, 0, n);
                    Assert(resp.StartsWith("HTTP/1.1 403 Forbidden"), "Blocked plain HTTP should return 403");
                }
                Assert(proxy.BlockedConnections == 2, "Expected BlockedConnections == 2");

                TcpListener upstreamListener = new TcpListener(IPAddress.Loopback, 0);
                upstreamListener.Start();
                int upstreamPort = ((IPEndPoint)upstreamListener.LocalEndpoint).Port;
                try
                {
                    int upstreamBytesReceived = -1;
                    Task drainTask = Task.Run(async delegate()
                    {
                        using (TcpClient up = await upstreamListener.AcceptTcpClientAsync())
                        using (NetworkStream us = up.GetStream())
                        {
                            byte[] b = new byte[1024];
                            upstreamBytesReceived = await us.ReadAsync(b, 0, b.Length);
                        }
                    });

                    using (TcpClient c3 = new TcpClient())
                    {
                        c3.Connect(IPAddress.Loopback, proxy.ListenPort);
                        NetworkStream s3 = c3.GetStream();
                        byte[] req = Encoding.ASCII.GetBytes("CONNECT innocent-alias.test:" + upstreamPort + " HTTP/1.1\r\nHost: innocent-alias.test:" + upstreamPort + "\r\n\r\n");
                        s3.Write(req, 0, req.Length);

                        byte[] hdrBuf = new byte[256];
                        int hn = s3.Read(hdrBuf, 0, hdrBuf.Length);
                        Assert(Encoding.ASCII.GetString(hdrBuf, 0, hn).StartsWith("HTTP/1.1 200"), "Expected 200 before ClientHello");

                        byte[] blockedSniCh = BuildSyntheticClientHello("sub.sni-tracker.test", 0);
                        s3.Write(blockedSniCh, 0, blockedSniCh.Length);
                        s3.Flush();

                        byte[] endBuf = new byte[64];
                        int rn = s3.Read(endBuf, 0, endBuf.Length);
                        Assert(rn == 0, "Expected proxy to close connection on blocked SNI");
                    }

                    drainTask.Wait(3000);
                    Assert(upstreamBytesReceived == 0, "Blocked SNI ClientHello must never reach upstream");
                    Assert(proxy.BlockedConnections == 3, "Expected BlockedConnections == 3 after SNI block");

                    byte[] ch = BuildSyntheticClientHello("allowed-ad.doubleclick.net", 0);
                    byte[] recv = SendViaConnectProxy(proxy.ListenPort, "allowed-ad.doubleclick.net", upstreamListener, upstreamPort, ch);
                    Assert(BytesEqual(ch, recv), "Whitelisted domain should bypass AdBlock");
                    Assert(proxy.ExcludedConnections == 1, "Expected ExcludedConnections == 1");
                }
                finally
                {
                    upstreamListener.Stop();
                }
            }
        }

        private static void TestProxyVerifyLoopbackAdBlock()
        {
            AppConfig cfg = AppConfig.CreateDefault();
            cfg.EnableAdBlock = true;
            cfg.BlocklistPreset = "starter";
            cfg.Whitelist = new List<string>();

            using (ProxyServer proxy = new ProxyServer(new StubLoopbackResolver(), cfg))
            {
                proxy.Start(0);
                Assert(proxy.IsAdBlockHostBlocked("doubleclick.net"), "Starter blocklist should block doubleclick.net");
                bool verified = proxy.VerifyLoopbackAdBlockAsync(CancellationToken.None).GetAwaiter().GetResult();
                Assert(verified, "Loopback self-check should receive 403 when AdBlock active");

                DomainBlocklist empty = new DomainBlocklist();
                proxy.SetCustomBlocklist(empty, false);
                Assert(!proxy.IsAdBlockHostBlocked("doubleclick.net"), "Disabled AdBlock must return false");
            }
        }

        private static void TestDohProviderCustomPortAndIp()
        {
            DohProvider customPort = DohProvider.FindById("custom", "https://dns.internal.test:8443/dns-query");
            Assert(customPort.Port == 8443, "Expected custom DoH port 8443, got " + customPort.Port);
            Assert(customPort.Url == "https://dns.internal.test:8443/dns-query", "Expected port in URL, got " + customPort.Url);
            Assert(customPort.BootstrapIps.Length == 0, "Hostname DoH must not invent BootstrapIps");

            DohProvider customIp = DohProvider.FindById("custom", "https://1.1.1.2/dns-query");
            Assert(customIp.Port == 443, "Expected default port 443");
            Assert(customIp.BootstrapIps.Length == 1 && customIp.BootstrapIps[0] == "1.1.1.2", "IP DoH should auto-populate BootstrapIps");
            Assert(customIp.SystemDnsIpv4.Length == 1 && customIp.SystemDnsIpv4[0] == "1.1.1.2", "IPv4 port-443 DoH should populate SystemDnsIpv4");
        }

        private static void TestDomainBlocklistPerPresetIsolation()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "NologGuard_CacheTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                // Verify legacy blocklist_cache.txt migrates once to the active preset without contaminating other presets
                string legacyPath = Path.Combine(tempDir, "blocklist_cache.txt");
                File.WriteAllText(legacyPath, "||legacy-pro-only.test^\n", Encoding.UTF8);

                DomainBlocklist pro = DomainBlocklist.Build("hagezi-pro", null, tempDir);
                Assert(pro.IsBlocked("legacy-pro-only.test"), "Legacy cache should migrate to active preset");
                Assert(!File.Exists(legacyPath), "Legacy cache file should be renamed to per-preset file");

                DomainBlocklist vn = DomainBlocklist.Build("hostsvn", null, tempDir);
                Assert(!vn.IsBlocked("legacy-pro-only.test"), "Other preset without cache must NOT load hagezi-pro rules");
                Assert(vn.IsBlocked("doubleclick.net"), "Other preset without cache must still include StarterDomains");

                File.WriteAllText(DomainBlocklist.GetCacheFilePath("hostsvn", tempDir), "0.0.0.0 vn-ad-only.test\n", Encoding.UTF8);
                DomainBlocklist vnLoaded = DomainBlocklist.Build("hostsvn", null, tempDir);
                Assert(vnLoaded.IsBlocked("vn-ad-only.test"), "hostsVN cache should load its own domain");
                Assert(!vnLoaded.IsBlocked("legacy-pro-only.test"), "hostsVN must stay isolated from hagezi-pro cache");
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        private static void TestProxyStopAbortsActiveTunnels()
        {
            AppConfig cfg = AppConfig.CreateDefault();
            cfg.SplitMode = SniSplitMode.SmartSni;
            cfg.Whitelist = new List<string>();

            TcpListener upstream = new TcpListener(IPAddress.Loopback, 0);
            upstream.Start();
            int upstreamPort = ((IPEndPoint)upstream.LocalEndpoint).Port;

            TcpClient acceptedUpstream = null;
            Task acceptTask = Task.Run(delegate()
            {
                try { acceptedUpstream = upstream.AcceptTcpClient(); } catch { }
            });

            try
            {
                ProxyServer proxy = new ProxyServer(new StubLoopbackResolver(), cfg);
                proxy.Start(0);

                using (TcpClient client = new TcpClient())
                {
                    client.Connect(IPAddress.Loopback, proxy.ListenPort);
                    NetworkStream ns = client.GetStream();
                    byte[] req = Encoding.ASCII.GetBytes("CONNECT keepalive.example.test:" + upstreamPort + " HTTP/1.1\r\nHost: keepalive.example.test:" + upstreamPort + "\r\n\r\n");
                    ns.Write(req, 0, req.Length);

                    byte[] resp = new byte[128];
                    int rn = ns.Read(resp, 0, resp.Length);
                    Assert(Encoding.ASCII.GetString(resp, 0, rn).StartsWith("HTTP/1.1 200"), "Expected 200 tunnel established");

                    byte[] ch = BuildSyntheticClientHello("keepalive.example.test", 0);
                    ns.Write(ch, 0, ch.Length);
                    ns.Flush();

                    acceptTask.Wait(2000);
                    Thread.Sleep(50);
                    Assert(proxy.ActiveConnections == 1, "Expected 1 active keep-alive tunnel before Stop(), got " + proxy.ActiveConnections);

                    System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                    proxy.Stop();
                    sw.Stop();

                    Assert(proxy.ActiveConnections == 0, "Stop() must immediately abort active keep-alive tunnels, got " + proxy.ActiveConnections);
                    Assert(sw.ElapsedMilliseconds < 1000, "Stop() should finish promptly, took " + sw.ElapsedMilliseconds + " ms");
                }
                proxy.Dispose();
            }
            finally
            {
                if (acceptedUpstream != null) { try { acceptedUpstream.Close(); } catch { } }
                upstream.Stop();
            }
        }

        private static void TestTlsCoalescedRecordBound()
        {
            byte[] chNoSni = new byte[] {
                0x16, 0x03, 0x03, 0x00, 0x35, // Record length = 53 bytes (45B ClientHello + 8B trailing data)
                0x01, 0x00, 0x00, 0x29,       // Handshake ClientHello length = 41 bytes (ends at offset 50)
                0x03, 0x03,                   // TLS 1.2
                // 32 random bytes (offsets 11..42):
                0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,
                0x00,                         // SessionID len = 0 (offset 43)
                0x00, 0x02, 0x13, 0x01,       // CipherSuites len = 2 (offsets 44..47)
                0x01, 0x00,                   // Compression len = 1 (offsets 48..49) -> no extensions in ClientHello!
                // Trailing coalesced bytes inside record (offsets 50..57) mimicking fake extension bytes:
                0x00, 0x06, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00
            };
            SniParseInfo info = TlsClientHelloParser.Parse(chNoSni, chNoSni.Length);
            Assert(info.IsTlsHandshake && !info.SniFound, "Parser must stop at ClientHello handshake boundary");
        }

        private static void TestSingleInstanceLockRelaunch()
        {
            string testMutexName = @"Local\NologGuard_Test_Mutex_" + Guid.NewGuid().ToString("N");
            ManualResetEventSlim acquiredEvent = new ManualResetEventSlim(false);
            ManualResetEventSlim allowReleaseEvent = new ManualResetEventSlim(false);

            Thread holder = new Thread(delegate()
            {
                using (SingleInstanceLock lck = SingleInstanceLock.TryAcquire(testMutexName, false, 0))
                {
                    acquiredEvent.Set();
                    allowReleaseEvent.Wait(2000);
                    Thread.Sleep(150);
                }
            });
            holder.IsBackground = true;
            holder.Start();

            Assert(acquiredEvent.Wait(2000), "Holder thread should acquire initial mutex");

            SingleInstanceLock immediate = SingleInstanceLock.TryAcquire(testMutexName, false, 0);
            Assert(immediate == null, "Non-wait acquire must return null while holder holds mutex");

            allowReleaseEvent.Set();
            using (SingleInstanceLock waited = SingleInstanceLock.TryAcquire(testMutexName, true, 3000))
            {
                Assert(waited != null, "Relaunch wait acquire should succeed once previous instance releases mutex");
            }
            holder.Join(2000);
        }

        private static void TestUpdateCheckerParsing()
        {
            Assert(UpdateChecker.IsNewerVersion("1.2.0", "v1.3.0"), "v1.3.0 should be newer than 1.2.0");
            Assert(UpdateChecker.IsNewerVersion("1.2.0", "v1.2.1-beta"), "v1.2.1-beta should be newer than 1.2.0");
            Assert(!UpdateChecker.IsNewerVersion("1.2.0", "v1.2.0"), "Same version must not trigger update");
            Assert(!UpdateChecker.IsNewerVersion("1.2.0", "v1.1.9"), "Older version must not trigger update");

            string sampleJson = "{\"tag_name\":\"v1.3.0\",\"html_url\":\"https:\\/\\/github.com\\/NologX-com\\/NologGuard\\/releases\\/tag\\/v1.3.0\"}";
            Assert(UpdateChecker.ExtractJsonField(sampleJson, "tag_name") == "v1.3.0", "Extract tag_name failed");
            Assert(UpdateChecker.ExtractJsonField(sampleJson, "html_url") == "https://github.com/NologX-com/NologGuard/releases/tag/v1.3.0",
                "Extract html_url failed");
        }

        private static byte[] SendViaConnectProxy(int proxyPort, string connectHost, TcpListener upstreamListener, int upstreamPort, byte[] payload)
        {
            byte[] captured = null;
            Task acceptTask = Task.Run(async delegate()
            {
                using (TcpClient up = await upstreamListener.AcceptTcpClientAsync())
                using (NetworkStream s = up.GetStream())
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] b = new byte[4096];
                    while (ms.Length < payload.Length)
                    {
                        int n = await s.ReadAsync(b, 0, b.Length);
                        if (n <= 0) break;
                        ms.Write(b, 0, n);
                    }
                    captured = ms.ToArray();
                    byte[] pong = Encoding.ASCII.GetBytes("PONG");
                    await s.WriteAsync(pong, 0, pong.Length);
                }
            });

            using (TcpClient client = new TcpClient())
            {
                client.Connect(IPAddress.Loopback, proxyPort);
                NetworkStream cs = client.GetStream();
                byte[] req = Encoding.ASCII.GetBytes("CONNECT " + connectHost + ":" + upstreamPort + " HTTP/1.1\r\nHost: " + connectHost + ":" + upstreamPort + "\r\n\r\n");
                cs.Write(req, 0, req.Length);

                byte[] hdrBuf = new byte[256];
                int hn = cs.Read(hdrBuf, 0, hdrBuf.Length);
                Assert(Encoding.ASCII.GetString(hdrBuf, 0, hn).StartsWith("HTTP/1.1 200"), "Expected 200 response");

                cs.Write(payload, 0, payload.Length);
                cs.Flush();

                byte[] pongBuf = new byte[16];
                int pn = cs.Read(pongBuf, 0, pongBuf.Length);
                Assert(Encoding.ASCII.GetString(pongBuf, 0, pn) == "PONG", "Expected PONG echo");
            }

            acceptTask.Wait(3000);
            return captured;
        }

        private static void TestLiveQuad9AndHttps()
        {
            AppConfig cfg = AppConfig.CreateDefault();
            cfg.DohProviderId = "quad9";
            cfg.SplitMode = SniSplitMode.SmartSni;
            cfg.ChunkDelayMs = 2;

            using (DohResolver resolver = new DohResolver(DohProvider.Presets[0]))
            {
                Tuple<bool, int, string> probe = resolver.ProbeLatencyAsync(CancellationToken.None).GetAwaiter().GetResult();
                Assert(probe.Item1, "Live Quad9 DoH probe failed: " + probe.Item3);
                Console.WriteLine(string.Format("  [LIVE] Quad9 DoH resolved example.com -> {0} in {1} ms", probe.Item3, probe.Item2));

                using (ProxyServer proxy = new ProxyServer(resolver, cfg))
                {
                    proxy.Start(0);
                    WebProxy wp = new WebProxy("127.0.0.1", proxy.ListenPort);
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://example.com/");
                    req.Proxy = wp;
                    req.Timeout = 8000;
                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    {
                        Assert(resp.StatusCode == HttpStatusCode.OK, "HTTPS via SNI-split proxy returned " + resp.StatusCode);
                        Console.WriteLine(string.Format("  [LIVE] HTTPS https://example.com/ via NologGuard Proxy -> {0} (ProtectedFlows={1})",
                            (int)resp.StatusCode, proxy.ProtectedConnections));
                    }
                }
            }
        }

        private static void TestAllLanguagesComplete()
        {
            Assert(Strings.Languages.Length == 32, "Expected 32 supported languages, got " + Strings.Languages.Length);
            Assert(Strings.IsRightToLeft("ar") && Strings.IsRightToLeft("fa") && Strings.IsRightToLeft("he"),
                "Arabic, Persian, and Hebrew must be marked RTL");
            Assert(!Strings.IsRightToLeft("en") && !Strings.IsRightToLeft("vi"),
                "English and Vietnamese must be LTR");

            ICollection<string> keys = Strings.GetAllKeys();
            Assert(keys.Count >= 60, "Expected full UI string key set (>=60), got " + keys.Count);

            for (int i = 0; i < Strings.Languages.Length; i++)
            {
                string code = Strings.Languages[i].Code;
                Assert(Strings.IsSupported(code), "Language should be supported: " + code);
                Assert(!string.IsNullOrEmpty(Strings.Languages[i].NativeName), "Native name missing for " + code);

                foreach (string key in keys)
                {
                    Assert(Strings.HasKey(code, key), "Missing explicit key [" + code + "]." + key);
                    string val = Strings.Get(code, key);
                    Assert(!string.IsNullOrEmpty(val) && val != key,
                        "Empty translation for [" + code + "]." + key);
                }
            }
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }
}
