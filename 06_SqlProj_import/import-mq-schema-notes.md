# Importing the `[mq]` schema into a SQL Server Database Project

Goal: end up with a `.sqlproj` that has a folder layout like:

```
mq/
  Tables/
    Table1.sql
    Table2.sql
  StoredProcedures/
    Proc1.sql
  Views/
  Functions/
  ...
```

This repo does not currently contain a real database project (the only
`.sqlproj` found is `05_SqlParser\src\SqlMigrationValidator.SqlProjConsumer\...`,
which is a tooling/consumer project, not a place to import a live schema
into). So step 0 below covers creating a project if you don't already have
one elsewhere.

## Prerequisites (at work, where you have DB access)

- Access to the MS SQL Server instance/database that owns the `[mq]` schema.
- **SqlPackage** CLI. Either:
  - `dotnet tool install -g microsoft.sqlpackage`, or
  - it ships with SQL Server Data Tools (SSDT) / Visual Studio under
    `...\Microsoft SQL Server\...\DAC\bin\SqlPackage.exe`.
- Visual Studio with the "SQL Server Data Tools" workload if you want to use
  the GUI import instead of the CLI (optional — CLI is enough).

## Step 0 — Create the target project (if you don't have one)

Pick one:

- **SDK-style project (recommended, modern)**: uses the `Microsoft.Build.Sql`
  SDK. `.sql` files under the project folder are auto-included — no need to
  edit the `.sqlproj` when you add files.

  ```powershell
  dotnet new install Microsoft.Build.Sql.Templates
  dotnet new sqlproj -n MyDatabase
  ```

- **Classic SSDT project**: created via Visual Studio → "New Project" →
  "SQL Server Database Project". Files must be explicitly added to the
  project (Visual Studio does this automatically when you use
  "Add > Existing Item", or right-click a folder → "Add").

If you already have an existing `.sqlproj` (classic or SDK-style) that you
want to add `mq` into, just skip to Step 1 and extract into that project's
folder.

## Step 1 — Extract the whole database with a schema/object-type folder layout

The simplest reliable way to get a `mq\Tables\...`, `mq\StoredProcedures\...`
layout is to let SqlPackage extract the **whole** database using the
`SchemaObjectType` extraction target, which naturally creates one folder per
schema and one subfolder per object type. You then just copy the `mq`
subfolder out.

```powershell
sqlpackage /Action:Extract `
  /SourceConnectionString:"Server=YOUR_SERVER;Database=YOUR_DB;Integrated Security=True;TrustServerCertificate=True;" `
  /TargetFile:"C:\temp\extract\YOUR_DB" `
  /p:ExtractTarget=SchemaObjectType `
  /p:ExtractAllTableData=False
```

Notes:
- `/p:` properties use `=` (e.g. `/p:ExtractTarget=SchemaObjectType`), unlike
  the top-level `/Action:` / `/TargetFile:` flags, which use `:`.
- `/TargetFile` here is a **folder** path (not `.dacpac`) because
  `ExtractTarget=SchemaObjectType` produces a file tree, not a single dacpac.
- `/p:ExtractAllTableData=False` keeps this schema-only (no row data). Omit
  it only if you also want data extracted as BCP files — not usually what
  you want for source control.
- If you only have SQL auth: replace `Integrated Security=True` with
  `User ID=...;Password=...`.
- If Extract fails on permissions for objects outside `[mq]`, you likely
  don't have rights to the whole DB — see the SSMS alternative in Step 1b.

After this runs, you'll have something like:

```
C:\temp\extract\YOUR_DB\
  dbo\
    Tables\...
    StoredProcedures\...
  mq\
    Tables\...
    StoredProcedures\...
    Views\...
    Functions\...
  Security\
    dbo.sql          (CREATE SCHEMA statements etc.)
    mq.sql
```

Copy just what you need:

```powershell
Copy-Item "C:\temp\extract\YOUR_DB\mq" "C:\Source\108_DB.SQL\<YourProject>\mq" -Recurse
```

**Important:** also copy the schema-creation script so `CREATE SCHEMA [mq]`
exists in the project (it lives in the `Security` folder, not inside `mq`):

```powershell
New-Item -ItemType Directory -Force "C:\Source\108_DB.SQL\<YourProject>\Security" | Out-Null
Copy-Item "C:\temp\extract\YOUR_DB\Security\mq.sql" "C:\Source\108_DB.SQL\<YourProject>\Security\mq.sql"
```

## Step 1b — Alternative: SSMS "Generate Scripts" (if you can't extract the whole DB)

If you only have access/permissions to the `mq` schema specifically (not the
whole database), use SSMS instead:

1. In SSMS Object Explorer, right-click the database → **Tasks → Generate
   Scripts...**
2. Choose **"Select specific database objects"**, expand the tree, and
   check only the tables/views/procs/functions under the `mq` schema.
3. On the "Set Scripting Options" page:
   - Output: **"Script to a specific location"**, choose
     **"one file per object"**, pick an output folder.
   - Advanced options → **"Script CREATE"**, **"Types of data to script:
     Schema only"**.
4. Finish. You'll get a folder of individual `.sql` files named
   `mq.TableName.sql`, `mq.ProcName.sql`, etc.
5. Manually sort them into subfolders to match SSDT conventions:
   - `mq\Tables\`
   - `mq\StoredProcedures\`
   - `mq\Views\`
   - `mq\Functions\`
6. Manually add a `Security\mq.sql` containing:
   ```sql
   CREATE SCHEMA [mq]
   GO
   ```

This is more manual but works when you don't have extract rights on the
full database.

## Step 2 — Add the files to the project

- **SDK-style project**: nothing to do — `.sql` files under the project
  directory are picked up automatically by the glob in the `.sqlproj`.
- **Classic SSDT project**: in Visual Studio, right-click the `mq` folder
  (after copying files into the project directory on disk) → **Add →
  Existing Item...** → select all the copied `.sql` files (do this per
  subfolder). Alternatively edit the `.sqlproj` XML directly and add
  `<Build Include="mq\Tables\Table1.sql" />` entries for each file — tedious
  by hand, so prefer the VS UI or a small script that globs the folder and
  writes the `<Build Include>` entries into the `.sqlproj`.

## Step 3 — Build and fix cross-schema references

- Build the project (`dotnet build` for SDK-style, or build in Visual
  Studio). Common issues after a partial import:
  - References from `mq` objects to `dbo` (or other) objects that weren't
    imported → either import those referenced objects too, or add them as
    an external reference / synonym, or exclude the offending statements.
  - Missing users/roles/permissions statements (Extract usually skips
    server-level principals) — usually fine to omit from the project.
- If you extracted the whole DB in Step 1 but only want `mq` in source
  control, you can still keep the rest as reference locally (not copied
  into the project) to resolve any cross-schema build errors, then decide
  later whether to bring those schemas in too.

## Quick reference — commands only

```powershell
# Extract whole DB into schema/object-type folder layout
sqlpackage /Action:Extract /SourceConnectionString:"Server=SRV;Database=DB;Integrated Security=True;TrustServerCertificate=True;" /TargetFile:"C:\temp\extract\DB" /p:ExtractTarget=SchemaObjectType /p:ExtractAllTableData=False

# Copy just the mq schema + its CREATE SCHEMA script into your project
Copy-Item "C:\temp\extract\DB\mq" "<ProjectDir>\mq" -Recurse
Copy-Item "C:\temp\extract\DB\Security\mq.sql" "<ProjectDir>\Security\mq.sql"
```
