# SqlMigrationValidator — How-To

## Projects

| Project | Purpose |
|---|---|
| `SqlMigrationValidator.Core` | Validation engine (netstandard2.0 library) |
| `SqlMigrationValidator.Cli` | Standalone CLI tool (net8.0 executable) |
| `SqlMigrationValidator.MSBuildTask` | MSBuild task NuGet package (net472) |
| `SqlMigrationValidator.Consumer` | Example project that consumes the task package |
| `SqlMigrationValidator.SqlProjConsumer` | Example SQL Server database project (`.sqlproj`, `Microsoft.Build.Sql` SDK) that consumes the task package |

---

## 1. Pack the MSBuild task

Run from the `src/` directory. The package is written to `../LocalPackages/`, which is registered as a local NuGet feed in `nuget.config`.

```bash
dotnet pack SqlMigrationValidator.MSBuildTask -c Release -o ../LocalPackages
```

Output: `../LocalPackages/SqlMigrationValidator.MSBuildTask.1.0.0.nupkg`

Package structure:

```
build/
  SqlMigrationValidator.MSBuildTask.targets   ← auto-imported by NuGet restore
tasks/net472/
  SqlMigrationValidator.MSBuildTask.dll
  SqlMigrationValidator.Core.dll
  Microsoft.SqlServer.TransactSql.ScriptDom.dll
```

---

## 2. Consume the package in a project

Add a `PackageReference` and set `MigrationsDirectory`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <MigrationsDirectory>$(MSBuildProjectDirectory)\Migrations</MigrationsDirectory>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="SqlMigrationValidator.MSBuildTask" Version="1.0.0" />
  </ItemGroup>

</Project>
```

The `.targets` file is imported automatically by NuGet restore. The `ValidateMigrationScripts` target runs **before every build** when `$(MigrationsDirectory)` is set.

---

## 3. Build — validation runs automatically

```bash
dotnet restore
dotnet build
```

Violations are reported as MSBuild warnings or errors with file, line, and rule code:

```
Migrations\V002__backfill.sql(15,1): warning DYNAMIC_SQL: EXECUTE with a dynamic
SQL string detected — ensure the generated SQL also complies with these rules.
```

Build **succeeds** on warnings; build **fails** on errors (e.g. DDL, transaction control, forbidden globals).

---

## 4. Optional properties

Set these in your project or on the command line to tune behaviour.

| Property | Default | Description |
|---|---|---|
| `MigrationsDirectory` | *(unset — validation skipped)* | Absolute or relative path to the folder containing `.sql` files |
| `MigrationSearchPattern` | `*.sql` | File glob passed to `Directory.GetFiles` |
| `TreatMigrationWarningsAsErrors` | `false` | Promotes warnings to errors, failing the build |
| `RunSqlMigrationValidation` | `true` | Set to `false` to disable the target entirely |

```bash
# Treat all warnings as errors
dotnet build -p:TreatMigrationWarningsAsErrors=true

# Skip validation (e.g. in a publish-only CI step)
dotnet build -p:RunSqlMigrationValidation=false
```

---

## 5. Bump the version

Edit `<Version>` in `SqlMigrationValidator.MSBuildTask.csproj`, repack, then update the `PackageReference` version in consuming projects.

```bash
dotnet pack SqlMigrationValidator.MSBuildTask -c Release -o ../LocalPackages /p:Version=1.1.0
```

---

## 6. Use the CLI tool instead of the MSBuild task

The CLI validates a directory directly without MSBuild integration:

```bash
dotnet run --project SqlMigrationValidator.Cli -- --directory path/to/migrations
```
