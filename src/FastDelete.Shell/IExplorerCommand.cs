using System;
using System.Runtime.InteropServices;

namespace FastDelete.Shell;

// Win11/Win10 资源管理器 IExplorerCommand（第一屏右键菜单命令接口）
[ComImport]
[Guid("93810521-1F4B-4051-BB9F-0D6982F0C8FC")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IExplorerCommand
{
    [PreserveSig]
    [return: MarshalAs(UnmanagedType.I4)]
    int GetTitle([MarshalAs(UnmanagedType.LPWStr)] out string title);

    [PreserveSig]
    [return: MarshalAs(UnmanagedType.I4)]
    int GetToolTip([MarshalAs(UnmanagedType.LPWStr)] out string tip);

    [PreserveSig]
    [return: MarshalAs(UnmanagedType.I4)]
    int GetIcon([MarshalAs(UnmanagedType.LPWStr)] out string icon);

    [PreserveSig]
    [return: MarshalAs(UnmanagedType.I4)]
    int CanShowMenuInfo(out int canShow);

    [PreserveSig]
    [return: MarshalAs(UnmanagedType.I4)]
    int GetDropTarget(out IntPtr dropTarget);

    [PreserveSig]
    [return: MarshalAs(UnmanagedType.I4)]
    int GetStretchedBitmapInfo(out int width, out int height, out IntPtr bits, out int pitch);

    [PreserveSig]
    [return: MarshalAs(UnmanagedType.I4)]
    int Invoke([MarshalAs(UnmanagedType.LPWStr)] string selectionPath, [MarshalAs(UnmanagedType.LPWStr)] string explorerOptions);

    [PreserveSig]
    [return: MarshalAs(UnmanagedType.I4)]
    int GetVerb([MarshalAs(UnmanagedType.LPWStr)] out string verb);
}

// IExplorerCommandOverview（预览面板）
[ComImport]
[Guid("250304C9-1E85-4538-8E37-CF586A00B47E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IExplorerCommandOverview
{
    [PreserveSig]
    [return: MarshalAs(UnmanagedType.I4)]
    int GetOverview([MarshalAs(UnmanagedType.LPWStr)] out string overview);

    [PreserveSig]
    [return: MarshalAs(UnmanagedType.I4)]
    int GetIcon([MarshalAs(UnmanagedType.LPWStr)] out string icon);
}
