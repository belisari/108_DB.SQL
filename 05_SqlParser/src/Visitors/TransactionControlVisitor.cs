using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlMigrationValidator.Visitors;

/// <summary>
/// Detects explicit transaction control statements.
/// The post-deployment script runs inside a single managed transaction —
/// nested BEGIN/COMMIT/ROLLBACK/SAVE will corrupt it.
/// </summary>
public sealed class TransactionControlVisitor : MigrationVisitorBase
{
    public TransactionControlVisitor(string filePath) : base(filePath) { }

    public override void Visit(BeginTransactionStatement node) =>
        AddError("NO_BEGIN_TRANSACTION",
            "BEGIN TRANSACTION is forbidden — do not open nested transactions in migration scripts.",
            node);

    public override void Visit(CommitTransactionStatement node) =>
        AddError("NO_EXPLICIT_COMMIT",
            "Explicit COMMIT is forbidden — the post-deployment script runs in a single managed transaction.",
            node);

    public override void Visit(RollbackTransactionStatement node) =>
        AddError("NO_EXPLICIT_ROLLBACK",
            "Explicit ROLLBACK is forbidden — let the outer transaction handle rollback on failure.",
            node);

    public override void Visit(SaveTransactionStatement node) =>
        AddError("NO_SAVE_TRANSACTION",
            "SAVE TRANSACTION (savepoints) is forbidden in migration scripts.",
            node);
}
