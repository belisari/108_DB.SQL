# mq schema import — dry run

Dry run of the workflow in [`import-mq-schema-notes.md`](import-mq-schema-notes.md)
against a disposable, dockerized SQL Server, so the real import at work is
just "point it at the real server instead of localhost".

Nothing here needs a native Windows SQL Server / SSDT install:

- **SQL Server** runs as the Linux container image `mcr.microsoft.com/mssql/server:2022-latest`
  via `docker compose`.
- **SqlPackage** (the extract tool) is installed as a cross-platform
  `dotnet` global tool (`dotnet tool install -g microsoft.sqlpackage`), not
  the Windows-only SSDT/DacFx install.
- **MqImport/** is an SDK-style `.sqlproj` (`Microsoft.Build.Sql` SDK) that
  builds with plain `dotnet build` — no Visual Studio required.

## Layout

```
06_SqlProj_import/
  docker-compose.yml        SQL Server 2022 (Linux container)
  init/
    01-create-mq-sample-db.sql   creates MqSampleDb + a sample [mq] schema
                                  (2 tables, a view, a function, a proc)
  scripts/
    01-start-db.ps1          docker compose up, wait for healthy, run init sql
    02-extract-mq.ps1        sqlpackage extract -> copies mq\* into MqImport
    03-stop-db.ps1           docker compose down (-RemoveData to wipe volume)
  MqImport/
    MqImport.sqlproj         SDK-style project; mq\ appears here after step 2
```

## Run it

```powershell
dotnet tool install -g microsoft.sqlpackage   # one-time, cross-platform

.\scripts\01-start-db.ps1     # starts the container + creates the sample [mq] schema
.\scripts\02-extract-mq.ps1   # extracts it and copies mq\ into MqImport\
dotnet build .\MqImport\MqImport.sqlproj
```

If the build succeeds you'll have `MqImport\bin\Debug\MqImport.dacpac` and a
`MqImport\mq\Tables\...`, `MqImport\mq\StoredProcedures\...`,
`MqImport\mq\Views\...`, `MqImport\mq\Functions\...` folder layout — the same
shape the real `[mq]` import will produce.

## Connect to the database

Defaults from `docker-compose.yml` / `scripts\01-start-db.ps1`:

| | |
|---|---|
| Server / host | `localhost` |
| Port | `14330` |
| Database | `MqSampleDb` |
| User | `sa` |
| Password | `YourStrong!Passw0rd` |
| Encryption | Trust server cert (self-signed) |

Connection string (ADO.NET style):

```
Server=localhost,14330;Database=MqSampleDb;User ID=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;
```

- **SSMS / Azure Data Studio**: server name `localhost,14330`, SQL auth,
  login `sa`, password as above, check "Trust server certificate".
- **sqlcmd**, without installing it locally — run it inside the container:
  ```powershell
  docker exec -it mq_import_sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "YourStrong!Passw0rd" -d MqSampleDb
  ```
- **VS Code "SQL Server (mssql)" extension**: same server/port/user/password.

The container only publishes to `localhost`, so it's reachable from the host
machine only. To use a different port/password, pass `-HostPort` /
`-SaPassword` to `scripts\01-start-db.ps1` (start fresh — a running
container isn't reconfigured by re-running the script).

Tear down when done:

```powershell
.\scripts\03-stop-db.ps1              # keep the data volume
.\scripts\03-stop-db.ps1 -RemoveData  # also delete the volume
```

## Going from this dry run to the real work DB

Everything in `scripts/02-extract-mq.ps1` mirrors
[`import-mq-schema-notes.md`](import-mq-schema-notes.md) Step 1. To
point it at the real database instead of the container, just change the
`/SourceConnectionString` (server, database, auth) — same `sqlpackage`
invocation, same `ExtractTarget:SchemaObjectType` trick, same "copy the `mq`
folder + `Security\mq.sql`" step. No Windows-native SQL Server tooling is
needed there either, as long as you have network access to the work SQL
Server from wherever you run `sqlpackage` (it works from Windows, WSL,
Linux, or macOS).
