using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlMigrationValidator.Visitors;

/// <summary>
/// Detects statements that change session state or database context.
/// These can affect subsequent scripts in the combined post-deployment script
/// in unpredictable ways.
/// </summary>
public sealed class SessionConfigVisitor : MigrationVisitorBase
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
}
