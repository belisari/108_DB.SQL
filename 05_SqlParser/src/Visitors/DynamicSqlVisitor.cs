using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlMigrationValidator.Visitors;

/// <summary>
/// Detects dynamic SQL execution and bulk operations.
/// Dynamic SQL cannot be statically analysed — the generated string may itself
/// contain forbidden statements. Bulk operations cannot participate in a
/// standard transaction.
/// </summary>
public sealed class DynamicSqlVisitor : MigrationVisitorBase
{
    public DynamicSqlVisitor(string filePath) : base(filePath) { }

    public override void Visit(ExecuteStatement node)
    {
        // EXEC dbo.SomeProc — stored proc calls are fine, skip them.
        if (node.ExecuteSpecification?.ExecutableEntity is ExecutableProcedureReference)
            return;

        // EXEC (@sql) or EXEC sp_executesql @sql — dynamic string execution.
        AddWarning("DYNAMIC_SQL",
            "EXECUTE with a dynamic SQL string detected — ensure the generated SQL also complies with these rules.",
            node);
    }

    public override void Visit(BulkInsertStatement node) =>
        AddError("NO_BULK_INSERT",
            "BULK INSERT is forbidden — it cannot participate in a standard transaction.",
            node);
}
