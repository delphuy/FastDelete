using System;
using System.IO;
using System.Windows.Forms;

namespace FastDelete;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (!EnsureDotNetRuntime())
        {
            MessageBox.Show(
                "FastDelete 需要 .NET 8 桌面运行时才能运行。\n\n" +
                "请前往下载并安装 .NET 8 Desktop Runtime：\n" +
                "https://dotnet.microsoft.com/download/dotnet/8.0\n" +
                "（选择「.NET 8 桌面运行时」，win-x64 即可）\n\n" +
                "安装完成后重新运行本程序。",
                "缺少运行时", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ApplicationConfiguration.Initialize();

        string? addDir = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("add", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                addDir = args[i + 1];
                break;
            }
        }

        if (addDir is not null)
        {
            if (Directory.Exists(addDir))
                Application.Run(new MainForm(autoAddDirectory: addDir));
            else
                MessageBox.Show("目录不存在或无法访问：" + addDir, "FastDelete",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        else
        {
            Application.Run(new MainForm());
        }
    }

    /// <summary>检查 .NET 桌面运行时是否存在（框架依赖版才需要；自包含版查到就放行）。</summary>
    static bool EnsureDotNetRuntime()
    {
        try
        {
            int major = 0;
            string tfm = AppContext.TargetFrameworkName;   // e.g. ".NETCoreApp,Version=v8.0"
            var m = System.Text.RegularExpressions.Regex.Match(tfm, "v(\\d+)\\.0");
            if (m.Success) int.TryParse(m.Groups[1].Value, out major);
            if (major < 1) major = 8;

            string sharedRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "dotnet", "shared", "Microsoft.WindowsDesktop.App");
            if (Directory.Exists(sharedRoot))
                foreach (var dir in Directory.GetDirectories(sharedRoot))
                    if (System.Version.TryParse(Path.GetFileName(dir), out var v) && v.Major >= major) return true;

            var dr = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            if (!string.IsNullOrEmpty(dr))
            {
                var p = Path.Combine(dr, "shared", "Microsoft.WindowsDesktop.App");
                if (Directory.Exists(p))
                    foreach (var dir in Directory.GetDirectories(p))
                        if (System.Version.TryParse(Path.GetFileName(dir), out var v) && v.Major >= major) return true;
            }
            return false;
        }
        catch { return true; } // 查不到就放行
    }
}
