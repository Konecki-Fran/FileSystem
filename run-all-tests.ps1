[CmdletBinding()]
param(
    [int]$PostgresPort = 54329
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = $PSScriptRoot
$composeProject = 'f24-integration-tests'
$databaseCompose = Join-Path $repositoryRoot 'docker-compose.db.yml'
$testCompose = Join-Path $repositoryRoot 'docker-compose.test.yml'
$frontendDirectory = Join-Path $repositoryRoot 'frontend'
$apiOutput = New-TemporaryFile
$apiError = New-TemporaryFile
$apiProcess = $null
$testsPassed = $false

$testEnvironment = @{
    POSTGRES_DB                       = 'f24'
    POSTGRES_USER                     = 'f24'
    POSTGRES_PASSWORD                 = 'f24-test-password'
    POSTGRES_PORT                     = $PostgresPort.ToString()
    POSTGRES_HOST                     = '127.0.0.1'
    RUN_POSTGRES_INTEGRATION_TESTS    = 'true'
}
$previousEnvironment = @{}

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory)] [scriptblock]$Command,
        [Parameter(Mandatory)] [string]$Description
    )

    Write-Host "`n==> $Description" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE."
    }
}

function Wait-ForPostgres {
    Write-Host "`n==> Waiting for PostgreSQL" -ForegroundColor Cyan
    for ($attempt = 1; $attempt -le 30; $attempt++) {
        docker compose -p $composeProject -f $databaseCompose exec -T postgres `
            pg_isready -U $env:POSTGRES_USER -d $env:POSTGRES_DB *> $null
        if ($LASTEXITCODE -eq 0) { return }
        Start-Sleep -Seconds 1
    }

    docker compose -p $composeProject -f $databaseCompose logs postgres
    throw 'PostgreSQL did not become ready in time.'
}

function Start-TestDatabase {
    param([switch]$WithSeedData)

    $arguments = @('compose', '-p', $composeProject, '-f', $databaseCompose)
    if ($WithSeedData) { $arguments += @('-f', $testCompose) }
    $arguments += @('up', '-d')

    Invoke-NativeCommand -Description 'Starting disposable PostgreSQL' -Command {
        docker @arguments
    }
    Wait-ForPostgres
}

function Remove-TestDatabase {
    Write-Host "`n==> Removing disposable PostgreSQL" -ForegroundColor Cyan
    docker compose -p $composeProject -f $databaseCompose -f $testCompose down -v --remove-orphans
    if ($LASTEXITCODE -ne 0) {
        Write-Warning 'The disposable PostgreSQL environment could not be removed automatically.'
    }
}

try {
    foreach ($name in $testEnvironment.Keys) {
        $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        [Environment]::SetEnvironmentVariable($name, $testEnvironment[$name], 'Process')
    }

    Invoke-NativeCommand -Description 'Installing frontend dependencies' -Command {
        Push-Location $frontendDirectory
        try { npm ci } finally { Pop-Location }
    }

    Invoke-NativeCommand -Description 'Running frontend unit tests' -Command {
        Push-Location $frontendDirectory
        try { npm test } finally { Pop-Location }
    }

    Invoke-NativeCommand -Description 'Ensuring Playwright Chromium is installed' -Command {
        Push-Location $frontendDirectory
        try { npx playwright install chromium } finally { Pop-Location }
    }

    Invoke-NativeCommand -Description 'Type-checking the frontend' -Command {
        Push-Location $frontendDirectory
        try { npm run typecheck } finally { Pop-Location }
    }

    Invoke-NativeCommand -Description 'Building the frontend' -Command {
        Push-Location $frontendDirectory
        try { npm run build } finally { Pop-Location }
    }

    Invoke-NativeCommand -Description 'Running backend unit tests' -Command {
        dotnet test (Join-Path $repositoryRoot 'backend/F24.Tests/F24.Tests.csproj')
    }

    Start-TestDatabase
    Invoke-NativeCommand -Description 'Running PostgreSQL integration tests' -Command {
        dotnet test (Join-Path $repositoryRoot 'backend/F24.IntegrationTests/F24.IntegrationTests.csproj')
    }

    # Integration tests reset the schema. Recreate the disposable database so
    # Playwright receives the deterministic seed data it expects.
    Remove-TestDatabase
    Start-TestDatabase -WithSeedData

    Write-Host "`n==> Starting API for Playwright" -ForegroundColor Cyan
    $apiProcess = Start-Process dotnet `
        -ArgumentList @(
            'run', '--project', (Join-Path $repositoryRoot 'backend/F24'),
            '--configuration', 'Release', '--no-launch-profile',
            '--urls', 'http://127.0.0.1:5000'
        ) `
        -WorkingDirectory $repositoryRoot `
        -WindowStyle Hidden `
        -RedirectStandardOutput $apiOutput.FullName `
        -RedirectStandardError $apiError.FullName `
        -PassThru

    for ($attempt = 1; $attempt -le 30; $attempt++) {
        try {
            $health = Invoke-WebRequest -UseBasicParsing 'http://127.0.0.1:5000/health/db' -TimeoutSec 2
            if ($health.StatusCode -eq 200) { break }
        }
        catch {
            if ($attempt -eq 30) {
                Write-Host (Get-Content -Raw $apiOutput.FullName)
                Write-Host (Get-Content -Raw $apiError.FullName)
                throw 'The API did not become ready in time.'
            }
            Start-Sleep -Seconds 1
        }
    }

    Invoke-NativeCommand -Description 'Running Playwright end-to-end tests' -Command {
        Push-Location $frontendDirectory
        try { npm run test:e2e } finally { Pop-Location }
    }

    $testsPassed = $true
    Write-Host "`nAll test suites passed." -ForegroundColor Green
}
finally {
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
        $apiProcess.WaitForExit()
    }

    Remove-TestDatabase

    foreach ($name in $previousEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process')
    }

    Remove-Item -LiteralPath $apiOutput.FullName, $apiError.FullName -Force -ErrorAction SilentlyContinue

    if (-not $testsPassed) {
        Write-Host "`nAt least one test suite failed." -ForegroundColor Red
    }
}
