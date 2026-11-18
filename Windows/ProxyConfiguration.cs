using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CodexProxyLauncher.Windows
{
    internal static class ProxyConfiguration
    {
        internal static bool TryNormalize(string raw, out string normalized, out string error)
        {
            normalized = null;
            error = null;
            Uri uri;
            var trimmed = (raw ?? string.Empty).Trim();
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(uri.Host) || !HasExplicitPort(trimmed) || uri.Port < 1 || uri.Port > 65535 ||
                !string.IsNullOrEmpty(uri.UserInfo) ||
                (uri.AbsolutePath != "/" && uri.AbsolutePath != string.Empty) ||
                !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            {
                error = "请填写 HTTP 代理地址，例如 http://127.0.0.1:7897。";
                return false;
            }

            var host = uri.HostNameType == UriHostNameType.IPv6
                ? "[" + uri.DnsSafeHost.Trim('[', ']') + "]"
                : uri.DnsSafeHost;
            normalized = Uri.UriSchemeHttp + "://" + host + ":" + uri.Port;
            return true;
        }

        private static bool HasExplicitPort(string raw)
        {
            var schemeEnd = raw.IndexOf("://", StringComparison.Ordinal);
            if (schemeEnd < 0) return false;
            var authorityStart = schemeEnd + 3;
            var authorityEnd = raw.IndexOfAny(new[] { '/', '?', '#' }, authorityStart);
            var authority = authorityEnd < 0
                ? raw.Substring(authorityStart)
                : raw.Substring(authorityStart, authorityEnd - authorityStart);
            if (authority.StartsWith("[", StringComparison.Ordinal))
            {
                var bracket = authority.IndexOf(']');
                return bracket > 0 && bracket + 1 < authority.Length && authority[bracket + 1] == ':';
            }
            return authority.LastIndexOf(':') > 0;
        }

        internal static async Task<bool> IsReachableAsync(string proxy, int timeoutMilliseconds)
        {
            string normalized;
            string error;
            if (!TryNormalize(proxy, out normalized, out error)) return false;
            var uri = new Uri(normalized);
            using (var client = new TcpClient())
            {
                var connect = client.ConnectAsync(uri.Host, uri.Port);
                var completed = await Task.WhenAny(connect, Task.Delay(timeoutMilliseconds));
                return completed == connect && !connect.IsFaulted && client.Connected;
            }
        }

        internal static void ApplyTo(ProcessStartInfo startInfo, string proxy)
        {
            foreach (var key in new[] { "ALL_PROXY", "all_proxy", "HTTP_PROXY", "http_proxy", "HTTPS_PROXY", "https_proxy", "NO_PROXY", "no_proxy" })
            {
                startInfo.EnvironmentVariables.Remove(key);
            }
            startInfo.EnvironmentVariables["HTTP_PROXY"] = proxy;
            startInfo.EnvironmentVariables["HTTPS_PROXY"] = proxy;
            startInfo.EnvironmentVariables["NO_PROXY"] = "127.0.0.1,localhost,::1";
        }

        internal static string ReadSystemProxyCandidate()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings"))
                {
                    if (!Equals(key?.GetValue("ProxyEnable"), 1)) return null;
                    var raw = key.GetValue("ProxyServer") as string;
                    if (string.IsNullOrWhiteSpace(raw)) return null;
                    if (raw.Contains(";"))
                    {
                        foreach (var item in raw.Split(';'))
                        {
                            var parts = item.Split(new[] { '=' }, 2);
                            if (parts.Length == 2 && (parts[0].Equals("https", StringComparison.OrdinalIgnoreCase) || parts[0].Equals("http", StringComparison.OrdinalIgnoreCase)))
                            {
                                raw = parts[1];
                                break;
                            }
                        }
                    }
                    if (!raw.Contains("://")) raw = "http://" + raw;
                    string normalized;
                    string error;
                    return TryNormalize(raw, out normalized, out error) ? normalized : null;
                }
            }
            catch { return null; }
        }
    }
}
