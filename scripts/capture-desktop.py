#!/usr/bin/env python3
"""Capture a screenshot of Chamara's active Windows 11 desktop and fetch it."""
import subprocess
import sys
import tempfile
import time
from pathlib import Path

ssh_config = Path('/Users/chamara/Projects/other/vmware/.local-ssh/config')
windows_cmd = Path('/Users/chamara/Projects/other/vmware/scripts/windows-command.py')

remote_png = r'C:\Users\winbuild\Desktop\pos_screen.png'

ps_script = f'''
$ErrorActionPreference = 'Stop'
$user = (Get-Process explorer -IncludeUserName | Select-Object -First 1).UserName
if (!$user) {{ throw 'No logged-in Windows desktop found.' }}

# Terminate any visible interactive consoles/terminals before capturing
Get-Process WindowsTerminal, OpenConsole -ErrorAction SilentlyContinue | Stop-Process -Force

$taskName = 'Capture-Desktop-Screen'
if (Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue) {{
    Stop-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue | Out-Null
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false | Out-Null
}}

$captureScript = 'C:\\Users\\winbuild\\Desktop\\do_capture.ps1'
$code = @'
$ErrorActionPreference = 'Stop'
Start-Sleep -Milliseconds 600
try {{
    Add-Type -AssemblyName System.Windows.Forms,System.Drawing
    Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public class DpiHelper {{ [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext); }}'
    [DpiHelper]::SetProcessDpiAwarenessContext([IntPtr](-4))
    $bounds = [Windows.Forms.Screen]::PrimaryScreen.Bounds
    $image = New-Object Drawing.Bitmap($bounds.Width, $bounds.Height)
    $g = [Drawing.Graphics]::FromImage($image)
    try {{
        $g.CopyFromScreen($bounds.Location, [Drawing.Point]::Empty, $bounds.Size)
        $pngPath = 'C:\\Users\\winbuild\\Desktop\\pos_screen.png'
        $image.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }} finally {{
        $g.Dispose()
        $image.Dispose()
    }}
}} catch {{
    # ignore
}}
'@
Set-Content -Path $captureScript -Value $code -Encoding UTF8

$action = New-ScheduledTaskAction -Execute 'C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe' -Argument "-NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy RemoteSigned -File `"$captureScript`""
$principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Limited
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit ([TimeSpan]::FromMinutes(1))

Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings | Out-Null
Start-ScheduledTask -TaskName $taskName

for ($i = 0; $i -lt 15; $i++) {{
    Start-Sleep -Seconds 1
    if (Test-Path 'C:\\Users\\winbuild\\Desktop\\pos_screen.png') {{
        break
    }}
}}

Unregister-ScheduledTask -TaskName $taskName -Confirm:$false | Out-Null
'''

with tempfile.NamedTemporaryFile('w', suffix='.ps1', delete=False) as f:
    f.write(ps_script)
    f_path = f.name

try:
    res = subprocess.run([sys.executable, str(windows_cmd), f_path], text=True, capture_output=True)
    if res.stderr:
        print("Guest agent stderr:", res.stderr)
finally:
    Path(f_path).unlink(missing_ok=True)

# Fetch screenshot via scp
local_dest = Path('/Users/chamara/.gemini/antigravity-ide/brain/6ee79a17-0190-4ba7-bea2-b2d2d42cc635/pos_desktop_preview.png')
scp_cmd = ['scp', '-F', str(ssh_config), 'utm-winforms:/C:/Users/winbuild/Desktop/pos_screen.png', str(local_dest)]
fetch_res = subprocess.run(scp_cmd, capture_output=True, text=True)
if fetch_res.returncode == 0:
    print(f"Screenshot successfully downloaded to {local_dest}")
else:
    print("SCP failed:", fetch_res.stderr)
