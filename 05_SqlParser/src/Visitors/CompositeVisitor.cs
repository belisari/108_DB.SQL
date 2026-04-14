using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqlMigrationValidator.Visitors;

namespace SqlMigrationValidator.Visitors;

/// <summary>
/// Runs all registered visitors against a parsed T-SQL fragment and
/// aggregates their violations.
///
/// To add a new rule category:
///   1. Create a new class in the Visitors/ folder that extends MigrationVisitorBase.
///   2. Register it in the BuildVisitors() method below — nothing else changes.
/// </summary>
public class CompositeVisitor
{
    private readonly IReadOnlyList<MigrationVisitorBase> _visitors;

    public CompositeVisitor(string filePath)
    {
        _visitors = BuildVisitors(filePath);
    }

    private static IReadOnlyList<MigrationVisitorBase> BuildVisitors(string filePath) =>
    [
        new TransactionControlVisitor(filePath),
        new IndexDdlVisitor(filePath),
        new TableDdlVisitor(filePath),
        new SchemaObjectDdlVisitor(filePath),
        new SessionConfigVisitor(filePath),
        new DynamicSqlVisitor(filePath),
        new GlobalVariableVisitor(filePath),
    ];

    /// <summary>
    /// Accepts the parsed fragment into every visitor and returns all violations,
    /// sorted by line then column so output matches source order.
    /// </summary>
    public IReadOnlyList<Violation> Accept(TSqlFragment fragment)
    {
        foreach (var visitor in _visitors)
            fragment.Accept(visitor);

        return _visitors
            .SelectMany(v => v.Violations)
            .OrderBy(v => v.Line)
            .ThenBy(v => v.Column)
            .ToList();
    }
}
