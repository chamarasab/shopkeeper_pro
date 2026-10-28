Add-Type @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public class WinProc {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);
    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public static uint FindPidByTitle(string sub) {
        uint targetPid = 0;
        EnumWindows((hWnd, lParam) => {
            var sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, 256);
            string title = sb.ToString();
            if (title.IndexOf(sub, StringComparison.OrdinalIgnoreCase) >= 0) {
                GetWindowThreadProcessId(hWnd, out targetPid);
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return targetPid;
    }
}
'@

$pidToKill = [WinProc]::FindPidByTitle("Administrator:")
Write-Output "Found target PID: $pidToKill"
if ($pidToKill -gt 0) {
    Stop-Process -Id $pidToKill -Force -ErrorAction SilentlyContinue
    Write-Output "Killed PID $pidToKill"
}
