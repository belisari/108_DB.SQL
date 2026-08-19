<#
.SYNOPSIS
Runs the same SqlPackage extract described in ../../import-mq-schema-notes.md
against the local dockerized SQL Server, then copies the resulting [mq]
folder (and its CREATE SCHEMA script) into the MqImport sqlproj.

Requires the 'microsoft.sqlpackage' dotnet global tool:
  dotnet tool install -g microsoft.sqlpackage
(cross-platform - no native SQL Server / SSDT install needed)
#>

param(
    [string]$SaPassword = "YourStrong!Passw0rd",
    [int]$HostPort = 14330,
    [string]$Database = "MqSampleDb"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$extractDir = Join-Path $root "_extract\$Database"
$projectDir = Join-Path $root "MqImport"

if (Test-Path $extractDir) {
    Remove-Item $extractDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $extractDir) | Out-Null

$connectionString = "Server=localhost,$HostPort;Database=$Database;User ID=sa;Password=$SaPassword;TrustServerCertificate=True;"

Write-Host "Extracting $Database into schema/object-type folder layout..." -ForegroundColor Cyan
sqlpackage /Action:Extract `
    /SourceConnectionString:$connectionString `
    /TargetFile:$extractDir `
    /p:ExtractTarget=SchemaObjectType `
    /p:ExtractAllTableData=False

$mqSource = Join-Path $extractDir "mq"
$mqSchemaScript = Join-Path $extractDir "Security\mq.sql"

if (-not (Test-Path $mqSource)) {
    throw "Extract did not produce an 'mq' folder at $mqSource - check that the [mq] schema exists and was scripted."
}

Write-Host "Copying mq\* into the sqlproj..." -ForegroundColor Cyan
$mqTarget = Join-Path $projectDir "mq"
if (Test-Path $mqTarget) {
    Remove-Item $mqTarget -Recurse -Force
}
Copy-Item $mqSource $mqTarget -Recurse

if (Test-Path $mqSchemaScript) {
    $securityTarget = Join-Path $projectDir "Security"
    New-Item -ItemType Directory -Force -Path $securityTarget | Out-Null
    Copy-Item $mqSchemaScript (Join-Path $securityTarget "mq.sql") -Force
} else {
    Write-Warning "No Security\mq.sql produced by extract - add 'CREATE SCHEMA [mq]' to the project manually."
}

Write-Host ""
Write-Host "Imported files:" -ForegroundColor Green
Get-ChildItem $mqTarget -Recurse -File | ForEach-Object { Write-Host "  $($_.FullName.Substring($projectDir.Length + 1))" }

Write-Host ""
Write-Host "Next: dotnet build `"$projectDir\MqImport.sqlproj`"" -ForegroundColor Green
