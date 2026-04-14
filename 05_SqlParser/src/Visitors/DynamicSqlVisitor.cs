using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlMigrationValidator.Visitors;

public sealed class DynamicSqlVisitor : VisitorBase
{
    public DynamicSqlVisitor(string filePath) : base(filePath) { }

    public override void Visit(ExecuteStatement node)
    {
        // EXEC dbo.SomeProc — stored proc calls are fine, skip them.
        if (node.ExecuteSpecification?.ExecutableEntity is ExecutableProcedureReference procRef)
        {
            // sp_executesql is a proc call but executes dynamic SQL — treat as dynamic
            var procName = procRef.ProcedureReference?.ProcedureReference?.Name?.BaseIdentifier?.Value;
            if (string.Equals(procName, "sp_executesql", StringComparison.OrdinalIgnoreCase))
            {
                AddWarning("DYNAMIC_SQL",
                    "sp_executesql executes a dynamic SQL string — ensure the generated SQL also complies with these rules.",
                    node);
            }
            return;
        }

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

