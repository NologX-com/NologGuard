# NologGuard

**Created by [NologX.com](https://nologx.com)** • [🇻🇳 Đọc hướng dẫn Tiếng Việt](README.vi.md) • [🔒 Privacy Policy](PRIVACY.md)

**1-Click Windows Privacy Shield** — Combines encrypted **Bootstrapped HTTP/2 DNS-over-HTTPS (DoH)**, **TLS SNI Fragmentation**, **QUIC Bypass/Firewall Blocking**, **Domain-Level AdBlocker & Anti-Tracking**, and a **Smart Whitelist** into one lightweight Windows desktop utility while keeping your real IP address and native connection speed.

---

## How NologGuard Hides Accessed Websites from Your ISP (and Honest Limits)

Normally, even when you visit an `https://` website, your ISP can still tell **which domain you are opening** through three leaks:
1. **Plaintext DNS Queries (UDP Port 53):** Your PC asks the ISP's DNS resolver `"What is the IP of example.com?"` in cleartext before connecting.
2. **TLS SNI (Server Name Indication):** Inside the very first packet of an HTTPS connection (`TLS ClientHello`), your browser sends the target hostname (`example.com`) in plaintext so the server knows which TLS certificate to present. ISP passive DPI boxes inspect this single packet to log or block sites.
3. **QUIC / HTTP3 (UDP Port 443):** Newer browsers try UDP/443 QUIC first, whose Initial packet also carries plaintext SNI and bypasses TCP-only proxies.

### How NologGuard Protects You:
* **Hides DNS Queries (Zero Plain-DNS Leak):** NologGuard resolves domains over **Bootstrapped HTTP/2 DoH** — connecting directly to resolver IPs (`9.9.9.9` Quad9, `1.1.1.1` Cloudflare, `8.8.8.8` Google, `94.140.14.14` AdGuard) so even the DoH server lookup itself never goes over plaintext UDP 53.
* **Obscures HTTPS Domains Against ISP Passive DPI (TLS SNI Splitting):** NologGuard buffers the complete `ClientHello` (even >1,700-byte TLS 1.3 post-quantum `X25519MLKEM768` handshakes), locates the exact SNI hostname bytes, and splits them across multiple tiny TCP segments (`TCP_NODELAY` + configurable millisecond delay). Stateless/passive DPI devices fail to parse the split hostname.
* **Stops QUIC Leaks:** Routing browsers through the local HTTP `CONNECT` proxy automatically makes Chrome, Edge, and Firefox fall back from UDP QUIC to TCP HTTPS (where SNI splitting applies). An optional Administrator toggle can also add a clean Windows Firewall rule blocking outbound UDP 443 for non-browser apps.

### Honest Threat Model (What NologGuard Does and Does NOT Hide)

| Layer | Without Protection | With NologGuard | With Full VPN / VPS |
| :--- | :--- | :--- | :--- |
| **DNS Queries (Domain Name)** | Visible to ISP (UDP 53) | **Hidden** (Bootstrapped HTTP/2 DoH) | **Hidden** |
| **TLS SNI Hostname (Passive DPI)** | Plaintext in 1st packet | **Split across TCP segments** (bypasses passive/stateless DPI) | **Hidden** |
| **QUIC UDP 443 SNI** | Plaintext in QUIC Initial | **Bypassed in browsers** (+ optional Firewall UDP 443 block) | **Hidden** |
| **3rd-Party Ads & Trackers** | Loaded & tracked | **Blocked before DNS/TCP (`403`)** (Built-in + hostsVN / HaGeZi / OISD / StevenBlack / Custom URL) | Varies by VPN |
| **First-Party Video Ads (YouTube)** | Loaded | **Not blocked** (use a browser extension like uBlock Origin alongside NologGuard) | Not blocked |
| **Destination IP Address** | Visible to ISP | **Still visible** (sites behind shared CDNs like Cloudflare remain ambiguous; dedicated server IPs are visible) | **Hidden** (ISP only sees VPN/VPS IP) |
| **Full Stateful / Offline TCP Reassembly** | Readable | **Can be reassembled** if ISP captures & reconstructs complete TCP streams | **Hidden** |
| **Speed / Latency / Real IP** | Native | **100% native speed & keeps real IP** (no VPN slowdowns or CAPTCHAs) | Reduced speed, IP changes |

> **Summary:** Use **NologGuard** when you want encrypted DNS, SNI obfuscation against ISP passive DPI/blocks, domain-level ad blocking, and full native speed with your real IP. Use a **VPN / VPS** when you need complete destination IP anonymity.

---

## All Features (`v1.2`)

1. **1-Click Shield + Auto-Protect on Launch:** Starts local HTTP/HTTPS `CONNECT` proxy (`127.0.0.1:8899`), applies Windows System Proxy (`HKCU`), and refreshes WinINet immediately without restarting browsers. Optional Settings toggle to protect automatically when NologGuard opens.
2. **Bootstrapped HTTP/2 DoH + 1-Click Self-Check:** Built-in Quad9 Security (`9.9.9.9`), Quad9 ECS (`9.9.9.11`), Cloudflare (`1.1.1.1`), Google (`8.8.8.8`), AdGuard (`94.140.14.14`), or any custom `https://...` DoH URL. The **Self-Check** button probes live DoH latency and runs a real loopback `403` AdBlock test.
3. **3 SNI Fragmentation Modes:**
   - **Smart SNI Middle Split (Recommended):** Splits the 2-byte TLS header prefix and cuts cleanly through the middle of the SNI hostname.
   - **Fixed Small Chunks:** Fragments the TLS prefix and SNI hostname into fixed small byte chunks while keeping trailing extensions fast.
   - **Off (DoH Proxy Only):** Encrypted DoH + AdBlock without TCP packet splitting.
4. **AdBlocker & Anti-Tracking (Zero HTTPS MitM):** Blocks third-party ad/tracker/telemetry domains at the proxy layer (`403 Forbidden`) before DNS resolution, without installing any root certificate or decrypting HTTPS traffic. Includes offline **Built-in Starter**, **hostsVN (Vietnam)**, **HaGeZi Multi LIGHT/PRO**, **OISD Small**, **StevenBlack Hosts**, **Custom Filter URL (`https://...`)**, and custom `domain.com` / `@@allow.com` rules.
5. **Smart Whitelist & LAN Exclusion:** Supports exact hosts, `*.domain.com` wildcards, and `# comments`. Automatically bypasses `localhost`, private LAN ranges, and Tailscale (`*.ts.net`, `100.64.0.0/10`). Whitelisted domains also override the AdBlocker.
6. **2 Optional System-Wide Admin Features:**
   - **Apply Windows 11 System DoH:** Sets physical adapter DNS + Windows 11 native DoH templates (`HKLM\...\Dnscache`) so non-browser apps use DoH too; automatically snapshots and restores your previous DNS settings on Stop/Exit.
   - **Block QUIC UDP 443 via Firewall:** Adds outbound rule `NologGuard_Block_QUIC_UDP_443` while protected and deletes it cleanly on Stop/Exit.
7. **System Tray Optimization & Quick Pause:**
   - **Auto-Trim RAM in Tray:** When minimized/closed to System Tray, compacts GC heaps and trims working set (`SetProcessWorkingSetSize` + `EmptyWorkingSet`), reducing background memory footprint dramatically (measured ~1–16 MB working set in tray).
   - **Tray Quick Pause:** Right-click the tray icon to pause **AdBlock (5 min)** or **Shield (15 min)** with an automatic countdown timer, or click **Resume Now**.
8. **Zero-Trace Privacy, 18 Languages & CLI Tools:**
   - Logs live strictly in volatile RAM (last 300 lines, never written to disk).
   - 18 built-in languages with Windows UI auto-detection (`English`, `Tiếng Việt`, `简体中文`, `繁體中文`, `日本語`, `한국어`, `Русский`, `Українська`, `Türkçe`, `Bahasa Indonesia`, `ภาษาไทย`, `Español`, `Português`, `Français`, `Deutsch`, `Italiano`, `Polski`, `हिन्दी`).
   - Manual **Check for Updates** button in Settings (queries GitHub Releases only when clicked).
   - CLI recovery/automation flags: `--minimized`, `--recover`, `--exit`.

---

## User Guide

### Installation
* **Installer (`NologGuard-Setup.exe`):** Download from [GitHub Releases](https://github.com/NologX-com/NologGuard/releases), run and follow the wizard (or install silently with `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART`).
* **Portable (`NologGuard.exe`):** Single ~260 KB standalone executable — zero installation, zero drivers, zero external runtimes required on Windows 10/11.

### Daily Use (Standard `USER` Mode — Recommended)
1. Launch **NologGuard** (no Administrator prompt needed for browser protection).
2. Click the blue **ENABLE SHIELD** button. The status turns green (**PROTECTED**) and Windows browsers automatically route DNS/TLS through `127.0.0.1:8899`.
3. Click **Self-Check** next to the DoH dropdown to confirm live DoH resolution latency (e.g., `dns.quad9.net`) and verify `AdBlock 403 Loopback OK`.

### Tab Overview
* **Shield (Dashboard):** Toggle protection, choose DoH provider & SNI split mode, run **Self-Check**, toggle System Proxy / AdBlock / Admin features, and view live counters (`SNI-Split`, `Ads Blocked`, `DoH (Cached)`, `Bypassed`).
* **AdBlock:** Pick a preset (**Built-in Starter** works offline right away; select **hostsVN**, **HaGeZi**, **OISD**, **StevenBlack**, or **Custom Filter URL** and click **Update List** to download), or add custom rules (`ads.example.com` to block, `@@safe.example.com` to allow).
* **Whitelist:** Add banking, government, or local domains (`*.domain.com`) that should bypass SNI splitting and AdBlock.
* **Settings:** Customize local proxy port (`8899`), initial chunk size (`2` bytes), inter-chunk delay (`2` ms), custom DoH URL, interface language, startup behavior (`Auto-Protect on Launch`, `Minimize to Tray`, `Start with Windows`), manual **Check for Updates**, and **Emergency Reset Windows Proxy & Firewall**.
* **Live Log:** View recent proxy/DoH activity in RAM (`Clear Log` anytime).
* **System Tray Icon:** Click **✕** to minimize cleanly into the System Tray (saving RAM automatically). Right-click the green/gray shield icon to **Pause AdBlock (5 min)**, **Pause Shield (15 min)**, **Toggle Shield**, or **Exit**.

### When Should You Click "Relaunch as Admin"?
* Normal web browsing (`Chrome`, `Edge`, `Firefox`) needs **ZERO Admin rights** (`USER` mode).
* Only click **Relaunch as Admin** if you want to turn on **Apply Windows 11 System DoH** (for desktop apps outside browsers) or **Block QUIC UDP 443 via Firewall**.

### Troubleshooting / Recovery
* If Windows loses power while the shield is active and browsers show "Unable to connect to the proxy server": simply reopen **NologGuard** (auto-recovers on startup), click **Emergency Reset** in Settings, or run `NologGuard.exe --recover`.

---

## Build From Source (Zero Extra Tools Required)

Every Windows 10/11 machine includes the `.NET Framework` C# compiler (`csc.exe`):

```bat
build.bat
```

To also run the live Quad9 DoH + HTTPS tunnel test:

```bat
build.bat --live
```

Or build with MSBuild:

```bat
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" NologGuard.csproj /p:Configuration=Release /p:Platform=x64
```

Installer packaging & publishing guide: [installer/NologGuard.iss](installer/NologGuard.iss) • [docs/PUBLISHING.md](docs/PUBLISHING.md)

---

## License & Author

MIT License — see [LICENSE](LICENSE).  
Created by **[NologX.com](https://nologx.com)** — [https://nologx.com](https://nologx.com)
