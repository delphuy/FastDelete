using System;

using System.IO;

namespace FastDelete;

/// <summary>
/// 使用原生 .NET 枚举（与 文化/编码 解耦）清点目录文件数。
/// 支持全量扫描与带早停阈值的扫描（达到 limit 立即停止并返回 count=limit）。
/// 仅 数文件个数，不读 字节大小（bytes 一律 返回 0）；被占用/无权限 时 返回 已 计数 部分。
/// 超长路径兼容：绝对盘符路径长度 > 240 时自动加 \\?\\ 前缀。
/// </summary>
public static class Scanner
{
    /// <summary>早停阈值：10 万文件。</summary>
    public static readonly long PartialLimit = 100_000L;

    /// <summary>返回 [是否存在, 文件数, 字节数(恒 0，不再 统计)]。目录 存在 但 无 文件 时 files=0、bytes=0。</summary>
    public static (bool exists, long files, long bytes) CountAll(string path) => Count(path, long.MaxValue);

    /// <summary>阈值扫描：累计 文件数 达到 limit 立即 停止， 返回 files=limit、bytes=0（未知）。</summary>
    public static (bool exists, long files, long bytes) CountPartial(string path, long limit) => Count(path, limit);

    static (bool, long, long) Count(string path, long limit)
    {
        string longPath = ToLongPath(path);
        if (!Directory.Exists(longPath))
            return (false, 0, 0);

        long files = 0;
        try
        {
            // 纯 点数：只 枚举 文件， 不 读 任何 属性； 达到 阈值 立即 返回， 保证 飞快
            foreach (string full in Directory.EnumerateFiles(longPath, "*", SearchOption.AllDirectories))
            {
                files++;
                if (files >= limit)
                    return (true, limit, 0);
            }
        }
        catch (Exception)
        {
            // 被占用/无权限 等 异常： 返回 已 计数 数
        }
        return (true, files, 0);
    }

    /// <summary>绝对 盘符 路径 长度 > 240 时 加 \\?\\ 前缀（超长 路径 兼容）。</summary>
    public static string ToLongPath(string p)
    {
        if (string.IsNullOrEmpty(p) || p.Length <= 240) return p;
        if (p.StartsWith(@"\\?\")) return p;
        if (p.Length >= 2 && p[1] == ':') return "\\?\\" + p;
        return p;
    }

    /// <summary>字节数 格式化为 可读 字符串（B/KB/MB/GB/TB）；负数 表示 未知。</summary>
    public static string FormatBytes(long bytes)
    {
        if (bytes < 0) return "未知，肯定很大";
        if (bytes < 1024) return bytes + " B";
        double kb = bytes / 1024.0;
        if (kb < 1024) return kb.ToString("F1") + " KB";
        double mb = kb / 1024.0;
        if (mb < 1024) return mb.ToString("F2") + " MB";
        double gb = mb / 1024.0;
        if (gb < 1024) return gb.ToString("F2") + " GB";
        double tb = gb / 1024.0;
        return tb.ToString("F2") + " TB";
    }
}
