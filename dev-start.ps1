param(
    [switch]$KeepExisting
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiPort = 5179
$blazorPort = 5275

function Get-ListeningConnection($Port) {
    Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
}

function Stop-PortProcess($Port) {
    $connection = Get-ListeningConnection -Port $Port
    if (-not $connection) {
        return
    }

    $ownerPid = $connection.OwningProcess
    $process = Get-Process -Id $ownerPid -ErrorAction SilentlyContinue

    if ($process) {
        Write-Host "Port $Port is in use by PID $ownerPid ($($process.ProcessName)). Stopping it..."
        Stop-Process -Id $ownerPid -Force
        Start-Sleep -Seconds 1
    }
}

if (-not $KeepExisting) {
    Stop-PortProcess -Port $apiPort
    Stop-PortProcess -Port $blazorPort
}

Push-Location $root
try {
    Write-Host "Starting API on http://localhost:$apiPort ..."
    $apiProcess = Start-Process -FilePath "dotnet" -ArgumentList @("run", "--project", "FitPlay.Api", "--no-launch-profile", "--urls", "http://localhost:$apiPort") -WorkingDirectory $root -RedirectStandardOutput "$root\api_output.log" -RedirectStandardError "$root\api_error.log" -PassThru

    Write-Host "Starting Blazor on http://localhost:$blazorPort ..."
    $blazorProcess = Start-Process -FilePath "dotnet" -ArgumentList @("run", "--project", "FitPlay-Blazor", "--no-launch-profile", "--urls", "http://localhost:$blazorPort") -WorkingDirectory $root -RedirectStandardOutput "$root\blazor_output.log" -RedirectStandardError "$root\blazor_error.log" -PassThru

    $apiStatus = $null
    $blazorStatus = $null
    for ($i = 0; $i -lt 30; $i++) {
        $apiStatus = Get-ListeningConnection -Port $apiPort
        $blazorStatus = Get-ListeningConnection -Port $blazorPort
        if ($apiStatus -and $blazorStatus) {
            break
        }

        if ($apiProcess.HasExited -or $blazorProcess.HasExited) {
            break
        }

        Start-Sleep -Seconds 2
    }

    if ($apiStatus -and $blazorStatus) {
        Write-Host ""
        Write-Host "Both services are running:"
        Write-Host "- API:    http://localhost:$apiPort/swagger"
        Write-Host "- Blazor: http://localhost:$blazorPort"
    }
    else {
        Write-Host ""
        if ($apiProcess.HasExited) {
            Write-Host "API process exited early with code $($apiProcess.ExitCode)."
        }
        if ($blazorProcess.HasExited) {
            Write-Host "Blazor process exited early with code $($blazorProcess.ExitCode)."
        }
        Write-Host "One or more services failed to start. Check these logs:"
        Write-Host "- $root\api_error.log"
        Write-Host "- $root\blazor_error.log"
    }
}
finally {
    Pop-Location
}
