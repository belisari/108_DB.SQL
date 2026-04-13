using SqlMigrationValidator;



if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: SqlMigrationValidator <directory|file.sql>");
    return 1;
}

var target = args[0];
var validator = new MigrationScriptValidator();

IReadOnlyList<Violation> violations;

if (File.Exists(target))
{
    Console.WriteLine($"Validating file: {target}");
    violations = validator.ValidateFile(target);
}
else if (Directory.Exists(target))
{
    Console.WriteLine($"Validating directory: {target}");
    violations = validator.ValidateDirectory(target);
}
else
{
    Console.Error.WriteLine($"Error: '{target}' is not a valid file or directory.");
    return 1;
}



if (violations.Count == 0)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\n✓ No violations found. All migration scripts are clean.");
    Console.ResetColor();
    return 0;
}

// Group by file for readable output
var byFile = violations.GroupBy(v => v.FilePath).OrderBy(g => g.Key);

foreach (var group in byFile)
{
    Console.WriteLine($"\n── {Path.GetFileName(group.Key)} ({group.Key})");

    foreach (var v in group.OrderBy(v => v.Line).ThenBy(v => v.Column))
    {
        Console.ForegroundColor = v.Severity == Severity.Error
            ? ConsoleColor.Red
            : ConsoleColor.Yellow;

        var icon = v.Severity == Severity.Error ? "✗ ERROR  " : "⚠ WARNING";
        Console.WriteLine($"   {icon}  Line {v.Line,4}, Col {v.Column,3}  [{v.RuleName}]");
        Console.ResetColor();
        Console.WriteLine($"            {v.Message}");
    }
}

// Summary
int errorCount   = violations.Count(v => v.Severity == Severity.Error);
int warningCount = violations.Count(v => v.Severity == Severity.Warning);

Console.WriteLine();
Console.ForegroundColor = errorCount > 0 ? ConsoleColor.Red : ConsoleColor.Yellow;
Console.WriteLine($"Summary: {errorCount} error(s), {warningCount} warning(s) across {byFile.Count()} file(s).");
Console.ResetColor();

// CI exit codes
if (errorCount > 0)   return 1;  // Hard failure — block the pipeline
if (warningCount > 0) return 2;  // Soft failure — optionally block
return 0;
