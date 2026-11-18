using System;
using System.Diagnostics;

namespace CodexProxyLauncher.Windows
{
    internal static class TestProgram
    {
        private static int failures;

        private static void Main()
        {
            Accepts("http://127.0.0.1:7897", "http://127.0.0.1:7897");
            Accepts(" http://localhost:10809/ ", "http://localhost:10809");
            Accepts("http://127.0.0.1:80", "http://127.0.0.1:80");
            Rejects("https://127.0.0.1:7897");
            Rejects("http://127.0.0.1");
            Rejects("http://user:password@127.0.0.1:7897");
            Rejects("http://127.0.0.1:7897/path");

            var startInfo = new ProcessStartInfo();
            startInfo.EnvironmentVariables["ALL_PROXY"] = "socks5://127.0.0.1:1080";
            ProxyConfiguration.ApplyTo(startInfo, "http://127.0.0.1:7897");
            Check(!startInfo.EnvironmentVariables.ContainsKey("ALL_PROXY"), "ALL_PROXY should be removed");
            Check(startInfo.EnvironmentVariables["HTTP_PROXY"] == "http://127.0.0.1:7897", "HTTP_PROXY should be set");
            Check(startInfo.EnvironmentVariables["HTTPS_PROXY"] == "http://127.0.0.1:7897", "HTTPS_PROXY should be set");
            Check(startInfo.EnvironmentVariables["NO_PROXY"].Contains("localhost"), "NO_PROXY should protect localhost");

            if (failures > 0) Environment.Exit(1);
            Console.WriteLine("Windows launcher core tests passed.");
        }

        private static void Accepts(string raw, string expected)
        {
            string normalized;
            string error;
            Check(ProxyConfiguration.TryNormalize(raw, out normalized, out error), raw + " should be valid");
            Check(normalized == expected, raw + " normalized unexpectedly: " + normalized);
        }

        private static void Rejects(string raw)
        {
            string normalized;
            string error;
            Check(!ProxyConfiguration.TryNormalize(raw, out normalized, out error), raw + " should be rejected");
        }

        private static void Check(bool condition, string message)
        {
            if (condition) return;
            failures++;
            Console.Error.WriteLine("FAILED: " + message);
        }
    }
}
