#!/usr/bin/env python3
import subprocess, tempfile, sys
from pathlib import Path

windows_cmd = Path('/Users/chamara/Projects/other/vmware/scripts/windows-command.py')

ps = '''
$ErrorActionPreference = 'Stop'
$user = (Get-Process explorer -IncludeUserName | Select-Object -First 1).UserName
$taskName = 'Clean-Desktop-Terminals'
if (Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue) {
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false | Out-Null
}

$cleanScript = 'C:\\Users\\winbuild\\Desktop\\do_clean.ps1'
$code = @'
Get-Process WindowsTerminal, wt -ErrorAction SilentlyContinue | Stop-Process -Force
'@
Set-Content -Path $cleanScript -Value $code -Encoding UTF8

$action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument "-NoProfile -ExecutionPolicy RemoteSigned -File `"$cleanScript`""
$principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Limited
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit ([TimeSpan]::FromMinutes(1))

Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings $settings | Out-Null
Start-ScheduledTask -TaskName $taskName
Start-Sleep -Seconds 2
Unregister-ScheduledTask -TaskName $taskName -Confirm:$false | Out-Null
'''

with tempfile.NamedTemporaryFile('w', suffix='.ps1', delete=False) as f:
    f.write(ps)
    p = f.name

try:
    res = subprocess.run([sys.executable, str(windows_cmd), p], text=True, capture_output=True)
    print(res.stdout)
    if res.stderr: print(res.stderr, file=sys.stderr)
finally:
    Path(p).unlink(missing_ok=True)
