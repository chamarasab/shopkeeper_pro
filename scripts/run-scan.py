#!/usr/bin/env python3
import subprocess, tempfile, sys
from pathlib import Path

windows_cmd = Path('/Users/chamara/Projects/other/vmware/scripts/windows-command.py')
ps = '''
$ErrorActionPreference = 'Stop'
$user = (Get-Process explorer -IncludeUserName | Select-Object -First 1).UserName
$taskName = 'Test-Scan-Input'
if (Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue) {
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false | Out-Null
}

$file = 'C:\\Users\\winbuild\\Desktop\\test_scan.ps1'
$action = New-ScheduledTaskAction -Execute 'C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe' -Argument "-NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy RemoteSigned -File `"$file`""
$principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Limited
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit ([TimeSpan]::FromMinutes(1))

Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings | Out-Null
Start-ScheduledTask -TaskName $taskName
Start-Sleep -Seconds 3
Unregister-ScheduledTask -TaskName $taskName -Confirm:$false | Out-Null
'''

with tempfile.NamedTemporaryFile('w', suffix='.ps1', delete=False) as f:
    f.write(ps)
    p = f.name
res = subprocess.run([sys.executable, str(windows_cmd), p], text=True, capture_output=True)
print(res.stdout)
if res.stderr: print(res.stderr, file=sys.stderr)
Path(p).unlink(missing_ok=True)
