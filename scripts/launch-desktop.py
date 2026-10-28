#!/usr/bin/env python3
"""Launch ShopKeeperPro onto Chamara's interactive Windows 11 desktop."""
import subprocess
import sys
from pathlib import Path

ssh_config = Path('/Users/chamara/Projects/other/vmware/.local-ssh/config')
if not ssh_config.is_file():
    sys.exit('UTM SSH configuration not found.')

remote_exe = r'C:\Dev\Projects\ShopKeeperPro\src\ShopKeeperPro.WinForms\bin\Debug\net10.0-windows\ShopKeeperPro.WinForms.exe'
work_dir = r'C:\Dev\Projects\ShopKeeperPro\src\ShopKeeperPro.WinForms'

ps_script = f'''
$ErrorActionPreference = 'Stop'
$user = (Get-Process explorer -IncludeUserName | Select-Object -First 1).UserName
if (!$user) {{ throw 'No logged-in Windows desktop found.' }}

$taskName = 'Launch-ShopKeeperPro'
if (Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue) {{
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false | Out-Null
}}

Get-Process ShopKeeperPro* -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

$exe = '{remote_exe}'
if (!(Test-Path $exe)) {{ throw "Executable not found at $exe" }}

$action = New-ScheduledTaskAction -Execute $exe -WorkingDirectory '{work_dir}'
$principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Limited
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit ([TimeSpan]::Zero)

Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings | Out-Null
Start-ScheduledTask -TaskName $taskName
Write-Output "ShopKeeperPro launched successfully on desktop for $user!"
'''

# Use windows-command.py to execute via guest agent as SYSTEM to register the interactive task
windows_cmd = Path('/Users/chamara/Projects/other/vmware/scripts/windows-command.py')
import tempfile

with tempfile.NamedTemporaryFile('w', suffix='.ps1', delete=False) as f:
    f.write(ps_script)
    f_path = f.name

try:
    res = subprocess.run([sys.executable, str(windows_cmd), f_path], text=True, capture_output=True)
    print(res.stdout)
    if res.stderr:
        print(res.stderr, file=sys.stderr)
    sys.exit(res.returncode)
finally:
    Path(f_path).unlink(missing_ok=True)
