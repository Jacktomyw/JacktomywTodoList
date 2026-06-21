using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TodoWidget.Native;

public static class DesktopWindow
{
    [DllImport("user32.dll")] private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);
    [DllImport("user32.dll")] private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    private static readonly IntPtr HWND_BOTTOM = new(1);
    private const uint SWP_NOSIZE = 1, SWP_NOMOVE = 2, SWP_NOACTIVATE = 0x10, SWP_SHOWWINDOW = 0x40;

    [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public static void PinToDesktop(Window window)
    {
        window.Loaded += (_, _) =>
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero) return;
            var progman = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Progman", null);
            if (progman != IntPtr.Zero)
            {
                SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, 0, 1000, IntPtr.Zero);
                IntPtr workerW = IntPtr.Zero;
                do { workerW = FindWindowEx(IntPtr.Zero, workerW, "WorkerW", null); if (workerW != IntPtr.Zero) { var dv = FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null); if (dv != IntPtr.Zero) { SetParent(handle, dv); return; } } } while (workerW != IntPtr.Zero);
            }
            SetWindowPos(handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        };
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, IntPtr lpdwResult);
}
