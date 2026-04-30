using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlMigrationValidator.Visitors;

public sealed class ExampleVisitor(string filePath) : VisitorBase(filePath)
{
    // ✅ GOOD — rule name is a const: analyzer stays silent
    private const string NoPrintRule = "NO_PRINT";

    public override void Visit(PrintStatement node) =>
        AddError(NoPrintRule, "PRINT statements are not allowed in migration scripts.", node);

    // ❌ BAD — raw string literal: analyzer fires SMV001 on the two lines below
    public override void Visit(RaiseErrorStatement node) =>
        AddError("NO_RAISERROR", "RAISERROR is not allowed; migrations must not raise custom errors.", node);

    public override void Visit(ThrowStatement node) =>
        AddWarning("NO_THROW", "THROW is not allowed; migrations must not raise custom errors.", node);
}
