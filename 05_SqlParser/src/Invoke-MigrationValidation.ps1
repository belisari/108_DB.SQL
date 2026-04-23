<#
.SYNOPSIS
    Runs SqlMigrationValidator against migration scripts and maps exit codes
    to pipeline task results.

.DESCRIPTION
    Builds the validator (if needed), runs it against the migration scripts
    directory, and returns the correct exit code for the CI agent.

    Exit codes from the validator:
      0 — clean
      1 — errors found    → pipeline FAILS
      2 — warnings only   → pipeline FAILS (change $FailOnWarnings to $false to warn only)

.PARAMETER MigrationsPath
    Path to the folder containing .sql migration scripts.

.PARAMETER ValidatorProjectPath
    Path to the SqlMigrationValidator .csproj (only needed if building from source).

.PARAMETER ValidatorExePath
    Path to a pre-built SqlMigrationValidator.exe (skips the build step).

.PARAMETER FailOnWarnings
    If $true (default), warnings also fail the pipeline.
    Set to $false to allow warnings through.
#>
param(
    [Parameter(Mandatory)]
    [string]$MigrationsPath,

    [string]$ValidatorProjectPath = "$PSScriptRoot/SqlMigrationValidator.Cli/SqlMigrationValidator.Cli.csproj",

    [string]$ValidatorExePath = "",

    [bool]$FailOnWarnings = $true
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# 1. Resolve the validator binary
# ---------------------------------------------------------------------------

if ($ValidatorExePath -and (Test-Path $ValidatorExePath)) {
    $exe = $ValidatorExePath
    Write-Host "Using pre-built validator: $exe"
}
else {
    Write-Host "Building SqlMigrationValidator..."

    $publishDir = "$PSScriptRoot/.publish"
    dotnet publish $ValidatorProjectPath `
        --configuration Release `
        --output $publishDir `
        --self-contained false `
        -p:PublishSingleFile=true | Out-Host

    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed (exit $LASTEXITCODE)"
        exit 1
    }

    $exe = Get-ChildItem $publishDir -Filter "SqlMigrationValidator*" `
        | Where-Object { $_.Extension -in @(".exe", "") } `
        | Select-Object -First 1 -ExpandProperty FullName

    if (-not $exe) {
        Write-Error "Could not find SqlMigrationValidator binary in $publishDir"
        exit 1
    }
}

# ---------------------------------------------------------------------------
# 2. Run validation
# ---------------------------------------------------------------------------

Write-Host "`nValidating migration scripts in: $MigrationsPath`n" -ForegroundColor Cyan
& $exe $MigrationsPath
$exitCode = $LASTEXITCODE

# ---------------------------------------------------------------------------
# 3. Map exit code → pipeline result
# ---------------------------------------------------------------------------

switch ($exitCode) {
    0 {
        Write-Host "`nAll migration scripts passed validation." -ForegroundColor Green
        exit 0
    }
    1 {
        Write-Host "`nValidation FAILED — fix the errors above before merging." -ForegroundColor Red
        exit 1
    }
    2 {
        if ($FailOnWarnings) {
            Write-Host "`nValidation FAILED — warnings are treated as errors in this pipeline." -ForegroundColor Red
            exit 1
        }
        else {
            Write-Warning "Validation completed with warnings. Review the output above."
            exit 0
        }
    }
    default {
        Write-Error "Unexpected exit code from validator: $exitCode"
        exit 1
    }
}
