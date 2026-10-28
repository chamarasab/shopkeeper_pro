Add-Type -AssemblyName System.Windows.Forms,System.Drawing

$p = Get-Process ShopKeeperPro.WinForms -ErrorAction SilentlyContinue
if (!$p) {
    Write-Output "ShopKeeperPro.WinForms is NOT running."
    exit
}

# We can query the main window handle
$hwnd = $p.MainWindowHandle
Write-Output "MainWindowHandle: $hwnd"

# Let's inspect using UIAutomation or native Windows API
Add-Type @'
using System;
using System.Runtime.InteropServices;
using System.Text;

public class Win32 {
[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
'@

$hwnd = [Win32]::FindWindow($null, "ShopKeeper Pro - Modern Retail Point of Sale")
Write-Output "MainWindowHandle: $hwnd"

$rect = New-Object Win32+RECT
[Win32]::GetWindowRect($hwnd, [ref]$rect)
Write-Output "MainWindow Rect: ($($rect.Left), $($rect.Top)) - ($($rect.Right), $($rect.Bottom)) [Width: $($rect.Right - $rect.Left), Height: $($rect.Bottom - $rect.Top)]"

$children = [System.Collections.Generic.List[string]]::new()
$proc = [Win32+EnumWindowsProc]{
    param($h, $l)
    $sbClass = New-Object System.Text.StringBuilder 256
    [Win32]::GetClassName($h, $sbClass, 256) | Out-Null
    $sbText = New-Object System.Text.StringBuilder 256
    [Win32]::GetWindowText($h, $sbText, 256) | Out-Null
    $r = New-Object Win32+RECT
    [Win32]::GetWindowRect($h, [ref]$r)
    $w = $r.Right - $r.Left
    $hH = $r.Bottom - $r.Top
    $children.Add("$($sbClass.ToString()) | '$($sbText.ToString())' | Rect: ($($r.Left), $($r.Top), $w, $hH)")
    return $true
}

[Win32]::EnumChildWindows($hwnd, $proc, [IntPtr]::Zero) | Out-Null
$children | ForEach-Object { Write-Output $_ }
