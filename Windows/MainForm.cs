using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CodexProxyLauncher.Windows
{
    internal sealed class MainForm : Form
    {
        private readonly TextBox proxyBox = new TextBox();
        private readonly TextBox appPathBox = new TextBox();
        private readonly Label statusLabel = new Label();
        private readonly Button launchButton = new Button();

        internal MainForm()
        {
            Text = "Codex 代理启动器";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(680, 390);
            MinimumSize = new Size(696, 429);
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(245, 247, 250);
            BuildInterface();
            Load += OnLoaded;
        }

        private void BuildInterface()
        {
            var title = new Label
            {
                Text = "Codex 代理启动器",
                Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                AutoSize = true,
                Location = new Point(34, 26)
            };
            var intro = new Label
            {
                Text = "为新启动的 ChatGPT / Codex 进程注入本机 HTTP 代理，不修改系统代理设置。",
                ForeColor = Color.FromArgb(75, 85, 99),
                AutoSize = true,
                Location = new Point(37, 70)
            };
            Controls.Add(title);
            Controls.Add(intro);

            Controls.Add(MakeLabel("HTTP 或混合代理地址", 38, 112));
            proxyBox.Location = new Point(40, 138);
            proxyBox.Size = new Size(430, 28);
            proxyBox.PlaceholderTextCompat("http://127.0.0.1:7897");
            Controls.Add(proxyBox);
            var testButton = MakeButton("检测代理", 486, 136, 142);
            testButton.Click += async (s, e) => await TestProxyAsync();
            Controls.Add(testButton);

            Controls.Add(MakeLabel("官方 ChatGPT / Codex 程序", 38, 188));
            appPathBox.Location = new Point(40, 214);
            appPathBox.Size = new Size(430, 28);
            appPathBox.ReadOnly = true;
            Controls.Add(appPathBox);
            var browseButton = MakeButton("选择程序", 486, 212, 142);
            browseButton.Click += (s, e) => ChooseApplication();
            Controls.Add(browseButton);

            statusLabel.Location = new Point(40, 264);
            statusLabel.Size = new Size(588, 42);
            statusLabel.ForeColor = Color.FromArgb(75, 85, 99);
            statusLabel.Text = "准备就绪";
            Controls.Add(statusLabel);

            var helpButton = MakeButton("查看使用流程图", 40, 320, 170);
            helpButton.Click += (s, e) => OpenGuide();
            Controls.Add(helpButton);

            launchButton.Text = "启动 Codex";
            launchButton.Location = new Point(432, 316);
            launchButton.Size = new Size(196, 42);
            launchButton.BackColor = Color.FromArgb(26, 140, 126);
            launchButton.ForeColor = Color.White;
            launchButton.FlatStyle = FlatStyle.Flat;
            launchButton.FlatAppearance.BorderSize = 0;
            launchButton.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            launchButton.Click += async (s, e) => await LaunchAsync();
            Controls.Add(launchButton);
        }

        private void OnLoaded(object sender, EventArgs e)
        {
            proxyBox.Text = SettingsStore.Proxy ?? ProxyConfiguration.ReadSystemProxyCandidate() ?? "http://127.0.0.1:7897";
            appPathBox.Text = CodexLocator.FindAutomatically() ?? string.Empty;
            statusLabel.Text = string.IsNullOrEmpty(appPathBox.Text)
                ? "未自动找到官方应用，请点击“选择程序”指定 ChatGPT.exe 或 Codex.exe。"
                : "已找到官方应用，可以检测代理后启动。";
        }

        private async Task TestProxyAsync()
        {
            string proxy;
            string error;
            if (!ProxyConfiguration.TryNormalize(proxyBox.Text, out proxy, out error))
            {
                ShowWarning(error);
                return;
            }
            SetBusy(true, "正在检测本机代理端口……");
            var reachable = await ProxyConfiguration.IsReachableAsync(proxy, 2500);
            SetBusy(false, reachable ? "代理端口可以连接。" : "代理端口无法连接，请确认代理软件已启动且端口正确。");
            statusLabel.ForeColor = reachable ? Color.FromArgb(20, 120, 90) : Color.FromArgb(180, 80, 45);
        }

        private void ChooseApplication()
        {
            using (var dialog = new OpenFileDialog
            {
                Title = "选择官方 ChatGPT.exe 或 Codex.exe",
                Filter = "官方应用 (ChatGPT.exe;Codex.exe)|ChatGPT.exe;Codex.exe|可执行文件 (*.exe)|*.exe",
                CheckFileExists = true,
                Multiselect = false
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                if (!CodexLocator.IsAllowedExecutable(dialog.FileName))
                {
                    ShowWarning("请选择官方应用目录中的 ChatGPT.exe 或 Codex.exe。不要选择快捷方式或其他程序。");
                    return;
                }
                appPathBox.Text = dialog.FileName;
                SettingsStore.ApplicationPath = dialog.FileName;
                statusLabel.Text = "已保存官方应用位置。";
            }
        }

        private async Task LaunchAsync()
        {
            string proxy;
            string error;
            if (!ProxyConfiguration.TryNormalize(proxyBox.Text, out proxy, out error))
            {
                ShowWarning(error);
                return;
            }
            if (!CodexLocator.IsAllowedExecutable(appPathBox.Text))
            {
                ShowWarning("尚未找到官方 ChatGPT / Codex 程序，请先点击“选择程序”。");
                return;
            }

            SetBusy(true, "正在检测代理并准备启动……");
            var reachable = await ProxyConfiguration.IsReachableAsync(proxy, 2500);
            if (!reachable)
            {
                SetBusy(false, "代理端口暂时无法连接。已取消启动。");
                var choice = MessageBox.Show(this,
                    "当前代理地址无法连接。请先启动代理软件或修正端口。\n\n仍要尝试启动吗？",
                    "代理不可连接", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (choice != DialogResult.Yes) return;
            }

            var running = Process.GetProcessesByName("ChatGPT")
                .Concat(Process.GetProcessesByName("Codex"))
                .Where(p => !p.HasExited).ToArray();
            if (running.Length > 0)
            {
                SetBusy(false, "检测到 ChatGPT / Codex 已在运行。新代理只对新进程生效。 ");
                var choice = MessageBox.Show(this,
                    "ChatGPT / Codex 已在运行。\n\n选择“是”：请求程序正常退出后重新启动。\n选择“否”：打开现有窗口，不改变它的代理。\n选择“取消”：返回启动器。",
                    "应用已在运行", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);
                if (choice == DialogResult.Cancel) return;
                if (choice == DialogResult.No)
                {
                    Activate(running.FirstOrDefault());
                    return;
                }
                foreach (var process in running)
                {
                    try { process.CloseMainWindow(); } catch { }
                }
                var deadline = DateTime.UtcNow.AddSeconds(15);
                while (DateTime.UtcNow < deadline && running.Any(p => IsRunning(p)))
                    await Task.Delay(250);
                if (running.Any(p => IsRunning(p)))
                {
                    ShowWarning("应用没有在 15 秒内退出。为保护正在执行的任务，启动器不会强制结束它。请手动退出后重试。");
                    return;
                }
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = appPathBox.Text,
                    WorkingDirectory = Path.GetDirectoryName(appPathBox.Text),
                    UseShellExecute = false
                };
                ProxyConfiguration.ApplyTo(startInfo, proxy);
                var process = Process.Start(startInfo);
                SettingsStore.Proxy = proxy;
                SettingsStore.ApplicationPath = appPathBox.Text;
                SetBusy(false, "已通过代理启动应用。以后请从本启动器进入 Codex。 ");
                await Task.Delay(2500);
                Activate(process);
            }
            catch (Exception exception)
            {
                SetBusy(false, "启动失败。 ");
                MessageBox.Show(this, exception.Message, "Codex 启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenGuide()
        {
            var guide = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Windows使用流程图.svg");
            if (!File.Exists(guide))
            {
                ShowWarning("没有找到使用流程图，请重新安装启动器。 ");
                return;
            }
            Process.Start(new ProcessStartInfo(guide) { UseShellExecute = true });
        }

        private static bool IsRunning(Process process)
        {
            try { return !process.HasExited; } catch { return false; }
        }

        private static void Activate(Process process)
        {
            if (process == null) return;
            try
            {
                process.Refresh();
                if (process.MainWindowHandle != IntPtr.Zero) NativeMethods.SetForegroundWindow(process.MainWindowHandle);
            }
            catch { }
        }

        private void SetBusy(bool busy, string status)
        {
            launchButton.Enabled = !busy;
            UseWaitCursor = busy;
            statusLabel.ForeColor = Color.FromArgb(75, 85, 99);
            statusLabel.Text = status;
        }

        private void ShowWarning(string message)
        {
            SetBusy(false, message);
            MessageBox.Show(this, message, "Codex 代理启动器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static Label MakeLabel(string text, int x, int y)
        {
            return new Label { Text = text, AutoSize = true, Location = new Point(x, y), ForeColor = Color.FromArgb(55, 65, 81) };
        }

        private static Button MakeButton(string text, int x, int y, int width)
        {
            return new Button { Text = text, Location = new Point(x, y), Size = new Size(width, 32), FlatStyle = FlatStyle.System };
        }
    }

    internal static class TextBoxCompatibility
    {
        internal static void PlaceholderTextCompat(this TextBox textBox, string placeholder)
        {
            textBox.AccessibleDescription = placeholder;
        }
    }
}
