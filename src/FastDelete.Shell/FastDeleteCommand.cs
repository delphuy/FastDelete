using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace FastDelete.Shell;

/// <summary>Win11 资源管理器右键菜单命令（IExplorerCommand），第一屏直显。</summary>
[ComVisible(true)]
[Guid("6F5E2C9D-8A1B-4C3D-9E7F-2A1B3C4D5E6F")]
[ClassInterface(ClassInterfaceType.None)]
public sealed class FastDeleteCommand : IExplorerCommand, IExplorerCommandOverview
{
    const int S_OK = 0;

    static string SelfDir => Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
    static string MainExe => Path.Combine(SelfDir, "FastDelete.exe");

    // ===== IExplorerCommand =====
    int IExplorerCommand.GetTitle([MarshalAs(UnmanagedType.LPWStr)] out string title)
    {
        title = "Fast Delete - 极速删除";
        return S_OK;
    }
    int IExplorerCommand.GetToolTip([MarshalAs(UnmanagedType.LPWStr)] out string tip)
    {
        tip = "用 FastDelete 批量极速删除此目录";
        return S_OK;
    }
    int IExplorerCommand.GetIcon([MarshalAs(UnmanagedType.LPWStr)] out string icon)
    {
        icon = MainExe + ",0";
        return S_OK;
    }
    int IExplorerCommand.CanShowMenuInfo(out int canShow)
    {
        canShow = 0;
        return S_OK;
    }
    int IExplorerCommand.GetDropTarget(out IntPtr dropTarget)
    {
        dropTarget = IntPtr.Zero;
        return S_OK;
    }
    int IExplorerCommand.GetStretchedBitmapInfo(out int width, out int height, out IntPtr bits, out int pitch)
    {
        width = 0; height = 0; bits = IntPtr.Zero; pitch = 0;
        return S_OK;
    }
    int IExplorerCommand.Invoke([MarshalAs(UnmanagedType.LPWStr)] string selectionPath, [MarshalAs(UnmanagedType.LPWStr)] string explorerOptions)
    {
        try
        {
            string path = (selectionPath ?? "").Trim();
            if (string.IsNullOrEmpty(path)) path = SelfDir;
            Process.Start(new ProcessStartInfo
            {
                FileName = MainExe,
                Arguments = "add \"" + path + "\"",
                WorkingDirectory = path,
                UseShellExecute = true,
            });
        }
        catch { }
        return S_OK;
    }
    int IExplorerCommand.GetVerb([MarshalAs(UnmanagedType.LPWStr)] out string verb)
    {
        verb = "fastdelete";
        return S_OK;
    }

    // ===== IExplorerCommandOverview =====
    int IExplorerCommandOverview.GetOverview([MarshalAs(UnmanagedType.LPWStr)] out string overview)
    {
        overview = "极速删除海量文件";
        return S_OK;
    }
    int IExplorerCommandOverview.GetIcon([MarshalAs(UnmanagedType.LPWStr)] out string icon)
    {
        icon = MainExe + ",0";
        return S_OK;
    }
}
