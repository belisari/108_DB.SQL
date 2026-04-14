using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqlMigrationValidator.Visitors;

namespace SqlMigrationValidator;

public class MigrationScriptValidator
{
    // Use the SQL Server version matching your target environment.
    // TSql160Parser = SQL Server 2022, TSql150Parser = 2019, TSql130Parser = 2016
    private static TSqlParser CreateParser() =>
        new TSql160Parser(initialQuotedIdentifiers: true);

    /// <summary>
    /// Validates a single .sql file. Returns all violations found.
    /// </summary>
    public IReadOnlyList<Violation> ValidateFile(string filePath)
    {
        string sql;
        try
        {
            sql = File.ReadAllText(filePath);
        }
        catch (Exception ex)
        {
            return
            [
                new Violation(filePath, "FILE_READ_ERROR", ex.Message, 0, 0, Severity.Error)
            ];
        }

        return ValidateSql(sql, filePath);
    }

    /// <summary>
    /// Validates a SQL string directly (useful for unit tests).
    /// </summary>
    public IReadOnlyList<Violation> ValidateSql(string sql, string filePath = "<inline>")
    {
        var violations = new List<Violation>();
        var parser = CreateParser();

        using var reader = new StringReader(sql);
        var fragment = parser.Parse(reader, out var parseErrors);

        // Parse errors themselves are violations
        foreach (var err in parseErrors)
        {
            violations.Add(new Violation(
                filePath,
                "PARSE_ERROR",
                $"SQL syntax error: {err.Message}",
                err.Line,
                err.Column,
                Severity.Error));
        }

        // Even with parse errors, ScriptDom returns a partial AST — still walk it
        var visitor = new ForbiddenStatementVisitor(filePath);
        fragment.Accept(visitor);
        violations.AddRange(visitor.Violations);

        return violations;
    }

    /// <summary>
    /// Validates all .sql files in a directory (recursive).
    /// </summary>
    public IReadOnlyList<Violation> ValidateDirectory(string directory, string searchPattern = "*.sql")
    {
        if (!Directory.Exists(directory))
            return [new Violation(
                directory, "DIR_NOT_FOUND",
                $"Directory not found: {directory}",
                0,
                0,
                Severity.Error)
            ];

        var allViolations = new List<Violation>();
        var files = Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories);

        foreach (var file in files.OrderBy(f => f))
        {
            allViolations.AddRange(ValidateFile(file));
        }

        return allViolations;
    }
}
