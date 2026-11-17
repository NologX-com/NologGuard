# NologGuard (Hướng dẫn Tiếng Việt)

**Tạo bởi [NologX.com](https://nologx.com)** • [🌐 Trang Giới Thiệu (Landing Page)](https://nologx-com.github.io/NologGuard/) • [🇬🇧 English README](README.md) • [🔒 Chính sách quyền riêng tư](PRIVACY.md)

**Công cụ 1-nút-bấm bảo vệ quyền riêng tư khi lướt web trên Windows** — Tích hợp **mã hóa DNS (DoH HTTP/2)**, **cắt nhỏ gói tin TLS SNI để giấu tên miền truy cập khỏi hệ thống lọc ISP**, **chặn/bỏ qua QUIC**, **chặn quảng cáo & tracker**, và **danh sách ngoại lệ** trong một ứng dụng gọn nhẹ (~260 KB), giữ nguyên IP thật và 100% tốc độ mạng gốc.

---

## Cơ chế giấu web truy cập với ISP (Nhà mạng) & Lưu ý trung thực

Bình thường khi bạn mở một trang web `https://`, dù nội dung bên trong đã mã hóa, nhà mạng (ISP) vẫn có thể biết bạn đang truy cập **tên miền nào** qua 3 khe hở:
1. **Truy vấn DNS không mã hóa (UDP cổng 53):** Máy tính gửi câu hỏi `"IP của example.com là gì?"` bằng chữ thường qua đường mạng ISP.
2. **Trường SNI (Server Name Indication) trong gói TLS đầu tiên:** Ngay ở gói kết nối `TLS ClientHello`, trình duyệt phải gửi kèm tên miền đích (`example.com`) dưới dạng chữ thường. Hệ thống kiểm duyệt/ghi log thụ động (**Passive/Stateless DPI**) của nhà mạng đọc gói đầu tiên này để nhận diện hoặc chặn trang web.
3. **Giao thức QUIC (UDP cổng 443):** Chrome/Edge ưu tiên dùng QUIC trên UDP 443, mang theo tên miền dạng đọc được và né qua các proxy TCP thông thường.

### NologGuard xử lý 3 điểm trên như thế nào?
* **Giấu hoàn toàn truy vấn DNS khỏi ISP (Bootstrapped HTTP/2 DoH):** Kết nối thẳng bằng IP gốc của máy chủ DoH (`9.9.9.9` Quad9, `1.1.1.1` Cloudflare, `8.8.8.8` Google, `94.140.14.14` AdGuard) qua HTTP/2 (`WinHTTP NO_PROXY`) — ngay cả bước tìm máy chủ DNS cũng **không rò ra UDP cổng 53** của nhà mạng.
* **Ẩn/làm mờ tên miền HTTPS trước DPI nhà mạng (Cắt nhỏ TLS SNI):** Đọc trọn vẹn gói `ClientHello` (kể cả gói lớn >1.700 byte TLS 1.3 hậu lượng tử `X25519MLKEM768`), xác định chính xác vị trí tên miền SNI và **cắt đôi/chẻ nhỏ tên miền đó qua nhiều gói TCP** (`TCP_NODELAY` + độ trễ ms tùy chỉnh). Các hệ thống DPI thụ động của nhà mạng sẽ không ráp được tên miền để đọc hay chặn.
* **Chặn rò rỉ QUIC:** Khi trình duyệt đi qua Local HTTP Proxy của NologGuard, Chrome/Edge/Firefox tự động chuyển về TCP HTTPS (áp dụng cắt SNI). Ngoài ra có tùy chọn quyền Admin để dựng rule Firewall chặn UDP 443 trên toàn hệ thống.

### Bảng so sánh trung thực phạm vi bảo vệ

| Lớp dữ liệu | Không dùng tool | Dùng NologGuard | Dùng VPN / VPS riêng |
| :--- | :--- | :--- | :--- |
| **Truy vấn DNS (Tên miền)** | Nhà mạng đọc được (UDP 53) | **Ẩn hoàn toàn** (DoH HTTP/2) | **Ẩn hoàn toàn** |
| **Tên miền HTTPS (SNI trước DPI thụ động)** | Hiện chữ thường ở gói 1 | **Bị cắt nhỏ qua nhiều gói TCP** (vượt lọc DPI thụ động ISP) | **Ẩn hoàn toàn** |
| **QUIC (UDP 443)** | Hiện tên miền ở gói UDP đầu | **Trình duyệt tự bỏ QUIC khi qua Proxy** (+ tùy chọn Firewall) | **Ẩn hoàn toàn** |
| **Quảng cáo / Tracker bên thứ 3** | Tải & theo dõi bình thường | **Chặn trước khi kết nối (`403`)** (Có sẵn + hostsVN / HaGeZi / OISD / StevenBlack / Custom URL) | Tùy VPN |
| **Quảng cáo cùng tên miền (vd YouTube)** | Hiện quảng cáo | **Chưa chặn được** (nên dùng kèm uBlock Origin trên trình duyệt) | Không chặn |
| **Địa chỉ IP máy chủ đích** | Nhà mạng thấy | **Vẫn thấy IP đích** (web qua CDN chung như Cloudflare sẽ khó phân biệt; web dùng IP riêng vẫn tra được IP) | **Ẩn hoàn toàn** (chỉ thấy IP VPN/VPS) |
| **Phân tích ghép luồng TCP chuyên sâu ngoại tuyến** | Đọc được | **Có thể ghép lại** nếu nhà mạng lưu và dựng lại toàn bộ phiên TCP | **Ẩn hoàn toàn** |
| **Tốc độ mạng & IP thật** | Nguyên gốc | **Giữ 100% tốc độ gốc & IP Việt Nam thật** (không dính CAPTCHA VPN) | Chậm hơn, đổi IP |

> **Tóm tắt:** Dùng **NologGuard** khi bạn muốn mã hóa DNS, giấu tên miền khỏi hệ thống lọc DPI/chặn web của ISP, chặn quảng cáo mức tên miền mà vẫn giữ **IP thật + tốc độ tối đa**. Nếu bạn cần giấu **tuyệt đối cả địa chỉ IP đích**, hãy kết hợp dùng thêm **VPN / VPS riêng**.

---

## Tất cả chức năng của NologGuard (`v1.2`)

1. **Khiên bảo vệ 1-nút-bấm & Tự bảo vệ khi khởi chạy (`AutoProtectOnLaunch`):** Tự mở Local Proxy (`127.0.0.1:8899`), gán Windows System Proxy (`HKCU`) và làm mới WinINet tức thì mà không cần tắt mở lại trình duyệt.
2. **DoH HTTP/2 + Nút Tự kiểm tra (`Self-Check`):** Hỗ trợ sẵn Quad9 Security (`9.9.9.9`), Quad9 ECS (`9.9.9.11`), Cloudflare (`1.1.1.1`), Google (`8.8.8.8`), AdGuard (`94.140.14.14`) hoặc URL DoH tùy chỉnh. Bấm **Tự kiểm tra** để đo độ trễ DoH thực tế và gửi kết nối Loopback kiểm chứng AdBlock trả đúng mã **`403`**.
3. **3 chế độ cắt gói SNI:**
   - **Cắt giữa tên miền SNI (Khuyên dùng):** Tách 2 byte đầu TLS header rồi cắt đôi ngay chính giữa tên miền SNI.
   - **Cắt nhiều mảnh nhỏ (Fixed Chunks):** Chẻ nhỏ vùng TLS header + tên miền SNI theo kích thước byte tùy chọn.
   - **Tắt cắt SNI (Chỉ dùng DoH):** Chỉ mã hóa DNS + AdBlock mà không cắt gói TCP.
4. **Chặn quảng cáo & Anti-Tracking (Không giải mã HTTPS MitM):** Chặn tên miền quảng cáo/analytics/telemetry ngay tại Proxy trước khi phân giải DNS. Hỗ trợ bộ lọc offline **Built-in Starter** (chạy ngay khi mới cài), **hostsVN (Việt Nam)**, **HaGeZi Multi LIGHT/PRO**, **OISD Small**, **StevenBlack Hosts**, **Link bộ lọc tùy chỉnh (`Custom Filter URL`)**, và quy tắc tự nhập (`tenmien.com` để chặn, `@@tenmien.com` để bỏ chặn).
5. **Danh sách Ngoại lệ (`Whitelist`) thông minh:** Hỗ trợ tên miền chính xác, wildcard `*.tenmien.com`, ghi chú `#`. Tự động bỏ qua mạng nội bộ `localhost`, `127.0.0.1`, LAN và Tailscale (`*.ts.net`, `100.64.0.0/10`). Miền trong Whitelist cũng vượt qua AdBlock.
6. **2 tùy chọn toàn hệ thống (Cần quyền Admin nếu bật):**
   - **Bật DoH toàn Windows 11:** Cấu hình DoH gốc trực tiếp trên card mạng vật lý + `HKLM\...\Dnscache` cho cả app ngoài trình duyệt; tự sao lưu DNS cũ và hoàn nguyên khi tắt.
   - **Chặn QUIC UDP 443 qua Firewall:** Dựng rule `NologGuard_Block_QUIC_UDP_443` khi khiên bật và tự xóa sạch khi tắt.
7. **Khay hệ thống (System Tray) tối ưu RAM & Tạm dừng nhanh:**
   - **Tự thu gọn RAM:** Khi bấm `✕` thu xuống khay hệ thống, NologGuard tạm dừng timer UI, dọn rác GC và gọi `SetProcessWorkingSetSize` + `EmptyWorkingSet` giúp RAM Working Set chạy nền giảm xuống mức rất thấp (~1–16 MB).
   - **Tạm dừng nhanh:** Chuột phải icon khay hệ thống để chọn **Tạm dừng Chặn QC (5 phút)** hoặc **Tạm dừng Bảo vệ (15 phút)** với hẹn giờ tự bật lại, hoặc bấm **Tiếp tục ngay**.
8. **Không lưu vết ra ổ cứng, 32 ngôn ngữ & Công cụ dòng lệnh:**
   - **Nhật ký** chỉ giữ tạm 300 dòng gần nhất trong **RAM**, tắt app là xóa sạch, tuyệt đối không ghi lịch sử duyệt web xuống ổ đĩa.
   - Hỗ trợ **32 ngôn ngữ** (tự nhận diện ngôn ngữ Windows, tự động đổi bố cục phải-sang-trái RTL cho tiếng Ả Rập/Ba Tư/Do Thái: `Tiếng Việt`, `English`, `简体中文`, `繁體中文`, `日本語`, `한국어`, `Русский`, `Українська`, `Türkçe`, `العربية`, `فارسی`, `עברית`, `Bahasa Indonesia`, `Bahasa Melayu`, `ภาษาไทย`, `বাংলা`, `हिन्दी`, `Español`, `Português`, `Français`, `Deutsch`, `Italiano`, `Nederlands`, `Polski`, `Čeština`, `Română`, `Magyar`, `Ελληνικά`, `Svenska`, `Norsk`, `Dansk`, `Suomi`), kiểm tra cập nhật GitHub thủ công trong Cài đặt, và các cờ CLI `--minimized`, `--recover`, `--exit`.

---

## Hướng dẫn sử dụng chi tiết

### 1. Cài đặt hoặc chạy ngay
* **Bản cài đặt (`NologGuard-Setup.exe`):** Tải từ [GitHub Releases](https://github.com/NologX-com/NologGuard/releases), mở cài đặt bình thường (hoặc cài ngầm với `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART`).
* **Bản Portable (`NologGuard.exe`):** Chỉ 1 file ~260 KB chạy thẳng trên Windows 10/11, không cần cài driver hay phần mềm phụ trợ.

### 2. Bật bảo vệ hằng ngày (Chỉ 3 bước, KHÔNG cần quyền Admin)
1. Mở **NologGuard** (mặc định chạy quyền `USER`, an toàn và không xung đột game/anti-cheat).
2. Bấm nút xanh **BẬT BẢO VỆ** (chuyển sang **ĐANG BẢO VỆ** màu xanh lá — mọi trình duyệt như Chrome, Edge, Firefox đã tự dùng DoH + cắt SNI + AdBlock).
3. Bấm nút **Tự kiểm tra** cạnh ô chọn máy chủ DoH để xác nhận kết nối DoH thành công và `AdBlock 403 Loopback OK`.

### 3. Hướng dẫn từng Tab trên giao diện
* **Bảo vệ (`Shield`):** Bật/tắt khiên, đổi máy chủ DoH (mặc định Quad9), chọn kiểu cắt SNI, bật/tắt nhanh Proxy hệ thống & Chặn quảng cáo, theo dõi bộ đếm (`Đã cắt SNI`, `QC/Tracker chặn`, `DoH Cache`, `Ngoại lệ`).
* **Chặn QC (`AdBlock`):** Chọn bộ lọc (**Built-in Starter** hoạt động luôn không cần tải; muốn chặn mạnh hơn hoặc chặn quảng cáo Việt Nam thì chọn **hostsVN** hoặc **HaGeZi Multi LIGHT** rồi bấm **Tải / Cập nhật**; hoặc chọn **Custom Filter URL** để dán link list riêng). Gõ tên miền cần chặn/gỡ chặn (thêm `@@` phía trước để cho phép) vào ô thêm nhanh.
* **Ngoại lệ (`Whitelist`):** Thêm trang ngân hàng, cổng dịch vụ công hoặc tên miền nội bộ (`*.tenmien.com`) nếu muốn bỏ qua cắt SNI và bỏ qua chặn quảng cáo.
* **Cài đặt (`Settings`):** Chỉnh cổng Local Proxy (`8899`), kích thước mảnh (`2` byte), độ trễ (`2` ms), URL DoH riêng, ngôn ngữ giao diện, bật **Tự động bật Bảo vệ ngay khi mở NologGuard**, kiểm tra cập nhật GitHub hoặc bấm nút đỏ **Khôi phục khẩn cấp** khi cần reset mạng.
* **Nhật ký (`Live Log`):** Quan sát hoạt động proxy/DoH theo thời gian thực (chỉ trên RAM).

### 4. Khi nào cần bấm "Chạy quyền Admin"?
* Chỉ cần bấm **Chạy quyền Admin** (hoặc bật 2 công tắc cuối ở Dashboard -> đồng ý UAC) khi bạn muốn áp dụng **DoH toàn Windows 11** cho cả các phần mềm ngoài trình duyệt hoặc muốn **Firewall chặn hẳn UDP 443 (QUIC)**. Khi thành công, huy hiệu trên đầu cửa sổ sẽ đổi từ `USER` sang **`ADMIN` màu xanh lá**.

### 5. Khắc phục sự cố mất điện / kẹt Proxy
* Nếu máy tắt đột ngột khi đang bật bảo vệ khiến trình duyệt báo không kết nối được Proxy: chỉ cần **mở lại NologGuard** (tự phục hồi khi khởi động), hoặc chạy lệnh `NologGuard.exe --recover` (có sẵn shortcut **Reset Proxy & Firewall (Recovery)** trong Start Menu nếu cài bằng bộ cài).

---

## Tự biên dịch từ mã nguồn

Windows 10/11 đã có sẵn trình biên dịch `.NET Framework` (`csc.exe`):

```bat
build.bat
```

Chạy kèm kiểm thử trực tiếp Quad9 DoH & HTTPS:

```bat
build.bat --live
```

Tài liệu đóng gói & phát hành lên GitHub / SignPath / Microsoft Store: [installer/NologGuard.iss](installer/NologGuard.iss) • [docs/PUBLISHING.md](docs/PUBLISHING.md)

---

## Giấy phép & Tác giả

MIT License — xem [LICENSE](LICENSE).  
Tạo bởi **[NologX.com](https://nologx.com)** — [https://nologx.com](https://nologx.com)
