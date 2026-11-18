using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;

namespace CodexProxyLauncher.Windows
{
    internal static class CodexLocator
    {
        internal static string FindAutomatically()
        {
            var candidates = new List<string>();
            Add(candidates, SettingsStore.ApplicationPath);
            foreach (var name in new[] { "ChatGPT.exe", "Codex.exe" })
            {
                Add(candidates, ReadAppPath(Registry.CurrentUser, name));
                Add(candidates, ReadAppPath(Registry.LocalMachine, name));
            }

            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            foreach (var root in new[] { local, programFiles, programFilesX86 })
            {
                Add(candidates, Path.Combine(root, "Programs", "ChatGPT", "ChatGPT.exe"));
                Add(candidates, Path.Combine(root, "Programs", "Codex", "Codex.exe"));
                Add(candidates, Path.Combine(root, "ChatGPT", "ChatGPT.exe"));
                Add(candidates, Path.Combine(root, "Codex", "Codex.exe"));
                Add(candidates, Path.Combine(root, "OpenAI", "ChatGPT", "ChatGPT.exe"));
                Add(candidates, Path.Combine(root, "OpenAI", "Codex", "Codex.exe"));
            }

            foreach (var candidate in candidates)
                if (IsAllowedExecutable(candidate)) return Path.GetFullPath(candidate);
            return null;
        }

        internal static bool IsAllowedExecutable(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
            var name = Path.GetFileName(path);
            return name.Equals("ChatGPT.exe", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Codex.exe", StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadAppPath(RegistryKey hive, string executable)
        {
            try
            {
                using (var key = hive.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\" + executable))
                    return key?.GetValue(null) as string;
            }
            catch { return null; }
        }

        private static void Add(ICollection<string> values, string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) values.Add(value.Trim('"'));
        }
    }
}
