# NologGuard Privacy Policy

**Effective Date:** 2026-11-17  
**Developer:** [NologX.com](https://nologx.com)

NologGuard is an open-source, client-side Windows privacy utility. We do not operate tracking servers and do not collect, store, sell, or share your personal information or browsing history.

## 1. Zero Browsing History & DNS Logs on Disk
- All proxy routing, DNS-over-HTTPS (DoH) resolution, TLS SNI splitting, and domain-level AdBlock filtering run **100% locally on your device**.
- The **Live Log** tab keeps only the most recent 300 entries **strictly in volatile RAM** (when enabled by you) and discards them immediately on close. Visited domains are **never written to disk** or transmitted to NologX.com.
- Local preferences (`%APPDATA%\NologGuard\config.json`) store only your settings (selected DoH provider, ports, whitelist, custom block rules, UI language).

## 2. Third-Party Network Connections Initiated by Your Choices
NologGuard only makes outbound requests strictly needed for features you choose to enable:
- **Encrypted DNS (DoH):** Sends DNS queries directly to your selected DoH provider (`Quad9`, `Cloudflare`, `Google`, `AdGuard`, or your custom DoH URL). Please refer to your chosen resolver's privacy policy.
- **Filter List Updates:** Downloads community blocklists (`hostsVN`, `HaGeZi`, `OISD`, `StevenBlack`, or your custom filter URL) **only when you click Update List**.
- **Manual Update Check:** Contacts the public GitHub Releases API (`api.github.com`) **only when you click Check for Updates** in Settings. NologGuard never performs background telemetry or automatic update polling.

## 3. Windows Settings & Clean Restoration
- NologGuard modifies your Windows User Proxy (`HKCU`) and—only with explicit opt-in and Administrator approval—Windows 11 adapter DNS or the `NologGuard_Block_QUIC_UDP_443` Windows Defender Firewall rule.
- Your previous settings are backed up locally in `%APPDATA%\NologGuard\backup_state.json` and automatically restored when you disable the shield, close/uninstall NologGuard, or run `NologGuard.exe --recover`.

## 4. Contact
Questions or privacy inquiries: [https://nologx.com](https://nologx.com) or via the official GitHub repository issues page.
