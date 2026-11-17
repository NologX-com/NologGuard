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
    /// Multi-language UI localization table supporting 18 languages with English fallback.
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
            new LanguageInfo("id", "Bahasa Indonesia"),
            new LanguageInfo("th", "ภาษาไทย"),
            new LanguageInfo("es", "Español"),
            new LanguageInfo("pt", "Português"),
            new LanguageInfo("fr", "Français"),
            new LanguageInfo("de", "Deutsch"),
            new LanguageInfo("it", "Italiano"),
            new LanguageInfo("pl", "Polski"),
            new LanguageInfo("hi", "हिन्दी")
        };

        private static readonly Dictionary<string, Dictionary<string, string>> Catalog = BuildCatalog();

        public static bool IsSupported(string code)
        {
            if (string.IsNullOrEmpty(code)) return false;
            return Catalog.ContainsKey(code);
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
                string two = ci.TwoLetterISOLanguageName ?? string.Empty;
                if (IsSupported(two))
                {
                    return two.ToLowerInvariant();
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

            AddExtra(cat, "ab_builtin", new string[,] {
                {"en","Built-in"}, {"vi","Có sẵn"}, {"zh","内置"}, {"zh-TW","內建"},
                {"ja","内蔵済み"}, {"ko","내장됨"}, {"ru","Встроен"}, {"uk","Вбудовано"},
                {"tr","Dahili"}, {"id","Bawaan"}, {"th","มีในตัว"}, {"es","Integrado"},
                {"pt","Integrado"}, {"fr","Intégré"}, {"de","Integriert"}, {"it","Integrato"},
                {"pl","Wbudow."}, {"hi","अंतर्निहित"}
            });
            AddExtra(cat, "ab_paused", new string[,] {
                {"en","Paused on Shield tab"}, {"vi","Đang tắt ở tab Bảo vệ"}, {"zh","已在防护页暂停"}, {"zh-TW","已在防護頁暫停"},
                {"ja","保護タブで一時停止中"}, {"ko","보호 탭에서 일시 중지됨"}, {"ru","Отключено во вкладке Защита"}, {"uk","Вимкнено у вкладці Захист"},
                {"tr","Kalkan sekmesinde duraklatıldı"}, {"id","Dijeda di tab Perisai"}, {"th","หยุดชั่วคราวที่แท็บป้องกัน"}, {"es","Pausado en pestaña Escudo"},
                {"pt","Pausado na aba Escudo"}, {"fr","En pause dans Bouclier"}, {"de","Im Tab Schutz pausiert"}, {"it","In pausa nella scheda Scudo"},
                {"pl","Wstrzymano w karcie Tarcza"}, {"hi","सुरक्षा टैब में रोका गया"}
            });
            AddExtra(cat, "btn_test_doh", new string[,] {
                {"en","Self-Check"}, {"vi","Tự kiểm tra"}, {"zh","一键自检"}, {"zh-TW","一鍵自檢"},
                {"ja","自己診断"}, {"ko","자가 진단"}, {"ru","Проверка"}, {"uk","Перевірка"},
                {"tr","Otomatik Test"}, {"id","Cek Sistem"}, {"th","ตรวจระบบ"}, {"es","Autotest"},
                {"pt","Autoteste"}, {"fr","Diagnostic"}, {"de","Selbsttest"}, {"it","Autotest"},
                {"pl","Autotest"}, {"hi","स्व-जांच"}
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
                {"id","✓ Cek OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"th","✓ ตรวจระบบ OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"es","✓ Autotest OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"pt","✓ Autoteste OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"fr","✓ Diagnostic OK : DoH {0} ({1}ms) • AdBlock {2}"},
                {"de","✓ Selbsttest OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"it","✓ Autotest OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"pl","✓ Autotest OK: DoH {0} ({1}ms) • AdBlock {2}"},
                {"hi","✓ जांच OK: DoH {0} ({1}ms) • AdBlock {2}"}
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
                {"id","URL Filter Kustom (hosts / domain):"},
                {"th","URL ตัวกรองกำหนดเอง (hosts / โดเมน):"},
                {"es","URL de filtro personalizado (hosts / dominios):"},
                {"pt","URL de filtro personalizado (hosts / domínios):"},
                {"fr","URL de filtre personnalisée (hosts / domaines) :"},
                {"de","Benutzerdefinierte Filter-URL (hosts / Domains):"},
                {"it","URL filtro personalizzato (hosts / domini):"},
                {"pl","Własny adres URL filtra (hosts / domeny):"},
                {"hi","कस्टम फ़िल्टर URL (hosts / डोमेन):"}
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
                {"id","Aktifkan Perisai otomatis saat NologGuard dibuka"},
                {"th","เปิดการป้องกันอัตโนมัติเมื่อเปิด NologGuard"},
                {"es","Activar Escudo automáticamente al abrir NologGuard"},
                {"pt","Ativar Proteção automaticamente ao iniciar o NologGuard"},
                {"fr","Activer automatiquement le Bouclier au lancement de NologGuard"},
                {"de","Schutz beim Start von NologGuard automatisch aktivieren"},
                {"it","Attiva automaticamente lo Scudo all'avvio di NologGuard"},
                {"pl","Automatycznie włączaj Tarczę po uruchomieniu NologGuard"},
                {"hi","NologGuard खुलने पर सुरक्षा स्वतः चालू करें"}
            });
            AddExtra(cat, "btn_check_update", new string[,] {
                {"en","Check for Updates"}, {"vi","Kiểm tra cập nhật"}, {"zh","检查更新"}, {"zh-TW","檢查更新"},
                {"ja","アップデート確認"}, {"ko","업데이트 확인"}, {"ru","Проверить обновления"}, {"uk","Перевірити оновлення"},
                {"tr","Güncelleme Kontrolü"}, {"id","Cek Pembaruan"}, {"th","ตรวจสอบอัปเดต"}, {"es","Buscar actualizaciones"},
                {"pt","Verificar atualizações"}, {"fr","Vérifier les mises à jour"}, {"de","Nach Updates suchen"}, {"it","Verifica aggiornamenti"},
                {"pl","Sprawdź aktualizacje"}, {"hi","अपडेट जांचें"}
            });
            AddExtra(cat, "upd_checking", new string[,] {
                {"en","Checking GitHub..."}, {"vi","Đang kiểm tra GitHub..."}, {"zh","正在检查 GitHub..."}, {"zh-TW","正在檢查 GitHub..."},
                {"ja","GitHub を確認中..."}, {"ko","GitHub 확인 중..."}, {"ru","Проверка GitHub..."}, {"uk","Перевірка GitHub..."},
                {"tr","GitHub kontrol ediliyor..."}, {"id","Memeriksa GitHub..."}, {"th","กำลังตรวจสอบ GitHub..."}, {"es","Comprobando GitHub..."},
                {"pt","Verificando GitHub..."}, {"fr","Vérification sur GitHub..."}, {"de","GitHub wird geprüft..."}, {"it","Controllo su GitHub..."},
                {"pl","Sprawdzanie GitHub..."}, {"hi","GitHub जांच रहे हैं..."}
            });
            AddExtra(cat, "upd_latest", new string[,] {
                {"en","Latest version (v{0})"}, {"vi","Đang dùng bản mới nhất (v{0})"}, {"zh","已是最新版 (v{0})"}, {"zh-TW","已是最新版 (v{0})"},
                {"ja","最新版を使用中 (v{0})"}, {"ko","최신 버전입니다 (v{0})"}, {"ru","Актуальная версия (v{0})"}, {"uk","Остання версія (v{0})"},
                {"tr","En güncel sürüm (v{0})"}, {"id","Versi terbaru (v{0})"}, {"th","เป็นเวอร์ชันล่าสุดแล้ว (v{0})"}, {"es","Última versión (v{0})"},
                {"pt","Versão mais recente (v{0})"}, {"fr","À jour (v{0})"}, {"de","Aktuellste Version (v{0})"}, {"it","Versione aggiornata (v{0})"},
                {"pl","Najnowsza wersja (v{0})"}, {"hi","नवीनतम संस्करण (v{0})"}
            });
            AddExtra(cat, "upd_available", new string[,] {
                {"en","New v{0} available! Opening..."}, {"vi","Có bản mới v{0}! Đang mở..."}, {"zh","发现新版本 v{0}！正在打开..."}, {"zh-TW","發現新版本 v{0}！正在開啟..."},
                {"ja","新バージョン v{0} あり！開いています..."}, {"ko","새 버전 v{0} 있음! 여는 중..."}, {"ru","Доступна v{0}! Открытие..."}, {"uk","Доступна v{0}! Відкриття..."},
                {"tr","Yeni v{0} mevcut! Açılıyor..."}, {"id","Versi baru v{0} tersedia! Membuka..."}, {"th","มีเวอร์ชันใหม่ v{0}! กำลังเปิด..."}, {"es","¡Nueva v{0} disponible! Abriendo..."},
                {"pt","Nova v{0} disponível! Abrindo..."}, {"fr","Nouvelle v{0} disponible ! Ouverture..."}, {"de","Neue v{0} verfügbar! Wird geöffnet..."}, {"it","Nuova v{0} disponibile! Apertura..."},
                {"pl","Nowa v{0} dostępna! Otwieranie..."}, {"hi","नया v{0} उपलब्ध! खुल रहा है..."}
            });
            AddExtra(cat, "upd_err", new string[,] {
                {"en","Update check failed: {0}"}, {"vi","Không kiểm tra được: {0}"}, {"zh","检查失败: {0}"}, {"zh-TW","檢查失敗: {0}"},
                {"ja","確認エラー: {0}"}, {"ko","확인 실패: {0}"}, {"ru","Ошибка проверки: {0}"}, {"uk","Помилка перевірки: {0}"},
                {"tr","Güncelleme hatası: {0}"}, {"id","Gagal memeriksa: {0}"}, {"th","ตรวจสอบไม่สำเร็จ: {0}"}, {"es","Error al comprobar: {0}"},
                {"pt","Falha ao verificar: {0}"}, {"fr","Échec de vérification : {0}"}, {"de","Prüfung fehlgeschlagen: {0}"}, {"it","Errore di verifica: {0}"},
                {"pl","Błąd sprawdzania: {0}"}, {"hi","जांच विफल: {0}"}
            });
            AddExtra(cat, "tray_pause_ab5", new string[,] {
                {"en","Pause AdBlock (5 min)"}, {"vi","Tạm dừng Chặn QC (5 phút)"}, {"zh","暂停去广告 (5 分钟)"}, {"zh-TW","暫停廣告封鎖 (5 分鐘)"},
                {"ja","広告ブロック一時停止 (5分)"}, {"ko","광고 차단 일시 중지 (5분)"}, {"ru","Пауза AdBlock (5 мин)"}, {"uk","Пауза AdBlock (5 хв)"},
                {"tr","AdBlock'u Duraklat (5 dk)"}, {"id","Jeda AdBlock (5 mnt)"}, {"th","หยุดพัก AdBlock (5 นาที)"}, {"es","Pausar AdBlock (5 min)"},
                {"pt","Pausar AdBlock (5 min)"}, {"fr","Mettre AdBlock en pause (5 min)"}, {"de","AdBlock pausieren (5 Min.)"}, {"it","Pausa AdBlock (5 min)"},
                {"pl","Wstrzymaj AdBlock (5 min)"}, {"hi","AdBlock रोकें (5 मिनट)"}
            });
            AddExtra(cat, "tray_pause_shield15", new string[,] {
                {"en","Pause Shield (15 min)"}, {"vi","Tạm dừng Bảo vệ (15 phút)"}, {"zh","暂停防护 (15 分钟)"}, {"zh-TW","暫停防護 (15 分鐘)"},
                {"ja","保護を一時停止 (15分)"}, {"ko","보호 일시 중지 (15분)"}, {"ru","Пауза защиты (15 мин)"}, {"uk","Пауза захисту (15 хв)"},
                {"tr","Kalkanı Duraklat (15 dk)"}, {"id","Jeda Perisai (15 mnt)"}, {"th","หยุดพักการป้องกัน (15 นาที)"}, {"es","Pausar Escudo (15 min)"},
                {"pt","Pausar Proteção (15 min)"}, {"fr","Mettre Bouclier en pause (15 min)"}, {"de","Schutz pausieren (15 Min.)"}, {"it","Pausa Scudo (15 min)"},
                {"pl","Wstrzymaj Tarczę (15 min)"}, {"hi","सुरक्षा रोकें (15 मिनट)"}
            });
            AddExtra(cat, "tray_resume", new string[,] {
                {"en","Resume Now"}, {"vi","Tiếp tục ngay"}, {"zh","立即恢复"}, {"zh-TW","立即恢復"},
                {"ja","今すぐ再開"}, {"ko","지금 재개"}, {"ru","Возобновить сейчас"}, {"uk","Відновити зараз"},
                {"tr","Şimdi Sürdür"}, {"id","Lanjutkan Sekarang"}, {"th","กลับมาทำงานทันที"}, {"es","Reanudar ahora"},
                {"pt","Retomar agora"}, {"fr","Reprendre maintenant"}, {"de","Jetzt fortsetzen"}, {"it","Riprendi ora"},
                {"pl","Wznów teraz"}, {"hi","अभी फिर शुरू करें"}
            });
            AddExtra(cat, "tray_paused_msg", new string[,] {
                {"en","Paused ({0}m left)"}, {"vi","Tạm dừng (còn {0}p)"}, {"zh","已暂停 (剩余 {0}分)"}, {"zh-TW","已暫停 (剩餘 {0}分)"},
                {"ja","一時停止中 (残り {0}分)"}, {"ko","일시 중지 (남은 {0}분)"}, {"ru","Пауза ({0} мин)"}, {"uk","Пауза ({0} хв)"},
                {"tr","Duraklatıldı ({0} dk)"}, {"id","Dijeda ({0} mnt lagi)"}, {"th","หยุดชั่วคราว ({0} นาที)"}, {"es","En pausa ({0} min)"},
                {"pt","Em pausa ({0} min)"}, {"fr","En pause ({0} min)"}, {"de","Pausiert ({0} Min.)"}, {"it","In pausa ({0} min)"},
                {"pl","Wstrzymano ({0} min)"}, {"hi","रोका गया ({0} मिनट)"}
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
