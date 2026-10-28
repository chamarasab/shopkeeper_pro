$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
[Console]::OutputEncoding = New-Object Text.UTF8Encoding($false)

$root = Split-Path $PSScriptRoot
Set-Location $root

$status = [ordered]@{
    passed = $false
    identity = [Security.Principal.WindowsIdentity]::GetCurrent().Name
    buildExitCode = $null
    testExitCode = $null
    runtimeExitCode = $null
    error = $null
}

$log = Join-Path $root 'build.log'
Start-Transcript -Path $log | Out-Null

try {
    $dotnet = 'C:\Program Files\dotnet\dotnet.exe'
    if (!(Test-Path $dotnet)) { $dotnet = 'dotnet' }

    Write-Output "=== 1. Building ShopKeeperPro Solution on Windows ARM64 ==="
    & $dotnet build ShopKeeperPro.sln --nologo
    $status.buildExitCode = $LASTEXITCODE
    if ($LASTEXITCODE -ne 0) { throw "Windows solution build failed with code $LASTEXITCODE." }

    Write-Output "=== 2. Running Core Checks & Database Tests ==="
    & $dotnet run --project tests/ShopKeeperPro.Tests --no-build -- --check-db
    $status.testExitCode = $LASTEXITCODE
    if ($LASTEXITCODE -ne 0) { throw "Windows core verification checks failed with code $LASTEXITCODE." }

    Write-Output "=== 3. Executing Native WinForms Responsive Self-Test ==="
    $exe = Join-Path $root 'src\ShopKeeperPro.WinForms\bin\Debug\net10.0-windows\ShopKeeperPro.WinForms.exe'
    $testResult = Join-Path $root 'runtime-test.json'
    
    $proc = Start-Process -FilePath $exe -ArgumentList @('--self-test', $testResult) -PassThru -Wait
    $status.runtimeExitCode = $proc.ExitCode
    
    if ($proc.ExitCode -ne 0 -or !(Test-Path $testResult)) {
        throw "WinForms runtime self-test failed with code $($proc.ExitCode)."
    }

    $runtimeData = Get-Content -Raw $testResult | ConvertFrom-Json
    if (!$runtimeData.passed) {
        throw "WinForms self-test reported failure."
    }

    Write-Output "Native WinForms self-test verified successfully on $($runtimeData.architecture)!"
    $status.passed = $true
}
catch {
    $status.error = $_.Exception.ToString()
    Write-Output "ERROR: $($status.error)"
}
finally {
    Stop-Transcript | Out-Null
    $status | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $root 'status.json') -Encoding UTF8
}

if (!$status.passed) { exit 1 }
