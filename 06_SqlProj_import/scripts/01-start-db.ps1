<#
.SYNOPSIS
Starts the dockerized SQL Server used for the mq-import dry run and creates
the MqSampleDb / [mq] sample schema inside it.

No native SQL Server install required - this runs the Linux SQL Server
container image via Docker.
#>

param(
    [string]$SaPassword = "YourStrong!Passw0rd",
    [string]$ContainerName = "mq_import_sql",
    [int]$HostPort = 14330
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "Starting SQL Server container via docker compose..." -ForegroundColor Cyan
Push-Location $root
try {
    docker compose up -d
} finally {
    Pop-Location
}

Write-Host "Waiting for SQL Server to become healthy..." -ForegroundColor Cyan
$maxAttempts = 30
for ($i = 1; $i -le $maxAttempts; $i++) {
    $status = docker inspect -f '{{.State.Health.Status}}' $ContainerName 2>$null
    if ($status -eq "healthy") {
        Write-Host "SQL Server is healthy." -ForegroundColor Green
        break
    }
    if ($i -eq $maxAttempts) {
        throw "SQL Server container did not become healthy in time. Check 'docker logs $ContainerName'."
    }
    Start-Sleep -Seconds 3
}

$initFile = Join-Path $root "init\01-create-mq-sample-db.sql"
Write-Host "Copying init script into container..." -ForegroundColor Cyan
docker cp $initFile "${ContainerName}:/tmp/01-create-mq-sample-db.sql"

Write-Host "Creating MqSampleDb and the [mq] sample schema..." -ForegroundColor Cyan
docker exec $ContainerName /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P $SaPassword -i /tmp/01-create-mq-sample-db.sql

Write-Host ""
Write-Host "Done. Connect with:" -ForegroundColor Green
Write-Host "  Server=localhost,$HostPort;Database=MqSampleDb;User ID=sa;Password=$SaPassword;TrustServerCertificate=True;"
