# Lộ trình phát hành NologGuard (GitHub + SignPath + Microsoft Store)

Tài liệu này hướng dẫn từng bước đưa **NologGuard** bản đầy đủ (có cả Admin Win11 DoH + Firewall QUIC) lên **GitHub Releases**, ký số miễn phí qua **SignPath Foundation**, và nộp lên **Microsoft Store (gói Win32 EXE)**.

---

## Bước 1: Đưa mã nguồn lên GitHub (Công khai, MIT License)

1. Repository chính thức trên GitHub: **`NologX-com/NologGuard`**.
2. Nếu bạn đổi sang tổ chức hoặc tên repo khác, nhớ cập nhật hằng số `GitHubRepo` trong [src/AppConfig.cs](src/AppConfig.cs) và link URL trong [installer/NologGuard.iss](installer/NologGuard.iss).
3. Khởi tạo Git và đẩy mã nguồn từ thư mục `NologGuard`:
   ```bat
   git init -b main
   git add .
   git commit -m "Release NologGuard v1.2.0"
   git remote add origin https://github.com/NologX-com/NologGuard.git
   git push -u origin main
   ```
4. Tạo tag để GitHub Actions tự build file `.exe` và bộ cài `NologGuard-Setup.exe`:
   ```bat
   git tag v1.2.0
   git push origin v1.2.0
   ```
   > Vào tab **Actions** trên GitHub, bạn sẽ thấy workflow `.github/workflows/ci.yml` chạy test (`18/18 PASS`) và xuất ra cả `NologGuard.exe` (portable) lẫn `NologGuard-Setup.exe` (Inno Setup).

---

## Bước 2: Xin ký số miễn phí qua SignPath Foundation (Chống SmartScreen + Chuẩn Store)

1. Vào **[signpath.org](https://signpath.org)** -> đăng ký chương trình **Free Code Signing for Open Source** (yêu cầu repo công khai, giấy phép MIT có sẵn trong `LICENSE`, `README.md`, build từ GitHub Actions).
2. Sau khi được duyệt và tạo Project + Signing Policy trên SignPath (ví dụ `project-slug: nologguard`, `signing-policy-slug: release-signing`), vào GitHub Repo -> **Settings > Secrets and variables > Actions**:
   - **Secrets** (thêm mới):
     - `SIGNPATH_API_TOKEN`
     - `SIGNPATH_ORGANIZATION_ID`
   - **Variables** (thêm mới):
     - `ENABLE_SIGNPATH` = `true`
     - *(Tùy chọn nếu đặt tên slug khác)* `SIGNPATH_PROJECT_SLUG`, `SIGNPATH_POLICY_SLUG`
3. Đẩy tag mới (`git tag v1.2.1 && git push origin v1.2.1`):
   - Workflow tự động ký **2 lớp**: ký `NologGuard.exe` trước -> nhúng vào `NologGuard-Setup.exe` -> ký tiếp `NologGuard-Setup.exe` -> đính kèm lên GitHub Releases kèm file `.sha256`.
4. *(Khuyên làm thêm)* Nộp file đã ký lên **[Microsoft Security Intelligence (WDSI)](https://www.microsoft.com/en-us/wdsi/filesubmission)** chọn mục **Software Developer** để SmartScreen & Defender xác nhận sạch nhanh hơn.

---

## Bước 3: Nộp bản đầy đủ lên Microsoft Store (Win32 Traditional EXE)

1. Đăng ký tài khoản **Individual developer (miễn phí 0đ)** tại **[storedeveloper.microsoft.com](https://storedeveloper.microsoft.com)** (xác minh CCCD/hộ chiếu + selfie).
2. Trong **Partner Center**, tạo App mới -> đặt tên **NologGuard**.
3. Tạo Submission mới và điền phần **Packages**:
   - **Installer type:** `EXE`
   - **Package URL:** Dán link trực tiếp tới bản **đã ký số** trên GitHub Releases, ví dụ:
     `https://github.com/NologX-com/NologGuard/releases/download/v1.2.1/NologGuard-Setup.exe`
   - **Installer parameters (Silent install switch):**
     `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART`
   - **Supported architectures:** `x64`
   - **Supported languages:** Chọn các ngôn ngữ bạn muốn hiển thị trên Store (app đã hỗ trợ sẵn 32 ngôn ngữ).
4. Trong mục **Properties / Store listings**:
   - **Privacy policy URL:** Gắn link trang chính sách riêng tư trên `NologX.com` hoặc link `https://github.com/NologX-com/NologGuard/blob/main/PRIVACY.md`.
5. Trong **Notes for certification** (rất quan trọng để qua duyệt nhanh):
   > *NologGuard is an open-source privacy utility providing a local DNS-over-HTTPS (DoH) HTTP/2 proxy (`127.0.0.1:8899`), TCP TLS SNI splitting, and domain-level ad/tracker blocking without HTTPS MitM decryption. All network changes (HKCU User Proxy, and optional Administrator-only Win11 adapter DNS / outbound UDP 443 QUIC firewall rule) are only applied on explicit user toggle, backed up in `%APPDATA%\NologGuard\backup_state.json`, and cleanly restored on disable, exit, `--recover`, or uninstall.*
   > *Silent install switch: `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART`.*
   > *Silent uninstall runs `unins000.exe /VERYSILENT /NORESTART` and automatically executes `NologGuard.exe --exit` and `--recover`.*
