using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlMigrationValidator.Visitors;

public sealed class SessionConfigVisitor : VisitorBase
{
    public SessionConfigVisitor(string filePath) : base(filePath) { }

    public override void Visit(UseStatement node) =>
        AddError("NO_USE_DATABASE",
            $"USE [{node.DatabaseName.Value}] is forbidden — do not switch database context inside a migration script.",
            node);

    public override void Visit(SetTransactionIsolationLevelStatement node) =>
        AddWarning("NO_SET_ISOLATION",
            $"SET TRANSACTION ISOLATION LEVEL {node.Level} can interfere with the outer transaction — discuss with the DBA team.",
            node);

    public override void Visit(WaitForStatement node) =>
        AddWarning("NO_WAITFOR",
            "WAITFOR will block the entire post-deployment transaction for its duration — remove it or move the delay logic to the pipeline.",
            node);
}
