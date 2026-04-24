using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlMigrationValidator.Visitors;

/// <summary>
/// Detects references to variables that were never declared in the same batch.
/// Reports warnings rather than errors — cross-batch declarations and proc
/// parameters are legitimate patterns that look undeclared from a single-batch
/// perspective.
/// </summary>
public sealed class UndeclaredVariableVisitor : VisitorBase
{
    public UndeclaredVariableVisitor(string filePath) : base(filePath) { }

    public override void Visit(TSqlBatch node)
    {
        // Collect declarations and references separately per batch.
        // Each GO-separated block is an independent scope in SQL Server.
        var declaredVisitor = new DeclarationCollector();
        var referenceVisitor = new ReferenceCollector();

        node.Accept(declaredVisitor);
        node.Accept(referenceVisitor);

        foreach (var (name, refNode) in referenceVisitor.References)
        {
            if (!declaredVisitor.DeclaredNames.Contains(name))
            {
                AddWarning("UNDECLARED_VARIABLE",
                    $"{name} is used but never declared in this batch — " +
                    $"check for a missing DECLARE or a typo.",
                    refNode);
            }
        }
    }

    // ------------------------------------------------------------------
    // Inner collectors
    // ------------------------------------------------------------------

    private sealed class DeclarationCollector : TSqlFragmentVisitor
    {
        public HashSet<string> DeclaredNames { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        // DECLARE @x INT
        public override void Visit(DeclareVariableElement node)
        {
            if (node.VariableName?.Value is string name)
                DeclaredNames.Add(name);   // stored with @  e.g. "@counter"
        }

        // Stored procedure / function parameters (@param in the signature)
        public override void Visit(ProcedureParameter node)
        {
            if (node.VariableName?.Value is string name)
                DeclaredNames.Add(name);
        }
    }

    private sealed class ReferenceCollector : TSqlFragmentVisitor
    {
        // (variableName, the AST node for line/col reporting)
        public List<(string Name, VariableReference Node)> References { get; } = new();

        public override void Visit(VariableReference node)
        {
            // Skip @@globals — those are caught by GlobalVariableVisitor
            if (node.Name.StartsWith("@@"))
                return;

            References.Add((node.Name, node));
        }
    }
}