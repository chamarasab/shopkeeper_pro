Add-Type @'
using System;
using System.Runtime.InteropServices;
public class Win {
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
'@

# Minimize Windows Terminal / CMD
Get-Process WindowsTerminal, cmd, powershell -ErrorAction SilentlyContinue | ForEach-Object {
    [Win]::ShowWindow($_.MainWindowHandle, 6) # SW_MINIMIZE
}

# Find ShopKeeperPro process and maximize/bring to front
$p = Get-Process ShopKeeperPro.WinForms -ErrorAction SilentlyContinue | Select-Object -First 1
if ($p) {
    [Win]::ShowWindow($p.MainWindowHandle, 3) # SW_MAXIMIZE
    [Win]::SetForegroundWindow($p.MainWindowHandle)
    Write-Output "ShopKeeperPro maximized and brought to foreground"
} else {
    Write-Output "ShopKeeperPro process not found"
}
