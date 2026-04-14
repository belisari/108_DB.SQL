using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlMigrationValidator.Visitors;

/// <summary>
/// Walks the T-SQL AST and collects violations.
/// Add new Visit* overrides here to detect more statement types.
/// </summary>
public class ForbiddenStatementVisitor : TSqlFragmentVisitor
{
    private readonly string _filePath;
    private readonly List<Violation> _violations = new();

    public IReadOnlyList<Violation> Violations => _violations;

    /// <summary>
    /// Global variables (@@name) that are forbidden in migration scripts.
    /// Stored normalised to uppercase, with @@ prefix.
    /// </summary>
    private static readonly HashSet<string> ForbiddenGlobals = new(StringComparer.OrdinalIgnoreCase)
    {
        // Transaction state — reading these implies scripts are trying to
        // inspect or branch on the managed transaction, which is forbidden.
        "@@TRANCOUNT",
        "@@NESTLEVEL",

        // Error-handling globals — use TRY/CATCH instead.
        "@@ERROR",

        // Row/identity side-effects that can mislead in combined scripts.
        "@@ROWCOUNT",
        "@@IDENTITY",       // use SCOPE_IDENTITY() or OUTPUT instead

        // Server/connection state — scripts must not be environment-aware.
        "@@SERVERNAME",
        "@@VERSION",
        "@@SPID",
        "@@DBTS",
    };

    /// <summary>
    /// Reason hints shown alongside the violation message, keyed by normalised name.
    /// Falls back to a generic message for unlisted entries.
    /// </summary>
    private static readonly Dictionary<string, string> GlobalReasons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["@@TRANCOUNT"] = "Reading @@TRANCOUNT implies dependency on transaction nesting level — scripts must not inspect or branch on the managed transaction.",
        ["@@NESTLEVEL"] = "@@NESTLEVEL exposes execution nesting — migration scripts must not depend on how they are called.",
        ["@@ERROR"] = "@@ERROR is legacy error handling — use TRY/CATCH blocks instead.",
        ["@@ROWCOUNT"] = "@@ROWCOUNT after a statement is unreliable when scripts are combined — capture row counts via OUTPUT or explicit variables if needed.",
        ["@@IDENTITY"] = "@@IDENTITY returns the last identity across the whole session and can return unexpected values in combined scripts — use SCOPE_IDENTITY() or an OUTPUT clause instead.",
        ["@@SERVERNAME"] = "Scripts must not branch on server name — environment-specific logic belongs in pipeline variables, not in migration SQL.",
        ["@@VERSION"] = "Scripts must not branch on server version — target a minimum supported version and write accordingly.",
        ["@@SPID"] = "@@SPID (session ID) is environment-specific and must not be used in migration scripts.",
        ["@@DBTS"] = "@@DBTS (current timestamp) is non-deterministic across environments — use GETUTCDATE() or SYSDATETIMEOFFSET() instead.",
    };

    public ForbiddenStatementVisitor(string filePath)
    {
        _filePath = filePath;
    }

    // -------------------------------------------------------------------------
    // Transaction control
    // -------------------------------------------------------------------------

    public override void Visit(CommitTransactionStatement node) =>
        AddError("NO_EXPLICIT_COMMIT",
            "Explicit COMMIT is forbidden — the post-deployment script runs in a single managed transaction.",
            node);

    public override void Visit(RollbackTransactionStatement node) =>
        AddError("NO_EXPLICIT_ROLLBACK",
            "Explicit ROLLBACK is forbidden — let the outer transaction handle rollback on failure.",
            node);

    public override void Visit(BeginTransactionStatement node) =>
        AddError("NO_BEGIN_TRANSACTION",
            "BEGIN TRANSACTION is forbidden — do not open nested transactions in migration scripts.",
            node);

    public override void Visit(SaveTransactionStatement node) =>
        AddError("NO_SAVE_TRANSACTION",
            "SAVE TRANSACTION (savepoints) is forbidden in migration scripts.",
            node);

    // -------------------------------------------------------------------------
    // Index DDL
    // -------------------------------------------------------------------------

    public override void Visit(CreateIndexStatement node) =>
        AddError("NO_CREATE_INDEX",
            $"CREATE INDEX [{node.Name?.Value}] is forbidden — index changes belong in the dacpac schema.",
            node);

    public override void Visit(DropIndexStatement node) =>
        AddError("NO_DROP_INDEX",
            "DROP INDEX is forbidden — index changes belong in the dacpac schema.",
            node);

    public override void Visit(AlterIndexStatement node)
    {
        // ALTER INDEX ... REBUILD / REORGANIZE can cause long locks and implicit commits
        var operation = node.AlterIndexType.ToString();
        AddError("NO_ALTER_INDEX",
            $"ALTER INDEX ({operation}) is forbidden — index maintenance must not run inside migration transactions.",
            node);
    }

    // -------------------------------------------------------------------------
    // Table DDL
    // -------------------------------------------------------------------------

    public override void Visit(CreateTableStatement node) =>
        AddError("NO_CREATE_TABLE",
            $"CREATE TABLE [{TableName(node.SchemaObjectName)}] is forbidden — schema changes belong in the dacpac.",
            node);

    public override void Visit(DropTableStatement node) =>
        AddError("NO_DROP_TABLE",
            "DROP TABLE is forbidden — schema changes belong in the dacpac.",
            node);

    public override void Visit(AlterTableStatement node) =>
        AddError("NO_ALTER_TABLE",
            $"ALTER TABLE [{TableName(node.SchemaObjectName)}] is forbidden — schema changes belong in the dacpac.",
            node);

    public override void Visit(TruncateTableStatement node) =>
        AddError("NO_TRUNCATE",
            $"TRUNCATE TABLE [{TableName(node.TableName)}] is forbidden — use DELETE with a WHERE clause.",
            node);

    // -------------------------------------------------------------------------
    // Other schema objects
    // -------------------------------------------------------------------------

    public override void Visit(CreateViewStatement node) =>
        AddError("NO_CREATE_VIEW",
            $"CREATE VIEW [{TableName(node.SchemaObjectName)}] is forbidden in data migration scripts.",
            node);

    public override void Visit(AlterViewStatement node) =>
        AddError("NO_ALTER_VIEW",
            $"ALTER VIEW is forbidden in data migration scripts.",
            node);

    public override void Visit(CreateProcedureStatement node) =>
        AddError("NO_CREATE_PROC",
            $"CREATE PROCEDURE is forbidden in data migration scripts.",
            node);

    public override void Visit(AlterProcedureStatement node) =>
        AddError("NO_ALTER_PROC",
            "ALTER PROCEDURE is forbidden in data migration scripts.",
            node);

    public override void Visit(CreateFunctionStatement node) =>
        AddError("NO_CREATE_FUNCTION",
            "CREATE FUNCTION is forbidden in data migration scripts.",
            node);

    public override void Visit(AlterFunctionStatement node) =>
        AddError("NO_ALTER_FUNCTION",
            "ALTER FUNCTION is forbidden in data migration scripts.",
            node);

    // -------------------------------------------------------------------------
    // Dangerous session/config changes
    // -------------------------------------------------------------------------

    public override void Visit(UseStatement node) =>
        AddError("NO_USE_DATABASE",
            $"USE [{node.DatabaseName.Value}] is forbidden — do not switch database context inside a migration script.",
            node);

    public override void Visit(SetTransactionIsolationLevelStatement node) =>
        AddWarning("NO_SET_ISOLATION",
            $"SET TRANSACTION ISOLATION LEVEL {node.Level} can interfere with the outer transaction — discuss with the DBA team.",
            node);

    // -------------------------------------------------------------------------
    // Dynamic SQL — warn, don't hard-fail (may be legitimate)
    // -------------------------------------------------------------------------

    public override void Visit(ExecuteStatement node)
    {
        // Flag EXEC/EXECUTE of a string (dynamic SQL) — not stored proc calls
        if (node.ExecuteSpecification?.ExecutableEntity is ExecutableProcedureReference procRef)
        {
            // Stored proc calls are fine — skip
            return;
        }

        AddWarning("DYNAMIC_SQL",
            "EXECUTE with a dynamic SQL string detected — ensure the generated SQL also complies with these rules.",
            node);
    }

    // -------------------------------------------------------------------------
    // Bulk operations
    // -------------------------------------------------------------------------

    public override void Visit(BulkInsertStatement node) =>
        AddError("NO_BULK_INSERT",
            "BULK INSERT is forbidden — it cannot participate in a standard transaction.",
            node);

    // -------------------------------------------------------------------------
    // Forbidden global variables  (@@name)
    // -------------------------------------------------------------------------

    public override void Visit(GlobalVariableExpression node)
    {
        // node.Name is already the full "@@TRANCOUNT" string (with @@ prefix).
        if (!ForbiddenGlobals.Contains(node.Name))
            return;

        var reason = GlobalReasons.TryGetValue(node.Name, out var hint)
            ? hint
            : $"{node.Name} is not permitted in migration scripts.";

        AddError("NO_GLOBAL_VARIABLE", $"{node.Name}: {reason}", node);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void AddError(string rule, string message, TSqlFragment node) =>
        _violations.Add(new Violation(
            _filePath, rule, message,
            node.StartLine, node.StartColumn,
            Severity.Error));

    private void AddWarning(string rule, string message, TSqlFragment node) =>
        _violations.Add(new Violation(
            _filePath, rule, message,
            node.StartLine, node.StartColumn,
            Severity.Warning));

    private static string TableName(SchemaObjectName? name) =>
        name?.BaseIdentifier?.Value ?? "?";
}
