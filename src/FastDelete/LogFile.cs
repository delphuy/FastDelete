using System;
using System.IO;
using System.Text;

namespace FastDelete;

/// <summary>
/// 日志落盘：优先 写 工具 同级 log\ 目录； 若 不可 写（如 非 管理员 运行 于 C:\Program Files）
/// 则 回退 到 %LOCALAPPDATA%\FastDelete\log（普通 用户 恒 可写）； 再 失败 则 降级 为 纯 内存 模式。
/// 任何 一步 失败 都 不 抛 异常、不 崩 程序 —— 界面 恒 能 正常 显示。
/// 文件 名 fastdelete_YYYY-MM-DD_HH-mm-ss-fff.log（Windows 文件 名 不 允许 冒号， 故 用 下划线/连字符）。
/// </summary>
public sealed class LogFile : IDisposable
{
    readonly string _path;
    StreamWriter? _writer; // null = 内存 模式（写 失败 不 致命）

    /// <summary>实际 落盘 的 log 目录（供 UI 展示/用户 查找）。 构造 后 恒 非 空。</summary>
    public string LogDirectory { get; private set; } = string.Empty;

    public string Path => _path;

    public LogFile(string subDir)
    {
        string s = string.IsNullOrEmpty(subDir) ? "log" : subDir;

        // 1) 首选：工具 目录 下 log/
        string toolDir = AppDomain.CurrentDomain.BaseDirectory;
        string cand1 = System.IO.Path.Combine(toolDir, s);
        if (TryOpen(cand1, out string? p1, out StreamWriter? w1))
        {
            LogDirectory = cand1; _path = p1 ?? ""; _writer = w1;
            return;
        }

        // 2) 回退：%LOCALAPPDATA%\FastDelete\log（普通 用户 恒 可写）
        string userRoot = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FastDelete");
        string cand2 = System.IO.Path.Combine(userRoot, s);
        if (TryOpen(cand2, out string? p2, out StreamWriter? w2))
        {
            LogDirectory = cand2; _path = p2 ?? ""; _writer = w2;
            return;
        }

        // 3) 最终 降级：内存 模式（不 落盘， 写 调用 全 变 no-op， 程序 不 崩）
        LogDirectory = cand2; _path = ""; _writer = null;
    }

    /// <summary>尝试 在 dir 下 建 目录 并 打开 日志 文件； 成功 返回 true 并 出参 路径/写流。 任何 异常 吞掉 返回 false。</summary>
    static bool TryOpen(string dir, out string path, out StreamWriter? writer)
    {
        path = ""; writer = null;
        try
        {
            Directory.CreateDirectory(dir);
            string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            path = System.IO.Path.Combine(dir, "fastdelete_" + stamp + ".log");
            writer = new StreamWriter(path, append: false, Encoding.UTF8) { AutoFlush = true };
            writer.WriteLine("=== FastDelete 日志 ===");
            writer.WriteLine("开始时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            writer.WriteLine("可执行文件: " + AppDomain.CurrentDomain.BaseDirectory);
            return true;
        }
        catch
        {
            // 权限/磁盘/路径 等 任何 失败 → 走 下一 候选 目录
            try { writer?.Dispose(); } catch { }
            writer = null;
            return false;
        }
    }

    public void Write(string line)
    {
        try { _writer?.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + "  " + line); } catch { }
    }

    public void CloseAndHeader()
    {
        try
        {
            if (_writer is not null)
            {
                _writer.WriteLine("结束时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                _writer.WriteLine("=== 日志结束 ===");
            }
        }
        catch { }
    }

    public void Dispose()
    {
        try { _writer?.Dispose(); } catch { }
        _writer = null;
    }
}
