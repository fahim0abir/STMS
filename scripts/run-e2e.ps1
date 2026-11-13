param(
    [switch]$Headed
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$apiPath = Join-Path $root 'Server\SmartTask.API'
$clientPath = Join-Path $root 'Client'

function Wait-Endpoint {
    param([string]$Url, [int]$TimeoutSeconds)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) { return }
        } catch {
            Start-Sleep -Seconds 2
        }
    }
    throw "Timed out waiting for $Url"
}

Write-Host 'Starting API on http://localhost:5195 (ASPNETCORE_ENVIRONMENT=E2E, InMemory database)...'
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$env:ASPNETCORE_ENVIRONMENT = 'E2E'
$apiProcess = Start-Process -FilePath 'dotnet' `
    -ArgumentList 'run', '--no-launch-profile', '--urls', 'http://localhost:5195' `
    -WorkingDirectory $apiPath -PassThru -WindowStyle Hidden

Write-Host 'Starting Angular dev server on http://localhost:4200...'
$clientProcess = Start-Process -FilePath 'npm.cmd' `
    -ArgumentList 'start', '--', '--port', '4200' `
    -WorkingDirectory $clientPath -PassThru -WindowStyle Hidden

$exitCode = 1
try {
    Wait-Endpoint -Url 'http://localhost:5195/health' -TimeoutSeconds 180
    Wait-Endpoint -Url 'http://localhost:4200' -TimeoutSeconds 240

    Push-Location $clientPath
    $playwrightArgs = @('playwright', 'test', '--reporter=line')
    if ($Headed) { $playwrightArgs += '--headed' }
    & npx @playwrightArgs
    $exitCode = $LASTEXITCODE
} finally {
    Pop-Location
    Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
    Stop-Process -Id $clientProcess.Id -Force -ErrorAction SilentlyContinue
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
}

exit $exitCode
