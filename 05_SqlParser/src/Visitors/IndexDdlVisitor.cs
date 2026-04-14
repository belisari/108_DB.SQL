using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlMigrationValidator.Visitors;

/// <summary>
/// Detects index DDL statements.
/// Index changes belong in the dacpac schema project, not in data migration scripts.
/// ALTER INDEX REBUILD/REORGANIZE also causes long locks that must not run inside
/// the post-deployment transaction.
/// </summary>
public sealed class IndexDdlVisitor : MigrationVisitorBase
{
    public IndexDdlVisitor(string filePath) : base(filePath) { }

    public override void Visit(CreateIndexStatement node) =>
        AddError("NO_CREATE_INDEX",
            $"CREATE INDEX [{node.Name?.Value}] is forbidden — index changes belong in the dacpac schema.",
            node);

    public override void Visit(DropIndexStatement node) =>
        AddError("NO_DROP_INDEX",
            "DROP INDEX is forbidden — index changes belong in the dacpac schema.",
            node);

    public override void Visit(AlterIndexStatement node) =>
        AddError("NO_ALTER_INDEX",
            $"ALTER INDEX ({node.AlterIndexType}) is forbidden — index maintenance must not run inside migration transactions.",
            node);
}
