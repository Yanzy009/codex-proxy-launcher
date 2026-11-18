using Microsoft.Win32;

namespace CodexProxyLauncher.Windows
{
    internal static class SettingsStore
    {
        private const string KeyPath = @"Software\Yanzy009\CodexProxyLauncher";

        internal static string Proxy
        {
            get { return Read("Proxy"); }
            set { Write("Proxy", value); }
        }

        internal static string ApplicationPath
        {
            get { return Read("ApplicationPath"); }
            set { Write("ApplicationPath", value); }
        }

        private static string Read(string name)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(KeyPath))
                    return key?.GetValue(name) as string;
            }
            catch { return null; }
        }

        private static void Write(string name, string value)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(KeyPath))
                key.SetValue(name, value ?? string.Empty, RegistryValueKind.String);
        }
    }
}
