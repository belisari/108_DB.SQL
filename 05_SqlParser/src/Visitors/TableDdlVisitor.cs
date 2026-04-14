using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlMigrationValidator.Visitors;

/// <summary>
/// Detects table DDL statements.
/// All table schema changes (CREATE / ALTER / DROP / TRUNCATE) belong in the
/// dacpac, not in data migration scripts.
/// </summary>
public sealed class TableDdlVisitor : MigrationVisitorBase
{
    public TableDdlVisitor(string filePath) : base(filePath) { }

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
}
