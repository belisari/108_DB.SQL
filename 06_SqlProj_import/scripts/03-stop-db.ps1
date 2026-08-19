<#
.SYNOPSIS
Stops and removes the dry-run SQL Server container. Pass -RemoveData to also
delete the persisted data volume.
#>

param(
    [switch]$RemoveData
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Push-Location $root
try {
    if ($RemoveData) {
        docker compose down -v
    } else {
        docker compose down
    }
} finally {
    Pop-Location
}
