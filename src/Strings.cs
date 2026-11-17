using System;
using System.Collections.Generic;
using System.Globalization;

namespace NologGuard
{
    public sealed class LanguageInfo
    {
        public string Code { get; private set; }
        public string NativeName { get; private set; }

        public LanguageInfo(string code, string nativeName)
        {
            Code = code;
            NativeName = nativeName;
        }
    }

    /// <summary>
    /// Multi-language UI localization table supporting 32 languages with English fallback.
    /// </summary>
    public static class Strings
    {
        public static readonly LanguageInfo[] Languages = new LanguageInfo[]
        {
            new LanguageInfo("en", "English"),
            new LanguageInfo("vi", "Tiếng Việt"),
            new LanguageInfo("zh", "简体中文"),
            new LanguageInfo("zh-TW", "繁體中文"),
            new LanguageInfo("ja", "日本語"),
            new LanguageInfo("ko", "한국어"),
            new LanguageInfo("ru", "Русский"),
            new LanguageInfo("uk", "Українська"),
            new LanguageInfo("tr", "Türkçe"),
            new LanguageInfo("ar", "العربية"),
            new LanguageInfo("fa", "فارسی"),
            new LanguageInfo("he", "עברית"),
            new LanguageInfo("id", "Bahasa Indonesia"),
            new LanguageInfo("ms", "Bahasa Melayu"),
            new LanguageInfo("th", "ภาษาไทย"),
            new LanguageInfo("bn", "বাংলা"),
            new LanguageInfo("hi", "हिन्दी"),
            new LanguageInfo("es", "Español"),
            new LanguageInfo("pt", "Português"),
            new LanguageInfo("fr", "Français"),
            new LanguageInfo("de", "Deutsch"),
            new LanguageInfo("it", "Italiano"),
            new LanguageInfo("nl", "Nederlands"),
            new LanguageInfo("pl", "Polski"),
            new LanguageInfo("cs", "Čeština"),
            new LanguageInfo("ro", "Română"),
            new LanguageInfo("hu", "Magyar"),
            new LanguageInfo("el", "Ελληνικά"),
            new LanguageInfo("sv", "Svenska"),
            new LanguageInfo("nb", "Norsk"),
            new LanguageInfo("da", "Dansk"),
            new LanguageInfo("fi", "Suomi")
        };

        private static readonly Dictionary<string, Dictionary<string, string>> Catalog = BuildCatalog();

        public static bool IsSupported(string code)
        {
            if (string.IsNullOrEmpty(code)) return false;
            return Catalog.ContainsKey(code);
        }

        public static bool IsRightToLeft(string code)
        {
            return string.Equals(code, "ar", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(code, "fa", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(code, "he", StringComparison.OrdinalIgnoreCase);
        }

        public static bool HasKey(string lang, string key)
        {
            Dictionary<string, string> dict;
            return !string.IsNullOrEmpty(lang) && Catalog.TryGetValue(lang, out dict) && dict.ContainsKey(key);
        }

        public static string ResolveSystemLanguage()
        {
            try
            {
                CultureInfo ci = CultureInfo.CurrentUICulture;
                string full = ci.Name ?? string.Empty;
                if (full.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(full, "zh-TW", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(full, "zh-HK", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(full, "zh-MO", StringComparison.OrdinalIgnoreCase))
                {
                    return "zh-TW";
                }
                if (full.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                {
                    return "zh";
                }
                string two = (ci.TwoLetterISOLanguageName ?? string.Empty).ToLowerInvariant();
                if (two == "no" || two == "nn") return "nb";
                if (two == "iw") return "he";
                if (two == "in") return "id";
                if (IsSupported(two))
                {
                    return two;
                }
            }
            catch
            {
            }
            return "en";
        }

        public static string Get(string lang, string key)
        {
            Dictionary<string, string> dict;
            if (!string.IsNullOrEmpty(lang) && Catalog.TryGetValue(lang, out dict))
            {
                string val;
                if (dict.TryGetValue(key, out val))
                {
                    return val;
                }
            }

            Dictionary<string, string> enDict;
            if (Catalog.TryGetValue("en", out enDict))
            {
                string fallback;
                if (enDict.TryGetValue(key, out fallback))
                {
                    return fallback;
                }
            }
            return key;
        }

        public static ICollection<string> GetAllKeys()
        {
            return Catalog["en"].Keys;
        }

        private static Dictionary<string, Dictionary<string, string>> BuildCatalog()
        {
            Dictionary<string, Dictionary<string, string>> cat =
                new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            // 1. English
            cat["en"] = Pack(
                "DoH + SNI Fragmentation + AdBlock • Keeps Real IP",
                "Shield", "AdBlock", "Whitelist", "Settings", "Live Log",
                "SHIELD DISABLED", "PROTECTED", "ENABLE SHIELD", "DISABLE SHIELD",
                "1-click to enable local DoH Proxy + SNI splitting + QUIC bypass.",
                "DoH Provider:", "SNI Split Mode:", "Test DoH",
                "Auto-set Windows System Proxy (Recommended)",
                "Enable AdBlocker & Anti-Tracking (Domain Shield)",
                "Apply Windows 11 System DoH (Requires Admin)",
                "Block QUIC UDP 443 via Firewall (Requires Admin)",
                "Relaunch as Admin", "ADMIN", "USER",
                "Smart SNI Middle Split (Recommended)", "Fixed Small Chunks", "Off (DoH Proxy Only)",
                "SNI-Split", "Ads Blocked", "DoH (Cached)", "Bypassed",
                "Honest scope: Keeps real IP, encrypts DNS, splits SNI, and blocks 3rd-party ad/tracker domains (does not inspect inside HTTPS for first-party video ads like YouTube).",
                "Filter List:", "Update List", "Active: {0} blocked domains | {1} @@ allow rules",
                "Custom block/allow rules (one per line; prefix @@ to allow/unblock; supports hosts & AdBlock syntax):",
                "AdBlock rules saved and applied live.",
                "Blocks ad/tracker domains at the proxy layer before DNS lookup without HTTPS MitM decryption.",
                "Domains & networks excluded from SNI splitting (one per line, supports *.domain.com and # comments):",
                "Add", "Save Changes", "Reset Defaults", "Whitelist saved and applied live.",
                "Local Proxy Port (1024-65535):", "Initial Chunk Size (bytes):", "Inter-Chunk Delay (ms):",
                "Custom DoH URL (https://...):", "Interface Language:",
                "Minimize to system tray on close", "Launch automatically on Windows startup",
                "Show domains in Live Log tab (RAM-only, never written to disk)",
                "Emergency Reset Windows Proxy & Firewall", "Settings saved.",
                "Logs live strictly in memory (last 300 entries) and are never saved to disk.", "Clear Log",
                "Open NologGuard", "Toggle Shield", "Exit (Disables Shield)", "NologGuard is still running in the system tray.",
                "Administrator Required",
                "System-wide Windows DoH and Firewall QUIC blocking require Administrator privileges. Restart NologGuard as Administrator now?",
                "? Why do some options need Admin?", "Why Do Some Options Require Administrator?",
                "1. Daily browser protection needs ZERO Admin rights:\n" +
                "   In normal USER mode, NologGuard already enables System Proxy, HTTP/2 DoH, SNI splitting, and AdBlock for Chrome, Edge, Firefox, etc., because proxy settings live under your user registry (HKCU).\n\n" +
                "2. Only 2 optional system-wide features require Admin (enforced by Windows):\n" +
                "   • Apply Windows 11 System DoH: Windows restricts modifying physical network adapter DNS and HKLM\\SYSTEM\\...\\Dnscache settings to Administrators.\n" +
                "   • Block QUIC UDP 443 via Firewall: Adding/removing Windows Defender Firewall rules requires Administrator privileges.\n\n" +
                "3. Safe & Reversible:\n" +
                "   NologGuard snapshots your original DNS settings first and automatically restores them and deletes its firewall rule when you disable the shield or exit.",
                "Restored Windows Proxy, DNS, and Firewall state cleanly."
            );

            // 2. Vietnamese
            cat["vi"] = Pack(
                "DoH + Cắt SNI + Chặn QC/Tracking • Giữ nguyên IP thật",
                "Bảo vệ", "Chặn QC", "Ngoại lệ", "Cài đặt", "Nhật ký",
                "CHƯA BẬT BẢO VỆ", "ĐANG BẢO VỆ", "BẬT BẢO VỆ", "TẮT BẢO VỆ",
                "Nhấn 1 nút để bật Proxy DoH + cắt nhỏ SNI + tránh QUIC.",
                "Máy chủ DoH:", "Kiểu cắt SNI:", "Kiểm tra DoH",
                "Tự gán Windows System Proxy (Khuyên dùng)",
                "Bật chặn quảng cáo & trình theo dõi (AdBlock / Anti-Tracking)",
                "Bật DoH toàn Windows 11 (cần Admin)",
                "Chặn QUIC UDP 443 qua Firewall (cần Admin)",
                "Chạy quyền Admin", "ADMIN", "USER",
                "Cắt giữa tên miền SNI (Khuyên dùng)", "Cắt nhiều mảnh nhỏ (Fixed Chunks)", "Tắt cắt SNI (Chỉ dùng DoH)",
                "Đã cắt SNI", "QC / Tracker chặn", "DoH (Cache)", "Ngoại lệ",
                "Lưu ý: Giữ IP thật, mã hóa DNS (DoH), cắt SNI và chặn tên miền quảng cáo/tracker bên thứ 3 (chưa thay thế hoàn toàn AdBlocker trong trình duyệt cho quảng cáo cùng tên miền như YouTube).",
                "Bộ lọc:", "Tải / Cập nhật", "Đang hoạt động: {0} tên miền chặn | {1} ngoại lệ @@",
                "Quy tắc riêng (mỗi dòng 1 tên miền; thêm @@ phía trước để bỏ chặn; hỗ trợ cú pháp hosts / AdBlock):",
                "Đã lưu và áp dụng quy tắc chặn.",
                "Chặn ngay ở tầng Proxy/DNS nên tác dụng với cả trình duyệt và app Windows. Không giải mã HTTPS.",
                "Danh sách web/mạng bỏ qua cắt SNI (mỗi dòng 1 mục, hỗ trợ *.tenmien.com và dấu # ghi chú):",
                "Thêm", "Lưu thay đổi", "Mặc định", "Đã lưu danh sách ngoại lệ (áp dụng ngay lập tức).",
                "Cổng Local Proxy (1024-65535):", "Kích thước mảnh đầu (bytes):", "Độ trễ giữa các mảnh (ms):",
                "URL DoH tùy chỉnh (https://...):", "Ngôn ngữ giao diện:",
                "Thu nhỏ xuống khay hệ thống khi bấm X", "Tự khởi động cùng Windows",
                "Hiển thị tên miền ở tab Nhật ký (chỉ lưu RAM, không ghi đĩa)",
                "Khôi phục khẩn cấp Proxy & Firewall Windows", "Đã lưu cài đặt.",
                "Nhật ký chỉ lưu tạm trong RAM (tối đa 300 dòng) và tự xóa khi đóng app.", "Xóa nhật ký",
                "Mở NologGuard", "Bật / Tắt bảo vệ", "Thoát hẳn (và tắt bảo vệ)", "NologGuard vẫn đang chạy nền trong khay hệ thống.",
                "Cần quyền Administrator",
                "Tùy chọn DoH toàn Windows / Chặn Firewall QUIC cần quyền Administrator. Bạn có muốn khởi động lại NologGuard với quyền Admin ngay bây giờ không?",
                "? Vì sao một số mục cần Admin?", "Vì sao có tùy chọn yêu cầu quyền Administrator?",
                "1. Dùng hằng ngày cho trình duyệt KHÔNG CẦN quyền Admin:\n" +
                "   Ở chế độ USER bình thường, NologGuard đã tự bật System Proxy, DoH HTTP/2, cắt SNI và chặn quảng cáo/tracker cho Chrome, Edge, Firefox... vì cài đặt Proxy nằm trong vùng tài khoản cá nhân (HKCU).\n\n" +
                "2. Chỉ 2 tùy chọn nâng cao toàn hệ thống mới cần Admin (do Windows khóa quyền):\n" +
                "   • Bật DoH toàn Windows 11: Windows chỉ cho quyền Admin sửa DNS trên card mạng vật lý và khóa hệ thống HKLM\\SYSTEM\\...\\Dnscache (để app ngoài trình duyệt cũng dùng DoH).\n" +
                "   • Chặn QUIC UDP 443 qua Firewall: Thêm/xóa rule trong Windows Defender Firewall cần quyền Admin để chống phần mềm lạ tự mở tường lửa.\n\n" +
                "3. An toàn & hoàn nguyên:\n" +
                "   NologGuard tự sao lưu DNS cũ trước khi đổi và tự động trả về nguyên trạng + xóa rule Firewall khi bạn bấm Tắt bảo vệ hoặc thoát app.",
                "Đã khôi phục cài đặt Proxy, DNS và quy tắc Firewall về trạng thái sạch."
            );

            // 3. Simplified Chinese
            cat["zh"] = Pack(
                "DoH + SNI 分片 + 广告拦截 • 保留真实 IP",
                "防护", "去广告", "白名单", "设置", "日志",
                "防护未开启", "正在防护", "开启防护", "关闭防护",
                "一键启用本地 DoH 代理、SNI 分片与 QUIC 绕过。",
                "DoH 服务商:", "SNI 分片模式:", "测试 DoH",
                "自动设置 Windows 系统代理 (推荐)",
                "启用广告与追踪器拦截 (域名级过滤)",
                "启用 Windows 11 全局 DoH (需要管理员)",
                "通过防火墙拦截 QUIC UDP 443 (需要管理员)",
                "以管理员重启", "管理员", "普通用户",
                "智能 SNI 中间分片 (推荐)", "固定小分片", "关闭分片 (仅 DoH 代理)",
                "SNI 分片", "已拦截广告", "DoH (缓存)", "已放行",
                "说明：保留真实 IP，加密 DNS 并分片 SNI，同时拦截第三方广告/追踪域名（无法拦截 YouTube 等同源视频内嵌广告）。",
                "过滤列表:", "更新规则", "已加载: {0} 条拦截域名 | {1} 条 @@ 放行规则",
                "自定义规则（每行一个域名；前缀 @@ 表示放行；支持 hosts / AdBlock 语法）：",
                "拦截规则已保存并生效。",
                "在代理层 DNS 解析前直接拦截，无需 HTTPS 中间人解密。",
                "跳过 SNI 分片的域名或网络（每行一条，支持 *.example.com 和 # 注释）：",
                "添加", "保存更改", "恢复默认", "白名单已保存并立即生效。",
                "本地代理端口 (1024-65535):", "初始分片大小 (字节):", "分片间隔延迟 (毫秒):",
                "自定义 DoH URL (https://...):", "界面语言:",
                "关闭窗口时最小化到系统托盘", "随 Windows 开机自启",
                "在日志标签页显示域名（仅存内存，绝不写入硬盘）",
                "紧急重置 Windows 代理与防火墙", "设置已保存。",
                "日志仅保存在内存中（最多 300 条），关闭程序即清除。", "清空日志",
                "打开 NologGuard", "开启 / 关闭防护", "完全退出（并关闭防护）", "NologGuard 仍在系统托盘后台运行。",
                "需要管理员权限",
                "启用 Windows 全局 DoH 或防火墙拦截 QUIC 需要管理员权限。立即以管理员身份重启 NologGuard 吗？",
                "? 为何部分选项需要管理员权限?", "为何部分系统选项需要管理员权限？",
                "1. 日常浏览器防护无需管理员权限：\n" +
                "   在普通用户模式下，NologGuard 已可为 Chrome、Edge、Firefox 等自动配置系统代理、HTTP/2 DoH、SNI 分片与广告拦截。\n\n" +
                "2. 仅以下两项系统级功能受 Windows 安全策略限制需要管理员：\n" +
                "   • Windows 11 全局 DoH：修改物理网卡 DNS 与系统 HKLM 注册表需要管理员。\n" +
                "   • 防火墙拦截 QUIC UDP 443：增删 Windows 防火墙规则需要管理员。\n\n" +
                "3. 安全可恢复：\n" +
                "   开启前会自动备份原 DNS，关闭防护或退出时自动恢复原状并删除临时防火墙规则。",
                "已干净恢复 Windows 代理、DNS 与防火墙状态。"
            );

            // 4. Traditional Chinese
            cat["zh-TW"] = Pack(
                "DoH + SNI 分片 + 廣告封鎖 • 保留真實 IP",
                "防護", "擋廣告", "白名單", "設定", "日誌",
                "防護未啟用", "防護運作中", "啟用防護", "停用防護",
                "一鍵啟用本機 DoH 代理、SNI 分片與 QUIC 繞過。",
                "DoH 供應商:", "SNI 分片模式:", "測試 DoH",
                "自動設定 Windows 系統代理 (建議)",
                "啟用廣告與追蹤器封鎖 (網域層防護)",
                "套用 Windows 11 全系統 DoH (需系統管理員)",
                "透過防火牆封鎖 QUIC UDP 443 (需系統管理員)",
                "以系統管理員重啟", "管理員", "一般用戶",
                "智慧 SNI 中段分片 (建議)", "固定小分片", "關閉分片 (僅 DoH 代理)",
                "SNI 分片", "已封鎖廣告", "DoH (快取)", "已略過",
                "說明：保留真實 IP、加密 DNS、分片 SNI，並封鎖第三方廣告/追蹤網域（不包含 YouTube 等同源內嵌影片廣告）。",
                "過濾清單:", "更新清單", "已載入: {0} 個封鎖網域 | {1} 條 @@ 允許規則",
                "自訂規則（每行一個網域；加上 @@ 前綴代表允許；支援 hosts / AdBlock 語法）：",
                "封鎖規則已儲存並即時套用。",
                "於代理層查詢 DNS 前即進行封鎖，不進行 HTTPS 中間人解密。",
                "排除 SNI 分片的網域與網路（每行一項，支援 *.example.com 與 # 註解）：",
                "新增", "儲存變更", "重設預設值", "白名單已儲存並即時生效。",
                "本機代理連接埠 (1024-65535):", "初始分片大小 (位元組):", "分片間隔延遲 (毫秒):",
                "自訂 DoH URL (https://...):", "介面語言:",
                "關閉視窗時最小化至系統匣", "隨 Windows 開機自動啟動",
                "於日誌分頁顯示網域（僅存於記憶體，絕不寫入磁碟）",
                "緊急重設 Windows 代理與防火牆", "設定已儲存。",
                "日誌僅暫存於記憶體中（最多 300 筆），結束程式即清除。", "清除日誌",
                "開啟 NologGuard", "切換防護", "完全結束（並停用防護）", "NologGuard 仍在系統匣背景執行。",
                "需要系統管理員權限",
                "全系統 Windows DoH 與防火牆 QUIC 封鎖需要系統管理員權限。立即以系統管理員身分重啟 NologGuard？",
                "? 為何部分選項需要管理員權限?", "為何部分選項需要系統管理員權限？",
                "1. 日常瀏覽器防護不需管理員權限：\n" +
                "   於一般使用者模式即可啟用系統代理、HTTP/2 DoH、SNI 分片與廣告封鎖。\n\n" +
                "2. 僅以下兩項全系統功能依 Windows 規範需要管理員權限：\n" +
                "   • Windows 11 全系統 DoH：需修改實體網路卡 DNS 與系統 HKLM 登錄檔。\n" +
                "   • 防火牆封鎖 QUIC UDP 443：需新增與刪除 Windows Defender 防火牆規則。\n\n" +
                "3. 安全且可自動還原：\n" +
                "   啟用前會先備份原始 DNS，停用防護或離開時自動還原並移除防火牆規則。",
                "已完整還原 Windows 代理、DNS 與防火牆設定。"
            );

            // 5. Japanese
            cat["ja"] = Pack(
                "DoH + SNI分割 + 広告ブロック • 実IP維持",
                "保護", "広告拒否", "除外", "設定", "ログ",
                "保護停止中", "保護有効", "保護を有効化", "保護を無効化",
                "1クリックでローカル DoH プロキシ + SNI 分割 + QUIC 回避を有効にします。",
                "DoH プロバイダ:", "SNI 分割モード:", "DoH テスト",
                "Windows システムプロキシを自動設定 (推奨)",
                "広告・トラッカーブロックを有効化 (ドメイン保護)",
                "Windows 11 システム全体の DoH を適用 (管理者権限)",
                "ファイアウォールで QUIC UDP 443 を遮断 (管理者権限)",
                "管理者として再起動", "管理者", "標準",
                "スマート SNI 中央分割 (推奨)", "固定小チャンク分割", "オフ (DoH プロキシのみ)",
                "SNI分割", "広告遮断", "DoH (キャッシュ)", "除外",
                "仕様: 実IPを維持したままDNSを暗号化し、SNI分割とサードパーティ広告・トラッカーの遮断を行います（YouTube等の同一ドメイン動画広告は対象外）。",
                "フィルター:", "リスト更新", "有効: {0} 件のブロック | {1} 件の @@ 許可",
                "カスタムルール（1行に1ドメイン、@@ を先頭に付けると許可、hosts / AdBlock 形式対応）：",
                "広告ブロックルールを保存し適用しました。",
                "DNS解決前にプロキシ層でブロックします（HTTPS複号化は行いません）。",
                "SNI 分割から除外するドメイン・ネットワーク（1行1項目、*.example.com と # コメント対応）：",
                "追加", "変更を保存", "初期値に戻す", "ホワイトリストを保存しました。",
                "ローカルプロキシポート (1024-65535):", "初期チャンクサイズ (bytes):", "チャンク間遅延 (ms):",
                "カスタム DoH URL (https://...):", "表示言語:",
                "閉じる時にタスクトレイへ最小化", "Windows 起動時に自動起動",
                "ログタブにドメインを表示（メモリのみ保持・ディスク保存なし）",
                "Windows プロキシ・ファイアウォール緊急リセット", "設定を保存しました。",
                "ログはメモリ内にのみ保持され（最大300件）、ディスクには一切保存されません。", "ログ消去",
                "NologGuard を開く", "保護の切り替え", "終了（保護を停止）", "NologGuard はタスクトレイで動作中です。",
                "管理者権限が必要です",
                "システム全体の DoH 設定およびファイアウォールでの QUIC 遮断には管理者権限が必要です。管理者として再起動しますか？",
                "? なぜ管理者権限が必要ですか?", "一部の機能で管理者権限が必要な理由",
                "1. 通常のブラウザ保護には管理者権限は不要です:\n" +
                "   標準ユーザー権限でも、システムプロキシ・HTTP/2 DoH・SNI分割・広告ブロックはすべて利用できます。\n\n" +
                "2. 次の2つのシステム全体オプションのみ Windows の仕様上、管理者権限が必要です:\n" +
                "   • Windows 11 システム DoH: ネットワークアダプタの DNS と HKLM レジストリの変更。\n" +
                "   • ファイアウォール QUIC 遮断: Windows Defender ファイアウォール規則の追加・削除。\n\n" +
                "3. 自動復元:\n" +
                "   変更前に元の DNS をバックアップし、無効化または終了時に自動で復元します。",
                "Windows プロキシ、DNS、ファイアウォールを正常に復元しました。"
            );

            // 6. Korean
            cat["ko"] = Pack(
                "DoH + SNI 분할 + 광고 차단 • 실제 IP 유지",
                "보호", "광고 차단", "예외", "설정", "로그",
                "보호 꺼짐", "보호 활성", "보호 켜기", "보호 끄기",
                "원클릭으로 로컬 DoH 프록시 + SNI 분할 + QUIC 우회를 활성화합니다.",
                "DoH 공급자:", "SNI 분할 모드:", "DoH 테스트",
                "Windows 시스템 프록시 자동 설정 (권장)",
                "광고 및 추적기 차단 사용 (도메인 보호)",
                "Windows 11 시스템 전체 DoH 적용 (관리자 필요)",
                "방화벽으로 QUIC UDP 443 차단 (관리자 필요)",
                "관리자로 재실행", "관리자", "사용자",
                "스마트 SNI 중앙 분할 (권장)", "고정 소형 청크", "끔 (DoH 프록시 전용)",
                "SNI 분할", "차단된 광고", "DoH (캐시)", "예외됨",
                "안내: 실제 IP를 유지하며 DNS 암호화, SNI 분할 및 타사 광고/추적기 도메인을 차단합니다 (YouTube 등 자사 영상 광고는 제외).",
                "필터 목록:", "목록 업데이트", "활성: 차단 도메인 {0}개 | @@ 허용 {1}개",
                "사용자 규칙 (한 줄에 하나, @@ 접두사로 허용, hosts / AdBlock 문법 지원):",
                "광고 차단 규칙이 저장 및 적용되었습니다.",
                "HTTPS 복호화 없이 DNS 조회 전 프록시 단계에서 즉시 차단합니다.",
                "SNI 분할에서 제외할 도메인 및 네트워크 (한 줄에 하나, *.example.com 및 # 주석 지원):",
                "추가", "변경 저장", "기본값 복원", "화이트리스트가 저장되어 즉시 적용되었습니다.",
                "로컬 프록시 포트 (1024-65535):", "초기 청크 크기 (bytes):", "청크 간 지연 (ms):",
                "사용자 지정 DoH URL (https://...):", "인터페이스 언어:",
                "닫을 때 시스템 트레이로 최소화", "Windows 시작 시 자동 실행",
                "로그 탭에 도메인 표시 (메모리에만 저장, 디스크 기록 안 함)",
                "Windows 프록시 및 방화벽 긴급 초기화", "설정이 저장되었습니다.",
                "로그는 메모리에만 임시 보관되며(최대 300줄) 디스크에 저장되지 않습니다.", "로그 지우기",
                "NologGuard 열기", "보호 켜기/끄기", "종료 (보호 중지)", "NologGuard이 시스템 트레이에서 계속 실행 중입니다.",
                "관리자 권한 필요",
                "시스템 전체 Windows DoH 및 방화벽 QUIC 차단은 관리자 권한이 필요합니다. 지금 관리자로 다시 시작할까요?",
                "? 왜 관리자 권한이 필요한가요?", "일부 옵션에 관리자 권한이 필요한 이유",
                "1. 일반 브라우저 보호에는 관리자 권한이 필요 없습니다:\n" +
                "   일반 사용자 모드에서도 시스템 프록시, HTTP/2 DoH, SNI 분할 및 광고 차단이 정상 작동합니다.\n\n" +
                "2. 다음 2가지 시스템 전체 옵션만 Windows 보안 정책상 관리자 권한이 필요합니다:\n" +
                "   • Windows 11 시스템 DoH: 물리 네트워크 어댑터 DNS 및 HKLM 시스템 레지스트리 변경.\n" +
                "   • 방화벽 QUIC UDP 443 차단: Windows Defender 방화벽 규칙 추가/삭제.\n\n" +
                "3. 안전한 자동 복구:\n" +
                "   변경 전 기존 DNS를 백업하며, 보호를 끄거나 종료하면 원래 상태로 자동 복원됩니다.",
                "Windows 프록시, DNS 및 방화벽 상태를 깔끔하게 복원했습니다."
            );

            // 7. Russian
            cat["ru"] = Pack(
                "DoH + Разделение SNI + AdBlock • Реальный IP",
                "Защита", "AdBlock", "Исключения", "Настройки", "Журнал",
                "ЗАЩИТА ОТКЛЮЧЕНА", "ЗАЩИТА ВКЛЮЧЕНА", "ВКЛЮЧИТЬ ЗАЩИТУ", "ОТКЛЮЧИТЬ ЗАЩИТУ",
                "Включите локальный DoH-прокси, фрагментацию SNI и обход QUIC одним кликом.",
                "DoH-сервер:", "Режим SNI:", "Тест DoH",
                "Автоматически настраивать системный прокси (Рекомендуется)",
                "Блокировка рекламы и трекеров (Фильтр доменов)",
                "Включить системный DoH в Windows 11 (Нужен Админ)",
                "Блокировать QUIC UDP 443 в Брандмауэре (Нужен Админ)",
                "Запуск от Админа", "АДМИН", "ЮЗЕР",
                "Умное разделение SNI (Рекомендуется)", "Мелкие фрагменты", "Выкл. (Только DoH)",
                "SNI-пакеты", "Блок. рекламы", "DoH (Кэш)", "Исключено",
                "Честно о возможностях: сохраняет ваш реальный IP, шифрует DNS, дробит SNI и блокирует стороннюю рекламу/трекеры (не вырезает встроенную видеорекламу вроде YouTube).",
                "Фильтр:", "Обновить", "Активно: {0} доменов в блоке | {1} исключений @@",
                "Свои правила (по одному на строку; @@ в начале для разрешения; формат hosts / AdBlock):",
                "Правила AdBlock сохранены и применены.",
                "Блокирует рекламные домены до DNS-запроса на уровне прокси без расшифровки HTTPS.",
                "Домены и сети, исключённые из фрагментации SNI (поддержка *.domain.com и # комментариев):",
                "Добавить", "Сохранить", "Сбросить", "Белый список сохранён и применён.",
                "Порт локального прокси (1024-65535):", "Размер первого фрагмента (байт):", "Задержка между фрагментами (мс):",
                "Свой URL DoH (https://...):", "Язык интерфейса:",
                "Сворачивать в трей при закрытии", "Запускать вместе с Windows",
                "Показывать домены во вкладке Журнал (только в ОЗУ, без записи на диск)",
                "Экстренный сброс прокси и Брандмауэра Windows", "Настройки сохранены.",
                "Журнал хранится только в оперативной памяти (до 300 строк) и никогда не пишется на диск.", "Очистить",
                "Открыть NologGuard", "Вкл / Выкл защиту", "Выход (Отключить защиту)", "NologGuard продолжает работать в системном трее.",
                "Требуются права администратора",
                "Системный DoH и блокировка QUIC через Брандмауэр требуют прав администратора. Перезапустить NologGuard от имени администратора?",
                "? Зачем нужны права администратора?", "Зачем отдельным функциям права администратора?",
                "1. Для обычной защиты браузеров права администратора НЕ нужны:\n" +
                "   В режиме обычного пользователя работают системный прокси (HKCU), DoH HTTP/2, фрагментация SNI и AdBlock.\n\n" +
                "2. Права администратора требуются Windows только для 2 общесистемных опций:\n" +
                "   • Системный DoH Windows 11: изменение DNS сетевого адаптера и ветки HKLM.\n" +
                "   • Блокировка QUIC UDP 443: добавление/удаление правила в Брандмауэре Windows.\n\n" +
                "3. Безопасный откат:\n" +
                "   NologGuard сохраняет прежний DNS и автоматически всё возвращает при выключении или выходе.",
                "Настройки прокси, DNS и Брандмауэра Windows успешно восстановлены."
            );

            // 8. Ukrainian
            cat["uk"] = Pack(
                "DoH + Фрагментація SNI + AdBlock • Реальний IP",
                "Захист", "AdBlock", "Винятки", "Параметри", "Журнал",
                "ЗАХИСТ ВИМКНЕНО", "ЗАХИСТ АКТИВНИЙ", "УВІМКНУТИ ЗАХИСТ", "ВИМКНУТИ ЗАХИСТ",
                "В один клік увімкніть локальний DoH-проксі, поділ SNI та обхід QUIC.",
                "Сервер DoH:", "Режим SNI:", "Тест DoH",
                "Автоматично налаштовувати системний проксі (Рекомендовано)",
                "Блокування реклами та трекерів (Фільтр доменів)",
                "Увімкнути системний DoH у Windows 11 (Потрібен Адмін)",
                "Блокувати QUIC UDP 443 у Брандмауері (Потрібен Адмін)",
                "Запуск від Адміна", "АДМІН", "КОРИСТ.",
                "Розумний поділ SNI (Рекомендовано)", "Фіксовані фрагменти", "Вимк. (Лише DoH)",
                "SNI-потоки", "Блок. реклами", "DoH (Кеш)", "Пропущено",
                "Чесні межі: зберігає реальний IP, шифрує DNS, ділить SNI та блокує сторонні рекламні домени (не блокує відеорекламу всередині YouTube).",
                "Фільтр:", "Оновити", "Активно: {0} заблокованих доменів | {1} винятків @@",
                "Власні правила (по одному на рядок; префікс @@ для дозволу; підтримка hosts / AdBlock):",
                "Правила AdBlock збережено та застосовано.",
                "Блокує рекламні домени на рівні проксі до DNS-запиту без розшифрування HTTPS.",
                "Домени та мережі поза поділом SNI (підтримка *.domain.com та # коментарів):",
                "Додати", "Зберегти", "Скинути", "Білий список збережено та застосовано.",
                "Порт локального проксі (1024-65535):", "Розмір початкового фрагмента (байт):", "Затримка між фрагментами (мс):",
                "Власний DoH URL (https://...):", "Мова інтерфейсу:",
                "Згортати в трей при закритті", "Запускати разом із Windows",
                "Показувати домени у вкладці Журнал (лише в ОЗП, без запису на диск)",
                "Екстрене скидання проксі та Брандмауера Windows", "Налаштування збережено.",
                "Журнал зберігається лише в оперативній пам'яті (до 300 рядків) і не записується на диск.", "Очистити",
                "Відкрити NologGuard", "Увімк / Вимк захист", "Вийти (Вимкнути захист)", "NologGuard продовжує працювати в системному треї.",
                "Потрібні права адміністратора",
                "Системний DoH та блокування QUIC через Брандмауер потребують прав адміністратора. Перезапустити NologGuard як адміністратор?",
                "? Навіщо потрібні права адміністратора?", "Чому окремі параметри вимагають прав адміністратора?",
                "1. Для щоденного захисту браузерів права адміністратора НЕ потрібні:\n" +
                "   У звичайному режимі працюють системний проксі (HKCU), DoH HTTP/2, поділ SNI та AdBlock.\n\n" +
                "2. Лише 2 загальносистемні опції обмежені захистом Windows:\n" +
                "   • Системний DoH Windows 11: зміна DNS мережевого адаптера та гілки HKLM.\n" +
                "   • Блокування QUIC UDP 443: додавання та видалення правил Брандмауера Windows.\n\n" +
                "3. Безпечне відновлення:\n" +
                "   NologGuard зберігає попередній DNS і автоматично все повертає при вимкненні чи виході.",
                "Проксі, DNS та правила Брандмауера Windows успішно відновлено."
            );

            // 9. Turkish
            cat["tr"] = Pack(
                "DoH + SNI Bölme + AdBlock • Gerçek IP Korunur",
                "Kalkan", "AdBlock", "İstisnalar", "Ayarlar", "Günlük",
                "KORUMA KAPALI", "KORUMA AKTİF", "KORUMAYI AÇ", "KORUMAYI KAPAT",
                "Yerel DoH Proxy + SNI bölme + QUIC atlamayı tek tıkla etkinleştirin.",
                "DoH Sağlayıcı:", "SNI Bölme Modu:", "DoH Test",
                "Windows Sistem Proxy'sini otomatik ayarla (Önerilen)",
                "Reklam ve İzleyici Engelleyiciyi Etkinleştir",
                "Windows 11 Sistem Geneli DoH Uygula (Yönetici)",
                "Güvenlik Duvarı ile QUIC UDP 443 Engelle (Yönetici)",
                "Yönetici Başlat", "YÖNETİCİ", "KULLANICI",
                "Akıllı SNI Orta Bölme (Önerilen)", "Sabit Küçük Parçalar", "Kapalı (Sadece DoH)",
                "SNI Bölünen", "Engellenen", "DoH (Önbellek)", "Atlanan",
                "Dürüst kapsam: Gerçek IP'nizi korur, DNS'i şifreler, SNI'ı böler ve üçüncü taraf reklam/izleyici alan adlarını engeller (YouTube video içi reklamlarını kaldırmaz).",
                "Filtre Listesi:", "Listeyi Güncelle", "Etkin: {0} engelli alan adı | {1} @@ izin kuralı",
                "Özel kurallar (satır başına bir adet; izin için @@ öneki; hosts / AdBlock biçimi desteklenir):",
                "AdBlock kuralları kaydedildi ve uygulandı.",
                "HTTPS çözme yapmadan DNS sorgusundan önce proxy katmanında engeller.",
                "SNI bölmeden hariç tutulan alan adları ve ağlar (satır başına bir; *.domain.com ve # destekler):",
                "Ekle", "Kaydet", "Varsayılanlar", "İstisna listesi kaydedildi ve uygulandı.",
                "Yerel Proxy Portu (1024-65535):", "İlk Parça Boyutu (bayt):", "Parçalar Arası Gecikme (ms):",
                "Özel DoH URL (https://...):", "Arayüz Dili:",
                "Kapatırken sistem tepsisine küçült", "Windows açılışında otomatik başlat",
                "Canlı Günlük sekmesinde alan adlarını göster (yalnızca RAM, diske yazılmaz)",
                "Windows Proxy ve Güvenlik Duvarını Acil Sıfırla", "Ayarlar kaydedildi.",
                "Günlükler yalnızca bellekte tutulur (son 300 kayıt) ve diske asla yazılmaz.", "Temizle",
                "NologGuard'i Aç", "Korumayı Aç/Kapat", "Çıkış (Korumayı Kapatır)", "NologGuard sistem tepsisinde çalışmaya devam ediyor.",
                "Yönetici İzni Gerekli",
                "Sistem geneli Windows DoH ve Güvenlik Duvarı QUIC engellemesi Yönetici izni gerektirir. Yönetici olarak yeniden başlatılsın mı?",
                "? Neden yönetici izni gerekiyor?", "Bazı Seçenekler Neden Yönetici İzni İster?",
                "1. Günlük tarayıcı koruması için Yönetici izni GEREKMEZ:\n" +
                "   Normal kullanıcı modunda sistem proxy'si (HKCU), HTTP/2 DoH, SNI bölme ve AdBlock sorunsuz çalışır.\n\n" +
                "2. Sadece 2 sistem geneli özellik Windows güvenliği nedeniyle Yönetici ister:\n" +
                "   • Windows 11 Sistem DoH: Fiziksel ağ bağdaştırıcısı DNS'i ve HKLM kayıt defteri değişikliği.\n" +
                "   • Güvenlik Duvarı QUIC Engeli: Windows Defender Güvenlik Duvarı kuralı ekleme/silme.\n\n" +
                "3. Güvenli Geri Alma:\n" +
                "   Eski DNS ayarlarınız yedeklenir ve koruma kapatıldığında otomatik geri yüklenir.",
                "Windows Proxy, DNS ve Güvenlik Duvarı ayarları temizce geri yüklendi."
            );

            // 10. Indonesian
            cat["id"] = Pack(
                "DoH + Pemecah SNI + AdBlock • IP Asli Tetap",
                "Perisai", "AdBlock", "Pengecualian", "Pengaturan", "Log",
                "PERISAI MATI", "TERLINDUNGI", "AKTIFKAN PERISAI", "MATIKAN PERISAI",
                "1-klik untuk mengaktifkan Proxy DoH lokal + pemecah SNI + bypass QUIC.",
                "Penyedia DoH:", "Mode Pemecah SNI:", "Tes DoH",
                "Atur Windows System Proxy otomatis (Disarankan)",
                "Aktifkan Pemblokir Iklan & Pelacak (AdBlock)",
                "Terapkan DoH Sistem Windows 11 (Perlu Admin)",
                "Blokir QUIC UDP 443 via Firewall (Perlu Admin)",
                "Jalankan sbg Admin", "ADMIN", "USER",
                "Pemecah Tengah SNI Cerdas (Disarankan)", "Potongan Kecil Tetap", "Mati (Hanya Proxy DoH)",
                "SNI Dipecah", "Iklan Diblokir", "DoH (Cache)", "Dilewati",
                "Cakupan jujur: Menjaga IP asli, mengenkripsi DNS, memecah SNI, dan memblokir domain iklan/pelacak pihak ketiga (tidak memblokir iklan video YouTube).",
                "Daftar Filter:", "Perbarui Daftar", "Aktif: {0} domain diblokir | {1} aturan @@ izin",
                "Aturan kustom (satu per baris; awali @@ untuk mengizinkan; mendukung format hosts / AdBlock):",
                "Aturan AdBlock disimpan dan langsung diterapkan.",
                "Memblokir domain iklan di lapisan proxy sebelum lookup DNS tanpa dekripsi HTTPS.",
                "Domain & jaringan yang dikecualikan dari pemecahan SNI (mendukung *.domain.com & komentar #):",
                "Tambah", "Simpan", "Reset Default", "Daftar pengecualian disimpan dan diterapkan.",
                "Port Proxy Lokal (1024-65535):", "Ukuran Potongan Awal (byte):", "Jeda Antar Potongan (ms):",
                "URL DoH Kustom (https://...):", "Bahasa Antarmuka:",
                "Perkecil ke system tray saat ditutup", "Jalankan otomatis saat Windows mulai",
                "Tampilkan domain di tab Log (hanya di RAM, tidak disimpan ke disk)",
                "Reset Darurat Proxy & Firewall Windows", "Pengaturan disimpan.",
                "Log hanya disimpan di memori (maks. 300 baris) dan tidak pernah ditulis ke disk.", "Hapus Log",
                "Buka NologGuard", "Ubah Status Perisai", "Keluar (Matikan Perisai)", "NologGuard masih berjalan di system tray.",
                "Diperlukan Hak Administrator",
                "DoH sistem Windows dan pemblokiran QUIC Firewall memerlukan hak Administrator. Mulai ulang NologGuard sebagai Administrator?",
                "? Mengapa perlu izin Admin?", "Mengapa Beberapa Opsi Memerlukan Administrator?",
                "1. Penggunaan harian di browser TIDAK perlu hak Admin:\n" +
                "   Di mode USER biasa, System Proxy (HKCU), DoH HTTP/2, pemecahan SNI, dan AdBlock sudah berjalan normal.\n\n" +
                "2. Hanya 2 fitur tingkat sistem yang dibatasi Windows ke Administrator:\n" +
                "   • DoH Sistem Windows 11: Mengubah DNS adaptor fisik dan registry HKLM.\n" +
                "   • Blokir QUIC UDP 443: Menambah/menghapus aturan Windows Defender Firewall.\n\n" +
                "3. Aman & Pulih Otomatis:\n" +
                "   Pengaturan DNS lama dicadangkan dan dipulihkan otomatis saat dimatikan atau keluar.",
                "Pengaturan Proxy, DNS, dan Firewall Windows berhasil dipulihkan."
            );

            // 11. Thai
            cat["th"] = Pack(
                "DoH + แยกแพ็กเก็ต SNI + AdBlock • คง IP จริง",
                "ป้องกัน", "บล็อกโฆษณา", "ข้อยกเว้น", "ตั้งค่า", "บันทึก",
                "ปิดการป้องกัน", "กำลังป้องกัน", "เปิดการป้องกัน", "ปิดการป้องกัน",
                "คลิกเดียวเพื่อเปิด DoH Proxy + แยก SNI + ข้าม QUIC",
                "ผู้ให้บริการ DoH:", "โหมดแยก SNI:", "ทดสอบ DoH",
                "ตั้งค่า Windows System Proxy อัตโนมัติ (แนะนำ)",
                "เปิดบล็อกโฆษณาและตัวติดตาม (AdBlock)",
                "ใช้ DoH ทั้งระบบ Windows 11 (ต้องใช้สิทธิ์แอดมิน)",
                "บล็อก QUIC UDP 443 ผ่าน Firewall (ต้องใช้สิทธิ์แอดมิน)",
                "เปิดแบบแอดมิน", "แอดมิน", "ผู้ใช้",
                "แยกกึ่งกลางชื่อ SNI อัจฉริยะ (แนะนำ)", "แบ่งชิ้นเล็กคงที่", "ปิด (ใช้เฉพาะ DoH)",
                "แยก SNI แล้ว", "บล็อกโฆษณา", "DoH (แคช)", "ข้ามแล้ว",
                "หมายเหตุ: คง IP จริง เข้ารหัส DNS แยก SNI และบล็อกโดเมนโฆษณา/ติดตามภายนอก (ไม่รวมโฆษณาในวิดีโอ YouTube)",
                "รายการตัวกรอง:", "อัปเดตรายการ", "ใช้งาน: บล็อก {0} โดเมน | อนุญาต @@ {1} รายการ",
                "กฎกำหนดเอง (บรรทัดละ 1 โดเมน; เติม @@ ด้านหน้าเพื่ออนุญาต; รองรับ hosts / AdBlock):",
                "บันทึกและใช้กฎบล็อกโฆษณาแล้ว",
                "บล็อกที่ระดับ Proxy ก่อนค้นหา DNS โดยไม่ถอดรหัส HTTPS",
                "โดเมนและเครือข่ายที่ยกเว้นการแยก SNI (รองรับ *.domain.com และ # หมายเหตุ):",
                "เพิ่ม", "บันทึก", "ค่าเริ่มต้น", "บันทึกรายการข้อยกเว้นแล้ว",
                "พอร์ต Local Proxy (1024-65535):", "ขนาดชิ้นแรก (ไบต์):", "หน่วงเวลาระหว่างชิ้น (ms):",
                "URL DoH กำหนดเอง (https://...):", "ภาษาของโปรแกรม:",
                "ย่อลงถาดระบบเมื่อกดปิด", "เปิดอัตโนมัติพร้อม Windows",
                "แสดงโดเมนในแท็บบันทึก (เก็บใน RAM เท่านั้น ไม่บันทึกลงดิสก์)",
                "รีเซ็ตฉุกเฉิน Proxy และ Firewall ของ Windows", "บันทึกการตั้งค่าแล้ว",
                "บันทึกเก็บในหน่วยความจำเท่านั้น (สูงสุด 300 บรรทัด) และไม่เขียนลงดิสก์", "ล้างบันทึก",
                "เปิด NologGuard", "สลับการป้องกัน", "ออก (ปิดการป้องกัน)", "NologGuard ยังทำงานอยู่ในถาดระบบ",
                "ต้องใช้สิทธิ์ Administrator",
                "DoH ทั้งระบบและ Firewall บล็อก QUIC ต้องใช้สิทธิ์ Administrator ต้องการเปิดใหม่แบบ Administrator หรือไม่?",
                "? ทำไมต้องขอสิทธิ์ Admin?", "ทำไมบางตัวเลือกต้องใช้สิทธิ์ Administrator?",
                "1. การใช้งานเบราว์เซอร์ทั่วไปไม่ต้องใช้สิทธิ์ Admin:\n" +
                "   ในโหมดผู้ใช้ปกติ Proxy, HTTP/2 DoH, การแยก SNI และ AdBlock ทำงานได้ครบถ้วน\n\n" +
                "2. เฉพาะ 2 ตัวเลือกระดับระบบที่ Windows บังคับสิทธิ์ Admin:\n" +
                "   • DoH ทั้งระบบ Windows 11: แก้ไข DNS การ์ดเครือข่ายและค่า HKLM\n" +
                "   • บล็อก QUIC ผ่าน Firewall: เพิ่ม/ลบกฎใน Windows Defender Firewall\n\n" +
                "3. ปลอดภัยและคืนค่าอัตโนมัติ:\n" +
                "   สำรองค่า DNS เดิมไว้และคืนค่าอัตโนมัติเมื่อปิดหรือออกจากโปรแกรม",
                "คืนค่า Proxy, DNS และ Firewall เรียบร้อยแล้ว"
            );

            // 12. Spanish
            cat["es"] = Pack(
                "DoH + Fragmentación SNI + AdBlock • IP Real",
                "Escudo", "AdBlock", "Exclusiones", "Ajustes", "Registro",
                "ESCUDO DESACTIVADO", "PROTEGIDO", "ACTIVAR ESCUDO", "DESACTIVAR ESCUDO",
                "1 clic para activar Proxy DoH local + división SNI + omisión QUIC.",
                "Proveedor DoH:", "Modo de división SNI:", "Probar DoH",
                "Configurar Proxy de Windows automáticamente (Recomendado)",
                "Activar bloqueador de anuncios y rastreadores (AdBlock)",
                "Aplicar DoH global en Windows 11 (Requiere Admin)",
                "Bloquear QUIC UDP 443 por Firewall (Requiere Admin)",
                "Reiniciar como Admin", "ADMIN", "USUARIO",
                "División inteligente en medio del SNI (Recomendado)", "Fragmentos fijos", "Desactivado (Solo DoH)",
                "SNI dividido", "Bloqueados", "DoH (Caché)", "Omitidos",
                "Alcance honesto: Mantiene su IP real, cifra DNS, divide SNI y bloquea dominios de anuncios/rastreo externos (no filtra anuncios internos de video como YouTube).",
                "Lista de filtros:", "Actualizar", "Activo: {0} dominios bloqueados | {1} reglas @@",
                "Reglas personalizadas (una por línea; prefijo @@ para permitir; compatible con hosts / AdBlock):",
                "Reglas AdBlock guardadas y aplicadas.",
                "Bloquea dominios en el proxy antes de consultar DNS sin descifrar HTTPS.",
                "Dominios y redes excluidos de la división SNI (admite *.dominio.com y # comentarios):",
                "Añadir", "Guardar", "Restablecer", "Lista de exclusiones guardada y aplicada.",
                "Puerto Proxy Local (1024-65535):", "Tamaño del fragmento inicial (bytes):", "Retardo entre fragmentos (ms):",
                "URL DoH personalizada (https://...):", "Idioma de interfaz:",
                "Minimizar a la bandeja del sistema al cerrar", "Iniciar automáticamente con Windows",
                "Mostrar dominios en Registro (solo en RAM, nunca en disco)",
                "Restablecimiento de emergencia de Proxy y Firewall", "Ajustes guardados.",
                "El registro solo vive en memoria (últimas 300 líneas) y nunca se guarda en disco.", "Limpiar",
                "Abrir NologGuard", "Alternar Escudo", "Salir (Desactiva Escudo)", "NologGuard sigue ejecutándose en la bandeja.",
                "Se requiere Administrador",
                "DoH del sistema y el bloqueo QUIC en Firewall requieren permisos de Administrador. ¿Reiniciar NologGuard como Administrador?",
                "? ¿Por qué pide Admin?", "¿Por qué algunas opciones piden Administrador?",
                "1. El uso diario en navegadores NO necesita Admin:\n" +
                "   Como usuario estándar funcionan el Proxy del sistema (HKCU), DoH HTTP/2, división SNI y AdBlock.\n\n" +
                "2. Solo 2 funciones globales requieren Admin por seguridad de Windows:\n" +
                "   • DoH global de Windows 11: modifica DNS del adaptador y clave HKLM.\n" +
                "   • Bloqueo QUIC UDP 443: añade y elimina reglas de Windows Defender Firewall.\n\n" +
                "3. Seguro y reversible:\n" +
                "   NologGuard guarda su DNS previo y lo restaura automáticamente al apagar o salir.",
                "Proxy, DNS y Firewall restaurados correctamente."
            );

            // 13. Portuguese
            cat["pt"] = Pack(
                "DoH + Fragmentação SNI + AdBlock • IP Real",
                "Escudo", "AdBlock", "Exceções", "Ajustes", "Registro",
                "ESCUDO DESATIVADO", "PROTEGIDO", "ATIVAR ESCUDO", "DESATIVAR ESCUDO",
                "1 clique para ativar Proxy DoH local + divisão SNI + bypass QUIC.",
                "Provedor DoH:", "Modo de divisão SNI:", "Testar DoH",
                "Definir Proxy do Windows automaticamente (Recomendado)",
                "Ativar bloqueador de anúncios e rastreadores (AdBlock)",
                "Aplicar DoH global no Windows 11 (Requer Admin)",
                "Bloquear QUIC UDP 443 via Firewall (Requer Admin)",
                "Reiniciar como Admin", "ADMIN", "USUÁRIO",
                "Divisão inteligente no meio do SNI (Recomendado)", "Blocos pequenos fixos", "Desligado (Só DoH)",
                "SNI dividido", "Bloqueados", "DoH (Cache)", "Ignorados",
                "Escopo honesto: Mantém seu IP real, criptografa DNS, divide SNI e bloqueia domínios de anúncios/rastreadores externos (não remove anúncios internos do YouTube).",
                "Lista de filtros:", "Atualizar", "Ativo: {0} domínios bloqueados | {1} regras @@",
                "Regras personalizadas (uma por linha; prefixo @@ para liberar; aceita hosts / AdBlock):",
                "Regras de AdBlock salvas e aplicadas.",
                "Bloqueia no proxy antes da consulta DNS sem descriptografar HTTPS.",
                "Domínios e redes fora da divisão SNI (suporta *.dominio.com e comentários #):",
                "Adicionar", "Salvar", "Padrão", "Lista de exceções salva e aplicada.",
                "Porta do Proxy Local (1024-65535):", "Tamanho do bloco inicial (bytes):", "Atraso entre blocos (ms):",
                "URL DoH personalizada (https://...):", "Idioma da interface:",
                "Minimizar para a bandeja ao fechar", "Iniciar automaticamente com o Windows",
                "Exibir domínios no Registro (apenas RAM, nunca gravado em disco)",
                "Redefinição de emergência de Proxy e Firewall", "Configurações salvas.",
                "O registro fica apenas na memória (300 linhas) e nunca é salvo no disco.", "Limpar",
                "Abrir NologGuard", "Alternar Escudo", "Sair (Desativa Escudo)", "NologGuard continua ativo na bandeja do sistema.",
                "Administrador necessário",
                "DoH do sistema e bloqueio QUIC no Firewall exigem privilégios de Administrador. Reiniciar o NologGuard como Administrador?",
                "? Por que pede Admin?", "Por que algumas opções pedem Administrador?",
                "1. O uso diário em navegadores NÃO precisa de Admin:\n" +
                "   Como usuário comum já funcionam Proxy do sistema (HKCU), DoH HTTP/2, divisão SNI e AdBlock.\n\n" +
                "2. Apenas 2 opções globais exigem Admin por regra do Windows:\n" +
                "   • DoH global do Windows 11: altera DNS do adaptador e registro HKLM.\n" +
                "   • Bloquear QUIC UDP 443: cria/remove regra no Windows Defender Firewall.\n\n" +
                "3. Seguro e reversível:\n" +
                "   O NologGuard salva seu DNS anterior e restaura tudo automaticamente ao sair.",
                "Proxy, DNS e Firewall do Windows restaurados com sucesso."
            );

            // 14. French
            cat["fr"] = Pack(
                "DoH + Fragmentation SNI + AdBlock • IP Réel",
                "Bouclier", "AdBlock", "Exclusions", "Paramètres", "Journal",
                "BOUCLIER DÉSACTIVÉ", "PROTÉGÉ", "ACTIVER LE BOUCLIER", "DÉSACTIVER",
                "1 clic pour activer Proxy DoH local + fragmentation SNI + contournement QUIC.",
                "Fournisseur DoH :", "Mode de découpe SNI :", "Tester DoH",
                "Configurer le Proxy Windows automatiquement (Recommandé)",
                "Activer le bloqueur de publicités et traceurs (AdBlock)",
                "Appliquer le DoH système Windows 11 (Admin requis)",
                "Bloquer QUIC UDP 443 via Pare-feu (Admin requis)",
                "Relancer en Admin", "ADMIN", "UTILIS.",
                "Découpe intelligente au milieu du SNI (Recommandé)", "Petits paquets fixes", "Désactivé (DoH seul)",
                "SNI découpés", "Bloqués", "DoH (Cache)", "Contournés",
                "Portée honnête : garde votre IP réelle, chiffre le DNS, découpe le SNI et bloque les domaines pub/traqueurs tiers (ne bloque pas les pubs vidéo intégrées comme YouTube).",
                "Liste de filtres :", "Mettre à jour", "Actif : {0} domaines bloqués | {1} règles @@",
                "Règles personnalisées (une par ligne ; préfixe @@ pour autoriser ; formats hosts / AdBlock) :",
                "Règles AdBlock enregistrées et appliquées.",
                "Bloque au niveau du proxy avant la requête DNS sans déchiffrement HTTPS.",
                "Domaines et réseaux exclus de la découpe SNI (*.domaine.com et commentaires # pris en charge) :",
                "Ajouter", "Enregistrer", "Défaut", "Liste d'exclusions enregistrée et appliquée.",
                "Port du Proxy local (1024-65535) :", "Taille initiale (octets) :", "Délai entre fragments (ms) :",
                "URL DoH personnalisée (https://...) :", "Langue de l'interface :",
                "Réduire dans la zone de notification à la fermeture", "Lancer au démarrage de Windows",
                "Afficher les domaines dans le Journal (RAM uniquement, jamais sur disque)",
                "Réinitialisation d'urgence Proxy & Pare-feu Windows", "Paramètres enregistrés.",
                "Le journal reste strictement en mémoire (300 lignes max) et n'est jamais écrit sur disque.", "Effacer",
                "Ouvrir NologGuard", "Basculer le Bouclier", "Quitter (Désactive)", "NologGuard s'exécute toujours dans la barre système.",
                "Droits Administrateur requis",
                "Le DoH système et le blocage QUIC par pare-feu exigent les droits Administrateur. Redémarrer en tant qu'Administrateur ?",
                "? Pourquoi les droits Admin ?", "Pourquoi certaines options demandent-elles l'Admin ?",
                "1. La protection quotidienne des navigateurs ne nécessite AUCUN droit Admin :\n" +
                "   En mode utilisateur standard, le Proxy système (HKCU), DoH HTTP/2, découpe SNI et AdBlock fonctionnent déjà.\n\n" +
                "2. Seules 2 options globales sont restreintes par Windows :\n" +
                "   • DoH système Windows 11 : modifie le DNS réseau et la ruche HKLM.\n" +
                "   • Blocage QUIC UDP 443 : crée/supprime une règle Pare-feu Windows Defender.\n\n" +
                "3. Sûr et réversible :\n" +
                "   Vos DNS d'origine sont sauvegardés et restaurés automatiquement à l'arrêt.",
                "Proxy, DNS et Pare-feu Windows restaurés proprement."
            );

            // 15. German
            cat["de"] = Pack(
                "DoH + SNI-Fragmentierung + AdBlock • Echte IP",
                "Schutz", "AdBlock", "Ausnahmen", "Optionen", "Protokoll",
                "SCHUTZ DEAKTIVIERT", "GESCHÜTZT", "SCHUTZ AKTIVIEREN", "DEAKTIVIEREN",
                "1-Klick für lokalen DoH-Proxy + SNI-Splitting + QUIC-Umgehung.",
                "DoH-Anbieter:", "SNI-Split-Modus:", "DoH testen",
                "Windows-Systemproxy automatisch setzen (Empfohlen)",
                "Werbe- & Tracker-Blocker aktivieren (Domain-Schutz)",
                "Systemweites Windows 11 DoH anwenden (Admin nötig)",
                "QUIC UDP 443 per Firewall blockieren (Admin nötig)",
                "Als Admin starten", "ADMIN", "USER",
                "Intelligenter SNI-Split in der Mitte (Empfohlen)", "Feste kleine Chunks", "Aus (Nur DoH-Proxy)",
                "SNI-Splits", "Blockiert", "DoH (Cache)", "Umgeleitet",
                "Transparenter Umfang: Behält echte IP, verschlüsselt DNS, teilt SNI und blockiert Drittanbieter-Werbung/Tracker (keine YouTube-Videoanzeigen).",
                "Filterliste:", "Aktualisieren", "Aktiv: {0} blockierte Domains | {1} @@ Ausnahmen",
                "Eigene Regeln (eine pro Zeile; @@ zum Erlauben voranstellen; hosts / AdBlock-Syntax):",
                "AdBlock-Regeln gespeichert und aktiv.",
                "Blockiert Domains vor dem DNS-Lookup im Proxy ohne HTTPS-Entschlüsselung.",
                "Von SNI-Splitting ausgenommene Domains & Netze (*.domain.de und # Kommentare möglich):",
                "Hinzufügen", "Speichern", "Standard", "Ausnahmeliste gespeichert und angewendet.",
                "Lokaler Proxy-Port (1024-65535):", "Anfangs-Chunk-Größe (Bytes):", "Verzögerung zwischen Chunks (ms):",
                "Eigene DoH-URL (https://...):", "Oberflächensprache:",
                "Beim Schließen in den Infobereich minimieren", "Automatisch mit Windows starten",
                "Domains im Protokoll anzeigen (nur RAM, niemals auf Festplatte)",
                "Notfall-Reset für Windows-Proxy & Firewall", "Einstellungen gespeichert.",
                "Protokolle bleiben nur im Arbeitsspeicher (max. 300 Zeilen) und werden nie gespeichert.", "Leeren",
                "NologGuard öffnen", "Schutz umschalten", "Beenden (Deaktiviert Schutz)", "NologGuard läuft im Infobereich weiter.",
                "Administratorrechte erforderlich",
                "Systemweites DoH und Firewall-QUIC-Blockierung erfordern Administratorrechte. Jetzt als Administrator neu starten?",
                "? Warum Admin-Rechte?", "Warum benötigen manche Optionen Admin-Rechte?",
                "1. Der tägliche Browser-Schutz braucht KEINE Admin-Rechte:\n" +
                "   Als normaler Benutzer funktionieren Systemproxy (HKCU), HTTP/2-DoH, SNI-Split und AdBlock.\n\n" +
                "2. Nur 2 systemweite Optionen werden von Windows eingeschränkt:\n" +
                "   • Windows 11 System-DoH: Ändert Adapter-DNS und HKLM-Systemzweige.\n" +
                "   • QUIC UDP 443 Firewall-Block: Erstellt/entfernt Regeln der Windows-Firewall.\n\n" +
                "3. Sicher & umkehrbar:\n" +
                "   Vorherige DNS-Daten werden gesichert und beim Beenden automatisch wiederhergestellt.",
                "Windows-Proxy, DNS und Firewall sauber wiederhergestellt."
            );

            // 16. Italian
            cat["it"] = Pack(
                "DoH + Frammentazione SNI + AdBlock • IP Reale",
                "Scudo", "AdBlock", "Esclusioni", "Opzioni", "Registro",
                "SCUDO DISATTIVO", "PROTETTO", "ATTIVA SCUDO", "DISATTIVA SCUDO",
                "1 clic per attivare Proxy DoH locale + divisione SNI + bypass QUIC.",
                "Provider DoH:", "Modalità divisione SNI:", "Test DoH",
                "Imposta Proxy di sistema Windows (Consigliato)",
                "Attiva blocco pubblicità e tracker (AdBlock)",
                "Applica DoH globale Windows 11 (Richiede Admin)",
                "Blocca QUIC UDP 443 via Firewall (Richiede Admin)",
                "Riavvia da Admin", "ADMIN", "UTENTE",
                "Divisione intelligente a metà SNI (Consigliato)", "Blocchi piccoli fissi", "Off (Solo Proxy DoH)",
                "SNI divisi", "Bloccati", "DoH (Cache)", "Esclusi",
                "Ambito trasparente: mantiene l'IP reale, cifra il DNS, divide l'SNI e blocca domini pubblicitari/tracker di terze parti (non blocca annunci video su YouTube).",
                "Lista filtri:", "Aggiorna", "Attivi: {0} domini bloccati | {1} regole @@",
                "Regole personalizzate (una per riga; @@ per consentire; supporta hosts / AdBlock):",
                "Regole AdBlock salvate e applicate.",
                "Blocca al livello proxy prima della query DNS senza decrittazione HTTPS.",
                "Domini e reti esclusi dalla divisione SNI (supporta *.dominio.it e commenti #):",
                "Aggiungi", "Salva", "Ripristina", "Esclusioni salvate e applicate.",
                "Porta Proxy Locale (1024-65535):", "Dimensione blocco iniziale (byte):", "Ritardo tra blocchi (ms):",
                "URL DoH personalizzato (https://...):", "Lingua interfaccia:",
                "Riduci nella barra di sistema alla chiusura", "Avvia automaticamente con Windows",
                "Mostra domini nel Registro (solo RAM, mai su disco)",
                "Ripristino d'emergenza Proxy e Firewall Windows", "Impostazioni salvate.",
                "I log restano solo in memoria (ultime 300 righe) e non vengono mai salvati su disco.", "Cancella",
                "Apri NologGuard", "Attiva/Disattiva Scudo", "Esci (Disattiva Scudo)", "NologGuard è ancora attivo nella barra di sistema.",
                "Richiesti diritti Amministratore",
                "Il DoH globale e il blocco QUIC via Firewall richiedono privilegi di Amministratore. Riavviare NologGuard come Amministratore?",
                "? Perché serve Admin?", "Perché alcune opzioni richiedono Amministratore?",
                "1. L'uso quotidiano sui browser NON richiede Admin:\n" +
                "   Come utente normale funzionano Proxy di sistema (HKCU), DoH HTTP/2, divisione SNI e AdBlock.\n\n" +
                "2. Solo 2 funzioni globali sono limitate da Windows:\n" +
                "   • DoH sistema Windows 11: modifica DNS di rete e registro HKLM.\n" +
                "   • Blocco QUIC UDP 443: gestisce regole di Windows Defender Firewall.\n\n" +
                "3. Sicuro e reversibile:\n" +
                "   I DNS precedenti vengono salvati e ripristinati automaticamente all'uscita.",
                "Proxy, DNS e Firewall Windows ripristinati correttamente."
            );

            // 17. Polish
            cat["pl"] = Pack(
                "DoH + Podział SNI + AdBlock • Prawdziwe IP",
                "Tarcza", "AdBlock", "Wyjątki", "Ustawienia", "Dziennik",
                "OCHRONA WYŁĄCZONA", "CHRONIONY", "WŁĄCZ OCHRONĘ", "WYŁĄCZ OCHRONĘ",
                "1 kliknięcie włącza lokalne Proxy DoH + podział SNI + ominięcie QUIC.",
                "Dostawca DoH:", "Tryb podziału SNI:", "Testuj DoH",
                "Automatycznie ustaw Proxy systemu Windows (Zalecane)",
                "Włącz blokowanie reklam i trackerów (AdBlock)",
                "Zastosuj systemowy DoH Windows 11 (Wymaga Admina)",
                "Blokuj QUIC UDP 443 w Zaporze (Wymaga Admina)",
                "Uruchom jako Admin", "ADMIN", "USER",
                "Inteligentny podział w środku SNI (Zalecane)", "Stałe małe fragmenty", "Wył. (Tylko DoH)",
                "Podzielone SNI", "Zablokowane", "DoH (Pamięć)", "Pominięte",
                "Uczciwy zakres: zachowuje prawdziwe IP, szyfruje DNS, dzieli SNI i blokuje domeny reklam/trackerów (nie usuwa reklam wideo wewnątrz YouTube).",
                "Lista filtrów:", "Aktualizuj", "Aktywne: {0} zablokowanych domen | {1} reguł @@",
                "Własne reguły (jedna na linię; prefiks @@ aby zezwolić; obsługa hosts / AdBlock):",
                "Reguły AdBlock zapisane i zastosowane.",
                "Blokuje domeny na warstwie proxy przed zapytaniem DNS bez deszyfrowania HTTPS.",
                "Domeny i sieci wykluczone z podziału SNI (obsługa *.domena.pl i komentarzy #):",
                "Dodaj", "Zapisz", "Domyślne", "Lista wyjątków zapisana i zastosowana.",
                "Port lokalnego Proxy (1024-65535):", "Rozmiar pierwszego fragmentu (bajty):", "Opóźnienie między fragmentami (ms):",
                "Własny URL DoH (https://...):", "Język interfejsu:",
                "Minimalizuj do zasobnika przy zamknięciu", "Uruchamiaj automatycznie z Windows",
                "Pokazuj domeny w Dzienniku (tylko w RAM, bez zapisu na dysk)",
                "Awaryjny reset Proxy i Zapory Windows", "Ustawienia zapisane.",
                "Dziennik jest przechowywany wyłącznie w pamięci RAM (300 wpisów) i nigdy na dysku.", "Wyczyść",
                "Otwórz NologGuard", "Przełącz Tarczę", "Zakończ (Wyłącza ochronę)", "NologGuard nadal działa w zasobniku systemowym.",
                "Wymagane uprawnienia Administratora",
                "Systemowy DoH oraz blokada QUIC w Zaporze wymagają uprawnień Administratora. Uruchomić ponownie jako Administrator?",
                "? Dlaczego wymagany Admin?", "Dlaczego niektóre opcje wymagają Administratora?",
                "1. Codzienna ochrona przeglądarek NIE wymaga Admina:\n" +
                "   Jako zwykły użytkownik działają Proxy systemowe (HKCU), DoH HTTP/2, podział SNI i AdBlock.\n\n" +
                "2. Tylko 2 opcje ogólnosystemowe wymagają praw Admina wg zasad Windows:\n" +
                "   • Systemowy DoH Windows 11: zmiana DNS karty sieciowej i klucza HKLM.\n" +
                "   • Blokada QUIC UDP 443: dodawanie i usuwanie reguł Zapory Windows.\n\n" +
                "3. Bezpieczne przywracanie:\n" +
                "   Poprzedni DNS jest zapisywany i automatycznie przywracany przy wyłączeniu.",
                "Proxy, DNS i Zapora Windows zostały przywrócone."
            );

            // 18. Hindi
            cat["hi"] = Pack(
                "DoH + SNI विभाजन + AdBlock • असली IP सुरक्षित",
                "सुरक्षा", "AdBlock", "अपवाद", "सेटिंग्स", "लॉग",
                "सुरक्षा बंद है", "सुरक्षित", "सुरक्षा चालू करें", "सुरक्षा बंद करें",
                "1-क्लिक में लोकल DoH प्रॉक्सी + SNI विभाजन + QUIC बायपास चालू करें।",
                "DoH प्रदाता:", "SNI विभाजन मोड:", "DoH जांचें",
                "Windows सिस्टम प्रॉक्सी स्वतः सेट करें (अनुशंसित)",
                "विज्ञापन और ट्रैकर अवरोधक चालू करें (AdBlock)",
                "Windows 11 सिस्टम DoH लागू करें (एडमिन आवश्यक)",
                "फ़ायरवॉल द्वारा QUIC UDP 443 ब्लॉक करें (एडमिन आवश्यक)",
                "एडमिन से खोलें", "एडमिन", "यूज़र",
                "स्मार्ट SNI मध्य विभाजन (अनुशंसित)", "निश्चित छोटे टुकड़े", "बंद (केवल DoH प्रॉक्सी)",
                "SNI विभाजन", "ब्लॉक किए गए", "DoH (कैश)", "छोड़े गए",
                "स्पष्ट दायरा: आपका असली IP रखता है, DNS एन्क्रिप्ट करता है, SNI विभाजित करता है और तृतीय-पक्ष विज्ञापन/ट्रैकर डोमेन ब्लॉक करता है (YouTube वीडियो विज्ञापनों को नहीं हटाता)।",
                "फ़िल्टर सूची:", "सूची अपडेट करें", "सक्रिय: {0} ब्लॉक डोमेन | {1} @@ अनुमति नियम",
                "कस्टम नियम (प्रति पंक्ति एक; अनुमति देने के लिए @@ उपसर्ग; hosts / AdBlock समर्थित):",
                "AdBlock नियम सहेजे और लागू किए गए।",
                "HTTPS डिक्रिप्शन के बिना DNS लुकअप से पहले प्रॉक्सी स्तर पर ब्लॉक करता है।",
                "SNI विभाजन से बाहर रखे गए डोमेन व नेटवर्क (*.example.com और # टिप्पणी समर्थित):",
                "जोड़ें", "सहेजें", "डिफ़ॉल्ट", "अपवाद सूची सहेजी और लागू की गई।",
                "लोकल प्रॉक्सी पोर्ट (1024-65535):", "प्रारंभिक टुकड़े का आकार (बाइट्स):", "टुकड़ों के बीच विलंब (ms):",
                "कस्टम DoH URL (https://...):", "इंटरफ़ेस भाषा:",
                "बंद करने पर सिस्टम ट्रे में छोटा करें", "Windows स्टार्टअप पर स्वतः चलाएं",
                "लॉग टैब में डोमेन दिखाएं (केवल RAM में, डिस्क पर कभी नहीं)",
                "Windows प्रॉक्सी और फ़ायरवॉल आपातकालीन रीसेट", "सेटिंग्स सहेजी गईं।",
                "लॉग केवल RAM में रहते हैं (अंतिम 300 पंक्तियाँ) और डिस्क पर कभी सहेजे नहीं जाते।", "लॉग साफ़ करें",
                "NologGuard खोलें", "सुरक्षा बदलें", "बाहर निकलें (सुरक्षा बंद)", "NologGuard अभी भी सिस्टम ट्रे में चल रहा है।",
                "एडमिनिस्ट्रेटर अनुमति आवश्यक",
                "सिस्टम-व्यापी DoH और फ़ायरवॉल QUIC ब्लॉक के लिए एडमिनिस्ट्रेटर अधिकार आवश्यक हैं। क्या एडमिन के रूप में पुनः प्रारंभ करें?",
                "? एडमिन अधिकार क्यों?", "कुछ विकल्पों को एडमिनिस्ट्रेटर अधिकार क्यों चाहिए?",
                "1. सामान्य ब्राउज़र सुरक्षा के लिए एडमिन अधिकार नहीं चाहिए:\n" +
                "   सामान्य यूज़र मोड में सिस्टम प्रॉक्सी (HKCU), HTTP/2 DoH, SNI विभाजन और AdBlock चलते हैं।\n\n" +
                "2. केवल 2 सिस्टम-व्यापी सुविधाओं के लिए Windows एडमिन मांगता है:\n" +
                "   • Windows 11 सिस्टम DoH: नेटवर्क अडैप्टर DNS और HKLM रजिस्ट्री बदलना।\n" +
                "   • फ़ायरवॉल QUIC UDP 443 ब्लॉक: Windows Defender फ़ायरवॉल नियम जोड़ना/हटाना।\n\n" +
                "3. सुरक्षित और स्वचालित बहाली:\n" +
                "   बंद करने या बाहर निकलने पर आपका पुराना DNS स्वतः बहाल हो जाता है।",
                "Windows प्रॉक्सी, DNS और फ़ायरवॉल सफलतापूर्वक बहाल हुए।"
            );

            // 19. Arabic (العربية)
            cat["ar"] = Pack(
                "DoH + تقطيع SNI + حظر الإعلانات • يحتفظ بـ IP الحقيقي",
                "الحماية", "الإعلانات", "الاستثناءات", "الإعدادات", "السجل",
                "الحماية متوقفة", "محمي", "تفعيل الحماية", "إيقاف الحماية",
                "نقرة واحدة لتفعيل وكيل DoH المحلي + تقطيع SNI + تجاوز QUIC.",
                "مزوّد DoH:", "وضع تقطيع SNI:", "فحص ذاتي",
                "تعيين وكيل نظام Windows تلقائياً (مستحسن)",
                "تفعيل مانع الإعلانات والمتتبعات (حماية النطاقات)",
                "تطبيق DoH على مستوى Windows 11 (يتطلب مسؤول)",
                "حظر QUIC UDP 443 عبر الجدار الناري (يتطلب مسؤول)",
                "إعادة التشغيل كمسؤول", "مسؤول", "مستخدم",
                "تقطيع ذكي في منتصف SNI (مستحسن)", "أجزاء صغيرة ثابتة", "إيقاف (وكيل DoH فقط)",
                "تقطيع SNI", "إعلانات محظورة", "DoH (مخبأ)", "مستثنى",
                "توضيح: يحتفظ بعنوان IP الحقيقي، يشفر DNS، يقطع SNI، ويحظر نطاقات الإعلانات والتتبع الخارجية (لا يزيل إعلانات الفيديو داخل نفس النطاق مثل يوتيوب).",
                "قائمة الفلترة:", "تحديث القائمة", "نشط: {0} نطاق محظور | {1} استثناء @@",
                "قواعد مخصصة (نطاق في كل سطر؛ أضف @@ للسماح؛ يدعم صيغة hosts و AdBlock):",
                "تم حفظ وتطبيق قواعد الحظر فوراً.",
                "يحظر نطاقات الإعلانات والتتبع في طبقة الوكيل قبل DNS دون فك تشفير HTTPS.",
                "النطاقات والشبكات المستثناة من تقطيع SNI (نطاق بالسطر، يدعم *.example.com و #):",
                "إضافة", "حفظ التغييرات", "افتراضي", "تم حفظ وتطبيق قائمة الاستثناءات.",
                "منفذ الوكيل المحلي (1024-65535):", "حجم الجزء الأولي (بايت):", "التأخير بين الأجزاء (مللي ثانية):",
                "رابط DoH مخصص (https://...):", "لغة الواجهة:",
                "تصغير إلى شريط المهام عند الإغلاق", "تشغيل تلقائي مع بدء Windows",
                "عرض النطاقات في السجل (في الذاكرة فقط، لا يُكتب على القرص)",
                "إعادة ضبط طارئة لوكيل Windows والجدار الناري", "تم حفظ الإعدادات.",
                "السجلات تبقى في الذاكرة المؤقتة فقط (آخر 300 سطر) ولا تُحفظ على القرص.", "مسح السجل",
                "فتح NologGuard", "تبديل الحماية", "خروج (إيقاف الحماية)", "NologGuard لا يزال يعمل في شريط النظام.",
                "صلاحيات المسؤول مطلوبة",
                "خيارا DoH للنظام وحظر QUIC بالجدار الناري يتطلبان صلاحيات المسؤول. هل تريد إعادة التشغيل كمسؤول الآن؟",
                "? لماذا تتطلب بعض الخيارات مسؤولاً؟", "لماذا تتطلب بعض الخيارات صلاحيات المسؤول؟",
                "1. التصفح اليومي لا يحتاج صلاحيات مسؤول:\n" +
                "   في وضع المستخدم العادي، يعمل وكيل النظام وDoH وتقطيع SNI وحظر الإعلانات لمتصفحات Chrome وEdge وFirefox تلقائياً.\n\n" +
                "2. خياران اختياريان فقط يطلبان المسؤول (بسبب حماية Windows):\n" +
                "   • DoH على مستوى Windows 11: تعديل DNS لبطاقات الشبكة ومفاتيح HKLM يتطلب صلاحيات المسؤول.\n" +
                "   • حظر QUIC UDP 443: إضافة وحذف قواعد جدار الحماية يتطلب صلاحيات المسؤول.\n\n" +
                "3. آمن وقابل للاسترجاع:\n" +
                "   يحفظ NologGuard نسخة احتياطية لإعدادات DNS ويعيدها تلقائياً عند الإيقاف أو الخروج.",
                "تمت استعادة وكيل Windows وDNS والجدار الناري بنجاح."
            );

            // 20. Persian / Farsi (فارسی)
            cat["fa"] = Pack(
                "DoH + قطعه‌بندی SNI + مسدودساز تبلیغات • حفظ IP واقعی",
                "محافظت", "تبلیغات", "استثناها", "تنظیمات", "گزارش",
                "محافظت غیرفعال", "محافظت فعال", "فعال‌سازی سپر", "غیرفعال‌سازی سپر",
                "یک کلیک برای فعال‌سازی پروکسی محلی DoH + قطعه‌بندی SNI + دور زدن QUIC.",
                "ارائه‌دهنده DoH:", "حالت قطعه‌بندی SNI:", "خودآزمایی",
                "تنظیم خودکار پروکسی سیستم Windows (پیشنهادی)",
                "فعال‌سازی مسدودساز تبلیغات و ردیاب‌ها (سپر دامنه)",
                "اعمال DoH سراسری Windows 11 (نیازمند مدیر)",
                "مسدودسازی QUIC UDP 443 با فایروال (نیازمند مدیر)",
                "اجرا با دسترسی مدیر", "مدیر", "کاربر",
                "تقسیم هوشمند میانه SNI (پیشنهادی)", "قطعات کوچک ثابت", "خاموش (فقط پروکسی DoH)",
                "تقسیم SNI", "تبلیغ مسدود", "DoH (کش)", "مستثنا",
                "توضیح شفاف: IP واقعی شما حفظ می‌شود، DNS رمزنگاری و SNI قطعه‌بندی می‌شود و دامنه‌های تبلیغاتی متفرقه مسدود می‌شوند (تبلیغات هم‌دامنه مانند ویدیوهای یوتیوب حذف نمی‌شوند).",
                "فهرست فیلتر:", "به‌روزرسانی", "فعال: {0} دامنه مسدود | {1} استثنای @@",
                "قوانین سفارشی (هر خط یک دامنه؛ برای مجاز کردن @@ بگذارید؛ پشتیبانی از hosts و AdBlock):",
                "قوانین مسدودسازی ذخیره و فوراً اعمال شدند.",
                "دامنه‌های تبلیغاتی را در لایه پروکسی پیش از DNS بدون رمزگشایی HTTPS مسدود می‌کند.",
                "دامنه‌ها و شبکه‌های مستثنا از تقسیم SNI (هر خط یک مورد، پشتیبانی از *.example.com و #):",
                "افزودن", "ذخیره تغییرات", "پیش‌فرض", "فهرست استثناها ذخیره و اعمال شد.",
                "پورت پروکسی محلی (1024-65535):", "اندازه قطعه اولیه (بایت):", "تأخیر بین قطعات (ms):",
                "آدرس DoH سفارشی (https://...):", "زبان رابط کاربری:",
                "کوچک‌سازی به سینی سیستم هنگام بستن", "اجرای خودکار هنگام شروع Windows",
                "نمایش دامنه‌ها در گزارش زنده (فقط در RAM، بدون ذخیره روی دیسک)",
                "بازنشانی اضطراری پروکسی و فایروال Windows", "تنظیمات ذخیره شد.",
                "گزارش‌ها فقط در RAM نگهداری می‌شوند (۳۰۰ خط آخر) و روی دیسک نوشته نمی‌شوند.", "پاک کردن",
                "باز کردن NologGuard", "تغییر وضعیت سپر", "خروج کامل (غیرفعال‌سازی سپر)", "NologGuard همچنان در پس‌زمینه فعال است.",
                "نیازمند دسترسی مدیر (Administrator)",
                "گزینه‌های DoH سراسری و مسدودسازی QUIC فایروال نیازمند دسترسی مدیر هستند. اکنون با دسترسی مدیر راه‌اندازی مجدد شود؟",
                "? چرا برخی گزینه‌ها مدیر می‌خواهند؟", "چرا برخی گزینه‌ها نیاز به دسترسی مدیر دارند؟",
                "1. استفاده روزمره مرورگر بدون نیاز به مدیر:\n" +
                "   در حالت کاربر عادی، پروکسی سیستم (HKCU)، DoH HTTP/2، تقسیم SNI و مسدودساز تبلیغات برای Chrome، Edge و Firefox کامل کار می‌کنند.\n\n" +
                "2. تنها ۲ گزینه سراسری نیازمند مدیر هستند:\n" +
                "   • DoH سراسری Windows 11: تغییر DNS کارت شبکه و تنظیمات HKLM فقط با دسترسی مدیر مجاز است.\n" +
                "   • مسدودسازی QUIC UDP 443: افزودن و حذف قانون فایروال Windows نیازمند مدیر است.\n\n" +
                "3. ایمن و قابل بازگشت:\n" +
                "   NologGuard پیش از تغییر، نسخه پشتیبان DNS را ذخیره کرده و هنگام خروج به‌طور خودکار به حالت اول برمی‌گرداند.",
                "پروکسی، DNS و فایروال Windows با موفقیت بازیابی شدند."
            );

            // 21. Hebrew (עברית)
            cat["he"] = Pack(
                "DoH + פיצול SNI + חסימת פרסומות • שומר על IP אמיתי",
                "הגנה", "פרסומות", "חריגים", "הגדרות", "יומן",
                "ההגנה כבויה", "מוגן", "הפעל הגנה", "כבה הגנה",
                "לחיצה אחת להפעלת פרוקסי DoH מקומי + פיצול SNI + עקיפת QUIC.",
                "ספק DoH:", "מצב פיצול SNI:", "בדיקה עצמית",
                "הגדרת פרוקסי מערכת Windows אוטומטית (מומלץ)",
                "הפעלת חוסם פרסומות ומעקב (הגנת דומיינים)",
                "החל DoH מערכתי ב-Windows 11 (דורש מנהל)",
                "חסום QUIC UDP 443 דרך חומת האש (דורש מנהל)",
                "הפעל כמנהל", "מנהל", "משתמש",
                "פיצול חכם במרכז SNI (מומלץ)", "חלקים קטנים קבועים", "כבוי (פרוקסי DoH בלבד)",
                "פיצול SNI", "פרסומות נחסמו", "DoH (מטמון)", "הוחרג",
                "גילוי נאות: שומר על ה-IP האמיתי, מצפין DNS, מפצל SNI וחוסם דומייני פרסום ומעקב צד-שלישי (אינו מסיר פרסומות וידאו פנימיות כמו יוטיוב).",
                "רשימת סינון:", "עדכן רשימה", "פעיל: {0} דומיינים חסומים | {1} התרי @@",
                "כללים מותאמים (דומיין בשורה; הוסף @@ לאישור; תומך hosts ו-AdBlock):",
                "כללי החסימה נשמרו והוחלו מיד.",
                "חוסם דומייני פרסום ומעקב בשכבת הפרוקסי לפני שאילתת DNS ללא פענוח HTTPS.",
                "דומיינים ורשתות הפטורים מפיצול SNI (שורה לפריט, תומך *.example.com והערות #):",
                "הוסף", "שמור שינויים", "ברירת מחדל", "רשימת החריגים נשמרה והוחלה.",
                "פורט פרוקסי מקומי (1024-65535):", "גודל חלק ראשוני (בתים):", "השהיה בין חלקים (ms):",
                "כתובת DoH מותאמת (https://...):", "שפת ממשק:",
                "מזער למגש המערכת בסגירה", "הפעל אוטומטית עם Windows",
                "הצג דומיינים ביומן (בזיכרון בלבד, לעולם לא נכתב לדיסק)",
                "איפוס חירום לפרוקסי וחומת האש של Windows", "ההגדרות נשמרו.",
                "היומן נשמר בזיכרון בלבד (300 שורות אחרונות) ואינו נשמר בדיסק.", "נקה יומן",
                "פתח את NologGuard", "הפעל / כבה הגנה", "יציאה מלאה (מכבה הגנה)", "NologGuard עדיין פועל במגש המערכת.",
                "נדרשות הרשאות מנהל",
                "DoH מערכתי וחסימת QUIC בחומת האש דורשים הרשאות מנהל. להפעיל מחדש כמנהל כעת?",
                "? למה חלק מהאפשרויות דורשות מנהל?", "מדוע חלק מהאפשרויות דורשות הרשאות מנהל?",
                "1. הגנה יומיומית לדפדפנים ללא הרשאות מנהל:\n" +
                "   במצב משתמש רגיל פועלים פרוקסי המערכת (HKCU), DoH HTTP/2, פיצול SNI וחוסם הפרסומות עבור Chrome, Edge ו-Firefox.\n\n" +
                "2. רק 2 אפשרויות מערכתיות דורשות הרשאת מנהל:\n" +
                "   • DoH מערכתי ל-Windows 11: שינוי DNS של כרטיסי הרשת ורישום HKLM.\n" +
                "   • חסימת QUIC UDP 443: הוספה והסרה של חוקי חומת האש של Windows.\n\n" +
                "3. בטוח והפיך:\n" +
                "   NologGuard מגבה את הגדרות ה-DNS ומשחזר אותן אוטומטית בכיבוי או ביציאה.",
                "פרוקסי Windows, DNS וחומת האש שוחזרו בהצלחה."
            );

            // 22. Bengali (বাংলা)
            cat["bn"] = Pack(
                "DoH + SNI বিভাজন + বিজ্ঞাপন ব্লক • আসল IP বজায় রাখে",
                "সুরক্ষা", "অ্যাডব্লক", "হোয়াইটলিস্ট", "সেটিংস", "লাইভ লগ",
                "সুরক্ষা বন্ধ", "সুরক্ষিত", "সুরক্ষা চালু করুন", "সুরক্ষা বন্ধ করুন",
                "১ ক্লিকে লোকাল DoH প্রক্সি + SNI স্প্লিটিং + QUIC বাইপাস চালু করুন।",
                "DoH প্রোভাইডার:", "SNI স্প্লিট মোড:", "স্ব-পরীক্ষা",
                "Windows সিস্টেম প্রক্সি স্বয়ংক্রিয় সেট করুন (প্রস্তাবিত)",
                "অ্যাডব্লকার ও অ্যান্টি-ট্র্যাকিং চালু করুন (ডোমেইন সুরক্ষা)",
                "Windows 11 সিস্টেম DoH প্রয়োগ করুন (Admin প্রয়োজন)",
                "ফায়ারওয়াল দিয়ে QUIC UDP 443 ব্লক করুন (Admin প্রয়োজন)",
                "Admin হিসেবে পুনরায় চালান", "ADMIN", "USER",
                "স্মার্ট SNI মাঝখান বিভাজন (প্রস্তাবিত)", "নির্দিষ্ট ছোট অংশ", "বন্ধ (শুধু DoH প্রক্সি)",
                "SNI-স্প্লিট", "বিজ্ঞাপন ব্লক", "DoH (ক্যাশ)", "বাইপাসড",
                "সৎ সীমা: আপনার আসল IP থাকে, DNS এনক্রিপ্ট ও SNI বিভক্ত হয় এবং তৃতীয়-পক্ষের বিজ্ঞাপন/ট্র্যাকার ব্লক হয় (YouTube-এর মতো নিজস্ব ভিডিও বিজ্ঞাপন ব্লক করে না)।",
                "ফিল্টার তালিকা:", "তালিকা আপডেট", "সক্রিয়: {0} ব্লক ডোমেইন | {1} @@ অনুমতি",
                "কাস্টম নিয়ম (প্রতি লাইনে একটি ডোমেইন; ছাড় দিতে @@ লিখুন; hosts / AdBlock সমর্থিত):",
                "AdBlock নিয়ম সংরক্ষিত ও সাথে সাথে প্রয়োগ হয়েছে।",
                "HTTPS ডিক্রিপশন ছাড়াই DNS লুকআপের আগে প্রক্সি স্তরে বিজ্ঞাপন ব্লক করে।",
                "SNI বিভাজন থেকে বাদ দেওয়া ডোমেইন ও নেটওয়ার্ক (*.example.com ও # মন্তব্য সমর্থিত):",
                "যোগ করুন", "সংরক্ষণ করুন", "ডিফল্ট", "হোয়াইটলিস্ট সংরক্ষিত ও প্রয়োগ হয়েছে।",
                "লোকাল প্রক্সি পোর্ট (1024-65535):", "প্রাথমিক অংশের আকার (বাইট):", "অংশের মাঝে বিলম্ব (ms):",
                "কাস্টম DoH URL (https://...):", "ইন্টারফেসের ভাষা:",
                "বন্ধ করলে সিস্টেম ট্রেতে ছোট করুন", "Windows স্টার্টআপে স্বয়ংক্রিয় চালু করুন",
                "লাইভ লগে ডোমেইন দেখান (শুধু RAM-এ, ডিস্কে কখনো লেখা হয় না)",
                "Windows প্রক্সি ও ফায়ারওয়াল জরুরি রিসেট", "সেটিংস সংরক্ষিত হয়েছে।",
                "লগ কেবল RAM-এ থাকে (শেষ ৩০০ লাইন) এবং ডিস্কে কখনো সংরক্ষিত হয় না।", "লগ মুছুন",
                "NologGuard খুলুন", "সুরক্ষা বদলান", "সম্পূর্ণ প্রস্থান (সুরক্ষা বন্ধ)", "NologGuard এখনো সিস্টেম ট্রেতে চলছে।",
                "অ্যাডমিনিস্ট্রেটর অনুমতি প্রয়োজন",
                "সিস্টেম-ব্যাপী DoH এবং ফায়ারওয়াল QUIC ব্লকের জন্য অ্যাডমিনিস্ট্রেটর অধিকার প্রয়োজন। এখন Admin হিসেবে পুনরায় চালু করবেন?",
                "? কিছু অপশনে Admin কেন লাগে?", "কেন কিছু অপশনে অ্যাডমিনিস্ট্রেটর অধিকার লাগে?",
                "1. সাধারণ ব্রাউজার সুরক্ষার জন্য Admin লাগে না:\n" +
                "   সাধারণ USER মোডেই সিস্টেম প্রক্সি (HKCU), HTTP/2 DoH, SNI স্প্লিটিং এবং AdBlock চলে।\n\n" +
                "2. শুধু ২টি সিস্টেম-ব্যাপী অপশনে Windows Admin চায়:\n" +
                "   • Windows 11 সিস্টেম DoH: নেটওয়ার্ক অ্যাডাপ্টার DNS ও HKLM রেজিস্ট্রি পরিবর্তন।\n" +
                "   • ফায়ারওয়াল QUIC UDP 443 ব্লক: ফায়ারওয়াল নিয়ম যোগ/মুছে ফেলা।\n\n" +
                "3. নিরাপদ ও পূর্বাবস্থায় ফেরানো যায়:\n" +
                "   বন্ধ বা প্রস্থান করলে পুরোনো DNS ও ফায়ারওয়াল স্বয়ংক্রিয়ভাবে পুনরুদ্ধার হয়।",
                "Windows প্রক্সি, DNS ও ফায়ারওয়াল সফলভাবে পুনরুদ্ধার হয়েছে।"
            );

            // 23. Malay (Bahasa Melayu)
            cat["ms"] = Pack(
                "DoH + Pemecahan SNI + Penyekat Iklan • Kekalkan IP Sebenar",
                "Perisai", "Sekat Iklan", "Senarai Putih", "Tetapan", "Log Langsung",
                "PERISAI DIMATIKAN", "DILINDUNGI", "AKTIFKAN PERISAI", "MATIKAN PERISAI",
                "1-klik untuk aktifkan Proksi DoH tempatan + pemecahan SNI + pintasan QUIC.",
                "Penyedia DoH:", "Mod Pemecahan SNI:", "Uji Sendiri",
                "Tetapkan Proksi Sistem Windows automatik (Disyorkan)",
                "Aktifkan Penyekat Iklan & Anti-Penjejak (Perisai Domain)",
                "Guna DoH Sistem Windows 11 (Perlu Admin)",
                "Sekat QUIC UDP 443 melalui Firewall (Perlu Admin)",
                "Buka semula sebagai Admin", "ADMIN", "USER",
                "Pecahan Tengah SNI Pintar (Disyorkan)", "Pecahan Kecil Tetap", "Mati (Proksi DoH Sahaja)",
                "Pecah-SNI", "Iklan Disekat", "DoH (Cache)", "Dipintas",
                "Skop jujur: Kekalkan IP sebenar, sulitkan DNS, pecahkan SNI dan sekat domain iklan/penjejak pihak ketiga (tidak menyekat iklan video pihak pertama seperti YouTube).",
                "Senarai Penapis:", "Kemas Kini", "Aktif: {0} domain disekat | {1} pengecualian @@",
                "Peraturan tersuai (satu baris satu; tambah @@ untuk benarkan; sokong sintaks hosts & AdBlock):",
                "Peraturan sekatan disimpan dan digunakan serta-merta.",
                "Menyekat domain iklan/penjejak pada lapisan proksi sebelum carian DNS tanpa penyahsulitan HTTPS.",
                "Domain & rangkaian dikecualikan daripada pemecahan SNI (*.example.com dan ulasan # disokong):",
                "Tambah", "Simpan", "Tetapan Asal", "Senarai putih disimpan dan digunakan serta-merta.",
                "Port Proksi Tempatan (1024-65535):", "Saiz Pecahan Awal (bait):", "Lengah Antara Pecahan (ms):",
                "URL DoH Tersuai (https://...):", "Bahasa Antara Muka:",
                "Kecilkan ke dulang sistem bila ditutup", "Lancar automatik semasa Windows bermula",
                "Papar domain dalam Log Langsung (RAM sahaja, tidak ditulis ke cakera)",
                "Tetap Semula Kecemasan Proksi & Firewall Windows", "Tetapan disimpan.",
                "Log hanya disimpan dalam RAM (300 baris terakhir) dan tidak ditulis ke cakera.", "Kosongkan Log",
                "Buka NologGuard", "Togol Perisai", "Keluar (Matikan Perisai)", "NologGuard masih berjalan di dulang sistem.",
                "Keizinan Pentadbir Diperlukan",
                "DoH sistem Windows dan sekatan QUIC Firewall memerlukan hak Pentadbir. Mulakan semula NologGuard sebagai Pentadbir sekarang?",
                "? Mengapa sesetengah pilihan perlu Admin?", "Mengapa Sesetengah Pilihan Memerlukan Pentadbir?",
                "1. Perlindungan pelayar harian TIDAK memerlukan hak Admin:\n" +
                "   Dalam mod USER biasa, Proksi Sistem (HKCU), HTTP/2 DoH, pecahan SNI dan AdBlock sudah berjalan untuk Chrome, Edge dan Firefox.\n\n" +
                "2. Hanya 2 ciri seluruh sistem memerlukan Admin (dikawal Windows):\n" +
                "   • DoH Sistem Windows 11: Mengubah DNS penyesuai rangkaian fizikal dan kekunci HKLM.\n" +
                "   • Sekat QUIC UDP 443 Firewall: Menambah/memadam peraturan Windows Defender Firewall.\n\n" +
                "3. Selamat & Boleh Dipulihkan:\n" +
                "   NologGuard menyandar tetapan DNS asal dahulu dan memulihkannya secara automatik apabila dimatikan atau keluar.",
                "Proksi Windows, DNS dan Firewall berjaya dipulihkan."
            );

            // 24. Dutch (Nederlands)
            cat["nl"] = Pack(
                "DoH + SNI-fragmentatie + AdBlock • Behoudt echt IP",
                "Schild", "AdBlock", "Uitzonderingen", "Instellingen", "Live Log",
                "SCHILD UITGESCHAKELD", "BESCHERMD", "SCHILD INSCHAKELEN", "SCHILD UITSCHAKELEN",
                "1-klik om lokale DoH-proxy + SNI-splitsing + QUIC-omzeiling in te schakelen.",
                "DoH-provider:", "SNI-splitsmodus:", "Zelftest",
                "Windows-systeemproxy automatisch instellen (Aanbevolen)",
                "AdBlocker & Anti-Tracking inschakelen (Domeinschild)",
                "Windows 11 Systeem-DoH toepassen (Vereist Admin)",
                "QUIC UDP 443 blokkeren via Firewall (Vereist Admin)",
                "Herstart als Admin", "ADMIN", "GEBR.",
                "Slimme SNI-middensplitsing (Aanbevolen)", "Vaste kleine blokken", "Uit (Alleen DoH-proxy)",
                "SNI-split", "Geblokkeerd", "DoH (Cache)", "Omzeild",
                "Eerlijke reikwijdte: Behoudt uw echte IP, versleutelt DNS, splitst SNI en blokkeert advertentie-/trackerdomeinen van derden (verwijdert geen YouTube-videoadvertenties).",
                "Filterlijst:", "Lijst bijwerken", "Actief: {0} geblokkeerde domeinen | {1} @@ uitzonderingen",
                "Aangepaste regels (één per regel; begin met @@ om toe te staan; ondersteunt hosts & AdBlock):",
                "AdBlock-regels opgeslagen en direct toegepast.",
                "Blokkeert advertentie-/trackerdomeinen op proxyniveau vóór DNS-opzoeking zonder HTTPS-ontsleuteling.",
                "Domeinen & netwerken uitgesloten van SNI-splitsing (ondersteunt *.domein.nl en # opmerkingen):",
                "Toevoegen", "Opslaan", "Standaard", "Uitzonderingenlijst opgeslagen en direct actief.",
                "Lokale proxypoort (1024-65535):", "Eerste blokgrootte (bytes):", "Vertraging tussen blokken (ms):",
                "Aangepaste DoH-URL (https://...):", "Interfacetaal:",
                "Minimaliseren naar systeemvak bij sluiten", "Automatisch starten met Windows",
                "Domeinen tonen in Live Log (alleen in RAM, nooit op schijf)",
                "Noodherstel Windows Proxy & Firewall", "Instellingen opgeslagen.",
                "Logboeken blijven uitsluitend in het werkgeheugen (laatste 300 regels) en worden nooit opgeslagen.", "Log wissen",
                "NologGuard openen", "Schild wisselen", "Afsluiten (Schild uit)", "NologGuard blijft actief in het systeemvak.",
                "Administratorrechten vereist",
                "Systeembrede Windows DoH en Firewall QUIC-blokkering vereisen Administratorrechten. NologGuard nu als Administrator herstarten?",
                "? Waarom is soms Admin nodig?", "Waarom vereisen sommige opties Administratorrechten?",
                "1. Dagelijkse browserbescherming werkt ZONDER Admin:\n" +
                "   In de normale USER-modus werken Systeemproxy (HKCU), HTTP/2 DoH, SNI-splitsing en AdBlock voor Chrome, Edge en Firefox volledig.\n\n" +
                "2. Slechts 2 systeembrede opties vereisen Admin:\n" +
                "   • Windows 11 Systeem-DoH: DNS op netwerkadapters en HKLM-sleutels aanpassen.\n" +
                "   • Firewall QUIC UDP 443: Firewallregels toevoegen of verwijderen.\n\n" +
                "3. Veilig & omkeerbaar:\n" +
                "   NologGuard maakt vooraf een back-up van uw DNS en herstelt alles automatisch bij uitschakelen.",
                "Windows Proxy, DNS en Firewall zijn netjes hersteld."
            );

            // 25. Swedish (Svenska)
            cat["sv"] = Pack(
                "DoH + SNI-fragmentering + AdBlock • Behåller riktig IP",
                "Skydd", "AdBlock", "Undantag", "Inställningar", "Live-logg",
                "SKYDD AVSTÄNGT", "SKYDDAD", "AKTIVERA SKYDD", "STÄNG AV SKYDD",
                "1-klick för lokal DoH-proxy + SNI-delning + QUIC-förbikoppling.",
                "DoH-leverantör:", "SNI-delningsläge:", "Självtest",
                "Ställ in Windows systemproxy automatiskt (Rekommenderas)",
                "Aktivera AdBlocker & Anti-Tracking (Domänskydd)",
                "Använd Windows 11 System-DoH (Kräver Admin)",
                "Blockera QUIC UDP 443 via Brandvägg (Kräver Admin)",
                "Starta om som Admin", "ADMIN", "ANV.",
                "Smart SNI-mittdelning (Rekommenderas)", "Fasta små segment", "Av (Endast DoH-proxy)",
                "SNI-delad", "Blockerade", "DoH (Cache)", "Förbigångna",
                "Ärlig omfattning: Behåller riktig IP, krypterar DNS, delar SNI och blockerar tredjeparts reklam-/spårardomäner (tar inte bort inbäddade videoannonser som YouTube).",
                "Filterlista:", "Uppdatera lista", "Aktiv: {0} blockerade domäner | {1} @@ tillåtna",
                "Egna regler (en per rad; prefix @@ för att tillåta; stöder hosts & AdBlock-syntax):",
                "AdBlock-regler sparade och aktiverade direkt.",
                "Blockerar annons-/spårardomäner i proxylagret före DNS utan HTTPS-dekryptering.",
                "Domäner och nätverk undantagna från SNI-delning (stöder *.example.com och # kommentarer):",
                "Lägg till", "Spara ändringar", "Standard", "Undantagslista sparad och aktiverad.",
                "Lokal proxyport (1024-65535):", "Första segmentstorlek (byte):", "Fördröjning mellan segment (ms):",
                "Anpassad DoH-URL (https://...):", "Gränssnittsspråk:",
                "Minimera till systemfältet vid stängning", "Starta automatiskt med Windows",
                "Visa domäner i Live-logg (endast i RAM, skrivs aldrig till disk)",
                "Nödåterställ Windows Proxy & Brandvägg", "Inställningar sparade.",
                "Loggar lagras endast i RAM (senaste 300 raderna) och sparas aldrig på disk.", "Rensa logg",
                "Öppna NologGuard", "Växla skydd", "Avsluta (Stänger skydd)", "NologGuard fortsätter köras i systemfältet.",
                "Administratörsbehörighet krävs",
                "Systemomfattande DoH och brandväggsblockering av QUIC kräver administratörsrättigheter. Starta om NologGuard som administratör nu?",
                "? Varför behövs Admin ibland?", "Varför kräver vissa val administratörsrättigheter?",
                "1. Dagligt webbläsarskydd kräver INGEN Admin:\n" +
                "   I normalt användarläge fungerar systemproxy (HKCU), HTTP/2 DoH, SNI-delning och AdBlock för Chrome, Edge och Firefox.\n\n" +
                "2. Endast 2 systemövergripande funktioner kräver Admin:\n" +
                "   • Windows 11 System-DoH: Ändra DNS på nätverkskort och i HKLM.\n" +
                "   • Blockera QUIC UDP 443: Lägga till eller ta bort brandväggsregler.\n\n" +
                "3. Säkert & återställbart:\n" +
                "   NologGuard säkerhetskopierar din DNS först och återställer allt automatiskt vid avstängning.",
                "Windows Proxy, DNS och brandväggsregler återställdes."
            );

            // 26. Norwegian Bokmål (Norsk)
            cat["nb"] = Pack(
                "DoH + SNI-fragmentering + AdBlock • Beholder ekte IP",
                "Skjold", "AdBlock", "Unntak", "Innstillinger", "Live-logg",
                "SKJOLD AVSLÅTT", "BESKYTTET", "AKTIVER SKJOLD", "DEAKTIVER SKJOLD",
                "1-klikk for lokal DoH-proxy + SNI-deling + QUIC-omgåelse.",
                "DoH-tilbyder:", "SNI-delingsmodus:", "Selvtest",
                "Sett Windows systemproxy automatisk (Anbefalt)",
                "Aktiver AdBlocker & Anti-sporing (Domeneskjold)",
                "Bruk Windows 11 System-DoH (Krever Admin)",
                "Blokker QUIC UDP 443 via Brannmur (Krever Admin)",
                "Start på nytt som Admin", "ADMIN", "BRUKER",
                "Smart SNI-midtdeling (Anbefalt)", "Faste små deler", "Av (Kun DoH-proxy)",
                "SNI-delt", "Blokkert", "DoH (Cache)", "Forbigått",
                "Ærlig omfang: Beholder din ekte IP, krypterer DNS, deler SNI og blokkerer tredjeparts annonse-/sporingsdomener (fjerner ikke førsteparts videoannonser som YouTube).",
                "Filterliste:", "Oppdater liste", "Aktiv: {0} blokkerte domener | {1} @@ unntak",
                "Egendefinerte regler (én per linje; bruk @@ for å tillate; støtter hosts & AdBlock):",
                "AdBlock-regler lagret og aktivert umiddelbart.",
                "Blokkerer annonse-/sporingsdomener i proxylaget før DNS uten HTTPS-dekryptering.",
                "Domener og nettverk unntatt fra SNI-deling (støtter *.example.com og # kommentarer):",
                "Legg til", "Lagre endringer", "Standard", "Unntaksliste lagret og aktivert.",
                "Lokal proxyport (1024-65535):", "Første delstørrelse (byte):", "Forsinkelse mellom deler (ms):",
                "Egendefinert DoH-URL (https://...):", "Grensesnittspråk:",
                "Minimer til systemstatusfeltet ved lukking", "Start automatisk med Windows",
                "Vis domener i Live-logg (kun i RAM, skrives aldri til disk)",
                "Nødtilbakestill Windows Proxy & Brannmur", "Innstillinger lagret.",
                "Logger lagres kun i minnet (siste 300 oppføringer) og aldri på disk.", "Tøm logg",
                "Åpne NologGuard", "Veksle skjold", "Avslutt (Slår av skjold)", "NologGuard kjører fortsatt i systemstatusfeltet.",
                "Administratorrettigheter kreves",
                "Systemdekkende DoH og brannmurblokkering av QUIC krever administratorrettigheter. Start NologGuard på nytt som administrator nå?",
                "? Hvorfor trenger noen valg Admin?", "Hvorfor krever noen valg administratorrettigheter?",
                "1. Daglig nettleserbeskyttelse trenger IKKE Admin:\n" +
                "   I vanlig brukermodus virker systemproxy (HKCU), HTTP/2 DoH, SNI-deling og AdBlock for Chrome, Edge og Firefox.\n\n" +
                "2. Bare 2 systemomfattende funksjoner krever Admin:\n" +
                "   • Windows 11 System-DoH: Endring av DNS på nettverkskort og i HKLM.\n" +
                "   • Blokker QUIC UDP 443: Legge til og fjerne brannmurregler.\n\n" +
                "3. Trygt & reversibelt:\n" +
                "   NologGuard sikkerhetskopierer gammel DNS først og gjenoppretter automatisk når skjoldet deaktiveres.",
                "Windows Proxy, DNS og brannmur ble gjenopprettet."
            );

            // 27. Danish (Dansk)
            cat["da"] = Pack(
                "DoH + SNI-opdeling + AdBlock • Beholder ægte IP",
                "Skjold", "AdBlock", "Undtagelser", "Indstillinger", "Live Log",
                "SKJOLD DEAKTIVERET", "BESKYTTET", "AKTIVER SKJOLD", "DEAKTIVER SKJOLD",
                "1-klik for at aktivere lokal DoH-proxy + SNI-opdeling + QUIC-omgåelse.",
                "DoH-udbyder:", "SNI-opdelingstilstand:", "Selvtest",
                "Indstil Windows systemproxy automatisk (Anbefalet)",
                "Aktiver AdBlocker & Anti-Tracking (Domæneskjold)",
                "Anvend Windows 11 System-DoH (Kræver Admin)",
                "Bloker QUIC UDP 443 via Firewall (Kræver Admin)",
                "Genstart som Admin", "ADMIN", "BRUGER",
                "Smart SNI-midtopdeling (Anbefalet)", "Faste små bidder", "Fra (Kun DoH-proxy)",
                "SNI-delt", "Blokeret", "DoH (Cache)", "Omgået",
                "Ærlig rækkevidde: Beholder din ægte IP, krypterer DNS, opdeler SNI og blokerer tredjeparts annonce-/sporingsdomæner (fjerner ikke YouTube-videoannoncer).",
                "Filterliste:", "Opdater liste", "Aktiv: {0} blokerede domæner | {1} @@ undtagelser",
                "Brugerdefinerede regler (én pr. linje; brug @@ for at tillade; understøtter hosts & AdBlock):",
                "AdBlock-regler gemt og aktiveret med det samme.",
                "Blokerer annonce-/sporingsdomæner i proxylaget før DNS uden HTTPS-dekryptering.",
                "Domæner & netværk undtaget fra SNI-opdeling (understøtter *.example.com og # kommentarer):",
                "Tilføj", "Gem ændringer", "Standard", "Undtagelsesliste gemt og aktiveret.",
                "Lokal proxyport (1024-65535):", "Første blokstørrelse (bytes):", "Forsinkelse mellem bidder (ms):",
                "Brugerdefineret DoH-URL (https://...):", "Grænsefladesprog:",
                "Minimer til proceslinjebakken ved lukning", "Start automatisk med Windows",
                "Vis domæner i Live Log (kun i RAM, gemmes aldrig på disk)",
                "Nødnulstil Windows Proxy & Firewall", "Indstillinger gemt.",
                "Logfiler gemmes kun i RAM (sidste 300 linjer) og aldrig på disk.", "Ryd log",
                "Åbn NologGuard", "Skift skjold", "Afslut (Deaktiverer skjold)", "NologGuard kører stadig i systembakken.",
                "Administratorrettigheder påkrævet",
                "Systemomfattende DoH og Firewall QUIC-blokering kræver administratorrettigheder. Genstart NologGuard som administrator nu?",
                "? Hvorfor kræver nogle valg Admin?", "Hvorfor kræver visse indstillinger administratorrettigheder?",
                "1. Daglig browserbeskyttelse kræver IKKE Admin:\n" +
                "   I almindelig brugertilstand virker systemproxy (HKCU), HTTP/2 DoH, SNI-opdeling og AdBlock for Chrome, Edge og Firefox.\n\n" +
                "2. Kun 2 systemfunktioner kræver Admin:\n" +
                "   • Windows 11 System-DoH: Ændring af DNS på netværkskort og HKLM.\n" +
                "   • Bloker QUIC UDP 443: Tilføje og fjerne Windows Firewall-regler.\n\n" +
                "3. Sikkert & reversibelt:\n" +
                "   NologGuard tager backup af din DNS først og gendanner automatisk ved stop.",
                "Windows Proxy, DNS og Firewall blev gendannet."
            );

            // 28. Finnish (Suomi)
            cat["fi"] = Pack(
                "DoH + SNI-pilkkominen + AdBlock • Säilyttää oikean IP:n",
                "Suoja", "AdBlock", "Poikkeukset", "Asetukset", "Loki",
                "SUOJA POIS PÄÄLTÄ", "SUOJATTU", "KÄYNNISTÄ SUOJA", "SAMMUTA SUOJA",
                "1 klikkaus: käynnistä paikallinen DoH-välityspalvelin + SNI-pilkkominen + QUIC-ohitus.",
                "DoH-palvelu:", "SNI-pilkontatila:", "Itsetesti",
                "Aseta Windowsin välityspalvelin automaattisesti (Suositus)",
                "Ota AdBlocker & seurannanesto käyttöön (Verkkotunnussuoja)",
                "Käytä Windows 11 -järjestelmän DoH:ta (Vaatii Adminin)",
                "Estä QUIC UDP 443 palomuurilla (Vaatii Adminin)",
                "Käynnistä ylläpitäjänä", "ADMIN", "KÄYTT.",
                "Älykäs SNI-keskipilkkominen (Suositus)", "Kiinteät pienet lohkot", "Pois (Vain DoH-välitys)",
                "SNI-pilkottu", "Estetyt", "DoH (Välimuisti)", "Ohitettu",
                "Rehellinen rajaus: Säilyttää oikean IP:n, salaa DNS:n, pilkkoo SNI:n ja estää kolmannen osapuolen mainos-/seurantaverkkotunnukset (ei poista YouTuben kaltaisia videomainoksia).",
                "Suodatinlista:", "Päivitä lista", "Käytössä: {0} estettyä | {1} @@-poikkeusta",
                "Omat säännöt (yksi per rivi; lisää @@ salliaksesi; tukee hosts- ja AdBlock-muotoa):",
                "AdBlock-säännöt tallennettu ja otettu käyttöön.",
                "Estää mainos-/seurantaverkkotunnukset välityspalvelintasolla ennen DNS-hakua ilman HTTPS-purkua.",
                "SNI-pilkonnasta ohitettavat verkkotunnukset ja verkot (*.example.com ja # kommentit tuettu):",
                "Lisää", "Tallenna", "Oletukset", "Poikkeuslista tallennettu ja käytössä.",
                "Paikallinen välitysportti (1024-65535):", "Alkulohkon koko (tavua):", "Viive lohkojen välillä (ms):",
                "Oma DoH-URL (https://...):", "Käyttöliittymän kieli:",
                "Pienennä ilmaisinalueelle suljettaessa", "Käynnistä automaattisesti Windowsin kanssa",
                "Näytä verkkotunnukset lokissa (vain RAM, ei koskaan levylle)",
                "Windows-välityksen & palomuurin hätäpalautus", "Asetukset tallennettu.",
                "Lokit pysyvät vain RAM-muistissa (viimeiset 300 riviä) eivätkä tallennu levylle.", "Tyhjennä loki",
                "Avaa NologGuard", "Vaihda suojaus", "Lopeta (Poistaa suojauksen)", "NologGuard on yhä käynnissä ilmaisinalueella.",
                "Järjestelmänvalvojan oikeudet vaaditaan",
                "Järjestelmänlaajuinen DoH ja palomuurin QUIC-esto vaativat järjestelmänvalvojan oikeudet. Käynnistetäänkö uudelleen järjestelmänvalvojana?",
                "? Miksi osa valinnoista vaatii Adminin?", "Miksi jotkin asetukset vaativat järjestelmänvalvojan oikeudet?",
                "1. Päivittäinen selainsuojaus EI vaadi Admin-oikeuksia:\n" +
                "   Normaalissa USER-tilassa toimivat järjestelmän välityspalvelin (HKCU), HTTP/2 DoH, SNI-pilkkominen ja AdBlock.\n\n" +
                "2. Vain 2 järjestelmätason asetusta vaatii Adminin:\n" +
                "   • Windows 11 DoH: verkkosovittimen DNS:n ja HKLM-avainten muokkaus.\n" +
                "   • Palomuuri QUIC UDP 443: Windows Defender -palomuurisäännön hallinta.\n\n" +
                "3. Turvallinen ja palautuva:\n" +
                "   NologGuard varmuuskopioi alkuperäisen DNS:n ja palauttaa kaiken automaattisesti suljettaessa.",
                "Windowsin välityspalvelin, DNS ja palomuuri palautettu siististi."
            );

            // 29. Czech (Čeština)
            cat["cs"] = Pack(
                "DoH + Rozdělení SNI + AdBlock • Zachovává skutečnou IP",
                "Štít", "AdBlock", "Výjimky", "Nastavení", "Živý log",
                "ŠTÍT VYPNUT", "CHRÁNĚNO", "ZAPNOUT ŠTÍT", "VYPNOUT ŠTÍT",
                "Jedním kliknutím zapnete místní DoH proxy + rozdělení SNI + obejití QUIC.",
                "Poskytovatel DoH:", "Režim dělení SNI:", "Autotest",
                "Automaticky nastavit systémové proxy Windows (Doporučeno)",
                "Zapnout AdBlocker a Anti-Tracking (Ochrana domén)",
                "Použít systémové DoH Windows 11 (Vyžaduje Admin)",
                "Blokovat QUIC UDP 443 přes Firewall (Vyžaduje Admin)",
                "Spustit jako Admin", "ADMIN", "USER",
                "Chytré rozdělení středu SNI (Doporučeno)", "Pevné malé části", "Vypnuto (Pouze DoH proxy)",
                "SNI-děleno", "Blokováno", "DoH (Cache)", "Vynecháno",
                "Upřímný rozsah: Zachovává vaši reálnou IP, šifruje DNS, dělí SNI a blokuje reklamní/sledovací domény třetích stran (neodstraňuje videoreklamy z YouTube).",
                "Seznam filtrů:", "Aktualizovat", "Aktivní: {0} blokovaných domén | {1} povolení @@",
                "Vlastní pravidla (jedno na řádek; předpona @@ povolí; podporuje formát hosts i AdBlock):",
                "Pravidla AdBlocku uložena a okamžitě aktivována.",
                "Blokuje reklamní/sledovací domény na vrstvě proxy ještě před DNS dotazem bez dešifrování HTTPS.",
                "Domény a sítě vyňaté z dělení SNI (podporuje *.example.com a komentáře #):",
                "Přidat", "Uložit změny", "Výchozí", "Seznam výjimek uložen a aktivován.",
                "Port místního proxy (1024-65535):", "Velikost počáteční části (bajty):", "Prodleva mezi částmi (ms):",
                "Vlastní DoH URL (https://...):", "Jazyk rozhraní:",
                "Minimalizovat do oznamovací oblasti při zavření", "Spouštět automaticky se systémem Windows",
                "Zobrazovat domény v Živém logu (pouze v RAM, nikdy na disk)",
                "Nouzový reset proxy a firewallu Windows", "Nastavení uloženo.",
                "Záznamy zůstávají výhradně v RAM (posledních 300 řádků) a nikdy se neukládají na disk.", "Vymazat log",
                "Otevřít NologGuard", "Přepnout štít", "Ukončit (Vypne štít)", "NologGuard stále běží v oznamovací oblasti.",
                "Vyžadována práva správce",
                "Systémové DoH Windows a blokování QUIC firewallem vyžadují práva správce. Restartovat NologGuard jako správce?",
                "? Proč některé volby vyžadují správce?", "Proč některé možnosti vyžadují oprávnění správce?",
                "1. Běžná ochrana prohlížečů NEVYŽADUJE práva správce:\n" +
                "   V běžném režimu USER fungují systémové proxy (HKCU), HTTP/2 DoH, dělení SNI i AdBlock pro Chrome, Edge i Firefox.\n\n" +
                "2. Pouze 2 celosystémové volby vyžadují Admina:\n" +
                "   • Systémové DoH Windows 11: Úprava DNS síťových adaptérů a registru HKLM.\n" +
                "   • Blokování QUIC UDP 443: Přidání a smazání pravidla Windows Firewallu.\n\n" +
                "3. Bezpečné a vratné:\n" +
                "   NologGuard si předem zálohuje původní DNS a při vypnutí vše automaticky obnoví.",
                "Proxy Windows, DNS a pravidla Firewallu byly úspěšně obnoveny."
            );

            // 30. Romanian (Română)
            cat["ro"] = Pack(
                "DoH + Fragmentare SNI + AdBlock • Păstrează IP-ul real",
                "Scut", "AdBlock", "Excepții", "Setări", "Jurnal live",
                "SCUT DEZACTIVAT", "PROTEJAT", "ACTIVEAZĂ SCUTUL", "DEZACTIVEAZĂ SCUTUL",
                "1-clic pentru a activa proxy DoH local + fragmentare SNI + ocolire QUIC.",
                "Furnizor DoH:", "Mod fragmentare SNI:", "Autotest",
                "Setează automat Proxy-ul de sistem Windows (Recomandat)",
                "Activează AdBlocker și Anti-Tracking (Scut domenii)",
                "Aplică DoH global în Windows 11 (Necesită Admin)",
                "Blochează QUIC UDP 443 prin Firewall (Necesită Admin)",
                "Repornește ca Admin", "ADMIN", "USER",
                "Fragmentare inteligentă la mijlocul SNI (Recomandat)", "Fragmente mici fixe", "Oprit (Doar proxy DoH)",
                "SNI-divizat", "Reclame blocate", "DoH (Cache)", "Exceptate",
                "Acoperire reală: Păstrează IP-ul real, criptează DNS, împarte SNI și blochează domenii terțe de reclame/tracking (nu elimină reclamele video din aceeași sursă, ca YouTube).",
                "Listă filtre:", "Actualizează", "Activ: {0} domenii blocate | {1} excepții @@",
                "Reguli proprii (una pe linie; prefix @@ pentru a permite; suportă format hosts & AdBlock):",
                "Regulile AdBlock au fost salvate și aplicate.",
                "Blochează reclamele și trackerele la nivelul proxy înainte de interogarea DNS, fără decriptare HTTPS.",
                "Domenii și rețele excluse de la fragmentarea SNI (suportă *.exemplu.ro și comentarii #):",
                "Adaugă", "Salvează", "Implicit", "Lista de excepții a fost salvată și aplicată.",
                "Port Proxy local (1024-65535):", "Dimensiune fragment inițial (octeți):", "Întârziere între fragmente (ms):",
                "URL DoH personalizat (https://...):", "Limba interfeței:",
                "Minimizează în zona de notificare la închidere", "Pornește automat odată cu Windows",
                "Afișează domeniile în Jurnal (doar în RAM, niciodată pe disc)",
                "Resetare de urgență Proxy și Firewall Windows", "Setări salvate.",
                "Jurnalul rămâne strict în RAM (ultimele 300 de intrări) și nu este scris pe disc.", "Șterge jurnal",
                "Deschide NologGuard", "Comută scutul", "Ieșire (Dezactivează scutul)", "NologGuard rulează încă în zona de notificare.",
                "Drepturi de Administrator necesare",
                "DoH la nivel de sistem și blocarea QUIC în Firewall necesită drepturi de Administrator. Reporniți NologGuard ca Administrator acum?",
                "? De ce cer unele opțiuni Admin?", "De ce unele opțiuni necesită drepturi de Administrator?",
                "1. Protecția zilnică în browser NU cere drepturi de Admin:\n" +
                "   În modul USER funcționează Proxy-ul de sistem (HKCU), DoH HTTP/2, fragmentarea SNI și AdBlock pentru Chrome, Edge și Firefox.\n\n" +
                "2. Doar 2 funcții la nivel de sistem cer Admin:\n" +
                "   • DoH Windows 11 global: Modificarea DNS-ului adaptoarelor și a cheii HKLM.\n" +
                "   • Blocare QUIC UDP 443: Adăugarea/ștergerea regulii în Windows Firewall.\n\n" +
                "3. Sigur și reversibil:\n" +
                "   NologGuard salvează DNS-ul original și restaurează automat totul la oprire sau ieșire.",
                "Proxy-ul Windows, DNS și regulile Firewall au fost restaurate."
            );

            // 31. Hungarian (Magyar)
            cat["hu"] = Pack(
                "DoH + SNI-feldarabolás + AdBlock • Valódi IP megtartása",
                "Pajzs", "AdBlock", "Kivételek", "Beállítások", "Élő napló",
                "PAJZS KIKAPCSOLVA", "VÉDETT", "PAJZS BEKAPCSOLÁSA", "PAJZS KIKAPCSOLÁSA",
                "1 kattintás a helyi DoH-proxy + SNI-darabolás + QUIC-megkerülés bekapcsolásához.",
                "DoH szolgáltató:", "SNI darabolási mód:", "Önellenőrzés",
                "Windows rendszerproxy automatikus beállítása (Ajánlott)",
                "Reklám- és nyomkövető-blokkoló engedélyezése (Domain-pajzs)",
                "Windows 11 rendszerszintű DoH alkalmazása (Admin szükséges)",
                "QUIC UDP 443 tiltása tűzfallal (Admin szükséges)",
                "Újraindítás Adminként", "ADMIN", "USER",
                "Okos SNI-középdarabolás (Ajánlott)", "Fix kis csomagok", "Ki (Csak DoH-proxy)",
                "SNI-darabolt", "Blokkolt", "DoH (Gyorsítótár)", "Kihagyva",
                "Őszinte hatókör: Megtartja a valódi IP-címet, titkosítja a DNS-t, feldarabolja az SNI-t és blokkolja a külső reklám-/követődomaineket (nem szűri a YouTube-videóreklámokat).",
                "Szűrőlista:", "Lista frissítése", "Aktív: {0} tiltott domain | {1} @@ kivétel",
                "Egyéni szabályok (soronként egy; @@ előtag engedélyez; hosts és AdBlock formátum):",
                "Az AdBlock-szabályok mentve és azonnal alkalmazva.",
                "A HTTPS visszafejtése nélkül, proxy szinten blokkolja a reklámokat a DNS-lekérdezés előtt.",
                "Az SNI-darabolásból kizárt domainek és hálózatok (*.example.com és # megjegyzések támogatottak):",
                "Hozzáadás", "Mentés", "Alapértelmezett", "Kivétellista mentve és alkalmazva.",
                "Helyi proxyport (1024-65535):", "Kezdeti szeletméret (bájt):", "Késleltetés a szeletek között (ms):",
                "Egyéni DoH URL (https://...):", "Felület nyelve:",
                "Kis méret a tálcára bezáráskor", "Automatikus indítás a Windows-zal",
                "Domainek megjelenítése a naplóban (csak RAM-ban, lemezre nem íródik)",
                "Windows Proxy és Tűzfal vész-visszaállítás", "Beállítások elmentve.",
                "A napló kizárólag a memóriában él (utolsó 300 sor), és sosem kerül lemezre.", "Napló törlése",
                "NologGuard megnyitása", "Pajzs váltása", "Kilépés (Pajzs leállítása)", "A NologGuard tovább fut a tálcán.",
                "Rendszergazdai jogosultság szükséges",
                "A rendszerszintű DoH és a tűzfalas QUIC-tiltás rendszergazdai jogot igényel. Újraindítja most rendszergazdaként?",
                "? Miért kér néha Admin jogot?", "Miért igényel néhány opció rendszergazdai jogot?",
                "1. A napi böngészővédelem NEM igényel Admin jogot:\n" +
                "   Normál USER módban működik a rendszerproxy (HKCU), a HTTP/2 DoH, az SNI-darabolás és az AdBlock Chrome, Edge és Firefox alatt.\n\n" +
                "2. Csak 2 rendszerszintű funkció igényel Admint:\n" +
                "   • Windows 11 rendszerszintű DoH: hálózati adapter DNS és HKLM módosítása.\n" +
                "   • QUIC UDP 443 tűzfalszabály: tűzfalszabály létrehozása és törlése.\n\n" +
                "3. Biztonságos és visszaállítható:\n" +
                "   A NologGuard előre menti az eredeti DNS-t, és kikapcsoláskor mindent automatikusan visszaállít.",
                "A Windows Proxy, DNS és tűzfal állapota sikeresen visszaállítva."
            );

            // 32. Greek (Ελληνικά)
            cat["el"] = Pack(
                "DoH + Κατάτμηση SNI + AdBlock • Διατηρεί την πραγματική IP",
                "Ασπίδα", "AdBlock", "Εξαιρέσεις", "Ρυθμίσεις", "Καταγραφή",
                "ΑΣΠΙΔΑ ΑΝΕΝΕΡΓΗ", "ΠΡΟΣΤΑΤΕΥΜΕΝΟ", "ΕΝΕΡΓΟΠΟΙΗΣΗ", "ΑΠΕΝΕΡΓΟΠΟΙΗΣΗ",
                "Με 1 κλικ ενεργοποιείτε το τοπικό DoH proxy + κατάτμηση SNI + παράκαμψη QUIC.",
                "Πάροχος DoH:", "Λειτουργία SNI:", "Αυτοέλεγχος",
                "Αυτόματος ορισμός Proxy συστήματος Windows (Προτείνεται)",
                "Ενεργοποίηση AdBlocker & Anti-Tracking (Προστασία τομέων)",
                "Εφαρμογή DoH σε όλο το Windows 11 (Απαιτεί Admin)",
                "Φραγή QUIC UDP 443 μέσω Firewall (Απαιτεί Admin)",
                "Επανεκκίνηση ως Admin", "ADMIN", "ΧΡΗΣΤΗΣ",
                "Έξυπνη κατάτμηση μέσου SNI (Προτείνεται)", "Σταθερά μικρά τμήματα", "Ανενεργό (Μόνο DoH Proxy)",
                "Κατάτμηση SNI", "Αποκλεισμένα", "DoH (Cache)", "Παρακάμφθηκαν",
                "Ειλικρινές εύρος: Διατηρεί την πραγματική IP, κρυπτογραφεί το DNS, κατακερματίζει το SNI και μπλοκάρει διαφημίσεις/trackers τρίτων (δεν αφαιρεί διαφημίσεις βίντεο ιδίου τομέα όπως YouTube).",
                "Λίστα φίλτρων:", "Ενημέρωση", "Ενεργό: {0} φραγμένοι τομείς | {1} εξαιρέσεις @@",
                "Προσαρμοσμένοι κανόνες (ένας ανά γραμμή· @@ για εξαίρεση· υποστηρίζει hosts & AdBlock):",
                "Οι κανόνες AdBlock αποθηκεύτηκαν και εφαρμόστηκαν.",
                "Αποκλείει διαφημίσεις/trackers στο επίπεδο proxy πριν από το αίτημα DNS χωρίς αποκρυπτογράφηση HTTPS.",
                "Τομείς & δίκτυα που εξαιρούνται από κατάτμηση SNI (υποστηρίζει *.example.com και σχόλια #):",
                "Προσθήκη", "Αποθήκευση", "Προεπιλογή", "Η λίστα εξαιρέσεων αποθηκεύτηκε και εφαρμόστηκε.",
                "Τοπική θύρα Proxy (1024-65535):", "Μέγεθος αρχικού τμήματος (bytes):", "Καθυστέρηση μεταξύ τμημάτων (ms):",
                "Προσαρμοσμένο DoH URL (https://...):", "Γλώσσα διεπαφής:",
                "Ελαχιστοποίηση στην περιοχή ειδοποιήσεων κατά το κλείσιμο", "Αυτόματη εκκίνηση με τα Windows",
                "Εμφάνιση τομέων στην Καταγραφή (μόνο στη RAM, ποτέ στον δίσκο)",
                "Επαναφορά έκτακτης ανάγκης Proxy & Firewall Windows", "Οι ρυθμίσεις αποθηκεύτηκαν.",
                "Τα αρχεία καταγραφής μένουν μόνο στη RAM (τελευταίες 300 γραμμές) και δεν γράφονται στον δίσκο.", "Εκκαθάριση",
                "Άνοιγμα NologGuard", "Εναλλαγή ασπίδας", "Έξοδος (Κλείνει ασπίδα)", "Το NologGuard εξακολουθεί να εκτελείται στο tray.",
                "Απαιτούνται δικαιώματα διαχειριστή",
                "Το DoH συστήματος και η φραγή QUIC στο Firewall απαιτούν δικαιώματα διαχειριστή. Επανεκκίνηση ως Διαχειριστής τώρα;",
                "? Γιατί κάποιες επιλογές θέλουν Admin;", "Γιατί κάποιες επιλογές απαιτούν Διαχειριστή;",
                "1. Η καθημερινή προστασία περιηγητή ΔΕΝ χρειάζεται Admin:\n" +
                "   Στη λειτουργία USER δουλεύουν κανονικά το Proxy συστήματος (HKCU), HTTP/2 DoH, κατάτμηση SNI και AdBlock.\n\n" +
                "2. Μόνο 2 επιλογές συστήματος απαιτούν Admin:\n" +
                "   • DoH συστήματος Windows 11: Τροποποίηση DNS καρτών δικτύου και HKLM.\n" +
                "   • Φραγή QUIC UDP 443: Προσθήκη/διαγραφή κανόνων στο Windows Firewall.\n\n" +
                "3. Ασφαλές & αναστρέψιμο:\n" +
                "   Το NologGuard αποθηκεύει αντίγραφο του DNS και επαναφέρει τα πάντα αυτόματα κατά τη διακοπή.",
                "Το Proxy, το DNS και το Firewall των Windows επαναφέρθηκαν επιτυχώς."
            );

            AddExtra(cat, "ab_builtin", new string[,] {
                {"en","Built-in"}, {"vi","Có sẵn"}, {"zh","内置"}, {"zh-TW","內建"},
                {"ja","内蔵済み"}, {"ko","내장됨"}, {"ru","Встроен"}, {"uk","Вбудовано"},
                {"tr","Dahili"}, {"ar","مضمّن"}, {"fa","داخلی"}, {"he","מובנה"},
                {"id","Bawaan"}, {"ms","Binaan dalam"}, {"th","มีในตัว"}, {"bn","অন্তর্নির্মিত"},
                {"hi","अंतर्निहित"}, {"es","Integrado"}, {"pt","Integrado"}, {"fr","Intégré"},
                {"de","Integriert"}, {"it","Integrato"}, {"nl","Ingebouwd"}, {"pl","Wbudow."},
                {"cs","Vestavěný"}, {"ro","Integrat"}, {"hu","Beépített"}, {"el","Ενσωματ."},
                {"sv","Inbyggd"}, {"nb","Innebygd"}, {"da","Indbygget"}, {"fi","Sisäinen"}
            });
            AddExtra(cat, "ab_paused", new string[,] {
                {"en","Paused on Shield tab"}, {"vi","Đang tắt ở tab Bảo vệ"}, {"zh","已在防护页暂停"}, {"zh-TW","已在防護頁暫停"},
                {"ja","保護タブで一時停止中"}, {"ko","보호 탭에서 일시 중지됨"}, {"ru","Отключено во вкладке Защита"}, {"uk","Вимкнено у вкладці Захист"},
                {"tr","Kalkan sekmesinde duraklatıldı"}, {"ar","متوقف مؤقتاً في تبويب الحماية"}, {"fa","در برگه محافظت متوقف است"}, {"he","מושהה בלשונית ההגנה"},
                {"id","Dijeda di tab Perisai"}, {"ms","Dijeda pada tab Perisai"}, {"th","หยุดชั่วคราวที่แท็บป้องกัน"}, {"bn","সুরক্ষা ট্যাবে থামানো আছে"},
                {"hi","सुरक्षा टैब में रोका गया"}, {"es","Pausado en pestaña Escudo"}, {"pt","Pausado na aba Escudo"}, {"fr","En pause dans Bouclier"},
                {"de","Im Tab Schutz pausiert"}, {"it","In pausa nella scheda Scudo"}, {"nl","Gepauzeerd op tabblad Schild"}, {"pl","Wstrzymano w karcie Tarcza"},
                {"cs","Pozastaveno na kartě Štít"}, {"ro","Întrerupt în fila Scut"}, {"hu","Szüneteltetve a Pajzs lapon"}, {"el","Σε παύση στην καρτέλα Ασπίδα"},
                {"sv","Pausad på fliken Skydd"}, {"nb","Pauset i Skjold-fanen"}, {"da","Sat på pause under Skjold"}, {"fi","Keskeytetty Suoja-välilehdellä"}
            });
            AddExtra(cat, "btn_test_doh", new string[,] {
                {"en","Self-Check"}, {"vi","Tự kiểm tra"}, {"zh","一键自检"}, {"zh-TW","一鍵自檢"},
                {"ja","自己診断"}, {"ko","자가 진단"}, {"ru","Проверка"}, {"uk","Перевірка"},
                {"tr","Otomatik Test"}, {"ar","فحص ذاتي"}, {"fa","خودآزمایی"}, {"he","בדיקה עצמית"},
                {"id","Cek Sistem"}, {"ms","Uji Sendiri"}, {"th","ตรวจระบบ"}, {"bn","স্ব-পরীক্ষা"},
                {"hi","स्व-जांच"}, {"es","Autotest"}, {"pt","Autoteste"}, {"fr","Diagnostic"},
                {"de","Selbsttest"}, {"it","Autotest"}, {"nl","Zelftest"}, {"pl","Autotest"},
                {"cs","Autotest"}, {"ro","Autotest"}, {"hu","Önellenőrzés"}, {"el","Αυτοέλεγχος"},
                {"sv","Självtest"}, {"nb","Selvtest"}, {"da","Selvtest"}, {"fi","Itsetesti"}
            });
            AddExtra(cat, "diag_ok_prefix", new string[,] {
                {"en","✓ Self-Check: DoH {0} ({1}ms) • AdBlock {2}"},
                {"vi","✓ Tự kiểm tra: DoH {0} ({1}ms) • AdBlock {2}"},
                {"zh","✓ 自检正常: DoH {0} ({1}ms) • 去广告 {2}"},
                {"zh-TW","✓ 自檢正常: DoH {0} ({1}ms) • 廣告封鎖 {2}"},
                {"ja","✓ 自己診断OK: DoH {0} ({1}ms) • 広告拒否 {2}"},
                {"ko","✓ 자가 진단 OK: DoH {0} ({1}ms) • 광고 차단 {2}"},
                {"ru","✓ Проверка OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"uk","✓ Перевірка OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"tr","✓ Test OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"ar","✓ فحص ذاتي OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"fa","✓ خودآزمایی OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"he","✓ בדיקה עצמית OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"id","✓ Cek OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"ms","✓ Uji OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"th","✓ ตรวจระบบ OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"bn","✓ পরীক্ষা OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"hi","✓ जांच OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"es","✓ Autotest OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"pt","✓ Autoteste OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"fr","✓ Diagnostic OK : DoH {0} ({1}ms) • AdBlock {2}"},
                {"de","✓ Selbsttest OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"it","✓ Autotest OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"nl","✓ Zelftest OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"pl","✓ Autotest OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"cs","✓ Autotest OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"ro","✓ Autotest OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"hu","✓ Teszt OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"el","✓ Έλεγχος OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"sv","✓ Självtest OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"nb","✓ Selvtest OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"da","✓ Selvtest OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"fi","✓ Itsetesti OK: DoH {0} ({1}ms) • AdBlock {2}"}
            });
            AddExtra(cat, "ab_custom_url", new string[,] {
                {"en","Custom Filter URL (hosts / domain list):"},
                {"vi","Link bộ lọc tùy chỉnh (hosts / tên miền):"},
                {"zh","自定义规则订阅链接 (hosts / 域名):"},
                {"zh-TW","自訂過濾清單網址 (hosts / 網域):"},
                {"ja","カスタムフィルター URL (hosts / ドメイン):"},
                {"ko","사용자 지정 필터 URL (hosts / 도메인):"},
                {"ru","Свой URL фильтра (hosts / домены):"},
                {"uk","Власний URL фільтра (hosts / домени):"},
                {"tr","Özel Filtre URL'si (hosts / alan adı):"},
                {"ar","رابط فلتر مخصص (hosts / قائمة نطاقات):"},
                {"fa","آدرس فیلتر سفارشی (فهرست hosts / دامنه):"},
                {"he","כתובת מסנן מותאמת (hosts / דומיינים):"},
                {"id","URL Filter Kustom (hosts / domain):"},
                {"ms","URL Penapis Tersuai (hosts / domain):"},
                {"th","URL ตัวกรองกำหนดเอง (hosts / โดเมน):"},
                {"bn","কাস্টম ফিল্টার URL (hosts / ডোমেইন):"},
                {"hi","कस्टम फ़िल्टर URL (hosts / डोमेन):"},
                {"es","URL de filtro personalizado (hosts / dominios):"},
                {"pt","URL de filtro personalizado (hosts / domínios):"},
                {"fr","URL de filtre personnalisée (hosts / domaines) :"},
                {"de","Benutzerdefinierte Filter-URL (hosts / Domains):"},
                {"it","URL filtro personalizzato (hosts / domini):"},
                {"nl","Aangepaste filter-URL (hosts / domeinen):"},
                {"pl","Własny adres URL filtra (hosts / domeny):"},
                {"cs","Vlastní URL filtru (hosts / domény):"},
                {"ro","URL filtru personalizat (hosts / domenii):"},
                {"hu","Egyéni szűrő URL (hosts / domainek):"},
                {"el","Προσαρμοσμένο URL φίλτρου (hosts / τομείς):"},
                {"sv","Anpassad filter-URL (hosts / domäner):"},
                {"nb","Egendefinert filter-URL (hosts / domener):"},
                {"da","Brugerdefineret filter-URL (hosts / domæner):"},
                {"fi","Oma suodatin-URL (hosts / verkkotunnukset):"}
            });
            AddExtra(cat, "chk_auto_protect", new string[,] {
                {"en","Automatically enable Shield when NologGuard launches"},
                {"vi","Tự động bật Bảo vệ ngay khi mở NologGuard"},
                {"zh","启动 NologGuard 时自动开启防护"},
                {"zh-TW","啟動 NologGuard 時自動開啟防護"},
                {"ja","NologGuard 起動時に自動で保護を有効にする"},
                {"ko","NologGuard 실행 시 자동으로 보호 켜기"},
                {"ru","Автоматически включать защиту при запуске NologGuard"},
                {"uk","Автоматично вмикати захист при запуску NologGuard"},
                {"tr","NologGuard açıldığında korumayı otomatik etkinleştir"},
                {"ar","تفعيل الحماية تلقائياً عند تشغيل NologGuard"},
                {"fa","فعال‌سازی خودکار سپر هنگام باز شدن NologGuard"},
                {"he","הפעל הגנה אוטומטית בעת פתיחת NologGuard"},
                {"id","Aktifkan Perisai otomatis saat NologGuard dibuka"},
                {"ms","Aktifkan Perisai automatik apabila NologGuard dibuka"},
                {"th","เปิดการป้องกันอัตโนมัติเมื่อเปิด NologGuard"},
                {"bn","NologGuard খুললে স্বয়ংক্রিয়ভাবে সুরক্ষা চালু করুন"},
                {"hi","NologGuard खुलने पर सुरक्षा स्वतः चालू करें"},
                {"es","Activar Escudo automáticamente al abrir NologGuard"},
                {"pt","Ativar Proteção automaticamente ao iniciar o NologGuard"},
                {"fr","Activer automatiquement le Bouclier au lancement de NologGuard"},
                {"de","Schutz beim Start von NologGuard automatisch aktivieren"},
                {"it","Attiva automaticamente lo Scudo all'avvio di NologGuard"},
                {"nl","Schild automatisch inschakelen bij starten van NologGuard"},
                {"pl","Automatycznie włączaj Tarczę po uruchomieniu NologGuard"},
                {"cs","Automaticky zapnout Štít při spuštění NologGuard"},
                {"ro","Activează automat Scutul la pornirea NologGuard"},
                {"hu","Pajzs automatikus bekapcsolása a NologGuard indításakor"},
                {"el","Αυτόματη ενεργοποίηση Ασπίδας κατά την εκκίνηση του NologGuard"},
                {"sv","Aktivera skydd automatiskt när NologGuard startas"},
                {"nb","Aktiver skjold automatisk når NologGuard starter"},
                {"da","Aktiver skjold automatisk når NologGuard åbner"},
                {"fi","Ota suoja käyttöön automaattisesti NologGuardin käynnistyessä"}
            });
            AddExtra(cat, "btn_check_update", new string[,] {
                {"en","Check for Updates"}, {"vi","Kiểm tra cập nhật"}, {"zh","检查更新"}, {"zh-TW","檢查更新"},
                {"ja","アップデート確認"}, {"ko","업데이트 확인"}, {"ru","Проверить обновления"}, {"uk","Перевірити оновлення"},
                {"tr","Güncelleme Kontrolü"}, {"ar","التحقق من التحديثات"}, {"fa","بررسی به‌روزرسانی"}, {"he","בדוק עדכונים"},
                {"id","Cek Pembaruan"}, {"ms","Semak Kemas Kini"}, {"th","ตรวจสอบอัปเดต"}, {"bn","আপডেট পরীক্ষা করুন"},
                {"hi","अपडेट जांचें"}, {"es","Buscar actualizaciones"}, {"pt","Verificar atualizações"}, {"fr","Vérifier les mises à jour"},
                {"de","Nach Updates suchen"}, {"it","Verifica aggiornamenti"}, {"nl","Controleren op updates"}, {"pl","Sprawdź aktualizacje"},
                {"cs","Zkontrolovat aktualizace"}, {"ro","Verifică actualizările"}, {"hu","Frissítések keresése"}, {"el","Έλεγχος ενημερώσεων"},
                {"sv","Sök efter uppdateringar"}, {"nb","Se etter oppdateringer"}, {"da","Søg efter opdateringer"}, {"fi","Tarkista päivitykset"}
            });
            AddExtra(cat, "upd_checking", new string[,] {
                {"en","Checking GitHub..."}, {"vi","Đang kiểm tra GitHub..."}, {"zh","正在检查 GitHub..."}, {"zh-TW","正在檢查 GitHub..."},
                {"ja","GitHub を確認中..."}, {"ko","GitHub 확인 중..."}, {"ru","Проверка GitHub..."}, {"uk","Перевірка GitHub..."},
                {"tr","GitHub kontrol ediliyor..."}, {"ar","جارٍ التحقق من GitHub..."}, {"fa","در حال بررسی GitHub..."}, {"he","בודק ב-GitHub..."},
                {"id","Memeriksa GitHub..."}, {"ms","Menyemak GitHub..."}, {"th","กำลังตรวจสอบ GitHub..."}, {"bn","GitHub পরীক্ষা করা হচ্ছে..."},
                {"hi","GitHub जांच रहे हैं..."}, {"es","Comprobando GitHub..."}, {"pt","Verificando GitHub..."}, {"fr","Vérification sur GitHub..."},
                {"de","GitHub wird geprüft..."}, {"it","Controllo su GitHub..."}, {"nl","GitHub controleren..."}, {"pl","Sprawdzanie GitHub..."},
                {"cs","Kontrola GitHubu..."}, {"ro","Se verifică GitHub..."}, {"hu","GitHub ellenőrzése..."}, {"el","Έλεγχος στο GitHub..."},
                {"sv","Kontrollerar GitHub..."}, {"nb","Sjekker GitHub..."}, {"da","Kontrollerer GitHub..."}, {"fi","Tarkistetaan GitHubista..."}
            });
            AddExtra(cat, "upd_latest", new string[,] {
                {"en","Latest version (v{0})"}, {"vi","Đang dùng bản mới nhất (v{0})"}, {"zh","已是最新版 (v{0})"}, {"zh-TW","已是最新版 (v{0})"},
                {"ja","最新版を使用中 (v{0})"}, {"ko","최신 버전입니다 (v{0})"}, {"ru","Актуальная версия (v{0})"}, {"uk","Остання версія (v{0})"},
                {"tr","En güncel sürüm (v{0})"}, {"ar","أحدث إصدار (v{0})"}, {"fa","آخرین نسخه (v{0})"}, {"he","הגרסה העדכנית ביותר (v{0})"},
                {"id","Versi terbaru (v{0})"}, {"ms","Versi terkini (v{0})"}, {"th","เป็นเวอร์ชันล่าสุดแล้ว (v{0})"}, {"bn","সর্বশেষ সংস্করণ (v{0})"},
                {"hi","नवीनतम संस्करण (v{0})"}, {"es","Última versión (v{0})"}, {"pt","Versão mais recente (v{0})"}, {"fr","À jour (v{0})"},
                {"de","Aktuellste Version (v{0})"}, {"it","Versione aggiornata (v{0})"}, {"nl","Nieuwste versie (v{0})"}, {"pl","Najnowsza wersja (v{0})"},
                {"cs","Nejnovější verze (v{0})"}, {"ro","Ultima versiune (v{0})"}, {"hu","Legújabb verzió (v{0})"}, {"el","Τελευταία έκδοση (v{0})"},
                {"sv","Senaste versionen (v{0})"}, {"nb","Nyeste versjon (v{0})"}, {"da","Nyeste version (v{0})"}, {"fi","Uusin versio (v{0})"}
            });
            AddExtra(cat, "upd_available", new string[,] {
                {"en","New v{0} available! Opening..."}, {"vi","Có bản mới v{0}! Đang mở..."}, {"zh","发现新版本 v{0}！正在打开..."}, {"zh-TW","發現新版本 v{0}！正在開啟..."},
                {"ja","新バージョン v{0} あり！開いています..."}, {"ko","새 버전 v{0} 있음! 여는 중..."}, {"ru","Доступна v{0}! Открытие..."}, {"uk","Доступна v{0}! Відкриття..."},
                {"tr","Yeni v{0} mevcut! Açılıyor..."}, {"ar","يتوفر v{0} جديد! جارٍ الفتح..."}, {"fa","نسخه جدید v{0} موجود است! در حال باز کردن..."}, {"he","גרסה חדשה v{0} זמינה! פותח..."},
                {"id","Versi baru v{0} tersedia! Membuka..."}, {"ms","Versi baharu v{0} tersedia! Membuka..."}, {"th","มีเวอร์ชันใหม่ v{0}! กำลังเปิด..."}, {"bn","নতুন v{0} উপলব্ধ! খোলা হচ্ছে..."},
                {"hi","नया v{0} उपलब्ध! खुल रहा है..."}, {"es","¡Nueva v{0} disponible! Abriendo..."}, {"pt","Nova v{0} disponível! Abrindo..."}, {"fr","Nouvelle v{0} disponible ! Ouverture..."},
                {"de","Neue v{0} verfügbar! Wird geöffnet..."}, {"it","Nuova v{0} disponibile! Apertura..."}, {"nl","Nieuwe v{0} beschikbaar! Openen..."}, {"pl","Nowa v{0} dostępna! Otwieranie..."},
                {"cs","Nová v{0} k dispozici! Otevírání..."}, {"ro","Noua v{0} disponibilă! Se deschide..."}, {"hu","Új v{0} érhető el! Megnyitás..."}, {"el","Νέα v{0} διαθέσιμη! Άνοιγμα..."},
                {"sv","Ny v{0} tillgänglig! Öppnar..."}, {"nb","Ny v{0} tilgjengelig! Åpner..."}, {"da","Ny v{0} tilgængelig! Åbner..."}, {"fi","Uusi v{0} saatavilla! Avataan..."}
            });
            AddExtra(cat, "upd_err", new string[,] {
                {"en","Update check failed: {0}"}, {"vi","Không kiểm tra được: {0}"}, {"zh","检查失败: {0}"}, {"zh-TW","檢查失敗: {0}"},
                {"ja","確認エラー: {0}"}, {"ko","확인 실패: {0}"}, {"ru","Ошибка проверки: {0}"}, {"uk","Помилка перевірки: {0}"},
                {"tr","Güncelleme hatası: {0}"}, {"ar","فشل التحقق: {0}"}, {"fa","خطای بررسی: {0}"}, {"he","בדיקת העדכון נכשלה: {0}"},
                {"id","Gagal memeriksa: {0}"}, {"ms","Semakan gagal: {0}"}, {"th","ตรวจสอบไม่สำเร็จ: {0}"}, {"bn","আপডেট পরীক্ষা ব্যর্থ: {0}"},
                {"hi","जांच विफल: {0}"}, {"es","Error al comprobar: {0}"}, {"pt","Falha ao verificar: {0}"}, {"fr","Échec de vérification : {0}"},
                {"de","Prüfung fehlgeschlagen: {0}"}, {"it","Errore di verifica: {0}"}, {"nl","Controleren mislukt: {0}"}, {"pl","Błąd sprawdzania: {0}"},
                {"cs","Kontrola selhala: {0}"}, {"ro","Verificarea a eșuat: {0}"}, {"hu","Ellenőrzés sikertelen: {0}"}, {"el","Αποτυχία ελέγχου: {0}"},
                {"sv","Kunde inte söka: {0}"}, {"nb","Oppdateringssjekk feilet: {0}"}, {"da","Opdateringstjek fejlede: {0}"}, {"fi","Tarkistus epäonnistui: {0}"}
            });
            AddExtra(cat, "tray_pause_ab5", new string[,] {
                {"en","Pause AdBlock (5 min)"}, {"vi","Tạm dừng Chặn QC (5 phút)"}, {"zh","暂停去广告 (5 分钟)"}, {"zh-TW","暫停廣告封鎖 (5 分鐘)"},
                {"ja","広告ブロック一時停止 (5分)"}, {"ko","광고 차단 일시 중지 (5분)"}, {"ru","Пауза AdBlock (5 мин)"}, {"uk","Пауза AdBlock (5 хв)"},
                {"tr","AdBlock'u Duraklat (5 dk)"}, {"ar","إيقاف مانع الإعلانات (5 د)"}, {"fa","توقف تبلیغ‌گیر (۵ دقیقه)"}, {"he","השהה חוסם פרסומות (5 דק')"},
                {"id","Jeda AdBlock (5 mnt)"}, {"ms","Jeda AdBlock (5 min)"}, {"th","หยุดพัก AdBlock (5 นาที)"}, {"bn","AdBlock থামান (৫ মি.)"},
                {"hi","AdBlock रोकें (5 मिनट)"}, {"es","Pausar AdBlock (5 min)"}, {"pt","Pausar AdBlock (5 min)"}, {"fr","Mettre AdBlock en pause (5 min)"},
                {"de","AdBlock pausieren (5 Min.)"}, {"it","Pausa AdBlock (5 min)"}, {"nl","AdBlock pauzeren (5 min)"}, {"pl","Wstrzymaj AdBlock (5 min)"},
                {"cs","Pozastavit AdBlock (5 min)"}, {"ro","Pauză AdBlock (5 min)"}, {"hu","AdBlock szüneteltetése (5 p)"}, {"el","Παύση AdBlock (5 λ.)"},
                {"sv","Pausa AdBlock (5 min)"}, {"nb","Pause AdBlock (5 min)"}, {"da","Sæt AdBlock på pause (5 min)"}, {"fi","Keskeytä AdBlock (5 min)"}
            });
            AddExtra(cat, "tray_pause_shield15", new string[,] {
                {"en","Pause Shield (15 min)"}, {"vi","Tạm dừng Bảo vệ (15 phút)"}, {"zh","暂停防护 (15 分钟)"}, {"zh-TW","暫停防護 (15 分鐘)"},
                {"ja","保護を一時停止 (15分)"}, {"ko","보호 일시 중지 (15분)"}, {"ru","Пауза защиты (15 мин)"}, {"uk","Пауза захисту (15 хв)"},
                {"tr","Kalkanı Duraklat (15 dk)"}, {"ar","إيقاف الحماية (15 د)"}, {"fa","توقف سپر (۱۵ دقیقه)"}, {"he","השהה הגנה (15 דק')"},
                {"id","Jeda Perisai (15 mnt)"}, {"ms","Jeda Perisai (15 min)"}, {"th","หยุดพักการป้องกัน (15 นาที)"}, {"bn","সুরক্ষা থামান (১৫ মি.)"},
                {"hi","सुरक्षा रोकें (15 मिनट)"}, {"es","Pausar Escudo (15 min)"}, {"pt","Pausar Proteção (15 min)"}, {"fr","Mettre Bouclier en pause (15 min)"},
                {"de","Schutz pausieren (15 Min.)"}, {"it","Pausa Scudo (15 min)"}, {"nl","Schild pauzeren (15 min)"}, {"pl","Wstrzymaj Tarczę (15 min)"},
                {"cs","Pozastavit Štít (15 min)"}, {"ro","Pauză Scut (15 min)"}, {"hu","Pajzs szüneteltetése (15 p)"}, {"el","Παύση Ασπίδας (15 λ.)"},
                {"sv","Pausa skydd (15 min)"}, {"nb","Pause Skjold (15 min)"}, {"da","Sæt Skjold på pause (15 min)"}, {"fi","Keskeytä suoja (15 min)"}
            });
            AddExtra(cat, "tray_resume", new string[,] {
                {"en","Resume Now"}, {"vi","Tiếp tục ngay"}, {"zh","立即恢复"}, {"zh-TW","立即恢復"},
                {"ja","今すぐ再開"}, {"ko","지금 재개"}, {"ru","Возобновить сейчас"}, {"uk","Відновити зараз"},
                {"tr","Şimdi Sürdür"}, {"ar","استئناف الآن"}, {"fa","ادامه فوری"}, {"he","חדש כעת"},
                {"id","Lanjutkan Sekarang"}, {"ms","Sambung Sekarang"}, {"th","กลับมาทำงานทันที"}, {"bn","এখনই চালু করুন"},
                {"hi","अभी फिर शुरू करें"}, {"es","Reanudar ahora"}, {"pt","Retomar agora"}, {"fr","Reprendre maintenant"},
                {"de","Jetzt fortsetzen"}, {"it","Riprendi ora"}, {"nl","Nu hervatten"}, {"pl","Wznów teraz"},
                {"cs","Obnovit nyní"}, {"ro","Reia acum"}, {"hu","Folytatás most"}, {"el","Συνέχιση τώρα"},
                {"sv","Återuppta nu"}, {"nb","Fortsett nå"}, {"da","Genoptag nu"}, {"fi","Jatka nyt"}
            });
            AddExtra(cat, "tray_paused_msg", new string[,] {
                {"en","Paused ({0}m left)"}, {"vi","Tạm dừng (còn {0}p)"}, {"zh","已暂停 (剩余 {0}分)"}, {"zh-TW","已暫停 (剩餘 {0}分)"},
                {"ja","一時停止中 (残り {0}分)"}, {"ko","일시 중지 (남은 {0}분)"}, {"ru","Пауза ({0} мин)"}, {"uk","Пауза ({0} хв)"},
                {"tr","Duraklatıldı ({0} dk)"}, {"ar","متوقف ({0} د)"}, {"fa","متوقف ({0} د)"}, {"he","מושהה ({0} דק')"},
                {"id","Dijeda ({0} mnt lagi)"}, {"ms","Dijeda ({0} min)"}, {"th","หยุดชั่วคราว ({0} นาที)"}, {"bn","থামানো ({0} মি.)"},
                {"hi","रोका गया ({0} मिनट)"}, {"es","En pausa ({0} min)"}, {"pt","Em pausa ({0} min)"}, {"fr","En pause ({0} min)"},
                {"de","Pausiert ({0} Min.)"}, {"it","In pausa ({0} min)"}, {"nl","Gepauzeerd ({0} min)"}, {"pl","Wstrzymano ({0} min)"},
                {"cs","Pozastaveno ({0} min)"}, {"ro","În pauză ({0} min)"}, {"hu","Szünet ({0} p)"}, {"el","Παύση ({0} λ.)"},
                {"sv","Pausad ({0} min)"}, {"nb","Pauset ({0} min)"}, {"da","Pause ({0} min)"}, {"fi","Tauko ({0} min)"}
            });

            return cat;
        }

        private static void AddExtra(Dictionary<string, Dictionary<string, string>> cat, string key, string[,] pairs)
        {
            for (int i = 0; i < pairs.GetLength(0); i++)
            {
                Dictionary<string, string> d;
                if (cat.TryGetValue(pairs[i, 0], out d))
                {
                    d[key] = pairs[i, 1];
                }
            }
        }

        private static Dictionary<string, string> Pack(
            string subtitle,
            string tabDashboard, string tabAdblock, string tabWhitelist, string tabSettings, string tabLog,
            string statusOff, string statusOn, string btnEnable, string btnDisable, string statusHintOff,
            string lblDoh, string lblSniMode, string btnTestDoh,
            string chkSysProxy, string chkAdblock, string chkSysDoh, string chkBlockQuic,
            string btnAdmin, string badgeAdmin, string badgeUser,
            string modeSmart, string modeFixed, string modeOff,
            string statProtected, string statBlocked, string statDoh, string statExcluded,
            string disclaimer,
            string abPreset, string abUpdate, string abRulesCount, string abDesc, string abSaved, string abNote,
            string wlDesc, string btnAdd, string btnSave, string btnReset, string wlSaved,
            string setPort, string setChunk, string setDelay, string setCustomDoh, string setLang,
            string chkTray, string chkStartup, string chkLogUi, string btnRecover, string setSaved,
            string logNote, string btnClear,
            string trayOpen, string trayToggle, string trayExit, string trayRunning,
            string adminTitle, string adminMsg, string btnWhyAdmin, string whyAdminTitle, string whyAdminBody,
            string recoverOk)
        {
            return new Dictionary<string, string>
            {
                { "subtitle", subtitle },
                { "tab_dashboard", tabDashboard },
                { "tab_adblock", tabAdblock },
                { "tab_whitelist", tabWhitelist },
                { "tab_settings", tabSettings },
                { "tab_log", tabLog },
                { "status_off", statusOff },
                { "status_on", statusOn },
                { "btn_enable", btnEnable },
                { "btn_disable", btnDisable },
                { "status_hint_off", statusHintOff },
                { "lbl_doh", lblDoh },
                { "lbl_sni_mode", lblSniMode },
                { "btn_test_doh", btnTestDoh },
                { "chk_sys_proxy", chkSysProxy },
                { "chk_adblock", chkAdblock },
                { "chk_sys_doh", chkSysDoh },
                { "chk_block_quic", chkBlockQuic },
                { "btn_admin", btnAdmin },
                { "badge_admin", badgeAdmin },
                { "badge_user", badgeUser },
                { "mode_smart", modeSmart },
                { "mode_fixed", modeFixed },
                { "mode_off", modeOff },
                { "stat_protected", statProtected },
                { "stat_blocked", statBlocked },
                { "stat_doh", statDoh },
                { "stat_excluded", statExcluded },
                { "disclaimer", disclaimer },
                { "ab_preset", abPreset },
                { "ab_update", abUpdate },
                { "ab_rules_count", abRulesCount },
                { "ab_desc", abDesc },
                { "ab_saved", abSaved },
                { "ab_note", abNote },
                { "wl_desc", wlDesc },
                { "btn_add", btnAdd },
                { "btn_save", btnSave },
                { "btn_reset", btnReset },
                { "wl_saved", wlSaved },
                { "set_port", setPort },
                { "set_chunk", setChunk },
                { "set_delay", setDelay },
                { "set_custom_doh", setCustomDoh },
                { "set_lang", setLang },
                { "chk_tray", chkTray },
                { "chk_startup", chkStartup },
                { "chk_log_ui", chkLogUi },
                { "btn_recover", btnRecover },
                { "set_saved", setSaved },
                { "log_note", logNote },
                { "btn_clear", btnClear },
                { "tray_open", trayOpen },
                { "tray_toggle", trayToggle },
                { "tray_exit", trayExit },
                { "tray_running", trayRunning },
                { "admin_needed_title", adminTitle },
                { "admin_needed_msg", adminMsg },
                { "btn_why_admin", btnWhyAdmin },
                { "why_admin_title", whyAdminTitle },
                { "why_admin_body", whyAdminBody },
                { "recover_ok", recoverOk }
            };
        }
    }
}
