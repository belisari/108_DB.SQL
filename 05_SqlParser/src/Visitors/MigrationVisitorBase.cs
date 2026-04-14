using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlMigrationValidator.Visitors;

/// <summary>
/// Base class for all migration script visitors.
/// Provides shared AddError / AddWarning helpers and the violation list.
/// Subclasses override Visit* methods for the statement types they care about.
/// </summary>
public abstract class MigrationVisitorBase : TSqlFragmentVisitor
{
    private readonly List<Violation> _violations = new();
    protected string FilePath { get; }

    public IReadOnlyList<Violation> Violations => _violations;

    protected MigrationVisitorBase(string filePath)
    {
        FilePath = filePath;
    }

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
