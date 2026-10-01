using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using static System.Net.Http.HttpCompletionOption;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FastDelete;

/// <summary>从 Gitee 检查更新并下载/应用新版本。</summary>
public static class Updater
{
    const string ApiReleases = "https://gitee.com/api/v5/repos/delphuy/FastDelete/releases";
    const string DownloadBase = "https://gitee.com/delphuy/FastDelete/releases/download/{tag}";

    /// 本地当前版本（由 MainForm 启动时注入）
    public static string LocalVersion { get; set; } = "1.0.0";

    /// 便携版判断：exe 不在 Program Files（即拷到任意位置的单文件）
    public static bool IsPortable { get; } = !Path.GetFullPath(AppContext.BaseDirectory)
        .StartsWith(@"C:\\Program Files", StringComparison.OrdinalIgnoreCase);

    /// <returns>(有更新?, 最新版本号, 最新下载直链)</returns>
    public static async Task<(bool hasUpdate, string latest, string url)> CheckLatestAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("FastDelete-Updater/1.0");
        string json = await http.GetStringAsync(ApiReleases);
        var list = JsonDocument.Parse(json);
        string bestTag = "";
        Version? bestVer = null;
        foreach (var rel in list.RootElement.EnumerateArray())
        {
            string tag = rel.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "";
            if (string.IsNullOrWhiteSpace(tag)) continue;
            string num = tag.TrimStart('v', 'V');
            if (!Version.TryParse(num, out var ver)) continue;
            if (bestVer == null || ver > bestVer) { bestVer = ver; bestTag = tag; }
        }
        if (bestVer == null) return (false, "", "");

        bool hasUpdate = bestVer > ParseVer(LocalVersion);
        string assetName = IsPortable
            ? $"FastDelete-{bestVer}-Windows-x64-Portable.exe"
            : $"FastDelete-{bestVer}-Windows-x64-Installer.exe";
        // 直接拼接（不用 string.Format，避免 {tag} 被当占位符解析）
        string url = DownloadBase.Replace("{tag}", Uri.EscapeDataString(bestTag)) + "/" + Uri.EscapeDataString(assetName);
        return (hasUpdate, bestVer.ToString(), url);
    }

    static Version ParseVer(string s)
    {
        if (string.IsNullOrEmpty(s)) return new Version(0,0,0,0);
        s = s.TrimStart('v', 'V');
        return Version.TryParse(s, out var v) ? v : new Version(0,0,0,0);
    }

    /// 下载 url 到 localPath，返回本地路径。onProgress 进度 0..1（-1 未知）。
    public static async Task<string> DownloadToAsync(string url, string localPath, Action<double>? onProgress = null)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("FastDelete-Updater/1.0");
        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
        using var resp = await http.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        long total = resp.Content.Headers.ContentLength ?? -1;
        await using var fs = File.Create(localPath);
        await using var stream = await resp.Content.ReadAsStreamAsync();
        var buf = new byte[8192];
        int read; long got = 0;
        while ((read = await stream.ReadAsync(buf)) > 0)
        {
            await fs.WriteAsync(buf, 0, read);
            got += read;
            onProgress?.Invoke(total > 0 ? (double)got / total : -1);
        }
        return localPath;
    }

    /// 应用更新。
    /// 便携版：下载新 exe，写 .bat 启动器完成「替换+重启+自删」，旧进程可退出。
    /// 安装版：下载 Inno 安装器并静默覆盖安装。
    /// 返回是否成功发起更新流程。
    public static async Task<bool> ApplyUpdateAsync(string url, Action<double>? onProgress = null, Action<string>? log = null)
    {
        try
        {
            string selfDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName)!;
            string tmp = Path.Combine(selfDir, "FastDelete_update.tmp");
            await DownloadToAsync(url, tmp, onProgress);
            log?.Invoke("已下载新版本");

            if (IsPortable)
            {
                // 写 .bat 启动器：旧 exe→.old、新 exe→旧名、启动新 exe、删 .old 与 .bat
                string oldExe = Path.Combine(selfDir, "FastDelete.exe");
                string bat = Path.Combine(selfDir, "FastDelete_update.bat");
                string[] lines =
                {
                    "@echo off",
                    "setlocal",
                    "set \"D=" + oldExe + "\"",
                    "set \"T=" + tmp + "\"",
                    "if not exist \"%T%\" exit /b 1",
                    "set \"O=%D%.old\"",
                    "move /Y \"%D%\" \"%O%\" >nul",
                    "move /Y \"%T%\" \"%D\"",
                    "if errorlevel 1 (",
                    "  if exist \"%O%\" move /Y \"%O%\" \"%D%\" >nul",
                    "  del \"%~dp0FastDelete_update.bat\" 2>nul",
                    "  exit /b 1",
                    ")",
                    "del \"%O%\" 2>nul",
                    "start \"\" \"%D\"",
                    "del \"%~dp0FastDelete_update.bat\"",
                    "exit /b 0"
                };
                File.WriteAllLines(bat, lines, new UTF8Encoding(false));
                Process.Start(new ProcessStartInfo { FileName = bat, UseShellExecute = false });
                log?.Invoke("便携版已更新，正在重启…");
                return true;
            }
            else
            {
                // 安装版：静默覆盖安装
                Process.Start(new ProcessStartInfo
                {
                    FileName = tmp,
                    Arguments = "/SILENT /SUPPRESSMSGBOXES /NORESTART /TASKS=rightclick",
                    UseShellExecute = true
                });
                try { File.Delete(tmp); } catch { }
                log?.Invoke("安装版已启动升级…");
                return true;
            }
        }
        catch (Exception ex)
        {
            log?.Invoke("更新失败：" + ex.Message);
            return false;
        }
    }
}