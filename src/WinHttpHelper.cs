using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NologGuard
{
    /// <summary>
    /// Shared zero-dependency HTTP/2 client built on Windows WinHTTP (NO_PROXY, TLS 1.2/1.3).
    /// </summary>
    public static class WinHttpHelper
    {
        public const int WinHttpAccessTypeNoProxy = 1;
        public const int WinHttpFlagSecure = 0x00800000;
        public const int WinHttpOptionSecureProtocols = 84;
        public const int WinHttpOptionEnableHttpProtocol = 133;
        public const int WinHttpProtocolFlagHttp2 = 0x1;
        public const int WinHttpFlagTls12 = 0x0800;
        public const int WinHttpFlagTls13 = 0x2000;
        public const uint WinHttpQueryStatusCode = 19;
        public const uint WinHttpQueryFlagNumber = 0x20000000;

        public static IntPtr CreateHttp2Session(string userAgent, int connectTimeoutMs, int receiveTimeoutMs)
        {
            IntPtr sess = WinHttpOpen(userAgent, WinHttpAccessTypeNoProxy, null, null, 0);
            if (sess == IntPtr.Zero)
            {
                throw new InvalidOperationException("WinHttpOpen failed: " + Marshal.GetLastWin32Error());
            }

            int h2 = WinHttpProtocolFlagHttp2;
            WinHttpSetOption(sess, WinHttpOptionEnableHttpProtocol, ref h2, 4);

            int tls = WinHttpFlagTls12 | WinHttpFlagTls13;
            if (!WinHttpSetOption(sess, WinHttpOptionSecureProtocols, ref tls, 4))
            {
                int tls12Only = WinHttpFlagTls12;
                WinHttpSetOption(sess, WinHttpOptionSecureProtocols, ref tls12Only, 4);
            }

            WinHttpSetTimeouts(sess, connectTimeoutMs, connectTimeoutMs, connectTimeoutMs, receiveTimeoutMs);
            return sess;
        }

        public static void CloseHandle(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
            {
                WinHttpCloseHandle(handle);
            }
        }

        public static byte[] ExecuteRequest(
            IntPtr sessionHandle,
            string method,
            string connectTarget,
            ushort port,
            string path,
            string extraHeaders,
            byte[] body,
            int maxResponseBytes,
            out int statusCode)
        {
            statusCode = 0;
            IntPtr conn = IntPtr.Zero;
            IntPtr req = IntPtr.Zero;

            try
            {
                conn = WinHttpConnect(sessionHandle, connectTarget, port, 0);
                if (conn == IntPtr.Zero)
                {
                    throw new IOException("WinHttpConnect failed: " + Marshal.GetLastWin32Error());
                }

                req = WinHttpOpenRequest(conn, method, path, null, null, IntPtr.Zero, WinHttpFlagSecure);
                if (req == IntPtr.Zero)
                {
                    throw new IOException("WinHttpOpenRequest failed: " + Marshal.GetLastWin32Error());
                }

                int h2 = WinHttpProtocolFlagHttp2;
                WinHttpSetOption(req, WinHttpOptionEnableHttpProtocol, ref h2, 4);

                string hdrs = string.IsNullOrEmpty(extraHeaders) ? null : extraHeaders;
                int hdrsLen = string.IsNullOrEmpty(hdrs) ? 0 : hdrs.Length;
                int bodyLen = (body != null) ? body.Length : 0;

                if (!WinHttpSendRequest(req, hdrs, hdrsLen, body, bodyLen, bodyLen, IntPtr.Zero) ||
                    !WinHttpReceiveResponse(req, IntPtr.Zero))
                {
                    throw new IOException("HTTP request failed (" + Marshal.GetLastWin32Error() + ")");
                }

                int status = 0;
                int len = 4;
                int idx = 0;
                WinHttpQueryHeaders(req, WinHttpQueryFlagNumber | WinHttpQueryStatusCode, null, out status, ref len, ref idx);
                statusCode = status;

                byte[] buf = new byte[16384];
                using (MemoryStream ms = new MemoryStream())
                {
                    int read;
                    while (WinHttpReadData(req, buf, buf.Length, out read) && read > 0)
                    {
                        if (ms.Length + read > maxResponseBytes)
                        {
                            throw new IOException("Response exceeds size limit.");
                        }
                        ms.Write(buf, 0, read);
                    }
                    return ms.ToArray();
                }
            }
            finally
            {
                if (req != IntPtr.Zero) WinHttpCloseHandle(req);
                if (conn != IntPtr.Zero) WinHttpCloseHandle(conn);
            }
        }

        public static Task<byte[]> GetBytesAsync(string url, int maxBytes, CancellationToken ct)
        {
            return Task.Run(delegate()
            {
                ct.ThrowIfCancellationRequested();
                Uri uri;
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri) ||
                    !string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Only HTTPS URLs are supported.");
                }

                IntPtr sess = IntPtr.Zero;
                try
                {
                    sess = CreateHttp2Session(AppConfig.UserAgent, 6000, 15000);
                    string path = string.IsNullOrEmpty(uri.PathAndQuery) ? "/" : uri.PathAndQuery;
                    int status;
                    byte[] data = ExecuteRequest(
                        sess,
                        "GET",
                        uri.Host,
                        (ushort)(uri.Port > 0 ? uri.Port : 443),
                        path,
                        "Accept: */*\r\n",
                        null,
                        maxBytes,
                        out status);

                    if (status != 200)
                    {
                        throw new IOException("HTTP " + status);
                    }
                    return data;
                }
                finally
                {
                    CloseHandle(sess);
                }
            }, ct);
        }

        [DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr WinHttpOpen(string agent, int accessType, string proxy, string bypass, int flags);

        [DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr WinHttpConnect(IntPtr session, string serverName, ushort port, int reserved);

        [DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr WinHttpOpenRequest(IntPtr connect, string verb, string objectName, string version, string referrer, IntPtr acceptTypes, int flags);

        [DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool WinHttpSetOption(IntPtr handle, int option, ref int buffer, int len);

        [DllImport("winhttp.dll", SetLastError = true)]
        private static extern bool WinHttpSetTimeouts(IntPtr handle, int resolveTimeout, int connectTimeout, int sendTimeout, int receiveTimeout);

        [DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool WinHttpSendRequest(IntPtr request, string headers, int headersLength, byte[] optional, int optionalLength, int totalLength, IntPtr context);

        [DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool WinHttpReceiveResponse(IntPtr request, IntPtr reserved);

        [DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool WinHttpReadData(IntPtr request, byte[] buffer, int bytesToRead, out int bytesRead);

        [DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool WinHttpQueryHeaders(IntPtr request, uint infoLevel, string name, out int buffer, ref int bufferLen, ref int index);

        [DllImport("winhttp.dll", SetLastError = true)]
        private static extern bool WinHttpCloseHandle(IntPtr handle);
    }

    public sealed class UpdateCheckResult
    {
        public bool Success { get; set; }
        public bool UpdateAvailable { get; set; }
        public string LatestVersion { get; set; }
        public string ReleaseUrl { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// User-triggered GitHub Releases update checker (never runs automatically in background).
    /// </summary>
    public static class UpdateChecker
    {
        public static async Task<UpdateCheckResult> CheckLatestAsync(string repoSlug, string currentVersion, CancellationToken ct)
        {
            UpdateCheckResult res = new UpdateCheckResult();
            try
            {
                string apiUrl = "https://api.github.com/repos/" + repoSlug.Trim('/') + "/releases/latest";
                byte[] raw = await WinHttpHelper.GetBytesAsync(apiUrl, 512 * 1024, ct).ConfigureAwait(false);
                string json = Encoding.UTF8.GetString(raw);

                string tag = ExtractJsonField(json, "tag_name");
                string htmlUrl = ExtractJsonField(json, "html_url");

                if (string.IsNullOrEmpty(tag))
                {
                    res.Success = false;
                    res.ErrorMessage = "No release tag found.";
                    return res;
                }

                res.Success = true;
                res.LatestVersion = tag;
                res.ReleaseUrl = string.IsNullOrEmpty(htmlUrl)
                    ? "https://github.com/" + repoSlug.Trim('/') + "/releases"
                    : htmlUrl;
                res.UpdateAvailable = IsNewerVersion(currentVersion, tag);
                return res;
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.ErrorMessage = ex.Message;
                return res;
            }
        }

        public static string ExtractJsonField(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return string.Empty;
            Match m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]+)\"");
            return m.Success ? m.Groups[1].Value.Replace("\\/", "/") : string.Empty;
        }

        public static bool IsNewerVersion(string currentVer, string latestTag)
        {
            Version cur = ParseSemVer(currentVer);
            Version lat = ParseSemVer(latestTag);
            if (cur == null || lat == null) return false;
            return lat > cur;
        }

        private static Version ParseSemVer(string ver)
        {
            if (string.IsNullOrEmpty(ver)) return null;
            string s = ver.Trim();
            if (s.StartsWith("v", StringComparison.OrdinalIgnoreCase)) s = s.Substring(1);
            int dash = s.IndexOf('-');
            if (dash > 0) s = s.Substring(0, dash);
            Version parsed;
            return Version.TryParse(s, out parsed) ? parsed : null;
        }
    }
}
