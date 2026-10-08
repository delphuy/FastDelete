using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.Http.HttpCompletionOption;

namespace FastDelete;

/// <summary>从 Gitee 检查更新并下载/应用新版本（含完整日志，写 %LocalAppData%\\FastDelete\\update.log）。</summary>
public static class Updater
{
    const string ApiReleases = "https://gitee.com/api/v5/repos/delphuy/FastDelete/releases";
    const string DownloadBase = "https://gitee.com/delphuy/FastDelete/releases/download/{tag}";

    /// <summary>本地当前版本（由 MainForm 启动时注入）</summary>
    public static string LocalVersion { get; set; } = "1.0.0";

    /// <summary>便携版判断：exe 不在 Program Files（即拷到任意位置的单文件）</summary>
public static bool IsPortable
    {
        get
        {
            // .NET 单文件 publish 的 AppContext.BaseDirectory 是临时解压目录，不可靠。
            // 改用 exe 真实路径判断：C:\Program Files 下 = 安装版，其他 = 便携版。
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule?.FileName
                    ?? Path.Combine(AppContext.BaseDirectory, "FastDelete.exe");
                exePath = Path.GetFullPath(exePath);
                return !exePath.StartsWith(@"C:\Program Files", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return true; // 探测失败当便携版处理
            }
        }
    }

    // ── 日志（写文件 + 内存，升级失败时看此文件定位断点）──
    static readonly List<string> LogLines = new();
    static string LogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FastDelete", "update.log");

    public static void UpdateLog(string msg)
    {
        var line = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + msg;
        LogLines.Add(line);
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.AppendAllText(LogPath, line + Environment.NewLine, System.Text.Encoding.UTF8);
        }
        catch { /* 日志写失败不影响升级流程 */ }
    }

    public static string GetLastLog() => LogLines.Count > 0
        ? string.Join(Environment.NewLine, LogLines) : "(无日志)";

    /// <summary>检查更新，返回 (有更新?, 最新版本号, 下载直链)。</summary>
    public static async Task<(bool hasUpdate, string latest, string url)> CheckLatestAsync()
    {
        UpdateLog("CheckLatest: 开始, 本地版本=" + LocalVersion + ", IsPortable=" + IsPortable);
        try
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
            if (bestVer == null)
            {
                UpdateLog("CheckLatest: Gitee 无有效 release, 返回(无更新)");
                return (false, "", "");
            }
            bool hasUpdate = bestVer > ParseVer(LocalVersion);
            string assetName = IsPortable
                ? $"FastDelete-{bestVer}-Windows-x64-Portable.exe"
                : $"FastDelete-{bestVer}-Windows-x64-Installer.exe";
            string url = DownloadBase.Replace("{tag}", Uri.EscapeDataString(bestTag)) + "/" + Uri.EscapeDataString(assetName);
            UpdateLog($"CheckLatest: Gitee 最新={bestTag}, 本地={LocalVersion}, 有更新={hasUpdate}, url={url}");
            return (hasUpdate, bestVer.ToString(), url);
        }
        catch (Exception ex)
        {
            UpdateLog("CheckLatest 异常: " + ex.GetType().Name + " | " + ex.Message);
            throw;
        }
    }

    static Version ParseVer(string s)
    {
        if (string.IsNullOrEmpty(s)) return new Version(0,0,0,0);
        s = s.TrimStart('v', 'V');
        // 去掉可能的 +git串 后缀（InformationalVersion 会带）
        int plus = s.IndexOf('+');
        if (plus >= 0) s = s.Substring(0, plus);
        return Version.TryParse(s, out var v) ? v : new Version(0,0,0,0);
    }

    /// <summary>下载 url 到 localPath，返回本地路径。onProgress 0..1（-1 未知）。</summary>
    public static async Task<string> DownloadToAsync(string url, string localPath, Action<double>? onProgress = null)
    {
        UpdateLog("Download: 开始 " + url);
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("FastDelete-Updater/1.0");
        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
        using var resp = await http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            UpdateLog("Download 失败: HTTP " + (int)resp.StatusCode + " 下载 " + url);
            throw new HttpRequestException("下载失败 HTTP " + (int)resp.StatusCode);
        }
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
        UpdateLog("Download 完成: " + localPath + " (" + got + " bytes, 预期 " + total + ")");
        return localPath;
    }

    /// <summary>应用更新（辉哥哥 6 步逻辑 + 全程日志）。</summary>
    /// <returns>是否成功发起升级流程。</returns>
    public static async Task<bool> ApplyUpdateAsync(string url, Action<double>? onProgress = null, Action<string>? log = null, System.Windows.Forms.Form? owner = null)
    {
        UpdateLog("=== ApplyUpdate 开始 (url=" + url + ") ===");
        try
        {
            string selfDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName)!;
            string tmp = Path.Combine(Path.GetTempPath(), "FastDelete_update_" + Guid.NewGuid().ToString("N") + ".tmp");

            // ① 下载
            UpdateLog("步骤1 下载: " + url + " → " + tmp);
            try
            {
                await DownloadToAsync(url, tmp, onProgress);
            }
            catch (Exception dex)
            {
                UpdateLog("下载异常: " + dex.GetType().Name + " | " + dex.Message);
                log?.Invoke("更新失败：下载异常 " + dex.Message);
                return false;
            }
            var fsi = new FileInfo(tmp);
            if (!fsi.Exists || fsi.Length < 100000)
            {
                UpdateLog("下载结果异常: 文件不存在或过小 size=" + (fsi.Exists ? fsi.Length.ToString() : "N/A"));
                log?.Invoke("更新失败：下载结果异常（文件过小）");
                return false;
            }
            log?.Invoke("已下载新版本到 " + tmp);
            UpdateLog("下载成功 (" + fsi.Length + " bytes)");
            // 把 .tmp 改成 .exe（避免 SmartScreen 对未知后缀拦截 Inno 安装器）
            string installExe = tmp;
            if (tmp.EndsWith(".tmp"))
            {
                string exeName = tmp.Substring(0, tmp.Length - 4) + ".exe";
                try { File.Move(tmp, exeName); tmp = exeName; installExe = exeName; UpdateLog(".tmp → .exe: " + tmp); }
                catch (Exception mex) { UpdateLog(".tmp 改名失败（保留 .tmp）: " + mex.Message); }
            }

            // 辉哥哥 6 步第 4 步：下载完成，询问"是否马上升级"
            if (owner != null)
            {
                var a = System.Windows.Forms.MessageBox.Show(
                    owner,
                    "已完成新版本下载：\r\n" + tmp + "\r\n是否马上升级？",
                    "FastDelete 更新",
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Question);
                UpdateLog("询问是否马上升级: 用户选择=" + a);
                if (a != System.Windows.Forms.DialogResult.Yes)
                {
                    log?.Invoke("已取消升级（新版本已下载到 " + tmp + "，稍后可手动安装）");
                    return false;
                }
            }

            if (IsPortable)
            {
                // ② 便携版：写 .bat 替换+重启
                string oldExe = Path.Combine(selfDir, "FastDelete.exe");
                string bat = Path.Combine(selfDir, "FastDelete_update.bat");
                string[] batLines =
                {
                    "@echo off",
                    "setlocal",
                    "set \"D=" + oldExe + "\"",
                    "set \"T=" + tmp + "\"",
                    "if not exist \"%T%\" exit /b 1",
                    "taskkill /F /IM FastDelete.exe >nul 2>&1",
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
                File.WriteAllLines(bat, batLines, new UTF8Encoding(false));
                UpdateLog("步骤2 便携版: 启动 bat " + bat);
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = bat, UseShellExecute = false });
                }
                catch (Exception bex)
                {
                    UpdateLog("启动 bat 失败: " + bex.GetType().Name + " | " + bex.Message);
                    log?.Invoke("更新失败：启动升级脚本异常 " + bex.Message);
                    return false;
                }
                log?.Invoke("便携版已更新，正在重启…");
                UpdateLog("=== ApplyUpdate 完成（便携版已启动）===");
                return true;
            }
            else
            {
                // ③ 安装版：辉哥哥要求的 2 点 —— ① 杀掉运行中 FastDelete 进程；② 打开下载好的 exe（就像双击）
                UpdateLog("步骤3 杀掉运行中 FastDelete 进程");
                try
                {
                    var procs = Process.GetProcessesByName("FastDelete");
                    UpdateLog("FastDelete 进程数=" + procs.Length);
                    foreach (var pr in procs)
                    {
                        try { if (pr.Id != Process.GetCurrentProcess().Id) pr.Kill(); } catch { }
                        try { pr.Dispose(); } catch { }
                    }
                    UpdateLog("运行中进程已杀掉（保留当前进程，等 Inno 装完）");
                }
                catch (Exception kex) { UpdateLog("杀进程异常: " + kex.Message); }

                // ④ 打开下载好的 exe（就像双击：UseShellExecute=true 走 Shell，Inno 自身要 UAC 时 Windows 自动弹）
                UpdateLog("步骤4 打开下载好的 exe: " + installExe);
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = installExe,
                        Arguments = "/TASKS=rightclick",
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetTempPath()
                    };
                    using (var proc = Process.Start(psi))
                    {
                        UpdateLog("安装器进程已启动 pid=" + proc.Id);
                        proc.WaitForExit(1000);
                    }
                    log?.Invoke("已打开安装程序，请在安装界面完成安装。若弹出 UAC 请允许。");
                    UpdateLog("=== ApplyUpdate 完成（安装版）===");
                    return true;
                }
                catch (Exception ex)
                {
                    UpdateLog("打开安装器失败: " + ex.GetType().Name + " | " + ex.Message);
                    log?.Invoke("打开安装器失败：" + ex.Message + "。请手动双击 " + installExe + " 完成安装。");
                    return false;
                }
                UpdateLog("=== ApplyUpdate 完成（安装版已启动）===");
                return true;
            }
        }
        catch (Exception ex)
        {
            UpdateLog("ApplyUpdate 顶层异常: " + ex.GetType().Name + " | " + ex.Message);
            log?.Invoke("更新失败：" + ex.Message);
            return false;
        }
    }
}
