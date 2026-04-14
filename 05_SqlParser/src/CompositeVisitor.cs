using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqlMigrationValidator.Visitors;

namespace SqlMigrationValidator;

public class CompositeVisitor(string filePath)
{
    private static readonly IReadOnlyList<Type> VisitorTypes =
        typeof(VisitorBase).Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(VisitorBase)) && !t.IsAbstract)
            .ToList();

    public IReadOnlyList<Violation> Accept(TSqlFragment fragment)
    {
        List<VisitorBase> visitors = VisitorTypes
            .Select(t => (VisitorBase)Activator.CreateInstance(t, filePath)!)
            .ToList();

        foreach (VisitorBase visitor in visitors)
            fragment.Accept(visitor);

        return visitors
            .SelectMany(v => v.Violations)
            .OrderBy(v => v.Line)
            .ThenBy(v => v.Column)
            .ToList();
    }
}