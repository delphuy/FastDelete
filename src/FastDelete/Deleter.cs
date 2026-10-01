using System;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace FastDelete;

/// <summary>
/// 删除执行器：按文件数分档执行不同 cmd 命令。
///  100 < M < 10万   →  rd /s /q
///  10万 ≤ M < 100万  →  del /f /s /q
///  M ≥ 100万          →  空目录 + robocopy /MIR 镜像覆盖 + 双 rd
/// 全部路径加 \\?\ 超长路径前缀；每一步写入日志。
/// </summary>
public static class Deleter
{
    /// <summary>执行删除。M 为该目录当前文件数（由调用方清点得到）。</summary>
    public static void Execute(string target, long M, Action<string> log)
    {
        string longPath = Scanner.ToLongPath(target);
        log("[删除] 目标: " + target);
        log("[删除] 文件数 M = " + M.ToString("N0"));

        switch (M)
        {
            case < 100_000:
                DoRd(longPath, log);
                break;
            case < 1_000_000:
                DoDel(longPath, log);
                break;
            default:
                DoRobocopy(longPath, log);
                break;
        }

        log("[删除] 完成。目标 " + target + " 已清理。");
    }

    /// <summary>小/中量：rd /s /q。</summary>
    static void DoRd(string longPath, Action<string> log)
    {
        string cmd = $"rd /s /q \"{longPath}\"";
        log("[命令] " + cmd);
        int code = Run(cmd, log);
        log("[结果] rd /s /q 退出码 = " + code + (code == 0 ? "（成功）" : "（可能部分文件被占用）"));
    }

    /// <summary>中/大量：del /f /s /q 清文件（保留目录壳），再 rd 清目录。</summary>
    static void DoDel(string longPath, Action<string> log)
    {
        string c1 = $"del /f /s /q \"{longPath}\\*.*\" > nul";
        log("[命令] " + c1);
        int code1 = Run(c1, log);
        log("[结果] del 退出码 = " + code1 + "（文件已删，目录壳保留）");

        string c2 = $"rd /s /q \"{longPath}\"";
        log("[命令] " + c2);
        int code2 = Run(c2, log);
        log("[结果] rd 退出码 = " + code2 + (Directory.Exists(longPath) ? "（目录仍存在，可能被占用或残留）" : "（目录已移除）"));
    }

    /// <summary>超大量：建空目录 + robocopy /MIR 镜像覆盖 + 双 rd。
    /// 空目录建在系统临时区，避免写死 D:\。\\?\ 前缀对 robocopy 不生效（robocopy 自解析），
    /// 故 robocopy 两步用普通路径，最后的 rd 用 \\?\ 前缀。</summary>
    static void DoRobocopy(string longPath, Action<string> log)
    {
        string normalPath = longPath.Replace(@"\\?\", "");
        string emptyDir = Path.Combine(Path.GetTempPath(), "fastdelete_empty_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(emptyDir);

        try
        {
            log("[临时] 创建空目录 " + emptyDir);

            string c1 = $"robocopy \"{emptyDir}\" \"{normalPath}\" /MIR /NFL /NDL /NP /R:0 /W:0";
            log("[命令] " + c1 + "（robocopy 镜像：用空目录覆盖目标，实际删除所有文件）");
            int code1 = Run(c1, log);
            log("[结果] robocopy 退出码 = " + code1 + "（0/1 均表示同步成功；≥8 为错误）");

            string c2 = $"rd /s /q \"{longPath}\"";
            log("[命令] " + c2);
            int code2 = Run(c2, log);
            log("[结果] rd 目标 = " + code2);
        }
        finally
        {
            try { if (Directory.Exists(emptyDir)) Directory.Delete(emptyDir, true); } catch { }
        }
    }

    /// <summary>通过 cmd /c 执行单条命令，捕获退出码与输出尾段，写入日志。</summary>
    /// 公开以便 LoadingForm 复用。</summary>
    public static int Run(string cmd, Action<string> log)
    {
        using var proc = new Process();
        proc.StartInfo = new ProcessStartInfo("cmd.exe", "/c " + cmd)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Environment.SystemDirectory
        };
        proc.Start();
        string outp = proc.StandardOutput.ReadToEnd();
        string errp = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        int code = proc.ExitCode;

        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(outp)) sb.AppendLine("[stdout] " + outp.Trim());
        if (!string.IsNullOrWhiteSpace(errp)) sb.AppendLine("[stderr] " + errp.Trim());
        if (sb.Length > 0) log(sb.ToString());
        return code;
    }
}
