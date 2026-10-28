Add-Type @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public class WinUtil {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);
    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public const uint WM_CLOSE = 0x0010;

    public static void CloseTerminalAndFocusShop() {
        EnumWindows((hWnd, lParam) => {
            var sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, 256);
            string title = sb.ToString();
            if (title.Contains("Administrator: C:\\WINDOWS") || title.Contains("Windows PowerShell") || title.Contains("Administrator: Windows PowerShell")) {
                PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
            if (title.Contains("ShopKeeper Pro")) {
                ShowWindow(hWnd, 3); // SW_MAXIMIZE
                SetForegroundWindow(hWnd);
            }
            return true;
        }, IntPtr.Zero);
    }
}
'@

[WinUtil]::CloseTerminalAndFocusShop()
Write-Output "Done window management"
