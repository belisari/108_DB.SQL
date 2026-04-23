using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlMigrationValidator.Visitors;

public abstract class VisitorBase : TSqlFragmentVisitor
{
    private readonly List<Violation> _violations = new();
    protected string FilePath { get; }

    public IReadOnlyList<Violation> Violations => _violations;

    protected VisitorBase(string filePath) => FilePath = filePath;

    protected void AddError(string rule, string message, TSqlFragment node) =>
        _violations.Add(new Violation(
            FilePath, rule, message,
            node.StartLine, node.StartColumn,
            Severity.Error));

    protected void AddWarning(string rule, string message, TSqlFragment node) =>
        _violations.Add(new Violation(
            FilePath, rule, message,
            node.StartLine, node.StartColumn,
            Severity.Warning));

    protected static string TableName(SchemaObjectName? name) =>
        name?.BaseIdentifier?.Value ?? "?";
}
